using Xunit;
using CrewChiefV4.HeadlessSimulation;
using System.Collections.Generic;

namespace XunitTest.HeadlessSimulation
{
    public class SimulationHelpersTests
    {
        [Fact]
        public void SimulationHelpers_UpdateIniKey_UpdatesExisting()
        {
            var lines = new List<string> {
                "[HEADER]",
                "Key1=Value1",
                "[SECTION]",
                "Key2=OldValue",
                "[FOOTER]"
            };

            SimulationHelpers.UpdateIniKey(lines, "SECTION", "Key2", "NewValue");

            Assert.Equal("Key2=NewValue", lines[3]);
        }

        [Fact]
        public void SimulationHelpers_UpdateIniKey_AddsNewKeyToExistingSection()
        {
            var lines = new List<string> {
                "[SECTION]",
                "Key1=Value1"
            };

            SimulationHelpers.UpdateIniKey(lines, "SECTION", "Key2", "NewValue");

            Assert.Equal(3, lines.Count);
            Assert.Equal("Key2=NewValue", lines[1]);
        }
        
        [Fact]
        public void SimulationHelpers_UpdateIniKey_AddsNewSection()
        {
            var lines = new List<string>();

            SimulationHelpers.UpdateIniKey(lines, "NEWSECTION", "Key1", "Value1");

            Assert.Equal(2, lines.Count);
            Assert.Equal("[NEWSECTION]", lines[0]);
            Assert.Equal("Key1=Value1", lines[1]);
        }
    }
}
