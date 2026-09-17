// extracted from MainWindow.cs to allow comparison of the handling of the
// different buttons which I naively expected to be similar.

void drivernames_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
             if (formClosed)
             {
                 return;
             }
             if (e.Error == null && !e.Cancelled)
             {
                 String extractingButtonText = Configuration.getUIString("extracting_driver_names");
                 downloadDriverNamesButton.Text = extractingButtonText;
                 string destPath = Path.Combine(AudioPlayer.soundFilesPathNoChiefOverride, "driver_names");
                 string tempPath = Path.Combine(AudioPlayer.soundFilesPathNoChiefOverride, "driver_names_temp");
                 var extractDriverNamesThread = new Thread(() =>
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
                         progressThread = createProgressThread(downloadDriverNamesButton, extractingButtonText);
                         progressThread.Start();
                         ZipFile.ExtractToDirectory(AudioPlayer.soundFilesPathNoChiefOverride + @"\" + driverNamesTempFileName, tempPath, Encoding.UTF8);

                         // If we made it here, block the shutdown to complete the move.
                         lock (MainWindow.instanceLock)
                         {
                             if (MainWindow.instance != null)
                             {
                                 // (driver_names zip includes driver_names folder)
                                 UpdateHelper.MoveDirectory(tempPath, AudioPlayer.soundFilesPathNoChiefOverride);
                                 success = true;
                             }
                         }
                     }
                     catch (Exception ee) {Log.Exception(ee);}
                     finally
                     {
                         if (progressThread != null)
                         {
                             progressThread.Abort();
                             Thread.Sleep(100);
                             downloadDriverNamesButton.Text = Configuration.getUIString("driver_names_are_up_to_date");
                         }
                         if (success)
                         {
                             try
                             {
                                 File.Delete(AudioPlayer.soundFilesPathNoChiefOverride + @"\" + driverNamesTempFileName);
                             }
                             catch (Exception ee) {Log.Exception(ee);}
                             SoundCache.DeleteZeroLengthWavFiles(destPath);
                         }
                         driverNamesProgressBar.Value = 0;
                         isDownloadingDriverNames = false;
                         if (success && !isDownloadingSoundPack && !isDownloadingPersonalisations)
                         {
                             doRestart(Configuration.getUIString(willNeedAnotherDrivernamesDownload ? "the_application_must_be_restarted_to_load_the_new_sounds_need_another_restart" :
                                 "the_application_must_be_restarted_to_load_the_new_sounds"), Configuration.getUIString("load_new_sounds"));
                         }
                     }
                     if (!success)
                     {
                         driverNamesUpdateFailed(false);
                     }
                 });
                 extractDriverNamesThread.Name = "MainWindow.extractDriverNamesThread";
                 ThreadManager.RegisterResourceThread(extractDriverNamesThread);
                 extractDriverNamesThread.Start();
             }
             else
             {
                 driverNamesUpdateFailed(e.Cancelled);
             }
         }

        private void driverNamesUpdateFailed(Boolean cancelled)
        {
            if (!cancelled && !usingRetryAddressForDriverNames && SoundPackVersionsHelper.retryReplace != null && SoundPackVersionsHelper.retryReplaceWith != null)
            {
                Console.WriteLine("Unable to get driver names from " + SoundPackVersionsHelper.retryReplace + " will try from " + SoundPackVersionsHelper.retryReplaceWith);
                usingRetryAddressForDriverNames = true;
                drivernamesDownloadURL = SoundPackVersionsHelper.replaceUrlToRetry(drivernamesDownloadURL);
                startDownload(DownloadType.DRIVER_NAMES);
            }
            else
            {
                startApplicationButton.Enabled = !isDownloadingSoundPack && !isDownloadingPersonalisations;
                if (SoundPackVersionsHelper.currentDriverNamesVersion == -1)
                {
                    downloadDriverNamesButton.Text = Configuration.getUIString("no_driver_names_detected_press_to_download");
                }
                else
                {
                    downloadDriverNamesButton.Text = Configuration.getUIString("updated_driver_names_available_press_to_download");
                }
                if (!IsAppRunning)
                {
                    downloadDriverNamesButton.Enabled = true;
                }
                if (!cancelled)
                {
                    MessageBox.Show(Configuration.getUIString("error_downloading_driver_names"), Configuration.getUIString("unable_to_download_driver_names"),
                        MessageBoxButtons.OK);
                }
            }
        }

        private void downloadDriverNamesButtonPress(object sender, EventArgs e)
        {
            if (AudioPlayer.soundPackLanguage == null)
            {
                DialogResult dialogResult = MessageBox.Show(
                    Utilities.Strings.NewlinesInLongString(Configuration.getUIString("unknown_driver_names_language_text")),
                    Configuration.getUIString("unknown_driver_names_language_title"), MessageBoxButtons.YesNo);
                if (dialogResult == DialogResult.Yes)
                {
                    startApplicationButton.Enabled = false;
                    downloadDriverNamesButton.Text = Configuration.getUIString("downloading_driver_names");
                    downloadDriverNamesButton.Enabled = false;
                    startDownload(DownloadType.DRIVER_NAMES);
                }
                else if (dialogResult == DialogResult.No)
                {
                }
            }
            else
            {
                startApplicationButton.Enabled = false;
                downloadDriverNamesButton.Text = Configuration.getUIString("downloading_driver_names");
                downloadDriverNamesButton.Enabled = false;
                startDownload(DownloadType.DRIVER_NAMES);
            }
        }
