        void soundpack_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if (formClosed)
            {
                return;
            }
            if (e.Error == null && !e.Cancelled)
            {
                if (crewChief.audioPlayer != null)
                    crewChief.audioPlayer.disposeBackgroundPlayer();
                String extractingButtonText = Configuration.getUIString("extracting_sound_pack");
                downloadSoundPackButton.Text = extractingButtonText;
                string destPath = AudioPlayer.soundFilesPathNoChiefOverride;
                string tempPath = Path.Combine(AudioPlayer.soundFilesPathNoChiefOverride, "sounds_temp");
                var extractSoundPackThread = new Thread(() =>
                {
                    Thread.CurrentThread.IsBackground = true;
                    Boolean success = false;
                    Thread progressThread = null;
                    try
                    {
                        if (Directory.Exists(tempPath))
                        {
                            Directory.Delete(tempPath, true);
                        }
                        if (formClosed)
                        {
                            return;
                        }
                        progressThread = createProgressThread(downloadSoundPackButton, extractingButtonText);
                        progressThread.Start();
                        ZipFile.ExtractToDirectory(AudioPlayer.soundFilesPathNoChiefOverride + @"\" + soundPackTempFileName, tempPath);
                        // It's important to note that the order of these two calls must *not* matter. If it does, the update process results will be inconsistent.
                        // The update pack can contain file rename instructions and file delete instructions but it can *never* contain obsolete files (or files
                        // with old names). As long as this is the case, it shouldn't matter what order we do these in...
                        UpdateHelper.ProcessFileUpdates(tempPath);

                        // If we made it here, block the shutdown to complete the move.
                        lock (MainWindow.instanceLock)
                        {
                            if (MainWindow.instance != null)
                            {
                                UpdateHelper.MoveDirectory(tempPath, destPath);
                                success = true;
                            }
                        }
                    }
                    catch (Exception unzipException)
                    {
                        Console.WriteLine("Error extracting sound pack update " + unzipException.Message + ", " + unzipException.StackTrace);
                    }
                    finally
                    {
                        if (progressThread != null)
                        {
                            progressThread.Abort();
                            Thread.Sleep(100);
                            downloadSoundPackButton.Text = Configuration.getUIString("sound_pack_is_up_to_date");
                        }
                        if (success)
                        {
                            try
                            {
                                File.Delete(AudioPlayer.soundFilesPathNoChiefOverride + @"\" + soundPackTempFileName);
                            }
                            catch (Exception ee) {Log.Exception(ee);}
                            SoundCache.DeleteZeroLengthWavFiles(destPath);
                        }
                        soundPackProgressBar.Value = 0;
                        isDownloadingSoundPack = false;
                        if (success && !isDownloadingDriverNames && !isDownloadingPersonalisations)
                        {
                            doRestart(Configuration.getUIString(willNeedAnotherSoundPackDownload ? "the_application_must_be_restarted_to_load_the_new_sounds_need_another_restart" :
                                "the_application_must_be_restarted_to_load_the_new_sounds"), Configuration.getUIString("load_new_sounds"));
                        }
                    }
                    if (!success)
                    {
                        soundPackUpdateFailed(false);
                    }
                });
                extractSoundPackThread.Name = "MainWindow.extractSoundPackThread";
                ThreadManager.RegisterResourceThread(extractSoundPackThread);
                extractSoundPackThread.Start();
            }
            else
            {
                soundPackUpdateFailed(e.Cancelled);
            }
        }

        private void soundPackUpdateFailed(Boolean cancelled)
        {
            if (!cancelled && !usingRetryAddressForSoundPack && SoundPackVersionsHelper.retryReplace != null && SoundPackVersionsHelper.retryReplaceWith != null)
            {
                Console.WriteLine("Unable to get sound pack from " + SoundPackVersionsHelper.retryReplace + " will try from " + SoundPackVersionsHelper.retryReplaceWith);
                usingRetryAddressForSoundPack = true;
                soundPackDownloadURL = SoundPackVersionsHelper.replaceUrlToRetry(soundPackDownloadURL);
                startDownload(DownloadType.SOUND_PACK);
            }
            else
            {
                startApplicationButton.Enabled = !isDownloadingDriverNames && !isDownloadingPersonalisations;
                if (SoundPackVersionsHelper.currentSoundPackVersion == -1)
                {
                    downloadSoundPackButton.Text = Configuration.getUIString("no_sound_pack_detected_press_to_download");
                }
                else
                {
                    downloadSoundPackButton.Text = Configuration.getUIString("updated_sound_pack_available_press_to_download");
                }
                if (!IsAppRunning)
                {
                    downloadSoundPackButton.Enabled = true;
                }
                if (!cancelled)
                {
                    MessageBox.Show(Configuration.getUIString("error_downloading_sound_pack"), Configuration.getUIString("unable_to_download_sound_pack"),
                        MessageBoxButtons.OK);
                }
            }
        }

        private void downloadSoundPackButtonPress(object sender, EventArgs e)
        {
            if (AudioPlayer.soundPackLanguage == null)
            {
                DialogResult dialogResult = MessageBox.Show(
                    Utilities.Strings.NewlinesInLongString(Configuration.getUIString("unknown_sound_pack_language_text")),
                    Configuration.getUIString("unknown_sound_pack_language_title"), MessageBoxButtons.YesNo);
                if (dialogResult == DialogResult.Yes)
                {
                    startApplicationButton.Enabled = false;
                    downloadSoundPackButton.Text = Configuration.getUIString("downloading_sound_pack");
                    downloadSoundPackButton.Enabled = false;
                    startDownload(DownloadType.SOUND_PACK);
                }
                else if (dialogResult == DialogResult.No)
                {
                }
            }
            else
            {
                startApplicationButton.Enabled = false;
                downloadSoundPackButton.Text = Configuration.getUIString("downloading_sound_pack");
                downloadSoundPackButton.Enabled = false;
                startDownload(DownloadType.SOUND_PACK);
            }
        }
