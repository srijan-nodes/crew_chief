using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.ComponentModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CrewChiefV4.HeadlessSimulation
{
    public class HeadlessSetupOptimizer
    {
        private string acDocumentsPath;
        private string acInstallPath;

        public HeadlessSetupOptimizer(string customAcInstallPath = null)
        {
            acDocumentsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Assetto Corsa");
            acInstallPath = customAcInstallPath ?? @"E:\SteamLibrary\steamapps\common\assettocorsa"; 
        }

        public bool RunSetupSearch(string carName, string trackName, string trackConfig = "", int numCandidates = 24)
        {
            CrewChiefV4.ConsoleLogger.Log.Verbose($"Starting {numCandidates}-car Headless Setup Search for {carName} at {trackName}...");

            string cfgPath = Path.Combine(acDocumentsPath, "cfg");
            string raceIniPath = Path.Combine(cfgPath, "race.ini");

            if (!Directory.Exists(cfgPath))
            {
                CrewChiefV4.ConsoleLogger.Log.Verbose("Could not find AC cfg directory at " + cfgPath);
                return false;
            }

            string raceIniBackup = raceIniPath + ".backup_" + Guid.NewGuid().ToString("N");
            if (File.Exists(raceIniPath)) File.Copy(raceIniPath, raceIniBackup, true);

            try
            {
                GenerateCandidateSetups(carName, trackName, numCandidates);
                int raceLaps = 1;
                ConfigureRaceIni(raceIniPath, carName, trackName, trackConfig, numCandidates, raceLaps);
                LaunchSimulationAndWait(numCandidates, raceLaps);

                int bestCarIndex = EvaluateRaceResults(numCandidates);
                if (bestCarIndex >= 0)
                {
                    SaveBaseRecSetup(carName, trackName, bestCarIndex);
                    
                    try
                    {
                        var lineFinder = new LineFinderOrchestrator(acInstallPath);
                        lineFinder.RunIdealLineSimulation(carName, trackName, trackConfig);
                    }
                    catch (Exception ex)
                    {
                        CrewChiefV4.ConsoleLogger.Log.Error($"LineFinderOrchestrator failed: {ex.Message}");
                    }
                    
                    return true;
                }
                return false;
            }
            finally
            {
                if (File.Exists(raceIniBackup))
                {
                    File.Copy(raceIniBackup, raceIniPath, true);
                    File.Delete(raceIniBackup);
                }
                CrewChiefV4.ConsoleLogger.Log.Verbose("Restored original race.ini");
            }
        }

        public bool Run50CarSetupSearch(string carName, string trackName, string trackConfig = "")
        {
            return RunSetupSearch(carName, trackName, trackConfig, 50);
        }

        private void GenerateCandidateSetups(string carName, string trackName, int numCandidates)
        {
            string setupsDir = Path.Combine(acDocumentsPath, "setups", carName, trackName);
            Directory.CreateDirectory(setupsDir);

            for (int i = 0; i < numCandidates; i++)
            {
                var rnd = new Random(i * 17 + 31);
                int pLf = 22 + rnd.Next(8); // 22 - 29 PSI
                int pRf = 22 + rnd.Next(8);
                int pLr = 22 + rnd.Next(8);
                int pRr = 22 + rnd.Next(8);
                int cFront = -35 + rnd.Next(16); // -3.5° to -2.0° (stored * 10)
                int cRear = -25 + rnd.Next(11);  // -2.5° to -1.5°
                int toeF = 10 + rnd.Next(15);
                int toeR = 15 + rnd.Next(10);

                string setupPath = Path.Combine(setupsDir, $"candidate_{i}.ini");
                string content = $@"[ABS]
VALUE=1

[BRAKE_POWER_MULT]
VALUE=100

[CAMBER_LF]
VALUE={cFront}

[CAMBER_LR]
VALUE={cRear}

[CAMBER_RF]
VALUE={cFront}

[CAMBER_RR]
VALUE={cRear}

[CAR]
MODEL={carName}

[FUEL]
VALUE=30

[PRESSURE_LF]
VALUE={pLf}

[PRESSURE_LR]
VALUE={pLr}

[PRESSURE_RF]
VALUE={pRf}

[PRESSURE_RR]
VALUE={pRr}

[TOE_OUT_LF]
VALUE={toeF}

[TOE_OUT_LR]
VALUE={toeR}

[TOE_OUT_RF]
VALUE={toeF}

[TOE_OUT_RR]
VALUE={toeR}

[TRACTION_CONTROL]
VALUE=1

[TYRES]
VALUE=0


";
                File.WriteAllText(setupPath, content);
            }
            CrewChiefV4.ConsoleLogger.Log.Verbose($"Generated {numCandidates} compliant candidate setups for {carName} at {trackName}");
        }

        private void ConfigureRaceIni(string raceIniPath, string carName, string trackName, string trackConfig, int numCars, int laps)
        {
            var lines = File.Exists(raceIniPath) ? File.ReadAllLines(raceIniPath).ToList() : new List<string>();
            
            // Fix 4: Safely wipe old CAR configurations without orphaning inner keys
            var cleanedLines = new List<string>();
            bool skipSection = false;
            foreach (var line in lines)
            {
                string t = line.Trim();
                if (t.StartsWith("["))
                {
                    skipSection = Regex.IsMatch(t, @"^\[CAR_\d+\]$", RegexOptions.IgnoreCase);
                }
                if (!skipSection)
                {
                    cleanedLines.Add(line);
                }
            }
            lines = cleanedLines;

            UpdateIniKey(lines, "[RACE]", "TRACK", trackName);
            UpdateIniKey(lines, "[RACE]", "CONFIG_TRACK", trackConfig);
            UpdateIniKey(lines, "[RACE]", "CARS", numCars.ToString());
            UpdateIniKey(lines, "[RACE]", "RACE_LAPS", laps.ToString());
            UpdateIniKey(lines, "[RACE]", "MODEL", carName);
            UpdateIniKey(lines, "[RACE]", "AI_LEVEL", "100");

            UpdateIniKey(lines, "[SESSION_0]", "TYPE", "3");
            UpdateIniKey(lines, "[SESSION_0]", "LAPS", laps.ToString());
            UpdateIniKey(lines, "[SESSION_0]", "SPAWN_SET", "START");

            for (int i = 0; i < numCars; i++)
            {
                lines.Add("");
                lines.Add($"[CAR_{i}]");
                lines.Add($"MODEL={carName}");
                lines.Add($"SETUP=candidate_{i}.ini");
                lines.Add("AI_LEVEL=100");
                lines.Add($"DRIVER_NAME=Candidate_{i}");
            }

            File.WriteAllLines(raceIniPath, lines);
            CrewChiefV4.ConsoleLogger.Log.Verbose($"Configured race.ini for {numCars} cars and {laps} laps.");
        }

        private void UpdateIniKey(List<string> lines, string section, string key, string value)
        {
            bool inSection = false;
            bool foundKey = false;
            for (int i = 0; i < lines.Count; i++)
            {
                string line = lines[i].Trim();
                if (line.StartsWith("["))
                {
                    if (inSection) break;
                    inSection = line.Equals(section, StringComparison.OrdinalIgnoreCase);
                }
                else if (inSection && line.StartsWith(key + "="))
                {
                    lines[i] = $"{key}={value}";
                    foundKey = true;
                    break;
                }
            }

            if (!foundKey)
            {
                int secIndex = lines.FindIndex(l => l.Trim().Equals(section, StringComparison.OrdinalIgnoreCase));
                if (secIndex >= 0)
                {
                    lines.Insert(secIndex + 1, $"{key}={value}");
                }
                else
                {
                    lines.Add("");
                    lines.Add(section);
                    lines.Add($"{key}={value}");
                }
            }
        }

        private void LaunchSimulationAndWait(int numCandidates, int laps)
        {
            string acsExe = Path.Combine(acInstallPath, "acs.exe");
            if (!File.Exists(acsExe))
            {
                throw new FileNotFoundException("Could not find acs.exe at " + acsExe);
            }

            string outJsonPath = Path.Combine(acDocumentsPath, "out", "race_out.json");
            if (File.Exists(outJsonPath))
            {
                try { File.Delete(outJsonPath); }
                catch (Exception ex)
                {
                    CrewChiefV4.ConsoleLogger.Log.Error($"Failed to delete stale telemetry file: {ex.Message}");
                    throw new InvalidOperationException("Cannot start simulation: stale telemetry file is locked.", ex);
                }
            }

            MemoryMappedFile shmCtrl0 = null;
            MemoryMappedFile shmState = null;
            MemoryMappedViewAccessor stateAccessor = null;
            ProcessJobTracker jobTracker = null;
            
            try
            {
                try 
                {
                    shmCtrl0 = MemoryMappedFile.CreateOrOpen("AcTools.CSP.NewBehaviour.CustomAI.CarControls0.v0", 64, MemoryMappedFileAccess.ReadWrite);
                    using (var stream = shmCtrl0.CreateViewStream())
                    {
                        byte[] zeros = new byte[64];
                        stream.Write(zeros, 0, 64);
                    }

                    shmState = MemoryMappedFile.CreateOrOpen("AcTools.CSP.NewBehaviour.CustomAI.SimState.v1", 16, MemoryMappedFileAccess.ReadWrite);
                    stateAccessor = shmState.CreateViewAccessor(0, 16);
                    WriteSimState(stateAccessor, false, false, true, 0, 10.0f);
                } 
                catch (Exception ex)
                {
                    CrewChiefV4.ConsoleLogger.Log.Verbose($"Error initializing shared memory: {ex.Message}");
                    throw;
                }

                CrewChiefV4.ConsoleLogger.Log.Verbose("[OK] Initialized CSP Custom AI: SimState 10x Speed & CarControls0 mapping");
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = acsExe,
                    WorkingDirectory = acInstallPath,
                    UseShellExecute = false
                };
                psi.EnvironmentVariables["AC_CFG_RACE_INI"] = "race.ini";
                if (psi.EnvironmentVariables.ContainsKey("AC_TOOL_RUN"))
                {
                    psi.EnvironmentVariables.Remove("AC_TOOL_RUN");
                }

                jobTracker = new ProcessJobTracker();

                using (Process p = Process.Start(psi))
                {
                    try
                    {
                        jobTracker.AddProcess(p);
                        CrewChiefV4.ConsoleLogger.Log.Verbose($"Simulation Executable PID: {p.Id} (Assigned to Job Object)");
                    }
                    catch (Exception ex) when (ex is InvalidOperationException || ex is Win32Exception)
                    {
                        if (!p.HasExited) throw;
                        CrewChiefV4.ConsoleLogger.Log.Verbose($"Process exited immediately before assignment.");
                    }

                    DateTime startTime = DateTime.UtcNow;
                    string logPath = Path.Combine(acDocumentsPath, "logs", "log.txt");

                    long lastLogPosition = 0;
                    int dynamicTimeoutSeconds = Math.Max(300, (laps * 200) + 120);

                    while ((DateTime.UtcNow - startTime).TotalSeconds < dynamicTimeoutSeconds)
                    {
                        if (p.HasExited) break;

                        WriteSimState(stateAccessor, false, false, true, 0, 10.0f);

                        if (File.Exists(outJsonPath) && (DateTime.UtcNow - startTime).TotalSeconds > 30)
                        {
                            CrewChiefV4.ConsoleLogger.Log.Verbose("[FINISH] Detected race_out.json flush.");
                            break;
                        }

                        // Fix 2: Log Scraper Byte-Offset Desync (Data Corruption)
                        if (File.Exists(logPath))
                        {
                            try
                            {
                                using (var fs = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                                {
                                    long length = fs.Length;
                                    if (length > lastLogPosition)
                                    {
                                        // Fix 3: Bound log chunking to max 5MB per tick to prevent OOM on huge logs
                                        long bytesToRead = Math.Min(length - lastLogPosition, 1024 * 1024 * 5);
                                        byte[] buffer = new byte[bytesToRead];
                                        fs.Seek(lastLogPosition, SeekOrigin.Begin);
                                        int read = fs.Read(buffer, 0, (int)bytesToRead);
                                        if (read > 0)
                                        {
                                            int lastNewlineIdx = -1;
                                            for (int i = read - 1; i >= 0; i--)
                                            {
                                                if (buffer[i] == '\n')
                                                {
                                                    lastNewlineIdx = i;
                                                    break;
                                                }
                                            }

                                            if (lastNewlineIdx >= 0)
                                            {
                                                // Convert only up to the last confirmed newline, guaranteeing complete lines
                                                string completeChunk = System.Text.Encoding.UTF8.GetString(buffer, 0, lastNewlineIdx + 1);
                                                
                                                // Advance position exactly by raw bytes consumed, avoiding BOM/char desyncs
                                                lastLogPosition += (lastNewlineIdx + 1);

                                                int completed = Regex.Matches(completeChunk, "LAP VALID: 1 CUTS:0, COUNT: 1").Count;
                                                if (completed > 0) 
                                                {
                                                     CrewChiefV4.ConsoleLogger.Log.Verbose($"Grid progress: +{completed} cars recorded valid lap.");
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            catch (IOException) { }
                        }

                        Thread.Sleep(1000);
                    }

                    if (!p.HasExited)
                    {
                        CrewChiefV4.ConsoleLogger.Log.Verbose("Simulation timed out. Terminating safely.");
                        try
                        {
                            p.Kill();
                            p.WaitForExit(5000);
                        }
                        catch (Exception killEx) 
                        {
                            CrewChiefV4.ConsoleLogger.Log.Verbose($"Error killing process: {killEx.Message}");
                        }
                    }
                }
                CrewChiefV4.ConsoleLogger.Log.Verbose("Simulation run ended.");
            }
            finally
            {
                if (stateAccessor != null) stateAccessor.Dispose();
                if (shmCtrl0 != null) shmCtrl0.Dispose();
                if (shmState != null) shmState.Dispose();
                if (jobTracker != null) jobTracker.Dispose();
            }
        }

        private void WriteSimState(MemoryMappedViewAccessor accessor, bool pause, bool restart, bool disableCollisions, byte extraSleep, float timeScale)
        {
            if (accessor == null) return;
            try
            {
                accessor.Write(0, pause);
                accessor.Write(1, restart);
                accessor.Write(2, disableCollisions);
                accessor.Write(3, extraSleep);
                accessor.Write(4, timeScale);
            }
            catch (Exception ex)
            {
                CrewChiefV4.ConsoleLogger.Log.Verbose($"Error writing SimState: {ex.Message}");
            }
        }

        private int EvaluateRaceResults(int numCandidates)
        {
            string outJson = Path.Combine(acDocumentsPath, "out", "race_out.json");
            
            for (int retries = 0; retries < 15; retries++)
            {
                if (File.Exists(outJson))
                {
                    try
                    {
                        string json = File.ReadAllText(outJson);
                        JObject result = JObject.Parse(json);
                        
                        var sessions = result["sessions"];
                        if (sessions != null && sessions.Any())
                        {
                            var lastSession = sessions.Last();
                            var laps = lastSession["laps"];
                            if (laps != null)
                            {
                                int bestCar = -1;
                                long bestTime = long.MaxValue;

                                foreach (var lap in laps)
                                {
                                    int cuts = (int?)lap["cuts"] ?? 0;
                                    long time = (long?)lap["time"] ?? 0;
                                    int carIdx = (int?)lap["car"] ?? -1;

                                    if (cuts == 0 && time > 0 && time < bestTime && carIdx >= 0)
                                    {
                                        bestTime = time;
                                        bestCar = carIdx;
                                    }
                                }

                                if (bestCar >= 0)
                                {
                                    CrewChiefV4.ConsoleLogger.Log.Verbose($"Found winning setup! Car {bestCar} (candidate_{bestCar}.ini) ran fastest lap of {bestTime / 1000.0:F3}s.");
                                    return bestCar;
                                }
                            }
                        }
                    }
                    catch (Exception ex) when (ex is IOException || ex is JsonException)
                    {
                        CrewChiefV4.ConsoleLogger.Log.Verbose($"[RETRY] Waiting for race_out.json write/flush to complete ({ex.GetType().Name})...");
                        Thread.Sleep(500);
                        continue;
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Error parsing race_out.json: " + ex.Message, ex);
                    }
                }
                Thread.Sleep(1000);
            }

            throw new Exception("Evaluation failed. Could not read valid race_out.json after simulation completion. No valid lap times found.");
        }

        private void SaveBaseRecSetup(string carName, string trackName, int bestCarIndex)
        {
            try
            {
                string setupsDir = Path.Combine(acDocumentsPath, "setups", carName, trackName);
                string winningSetup = Path.Combine(setupsDir, $"candidate_{bestCarIndex}.ini");
                string baseRecPath = Path.Combine(setupsDir, "base_rec.ini");

                if (File.Exists(winningSetup))
                {
                    File.Copy(winningSetup, baseRecPath, true);
                    CrewChiefV4.ConsoleLogger.Log.Verbose($"Saved winning setup as '{baseRecPath}'. Ready for Ideal Line Finder!");
                }
                else
                {
                    throw new FileNotFoundException("Could not find the winning setup file: " + winningSetup);
                }
            }
            catch (Exception ex)
            {
                CrewChiefV4.ConsoleLogger.Log.Error($"Failed to save base rec setup: {ex.Message}");
                throw;
            }
        }
    }
}

