using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text.RegularExpressions;
using CrewChiefV4.assetto.assettoData;
using System.Runtime.InteropServices;

namespace CrewChiefV4.HeadlessSimulation
{
    public static class SimulationHelpers
    {
        public static void UpdateIniKey(System.Collections.Generic.List<string> lines, string section, string key, string value)
        {
            if (lines == null) return;
            string cleanSection = section?.Trim().Trim('[', ']') ?? "";
            string targetSection = $"[{cleanSection}]";

            bool inSection = false;
            bool foundKey = false;
            for (int i = 0; i < lines.Count; i++)
            {
                string line = lines[i].Trim();
                if (line.StartsWith("["))
                {
                    if (inSection) break;
                    inSection = line.Equals(targetSection, StringComparison.OrdinalIgnoreCase);
                }
                else if (inSection && line.StartsWith(key + "=", StringComparison.OrdinalIgnoreCase))
                {
                    lines[i] = $"{key}={value}";
                    foundKey = true;
                    break;
                }
            }
            if (!foundKey)
            {
                int secIndex = lines.FindIndex(l => l.Trim().Equals(targetSection, StringComparison.OrdinalIgnoreCase));
                if (secIndex >= 0)
                {
                    lines.Insert(secIndex + 1, $"{key}={value}");
                }
                else
                {
                    lines.Add(targetSection);
                    lines.Add($"{key}={value}");
                }
            }
        }

        public static void WriteSimState(MemoryMappedViewAccessor accessor, bool isPaused, bool isCrashed, bool isSteeringOk, int controlAction, float multiplier)
        {
            if (accessor == null) throw new ArgumentNullException(nameof(accessor));
            
            try 
            {
                int offset = 0;
                accessor.Write(offset, isPaused ? 1 : 0);
                offset += 4;
                accessor.Write(offset, isCrashed ? 1 : 0);
                offset += 4;
                accessor.Write(offset, isSteeringOk ? 1 : 0);
                offset += 4;
                accessor.Write(offset, controlAction);
                offset += 4;
                accessor.Write(offset, multiplier);
            }
            catch (Exception ex)
            {
                CrewChiefV4.ConsoleLogger.Log.Verbose($"Error writing to shared memory: {ex.Message}");
            }
        }
    }
}
