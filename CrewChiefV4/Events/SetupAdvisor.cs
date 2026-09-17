using System;
using System.Collections.Generic;
using System.Linq;
using CrewChiefV4.Audio;
using CrewChiefV4.GameState;
using CrewChiefV4.HeadlessSimulation;
using CrewChiefV4.assetto.assettoData;

namespace CrewChiefV4.Events
{
    public class SetupAdvisor : AbstractEvent
    {
        private GripLossAnalyzer gripAnalyzer = new GripLossAnalyzer();
        private TelemetrySegmenter segmenter = new TelemetrySegmenter();
        private bool profileLoaded = false;
        
        // Accumulate diagnoses per stint for pit debrief
        private Queue<GripLossAnalyzer.GripDiagnosis> stintDiagnoses = new Queue<GripLossAnalyzer.GripDiagnosis>();
        private SessionDataRecorder sessionRecorder = new SessionDataRecorder();
        private int isOptimizerRunning = 0;
        
        private void ProcessACTelemetry(GameStateData currentGameState, TelemetryData physics, float normalizedPosition)
        {
            var activeTurn = segmenter.UpdatePosition(currentGameState.PositionAndMotionData.DistanceRoundTrack, normalizedPosition);
            
            GripLossAnalyzer.CornerPhase phase = GripLossAnalyzer.CornerPhase.Straight;
            if (activeTurn != null)
            {
                float distToApex = activeTurn.ApexNormalized - normalizedPosition;
                if (distToApex < -0.5f) distToApex += 1.0f;
                else if (distToApex > 0.5f) distToApex -= 1.0f;

                if (distToApex > 0.02f && physics.Brake > 0.1f)
                    phase = GripLossAnalyzer.CornerPhase.Entry;
                else if (distToApex < -0.02f && physics.Gas > 0.15f)
                    phase = GripLossAnalyzer.CornerPhase.Exit;
                else
                    phase = GripLossAnalyzer.CornerPhase.MidCorner;
            }
            
            sessionRecorder.RecordTelemetrySample(physics, currentGameState.PositionAndMotionData.DistanceRoundTrack, normalizedPosition, phase, currentGameState.SessionData.CompletedLaps);
            
            var diagnosis = gripAnalyzer.AnalyzeLiveTelemetry(physics, phase, activeTurn);
            
            if (diagnosis.IsOversteering || diagnosis.IsUndersteering || diagnosis.IsLockingBrakes)
            {
                sessionRecorder.RecordDiagnosis(diagnosis);
                if (stintDiagnoses.Count == 0 || stintDiagnoses.Last().Summary != diagnosis.Summary)
                {
                    if (stintDiagnoses.Count >= 50) stintDiagnoses.Dequeue();
                    stintDiagnoses.Enqueue(diagnosis);
                    CrewChiefV4.UserInterface.TopicWindows.TopicWindowSetupAdvisor.UpdateDiagnosis(diagnosis);
                }
            }
        }
        public SetupAdvisor(AudioPlayer audioPlayer) 
        { 
            this.audioPlayer = audioPlayer; 
        }
        
        protected override void triggerInternal(GameStateData previousGameState, GameStateData currentGameState)
        {
            if (currentGameState == null || currentGameState.SessionData.TrackDefinition == null || currentGameState.carClass == null) return;

            // Load track profile once per session
            if (!profileLoaded)
            {
                try
                {
                    segmenter.LoadTrackProfile(
                        currentGameState.carClass.getClassIdentifier(),
                        currentGameState.SessionData.TrackDefinition.name);
                }
                catch (Exception ex)
                {
                    CrewChiefV4.ConsoleLogger.Log.Verbose($"Could not load track profile: {ex.Message}");
                }
                profileLoaded = true;
            }

            if (previousGameState != null && previousGameState.PitData.InPitlane && !currentGameState.PitData.InPitlane)
            {
                try
                {
                    string track = currentGameState.SessionData.TrackDefinition.name;
                    string car = currentGameState.carClass.getClassIdentifier();
                    SetupFileParser.CarSetup setup = null;
                    if (CrewChief.gameDefinition.gameEnum == GameEnum.ASSETTO_CORSA_COMPETIZIONE)
                    {
                        string setupPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Assetto Corsa Competizione", "Customs", "Setups", car, track, "base_rec.json");
                        if (System.IO.File.Exists(setupPath)) setup = SetupFileParser.ParseACCSetup(setupPath);
                    }
                    else
                    {
                        string setupPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Assetto Corsa", "setups", car, track, "base_rec.ini");
                        if (System.IO.File.Exists(setupPath)) setup = SetupFileParser.ParseACSetup(setupPath);
                    }
                    if (setup != null)
                    {
                        CrewChiefV4.ConsoleLogger.Log.Verbose($"[Stint Start] Loaded Setup Snapshot: FrontARB={setup.FrontARB}, RearARB={setup.RearARB}");
                    }
                    sessionRecorder.StartStint(CrewChief.gameDefinition?.name ?? "Assetto", car, track, setup);
                }
                catch (Exception ex)
                {
                    CrewChiefV4.ConsoleLogger.Log.Verbose($"Error snapshotting setup at stint start: {ex.Message}");
                }
            }
            
            if (previousGameState != null && !previousGameState.PitData.InPitlane && currentGameState.PitData.InPitlane)
            {
                string savedRecordPath = sessionRecorder.StopStintAndSave(segmenter.GetStintDistanceMeters(), currentGameState.SessionData.CompletedLaps);
                try
                {
                    var pitAgent = new PitAdvisorAgent(audioPlayer);
                    string track = currentGameState.SessionData.TrackDefinition.name;
                    string car = currentGameState.carClass.getClassIdentifier();
                    string baseRecPath = "";
                    if (CrewChief.gameDefinition.gameEnum == GameEnum.ASSETTO_CORSA_COMPETIZIONE)
                    {
                        baseRecPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Assetto Corsa Competizione", "Customs", "Setups", car, track, "base_rec.json");
                    }
                    else
                    {
                        baseRecPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Assetto Corsa", "setups", car, track, "base_rec.ini");
                    }
                    
                    string speech = pitAgent.GenerateDebriefPrompt(track, baseRecPath, stintDiagnoses.ToArray());
                    pitAgent.SimulatePitInteraction(speech);
                }
                catch (Exception ex)
                {
                    CrewChiefV4.ConsoleLogger.Log.Verbose($"[PitAdvisorAgent] error in triggerInternal: {ex.Message}");
                }
                finally
                {
                    stintDiagnoses.Clear();
                }
            }

            if (CrewChief.gameDefinition.gameEnum == GameEnum.ASSETTO_64BIT || CrewChief.gameDefinition.gameEnum == GameEnum.ASSETTO_64BIT_UI)
            {
                var wrapper = currentGameState.rawGameData as CrewChiefV4.ACS.ACSSharedMemoryReader.ACSStructWrapper;
                if (wrapper != null)
                {
                    var acsPhysics = wrapper.data.acsPhysics;
                    var unifiedPhysics = new TelemetryData
                    {
                        WheelSlip = acsPhysics.wheelSlip,
                        AccG = acsPhysics.accG,
                        SteerAngle = acsPhysics.steerAngle,
                        Gas = acsPhysics.gas,
                        Brake = acsPhysics.brake,
                        SpeedKmh = acsPhysics.speedKmh
                    };
                    ProcessACTelemetry(currentGameState, unifiedPhysics, wrapper.data.acsGraphic.normalizedCarPosition);
                }
            }
            else if (CrewChief.gameDefinition.gameEnum == GameEnum.ASSETTO_CORSA_COMPETIZIONE)
            {
                var wrapper = currentGameState.rawGameData as CrewChiefV4.ACC.ACCSharedMemoryReader.ACCStructWrapper;
                if (wrapper != null)
                {
                    var accPhysics = wrapper.data.accPhysics;
                    var unifiedPhysics = new TelemetryData
                    {
                        WheelSlip = accPhysics.slipRatio,
                        AccG = accPhysics.accG,
                        SteerAngle = accPhysics.steerAngle,
                        Gas = accPhysics.gas,
                        Brake = accPhysics.brake,
                        SpeedKmh = accPhysics.speedKmh
                    };
                    ProcessACTelemetry(currentGameState, unifiedPhysics, wrapper.data.accGraphic.normalizedCarPosition);
                }
            }
        }

        public override void clearState() 
        { 
            profileLoaded = false;
            stintDiagnoses.Clear();
            segmenter.ClearState();
            sessionRecorder.StopStintAndSave(segmenter.GetStintDistanceMeters(), 0);
        }

        public override SpeechCommands.ID HandlesEvent(String voiceMessage)
        {
            if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.SETUP_ADVISOR_WHATS_WRONG))
                return SpeechCommands.ID.SETUP_ADVISOR_WHATS_WRONG;
            if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.SETUP_ADVISOR_SUGGEST_CHANGES))
                return SpeechCommands.ID.SETUP_ADVISOR_SUGGEST_CHANGES;
            if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.SETUP_ADVISOR_RUN_OPTIMIZER))
                return SpeechCommands.ID.SETUP_ADVISOR_RUN_OPTIMIZER;
            if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.SETUP_ADVISOR_HOW_AM_I_DOING))
                return SpeechCommands.ID.SETUP_ADVISOR_HOW_AM_I_DOING;
                
            return SpeechCommands.ID.NO_COMMAND;
        }

        private List<MessageFragment> TTSMessage(string msg)
        {
            var frags = MessageContents(msg);
            foreach (var frag in frags) { if (frag != null) frag.allowTTS = true; }
            return frags;
        }

        public override void respond(String voiceMessage, SpeechCommands.ID cmd)
        {
            if (cmd == SpeechCommands.ID.SETUP_ADVISOR_WHATS_WRONG)
            {
                string msg = stintDiagnoses.Count > 0 ? stintDiagnoses.Last().Summary : "I am analyzing the car's grip. Everything seems fine.";
                audioPlayer.playMessageImmediately(new QueuedMessage("setup_advisor_whats_wrong", 0, messageFragments: TTSMessage(msg)));
            }
            else if (cmd == SpeechCommands.ID.SETUP_ADVISOR_SUGGEST_CHANGES)
            {
                bool understeer = stintDiagnoses.Any(d => d.IsUndersteering);
                bool oversteer = stintDiagnoses.Any(d => d.IsOversteering);
                string msg = "I have no specific advice at this time.";
                try {
                    if (CrewChief.currentGameState?.SessionData?.TrackDefinition == null ||
                        CrewChief.currentGameState?.carClass == null)
                    {
                        audioPlayer.playMessageImmediately(new QueuedMessage("setup_advisor_suggest", 0, messageFragments: TTSMessage("Session metadata is not available yet.")));
                        return;
                    }
                    string track = CrewChief.currentGameState.SessionData.TrackDefinition.name;
                    string car = CrewChief.currentGameState.carClass.getClassIdentifier();

                    SetupFileParser.CarSetup setup = null;
                    if (CrewChief.gameDefinition.gameEnum == GameEnum.ASSETTO_CORSA_COMPETIZIONE)
                    {
                        string setupPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Assetto Corsa Competizione", "Customs", "Setups", car, track, "base_rec.json");
                        if (System.IO.File.Exists(setupPath)) setup = SetupFileParser.ParseACCSetup(setupPath);
                    }
                    else
                    {
                        string setupPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Assetto Corsa", "setups", car, track, "base_rec.ini");
                        if (System.IO.File.Exists(setupPath)) setup = SetupFileParser.ParseACSetup(setupPath);
                    }

                    if (setup != null) {
                        if (understeer) {
                            int newArb = Math.Max(0, setup.FrontARB - 1);
                            msg = $"Understeer detected mid-corner. I recommend softening the front anti-roll bar from {setup.FrontARB} to {newArb}.";
                        } else if (oversteer) {
                            int newArb = setup.RearARB + 1;
                            msg = $"Oversteer detected. I recommend stiffening the rear anti-roll bar from {setup.RearARB} to {newArb}.";
                        }
                    }
                } catch (Exception ex) { 
                    CrewChiefV4.ConsoleLogger.Log.Error($"Error in SETUP_ADVISOR_SUGGEST_CHANGES: {ex.Message}");
                }
                audioPlayer.playMessageImmediately(new QueuedMessage("setup_advisor_suggest", 0, messageFragments: TTSMessage(msg)));
            }
            else if (cmd == SpeechCommands.ID.SETUP_ADVISOR_RUN_OPTIMIZER)
            {
                if (System.Threading.Interlocked.CompareExchange(ref isOptimizerRunning, 1, 0) == 1)
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage("setup_advisor_optimizer_running", 0, messageFragments: TTSMessage("Optimizer is already running.")));
                    return;
                }

                audioPlayer.playMessageImmediately(new QueuedMessage("setup_advisor_optimizer", 0, messageFragments: TTSMessage("Starting headless setup search in the background.")));
                
                if (CrewChief.currentGameState?.SessionData?.TrackDefinition == null ||
                    CrewChief.currentGameState?.carClass == null)
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage("setup_advisor_optimizer_error", 0, messageFragments: TTSMessage("Session metadata is not available yet.")));
                    System.Threading.Interlocked.Exchange(ref isOptimizerRunning, 0);
                    return;
                }
                
                string carId = CrewChief.currentGameState.carClass.getClassIdentifier();
                string trackName = CrewChief.currentGameState.SessionData.TrackDefinition.name;

                System.Threading.Tasks.Task.Run(() => 
                {
                    try 
                    {
                        var optimizer = new HeadlessSetupOptimizer();
                        optimizer.Run50CarSetupSearch(carId, trackName, "");
                    } 
                    catch (Exception ex)
                    {
                        CrewChiefV4.ConsoleLogger.Log.Error($"HeadlessSetupOptimizer failed: {ex.Message}");
                    }
                    finally
                    {
                        System.Threading.Interlocked.Exchange(ref isOptimizerRunning, 0);
                    }
                });
            }
            else if (cmd == SpeechCommands.ID.SETUP_ADVISOR_HOW_AM_I_DOING)
            {
                string statusMsg = stintDiagnoses.Count == 0 ? "You are doing great, no grip issues detected." : $"You have had {stintDiagnoses.Count} grip incidents this stint."; 
                audioPlayer.playMessageImmediately(new QueuedMessage("setup_advisor_how_am_i_doing", 0, messageFragments: TTSMessage(statusMsg)));
            }
        }
    }
}
