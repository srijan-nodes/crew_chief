using Xunit;
using CrewChiefV4.HeadlessSimulation;
using System;

namespace XunitTest.HeadlessSimulation
{
    public class GripLossAnalyzerTests
    {
        [Fact]
        public void GripLossAnalyzer_Understeer_Detected()
        {
            var analyzer = new GripLossAnalyzer();
            var physics = new TelemetryData
            {
                WheelSlip = new float[] { 3.5f, 3.5f, 1.0f, 1.0f },
                SteerAngle = 0.5f,
                AccG = new float[] { 0.5f, 0, 0 }
            };

            var diag = analyzer.AnalyzeLiveTelemetry(physics, GripLossAnalyzer.CornerPhase.MidCorner, null);
            
            Assert.True(diag.IsUndersteering);
            Assert.False(diag.IsOversteering);
            Assert.False(diag.IsTractionLoss);
            Assert.False(diag.IsLockingBrakes);
        }

        [Fact]
        public void GripLossAnalyzer_Oversteer_Detected()
        {
            var analyzer = new GripLossAnalyzer();
            var physics = new TelemetryData
            {
                WheelSlip = new float[] { 1.0f, 1.0f, 3.5f, 3.5f },
                AccG = new float[] { 0, 0, 0 }
            };
            
            var diag = analyzer.AnalyzeLiveTelemetry(physics, GripLossAnalyzer.CornerPhase.Entry, null);
            
            Assert.False(diag.IsUndersteering);
            Assert.True(diag.IsOversteering);
        }

        [Fact]
        public void GripLossAnalyzer_BrakeLock_Detected()
        {
            var analyzer = new GripLossAnalyzer();
            var physics = new TelemetryData
            {
                WheelSlip = new float[] { 4.0f, 4.0f, 1.0f, 1.0f },
                Brake = 0.8f,
                AccG = new float[] { 0, 0, 0 }
            };
            
            var diag = analyzer.AnalyzeLiveTelemetry(physics, GripLossAnalyzer.CornerPhase.Entry, null);
            
            Assert.True(diag.IsLockingBrakes);
        }
        
        [Fact]
        public void GripLossAnalyzer_Clean_Corner()
        {
            var analyzer = new GripLossAnalyzer();
            var physics = new TelemetryData
            {
                WheelSlip = new float[] { 1.0f, 1.0f, 1.0f, 1.0f },
                Brake = 0.0f,
                Gas = 0.2f,
                AccG = new float[] { 0, 0, 0 }
            };
            
            var diag = analyzer.AnalyzeLiveTelemetry(physics, GripLossAnalyzer.CornerPhase.MidCorner, null);
            
            Assert.False(diag.IsUndersteering);
            Assert.False(diag.IsOversteering);
            Assert.False(diag.IsLockingBrakes);
            Assert.False(diag.IsTractionLoss);
        }
    }
}
