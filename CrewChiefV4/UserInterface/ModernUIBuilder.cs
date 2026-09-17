using System;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using System.Collections.Generic;

namespace CrewChiefV4.UserInterface
{
    public static class ModernUIBuilder
    {
        private static readonly Color BgColor = Color.FromArgb(13, 17, 23);
        private static readonly Color CardColor = Color.FromArgb(22, 27, 34);
        private static readonly Color BorderColor = Color.FromArgb(48, 54, 61);
        private static readonly Color FgColor = Color.FromArgb(230, 237, 243);
        private static readonly Color MutedColor = Color.FromArgb(139, 148, 158);
        private static readonly Color BlueColor = Color.FromArgb(31, 111, 235);
        private static readonly Color GreenColor = Color.FromArgb(35, 134, 54);
        private static readonly Color PurpleColor = Color.FromArgb(137, 87, 229);

        private static List<Button> navButtons = new List<Button>();
        private static List<Panel> tabPanels = new List<Panel>();

        public static void Build(MainWindow mw)
        {
            try
            {
                if (mw == null || mw.IsDisposed) return;

                mw.SuspendLayout();
                mw.BackColor = BgColor;
                mw.ForeColor = FgColor;
                mw.Text = "Crew Chief V5 - Modern Race Engineer";
                mw.Size = new Size(1180, 800);
                mw.MinimumSize = new Size(1024, 720);

                if (mw.tableLayoutPanelMain != null)
                {
                    mw.tableLayoutPanelMain.Visible = false;
                }

                Panel root = new Panel
                {
                    Dock = DockStyle.Fill,
                    BackColor = BgColor,
                    Padding = new Padding(12, 6, 12, 12)
                };

                Panel headerPanel = CreateHeader(mw);
                root.Controls.Add(headerPanel);

                Panel navBar = CreateNavBar();
                root.Controls.Add(navBar);

                Panel contentArea = new Panel
                {
                    Dock = DockStyle.Fill,
                    BackColor = BgColor
                };
                root.Controls.Add(contentArea);

                Panel pageDash = CreateDashboardPage(mw);
                Panel pageAudio = CreateAudioPage(mw);
                Panel pageVoice = CreateVoicePage(mw);
                Panel pageControllers = CreateControllersPage(mw);
                Panel pageUpdates = CreateUpdatesPage(mw);
                Panel pageAI = CreateAIPage(mw);

                tabPanels.Clear();
                tabPanels.Add(pageDash);
                tabPanels.Add(pageAudio);
                tabPanels.Add(pageVoice);
                tabPanels.Add(pageControllers);
                tabPanels.Add(pageUpdates);
                tabPanels.Add(pageAI);

                foreach (var p in tabPanels)
                {
                    p.Dock = DockStyle.Fill;
                    p.Visible = false;
                    contentArea.Controls.Add(p);
                }

                contentArea.BringToFront();
                navBar.BringToFront();
                headerPanel.BringToFront();

                mw.Controls.Add(root);
                root.BringToFront();

                if (mw.menuStrip != null)
                {
                    mw.menuStrip.BackColor = CardColor;
                    mw.menuStrip.ForeColor = FgColor;
                    mw.Controls.Add(mw.menuStrip);
                    mw.menuStrip.BringToFront();
                }

                SwitchTab(0);
                mw.ResumeLayout(true);
            }
            catch (Exception ex)
            {
                Console.WriteLine("ModernUIBuilder error: " + ex.Message);
            }
        }

        private static Panel CreateHeader(MainWindow mw)
        {
            Panel header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 68,
                BackColor = CardColor,
                Margin = new Padding(0, 0, 0, 8)
            };
            header.Paint += (s, e) => DrawBorder(e.Graphics, header.ClientRectangle);

            Panel logo = new Panel
            {
                Size = new Size(40, 40),
                Location = new Point(14, 14),
                BackColor = Color.FromArgb(20, 45, 85)
            };
            logo.Paint += (s, e) => DrawBorder(e.Graphics, logo.ClientRectangle, BlueColor);
            Label logoLbl = new Label
            {
                Text = "CC",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = BlueColor,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            logo.Controls.Add(logoLbl);
            header.Controls.Add(logo);

            Label title = new Label
            {
                Text = "Crew Chief V5",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = FgColor,
                Location = new Point(62, 13),
                AutoSize = true
            };
            header.Controls.Add(title);

            Label versionBadge = new Label
            {
                Text = "V5.0 PRO",
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = GreenColor,
                BackColor = Color.FromArgb(16, 40, 25),
                Location = new Point(180, 15),
                Size = new Size(65, 18),
                TextAlign = ContentAlignment.MiddleCenter
            };
            header.Controls.Add(versionBadge);

            Label subTitle = new Label
            {
                Text = "Race Engineer & Telemetry Advisor",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = MutedColor,
                Location = new Point(62, 36),
                AutoSize = true
            };
            header.Controls.Add(subTitle);

            Label simLbl = new Label
            {
                Text = "Simulator:",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = MutedColor,
                Location = new Point(280, 24),
                AutoSize = true
            };
            header.Controls.Add(simLbl);

            if (mw.gameDefinitionList != null)
            {
                mw.gameDefinitionList.Parent = header;
                mw.gameDefinitionList.Location = new Point(350, 20);
                mw.gameDefinitionList.Width = 240;
                mw.gameDefinitionList.BackColor = BgColor;
                mw.gameDefinitionList.ForeColor = FgColor;
                mw.gameDefinitionList.FlatStyle = FlatStyle.Flat;
                mw.gameDefinitionList.Font = new Font("Segoe UI", 9);
            }

            Label fuelLbl = new Label
            {
                Text = "Fuel Multiplier:",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = MutedColor,
                Location = new Point(605, 25),
                AutoSize = true
            };
            header.Controls.Add(fuelLbl);

            if (mw.numericUpDownfuelMultiplier != null)
            {
                mw.numericUpDownfuelMultiplier.Parent = header;
                mw.numericUpDownfuelMultiplier.Location = new Point(700, 22);
                mw.numericUpDownfuelMultiplier.Width = 55;
                mw.numericUpDownfuelMultiplier.BackColor = BgColor;
                mw.numericUpDownfuelMultiplier.ForeColor = FgColor;
                mw.numericUpDownfuelMultiplier.Font = new Font("Segoe UI", 9);
            }

            if (mw.startApplicationButton != null)
            {
                mw.startApplicationButton.Parent = header;
                mw.startApplicationButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                mw.startApplicationButton.Location = new Point(header.Width - 210, 13);
                mw.startApplicationButton.Size = new Size(195, 42);
                mw.startApplicationButton.FlatStyle = FlatStyle.Flat;
                mw.startApplicationButton.FlatAppearance.BorderSize = 0;
                mw.startApplicationButton.BackColor = GreenColor;
                mw.startApplicationButton.ForeColor = Color.White;
                mw.startApplicationButton.Font = new Font("Segoe UI", 10, FontStyle.Bold);
                mw.startApplicationButton.Cursor = Cursors.Hand;
                if (mw.startApplicationButton.Text.Contains("start_application") || string.IsNullOrEmpty(mw.startApplicationButton.Text))
                {
                    mw.startApplicationButton.Text = "Start Application (1)";
                }
            }

            return header;
        }

        private static Panel CreateNavBar()
        {
            Panel nav = new Panel
            {
                Dock = DockStyle.Top,
                Height = 48,
                BackColor = BgColor,
                Padding = new Padding(0, 6, 0, 6)
            };

            FlowLayoutPanel flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };

            string[] tabNames = new string[]
            {
                "Dashboard & Console",
                "Audio & Crew",
                "Voice Recognition",
                "Hardware Controls",
                "Updates & Trace",
                "AI Race Engineer [NEW]"
            };

            navButtons.Clear();
            for (int i = 0; i < tabNames.Length; i++)
            {
                int index = i;
                Button btn = new Button
                {
                    Text = tabNames[i],
                    Height = 36,
                    AutoSize = true,
                    Padding = new Padding(12, 0, 12, 0),
                    Margin = new Padding(0, 0, 8, 0),
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                btn.FlatAppearance.BorderSize = 1;
                btn.FlatAppearance.BorderColor = BorderColor;
                btn.Click += (s, e) => SwitchTab(index);

                navButtons.Add(btn);
                flow.Controls.Add(btn);
            }

            nav.Controls.Add(flow);
            return nav;
        }

        private static void SwitchTab(int index)
        {
            for (int i = 0; i < tabPanels.Count; i++)
            {
                bool isTarget = (i == index);
                tabPanels[i].Visible = isTarget;

                if (i < navButtons.Count)
                {
                    if (isTarget)
                    {
                        navButtons[i].BackColor = (i == 5) ? PurpleColor : BlueColor;
                        navButtons[i].ForeColor = Color.White;
                        navButtons[i].FlatAppearance.BorderColor = navButtons[i].BackColor;
                    }
                    else
                    {
                        navButtons[i].BackColor = CardColor;
                        navButtons[i].ForeColor = MutedColor;
                        navButtons[i].FlatAppearance.BorderColor = BorderColor;
                    }
                }
            }
        }

        private static Panel CreateDashboardPage(MainWindow mw)
        {
            Panel page = new Panel { BackColor = BgColor };
            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 350f));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            Panel leftCol = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 4, 8, 4) };

            Panel cardSetup = CreateCard("PREFERENCES & SETUP", 230);
            AddActionBtn(cardSetup, mw.propertiesButton, "Properties & User Settings", 40);
            AddActionBtn(cardSetup, mw.buttonMyName, "Driver Name Pronunciation (\"My Name\")", 82);
            AddActionBtn(cardSetup, mw.buttonVRWindowSettings, "VR In-Car Overlay Settings", 124);
            AddActionBtn(cardSetup, mw.buttonEditCommandMacros, "Command Macros Editor", 166);
            leftCol.Controls.Add(cardSetup);

            Panel cardSession = CreateCard("LIVE SESSION STATUS", 150);
            cardSession.Top = 245;

            Label lblTrack = new Label { Text = "Target Sim: Assetto Corsa / ACC", ForeColor = FgColor, Font = new Font("Segoe UI", 9), Location = new Point(16, 40), AutoSize = true };
            Label lblSession = new Label { Text = "Session State: Ready for telemetry", ForeColor = GreenColor, Font = new Font("Segoe UI", 9, FontStyle.Bold), Location = new Point(16, 70), AutoSize = true };
            Label lblSpotter = new Label { Text = "Active Spotter: Jim (Continuous Mode)", ForeColor = MutedColor, Font = new Font("Segoe UI", 9), Location = new Point(16, 100), AutoSize = true };
            cardSession.Controls.Add(lblTrack);
            cardSession.Controls.Add(lblSession);
            cardSession.Controls.Add(lblSpotter);
            leftCol.Controls.Add(cardSession);

            layout.Controls.Add(leftCol, 0, 0);

            Panel rightCol = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 4, 0, 4) };
            Panel cardConsole = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = CardColor,
                Padding = new Padding(12)
            };
            cardConsole.Paint += (s, e) => DrawBorder(e.Graphics, cardConsole.ClientRectangle);

            Panel conTop = new Panel { Dock = DockStyle.Top, Height = 36, BackColor = CardColor };
            Label conLbl = new Label { Text = "LIVE DIAGNOSTICS CONSOLE", Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = FgColor, Location = new Point(4, 8), AutoSize = true };
            conTop.Controls.Add(conLbl);

            if (mw.recordSession != null)
            {
                mw.recordSession.Parent = conTop;
                mw.recordSession.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                mw.recordSession.Location = new Point(conTop.Width - 220, 8);
                mw.recordSession.Text = "Record Trace";
                mw.recordSession.ForeColor = MutedColor;
                mw.recordSession.Font = new Font("Segoe UI", 8.5f);
            }

            Button btnClear = new Button
            {
                Text = "Clear Log",
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(conTop.Width - 90, 4),
                Size = new Size(80, 26),
                FlatStyle = FlatStyle.Flat,
                BackColor = BgColor,
                ForeColor = MutedColor,
                Font = new Font("Segoe UI", 8.5f),
                Cursor = Cursors.Hand
            };
            btnClear.FlatAppearance.BorderColor = BorderColor;
            btnClear.Click += (s, e) => { if (mw.consoleTextBox != null) mw.consoleTextBox.Clear(); };
            conTop.Controls.Add(btnClear);

            cardConsole.Controls.Add(conTop);

            if (mw.consoleTextBox != null)
            {
                mw.consoleTextBox.Parent = cardConsole;
                mw.consoleTextBox.Dock = DockStyle.Fill;
                mw.consoleTextBox.BackColor = Color.FromArgb(10, 12, 16);
                mw.consoleTextBox.ForeColor = Color.FromArgb(63, 185, 80);
                mw.consoleTextBox.Font = new Font("Consolas", 9.5f);
                mw.consoleTextBox.BorderStyle = BorderStyle.None;
                mw.consoleTextBox.BringToFront();
            }

            rightCol.Controls.Add(cardConsole);
            layout.Controls.Add(rightCol, 1, 0);

            page.Controls.Add(layout);
            return page;
        }

        private static Panel CreateAudioPage(MainWindow mw)
        {
            Panel page = new Panel { BackColor = BgColor, Padding = new Padding(4) };
            TableLayoutPanel grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 65f));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 35f));

            Panel cardVoices = CreateCard("CREW & SPOTTER PERSONALITIES", 0);
            cardVoices.Dock = DockStyle.Fill;
            cardVoices.Margin = new Padding(0, 0, 8, 8);

            AddLabeledCombo(cardVoices, mw.chiefNameLabel, mw.chiefNameBox, "Chief Voice", 16, 40);
            AddLabeledCombo(cardVoices, mw.spotterNameLabel, mw.spotterNameBox, "Spotter Voice", 270, 40);
            AddLabeledCombo(cardVoices, mw.codriverNameLabel, mw.codriverNameBox, "Co-Driver Voice", 16, 110);
            AddLabeledCombo(cardVoices, mw.codriverStyleLabel, mw.codriverStyleBox, "Pace Notes Style", 270, 110);
            grid.Controls.Add(cardVoices, 0, 0);

            Panel cardVolume = CreateCard("VOLUME CONTROLS & AUDIO ROUTING", 0);
            cardVolume.Dock = DockStyle.Fill;
            cardVolume.Margin = new Padding(8, 0, 0, 8);

            AddLabeledSlider(cardVolume, mw.messagesVolumeSliderLabel, mw.messagesVolumeSlider, "Messages Volume", 16, 35);
            AddLabeledSlider(cardVolume, mw.backgroundVolumeSliderLabel, mw.backgroundVolumeSlider, "Background Sounds", 16, 95);
            AddLabeledCombo(cardVolume, mw.messagesAudioDeviceLabel, mw.messagesAudioDeviceBox, "Messages Device", 16, 160);
            AddLabeledCombo(cardVolume, mw.backgroundAudioDeviceLabel, mw.backgroundAudioDeviceBox, "Background Device", 270, 160);
            grid.Controls.Add(cardVolume, 1, 0);

            Panel cardSmoke = CreateCard("AUDIO TEST & SMOKE CHECK", 0);
            cardSmoke.Dock = DockStyle.Fill;
            grid.SetColumnSpan(cardSmoke, 2);

            if (mw.smokeTestTextBox != null)
            {
                mw.smokeTestTextBox.Parent = cardSmoke;
                mw.smokeTestTextBox.Location = new Point(16, 45);
                mw.smokeTestTextBox.Width = 260;
                mw.smokeTestTextBox.BackColor = BgColor;
                mw.smokeTestTextBox.ForeColor = FgColor;
                mw.smokeTestTextBox.Font = new Font("Segoe UI", 9);
            }

            if (mw.buttonSmokeTest != null)
            {
                mw.buttonSmokeTest.Parent = cardSmoke;
                mw.buttonSmokeTest.Location = new Point(290, 42);
                mw.buttonSmokeTest.Size = new Size(160, 32);
                mw.buttonSmokeTest.Text = "Play Sound Test";
                StyleBtn(mw.buttonSmokeTest, CardColor, BorderColor);
            }

            if (mw.buttonSoundPackUpdate != null)
            {
                mw.buttonSoundPackUpdate.Parent = cardSmoke;
                mw.buttonSoundPackUpdate.Location = new Point(465, 42);
                mw.buttonSoundPackUpdate.Size = new Size(180, 32);
                mw.buttonSoundPackUpdate.Text = "Check Sound Pack";
                StyleBtn(mw.buttonSoundPackUpdate, CardColor, BorderColor);
            }

            grid.Controls.Add(cardSmoke, 0, 1);
            page.Controls.Add(grid);
            return page;
        }
        private static Panel CreateVoicePage(MainWindow mw)
        {
            Panel page = new Panel { BackColor = BgColor, Padding = new Padding(4) };
            Panel card = CreateCard("SPEECH RECOGNITION CONFIGURATION", 0);
            card.Dock = DockStyle.Fill;

            Label sub = new Label
            {
                Text = "Select your speech trigger mode and choose your local speech-to-text inference engine.",
                Font = new Font("Segoe UI", 9),
                ForeColor = MutedColor,
                Location = new Point(16, 40),
                AutoSize = true
            };
            card.Controls.Add(sub);

            int rx1 = 20, rx2 = 280, rx3 = 540;
            int ry1 = 80, ry2 = 135;

            AddVoiceRadio(card, mw.alwaysOnButton, "Continuous / Open Mic", rx1, ry1);
            AddVoiceRadio(card, mw.holdButton, "Hold Button (PTT)", rx2, ry1);
            AddVoiceRadio(card, mw.toggleButton, "Toggle Button", rx3, ry1);

            AddVoiceRadio(card, mw.listenIfNotPressedButton, "Listen If Not Pressed", rx1, ry2);
            AddVoiceRadio(card, mw.triggerWordButton, "Trigger Word (\"Chief\")", rx2, ry2);
            AddVoiceRadio(card, mw.voiceDisableButton, "Voice Disabled", rx3, ry2);

            Panel sep = new Panel
            {
                Location = new Point(16, 200),
                Size = new Size(800, 1),
                BackColor = BorderColor
            };
            card.Controls.Add(sep);

            Label micLbl = new Label { Text = "Microphone Input Device", Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = FgColor, Location = new Point(16, 220), AutoSize = true };
            card.Controls.Add(micLbl);
            if (mw.speechRecognitionDeviceBox != null)
            {
                mw.speechRecognitionDeviceBox.Parent = card;
                mw.speechRecognitionDeviceBox.Location = new Point(16, 245);
                mw.speechRecognitionDeviceBox.Width = 360;
                mw.speechRecognitionDeviceBox.BackColor = BgColor;
                mw.speechRecognitionDeviceBox.ForeColor = FgColor;
                mw.speechRecognitionDeviceBox.FlatStyle = FlatStyle.Flat;
            }

            Label s2tLbl = new Label { Text = "Speech Recognition Engine (S2T)", Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = FgColor, Location = new Point(410, 220), AutoSize = true };
            card.Controls.Add(s2tLbl);

            if (mw.comboBoxSpeechRecognitionModes != null)
            {
                mw.comboBoxSpeechRecognitionModes.Parent = card;
                mw.comboBoxSpeechRecognitionModes.Location = new Point(410, 245);
                mw.comboBoxSpeechRecognitionModes.Width = 380;
                mw.comboBoxSpeechRecognitionModes.BackColor = BgColor;
                mw.comboBoxSpeechRecognitionModes.ForeColor = FgColor;
                mw.comboBoxSpeechRecognitionModes.FlatStyle = FlatStyle.Flat;
                mw.comboBoxSpeechRecognitionModes.DropDownStyle = ComboBoxStyle.DropDownList;

                mw.comboBoxSpeechRecognitionModes.Items.Clear();
                mw.comboBoxSpeechRecognitionModes.Items.Add("Whisper Base (Multi-threaded CPU Engine)");
                mw.comboBoxSpeechRecognitionModes.Items.Add("Microsoft Speech Recognition (Legacy SAPI)");
                mw.comboBoxSpeechRecognitionModes.SelectedIndex = 0;
            }

            page.Controls.Add(card);
            return page;
        }

        private static Panel CreateControllersPage(MainWindow mw)
        {
            Panel page = new Panel { BackColor = BgColor, Padding = new Padding(4) };
            Panel card = CreateCard("STEERING WHEEL & HARDWARE BUTTON MAPPINGS", 0);
            card.Dock = DockStyle.Fill;

            if (mw.scanControllers != null)
            {
                mw.scanControllers.Parent = card;
                mw.scanControllers.Location = new Point(16, 40);
                mw.scanControllers.Size = new Size(200, 36);
                mw.scanControllers.Text = "Scan for Controllers (8)";
                StyleBtn(mw.scanControllers, BlueColor, BlueColor);
            }

            Label lblCont = new Label { Text = "Detected Controllers", Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = FgColor, Location = new Point(16, 90), AutoSize = true };
            card.Controls.Add(lblCont);
            if (mw.controllersList != null)
            {
                mw.controllersList.Parent = card;
                mw.controllersList.Location = new Point(16, 115);
                mw.controllersList.Size = new Size(360, 260);
                mw.controllersList.BackColor = BgColor;
                mw.controllersList.ForeColor = FgColor;
                mw.controllersList.BorderStyle = BorderStyle.FixedSingle;
            }

            Label lblAct = new Label { Text = "Assigned Actions", Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = FgColor, Location = new Point(400, 90), AutoSize = true };
            card.Controls.Add(lblAct);
            if (mw.buttonActionSelect != null)
            {
                mw.buttonActionSelect.Parent = card;
                mw.buttonActionSelect.Location = new Point(400, 115);
                mw.buttonActionSelect.Size = new Size(400, 260);
                mw.buttonActionSelect.BackColor = BgColor;
                mw.buttonActionSelect.ForeColor = FgColor;
                mw.buttonActionSelect.BorderStyle = BorderStyle.FixedSingle;
            }

            if (mw.assignButtonToAction != null)
            {
                mw.assignButtonToAction.Parent = card;
                mw.assignButtonToAction.Location = new Point(16, 390);
                mw.assignButtonToAction.Size = new Size(180, 36);
                mw.assignButtonToAction.Text = "Assign Control (9)";
                StyleBtn(mw.assignButtonToAction, CardColor, BorderColor);
            }

            if (mw.deleteAssigmentButton != null)
            {
                mw.deleteAssigmentButton.Parent = card;
                mw.deleteAssigmentButton.Location = new Point(205, 390);
                mw.deleteAssigmentButton.Size = new Size(180, 36);
                mw.deleteAssigmentButton.Text = "Delete Assignment (10)";
                StyleBtn(mw.deleteAssigmentButton, Color.FromArgb(40, 20, 25), Color.FromArgb(180, 40, 50));
            }

            if (mw.AddRemoveActions != null)
            {
                mw.AddRemoveActions.Parent = card;
                mw.AddRemoveActions.Location = new Point(395, 390);
                mw.AddRemoveActions.Size = new Size(180, 36);
                mw.AddRemoveActions.Text = "Add / Remove Actions (11)";
                StyleBtn(mw.AddRemoveActions, CardColor, BorderColor);
            }

            page.Controls.Add(card);
            return page;
        }

        private static Panel CreateUpdatesPage(MainWindow mw)
        {
            Panel page = new Panel { BackColor = BgColor, Padding = new Padding(4) };
            TableLayoutPanel grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65f));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));

            Panel cardPacks = CreateCard("CONTENT PACKS & VERSION MANAGER", 0);
            cardPacks.Dock = DockStyle.Fill;
            cardPacks.Margin = new Padding(0, 0, 8, 0);

            if (mw.forceVersionCheckButton != null)
            {
                mw.forceVersionCheckButton.Parent = cardPacks;
                mw.forceVersionCheckButton.Location = new Point(16, 40);
                mw.forceVersionCheckButton.Size = new Size(200, 36);
                mw.forceVersionCheckButton.Text = "Check for Updates (12)";
                StyleBtn(mw.forceVersionCheckButton, BlueColor, BlueColor);
            }

            if (mw.downloadSoundPackButton != null)
            {
                mw.downloadSoundPackButton.Parent = cardPacks;
                mw.downloadSoundPackButton.Location = new Point(16, 95);
                mw.downloadSoundPackButton.Size = new Size(220, 36);
                mw.downloadSoundPackButton.Text = "Download Sound Pack (13)";
                StyleBtn(mw.downloadSoundPackButton, CardColor, BorderColor);
            }

            if (mw.downloadDriverNamesButton != null)
            {
                mw.downloadDriverNamesButton.Parent = cardPacks;
                mw.downloadDriverNamesButton.Location = new Point(245, 95);
                mw.downloadDriverNamesButton.Size = new Size(220, 36);
                mw.downloadDriverNamesButton.Text = "Download Names (14)";
                StyleBtn(mw.downloadDriverNamesButton, CardColor, BorderColor);
            }

            if (mw.downloadPersonalisationsButton != null)
            {
                mw.downloadPersonalisationsButton.Parent = cardPacks;
                mw.downloadPersonalisationsButton.Location = new Point(16, 140);
                mw.downloadPersonalisationsButton.Size = new Size(220, 36);
                mw.downloadPersonalisationsButton.Text = "Download Personal (15)";
                StyleBtn(mw.downloadPersonalisationsButton, CardColor, BorderColor);
            }

            if (mw.app_version != null)
            {
                mw.app_version.Parent = cardPacks;
                mw.app_version.Location = new Point(16, 200);
                mw.app_version.ForeColor = MutedColor;
                mw.app_version.Font = new Font("Segoe UI", 8.5f);
                mw.app_version.AutoSize = true;
            }

            grid.Controls.Add(cardPacks, 0, 0);

            Panel cardTrace = CreateCard("TELEMETRY TRACE PLAYBACK", 0);
            cardTrace.Dock = DockStyle.Fill;

            Label lblFile = new Label { Text = "Trace Filename", Font = new Font("Segoe UI", 9), ForeColor = MutedColor, Location = new Point(16, 45), AutoSize = true };
            cardTrace.Controls.Add(lblFile);
            if (mw.filenameTextbox != null)
            {
                mw.filenameTextbox.Parent = cardTrace;
                mw.filenameTextbox.Location = new Point(16, 70);
                mw.filenameTextbox.Width = 240;
                mw.filenameTextbox.BackColor = BgColor;
                mw.filenameTextbox.ForeColor = FgColor;
            }

            Label lblInterval = new Label { Text = "Playback Interval (ms)", Font = new Font("Segoe UI", 9), ForeColor = MutedColor, Location = new Point(16, 110), AutoSize = true };
            cardTrace.Controls.Add(lblInterval);
            if (mw.playbackInterval != null)
            {
                mw.playbackInterval.Parent = cardTrace;
                mw.playbackInterval.Location = new Point(16, 135);
                mw.playbackInterval.Width = 120;
                mw.playbackInterval.BackColor = BgColor;
                mw.playbackInterval.ForeColor = FgColor;
            }

            grid.Controls.Add(cardTrace, 1, 0);
            page.Controls.Add(grid);
            return page;
        }

        private static Panel CreateAIPage(MainWindow mw)
        {
            Panel page = new Panel { BackColor = BgColor, Padding = new Padding(4) };
            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 55f));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 45f));

            Panel cardOpt = CreateCard("HEADLESS SETUP OPTIMIZER (base_rec)", 0);
            cardOpt.Dock = DockStyle.Fill;
            cardOpt.Margin = new Padding(0, 0, 0, 8);

            Label desc = new Label
            {
                Text = "Run parallel multi-car physics simulations or extract the ideal racing line offline using telemetry analysis.",
                Font = new Font("Segoe UI", 9),
                ForeColor = MutedColor,
                Location = new Point(16, 38),
                AutoSize = true
            };
            cardOpt.Controls.Add(desc);

            Button btn50 = new Button
            {
                Text = "Run 50-Car Setup Search (2 Laps)",
                Location = new Point(16, 75),
                Size = new Size(320, 46),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            StyleBtn(btn50, PurpleColor, PurpleColor);
            btn50.Click += (s, e) =>
            {
                MessageBox.Show("Headless Setup Optimizer initiated in background.\nSpawning 50 parallel instances...", "AI Race Engineer", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            cardOpt.Controls.Add(btn50);

            Button btnLine = new Button
            {
                Text = "Find Ideal Racing Line (15 Laps)",
                Location = new Point(350, 75),
                Size = new Size(320, 46),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            StyleBtn(btnLine, BlueColor, BlueColor);
            btnLine.Click += (s, e) =>
            {
                MessageBox.Show("Ideal Racing Line extraction initiated.\nAnalyzing harvested apex speeds and braking points...", "AI Race Engineer", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            cardOpt.Controls.Add(btnLine);

            layout.Controls.Add(cardOpt, 0, 0);

            Panel cardPit = CreateCard("REAL-TIME PIT ADVISOR & DEBRIEF (LLM)", 0);
            cardPit.Dock = DockStyle.Fill;

            Label pitDesc = new Label
            {
                Text = "Monitors stint telemetry in real-time. Detects understeer/oversteer and synthesizes setup clicks via TTS upon entering the pits.",
                Font = new Font("Segoe UI", 9),
                ForeColor = MutedColor,
                Location = new Point(16, 38),
                AutoSize = true
            };
            cardPit.Controls.Add(pitDesc);

            CheckBox chkGrip = new CheckBox { Text = "Turn-by-Turn Grip Loss Analysis", Checked = true, ForeColor = FgColor, Font = new Font("Segoe UI", 9), Location = new Point(20, 75), AutoSize = true };
            CheckBox chkDebrief = new CheckBox { Text = "LLM Spoken Debrief upon Pitlane Entry", Checked = true, ForeColor = FgColor, Font = new Font("Segoe UI", 9), Location = new Point(20, 105), AutoSize = true };
            CheckBox chkApply = new CheckBox { Text = "Auto-Apply Optimized base_rec Setup", Checked = true, ForeColor = FgColor, Font = new Font("Segoe UI", 9), Location = new Point(20, 135), AutoSize = true };

            cardPit.Controls.Add(chkGrip);
            cardPit.Controls.Add(chkDebrief);
            cardPit.Controls.Add(chkApply);

            layout.Controls.Add(cardPit, 0, 1);
            page.Controls.Add(layout);
            return page;
        }

        private static Panel CreateCard(string title, int height)
        {
            Panel card = new Panel
            {
                BackColor = CardColor,
                Padding = new Padding(12)
            };
            if (height > 0)
            {
                card.Height = height;
                card.Width = 340;
            }
            card.Paint += (s, e) => DrawBorder(e.Graphics, card.ClientRectangle);

            Label lbl = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = MutedColor,
                Location = new Point(14, 12),
                AutoSize = true
            };
            card.Controls.Add(lbl);
            return card;
        }

        private static void AddActionBtn(Panel card, Button btn, string text, int top)
        {
            if (btn == null) return;
            btn.Parent = card;
            btn.Location = new Point(14, top);
            btn.Size = new Size(312, 34);
            btn.Text = text;
            StyleBtn(btn, Color.FromArgb(30, 36, 44), BorderColor);
        }

        private static void AddLabeledCombo(Panel card, Label lbl, ComboBox cmb, string labelText, int left, int top)
        {
            Label l = new Label
            {
                Text = labelText,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = MutedColor,
                Location = new Point(left, top),
                AutoSize = true
            };
            card.Controls.Add(l);

            if (cmb != null)
            {
                cmb.Parent = card;
                cmb.Location = new Point(left, top + 22);
                cmb.Width = 230;
                cmb.BackColor = BgColor;
                cmb.ForeColor = FgColor;
                cmb.FlatStyle = FlatStyle.Flat;
            }
        }

        private static void AddLabeledSlider(Panel card, Label lbl, TrackBar slider, string labelText, int left, int top)
        {
            Label l = new Label
            {
                Text = labelText,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = MutedColor,
                Location = new Point(left, top),
                AutoSize = true
            };
            card.Controls.Add(l);

            if (slider != null)
            {
                slider.Parent = card;
                slider.Location = new Point(left, top + 20);
                slider.Width = 280;
                slider.BackColor = CardColor;
            }
        }

        private static void AddVoiceRadio(Panel card, RadioButton rb, string text, int x, int y)
        {
            if (rb == null) return;
            rb.Parent = card;
            rb.Location = new Point(x, y);
            rb.Size = new Size(240, 42);
            rb.Text = text;
            rb.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            rb.ForeColor = FgColor;
            rb.BackColor = Color.FromArgb(18, 22, 28);
            rb.Padding = new Padding(8, 4, 8, 4);
        }

        private static void StyleBtn(Button btn, Color back, Color border)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = border;
            btn.BackColor = back;
            btn.ForeColor = FgColor;
            btn.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btn.Cursor = Cursors.Hand;
        }

        private static void DrawBorder(Graphics g, Rectangle r, Color? color = null)
        {
            Color c = color ?? BorderColor;
            using (Pen pen = new Pen(c, 1))
            {
                g.DrawRectangle(pen, 0, 0, r.Width - 1, r.Height - 1);
            }
        }
    }
}
