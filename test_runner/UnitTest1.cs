using System;
using System.IO;
using Xunit;
using CrewChiefV4.HeadlessSimulation;

namespace test_runner
{
    public class ProductionEdgeCaseTests
    {
        [Fact]
        public void SessionDataRecorder_CapsSamplesAtMaxLimit()
        {
            var recorder = new SessionDataRecorder();
            recorder.StartStint("Assetto Corsa", "porsche_911_gt3_r", "spa", new SetupFileParser.CarSetup());
            
            var physics = new TelemetryData
            {
                SpeedKmh = 180f,
                Gas = 0.8f,
                Brake = 0.0f,
                SteerAngle = 0.02f,
                WheelSlip = new float[] { 1f, 1f, 1f, 1f }
            };

            // Record 25,005 samples in corner phase to bypass downsampling
            for (int i = 0; i < 25005; i++)
            {
                recorder.RecordTelemetrySample(physics, 500f, 0.1f, GripLossAnalyzer.CornerPhase.MidCorner, 1);
            }

            var stint = recorder.GetCurrentStint();
            Assert.NotNull(stint);
            Assert.Equal(25000, stint.TelemetrySamples.Count);
        }

        [Fact]
        public void SetupFileParser_NonExistentAndCorruptFile_HandledGracefully()
        {
            var emptyAc = SetupFileParser.ParseACSetup("non_existent_file.ini");
            Assert.NotNull(emptyAc);
            Assert.Equal(0, emptyAc.FrontARB);

            var emptyAcc = SetupFileParser.ParseACCSetup("non_existent_file.json");
            Assert.NotNull(emptyAcc);
            Assert.Equal(0, emptyAcc.FrontARB);

            string corruptJson = Path.GetTempFileName();
            File.WriteAllText(corruptJson, "{ not valid json [}");
            try
            {
                var corruptAcc = SetupFileParser.ParseACCSetup(corruptJson);
                Assert.NotNull(corruptAcc);
            }
            finally
            {
                File.Delete(corruptJson);
            }
        }

        [Fact]
        public void GripLossAnalyzer_NullAndIncompletePhysics_DoesNotThrow()
        {
            var analyzer = new GripLossAnalyzer();
            
            var nullPhysics = new TelemetryData();
            var diag1 = analyzer.AnalyzeLiveTelemetry(nullPhysics, GripLossAnalyzer.CornerPhase.MidCorner);
            Assert.False(diag1.IsUndersteering);

            var incompleteSlip = new TelemetryData { WheelSlip = new float[] { 1.0f, 2.0f } };
            var diag2 = analyzer.AnalyzeLiveTelemetry(incompleteSlip, GripLossAnalyzer.CornerPhase.MidCorner);
            Assert.False(diag2.IsUndersteering);
        }
    }
}