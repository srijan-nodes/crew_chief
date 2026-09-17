using Xunit;
using CrewChiefV4.HeadlessSimulation;
using System.IO;

namespace XunitTest.HeadlessSimulation
{
    public class SetupFileParserTests
    {
        [Fact]
        public void SetupFileParser_ParseACSetup()
        {
            string tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, "[ARB]\nFRONT_ARB=5\nREAR_ARB=3\n[AERO]\nWING_1=2\nWING_2=4");
            
            var setup = SetupFileParser.ParseACSetup(tempFile);
            
            Assert.Equal(5, setup.FrontARB);
            Assert.Equal(3, setup.RearARB);
            Assert.Equal(2, setup.FrontWing);
            Assert.Equal(4, setup.RearWing);
            
            File.Delete(tempFile);
        }

        [Fact]
        public void SetupFileParser_ParseACCSetup()
        {
            string tempFile = Path.GetTempFileName();
            string json = "{ \"basicSetup\": { \"alignment\": { \"camber\": [-3.0, -3.0, -2.0, -2.0] } }, \"advancedSetup\": { \"mechanical\": { \"aRFront\": 4, \"aRRear\": 6 } } }";
            File.WriteAllText(tempFile, json);
            
            var setup = SetupFileParser.ParseACCSetup(tempFile);
            
            Assert.Equal(-3.0f, setup.Camber[0]);
            Assert.Equal(4, setup.FrontARB);
            Assert.Equal(6, setup.RearARB);
            
            File.Delete(tempFile);
        }
    }
}
