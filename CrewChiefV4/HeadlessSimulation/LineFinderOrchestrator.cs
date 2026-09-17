using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Runtime.InteropServices;
using System.ComponentModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using CrewChiefV4.assetto.assettoData;

namespace CrewChiefV4.HeadlessSimulation
{
    public class LineFinderOrchestrator
    {
        private string acDocumentsPath;
        private string acInstallPath;
        private List<TurnSegment> harvestedTurns = new List<TurnSegment>();
        private volatile float harvestedTrackLengthMeters = 0f;

        public LineFinderOrchestrator(string customAcInstallPath = null)
        {
            acDocumentsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Assetto Corsa");
            if (customAcInstallPath != null)
            {
                acInstallPath = customAcInstallPath;
            }
            else
            {
                try
                {
                    using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 244210"))
                    {
                        if (key != null)
                        {
                            acInstallPath = key.GetValue("InstallLocation") as string;
                        }
                    }
                    if (string.IsNullOrEmpty(acInstallPath))
                    {
                        using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 244210"))
                        {
                            if (key != null)
                            {
                                acInstallPath = key.GetValue("InstallLocation") as string;
                            }
                        }
                    }
                }
                catch { }

                if (string.IsNullOrEmpty(acInstallPath))
                {
                    throw new InvalidOperationException("Assetto Corsa installation path not found in Registry. Ensure the game is installed.");
                }
            }
        }

        public bool RunIdealLineSimulation(string carName, string trackName, string trackConfig = "")
        {
            CrewChiefV4.ConsoleLogger.Log.Verbose($"Starting Ideal Racing Line Finder (15 laps) for {carName} at {trackName}...");

            string setupsDir = Path.Combine(acDocumentsPath, "setups", carName, trackName);
            string baseRecPath = Path.Combine(setupsDir, "base_rec.ini");

            if (!File.Exists(baseRecPath))
            {
                throw new FileNotFoundException($"base_rec.ini not found at {baseRecPath}. Run Setup Search before attempting line finder.");
            }

            string cfgPath = Path.Combine(acDocumentsPath, "cfg");
            string raceIniPath = Path.Combine(cfgPath, "race.ini");

            if (!Directory.Exists(cfgPath))
            {
                throw new DirectoryNotFoundException("Could not find AC cfg directory at " + cfgPath);
            }

            string raceIniBackup = raceIniPath + ".backup_linefinder";
            if (File.Exists(raceIniPath) && !File.Exists(raceIniBackup)) File.Copy(raceIniPath, raceIniBackup, true);

            try
            {
                int laps = 15;
                ConfigureRaceIniForLineFinder(raceIniPath, carName, trackName, trackConfig, 2, laps);
                
                LaunchSimulationAndWaitAndHarvest(laps);
                ExtractIdealTrajectory(carName, trackName);
                
                return true;
            }
            finally
            {
                if (File.Exists(raceIniBackup))
                {
                    File.Copy(raceIniBackup, raceIniPath, true);
                    File.Delete(raceIniBackup);
                }
            }
        }

        private void ConfigureRaceIniForLineFinder(string raceIniPath, string carName, string trackName, string trackConfig, int numCars, int laps)
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

            SimulationHelpers.UpdateIniKey(lines, "[RACE]", "TRACK", trackName);
            SimulationHelpers.UpdateIniKey(lines, "[RACE]", "CONFIG_TRACK", trackConfig);
            SimulationHelpers.UpdateIniKey(lines, "[RACE]", "CARS", numCars.ToString());
            SimulationHelpers.UpdateIniKey(lines, "[RACE]", "RACE_LAPS", laps.ToString());
            SimulationHelpers.UpdateIniKey(lines, "[RACE]", "MODEL", carName);
            SimulationHelpers.UpdateIniKey(lines, "[RACE]", "AI_LEVEL", "100");

            SimulationHelpers.UpdateIniKey(lines, "[SESSION_0]", "TYPE", "3");
            SimulationHelpers.UpdateIniKey(lines, "[SESSION_0]", "LAPS", laps.ToString());
            SimulationHelpers.UpdateIniKey(lines, "[SESSION_0]", "SPAWN_SET", "START");

            lines.Add("");
            lines.Add("[CAR_0]");
            lines.Add($"MODEL={carName}");
            lines.Add("SETUP=base_rec.ini");
            lines.Add("AI_LEVEL=100");
            lines.Add("DRIVER_NAME=DummyPlayer");

            lines.Add("");
            lines.Add("[CAR_1]");
            lines.Add($"MODEL={carName}");
            lines.Add("SETUP=base_rec.ini");
            lines.Add("AI_LEVEL=100");
            lines.Add("DRIVER_NAME=ReferenceAI");

            File.WriteAllLines(raceIniPath, lines);
            CrewChiefV4.ConsoleLogger.Log.Verbose($"Configured race.ini for Ideal Line Finder (2 cars, {laps} laps, base_rec.ini).");
        }


        private void LaunchSimulationAndWaitAndHarvest(int laps)
        {
            string acsExe = Path.Combine(acInstallPath, "acs.exe");
            if (!File.Exists(acsExe))
            {
                throw new FileNotFoundException("Could not find acs.exe at " + acsExe);
            }

            // Fix 1: Delete stale race_out.json before launching
            string outJsonPath = Path.Combine(acDocumentsPath, "out", "race_out.json");
            if (File.Exists(outJsonPath))
            {
                try { File.Delete(outJsonPath); }
                catch (Exception ex)
                {
                    CrewChiefV4.ConsoleLogger.Log.Error($"[Telemetry Harvester] Error deleting race_out.json: {ex.Message}");
                    throw new InvalidOperationException("Cannot start simulation: stale telemetry file is locked.", ex);
                }
            }

            CrewChiefV4.ConsoleLogger.Log.Verbose("Launching 15-Lap Reference Simulation for Telemetry Harvesting...");

            MemoryMappedFile shmCtrl0 = null;
            MemoryMappedFile shmState = null;
            MemoryMappedViewAccessor stateAccessor = null;
            ProcessJobTracker jobTracker = null;
            
            using (CancellationTokenSource cts = new CancellationTokenSource())
            {
                harvestedTurns.Clear();
                harvestedTrackLengthMeters = 0f;

                Thread harvestingThread = new Thread(() => 
                {
                    HarvestTelemetryLoop(cts.Token);
                }) { IsBackground = true };

                try
                {
                    shmCtrl0 = MemoryMappedFile.CreateOrOpen("AcTools.CSP.NewBehaviour.CustomAI.CarControls0.v0", 64, MemoryMappedFileAccess.ReadWrite);
                    using (var stream = shmCtrl0.CreateViewStream())
                    {
                        byte[] zeros = new byte[64];
                        stream.Write(zeros, 0, 64);
                    }

                    shmState = MemoryMappedFile.CreateOrOpen("AcTools.CSP.NewBehaviour.CustomAI.SimState.v1", 32, MemoryMappedFileAccess.ReadWrite);
                    stateAccessor = shmState.CreateViewAccessor(0, 32);
                    SimulationHelpers.WriteSimState(stateAccessor, false, false, true, 0, 10.0f);

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
                        }
                        catch (Exception ex) when (ex is InvalidOperationException || ex is Win32Exception)
                        {
                            if (!p.HasExited) throw;
                        }

                        harvestingThread.Start();

                        DateTime startTime = DateTime.UtcNow;
                        int dynamicTimeout = Math.Max(300, laps * 150);

                        while ((DateTime.UtcNow - startTime).TotalSeconds < dynamicTimeout)
                        {
                            if (p.HasExited) break;
                            SimulationHelpers.WriteSimState(stateAccessor, false, false, true, 0, 10.0f);
                            Thread.Sleep(1000);
                        }

                        if (!p.HasExited)
                        {
                            try
                            {
                                p.Kill();
                                p.WaitForExit(5000);
                            }
                            catch (Exception ex) { CrewChiefV4.ConsoleLogger.Log.Verbose($"[Telemetry Harvester] Error killing acs process: {ex.Message}"); }
                        }
                    }
                    CrewChiefV4.ConsoleLogger.Log.Verbose("Line Finder Simulation and Harvesting finished.");
                }
                finally
                {
                    cts.Cancel();
                    // Fix 5: Ensure the background thread is joined securely even on launch failure
                    if (harvestingThread.IsAlive)
                    {
                        if (!harvestingThread.Join(5000))
                        {
                            CrewChiefV4.ConsoleLogger.Log.Verbose("Harvesting thread deadlocked. Forcibly aborting.");
                            try { harvestingThread.Abort(); } catch { }
                        }
                    }
                    
                    if (stateAccessor != null) stateAccessor.Dispose();
                    if (shmCtrl0 != null) shmCtrl0.Dispose();
                    if (shmState != null) shmState.Dispose();
                    if (jobTracker != null) jobTracker.Dispose();
                }
            }
        }

        private void HarvestTelemetryLoop(CancellationToken token)
        {
            MemoryMappedFile physMap = null;
            MemoryMappedFile gfxMap = null;
            MemoryMappedFile staticMap = null;
            MemoryMappedViewAccessor physView = null;
            MemoryMappedViewAccessor gfxView = null;
            MemoryMappedViewAccessor staticView = null;

            GCHandle physHandle = default;
            GCHandle gfxHandle = default;
            GCHandle staticHandle = default;

            try 
            {
                // Wait for the game to initialize memory structures
                for (int i = 0; i < 30; i++)
                {
                    if (token.IsCancellationRequested) return;
                    try
                    {
                        physMap = MemoryMappedFile.OpenExisting("Local\\acpmf_physics");
                        gfxMap = MemoryMappedFile.OpenExisting("Local\\acpmf_graphics");
                        staticMap = MemoryMappedFile.OpenExisting("Local\\acpmf_static");

                        physView = physMap.CreateViewAccessor();
                        gfxView = gfxMap.CreateViewAccessor();
                        staticView = staticMap.CreateViewAccessor();
                        
                        // Fix 1: Only break if all three views mapped successfully
                        break;
                    }
                    catch (Exception ex)
                    {
                        if (staticView != null) { staticView.Dispose(); staticView = null; }
                        if (staticMap != null) { staticMap.Dispose(); staticMap = null; }
                        if (physView != null) { physView.Dispose(); physView = null; }
                        if (gfxView != null) { gfxView.Dispose(); gfxView = null; }
                        if (physMap != null) { physMap.Dispose(); physMap = null; }
                        if (gfxMap != null) { gfxMap.Dispose(); gfxMap = null; }
                        Thread.Sleep(500);
                    }
                }

                if (physView == null || gfxView == null || staticView == null) return;

                byte[] physBuffer = new byte[Marshal.SizeOf(typeof(SPageFilePhysics))];
                byte[] gfxBuffer = new byte[Marshal.SizeOf(typeof(SPageFileGraphic))];
                byte[] staticBuffer = new byte[Marshal.SizeOf(typeof(SPageFileStatic))];

                physHandle = GCHandle.Alloc(physBuffer, GCHandleType.Pinned);
                gfxHandle = GCHandle.Alloc(gfxBuffer, GCHandleType.Pinned);
                staticHandle = GCHandle.Alloc(staticBuffer, GCHandleType.Pinned);

                bool inBrakingZone = false;
                float currentBrakeStartDist = 0;
                float currentMinApexSpeed = 999f;
                float currentApexDist = 0;
                int turnCount = 1;
                
                object physObj = new SPageFilePhysics();
                object gfxObj = new SPageFileGraphic();
                object staticObj = new SPageFileStatic();

                while (!token.IsCancellationRequested)
                {
                    physView.ReadArray(0, physBuffer, 0, physBuffer.Length);
                    gfxView.ReadArray(0, gfxBuffer, 0, gfxBuffer.Length);
                    
                    Marshal.PtrToStructure(physHandle.AddrOfPinnedObject(), physObj);
                    Marshal.PtrToStructure(gfxHandle.AddrOfPinnedObject(), gfxObj);
                    SPageFilePhysics phys = (SPageFilePhysics)physObj;
                    SPageFileGraphic gfx = (SPageFileGraphic)gfxObj;
                    
                    if (gfx.status == AC_STATUS.AC_LIVE)
                    {
                        if (harvestedTrackLengthMeters <= 0)
                        {
                            try
                            {
                                staticView.ReadArray(0, staticBuffer, 0, staticBuffer.Length);
                                Marshal.PtrToStructure(staticHandle.AddrOfPinnedObject(), staticObj);
                                SPageFileStatic staticData = (SPageFileStatic)staticObj;
                                if (staticData.trackSPlineLength > 0)
                                {
                                    Interlocked.Exchange(ref harvestedTrackLengthMeters, staticData.trackSPlineLength);
                                }
                            }
                            catch (Exception ex) { CrewChiefV4.ConsoleLogger.Log.Verbose("Static read error: " + ex.Message); }
                        }
                        float lapDist = gfx.normalizedCarPosition;
                        int currentLap = gfx.completedLaps;

                        if (phys.brake > 0.5f && phys.gas < 0.1f && phys.speedKmh > 50)
                        {
                            if (!inBrakingZone)
                            {
                                inBrakingZone = true;
                                currentBrakeStartDist = lapDist;
                                currentMinApexSpeed = phys.speedKmh;
                            }
                        }
                        
                        if (inBrakingZone)
                        {
                            if (phys.speedKmh < currentMinApexSpeed)
                            {
                                currentMinApexSpeed = phys.speedKmh;
                                currentApexDist = lapDist;
                            }

                            if (phys.gas > 0.5f && phys.brake < 0.1f)
                            {
                                inBrakingZone = false;
                                lock (harvestedTurns)
                                {
                                    harvestedTurns.Add(new TurnSegment {
                                        TurnId = turnCount++,
                                        LapNumber = currentLap,
                                        StartNormalized = currentBrakeStartDist,
                                        ApexNormalized = currentApexDist,
                                        EndNormalized = lapDist,
                                        ApexSpeedKmh = currentMinApexSpeed
                                    });
                                }
                            }
                        }
                    }
                    Thread.Sleep(3);
                }
            }
            catch (Exception ex)
            {
                CrewChiefV4.ConsoleLogger.Log.Verbose("Telemetry harvesting error: " + ex.Message);
            }
            finally
            {
                if (physHandle.IsAllocated) physHandle.Free();
                if (gfxHandle.IsAllocated) gfxHandle.Free();
                if (staticHandle.IsAllocated) staticHandle.Free();

                if (staticView != null) staticView.Dispose();
                if (staticMap != null) staticMap.Dispose();
                if (physView != null) physView.Dispose();
                if (physMap != null) physMap.Dispose();
                if (gfxView != null) gfxView.Dispose();
                if (gfxMap != null) gfxMap.Dispose();
            }
        }


        private void ExtractIdealTrajectory(string carName, string trackName)
        {
            string outDir = Path.Combine(acDocumentsPath, "out");
            Directory.CreateDirectory(outDir);
            string referenceFile = Path.Combine(outDir, $"{trackName}_{carName}_ideal_line.json");
            string outJson = Path.Combine(outDir, "race_out.json");
            
            long actualBestLapTimeMs = 0;
            
            for (int retries = 0; retries < 10; retries++)
            {
                if (File.Exists(outJson))
                {
                    try
                    {
                        JObject result = JObject.Parse(File.ReadAllText(outJson));
                        var sessions = result["sessions"];
                        if (sessions != null && sessions.Any())
                        {
                            var lastSession = sessions.Last();
                            var laps = lastSession["laps"];
                            long bestTime = long.MaxValue;
                            if (laps != null)
                            {
                                foreach (var lap in laps)
                                {
                                    long time = (long?)lap["time"] ?? 0;
                                    int cuts = (int?)lap["cuts"] ?? 0;
                                    if (cuts == 0 && time > 0 && time < bestTime)
                                    {
                                        bestTime = time;
                                    }
                                }
                            }
                            if (bestTime != long.MaxValue)
                            {
                                actualBestLapTimeMs = bestTime;
                                break;
                            }
                        }
                    }
                    catch (Exception ex) when (ex is IOException || ex is JsonException || ex is UnauthorizedAccessException)
                    {
                        Thread.Sleep(500);
                        continue;
                    }
                }
                Thread.Sleep(500);
            }

            lock (harvestedTurns)
            {
                var lapGroups = harvestedTurns.GroupBy(t => t.LapNumber).ToList();
                var bestLapGroup = lapGroups.OrderByDescending(g => g.Count()).FirstOrDefault();

                var uniqueTurns = new List<TurnSegment>();
                if (bestLapGroup != null)
                {
                    uniqueTurns = bestLapGroup.OrderBy(t => t.StartNormalized).ToList();
                    for (int i = 0; i < uniqueTurns.Count; i++) uniqueTurns[i].TurnId = i + 1;
                }

                if (uniqueTurns.Count == 0)
                {
                    throw new InvalidOperationException($"Ideal line trajectory extraction failed: 0 valid turns harvested for {carName} at {trackName}.");
                }

                if (actualBestLapTimeMs <= 0)
                {
                    throw new InvalidOperationException($"Ideal line trajectory extraction failed: No valid lap time recorded in {outJson}.");
                }

                var payload = new
                {
                    track = trackName,
                    car = carName,
                    setup = "base_rec.ini",
                    bestLapTimeMs = actualBestLapTimeMs,
                    trackLengthMeters = (double)harvestedTrackLengthMeters,
                    turns = uniqueTurns
                };

                File.WriteAllText(referenceFile, JsonConvert.SerializeObject(payload, Formatting.Indented));
                CrewChiefV4.ConsoleLogger.Log.Verbose($"Saved Reference Ideal Line Profile to {referenceFile} with {uniqueTurns.Count} harvested turns and {harvestedTrackLengthMeters:F1}m track length.");
            }
        }
    }
}










