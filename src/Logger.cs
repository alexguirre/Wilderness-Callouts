namespace WildernessCallouts
{ 
    using Rage;
    using System;

    internal static class Logger
    {
        public static void LogTrivial(string text)
        {
            Game.LogTrivial("[Wilderness Callouts] " + text);
        }

        public static void LogTrivial(string specific, string text)
        {
            Game.LogTrivial("[Wilderness Callouts | " + specific + "] " + text);
        }


        public static void LogDebug(string text)
        {
            if (Settings.General.IsDebugBuild) Game.LogTrivial("[Wilderness Callouts]<DEBUG> " + text);
        }

        public static void LogDebug(string specific, string text)
        {
            if (Settings.General.IsDebugBuild) Game.LogTrivial("[Wilderness Callouts | " + specific + "]<DEBUG> " + text);
        }


        public static void LogException(Exception ex)
        {
            Game.LogTrivial("[Wilderness Callouts]<EXCEPTION> " + ex.Message + " :: " + ex.StackTrace);
        }

        public static void LogException(string specific, Exception ex)
        {
            Game.LogTrivial("[Wilderness Callouts | " + specific + "]<EXCEPTION> " + ex.Message + " :: " + ex.StackTrace);
        }

        public static void LogExceptionDebug(Exception ex)
        {
            if (Settings.General.IsDebugBuild) Game.LogTrivial("[Wilderness Callouts]<DEBUG | EXCEPTION> " + ex.Message + " :: " + ex.StackTrace);
        }

        public static void LogExceptionDebug(string specific, Exception ex)
        {
            if (Settings.General.IsDebugBuild) Game.LogTrivial("[Wilderness Callouts | " + specific + "]<DEBUG | EXCEPTION> " + ex.Message + " :: " + ex.StackTrace);
        }

        public static void LogWelcome()
        {
            if (Settings.CheckIniFile())
            {
                Game.Console.Print("=============================================== Wilderness Callouts ===============================================");
                Game.Console.Print("Created by:  alexguirre");
                Game.Console.Print("Version:  " + WildernessCallouts.Common.GetFileVersion(@"Plugins\LSPDFR\Wilderness Callouts.dll"));
                Game.Console.Print("RAGEPluginHook Version:  " + WildernessCallouts.Common.GetFileVersion("RAGEPluginHook.exe"));
                Game.Console.Print("RAGENativeUI Version:  " + WildernessCallouts.Common.GetFileVersion("RAGENativeUI.dll"));
                Game.Console.Print();
                Game.Console.Print("Report any issues you have in the Wilderness Callouts topic or in the comments section and include the RagePluginHook.log");
                Game.Console.Print("Enjoy!");
                Game.Console.Print("=============================================== Wilderness Callouts ===============================================");
            }
            else
            {
                Game.DisplayNotification("~r~[WARNING] ~b~Wilderness Callouts Config.ini ~w~can't be read. Using default settings");
                Game.Console.Print("=============================================== Wilderness Callouts ===============================================");
                Game.Console.Print("Created by:  alexguirre");
                Game.Console.Print("Version:  " + WildernessCallouts.Common.GetFileVersion(@"Plugins\LSPDFR\Wilderness Callouts.dll"));
                Game.Console.Print();
                Game.Console.Print("Wilderness Callouts Config.ini can't be read. Using default settings");
                Game.Console.Print();
                Game.Console.Print("Report any issues you have in the Wilderness Callouts topic or in the comments section and include the RagePluginHook.log");
                Game.Console.Print("Enjoy");
                Game.Console.Print("=============================================== Wilderness Callouts ===============================================");
            }
        }
    }
}
