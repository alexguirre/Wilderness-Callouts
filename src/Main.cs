using WildernessCallouts.Integrations;

namespace WildernessCallouts
{
    using System;
    using System.Text;
    using System.Reflection;

    // RPH
    using Rage;

    // LSPDFR
    using LSPD_First_Response.Mod.API;

    // WildernessCallouts
    using WildernessCallouts.CalloutFunct;
    using WildernessCallouts.Menus;
    using WildernessCallouts.AmbientEvents;

    internal class Main : Plugin
    {

        /// <summary>
        /// Whether or not Police Smart Radio is running.
        /// </summary>
        public bool PoliceSmartRadioAvailable = false;

        /// <summary>
        /// Access to the Police Smart Radio functions singleton instance.
        /// </summary>
        public PoliceSmartRadioFunctions PoliceSmartRadioFunctions;

        public override void Initialize()
        {
            Logger.LogWelcome();
            //Globals.CheckForUpdate();
            Globals.CheckRPHVersion(0.34f);

            MenuCommon.InitializeAllMenus();

            Functions.OnOnDutyStateChanged += Functions_OnOnDutyStateChanged;
            Game.FrameRender += Main.Process;
        }

        public void Functions_OnOnDutyStateChanged(bool onDuty)
        {
            if (onDuty)
            {
                Common.RegisterCallouts();

                if (Settings.AmbientEvents.EnableAmbientEvents)
                    EventPool.EventsController();

                Globals.HeliCamera.ManagerFiber.Start();

                // set up integration with PoliceSmartRadio
                if (IsLSPDFRPluginRunning("PoliceSmartRadio"))
                {
                    PoliceSmartRadioAvailable = true;
                    PoliceSmartRadioFunctions = new PoliceSmartRadioFunctions();
                }

                GameFiber.StartNew(delegate
                {
                    Logger.LogTrivial("Functions fiber started");

                    while (true)
                    {
                        GameFiber.Yield();

                        if (Controls.ToggleBinoculars.IsJustPressed() &&
                            !Game.LocalPlayer.Character.IsInAnyVehicle(false) && !Binoculars.IsActive &&
                            Settings.General.IsBinocularEnable)
                        {
                            Binoculars.EnableBinoculars();
                        }

                        if (Controls.ToggleInteractionMenu.IsJustPressed() && !Binoculars.IsActive)
                        {
                            InteractionMenu.DisEnable();
                        }
                    }
                });

                Game.DisplayNotification("~g~<font size=\"14\"><b>WILDERNESS CALLOUTS</b></font>~s~~n~Version: ~b~" +
                                         WildernessCallouts.Common.GetVersion(
                                             @"Plugins\LSPDFR\Wilderness Callouts.dll") + "~s~~n~Loaded!");
            }
        }


        public override void Finally()
        {
        }


        public static void Process(object sender, GraphicsEventArgs e)
        {
            MenuCommon.Pool.ProcessMenus();

            if (InteractionMenu.MainMenu.Visible)
            {
                foreach (Ped staticPeds in InteractionMenu.StaticPeds)
                {
                    if (staticPeds.Exists())
                    {
                        staticPeds.Tasks.AchieveHeading(staticPeds.GetHeadingTowards(Game.LocalPlayer.Character));
                    }
                }
            }
        }


        /// <summary>
        /// Determine if the given assembly name is running inside LSPDFR.
        /// </summary>
        /// <param name="Plugin"></param>
        /// <returns></returns>
        public static bool IsLSPDFRPluginRunning(string Plugin)
        {
            try
            {
                foreach (Assembly assembly in Functions.GetAllUserPlugins())
                {
                    if (string.Equals(assembly.GetName().Name, Plugin, StringComparison.CurrentCultureIgnoreCase))
                    {
                        return true;
                    }
                }

                return false;
            }
            catch (Exception e)
            {
                Game.LogTrivial($"{e}");
                return false;
            }
        }
    }
}