namespace WildernessCallouts
{
    using System;
    using System.IO;
    using System.Diagnostics;
    using System.Globalization;
    using Rage;
    using WildernessCallouts.Types;
    using WildernessCallouts.CalloutFunct;

    internal static class Globals
    {
        public static Random Random = new Random();

        public static HeliCamera HeliCamera = new HeliCamera();

        public static StaticFinalizer Finalizer = new StaticFinalizer(delegate
        {
            AmbientEvents.EventPool.EndCurrentEvent();
            HeliCamera.CleanUp();
        });

        //public static void CheckForUpdate()
        //{
        //    if(!Settings.AllowUpdatesCheck) return;

        //    GameFiber.StartNew(delegate
        //    {
        //        //Stopwatch sw = Stopwatch.StartNew();
        //        try
        //        {
        //            while (Game.IsLoading)
        //                GameFiber.Yield();

        //            Version fileVersion = CalloutHelper.GetVersion(@"Plugins\LSPDFR\Wilderness Callouts.dll"); 
        //            Version publicVersion = new Version(CalloutHelper.DownloadText("http://www.lcpdfr.com/applications/downloadsng/interface/api.php?do=checkForUpdates&fileId=8108&textOnly=1")); 

        //            int comparition = fileVersion.CompareTo(publicVersion); 

        //            if (comparition == 0 || comparition > 0)
        //            {
        //                Logger.LogTrivial("Current version is up to date"); 
        //            }
        //            else if (comparition < 0)
        //            {
        //                Logger.LogTrivial("Current version is outdated"); 
        //                Logger.LogTrivial("Current version is v" + fileVersion + ", newest version is v" + publicVersion);
        //                if (Settings.AllowUpdateRedirect)
        //                {
        //                    Logger.LogTrivial("Redirect allowed...");
        //                    Game.DisplayHelp("~g~Wilderness Callouts~s~ is ~r~outdated~s~. You will be redirected to the download page in 5 seconds", 5000);
        //                    GameFiber.Sleep(5000);
        //                    Process.Start("http://www.lcpdfr.com/files/file/8108-wilderness-callouts/");
        //                }
        //                else
        //                {
        //                    Game.DisplayHelp("~g~Wilderness Callouts~s~ is ~r~outdated~s~. Check the download page for the latest version", 5000);
        //                    Logger.LogTrivial("Redirect not allowed. Check http://www.lcpdfr.com/files/file/8108-wilderness-callouts/ for the update");
        //                }
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            Logger.LogTrivial("Exception handled while checking for updates");
        //            Logger.LogException(ex);
        //        }
        //        //sw.Stop();
        //        //Logger.LogTrivial("Ms: " + sw.ElapsedMilliseconds);
        //        //Logger.LogTrivial("Ticks: " + sw.ElapsedTicks);
        //        //Logger.LogTrivial("Time: " + sw.Elapsed);

        //    }, "Update Checker Fiber");
        //}

        /// <summary>
        /// Checks whether the person has the specified minimum version or higher. 
        /// </summary>
        /// <param name="minimumVersion">Provide in the format of a float i.e.: 0.22</param>
        /// <returns></returns>
        public static bool CheckRPHVersion(float minimumVersion)
        {
            bool correctVersion;

            var versionInfo = FileVersionInfo.GetVersionInfo("RAGEPluginHook.exe");
            float Rageversion;

            try
            {
                //If you decide to use this in your plugin, I would appreciate some credit :)
                Rageversion = Single.Parse(versionInfo.ProductVersion.Substring(0, 4), CultureInfo.InvariantCulture);
                Logger.LogTrivial("Detected RAGEPluginHook version: " + Rageversion);

                //If user's RPH version is older than the minimum
                if (Rageversion < minimumVersion)
                {
                    correctVersion = false;
                    GameFiber.StartNew(delegate
                    {
                        while (Game.IsLoading)
                        {
                            GameFiber.Yield();
                        }
                        //If you decide to use this in your plugin, I would appreciate some credit :)
                        Game.DisplayNotification("RPH ~r~v" + Rageversion + " ~s~detected. ~b~Version ~s~required is ~b~v" + minimumVersion + " ~s~or higher.");
                        GameFiber.Sleep(5000);
                        Logger.LogTrivial("RAGEPluginHook version " + Rageversion + " detected. Version required is v" + minimumVersion + " or higher.");
                        Logger.LogTrivial("Preparing redirect...");
                        Game.DisplayNotification("You are being redirected to the RagePluginHook website so you can download the latest version.");
                        Game.DisplayNotification("Press Backspace to cancel the redirect.");

                        int count = 0;
                        while (true)
                        {
                            GameFiber.Sleep(5);
                            count++;
                            if (Game.IsKeyDownRightNow(System.Windows.Forms.Keys.Back))
                            {
                                Game.DisplayNotification("Redirect cancelled");
                                break;
                            }
                            if (count >= 575)
                            {
                                //URL to the RPH download page.
                                System.Diagnostics.Process.Start("https://ragepluginhook.net/Downloads.aspx");
                                break;
                            }
                        }

                    });
                }
                //If user's RPH version is (above) the specified minimum
                else
                {
                    correctVersion = true;
                }
            }
            catch (Exception e)
            {
                //If for whatever reason the version couldn't be found.
                Logger.LogTrivial(e.ToString());
                Logger.LogTrivial("Unable to detect your Rage installation.");
                if (File.Exists("RAGEPluginHook.exe"))
                {
                    Logger.LogTrivial("RAGEPluginHook.exe exists");
                }
                else { Game.LogTrivial("RAGEPluginHook doesn't exist."); }
                Logger.LogTrivial("Rage Version: " + versionInfo.ProductVersion.ToString());
                Game.DisplayNotification("Wilderness Callouts by alexguirre unable to detect RPH installation. Please send me your logfile.");
                correctVersion = false;

            }

            return correctVersion;
        }


        //public static void CreateResourcesFolder()
        //{

        //}

        //public static bool CheckResourcesFolder()
        //{
        //    if (!Directory.Exists(@"Plugins\LSPDFR\Wilderness Callouts"))
        //        return false;

        //    if (!File.Exists(@"Plugins\LSPDFR\Wilderness Callouts\BinocularsTexture.png"))
        //        return false;

        //    //if (!File.Exists(@"Plugins\LSPDFR\Wilderness Callouts\RedWire.png"))
        //    //    return false;

        //    //if (!File.Exists(@"Plugins\LSPDFR\Wilderness Callouts\GreenWire.png"))
        //    //    return false;

        //    //if (!File.Exists(@"Plugins\LSPDFR\Wilderness Callouts\BlueWire.png"))
        //    //    return false;

        //    //if (!File.Exists(@"Plugins\LSPDFR\Wilderness Callouts\WhiteWire.png"))
        //    //    return false;

        //    return true;
        //}

        
    }
}
