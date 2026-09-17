using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace CrewChiefV4.HeadlessSimulation
{
    public class TelemetrySegmenter
    {
        private List<TurnSegment> trackTurns;
        private TurnSegment currentTurn = null;
        private string acDocumentsPath;
        private float lastDistanceTraveledMeters = 0f;
        private float stintDistanceMeters = 0f;

        public TelemetrySegmenter()
        {
            acDocumentsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Assetto Corsa");
        }

        public void LoadTrackProfile(string carName, string trackName)
        {
            string outDir = Path.Combine(acDocumentsPath, "out");
            string referenceFile = Path.Combine(outDir, $"{trackName}_{carName}_ideal_line.json");

            if (File.Exists(referenceFile))
            {
                try
                {
                    string json = File.ReadAllText(referenceFile);
                    var payload = JsonConvert.DeserializeAnonymousType(json, new { turns = new List<TurnSegment>() });
                    if (payload != null && payload.turns != null && payload.turns.Count > 0)
                    {
                        trackTurns = payload.turns;
                        CrewChiefV4.ConsoleLogger.Log.Verbose($"Loaded {trackTurns.Count} validated turns from {referenceFile}");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    CrewChiefV4.ConsoleLogger.Log.Verbose($"Failed to parse ideal_line.json: {ex.Message}");
                }
            }

            CrewChiefV4.ConsoleLogger.Log.Verbose("Warning: Could not load track profile. Telemetry segmentation will be inactive.");
            trackTurns = new List<TurnSegment>();
        }

        public TurnSegment UpdatePosition(float distanceTraveledMeters, float normalizedPosition)
        {
            if (trackTurns == null || trackTurns.Count == 0) return null;

            // Track genuine odometer and stint distance
                        if (distanceTraveledMeters >= lastDistanceTraveledMeters)
            {
                stintDistanceMeters += (distanceTraveledMeters - lastDistanceTraveledMeters);
            }
            else if (distanceTraveledMeters >= 0)
            {
                stintDistanceMeters += distanceTraveledMeters; // Wrapped around
            }
            lastDistanceTraveledMeters = distanceTraveledMeters;

            // Robust Euclidean normalization strictly bounded between 0.0f and 1.0f
            // Handles negative normalizedPosition values safely
            float lapDist = (normalizedPosition % 1.0f + 1.0f) % 1.0f;

            foreach (var turn in trackTurns)
            {
                bool inTurn = false;
                if (turn.StartNormalized <= turn.EndNormalized)
                {
                    inTurn = (lapDist >= turn.StartNormalized && lapDist <= turn.EndNormalized);
                }
                else
                {
                    // Handles turn spanning across start/finish line wrap-around
                    inTurn = (lapDist >= turn.StartNormalized || lapDist <= turn.EndNormalized);
                }

                if (inTurn)
                {
                    if (currentTurn != turn)
                    {
                        currentTurn = turn;
                    }
                    return turn;
                }
            }

            if (currentTurn != null)
            {
                currentTurn = null;
            }

            return null;
        }

        public float GetStintDistanceMeters()
        {
            return stintDistanceMeters;
        }

        public void ClearState()
        {
            stintDistanceMeters = 0f;
            lastDistanceTraveledMeters = 0f;
            currentTurn = null;
        }
    }
}
