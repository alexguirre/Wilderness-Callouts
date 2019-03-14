namespace WildernessCallouts.Callouts
{
    using Rage;
    using Rage.Native;
    using LSPD_First_Response.Mod.Callouts;
    using System.Drawing;
    using System.Collections.Generic;
    using WildernessCallouts.Peds;
    using WildernessCallouts.Types;
    
    [CalloutInfo("RocksBlockingRoad", CalloutProbability.Low)]
    internal class RocksBlockingRoad : CalloutBase
    {
        private static Model[] rockModel = { "prop_rock_4_c", "prop_rock_4_d", 0x2be77a5b, 0x7f86218f, 0xdbb36e2d, 0x819cba01, 0xe63d798c, 0xb837a736, 0x6d6a919d, 0xbd4bd936 };
        //private Rage.Object rock;
        private List<Rage.Object> rocksList = new List<Rage.Object>();
        private Vector3 spawnPoint; // a Vector3
        private Blip rockBlip; // a rage blip
        

        private bool breakForceEnd = false;

        private ERocksBlockingRoadState state;

        /// <summary>
        /// OnBeforeCalloutDisplayed is where we create a blip for the user to see where the pursuit is happening, we initiliaize any variables above and set
        /// the callout message and position for the API to display
        /// </summary>
        /// <returns></returns>
        public override bool OnBeforeCalloutDisplayed()
        {
            //Set our spawn point to be on a street around 300f (distance) away from the player.
            spawnPoint = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.AroundPosition(400f));

            EWorldArea spawnZone = WorldZone.GetArea(spawnPoint);
            if (spawnZone == EWorldArea.Los_Santos) return false;

            if (Vector3.Distance(Game.LocalPlayer.Character.Position, spawnPoint) < 30.0f) return false;

            int a = Globals.Random.Next(12, 30);
            rockModel.Shuffle();
            for (int i = 1; i < a; i++)
            {
                Vector3 spawnPos = spawnPoint.AroundPosition(8.5f);
                Vector3 spawnPos2 = spawnPos + new Vector3(MathHelper.GetRandomSingle(0.0f, 8.0f), MathHelper.GetRandomSingle(0.0f, 8.0f), 325.0f);

                Rage.Object rock = new Rage.Object(rockModel.GetRandomElement(), spawnPos2.ToGroundUsingRaycasting(Game.LocalPlayer.Character));
                //Game.LogTrivial("1 : ~b~" + rock.Position.Z.ToString());
                rock.Heading = MathHelper.GetRandomSingle(0.0f, 360.0f);
                //rock.SetPositionZ(funct.GetGroundZForVector3(rock.Position) + 0.125f);
                NativeFunction.Natives.SET_ACTIVATE_OBJECT_PHYSICS_AS_SOON_AS_IT_IS_UNFROZEN(rock, true);
                rocksList.Add(rock);
            }
            foreach (Rage.Object rocks in rocksList)
            {
                if (rocks.Exists())
                {
                    float z = rocks.Position.GetGroundZ();
                    //Game.LogTrivial("2 : ~r~" + rocks.Position.Z.ToString());
                    if (rocks.Exists()) rocks.SetPositionZ(z);
                }
            }
            foreach (Rage.Object rocks in rocksList)
            {
                if (!rocks.Exists()) return false;
                if (rocks.Exists() && rocks.Position.Z < 1.25f) return false;
            }
            //Now we have spawned them, check they actually exist and if not return false (preventing the callout from being accepted and aborting it)
            //if (!rock.Exists()) return false;

            // Show the user where the pursuit is about to happen and block very close peds.
            this.ShowCalloutAreaBlipBeforeAccepting(spawnPoint, 35f);
            this.AddMinimumDistanceCheck(20f, spawnPoint);

            // Set up our callout message and location
            this.CalloutMessage = "Rocks blocking the road";
            this.CalloutPosition = spawnPoint;

            LSPD_First_Response.Mod.API.Functions.PlayScannerAudioUsingPosition("WE_HAVE ROAD_BLOCKED IN_OR_ON_POSITION UNITS_RESPOND_CODE_02", spawnPoint);

            return base.OnBeforeCalloutDisplayed();
        }


        /// <summary>
        /// OnCalloutAccepted is where we begin our callout's logic. In this instance we create our pursuit and add our ped from eariler to the pursuit as well
        /// </summary>
        /// <returns></returns>
        public override bool OnCalloutAccepted()
        {
            //We accepted the callout, so lets initilize our blip from before and attach it to our ped so we know where he is.
            rockBlip = new Blip(spawnPoint);
            rockBlip.Color = Color.Yellow;
            rockBlip.EnableRoute(Color.Yellow);

            GameFiber.StartNew(delegate
            {
                state = ERocksBlockingRoadState.EnRoute;

                while (true)
                {
                    if (breakForceEnd) break;

                    switch (state)
                    {
                        case ERocksBlockingRoadState.EnRoute:
                            //Game.DisplayNotification("State: EnRoute");
                            while (true)
                            {
                                GameFiber.Yield();
                                if (Vector3.Distance(Game.LocalPlayer.Character.Position, spawnPoint) < 30.0f)
                                {
                                    state = ERocksBlockingRoadState.OnScene;
                                    break;
                                }
                                if (breakForceEnd) break;
                            }
                            break;


                        case ERocksBlockingRoadState.OnScene:
                            //Game.DisplayNotification("State: OnScene");
                            while (true)
                            {
                                GameFiber.Yield();
                                Game.DisplayHelp("Press ~b~" + Controls.PrimaryAction.ToUserFriendlyName() + "~w~ to call a truck");
                                if (Controls.PrimaryAction.IsJustPressed())
                                {
                                    state = ERocksBlockingRoadState.CallTruck;
                                    break;
                                }
                                if (breakForceEnd) break;
                            }
                            break;


                        case ERocksBlockingRoadState.CallTruck:
                            //Game.DisplayNotification("State: CallTruck");
                            Vector3 spawnPos = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.AroundPosition(150f));
                            while (spawnPos.DistanceTo(Game.LocalPlayer.Character.Position) < 50.0f)
                            {
                                spawnPos = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.AroundPosition(150.0f));
                                GameFiber.Yield();
                            }
                            Trucker trucker = new Trucker("s_m_m_trucker_01", spawnPos, 0.0f);
                            Vector3 posToDrive = spawnPoint.AroundPosition(12.5f);
                            Vector3 posToWalk = spawnPoint.AroundPosition(3.0f);
                            trucker.Job(posToDrive, posToWalk);

                            while (true)
                            {
                                if (breakForceEnd) break;

                                if (Vector3.Distance(posToWalk, trucker.Position) < 4.0f && trucker.Speed <= 0.5f)
                                {
                                    GameFiber.Wait(4500);

                                    foreach (Rage.Object rockObj in rocksList)
                                    {
                                        if (rockObj.Exists() && !breakForceEnd) rockObj.Delete();
                                    }

                                    break;
                                }
                                GameFiber.Yield();
                            }

                            this.End();
                            break;


                        default:
                            break;
                    }

                    GameFiber.Yield();
                }
                    
            });

            return base.OnCalloutAccepted();
        }

        /// <summary>
        /// If you don't accept the callout this will be called, we clear anything we spawned here to prevent it staying in the game
        /// </summary>
        public override void OnCalloutNotAccepted()
        {
            breakForceEnd = true;

            base.OnCalloutNotAccepted();
            if (rockBlip.Exists()) rockBlip.Delete();
            foreach (Rage.Object rockObj in rocksList)
            {
                if (rockObj.Exists()) rockObj.Dismiss();
            }
        }

        //This is where it all happens, run all of your callouts logic here
        public override void Process()
        {
            base.Process();
        }

        /// <summary>
        /// More cleanup, when we call end you clean away anything left over
        /// This is also important as this will be called if a callout gets aborted (for example if you force a new callout)
        /// </summary>
        public override void End()
        {
            breakForceEnd = true;

            base.End();
            if (rockBlip.Exists()) rockBlip.Delete();
            foreach (Rage.Object rockObj in rocksList)
            {
                if (rockObj.Exists()) rockObj.Dismiss();
            }
        }

        protected override void CleanUp()
        {
            // TODO: implement CleanUp()
        }

        public enum ERocksBlockingRoadState
        {
            EnRoute,
            OnScene,
            CallTruck,
        }
    }
}
