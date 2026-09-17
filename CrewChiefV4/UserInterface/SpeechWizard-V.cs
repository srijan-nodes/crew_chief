using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using CrewChiefV4.UserInterface.Models;
using CrewChiefV4.UserInterface.VMs;

namespace CrewChiefV4.UserInterface
{
    public partial class SpeechWizard_V : Form
    {
        private readonly SpeechWizard_VM vm;
        private readonly SpeechWizard model;

        public SpeechWizard_V(bool IsAppRunning)
        {
            vm = new SpeechWizard_VM(this);
            model = new SpeechWizard(vm);
            ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainWindow));
            this.Icon = ((Icon)(resources.GetObject("$this.Icon")));
            this.StartPosition = FormStartPosition.CenterParent;
            InitializeComponent();

            if (DarkModeForms.DarkModeCS.IsDarkModeCSEnabled)
                _ = new DarkModeForms.DarkModeCS(this);

            this.toolTip1.SetToolTip(this.radioButtonMicrosoftSRE, Configuration.getUIString("prefer_microsoft_sre_help"));
            this.toolTip1.SetToolTip(this.radioButtonSystemSRE, Configuration.getUIString("prefer_system_sre_help"));
            this.toolTip1.SetToolTip(this.checkBoxNaudio, Configuration.getUIString("use_naudio_for_speech_recognition_help"));
            this.toolTip1.SetToolTip(this.textBoxSpeechRecognitionCountry, Configuration.getUIString("speech_recognition_country_help"));
            this.toolTip1.SetToolTip(this.textBoxUILanguage, Configuration.getUIString("ui_language_help"));
            this.toolTip1.SetToolTip(this.checkBoxFreeDictation, Configuration.getUIString("use_free_dictation_for_chat_help"));
            checkBoxNaudio.Checked = UserSettings.GetUserSettings().getBoolean("use_naudio_for_speech_recognition");
            radioButtonMicrosoftSRE.Checked = !UserSettings.GetUserSettings().getBoolean("prefer_system_sre");
            radioButtonSystemSRE.Checked = UserSettings.GetUserSettings().getBoolean("prefer_system_sre");
            checkBoxFreeDictation.Checked = UserSettings.GetUserSettings().getBoolean("use_free_dictation_for_chat");
            textBoxSpeechRecognitionCountry.Text = UserSettings.GetUserSettings().getString("speech_recognition_country");
            textBoxUILanguage.Text = UserSettings.GetUserSettings().getString("ui_language");
            model.Init(IsAppRunning);
            SpeechWizard.Active = true;
        }

        private void buttonRestartCrewChief_Click(object sender, EventArgs e)
        {
            model.buttonRestartCrewChief_Click();
        }

        private void radioButtonMicrosoftSRE_CheckedChanged(object sender, EventArgs e)
        {
            model.radioButtonMicrosoftSRE_CheckedChanged();
        }

        private void radioButtonSystem_CheckedChanged(object sender, EventArgs e)
        {
            model.radioButtonSystem_CheckedChanged();
        }

        private void checkBoxNaudio_CheckedChanged(object sender, EventArgs e)
        {
            model.checkBoxNaudio_CheckedChanged(checkBoxNaudio.Checked);
        }

        private void checkBoxFreeDictation_CheckedChanged(object sender, EventArgs e)
        {
            model.checkBoxFreeDictation_CheckedChanged(checkBoxFreeDictation.Checked);
        }

        private void textBoxUILanguage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                model.textBoxUILanguage_KeyDown(textBoxUILanguage.Text);
            }
        }

        private void textBoxSpeechRecognitionCountry_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                model.textBoxSpeechRecognitionCountry_KeyDown(textBoxSpeechRecognitionCountry.Text);
            }
        }

        private void SpeechWizard_V_FormClosed(object sender, FormClosedEventArgs e)
        {
            SpeechWizard.Active = false;
            model.ShutDown();
        }

        private void buttonHelp_Click(object sender, EventArgs e)
        {
            var form = new CrewChiefV4.HelpWindow(this, "VoiceRecognition_InstallationTraining");
            form.ShowDialog(this);
        }
    }
}

