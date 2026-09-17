        void personalisations_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if (formClosed)
            {
                return;
            }
            if (e.Error == null && !e.Cancelled)
            {
                String extractingButtonText = Configuration.getUIString("extracting_personalisations");
                downloadPersonalisationsButton.Text = extractingButtonText;
                string destPath = Path.Combine(AudioPlayer.soundFilesPathNoChiefOverride, "personalisations");
                string tempPath = Path.Combine(AudioPlayer.soundFilesPathNoChiefOverride, "personalisations_temp");
                var extractPersonalizationsThread = new Thread(() =>
                {
                    Thread.CurrentThread.IsBackground = true;
                    Boolean success = false;
                    Thread progressThread = null;
                    try
                    {
                        if (e.Error == null && !e.Cancelled)
                        {

                            if (Directory.Exists(tempPath))
                            {
                                Directory.Delete(tempPath, true);
                            }
                            if (formClosed)
                            {
                                return;
                            }
                            progressThread = createProgressThread(downloadPersonalisationsButton, extractingButtonText);
                            progressThread.Start();
                            ZipFile.ExtractToDirectory(AudioPlayer.soundFilesPathNoChiefOverride + @"\" + personalisationsTempFileName, tempPath, Encoding.UTF8);

                            // If we made it here, block the shutdown to complete the move.
                            lock (MainWindow.instanceLock)
                            {
                                if (MainWindow.instance != null)
                                {
                                    // (personalisations zip does not include personalisations folder)
                                    UpdateHelper.MoveDirectory(tempPath, destPath);
                                    success = true;
                                }
                            }
                        }
                    }
                    catch (Exception e2)
                    {
                        Console.WriteLine("Error extracting, " + e2.Message);
                    }
                    finally
                    {
                        if (progressThread != null)
                        {
                            progressThread.Abort();
                            Thread.Sleep(100);
                            downloadPersonalisationsButton.Text = Configuration.getUIString("personalisations_are_up_to_date");
                        }
                        if (success)
                        {
                            try
                            {
                                File.Delete(AudioPlayer.soundFilesPathNoChiefOverride + @"\" + personalisationsTempFileName);
                            }
                            catch (Exception ee) {Log.Exception(ee);}
                            SoundCache.DeleteZeroLengthWavFiles(destPath);
                        }
                        personalisationsProgressBar.Value = 0;
                        isDownloadingPersonalisations = false;
                        if (success && !isDownloadingSoundPack && !isDownloadingDriverNames)
                        {
                            doRestart(Configuration.getUIString(willNeedAnotherPersonalisationsDownload ? "the_application_must_be_restarted_to_load_the_new_sounds_need_another_restart" :
                                "the_application_must_be_restarted_to_load_the_new_sounds"), Configuration.getUIString("load_new_sounds"));
                        }
                    }
                    if (!success)
                    {
                        personalisationsUpdateFailed(false);
                    }
                });
                extractPersonalizationsThread.Name = "MainWindow.extractPersonalizationsThread";
                ThreadManager.RegisterResourceThread(extractPersonalizationsThread);
                extractPersonalizationsThread.Start();
            }
            else
            {
                personalisationsUpdateFailed(e.Cancelled);
            }
        }

        private void personalisationsUpdateFailed(Boolean cancelled)
        {
            if (!cancelled && !usingRetryAddressForPersonalisations && SoundPackVersionsHelper.retryReplace != null && SoundPackVersionsHelper.retryReplaceWith != null)
            {
                if (!cancelled && !usingRetryAddressForPersonalisations && SoundPackVersionsHelper.retryReplace != null && SoundPackVersionsHelper.retryReplaceWith != null)
                {
                    Console.WriteLine("Unable to get personalisations from " + SoundPackVersionsHelper.retryReplace + " will try from " + SoundPackVersionsHelper.retryReplaceWith);
                }
                usingRetryAddressForPersonalisations = true;
                personalisationsDownloadURL = SoundPackVersionsHelper.replaceUrlToRetry(personalisationsDownloadURL);
                startDownload(DownloadType.PERSONALISATIONS);
            }
            else
            {
                startApplicationButton.Enabled = !isDownloadingSoundPack && !isDownloadingDriverNames;
                if (SoundPackVersionsHelper.currentPersonalisationsVersion == -1)
                {
                    downloadPersonalisationsButton.Text = Configuration.getUIString("no_personalisations_detected_press_to_download");
                }
                else
                {
                    downloadPersonalisationsButton.Text = Configuration.getUIString("updated_personalisations_available_press_to_download");
                }
                if (!IsAppRunning)
                {
                    downloadPersonalisationsButton.Enabled = true;
                }
                if (!cancelled)
                {
                    MessageBox.Show(Configuration.getUIString("error_downloading_personalisations"), Configuration.getUIString("unable_to_download_personalisations"),
                        MessageBoxButtons.OK);
                }
            }
        }

        private void downloadPersonalisationsButtonPress(object sender, EventArgs e)
        {
            if (AudioPlayer.soundPackLanguage == null)
            {
                DialogResult dialogResult = MessageBox.Show(
                    Utilities.Strings.NewlinesInLongString(Configuration.getUIString("unknown_personalisations_language_text")),
                    Configuration.getUIString("unknown_personalisations_language_title"), MessageBoxButtons.YesNo);
                if (dialogResult == DialogResult.Yes)
                {
                    startApplicationButton.Enabled = false;
                    downloadPersonalisationsButton.Text = Configuration.getUIString("downloading_personalisations");
                    downloadPersonalisationsButton.Enabled = false;
                    startDownload(DownloadType.PERSONALISATIONS);
                }
                else if (dialogResult == DialogResult.No)
                {
                }
            }
            else
            {
                startApplicationButton.Enabled = false;
                downloadPersonalisationsButton.Text = Configuration.getUIString("downloading_personalisations");
                downloadPersonalisationsButton.Enabled = false;
                startDownload(DownloadType.PERSONALISATIONS);
            }
        }
