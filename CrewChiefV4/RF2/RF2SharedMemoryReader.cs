//#define TRACE_BUFFER_READ_ELAPSED_TIME

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Serialization;

using CrewChiefV4.rFactor2;
using rF2SharedMemory;
using rF2SharedMemory.rFactor2Data;

namespace CrewChiefV4.rFactor2
{
    public class RF2SharedMemoryReader : GameDataReader
    {
        // Note: to check that our struct sizes are correct.
        public RF2SharedMemoryReader()
        {
            Debug.Assert(Marshal.SizeOf<rF2Telemetry>() == rFactor2Constants.MM_TELEMETRY_STRUCT_SIZE, $"Size of rF2Telemetry struct is {Marshal.SizeOf<rF2Telemetry>()}, should be {rFactor2Constants.MM_TELEMETRY_STRUCT_SIZE} bytes");
            Debug.Assert(Marshal.SizeOf<rF2Scoring>() == rFactor2Constants.MM_SCORING_STRUCT_SIZE, $"Size of rF2Scoring struct is {Marshal.SizeOf<rF2Scoring>()}, should be {rFactor2Constants.MM_SCORING_STRUCT_SIZE} bytes");
            Debug.Assert(Marshal.SizeOf<rF2Rules>() == rFactor2Constants.MM_RULES_STRUCT_SIZE, $"Size of rF2Rules struct is {Marshal.SizeOf<rF2Rules>()}, should be {rFactor2Constants.MM_RULES_STRUCT_SIZE} bytes");
            Debug.Assert(Marshal.SizeOf<rF2Extended>() == rFactor2Constants.MM_EXTENDED_STRUCT_SIZE, $"Size of rF2Extended struct is {Marshal.SizeOf<rF2Extended>()}, should be {rFactor2Constants.MM_EXTENDED_STRUCT_SIZE} bytes");
        }

        MappedBuffer<rF2Telemetry> telemetryBuffer = new MappedBuffer<rF2Telemetry>(rFactor2Constants.MM_TELEMETRY_FILE_NAME, true /*partial*/, true /*skipUnchanged*/);
        MappedBuffer<rF2Scoring> scoringBuffer = new MappedBuffer<rF2Scoring>(rFactor2Constants.MM_SCORING_FILE_NAME, true /*partial*/, true /*skipUnchanged*/);
        MappedBuffer<rF2Rules> rulesBuffer = new MappedBuffer<rF2Rules>(rFactor2Constants.MM_RULES_FILE_NAME, true /*partial*/, true /*skipUnchanged*/);
        MappedBuffer<rF2Extended> extendedBuffer = new MappedBuffer<rF2Extended>(rFactor2Constants.MM_EXTENDED_FILE_NAME, false /*partial*/, true /*skipUnchanged*/);

        private bool initialised = false;
        private List<RF2StructWrapper> dataToDump;
        private RF2StructWrapper[] dataReadFromFile = null;
        private int dataReadFromFileIndex = 0;
        private string lastReadFileName = null;

        // Capture mCurrentET from scoring to prevent dumping double frames to the file.
        private double lastScoringET = -1.0;
        // Capture mElapsedTime from telemetry of the first vehicle to prevent dumping double frames to the file.
        private double lastTelemetryET = -1.0;

        public class RF2StructWrapper
        {
            public long ticksWhenRead;
            public rF2Telemetry telemetry;
            public rF2Scoring scoring;
            public rF2Rules rules;
            public rF2Extended extended;
        }

        public override void DumpRawGameData()
        {
            if (this.dumpToFile && this.dataToDump != null && this.dataToDump.Count > 0 && this.filenameToDump != null)
            {
                this.SerializeObject(this.dataToDump.ToArray<RF2StructWrapper>(), this.filenameToDump);

            }
        }

        public override void ResetGameDataFromFile()
        {
            this.dataReadFromFileIndex = 0;
        }

        public override TracedGameData ReadGameDataFromFile(String filename, int pauseBeforeStart)
        {
            if (this.dataReadFromFile == null || filename != this.lastReadFileName)
            {
                this.dataReadFromFileIndex = 0;

                var filePathResolved = Utilities.ResolveDataFile(this.dataFilesPath, filename);
                dataReadFromFile = DeSerializeObject<RF2StructWrapper[]>(filePathResolved);

                this.lastReadFileName = filename;
                Thread.Sleep(pauseBeforeStart);
            }
            if (dataReadFromFile != null && dataReadFromFile.Length > this.dataReadFromFileIndex)
            {
                RF2StructWrapper structWrapperData = dataReadFromFile[this.dataReadFromFileIndex];
                this.dataReadFromFileIndex++;
                return new TracedGameData
                {
                    GameData = structWrapperData, 
                    TicksWhenRead = structWrapperData.ticksWhenRead
                };
            }
            else
            {
                return null;
            }
        }

        protected override Boolean InitialiseInternal()
        {
            this.lastScoringET = -1.0;

            // This needs to be synchronized, because disconnection happens from CrewChief.Run and MainWindow.Dispose.
            lock (this)
            {
                if (!this.initialised)
                {
                    try
                    {
                        this.telemetryBuffer.Connect();
                        this.scoringBuffer.Connect();
                        this.rulesBuffer.Connect();
                        this.extendedBuffer.Connect();

                        // Clear mapped views.
                        this.telemetry = new rF2Telemetry();
                        this.scoring = new rF2Scoring();
                        this.extended = new rF2Extended();
                        this.rules = new rF2Rules();

                        if (dumpToFile)
                            this.dataToDump = new List<RF2StructWrapper>();

                        this.initialised = true;

                        Console.WriteLine($"Initialized {CrewChief.gameDefinition.friendlyName} Shared Memory");
                    }
                    catch (Exception)
                    {
                        Log.DontSpam($"Failed to initialize {CrewChief.gameDefinition.friendlyName} Shared Memory").Warning();
                        this.initialised = false;
                        this.DisconnectInternal();
                    }
                }
                return this.initialised;
            }
        }

        // Marshalled views:
        private rF2Telemetry telemetry;
        private rF2Scoring scoring;
        private rF2Rules rules;
        private rF2Extended extended;

        public override Object ReadGameData(Boolean forSpotter)
        {
            lock (this)
            {
                if (!this.initialised)
                {
                    if (!this.InitialiseInternal())
                    {
                        throw new GameDataReadException("Failed to initialise shared memory");
                    }
                }
                try 
                {
#if TRACE_BUFFER_READ_ELAPSED_TIME
                    var watch = System.Diagnostics.Stopwatch.StartNew();
#endif
                    extendedBuffer.GetMappedData(ref this.extended);
                    telemetryBuffer.GetMappedData(ref this.telemetry);
                    rulesBuffer.GetMappedData(ref this.rules);

                    // Scoring is the most important game data in Crew Chief sense, 
                    // so acquire it last, hoping it will be most recent view of all buffer types.
                    scoringBuffer.GetMappedData(ref this.scoring);

                    // Create a new copy marshalled views.  Thia is necessary because core code caches states, so each
                    // state has to be an individual object.  We can't avoid copy by marshalling directly into wrapper,
                    // because not all marshalling calls fetch new buffer.
                    var wrapper = new RF2StructWrapper()
                    {
                        extended = this.extended,
                        telemetry = this.telemetry,
                        rules = this.rules,  // TODO_RF2:  we probably don't need rules buffer if reading for spotter.
                        scoring = this.scoring,
                        ticksWhenRead = DateTime.UtcNow.Ticks
                    };

                    if (!forSpotter && currentlyTracingGameData && this.dataToDump != null)
                    {
                        // Note: this is lossy save, because we only save update if Telemtry or Scoring changed.
                        // Other buffers don't change that much, so it should be fine.

                        // Exclude empty frames.
                        if (wrapper.scoring.mScoringInfo.mNumVehicles > 0
                            && wrapper.extended.mSessionStarted == 1)
                        {
                            var hasTelemetryChanged = false;
                            if (wrapper.telemetry.mNumVehicles > 0)
                            {
                                var currTelET = wrapper.telemetry.mVehicles[0].mElapsedTime;
                                hasTelemetryChanged = currTelET != this.lastTelemetryET;
                                this.lastTelemetryET = currTelET;
                            }

                            var currScoringET = wrapper.scoring.mScoringInfo.mCurrentET;
                            if (currScoringET != this.lastScoringET  // scoring contains new payload
                                || hasTelemetryChanged)  // Or, telemetry updated.
                            {
                                // NOTE: truncation code could be moved to DumpRawGameData method for reduced CPU use.
                                // However, this causes memory pressure (~250Mb/minute with 22 vehicles), so probably better done here.
                                wrapper.telemetry.mVehicles = this.GetPopulatedVehicleInfoArray<rF2VehicleTelemetry>(wrapper.telemetry.mVehicles, wrapper.telemetry.mNumVehicles);
                                wrapper.scoring.mVehicles = this.GetPopulatedVehicleInfoArray<rF2VehicleScoring>(wrapper.scoring.mVehicles, wrapper.scoring.mScoringInfo.mNumVehicles);

                                // For rules, exclude empty messages from serialization.
                                wrapper.rules.mTrackRules.mMessage = wrapper.rules.mTrackRules.mMessage[0] != 0 ? wrapper.rules.mTrackRules.mMessage : null;
                                wrapper.rules.mParticipants = this.GetPopulatedVehicleInfoArray<rF2TrackRulesParticipant>(wrapper.rules.mParticipants, wrapper.rules.mTrackRules.mNumParticipants);
                                for (int i = 0; i < wrapper.rules.mParticipants.Length; ++i)
                                    wrapper.rules.mParticipants[i].mMessage = wrapper.rules.mParticipants[i].mMessage[0] != 0 ? wrapper.rules.mParticipants[i].mMessage : null;

                                int maxmID = 0;
                                foreach (var vehicleScoring in wrapper.scoring.mVehicles)
                                    maxmID = Math.Max(maxmID, vehicleScoring.mID);

                                if (maxmID < rFactor2Constants.MAX_MAPPED_IDS)
                                {
                                    // Since serialization to XML produces a lot of useless tags even for small arrays, truncate tracked damage array.
                                    // It is indexed by mID.  Max mID in current set is equal to mNumVehicles in 99% of cases, so just truncate to this size.
                                    wrapper.extended.mTrackedDamages = this.GetPopulatedVehicleInfoArray<rF2TrackedDamage>(wrapper.extended.mTrackedDamages, maxmID + 1);
                                }

                                this.dataToDump.Add(wrapper);
                                this.lastScoringET = currScoringET;
                            }
                        }
                    }

#if TRACE_BUFFER_READ_ELAPSED_TIME
                    watch.Stop();
                    var microseconds = watch.ElapsedTicks * 1000000 / System.Diagnostics.Stopwatch.Frequency;
                    System.Console.WriteLine("Buffer read microseconds: " + microseconds);
#endif
                    return wrapper;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(CrewChief.gameDefinition.friendlyName + " Shared Memory connection failed.");
                    this.DisconnectInternal();
                    throw new GameDataReadException(ex.Message, ex);
                }
            }
        }

        private VehicleInfoT[] GetPopulatedVehicleInfoArray<VehicleInfoT>(VehicleInfoT[] vehicles, int numPopulated)
        {
            // To reduce serialized size, only return non-empty vehicles.
            var populated = new List<VehicleInfoT>();
            for (int i = 0; i < numPopulated; ++i)
                populated.Add(vehicles[i]);

            return populated.ToArray();
        }

        public override void DisconnectFromProcess()
        {
            var wasInitialised = this.initialised;
            if (wasInitialised)
            {
                Console.WriteLine("Telemetry: " + this.telemetryBuffer.GetStats());
                Console.WriteLine("Scoring: " + this.scoringBuffer.GetStats());
                Console.WriteLine("Rules: " + this.rulesBuffer.GetStats());
                Console.WriteLine("Extended: " + this.extendedBuffer.GetStats());
            }

            this.DisconnectInternal();

            // There's still possibility of double message, but who cares.
            if (wasInitialised)
                Console.WriteLine($"Disconnected from {CrewChief.gameDefinition.friendlyName} Shared Memory");

            // Hack to re-check plugin version.
            RF2GameStateMapper.pluginVerified = false;
        }

        private void DisconnectInternal()
        {
            this.initialised = false;

            this.telemetryBuffer.Disconnect();
            this.scoringBuffer.Disconnect();
            this.rulesBuffer.Disconnect();
            this.extendedBuffer.Disconnect();
        }

        public override void Dispose()
        {
            try
            {
                this.DisconnectInternal();
            }
            catch (Exception e) {Log.Exception(e);}
        }
    }
}
