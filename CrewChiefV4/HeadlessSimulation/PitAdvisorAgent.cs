using System;
using CrewChiefV4.Audio;
using CrewChiefV4.Events;

namespace CrewChiefV4.HeadlessSimulation
{
    public class PitAdvisorAgent
    {
        private AudioPlayer audioPlayer;

        public PitAdvisorAgent(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
        }

        public string GenerateDebriefPrompt(string trackName, string baseRecPath, GripLossAnalyzer.GripDiagnosis[] sessionDiagnoses)
        {
            string prompt = $@"Driver just entered the pit lane at {trackName}. ";
            bool hasIssues = false;

            if (sessionDiagnoses != null)
            {
                foreach (var diag in sessionDiagnoses)
                {
                    if (diag != null && !string.IsNullOrEmpty(diag.Summary))
                    {
                        prompt += $"{diag.Summary} ";
                        hasIssues = true;
                    }
                }
            }

            if (hasIssues)
            {
                prompt += $"Suggesting driver loads the optimized setup: {baseRecPath}.";
            }
            else
            {
                prompt += "Stint looked clean. Box, box.";
            }

            return prompt;
        }

        public void SimulatePitInteraction(string speechText)
        {
            CrewChiefV4.ConsoleLogger.Log.Verbose($"[TTS Pit Advisor]: {speechText}");
            try
            {
                var frags = AbstractEvent.MessageContents(speechText);
                foreach (var frag in frags) { if (frag != null) frag.allowTTS = true; }
                
                audioPlayer.playMessageImmediately(
                    new QueuedMessage("setup_advisor", 0, 
                        messageFragments: frags));
            }
            catch (Exception ex)
            {
                CrewChiefV4.ConsoleLogger.Log.Verbose($"[TTS Error]: Could not play audio. {ex.Message}");
            }
        }
    }
}
