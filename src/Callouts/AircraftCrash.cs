namespace WildernessCallouts.Callouts
{
    using Rage;
    using LSPD_First_Response;
    using LSPD_First_Response.Mod.API;
    using LSPD_First_Response.Mod.Callouts;
    using System.Drawing;
    using WildernessCallouts.Types;
    using System;

    [CalloutInfo("AircraftCrash", CalloutProbability.VeryLow)]
    internal class AircraftCrash : CalloutBase
    {
        private Rage.Object crash; // a rage vehicle
        private Ped dead; // a rage ped
        private Vector3 spawnPoint; // a Vector3
        private Blip heliBlip; // a rage blip

        private static string[] pilotModel = { "s_m_m_pilot_01", "s_m_m_pilot_02", "s_m_y_pilot_01" };

        private static string[] crashVehModel = { "prop_crashed_heli", "prop_shamal_crash", "apa_mp_apa_crashed_usaf_01a", "prop_wrecked_buzzard" };

        private bool explosionBool;

        //private bool breakForceEnd = false;

        /// <summary>
        /// OnBeforeCalloutDisplayed is where we create a blip for the user to see where the pursuit is happening, we initiliaize any variables above and set
        /// the callout message and position for the API to display
        /// </summary>
        /// <returns></returns>
        public override bool OnBeforeCalloutDisplayed()
        {
            //Set our spawn point to be on a street around 300f (distance) away from the player.
            spawnPoint = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.AroundPosition(800f));
          
            if (Vector3.Distance(Game.LocalPlayer.Character.Position, spawnPoint) < 115.0f) return false;


            //Create the vehicle for our ped
            crash = new Rage.Object(crashVehModel.GetRandomElement(), spawnPoint);

            //Create our ped in the world
            dead = new Ped(pilotModel.GetRandomElement(), crash.Position.AroundPosition(5.0f), 0f);
            if (dead.Exists()) dead.IsRagdoll = true;
            GameFiber.Wait(500);
            if (dead.Exists()) dead.Kill();

            //Now we have spawned them, check they actually exist and if not return false (preventing the callout from being accepted and aborting it)
            if (!dead.Exists()) return false;
            if (!crash.Exists()) return false;

            GameFiber.StartNew(delegate
            {
                GameFiber.Sleep(MathHelper.GetRandomInteger(500, 12501));
                if (crash.Exists())
                {
                    int rndFarExplosion = MathHelper.GetRandomInteger(101);
                    if (rndFarExplosion < 80) World.SpawnExplosion(crash.Position.AroundPosition(5.0f), 5, 10.0f, true, false, MathHelper.GetRandomSingle(0.0f, 4.0f));
                }
            });

            // Show the user where the pursuit is about to happen and block very close peds.
            this.ShowCalloutAreaBlipBeforeAccepting(spawnPoint, 25f);
            this.AddMinimumDistanceCheck(5f, dead.Position);

            // Set up our callout message and location
            this.CalloutMessage = "Aircraft crash";
            this.CalloutPosition = spawnPoint;

            Functions.PlayScannerAudioUsingPosition("ATTENTION_ALL_UNITS ASSISTANCE_REQUIRED CRIME_AIRCRAFT_CRASH IN_OR_ON_POSITION UNITS_RESPOND_CODE_99", spawnPoint);

            return base.OnBeforeCalloutDisplayed();
        }


        /// <summary>
        /// OnCalloutAccepted is where we begin our callout's logic. In this instance we create our pursuit and add our ped from eariler to the pursuit as well
        /// </summary>
        /// <returns></returns>
        public override bool OnCalloutAccepted()
        {
            //We accepted the callout, so lets initilize our blip from before and attach it to our ped so we know where he is.
            heliBlip = crash.AttachBlip();
            if (crash.Model == new Model("prop_crashed_heli") || crash.Model == new Model("prop_wrecked_buzzard")) heliBlip.Sprite = BlipSprite.Helicopter;
            else if (crash.Model == new Model("prop_shamal_crash") || crash.Model == new Model("apa_mp_apa_crashed_usaf_01a")) heliBlip.Sprite = BlipSprite.Plane;
            heliBlip.Color = Color.Yellow;
            heliBlip.EnableRoute(Color.Yellow);

            Functions.PlayScannerAudioUsingPosition("CRIME_AMBULANCE_REQUESTED IN_OR_ON_POSITION", spawnPoint);

            explosionBool = true;

            GameFiber.StartNew(delegate
            {
                GameFiber.Sleep(MathHelper.GetRandomInteger(500, 12501));
                if (crash.Exists())
                {
                    int rndFarExplosion = MathHelper.GetRandomInteger(101);
                    if (rndFarExplosion < 75) World.SpawnExplosion(crash.Position.AroundPosition(5.0f), 5, 10.0f, true, false, MathHelper.GetRandomSingle(0.0f, 4.0f));
                }
            });

            return base.OnCalloutAccepted();
        }

        /// <summary>
        /// If you don't accept the callout this will be called, we clear anything we spawned here to prevent it staying in the game
        /// </summary>
        public override void OnCalloutNotAccepted()
        {
            base.OnCalloutNotAccepted();
            if (dead.Exists()) dead.Delete();
            if (crash.Exists()) crash.Delete();
            if (heliBlip.Exists()) heliBlip.Delete();
        }

        //This is where it all happens, run all of your callouts logic here
        public override void Process()
        {
            base.Process();

            if (Vector3.Distance(Game.LocalPlayer.Character.Position, crash.Position) < 42.0f && explosionBool)
            {
                explosionBool = false;

                CreateExplosions();

                Functions.RequestBackup(crash.Position.AroundPosition(15.0f), EBackupResponseType.Code3, EBackupUnitType.Firetruck);
                Functions.RequestBackup(crash.Position.AroundPosition(15.0f), EBackupResponseType.Code3, EBackupUnitType.LocalUnit);
                Functions.RequestBackup(crash.Position.AroundPosition(15.0f), EBackupResponseType.Code3, EBackupUnitType.Ambulance);
                Functions.RequestBackup(crash.Position.AroundPosition(15.0f), EBackupResponseType.Code3, EBackupUnitType.Firetruck);
            }

            if (Vector3.Distance(Game.LocalPlayer.Character.Position, crash.Position) < 30.0f)
            {
               
                Game.DisplayHelp("Press ~b~" + Controls.PrimaryAction.ToUserFriendlyName() + "~w~ to tell dispatch you are leaving. Press ~r~" + Controls.SecondaryAction.ToUserFriendlyName() + "~w~ to call fire services");

                if (Controls.PrimaryAction.IsJustPressed())
                {
                    Game.DisplayNotification("~b~" + Settings.General.Name + ": ~w~Dispatch, the situation is under control, leaving the scene");
                    GameFiber.Wait(Globals.Random.Next(1250, 3001));
                    Game.DisplayNotification("~b~Dispatch: ~w~Roger");
                    this.End();
                }
                else if (Controls.SecondaryAction.IsJustPressed())
                {
                    GameFiber.StartNew(delegate
                    {
                        Game.DisplayNotification("~b~" + Settings.General.Name + ": ~w~Ambulance and firetruck needed");
                        GameFiber.Wait(Globals.Random.Next(1250, 2501));
                        Game.DisplayNotification("~b~Dispatch: ~w~Roger, ambulance and firetruck en route");

                        Functions.PlayScannerAudioUsingPosition("CRIME_AMBULANCE_REQUESTED IN_OR_ON_POSITION UNITS_RESPOND_CODE_99", spawnPoint);

                        Functions.RequestBackup(Game.LocalPlayer.Character.Position.AroundPosition(12.0f), EBackupResponseType.Code3, EBackupUnitType.Ambulance);
                        Functions.RequestBackup(Game.LocalPlayer.Character.Position.AroundPosition(12.0f), EBackupResponseType.Code3, EBackupUnitType.Firetruck);
                    });
                }
            }
        }

        /// <summary>
        /// More cleanup, when we call end you clean away anything left over
        /// This is also important as this will be called if a callout gets aborted (for example if you force a new callout)
        /// </summary>
        public override void End()
        {
            base.End();
            if (heliBlip.Exists()) heliBlip.Delete();
            if (dead.Exists()) dead.Dismiss();
            if (crash.Exists()) crash.Dismiss();
        }


        public void CreateExplosions()
        {
            GameFiber.StartNew(delegate
            {
                if (Globals.Random.Next(1, 101) <= 65)
                {
                    World.SpawnExplosion(crash.Position.AroundPosition(2.0f), 9, 5.0f, true, false, 2.0f);
                    World.SpawnExplosion(crash.Position.AroundPosition(2.8f), 9, 5.0f, true, false, 2.0f);
                    World.SpawnExplosion(crash.Position.AroundPosition(2.8f), 28, 5.0f, true, false, 2.0f);
                    World.SpawnExplosion(crash.Position.AroundPosition(3.2f), 36, 5.0f, true, false, 2.0f);
                    World.SpawnExplosion(crash.Position.AroundPosition(4.0f), 3, 5.0f, false, false, 2.0f);
                    GameFiber.Wait(Globals.Random.Next(250, 1001));
                }

                if (Globals.Random.Next(1, 101) <= 35)
                {
                    World.SpawnExplosion(crash.Position.AroundPosition(1.0f), 3, 5.0f, false, false, 2.0f);
                    World.SpawnExplosion(crash.Position.AroundPosition(2.8f), 9, 5.0f, true, false, 2.0f);
                    World.SpawnExplosion(crash.Position.AroundPosition(2.8f), 28, 5.0f, true, false, 2.0f);
                    World.SpawnExplosion(crash.Position.AroundPosition(6.0f), 3, 5.0f, false, false, 2.0f);
                    World.SpawnExplosion(crash.Position.AroundPosition(3.0f), 3, 5.0f, false, false, 2.0f);
                    GameFiber.Wait(Globals.Random.Next(250, 2001));
                }

                if (Globals.Random.Next(1, 101) <= 55)
                {
                    World.SpawnExplosion(crash.Position.AroundPosition(2.8f), 9, 5.0f, true, false, 2.0f);
                    World.SpawnExplosion(crash.Position.AroundPosition(2.8f), 28, 5.0f, true, false, 2.0f);
                    World.SpawnExplosion(crash.Position.AroundPosition(6.0f), 3, 5.0f, false, false, 2.0f);
                    World.SpawnExplosion(crash.Position.AroundPosition(3.0f), 3, 5.0f, false, false, 2.0f);
                    GameFiber.Wait(Globals.Random.Next(500, 2250));
                }

                if (Globals.Random.Next(1, 101) <= 55)
                {
                    World.SpawnExplosion(crash.Position.AroundPosition(2.8f), 9, 5.0f, true, false, 2.0f);
                    World.SpawnExplosion(crash.Position.AroundPosition(2.8f), 28, 5.0f, true, false, 2.0f);
                    World.SpawnExplosion(crash.Position.AroundPosition(6.0f), 3, 5.0f, false, false, 2.0f);
                    World.SpawnExplosion(crash.Position.AroundPosition(3.0f), 3, 5.0f, false, false, 2.0f);
                    GameFiber.Wait(Globals.Random.Next(500, 2250));
                }

                if (Globals.Random.Next(1, 101) <= 25)
                {
                    World.SpawnExplosion(crash.Position.AroundPosition(1.0f), 3, 5.0f, false, false, 2.0f);
                    World.SpawnExplosion(crash.Position.AroundPosition(2.8f), 9, 5.0f, true, false, 2.0f);
                    World.SpawnExplosion(crash.Position.AroundPosition(2.8f), 28, 5.0f, true, false, 2.0f);
                    World.SpawnExplosion(crash.Position.AroundPosition(6.0f), 3, 5.0f, false, false, 2.0f);
                    World.SpawnExplosion(crash.Position.AroundPosition(3.0f), 3, 5.0f, false, false, 2.0f);
                }

                if (Globals.Random.Next(1, 101) <= 15)
                {
                    World.SpawnExplosion(crash.Position.AroundPosition(1.0f), 3, 5.0f, false, false, 2.0f);
                    World.SpawnExplosion(crash.Position.AroundPosition(2.8f), 9, 5.0f, true, false, 2.0f);
                    World.SpawnExplosion(crash.Position.AroundPosition(2.8f), 28, 5.0f, true, false, 2.0f);
                    World.SpawnExplosion(crash.Position.AroundPosition(6.0f), 3, 5.0f, false, false, 2.0f);
                    World.SpawnExplosion(crash.Position.AroundPosition(2.8f), 9, 5.0f, true, false, 2.0f);
                    World.SpawnExplosion(crash.Position.AroundPosition(2.8f), 28, 5.0f, true, false, 2.0f);
                    World.SpawnExplosion(crash.Position.AroundPosition(6.0f), 3, 5.0f, false, false, 2.0f);
                    World.SpawnExplosion(crash.Position.AroundPosition(3.0f), 3, 5.0f, false, false, 2.0f);
                    GameFiber.Wait(Globals.Random.Next(250, 1001));
                }
            });
        }

        protected override void CleanUp()
        {
            // TODO: implement CleanUp()
        }
    }
}
