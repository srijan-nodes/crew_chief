using System;
using CrewChiefV4.assetto.assettoData;

namespace CrewChiefV4.HeadlessSimulation
{
    public class TelemetryData
    {
        public float[] WheelSlip { get; set; }
        public float[] AccG { get; set; }
        public float SteerAngle { get; set; }
        public float Gas { get; set; }
        public float Brake { get; set; }
        public float SpeedKmh { get; set; }
    }

    public class GripLossAnalyzer
    {
        public enum CornerPhase
        {
            Straight,
            Entry,
            MidCorner,
            Exit
        }

        private const float UndersteerSlipDelta = 0.5f;
        private const float OversteerSlipDelta = 0.8f;
        private const float ExtremeSlipThreshold = 3.0f;
        private const float HeavyPedalThreshold = 0.5f;
        private const float SteerThreshold = 0.1f;
        private const float LateralGThreshold = 0.3f;
        private const float TimeLostPerKmhMissed = 0.02f;

        public class GripDiagnosis
        {
            public bool IsUndersteering { get; set; }
            public bool IsOversteering { get; set; }
            public bool IsTractionLoss { get; set; }
            public bool IsLockingBrakes { get; set; }
            public string Summary { get; set; }
            public float TimeLostSeconds { get; set; }
        }

        public GripDiagnosis AnalyzeLiveTelemetry(TelemetryData physics, CornerPhase phase, TurnSegment activeTurn = null)
        {
            var diagnosis = new GripDiagnosis();
            
            if (physics.WheelSlip == null || physics.WheelSlip.Length < 4) return diagnosis;
            
            float frontSlip = (Math.Abs(physics.WheelSlip[0]) + Math.Abs(physics.WheelSlip[1])) / 2.0f;
            float rearSlip = (Math.Abs(physics.WheelSlip[2]) + Math.Abs(physics.WheelSlip[3])) / 2.0f;
            float lateralG = (physics.AccG != null && physics.AccG.Length > 0) ? Math.Abs(physics.AccG[0]) : 0f;

            if (phase == CornerPhase.MidCorner && frontSlip > (rearSlip + UndersteerSlipDelta) && Math.Abs(physics.SteerAngle) > SteerThreshold && lateralG > LateralGThreshold)
            {
                diagnosis.IsUndersteering = true;
                diagnosis.Summary += (string.IsNullOrEmpty(diagnosis.Summary) ? "" : " ") + $"Pushing mid-corner. Front ND slip: {frontSlip:F1}.";
            }

            if ((phase == CornerPhase.Entry || phase == CornerPhase.MidCorner) && rearSlip > (frontSlip + OversteerSlipDelta))
            {
                diagnosis.IsOversteering = true;
                diagnosis.Summary += (string.IsNullOrEmpty(diagnosis.Summary) ? "" : " ") + $"Snap oversteer detected. Rear ND slip: {rearSlip:F1}.";
            }

            if (phase == CornerPhase.Exit && rearSlip > ExtremeSlipThreshold && physics.Gas > HeavyPedalThreshold)
            {
                diagnosis.IsTractionLoss = true;
                diagnosis.IsOversteering = true; 
                diagnosis.Summary += (string.IsNullOrEmpty(diagnosis.Summary) ? "" : " ") + $"Wheelspin on exit. Rear ND slip: {rearSlip:F1}.";
            }

            if (phase == CornerPhase.Entry && frontSlip > ExtremeSlipThreshold && physics.Brake > HeavyPedalThreshold)
            {
                diagnosis.IsLockingBrakes = true;
                diagnosis.Summary += (string.IsNullOrEmpty(diagnosis.Summary) ? "" : " ") + "Front locking under heavy braking.";
            }

            // Ideal line delta computation
            if (activeTurn != null && phase == CornerPhase.MidCorner)
            {
                float speedDelta = activeTurn.ApexSpeedKmh - physics.SpeedKmh;
                if (speedDelta > 5.0f)
                {
                    // Basic estimation: lost 0.1s for every 5km/h under target apex speed
                    diagnosis.TimeLostSeconds = speedDelta * TimeLostPerKmhMissed;
                    diagnosis.Summary += (string.IsNullOrEmpty(diagnosis.Summary) ? "" : " ") + 
                        $"Missed apex speed by {speedDelta:F1}km/h (lost ~{diagnosis.TimeLostSeconds:F2}s).";
                }
            }

            return diagnosis;
        }
    }
}


