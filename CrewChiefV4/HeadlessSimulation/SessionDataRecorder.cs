using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace CrewChiefV4.HeadlessSimulation
{
    public class SessionDataRecorder
    {
        public class TelemetrySample
        {
            public float TimestampSeconds { get; set; }
            public int LapNumber { get; set; }
            public float NormalizedPosition { get; set; }
            public float DistanceMeters { get; set; }
            public float SpeedKmh { get; set; }
            public float Gas { get; set; }
            public float Brake { get; set; }
            public float SteerAngle { get; set; }
            public float LateralG { get; set; }
            public float[] WheelSlip { get; set; }
            public GripLossAnalyzer.CornerPhase Phase { get; set; }
        }

        public class StintRecord
        {
            public string Game { get; set; }
            public string CarClass { get; set; }
            public string TrackName { get; set; }
            public DateTime StartTimeUtc { get; set; }
            public DateTime EndTimeUtc { get; set; }
            public SetupFileParser.CarSetup InitialSetup { get; set; }
            public List<TelemetrySample> TelemetrySamples { get; set; } = new List<TelemetrySample>();
            public List<GripLossAnalyzer.GripDiagnosis> Diagnoses { get; set; } = new List<GripLossAnalyzer.GripDiagnosis>();
            public float TotalDistanceMeters { get; set; }
            public int TotalLapsCompleted { get; set; }
        }

        private StintRecord currentStint;
        private DateTime stintStartTime;
        private readonly object recordLock = new object();
        private int sampleCounter = 0;
        private const int DownsampleRate = 5; // Record every 5th tick to prevent excessive memory/storage

        public bool IsRecording => currentStint != null;

        public void StartStint(string game, string carClass, string trackName, SetupFileParser.CarSetup initialSetup)
        {
            lock (recordLock)
            {
                stintStartTime = DateTime.UtcNow;
                currentStint = new StintRecord
                {
                    Game = game ?? "UnknownGame",
                    CarClass = carClass ?? "UnknownCar",
                    TrackName = trackName ?? "UnknownTrack",
                    StartTimeUtc = stintStartTime,
                    InitialSetup = initialSetup,
                    TelemetrySamples = new List<TelemetrySample>(),
                    Diagnoses = new List<GripLossAnalyzer.GripDiagnosis>()
                };
                sampleCounter = 0;
                ConsoleLogger.Log.Verbose($"[SessionDataRecorder] Started session recording for {carClass} at {trackName} ({game})");
            }
        }

        public void RecordTelemetrySample(TelemetryData physics, float distanceRoundTrack, float normalizedPosition, GripLossAnalyzer.CornerPhase phase, int lapNumber)
        {
            if (currentStint == null || physics == null) return;

            // Only capture every N ticks unless we are in an active corner phase with slip
            sampleCounter++;
            if (sampleCounter % DownsampleRate != 0 && phase == GripLossAnalyzer.CornerPhase.Straight)
            {
                return;
            }

            lock (recordLock)
            {
                if (currentStint == null) return;

                var sample = new TelemetrySample
                {
                    TimestampSeconds = (float)(DateTime.UtcNow - stintStartTime).TotalSeconds,
                    LapNumber = lapNumber,
                    NormalizedPosition = normalizedPosition,
                    DistanceMeters = distanceRoundTrack,
                    SpeedKmh = physics.SpeedKmh,
                    Gas = physics.Gas,
                    Brake = physics.Brake,
                    SteerAngle = physics.SteerAngle,
                    LateralG = (physics.AccG != null && physics.AccG.Length > 0) ? physics.AccG[0] : 0f,
                    WheelSlip = physics.WheelSlip != null ? (float[])physics.WheelSlip.Clone() : null,
                    Phase = phase
                };

                const int MaxSamples = 25000;
                if (currentStint.TelemetrySamples.Count < MaxSamples)
                {
                    currentStint.TelemetrySamples.Add(sample);
                }
            }
        }

        public void RecordDiagnosis(GripLossAnalyzer.GripDiagnosis diagnosis)
        {
            if (currentStint == null || diagnosis == null) return;

            lock (recordLock)
            {
                if (currentStint != null)
                {
                    currentStint.Diagnoses.Add(diagnosis);
                }
            }
        }

        public string StopStintAndSave(float stintDistanceMeters, int lapsCompleted, string customOutputDir = null)
        {
            lock (recordLock)
            {
                if (currentStint == null) return null;

                currentStint.EndTimeUtc = DateTime.UtcNow;
                currentStint.TotalDistanceMeters = stintDistanceMeters;
                currentStint.TotalLapsCompleted = lapsCompleted;

                try
                {
                    string outputDir = customOutputDir;
                    if (string.IsNullOrEmpty(outputDir))
                    {
                        outputDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CrewChiefV4", "SessionDataRecords");
                    }

                    if (!Directory.Exists(outputDir))
                    {
                        Directory.CreateDirectory(outputDir);
                    }

                    string safeCar = SanitizeFileName(currentStint.CarClass);
                    string safeTrack = SanitizeFileName(currentStint.TrackName);
                    string fileName = $"stint_{safeCar}_{safeTrack}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";
                    string fullPath = Path.Combine(outputDir, fileName);

                    string json = JsonConvert.SerializeObject(currentStint, Formatting.Indented);
                    File.WriteAllText(fullPath, json);

                    ConsoleLogger.Log.Verbose($"[SessionDataRecorder] Successfully saved stint data ({currentStint.TelemetrySamples.Count} samples, {currentStint.Diagnoses.Count} diagnoses) to {fullPath}");
                    return fullPath;
                }
                catch (Exception ex)
                {
                    ConsoleLogger.Log.Error($"[SessionDataRecorder] Failed to save stint data: {ex.Message}");
                    return null;
                }
                finally
                {
                    currentStint = null;
                }
            }
        }

        public StintRecord GetCurrentStint()
        {
            lock (recordLock)
            {
                if (currentStint == null) return null;
                return new StintRecord
                {
                    Game = currentStint.Game,
                    CarClass = currentStint.CarClass,
                    TrackName = currentStint.TrackName,
                    StartTimeUtc = currentStint.StartTimeUtc,
                    EndTimeUtc = currentStint.EndTimeUtc,
                    InitialSetup = currentStint.InitialSetup,
                    TotalDistanceMeters = currentStint.TotalDistanceMeters,
                    TotalLapsCompleted = currentStint.TotalLapsCompleted,
                    TelemetrySamples = new List<TelemetrySample>(currentStint.TelemetrySamples),
                    Diagnoses = new List<GripLossAnalyzer.GripDiagnosis>(currentStint.Diagnoses)
                };
            }
        }

        private static string SanitizeFileName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "unknown";
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }
            return name;
        }
    }
}
