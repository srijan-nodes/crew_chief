using Newtonsoft.Json;

namespace CrewChiefV4.HeadlessSimulation
{
    public class TurnSegment
    {
        [JsonProperty("turnId")]
        public int TurnId { get; set; }
        
        [JsonProperty("lapNumber")]
        public int LapNumber { get; set; }

        [JsonProperty("startNormalized")]
        public float StartNormalized { get; set; }
        
        [JsonProperty("endNormalized")]
        public float EndNormalized { get; set; }
        
        [JsonProperty("apexNormalized")]
        public float ApexNormalized { get; set; }
        
        [JsonProperty("apexSpeedKmh")]
        public float ApexSpeedKmh { get; set; }
    }
}
