using Xunit;
using CrewChiefV4.HeadlessSimulation;
using System.Collections.Generic;
using System.Reflection;

namespace XunitTest.HeadlessSimulation
{
    public class TelemetrySegmenterTests
    {
        [Fact]
        public void TelemetrySegmenter_TurnMappingAndStintDistance()
        {
            var segmenter = new TelemetrySegmenter();
            
            var trackTurns = new List<TurnSegment>
            {
                new TurnSegment { TurnId = 1, StartNormalized = 0.1f, EndNormalized = 0.2f },
                new TurnSegment { TurnId = 2, StartNormalized = 0.4f, EndNormalized = 0.5f },
                new TurnSegment { TurnId = 3, StartNormalized = 0.9f, EndNormalized = 0.05f } // Wrap-around turn
            };
            
            var field = typeof(TelemetrySegmenter).GetField("trackTurns", BindingFlags.NonPublic | BindingFlags.Instance);
            field.SetValue(segmenter, trackTurns);
            
            // 1. Initial position - not in turn
            var turn = segmenter.UpdatePosition(100f, 0.08f);
            Assert.Null(turn);
            Assert.Equal(100f, segmenter.GetStintDistanceMeters());
            
            // 2. Enter Turn 1
            turn = segmenter.UpdatePosition(200f, 0.15f);
            Assert.NotNull(turn);
            Assert.Equal(1, turn.TurnId);
            Assert.Equal(200f, segmenter.GetStintDistanceMeters());
            
            // 3. Enter Turn 2
            turn = segmenter.UpdatePosition(400f, 0.45f);
            Assert.NotNull(turn);
            Assert.Equal(2, turn.TurnId);
            Assert.Equal(400f, segmenter.GetStintDistanceMeters());
            
            // 4. Enter wrap-around turn before finish line
            turn = segmenter.UpdatePosition(800f, 0.95f);
            Assert.NotNull(turn);
            Assert.Equal(3, turn.TurnId);
            Assert.Equal(800f, segmenter.GetStintDistanceMeters());
            
            // 5. Cross finish line, distance resets (simulating new lap in game), but stint distance continues accumulating
            turn = segmenter.UpdatePosition(10f, 0.02f);
            Assert.NotNull(turn);
            Assert.Equal(3, turn.TurnId);
            Assert.Equal(810f, segmenter.GetStintDistanceMeters());
            
            // 6. Exit wrap around turn
            turn = segmenter.UpdatePosition(50f, 0.06f);
            Assert.Null(turn);
            Assert.Equal(850f, segmenter.GetStintDistanceMeters());
        }
    }
}
