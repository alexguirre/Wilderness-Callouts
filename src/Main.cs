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

                if(Settings.AmbientEvents.EnableAmbientEvents)
                    EventPool.EventsController();

                Globals.HeliCamera.ManagerFiber.Start();

                GameFiber.StartNew(delegate
                {
                    Logger.LogTrivial("Functions fiber started");

                    while (true)
                    {
                        GameFiber.Yield();

                        if (Controls.ToggleBinoculars.IsJustPressed() && !Game.LocalPlayer.Character.IsInAnyVehicle(false) && !Binoculars.IsActive && Settings.General.IsBinocularEnable)
                        {
                            Binoculars.EnableBinoculars();
                        }

                        if (Controls.ToggleInteractionMenu.IsJustPressed() && !Binoculars.IsActive)
                        {
                            InteractionMenu.DisEnable();
                        }
                    }
                });

                Game.DisplayNotification("~g~<font size=\"14\"><b>WILDERNESS CALLOUTS</b></font>~s~~n~Version: ~b~" + WildernessCallouts.Common.GetVersion(@"Plugins\LSPDFR\Wilderness Callouts.dll") + "~s~~n~Loaded!");
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
    }
}