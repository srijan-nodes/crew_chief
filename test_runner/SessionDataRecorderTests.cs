using Xunit;
using CrewChiefV4.HeadlessSimulation;
using System;
using System.IO;

namespace XunitTest.HeadlessSimulation
{
    public class SessionDataRecorderTests
    {
        [Fact]
        public void SessionDataRecorder_RecordsStintAndSavesJson()
        {
            var recorder = new SessionDataRecorder();
            var setup = new SetupFileParser.CarSetup
            {
                FrontARB = 5,
                RearARB = 3,
                BrakeBias = 60,
                FrontWing = 2,
                RearWing = 4
            };

            recorder.StartStint("Assetto Corsa", "ferrari_488_gt3", "monza", setup);
            Assert.True(recorder.IsRecording);

            var physics = new TelemetryData
            {
                SpeedKmh = 220f,
                Gas = 1.0f,
                Brake = 0.0f,
                SteerAngle = 0.05f,
                AccG = new float[] { 1.5f, 0.2f, 0.0f },
                WheelSlip = new float[] { 1.1f, 1.1f, 1.0f, 1.0f }
            };

            recorder.RecordTelemetrySample(physics, 1200f, 0.21f, GripLossAnalyzer.CornerPhase.MidCorner, 1);

            var diagnosis = new GripLossAnalyzer.GripDiagnosis
            {
                IsUndersteering = true,
                Summary = "Pushing mid-corner",
                TimeLostSeconds = 0.15f
            };
            recorder.RecordDiagnosis(diagnosis);

            string tempDir = Path.Combine(Path.GetTempPath(), "CC_Test_SessionRecords_" + Guid.NewGuid().ToString("N"));
            try
            {
                string savedFile = recorder.StopStintAndSave(5793f, 3, tempDir);

                Assert.NotNull(savedFile);
                Assert.True(File.Exists(savedFile));
                Assert.False(recorder.IsRecording);

                string jsonContent = File.ReadAllText(savedFile);
                Assert.Contains("ferrari_488_gt3", jsonContent);
                Assert.Contains("monza", jsonContent);
                Assert.Contains("Pushing mid-corner", jsonContent);
                Assert.Contains("\"FrontARB\": 5", jsonContent);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }
    }
}
