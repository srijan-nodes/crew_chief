using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CrewChiefV4.HeadlessSimulation
{
    public class SetupFileParser
    {
        public class CarSetup
        {
            public float[] TyrePressures { get; set; } = new float[4];
            public float[] Camber { get; set; } = new float[4];
            public float[] ToeOut { get; set; } = new float[4];
            public int BrakeBias { get; set; }
            public int FrontARB { get; set; }
            public int RearARB { get; set; }
            public int FrontWing { get; set; }
            public int RearWing { get; set; }
        }

        public static CarSetup ParseACSetup(string iniPath)
        {
            var setup = new CarSetup();
            if (string.IsNullOrEmpty(iniPath) || !File.Exists(iniPath)) return setup;

            string[] lines; 
            try 
            { 
                using (var fs = new FileStream(iniPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var sr = new StreamReader(fs))
                {
                    string content = sr.ReadToEnd();
                    lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                }
            } 
            catch (Exception ex) 
            { 
                CrewChiefV4.ConsoleLogger.Log.Verbose("ParseACSetup error: " + ex.Message); 
                return setup; 
            }

            string currentSection = "";
            foreach (var line in lines)
            {
                var trimLine = line.Trim();
                if (trimLine.StartsWith("[") && trimLine.EndsWith("]"))
                {
                    currentSection = trimLine.ToUpperInvariant();
                    continue;
                }

                if (trimLine.Contains("="))
                {
                    var parts = trimLine.Split('=');
                    if (parts.Length == 2)
                    {
                        var key = parts[0].Trim();
                        var value = parts[1].Trim();
                        
                        if (int.TryParse(value, out int intVal))
                        {
                            if (currentSection == "[ARB]")
                            {
                                if (key == "FRONT_ARB") setup.FrontARB = intVal;
                                if (key == "REAR_ARB") setup.RearARB = intVal;
                            }
                            if (currentSection == "[AERO]")
                            {
                                if (key == "WING_1") setup.FrontWing = intVal;
                                if (key == "WING_2") setup.RearWing = intVal;
                            }
                            
                            if (key == "BIAS") setup.BrakeBias = intVal;
                        }
                        if (float.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float floatVal))
                        {
                            if (key == "PRESSURE_LF") setup.TyrePressures[0] = floatVal;
                            if (key == "PRESSURE_RF") setup.TyrePressures[1] = floatVal;
                            if (key == "PRESSURE_LR") setup.TyrePressures[2] = floatVal;
                            if (key == "PRESSURE_RR") setup.TyrePressures[3] = floatVal;

                            if (key == "CAMBER_LF") setup.Camber[0] = floatVal;
                            if (key == "CAMBER_RF") setup.Camber[1] = floatVal;
                            if (key == "CAMBER_LR") setup.Camber[2] = floatVal;
                            if (key == "CAMBER_RR") setup.Camber[3] = floatVal;

                            if (key == "TOE_OUT_LF") setup.ToeOut[0] = floatVal;
                            if (key == "TOE_OUT_RF") setup.ToeOut[1] = floatVal;
                            if (key == "TOE_OUT_LR") setup.ToeOut[2] = floatVal;
                            if (key == "TOE_OUT_RR") setup.ToeOut[3] = floatVal;
                        }
                    }
                }
            }

            return setup;
        }

        public static CarSetup ParseACCSetup(string jsonPath)
        {
            var setup = new CarSetup();
            if (string.IsNullOrEmpty(jsonPath) || !File.Exists(jsonPath)) return setup;

            try
            {
                using (var fs = new FileStream(jsonPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var sr = new StreamReader(fs))
                using (var reader = new JsonTextReader(sr))
                {
                    var data = JObject.Load(reader);

                    var basic = data?["basicSetup"];
                    if (basic != null && basic.Type != JTokenType.Null)
                    {
                        var alignment = basic["alignment"];
                        if (alignment != null && alignment.Type != JTokenType.Null)
                        {
                            if (alignment["camber"] is JArray camberArray)
                            {
                                for (int i = 0; i < 4 && i < camberArray.Count; i++)
                                    setup.Camber[i] = camberArray[i]?.Value<float>() ?? 0f;
                            }
                            if (alignment["toe"] is JArray toeArray)
                            {
                                for (int i = 0; i < 4 && i < toeArray.Count; i++)
                                    setup.ToeOut[i] = toeArray[i]?.Value<float>() ?? 0f;
                            }
                        }

                        var tyres = basic["tyres"];
                        if (tyres != null && tyres.Type != JTokenType.Null && tyres["tyrePressure"] is JArray pressureArray)
                        {
                            for (int i = 0; i < 4 && i < pressureArray.Count; i++)
                                setup.TyrePressures[i] = pressureArray[i]?.Value<float>() ?? 0f;
                        }
                    }

                    var advanced = data?["advancedSetup"];
                    if (advanced != null && advanced.Type != JTokenType.Null)
                    {
                        var mech = advanced["mechanicalBalance"] ?? advanced["mechanical"];
                        if (mech != null && mech.Type != JTokenType.Null)
                        {
                            setup.FrontARB = (mech["aRBFront"] ?? mech["aRFront"])?.Value<int>() ?? 0;
                            setup.RearARB = (mech["aRBRear"] ?? mech["aRRear"])?.Value<int>() ?? 0;
                            setup.BrakeBias = (mech["brakeBias"] ?? mech["bBias"])?.Value<int>() ?? 0;
                        }

                        var aero = advanced["aeroBalance"] ?? advanced["aero"];
                        if (aero != null && aero.Type != JTokenType.Null)
                        {
                            setup.FrontWing = (aero["frontSplitter"] ?? aero["wing_1"])?.Value<int>() ?? 0;
                            setup.RearWing = (aero["rearWing"] ?? aero["wing_2"])?.Value<int>() ?? 0;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CrewChiefV4.ConsoleLogger.Log.Verbose("ParseACCSetup error: " + ex.Message);
            }

            return setup;
        }
    }
}
