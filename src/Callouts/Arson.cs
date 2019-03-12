namespace WildernessCallouts.Callouts
{
    using Rage;
    using Rage.Native;
    using LSPD_First_Response.Mod.API;
    using LSPD_First_Response.Mod.Callouts;
    using System.Drawing;
    using System.Collections.Generic;
    using WildernessCallouts.Types;
    using WildernessCallouts.Peds;

    [CalloutInfo("Arson", CalloutProbability.Medium)]
    internal class Arson : CalloutBase
    {
        Ped suspect = null;

        Vehicle possibleVeh = null;

        static string[] vehModels = { "asea"    , "asterope", "banshee"  , "baller"     , "baller2"     , "coquette"    , "buffalo"   , "buffalo2" , "buccaneer", "buccaneer2", "bison"    , "faction"   ,
                                      "faction2", "moonbeam", "moonbeam2", "primo2"     , "voodoo"      , "carbonizzare", "dilettante", "cogcabrio", "panto"    , "prairie"   , "emperor"  , "ztype"     ,
                                      "adder"   , "zentorno", "akuma"    , "baller"     , "banshee"     , "bati"        , "bjxl"      , "buccaneer", "buffalo"  , "cheetah"   , "cogcabrio", "dominator" ,
                                      "entityxf", "gauntlet", "granger"  , "hotknife"   , "infernus"    , "ztype"       , "casco"     , "lectro"   , "guardian" , "kuruma"    , "kuruma2"  , "chino"     ,
                                      "dukes"   , "stalion" , "furoregt" , "slamvan2"   , "enduro"      , "blista"      , "asea"      , "daemon"   , "peyote"   , "primo"     , "sandking" , "sandking2" ,
                                      "bagger"  , "blazer"  , "blazer3"  , "bobcatxl"   , "buccaneer2"  , "chino2"      , "faction"   , "faction2" , "moonbeam" , "moonbeam2" , "primo2"   , "voodoo"    ,
                                      "panto"   , "prairie" , "emperor"  , "exemplar"   , "ztype"       , "lurcher"     , "rhapsody"  , "pigalle"  , "warrener" , "blade"     , "glendale" , "huntley"   ,
                                      "massacro", "thrust"  , "alpha"    , "jester"     , "turismor"    , "bifta"       , "kalahari"  , "paradise" , "furoregt" , "innovation", "coquette2", "feltzer3"  ,
                                      "osiris"  , "windsor" , "brawler"  , "chino"      , "coquette3"   , "vindicator"  , "t20"       , "btype2"   , "rocoto"   , "comet2"    , "bullet"   , "rebel"     ,
                                      "rebel2"  , "vigero"  , "youga"    , "zion"       , "zion2"       , "washington"  , "voltic"    , "virgo"    , "tampa"    , "baller3"   , "baller4"  , "baller5"   ,
                                      "baller6" , "cog55"   , "cog552"   , "cognoscenti", "cognoscenti2", "mamba"       , "nightshade", "schafter3", "schafter4", "schafter5" , "schafter6", "verlierer2",
                                      "f620"    , "elegy2"  , "surano"   , "sultanrs"   , "banshee2"    , "faction3"    , "minivan2"  , "sabregt2" , "slamvan3" , "tornado5"  , "virgo2"   , "virgo3"
                                    };

        static string[] weapons = { "WEAPON_MOLOTOV", "WEAPON_PETROLCAN" };

        Blip fireBlip = null;
        Blip chiefBlip = null;
        Blip suspectBlip = null;
        Blip searchAreaBlip = null;

        ArsonSpawn arsonSpawnUsed = null;

        HeliPilot airSupport = null;

        LHandle pursuit = null;
        bool isPursuitInitiated = false;

        EArsonState state;
        bool isFireCreated = false;
        bool areFiremanWorking = false;
        bool isFleeingInVeh = false;

        string weaponUsed;

        float searchBlipRadius = 75.0f;

        public override bool OnBeforeCalloutDisplayed()
        {
            arsonSpawnUsed = ArsonSpawns.GetRandomElement(true);
            for (int i = 0; i < 20; i++)
            {
                Logger.LogTrivial(this.GetType().Name, "Get spawn attempt #" + i);
                if (arsonSpawnUsed.FirePosition.DistanceTo(Game.LocalPlayer.Character) < 1500f &&
                    arsonSpawnUsed.FirePosition.DistanceTo(Game.LocalPlayer.Character) > 40f)
                    break;
                arsonSpawnUsed = ArsonSpawns.GetRandomElement(true);
            }
            if (arsonSpawnUsed.FirePosition.DistanceTo(Game.LocalPlayer.Character) > 1500f)
            {
                Logger.LogTrivial(this.GetType().Name, "Aborting: Spawn too far");
                return false;
            }
            else if (arsonSpawnUsed.FirePosition.DistanceTo(Game.LocalPlayer.Character) < 40f)
            {
                Logger.LogTrivial(this.GetType().Name, "Aborting: Spawn too close");
                return false;
            }
            //for (int i = 0; i < 15; i++)
            //{
            //    arsonSpawnUsed = ArsonSpawns.GetRandomElement(true);
            //    if (Vector3.Distance(Game.LocalPlayer.Character.Position, arsonSpawnUsed.FirePosition) < 1250.0f && Vector3.Distance(Game.LocalPlayer.Character.Position, arsonSpawnUsed.FirePosition) > 57.5f) break;
            //}
            //if (Vector3.Distance(Game.LocalPlayer.Character.Position, arsonSpawnUsed.FirePosition) > 1250.0f || Vector3.Distance(Game.LocalPlayer.Character.Position, arsonSpawnUsed.FirePosition) < 57.5f)
            //    return false;

            arsonSpawnUsed.Create(false);
            suspect = new Ped(World.GetNextPositionOnStreet(arsonSpawnUsed.FirePosition.AroundPosition(150.0f)).ToGround());

            if (!suspect.Exists()) return false;

            if (Globals.Random.Next(101) < 40 && arsonSpawnUsed.CanSpawnCar)
            {
                string vModel = vehModels.GetRandomElement(true);
                for (int i = 0; i < 5; i++)
                {
                    if (new Model(vModel).IsValid)
                        break;
                    else
                    {
                        Logger.LogTrivial(this.GetType().Name, "Vehicle Model < " + vModel + " > is invalid. Choosing new model...");
                        vModel = vehModels.GetRandomElement(false);
                    }
                }
                if (!new Model(vModel).IsValid)
                {
                    Logger.LogTrivial(this.GetType().Name, "Aborting: Final Vehicle Model < " + vModel + " > is invalid");
                    return false;
                }
                possibleVeh = new Vehicle(vModel, arsonSpawnUsed.FirePosition, MathHelper.GetRandomSingle(1.0f, 358.0f));

                if (!possibleVeh.Exists()) return false;

                if (Globals.Random.Next(6) <= 2) possibleVeh.InstallRandomMods();

                for (int i = 0; i < Globals.Random.Next(0, 35); i++)
                    possibleVeh.Deform(new Vector3(MathHelper.GetRandomSingle(-1.5f, 1.5f), MathHelper.GetRandomSingle(-1.5f, 1.5f), MathHelper.GetRandomSingle(-1.5f, 1.5f)), 6.75f, 2175.0f);
            }
            else if (Globals.Random.Next(101) < 65)
            {
                isFleeingInVeh = true;
                Vector3 vehSpawnPos = World.GetNextPositionOnStreet(suspect.Position.AroundPosition(20.0f));
                suspect.Position = Vector3.Zero;
                possibleVeh = new Vehicle(vehModels.GetRandomElement(true), vehSpawnPos, vehSpawnPos.GetClosestVehicleNodeHeading());
                if (Globals.Random.Next(6) <= 4) possibleVeh.InstallRandomMods();
                suspect.WarpIntoVehicle(possibleVeh, -1);
                VehicleDrivingFlags drivingStyle;
                switch (Globals.Random.Next(0, 5))
                {
                    case 1:
                        drivingStyle = (VehicleDrivingFlags)20;
                        break;
                    case 2:
                        drivingStyle = (VehicleDrivingFlags)786468;
                        break;
                    case 3:
                        drivingStyle = (VehicleDrivingFlags)786468;
                        break;
                    default:
                        drivingStyle = (VehicleDrivingFlags)20;
                        break;
                }
                NativeFunction.CallByName<uint>("SET_DRIVER_ABILITY", suspect, MathHelper.GetRandomSingle(0.0f, 100.0f));
                if (possibleVeh.Model.IsBike || possibleVeh.Model.IsBicycle) suspect.GiveHelmet(false, HelmetTypes.RegularMotorcycleHelmet, -1);
                suspect.Tasks.CruiseWithVehicle(possibleVeh, 200.0f, drivingStyle);
            }

            foreach (Ped firefighter in arsonSpawnUsed.Firefighters)
                if (firefighter == null || !firefighter.Exists()) return false;

            foreach (Vehicle firetruk in arsonSpawnUsed.Firetruks)
                if (firetruk == null || !firetruk.Exists()) return false;

            weaponUsed = weapons.GetRandomElement();

            this.ShowCalloutAreaBlipBeforeAccepting(arsonSpawnUsed.FirePosition, 45.0f);
            this.AddMinimumDistanceCheck(35.0f, arsonSpawnUsed.FirePosition);

            // Set up our callout message and location
            this.CalloutMessage = "Arson";
            this.CalloutPosition = arsonSpawnUsed.FirePosition;

            //Play the police scanner audio for this callout (available as of the 0.2a API)
            Functions.PlayScannerAudioUsingPosition("CITIZENS_REPORT CRIME_ARSON IN_OR_ON_POSITION UNITS_RESPOND_CODE_03", arsonSpawnUsed.FirePosition);

            return base.OnBeforeCalloutDisplayed();
        }


        /// <summary>
        /// OnCalloutAccepted is where we begin our callout's logic. In this instance we create our pursuit and add our ped from eariler to the pursuit as well
        /// </summary>
        /// <returns></returns>
        public override bool OnCalloutAccepted()
        {
            state = EArsonState.EnRoute;
            suspect.Inventory.GiveNewWeapon(weaponUsed, 1, true);
            fireBlip = new Blip(arsonSpawnUsed.Firefighters[0].Position);
            fireBlip.Color = Color.Yellow;
            fireBlip.EnableRoute(Color.Yellow);

            if (!isFleeingInVeh) suspect.ReactAndFlee(arsonSpawnUsed.Firefighters[0]);
            return base.OnCalloutAccepted();
        }

        /// <summary>
        /// If you don't accept the callout this will be called, we clear anything we spawned here to prevent it staying in the game
        /// </summary>
        public override void OnCalloutNotAccepted()
        {
            if (suspect.Exists()) suspect.Delete();
            if (possibleVeh.Exists()) possibleVeh.Delete();
            if (fireBlip.Exists()) fireBlip.Delete();
            if (chiefBlip.Exists()) chiefBlip.Delete();
            if (suspectBlip.Exists()) suspectBlip.Delete();
            if (searchAreaBlip.Exists()) searchAreaBlip.Delete();
            if (airSupport.Exists()) airSupport.CleanUpHeliPilot();
            if (isPursuitInitiated) Functions.ForceEndPursuit(pursuit);
            arsonSpawnUsed.Delete();

            base.OnCalloutNotAccepted();
        }

        //This is where it all happens, run all of your callouts logic here
        public override void Process()
        {
            base.Process();

            if (!isFireCreated && Vector3.Distance(Game.LocalPlayer.Character.Position, arsonSpawnUsed.FirePosition) < 80.0f)
            {
                arsonSpawnUsed.CreateFire();
                isFireCreated = true;
            }
            if (!areFiremanWorking && Vector3.Distance(Game.LocalPlayer.Character.Position, arsonSpawnUsed.FirePosition) < 65.0f)
            {
                foreach (Ped p in arsonSpawnUsed.Firefighters)
                {
                    if (p != arsonSpawnUsed.Firefighters[0])
                    {
                        GameFiber.StartNew(delegate
                        {
                            Vector3 pos = arsonSpawnUsed.Fires[Globals.Random.Next(arsonSpawnUsed.Fires.Count)].Position.AroundPosition(5.0f).ToGroundUsingRaycasting(p);
                            p.Tasks.FollowNavigationMeshToPosition(pos, pos.GetHeadingTowards(arsonSpawnUsed.FirePosition), 20.0f).WaitForCompletion();
                            NativeFunction.CallByName<uint>("TASK_SHOOT_AT_COORD", p, pos.X, pos.Y, pos.Z, -1, (uint)Rage.FiringPattern.FullAutomatic);
                        });
                        GameFiber.Sleep(500);
                    }
                }
                areFiremanWorking = true;
            }

            if (suspect.Position.Z <= 2.5f) suspect.Position = suspect.Position.ToGround();

            if (state == EArsonState.EnRoute && Vector3.Distance(Game.LocalPlayer.Character.Position, arsonSpawnUsed.Firefighters[0].Position) < 10.0f)
            {
                state = EArsonState.OnScene;
                GameFiber.StartNew(delegate
                {
                    if (fireBlip.Exists()) fireBlip.Delete();

                    string sex = suspect.IsMale ? "male" : "female";
                    arsonSpawnUsed.Firefighters[0].PlayAmbientSpeech(OfficerNeedsTransport.maleVoices.GetRandomElement(), Globals.Random.Next(2) == 1 ? Globals.Random.Next(2) == 1 ? Speech.KIFFLOM_GREET : Speech.GENERIC_HI : Speech.GENERIC_HOWS_IT_GOING, 0, SpeechModifier.ForceFrontend);
                    Game.DisplaySubtitle("~b~Fire Chief:~s~ Hello officer", 2000);
                    arsonSpawnUsed.Firefighters[0].Tasks.AchieveHeading(arsonSpawnUsed.Firefighters[0].GetHeadingTowards(suspect)).WaitForCompletion(2250);
                    arsonSpawnUsed.Firefighters[0].Tasks.PlayAnimation("gestures@m@standing@casual", "gesture_point", -1, 4.0f, 0.375f, 0.0f, AnimationFlags.UpperBodyOnly);
                    if (isFleeingInVeh)
                    {
                        searchBlipRadius = 200.0f;
                        Game.DisplaySubtitle("~b~Fire Chief:~s~ We saw a car fleeing in that direction", 2500);
                        GameFiber.Sleep(2500);
                        string plate = possibleVeh.LicensePlate.Substring(0, Globals.Random.Next(2, 5));
                        Game.DisplaySubtitle("~b~Fire Chief:~s~ I think the model was a ~b~" + possibleVeh.Model.Name + "~s~, and the plate started with ~b~" + plate + "~s~", 4500);
                        bool sayColor = Globals.Random.Next(0, 2) == 1;
                        if (sayColor)
                        {
                            GameFiber.Sleep(3750);
                            Game.DisplaySubtitle("~b~Fire Chief:~s~ And the color was ~b~" + possibleVeh.GetPrimaryColor().ToFriendlyName(), 4750);
                        }

                        GameFiber.Sleep(750);
                        GameFiber.StartNew(delegate
                        {
                            Vector3 position = Game.LocalPlayer.Character.Position;
                            Game.DisplayNotification("~b~" + Settings.General.Name + ":~s~ Requesting air support over ~b~" + position.GetZoneName());
                            if (sayColor)
                                Game.DisplayNotification("~b~" + Settings.General.Name + ":~s~ Searching a ~b~" + possibleVeh.Model.Name + "~s~, color ~b~" + possibleVeh.GetPrimaryColor().ToFriendlyName() + "~s~, plate #~b~" + plate);
                            else
                                Game.DisplayNotification("~b~" + Settings.General.Name + ":~s~ Searching a ~b~" + possibleVeh.Model.Name + "~s~, plate #~b~" + plate);
                            Game.DisplayNotification("~b~Dispatch: ~w~Air unit deployed");

                            GameFiber.Wait(Globals.Random.Next(1250, 2750));
                            Game.DisplayNotification("~b~" + Settings.General.Name + ": ~w~10-4");
                        });
                    }
                    else
                    {
                        Game.DisplaySubtitle("~b~Fire Chief:~s~ When we arrived, we saw a ~b~" + sex + "~s~ running in that direction", 4500);
                        GameFiber.Sleep(2500);
                        Game.DisplaySubtitle("~b~Fire Chief:~s~ I think " + (suspect.IsMale ? "he's" : "she's") + " carrying a ~b~" + (weaponUsed == "WEAPON_MOLOTOV" ? "molotov" : "petrol can"), 4500);
                    }
                    GameFiber.Sleep(4500);
                    Game.DisplaySubtitle("~b~Fire Chief:~s~ We'll take care of this", 2000);


                    if (suspect.Exists()) searchAreaBlip = new Blip(suspect.Position.AroundPosition(50.0f), searchBlipRadius);
                    searchAreaBlip.Color = Color.FromArgb(100, Color.Yellow);

                    if (!isFleeingInVeh && suspect.Exists()) suspect.ReactAndFlee(arsonSpawnUsed.Firefighters[0]);
                    state = EArsonState.Searching;
                    UpdateSearchArea();
                });
            }
            if (state == EArsonState.Searching)
            {
                if (Vector3.Distance(Game.LocalPlayer.Character.Position, suspect.Position) < 17.25f && suspect.Exists())
                {
                    if (!isFleeingInVeh)
                    {
                        suspect.Tasks.Clear();
                        if (Globals.Random.Next(4) <= 2 && weaponUsed == "WEAPON_MOLOTOV")
                        {
                            suspect.Tasks.AchieveHeading(suspect.GetHeadingTowards(Game.LocalPlayer.Character)).WaitForCompletion(1500);
                            NativeFunction.CallByName<uint>("TASK_THROW_PROJECTILE", suspect, Game.LocalPlayer.Character.Position.X, Game.LocalPlayer.Character.Position.Y, Game.LocalPlayer.Character.Position.Z);
                        }
                        else suspect.ReactAndFlee(Game.LocalPlayer.Character);
                    }
                    Game.DisplayNotification("~b~" + Settings.General.Name + ":~s~ Dispatch, arson suspect located, approaching");
                    state = EArsonState.Found;

                    if (searchAreaBlip.Exists()) searchAreaBlip.Delete();
                    GameFiber.Sleep(1750);
                    if (!isFleeingInVeh)
                    {
                        suspect.ReactAndFlee(Game.LocalPlayer.Character);
                        suspectBlip = new Blip(suspect);
                    }
                    else
                    {
                        pursuit = Functions.CreatePursuit();
                        isPursuitInitiated = true;
                        Functions.AddPedToPursuit(pursuit, suspect);
                    }
                }
            }

            if (!suspect.Exists() || suspect.IsDead || Functions.IsPedArrested(suspect)) this.End();
        }

        /// <summary>
        /// More cleanup, when we call end you clean away anything left over
        /// This is also important as this will be called if a callout gets aborted (for example if you force a new callout)
        /// </summary>
        public override void End()
        {
            state = EArsonState.End;
            if (suspect.Exists()) suspect.Dismiss();
            if (possibleVeh.Exists()) possibleVeh.Dismiss();
            if (fireBlip.Exists()) fireBlip.Delete();
            if (chiefBlip.Exists()) chiefBlip.Delete();
            if (suspectBlip.Exists()) suspectBlip.Delete();
            if (searchAreaBlip.Exists()) searchAreaBlip.Delete();
            if (airSupport.Exists()) airSupport.CleanUpHeliPilot();
            if (isPursuitInitiated) Functions.ForceEndPursuit(pursuit);
            arsonSpawnUsed.Dismiss();
            base.End();
        }

        public void UpdateSearchArea()
        {
            Logger.LogTrivial(this.GetType().Name, "UpdateSearchArea()");

            GameFiber.StartNew(delegate
            {
                if (!isFleeingInVeh)
                {
                    while (state == EArsonState.Searching)
                    {
                        GameFiber.Sleep(3500);
                        if ((suspect.Exists() && searchAreaBlip.Exists()))
                            if (Vector3.Distance2D(suspect.Position, searchAreaBlip.Position) > searchBlipRadius - 5.0f)
                                if (searchAreaBlip.Exists()) searchAreaBlip.Position = suspect.Position.AroundPosition(searchBlipRadius - 100.0f);

                        GameFiber.Yield();
                    }
                }
                else
                {
                    airSupport = new HeliPilot(Vector3.Zero, 0.0f);
                    airSupport.JobFollow(suspect);

                    while (state == EArsonState.Searching)
                    {
                        GameFiber.Wait(15000);
                        if (searchAreaBlip.Exists())
                        {
                            if (Vector3.Distance2D(suspect.Position, searchAreaBlip.Position) > searchBlipRadius - 5.0f)
                            {

                                Functions.PlayScannerAudio("REQUEST_GUIDANCE_DISPATCH");

                                Logger.LogTrivial(this.GetType().Name, "SearchArea Updated");

                                GameFiber.Wait(4900);
                                if (searchAreaBlip.Exists())
                                {
                                    //ExtendedPosition extPos = WorldZone.GetExtendedPosition(suspect.Position);

                                    Vector3 position = suspect.Position;

                                    WildernessCallouts.Common.PlayHeliAudioWithEntityCardinalDirection("OFFICER_INTRO", suspect);

                                    if (searchAreaBlip.Exists()) searchAreaBlip.Position = suspect.Position;

                                    if (suspect.IsInAnyVehicle(false))
                                    {
                                        if (searchAreaBlip.Exists()) Game.DisplayNotification("~b~Air support: ~w~Suspect vehicle model is ~b~" + suspect.CurrentVehicle.Model.Name + "~w~, color ~b~" + suspect.CurrentVehicle.GetPrimaryColor().ToFriendlyName().ToLower());
                                        if (searchAreaBlip.Exists()) Game.DisplayNotification("~b~Air support: ~w~Near ~b~" + position.GetStreetName() + "~w~, in ~b~" + position.GetZoneName());
                                    }
                                    else
                                    {
                                        if (searchAreaBlip.Exists()) Game.DisplayNotification("~b~Air support: ~w~Suspect is ~b~on foot");
                                        if (searchAreaBlip.Exists()) Game.DisplayNotification("~b~Air support: ~w~Near ~b~" + position.GetStreetName() + "~w~, in ~b~" + position.GetZoneName());
                                    }
                                }
                            }
                        }

                        GameFiber.Yield();
                    }
                }
            });
        }

        protected override void CleanUp()
        {
            // TODO: implement CleanUp()
        }


        public static List<ArsonSpawn> ArsonSpawns = new List<ArsonSpawn>()        // ADD MORE SPAWNS
        {
#region Arson Spawns List
            new ArsonSpawn
                (
                    new Vector3(532.18f, 1232.5f, 294.4f),
                    new List<SpawnPoint>()
                    {
                        new SpawnPoint(new Vector3(500.68f, 1237.19f, 286.1f), 266.95f),
                    },
                    new List<SpawnPoint>()
                    {
                        new SpawnPoint(new Vector3(510.4f, 1232.02f, 288.53f), 64.45f),
                        new SpawnPoint(new Vector3(522.73f, 1221.49f, 292.35f), 335.85f),
                        new SpawnPoint(new Vector3(519.26f, 1229.27f, 290.93f), 44.12f),
                    },
                    14
                ),


            new ArsonSpawn
                (
                    new Vector3(-549.2f, 1999.72f, 202.68f),
                    new List<SpawnPoint>()
                    {
                        new SpawnPoint(new Vector3(-527.2f, 1988.37f, 205.94f), 148.28f),
                    },
                    new List<SpawnPoint>()
                    {
                        new SpawnPoint(new Vector3(-515.4f, 1996.5f, 205.98f), 266.06f),
                        new SpawnPoint(new Vector3(-535.68f, 1980.47f, 205.74f), 52.72f),
                        new SpawnPoint(new Vector3(-526.58f, 2014.67f, 204.02f), 138.58f),
                        new SpawnPoint(new Vector3(-521.65f, 2011.52f, 204.46f), 7.36f),
                    },
                    13
                ),


            new ArsonSpawn
                (
                    new Vector3(-196.70f, 1274.74f, 304.5f),
                    new List<SpawnPoint>()
                    {
                        new SpawnPoint(new Vector3(-192.04f, 1313.16f, 303.63f), 317.48f),
                    },
                    new List<SpawnPoint>()
                    {
                        new SpawnPoint(new Vector3(-198.36f, 1303.52f, 304.42f), 25.68f),
                        new SpawnPoint(new Vector3(-195.99f, 1292.74f, 305.0f), 182.93f),
                        new SpawnPoint(new Vector3(-177.7f, 1293.93f, 303.52f), 196.04f),
                        new SpawnPoint(new Vector3(-165.46f, 1277.77f, 301.76f), 113.1f),
                    },
                    14
                ),


            new ArsonSpawn
                (
                    new Vector3(-1298.99f, 2179.42f, 51.09f),
                    new List<SpawnPoint>()
                    {
                        new SpawnPoint(new Vector3(-1267.28f, 2174.0f, 60.02f), 108.66f),
                    },
                    new List<SpawnPoint>()
                    {
                        new SpawnPoint(new Vector3(-1279.03f, 2169.57f, 58.38f), 214.04f),
                        new SpawnPoint(new Vector3(-1282.93f, 2173.0f, 57.29f), 62.58f),
                        new SpawnPoint(new Vector3(-1310.97f, 2166.13f, 55.12f), 340.63f),
                    },
                    14
                ),


            new ArsonSpawn
                (
                    new Vector3(2028.41f, -868.89f, 85.07f),
                    new List<SpawnPoint>()
                    {
                        new SpawnPoint(new Vector3(2048.93f, -892.83f, 79.11f), 285.6f),
                    },
                    new List<SpawnPoint>()
                    {
                        new SpawnPoint(new Vector3(2029.72f, -897.71f, 79.14f), 64.3f),
                        new SpawnPoint(new Vector3(2051.14f, -877.97f, 79.02f), 66.22f),
                        new SpawnPoint(new Vector3(2015.66f, -890.49f, 79.04f), 319.64f),
                        new SpawnPoint(new Vector3(2042.62f, -841.69f, 94.9f), 142.47f),
                    },
                    13
                ),


            new ArsonSpawn
                (
                    new Vector3(2228.01f, -791.59f, 69.47f),
                    new List<SpawnPoint>()
                    {
                        new SpawnPoint(new Vector3(2218.35f, -734.91f, 66.52f), 126.66f),
                    },
                    new List<SpawnPoint>()
                    {
                        new SpawnPoint(new Vector3(2216.46f, -755.39f, 67.14f), 50.98f),
                        new SpawnPoint(new Vector3(2199.57f, -782.19f, 69.68f), 252.44f),
                        new SpawnPoint(new Vector3(2224.05f, -761.76f, 67.43f), 205.65f),
                        new SpawnPoint(new Vector3(2214.05f, -769.44f, 68.14f), 203.65f),
                    },
                    14
                ),


            new ArsonSpawn
                (
                    new Vector3(1149.87f, 1348.02f, 155.83f),
                    new List<SpawnPoint>()
                    {
                        new SpawnPoint(new Vector3(1169.81f, 1374.65f, 153.32f), 27.27f),
                    },
                    new List<SpawnPoint>()
                    {
                        new SpawnPoint(new Vector3(1173.73f, 1366.64f, 151.87f), 170.84f),
                        new SpawnPoint(new Vector3(1156.18f, 1369.37f, 153.6f), 165.6f),
                        new SpawnPoint(new Vector3(1178.87f, 1340.17f, 148.33f), 82.52f),
                    },
                    13
                ),


            new ArsonSpawn
                (
                    new Vector3(925.92f, 1695.01f, 164.66f),
                    new List<SpawnPoint>()
                    {
                        new SpawnPoint(new Vector3(905.18f, 1725.14f, 167.28f), 107.58f),
                    },
                    new List<SpawnPoint>()
                    {
                        new SpawnPoint(new Vector3(914.81f, 1725.88f, 166.86f), 246.1f),
                        new SpawnPoint(new Vector3(902.17f, 1713.57f, 167.59f), 228.83f),
                        new SpawnPoint(new Vector3(914.28f, 1716.42f, 166.98f), 201.23f),
                        new SpawnPoint(new Vector3(932.98f, 1720.78f, 166.11f), 153.54f),
                    },
                    15
                ),


            new ArsonSpawn
                (
                    new Vector3(1249.61f, 1765.78f, 81.4f),
                    new List<SpawnPoint>()
                    {
                        new SpawnPoint(new Vector3(1221.01f, 1756.79f, 78.73f), 42.5f),
                    },
                    new List<SpawnPoint>()
                    {
                        new SpawnPoint(new Vector3(1225.56f, 1748.42f, 79.31f), 205.95f),
                        new SpawnPoint(new Vector3(1235.04f, 1741.48f, 79.9f), 323.79f),
                        new SpawnPoint(new Vector3(1232.9f, 1744.27f, 79.67f), 312.26f),
                        new SpawnPoint(new Vector3(1236.88f, 1761.83f, 79.65f), 285.02f),
                    },
                    16
                ),


            new ArsonSpawn
                (
                    new Vector3(1217.42f, 1723.78f, 81.68f),
                    new List<SpawnPoint>()
                    {
                        new SpawnPoint(new Vector3(1198.13f, 1764.37f, 77.52f), 220.15f),
                    },
                    new List<SpawnPoint>()
                    {
                        new SpawnPoint(new Vector3(1201.37f, 1765.97f, 77.61f), 20.83f),
                        new SpawnPoint(new Vector3(1207.16f, 1757.94f, 78.16f), 220.49f),
                        new SpawnPoint(new Vector3(1209.58f, 1748.65f, 78.65f), 213.03f),
                        new SpawnPoint(new Vector3(1198.99f, 1735.32f, 86.24f), 227.71f),
                    },
                    16
                ),


            new ArsonSpawn
                (
                    new Vector3(1500.85f, 1841.57f, 107.07f),
                    new List<SpawnPoint>()
                    {
                        new SpawnPoint(new Vector3(1494.93f, 1807.47f, 108.14f), 12.6f),
                    },
                    new List<SpawnPoint>()
                    {
                        new SpawnPoint(new Vector3(1490.73f, 1812.78f, 107.97f), 34.56f),
                        new SpawnPoint(new Vector3(1496.22f, 1813.04f, 108.11f), 320.87f),
                        new SpawnPoint(new Vector3(1499.74f, 1812.14f, 108.14f), 3.66f),
                        new SpawnPoint(new Vector3(1504.96f, 1813.35f, 108.09f), 16.93f),
                    },
                    15
                ),


            new ArsonSpawn
                (
                    new Vector3(1855.55f, 2119.73f, 54.95f),
                    new List<SpawnPoint>()
                    {
                        new SpawnPoint(new Vector3(1872.38f, 2142.74f, 54.54f), 0.7f),
                        new SpawnPoint(new Vector3(1883.85f, 2149.89f, 54.56f), 0.45f),
                    },
                    new List<SpawnPoint>()
                    {
                        new SpawnPoint(new Vector3(1874.44f, 2133.16f, 64.56f), 198.29f),
                        new SpawnPoint(new Vector3(1871.68f, 2134.67f, 54.49f), 124.0f),
                        new SpawnPoint(new Vector3(1869.34f, 2135.28f, 54.54f), 238.84f),
                        new SpawnPoint(new Vector3(1852.62f, 2145.29f, 55.28f), 174.88f),
                        new SpawnPoint(new Vector3(1847.06f, 2144.31f, 55.55f), 192.9f),
                        new SpawnPoint(new Vector3(1865.6f, 2145.63f, 54.69f), 158.04f),
                    },
                    17
                ),


            new ArsonSpawn
                (
                    new Vector3(1941.43f, 3692.75f, 32.17f),
                    new List<SpawnPoint>()
                    {
                        new SpawnPoint(new Vector3(1911.62f, 3688.75f, 32.78f), 297.86f),
                        new SpawnPoint(new Vector3(1926.63f, 3710.85f, 32.48f), 115.13f),
                    },
                    new List<SpawnPoint>()
                    {
                        new SpawnPoint(new Vector3(1920.1f, 3694.37f, 32.68f), 330.76f),
                        new SpawnPoint(new Vector3(1920.07f, 3691.54f, 32.64f), 265.34f),
                        new SpawnPoint(new Vector3(1928.88f, 3709.07f, 32.49f), 218.87f),
                        new SpawnPoint(new Vector3(1942.31f, 3707.74f, 32.34f), 200.11f),
                    },
                    16
                ),


            new ArsonSpawn
                (
                    new Vector3(1987.37f, 3707.58f, 32.38f),
                    new List<SpawnPoint>()
                    {
                        new SpawnPoint(new Vector3(1987.62f, 3732.39f, 32.35f), 296.72f),
                    },
                    new List<SpawnPoint>()
                    {
                        new SpawnPoint(new Vector3(1986.42f, 3734.74f, 32.45f), 64.96f),
                        new SpawnPoint(new Vector3(1992.8f, 3731.47f, 32.39f), 195.07f),
                        new SpawnPoint(new Vector3(1995.03f, 3731.28f, 32.26f), 172.62f),
                    },
                    15
                ),


            new ArsonSpawn
                (
                    new Vector3(1780.13f, 3333.7f, 41.18f),
                    new List<SpawnPoint>()
                    {
                        new SpawnPoint(new Vector3(1764.47f, 3378.5f, 39.38f), 208.78f),
                        new SpawnPoint(new Vector3(1786.4f, 3361.49f, 40.32f), 208.85f),
                    },
                    new List<SpawnPoint>()
                    {
                        new SpawnPoint(new Vector3(1784.83f, 3359.55f, 40.36f), 170.6f),
                        new SpawnPoint(new Vector3(1778.79f, 3365.24f, 40.09f), 172.45f),
                        new SpawnPoint(new Vector3(1770.19f, 3364.6f, 39.81f), 201.54f),
                        new SpawnPoint(new Vector3(1796.55f, 3344.96f, 41.02f), 147.0f),
                    },
                    15
                ),


            new ArsonSpawn
                (
                    new Vector3(-2092.24f, -320.1f, 13.03f),
                    new List<SpawnPoint>()
                    {
                        new SpawnPoint(new Vector3(-2102.91f, -350.02f, 12.83f), 3.79f),
                        new SpawnPoint(new Vector3(-2083.49f, -365.58f, 12.2f), 60.05f),
                        new SpawnPoint(new Vector3(-2127.16f, -293.47f, 13.3f), 79.85f),
                    },
                    new List<SpawnPoint>()
                    {
                        new SpawnPoint(new Vector3(-2094.76f, -353.06f, 12.71f), 139.75f),
                        new SpawnPoint(new Vector3(-2104.48f, -340.89f, 13.0f), 341.25f),
                        new SpawnPoint(new Vector3(-2090.7f, -340.4f, 13.04f), 25.13f),
                        new SpawnPoint(new Vector3(-2115.84f, -299.45f, 13.03f), 249.47f),
                    },
                    14
                ),


            new ArsonSpawn
                (
                    new Vector3(-23.63f, -229.56f, 46.18f),
                    new List<SpawnPoint>()
                    {
                        new SpawnPoint(new Vector3(-39.59f, -243.15f, 45.76f), 65.82f),
                    },
                    new List<SpawnPoint>()
                    {
                        new SpawnPoint(new Vector3(-33.02f, -243.37f, 46.09f), 275.54f),
                        new SpawnPoint(new Vector3(-24.49f, -246.66f, 46.35f), 3.85f),
                        new SpawnPoint(new Vector3(-26.68f, -246.65f, 46.23f), 345.97f),
                    },
                    16
                ),


            new ArsonSpawn
                (
                    new Vector3(625.46f, 259.02f, 103.05f),
                    new List<SpawnPoint>()
                    {
                        new SpawnPoint(new Vector3(607.05f, 224.9f, 102.14f), 258.41f),
                        new SpawnPoint(new Vector3(623.47f, 309.89f, 107.08f), 299.89f),
                    },
                    new List<SpawnPoint>()
                    {
                        new SpawnPoint(new Vector3(595.62f, 228.3f, 102.8f), 37.63f),
                        new SpawnPoint(new Vector3(626.15f, 301.43f, 105.17f), 184.51f),
                        new SpawnPoint(new Vector3(615.21f, 296.07f, 104.03f), 197.0f),
                        new SpawnPoint(new Vector3(616.04f, 245.94f, 102.51f), 9.96f),
                    },
                    16
                ),


            new ArsonSpawn
                (
                    new Vector3(279.14f, -1263.49f, 29.2f),
                    new List<SpawnPoint>()
                    {
                        new SpawnPoint(new Vector3(283.28f, -1291.08f, 29.53f), 84.64f),
                        new SpawnPoint(new Vector3(250.32f, -1226.54f, 29.38f), 273.68f),
                    },
                    new List<SpawnPoint>()
                    {
                        new SpawnPoint(new Vector3(274.18f, -1289.68f, 29.28f), 143.99f),
                        new SpawnPoint(new Vector3(251.02f, -1230.74f, 29.26f), 194.89f),
                        new SpawnPoint(new Vector3(267.98f, -1238.2f, 29.17f), 183.29f),
                        new SpawnPoint(new Vector3(274.6f, -1281.4f, 29.2f), 44.23f),
                    },
                    17
                ),


            new ArsonSpawn
                (
                    new Vector3(269.33f, -1248.77f, 29.15f),
                    new List<SpawnPoint>()
                    {
                        new SpawnPoint(new Vector3(283.28f, -1291.08f, 29.53f), 84.64f),
                        new SpawnPoint(new Vector3(238.37f, -1252.06f, 29.16f), 350.07f),
                    },
                    new List<SpawnPoint>()
                    {
                        new SpawnPoint(new Vector3(236.57f, -1264.05f, 29.27f), 140.44f),
                        new SpawnPoint(new Vector3(243.38f, -1249.83f, 29.27f), 256.48f),
                        new SpawnPoint(new Vector3(243.79f, -1254.13f, 29.47f), 258.19f),
                        new SpawnPoint(new Vector3(252.47f, -1280.33f, 29.35f), 330.55f),
                    },
                    16
                ),


            new ArsonSpawn
                (
                    new Vector3(-5.36f, -1483.35f, 30.16f),
                    new List<SpawnPoint>()
                    {
                        new SpawnPoint(new Vector3(22.78f, -1466.51f, 30.4f), 47.76f),
                    },
                    new List<SpawnPoint>()
                    {
                        new SpawnPoint(new Vector3(18.34f, -1464.04f, 30.49f), 97.76f),
                        new SpawnPoint(new Vector3(10.32f, -1449.4f, 30.4f), 5.42f),
                        new SpawnPoint(new Vector3(-0.54f, -1467.02f, 30.4f), 196.49f),
                    },
                    16
                ),


            new ArsonSpawn
                (
                    new Vector3(-662.17f, 5268.64f, 75.86f),
                    new List<SpawnPoint>()
                    {
                        new SpawnPoint(new Vector3(-655.21f, 5235.61f, 76.79f), 241.56f),
                    },
                    new List<SpawnPoint>()
                    {
                        new SpawnPoint(new Vector3(-663.17f, 5242.58f, 76.7f), 20.46f),
                        new SpawnPoint(new Vector3(-661.5f, 5246.19f, 76.33f), 99.97f),
                        new SpawnPoint(new Vector3(-673.17f, 5252.03f, 76.5f), 340.5f),
                    },
                    20
                ),


            new ArsonSpawn
                (
                    new Vector3(-51.64f, -1482.39f, 31.86f),
                    new List<SpawnPoint>()
                    {
                        new SpawnPoint(new Vector3(-53.09f, -1459.92f, 31.9f), 94.55f),
                    },
                    new List<SpawnPoint>()
                    {
                        new SpawnPoint(new Vector3(-53.67f, -1462.32f, 32.02f), 198.33f),
                        new SpawnPoint(new Vector3(-48.68f, -1465.93f, 32.06f), 274.83f),
                        new SpawnPoint(new Vector3(-63.96f, -1474.31f, 31.97f), 104.92f),
                    },
                    12
                ),


            new ArsonSpawn
                (
                    new Vector3(-1104.71f, -3428.17f, 13.95f),
                    new List<SpawnPoint>()
                    {
                        new SpawnPoint(new Vector3(-1076.84f, -3430.75f, 13.95f), 55.83f),
                        new SpawnPoint(new Vector3(-1100.45f, -3417.07f, 13.95f), 55.83f),
                        new SpawnPoint(new Vector3(-1102.3f, -3400.82f, 13.95f), 235.38f),
                    },
                    new List<SpawnPoint>()
                    {
                        new SpawnPoint(new Vector3(-1098.72f, -3414.28f, 13.95f), 327.19f),
                        new SpawnPoint(new Vector3(-1106.77f, -3402.47f, 13.95f), 163.72f),
                        new SpawnPoint(new Vector3(-1118.01f, -3406.67f, 13.95f), 182.7f),
                        new SpawnPoint(new Vector3(-1120.37f, -3406.92f, 13.95f), 225.56f),
                        new SpawnPoint(new Vector3(-1080.78f, -3433.35f, 13.95f), 81.58f),
                    },
                    12
                ),


            new ArsonSpawn
                (
                    new Vector3(-981.3f, -2860.74f, 13.95f),
                    new List<SpawnPoint>()
                    {
                        new SpawnPoint(new Vector3(-1015.0f, -2872.93f, 13.96f), 152.24f),
                        new SpawnPoint(new Vector3(-1009.46f, -2892.91f, 13.96f), 330.39f),
                    },
                    new List<SpawnPoint>()
                    {
                        new SpawnPoint(new Vector3(-1016.72f, -2880.94f, 13.96f), 237.14f),
                        new SpawnPoint(new Vector3(-1003.67f, -2888.43f, 13.96f), 316.62f),
                        new SpawnPoint(new Vector3(-999.73f, -2891.55f, 13.97f), 333.59f),
                        new SpawnPoint(new Vector3(-1006.93f, -2867.58f, 13.96f), 287.61f),
                    },
                    19,
                    false
                ),


            new ArsonSpawn
                (
                    new Vector3(-1312.16f, -2670.44f, 13.94f),
                    new List<SpawnPoint>()
                    {
                        new SpawnPoint(new Vector3(-1338.61f, -2686.15f, 13.94f), 294.58f),
                        new SpawnPoint(new Vector3(-1306.15f, -2706.54f, 13.94f), 20.22f),
                    },
                    new List<SpawnPoint>()
                    {
                        new SpawnPoint(new Vector3(-1336.82f, -2690.69f, 13.94f), 215.24f),
                        new SpawnPoint(new Vector3(-1309.33f, -2704.33f, 13.94f), 22.26f),
                        new SpawnPoint(new Vector3(-1302.2f, -2697.7f, 13.94f), 34.44f),
                        new SpawnPoint(new Vector3(-1328.61f, -2692.08f, 13.94f), 315.85f),
                    },
                    18,
                    false
                ),


            new ArsonSpawn
                (
                    new Vector3(-1391.93f, -2599.03f, 13.94f),
                    new List<SpawnPoint>()
                    {
                        new SpawnPoint(new Vector3(-1378.84f, -2579.37f, 13.94f), 169.41f),
                        new SpawnPoint(new Vector3(-1390.93f, -2632.04f, 13.94f), 21.28f),
                    },
                    new List<SpawnPoint>()
                    {
                        new SpawnPoint(new Vector3(-1382.84f, -2581.6f, 13.94f), 125.99f),
                        new SpawnPoint(new Vector3(-1387.53f, -2629.33f, 13.94f), 11.66f),
                        new SpawnPoint(new Vector3(-1368.41f, -2609.78f, 13.94f), 75.24f),
                        new SpawnPoint(new Vector3(-1367.09f, -2605.66f, 13.94f), 93.4f),
                    },
                    19,
                    false
                ),


            new ArsonSpawn
                (
                    new Vector3(285.95f, -2059.11f, 18.43f),
                    new List<SpawnPoint>()
                    {
                        new SpawnPoint(new Vector3(266.11f, -2070.14f, 17.2f), 199.99f),
                        new SpawnPoint(new Vector3(257.03f, -2083.98f, 16.99f), 45.49f),
                    },
                    new List<SpawnPoint>()
                    {
                        new SpawnPoint(new Vector3(268.01f, -2068.35f, 17.1f), 293.72f),
                        new SpawnPoint(new Vector3(269.72f, -2071.75f, 17.04f), 303.05f),
                        new SpawnPoint(new Vector3(284.56f, -2074.44f, 17.24f), 14.25f),
                        new SpawnPoint(new Vector3(260.77f, -2082.85f, 17.02f), 293.89f),
                    },
                    17
                ),


            new ArsonSpawn
                (
                    new Vector3(-1369.97f, -1625.89f, 3.73f),
                    new List<SpawnPoint>()
                    {
                        new SpawnPoint(new Vector3(-1326.03f, -1611.28f, 4.23f), 50.44f),
                    },
                    new List<SpawnPoint>()
                    {
                        new SpawnPoint(new Vector3(-1329.56f, -1614.6f, 3.75f), 131.41f),
                        new SpawnPoint(new Vector3(-1359.58f, -1641.84f, 2.15f), 31.69f),
                        new SpawnPoint(new Vector3(-1353.27f, -1621.83f, 2.54f), 93.97f),
                    },
                    14,
                    false
                ),


            new ArsonSpawn
                (
                    new Vector3(-1382.25f, -1324.18f, 4.15f),
                    new List<SpawnPoint>()
                    {
                        new SpawnPoint(new Vector3(-1364.22f, -1313.59f, 4.49f), 32.96f),
                    },
                    new List<SpawnPoint>()
                    {
                        new SpawnPoint(new Vector3(-1360.87f, -1312.17f, 4.48f), 279.48f),
                        new SpawnPoint(new Vector3(-1363.67f, -1322.26f, 4.48f), 112.82f),
                        new SpawnPoint(new Vector3(-1375.23f, -1308.22f, 4.42f), 164.41f),
                    },
                    19,
                    false
                ),


            new ArsonSpawn
                (
                    new Vector3(-1437.42f, -969.64f, 7.35f),
                    new List<SpawnPoint>()
                    {
                        new SpawnPoint(new Vector3(-1449.2f, -951.73f, 7.39f), 140.69f),
                    },
                    new List<SpawnPoint>()
                    {
                        new SpawnPoint(new Vector3(-1445.5f, -949.2f, 7.77f), 306.49f),
                        new SpawnPoint(new Vector3(-1444.86f, -954.37f, 7.62f), 209.28f),
                        new SpawnPoint(new Vector3(-1456.16f, -965.91f, 7.26f), 253.8f),
                    },
                    19
                ),


            new ArsonSpawn
                (
                    new Vector3(-1695.6f, -1092.5f, 13.15f),
                    new List<SpawnPoint>()
                    {
                        new SpawnPoint(new Vector3(-1713.38f, -1080.86f, 13.04f), 138.54f),
                    },
                    new List<SpawnPoint>()
                    {
                        new SpawnPoint(new Vector3(-1711.58f, -1082.34f, 13.08f), 235.5f),
                        new SpawnPoint(new Vector3(-1706.61f, -1077.45f, 13.1f), 292.207f),
                        new SpawnPoint(new Vector3(-1716.23f, -1089.04f, 13.03f), 254.52f),
                    },
                    20,
                    false
                ),


            new ArsonSpawn
                (
                    new Vector3(339.51f, -964.39f, 29.43f),
                    new List<SpawnPoint>()
                    {
                        new SpawnPoint(new Vector3(355.85f, -950.64f, 39.4f), 266.4f),
                    },
                    new List<SpawnPoint>()
                    {
                        new SpawnPoint(new Vector3(360.54f, -953.64f, 29.48f), 160.69f),
                        new SpawnPoint(new Vector3(343.96f, -953.2f, 29.47f), 201.17f),
                        new SpawnPoint(new Vector3(335.54f, -952.91f, 29.46f), 220.61f),
                    },
                    15,
                    false
                ),


            new ArsonSpawn
                (
                    new Vector3(823.2f, -1028.98f, 26.26f),
                    new List<SpawnPoint>()
                    {
                        new SpawnPoint(new Vector3(792.89f, -1032.59f, 26.31f), 2.86f),
                    },
                    new List<SpawnPoint>()
                    {
                        new SpawnPoint(new Vector3(793.14f, -1037.76f, 26.5f), 182.07f),
                        new SpawnPoint(new Vector3(800.46f, -1046.61f, 26.83f), 306.03f),
                        new SpawnPoint(new Vector3(821.98f, -1012.66f, 26.22f), 359.65f),
                    },
                    20
                ),


            new ArsonSpawn
                (
                    new Vector3(-316.57f, -1469.45f, 31.55f),
                    new List<SpawnPoint>()
                    {
                        new SpawnPoint(new Vector3(-334.64f, -1441.56f, 30.22f), 266.33f),
                        new SpawnPoint(new Vector3(-291.8f, -1480.22f, 30.81f), 171.77f),
                    },
                    new List<SpawnPoint>()
                    {
                        new SpawnPoint(new Vector3(-296.75f, -1472.27f, 30.97f), 269.6f),
                        new SpawnPoint(new Vector3(-304.04f, -1492.99f, 30.27f), 55.18f),
                        new SpawnPoint(new Vector3(-315.12f, -1450.09f, 31.14f), 167.64f),
                        new SpawnPoint(new Vector3(-330.17f, -1453.34f, 30.63f), 199.31f),
                    },
                    20
                ),


            new ArsonSpawn
               (
                   new Vector3(-728.2169f, -936.1355f, 19.01702f),
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-749.2657f, -935.8545f, 18.26438f), 341.3737f),
                       new SpawnPoint(new Vector3(-759.0987f, -932.6418f, 18.25347f), 160.2447f),
                       new SpawnPoint(new Vector3(-724.8357f, -965.3347f, 18.17868f), 274.89f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-713.6979f, -963.2858f, 18.49054f), 2.284344f),
                       new SpawnPoint(new Vector3(-705.5796f, -948.6405f, 19.24186f), 52.25158f),
                       new SpawnPoint(new Vector3(-735.3229f, -949.7592f, 18.1689f), 311.6387f),
                       new SpawnPoint(new Vector3(-737.6582f, -924.7081f, 19.17891f), 254.5183f),
                       new SpawnPoint(new Vector3(-708.5635f, -919.4989f, 19.01392f), 136.8717f),
                   },
                   19
               ),


            new ArsonSpawn
               (
                   new Vector3(-1436.726f, -276.5552f, 46.2077f),
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-1450.194f, -293.3411f, 45.76786f), 42.27089f),
                       new SpawnPoint(new Vector3(-1450.312f, -308.0727f, 45.11091f), 41.06584f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-1438.75f, -296.4979f, 45.0025f), 146.6936f),
                       new SpawnPoint(new Vector3(-1430.39f, -296.6044f, 45.55345f), 343.3855f),
                       new SpawnPoint(new Vector3(-1416.272f, -282.9913f, 46.26612f), 75.62012f),
                       new SpawnPoint(new Vector3(-1449.128f, -265.165f, 46.63708f), 215.1089f),
                   },
                   20
               ),


            new ArsonSpawn
               (
                   new Vector3(-1488.607f, -381.1567f, 40.16338f),
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-1502.17f, -407.056f, 38.80791f), 227.3083f),
                       new SpawnPoint(new Vector3(-1479.18f, -407.2066f, 37.17943f), 39.29257f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-1482.102f, -396.5864f, 38.58415f), 105.3781f),
                       new SpawnPoint(new Vector3(-1493.482f, -388.8444f, 39.67479f), 334.5997f),
                       new SpawnPoint(new Vector3(-1498.234f, -383.3151f, 40.39012f), 243.2216f),
                       new SpawnPoint(new Vector3(-1499.798f, -387.917f, 40.16207f), 280.5219f),
                   },
                   18,
                  false
               ),


            new ArsonSpawn
               (
                   new Vector3(1203.424f, -1396.63f, 35.22508f),
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(1228.342f, -1414.266f, 35.10818f), 144.5253f),
                       new SpawnPoint(new Vector3(1186.882f, -1421.818f, 34.87754f), 90.77014f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(1234.495f, -1407.584f, 35.03514f), 359.6976f),
                       new SpawnPoint(new Vector3(1225.273f, -1400.535f, 35.1097f), 91.66822f),
                       new SpawnPoint(new Vector3(1223.733f, -1389.7f, 35.01497f), 116.0265f),
                       new SpawnPoint(new Vector3(1201.8f, -1416.725f, 35.15482f), 339.7795f),
                   },
                   20
               ),


            new ArsonSpawn
               (
                   new Vector3(1209.506f, -1402.83f, 35.22414f),
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(1224.734f, -1381.903f, 35.17538f), 189.3092f),
                       new SpawnPoint(new Vector3(1258.234f, -1409.617f, 34.97136f), 21.02189f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(1218.658f, -1386.188f, 35.14248f), 199.4711f),
                       new SpawnPoint(new Vector3(1221.458f, -1403.341f, 35.17001f), 55.77022f),
                       new SpawnPoint(new Vector3(1197.056f, -1418.292f, 35.17506f), 342.378f),
                   },
                   19
               ),


            new ArsonSpawn
               (
                   new Vector3(382.248f, 789.0779f, 187.671f),
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(368.854f, 769.4368f, 183.5535f), 116.9665f),
                       new SpawnPoint(new Vector3(403.2426f, 785.0169f, 187.8362f), 321.3976f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(397.4505f, 778.2451f, 186.8495f), 63.56684f),
                       new SpawnPoint(new Vector3(374.3843f, 774.9604f, 184.6743f), 345.8222f),
                       new SpawnPoint(new Vector3(365.5587f, 786.8194f, 186.6112f), 275.7468f),
                   },
                   20
               ),


            new ArsonSpawn
               (
                   new Vector3(1388.806f, 1139.787f, 114.3361f),
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(1362.974f, 1138.129f, 113.759f), 282.6012f),
                       new SpawnPoint(new Vector3(1328.027f, 1132.405f, 109.8614f), 177.7479f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(1348.69f, 1142.823f, 113.7348f), 67.89338f),
                       new SpawnPoint(new Vector3(1367.412f, 1156.05f, 113.759f), 289.6882f),
                       new SpawnPoint(new Vector3(1374.447f, 1131.9f, 114.1089f), 320.6121f),
                   },
                   18,
                   false
               ),
            new ArsonSpawn
               (
                   new Vector3(264.3999f, 2603.534f, 44.84949f),
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(242.3941f, 2621.608f, 45.85473f), 273.4288f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(239.4637f, 2616.172f, 45.92242f), 147.7829f),
                       new SpawnPoint(new Vector3(240.7184f, 2604.735f, 45.14594f), 272.1329f),
                       new SpawnPoint(new Vector3(254.5821f, 2617.822f, 45.19829f), 217.3099f),
                   },
                   16
               ),


            new ArsonSpawn
               (
                   new Vector3(49.1908f, 2776.138f, 57.88401f),
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(38.53823f, 2771.351f, 58.01965f), 55.63673f),
                       new SpawnPoint(new Vector3(52.47302f, 2739.275f, 56.93087f), 223.4171f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(47.40558f, 2746.623f, 57.32721f), 37.49776f),
                       new SpawnPoint(new Vector3(61.14762f, 2755.915f, 57.16882f), 355.4889f),
                       new SpawnPoint(new Vector3(27.50518f, 2781.851f, 57.90956f), 315.4908f),
                   },
                   20
               ),


            new ArsonSpawn
               (
                   new Vector3(-2554.268f, 2331.125f, 33.06002f),
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-2472.905f, 2317.517f, 31.96568f), 70.47023f),
                       new SpawnPoint(new Vector3(-2582.441f, 2318.682f, 32.93365f), 284.0915f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-2502.644f, 2327.658f, 32.99393f), 166.055f),
                       new SpawnPoint(new Vector3(-2578.468f, 2321.262f, 33.05946f), 286.7754f),
                       new SpawnPoint(new Vector3(-2574.356f, 2327.996f, 33.05995f), 284.7235f),
                       new SpawnPoint(new Vector3(-2529.884f, 2321.958f, 33.05988f), 64.07634f),
                   },
                   18
               ),


            new ArsonSpawn
               (
                   new Vector3(-3199.76f, 979.3226f, 20.22274f),
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-3172.763f, 960.8762f, 14.88943f), 177.9617f),
                       new SpawnPoint(new Vector3(-3167.458f, 983.2066f, 15.77269f), 151.3115f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-3166.82f, 979.0817f, 15.57836f), 255.6976f),
                       new SpawnPoint(new Vector3(-3173.887f, 976.114f, 15.62302f), 125.7255f),
                       new SpawnPoint(new Vector3(-3176.795f, 963.8707f, 15.22638f), 72.32101f),
                   },
                   17
               ),


            new ArsonSpawn
               (
                   new Vector3(-1122.561f, -1975.783f, 13.16184f),
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-1096.88f, -1958.083f, 12.98954f), 160.7921f),
                       new SpawnPoint(new Vector3(-1100.843f, -1993.222f, 13.14402f), 183.9029f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-1100.894f, -1969.636f, 13.15127f), 251.2576f),
                       new SpawnPoint(new Vector3(-1104.558f, -2008.793f, 13.16598f), 60.81666f),
                       new SpawnPoint(new Vector3(-1103.916f, -1975.063f, 13.0442f), 101.9416f),
                   },
                   18
               ),


            new ArsonSpawn
               (
                   new Vector3(-1921.221f, 2048.179f, 140.7354f),
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-1890.214f, 2047.367f, 140.8656f), 67.34678f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-1900.789f, 2052.915f, 140.7943f), 120.2011f),
                       new SpawnPoint(new Vector3(-1901.014f, 2044.808f, 140.7834f), 80.96359f),
                       new SpawnPoint(new Vector3(-1903.176f, 2035.5f, 140.7394f), 60.00785f),
                   },
                   16
               ),


            new ArsonSpawn
               (
                   new Vector3(-1730.423f, 1976.31f, 121.6113f),
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-1759.788f, 1980.326f, 118.8129f), 324.9944f),
                       new SpawnPoint(new Vector3(-1759.381f, 2011.02f, 118.7169f), 229.9529f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-1751.875f, 1992.651f, 117.1652f), 265.8949f),
                       new SpawnPoint(new Vector3(-1753.168f, 1982.027f, 118.4095f), 251.7332f),
                       new SpawnPoint(new Vector3(-1747.82f, 1997.836f, 116.9645f), 243.5156f),
                   },
                   19,
                   false
               ),


            new ArsonSpawn
               (
                   new Vector3(725.9325f, 4189.153f, 40.70924f),
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(749.0913f, 4193.621f, 40.79007f), 109.9272f),
                       new SpawnPoint(new Vector3(777.9384f, 4200.177f, 43.03612f), 114.03f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(746.1503f, 4186.412f, 40.75711f), 34.13688f),
                       new SpawnPoint(new Vector3(741.8054f, 4185.711f, 40.73474f), 72.81355f),
                       new SpawnPoint(new Vector3(737.8152f, 4179.15f, 40.72482f), 44.76297f),
                   },
                   18,
                   false
               ),


            new ArsonSpawn
               (
                   new Vector3(-2182.602f, 4258.074f, 48.85698f),
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-2224.43f, 4266.898f, 46.62888f), 327.7561f),
                       new SpawnPoint(new Vector3(-2207.042f, 4266.905f, 47.80288f), 205.2405f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-2208.875f, 4257.665f, 47.43122f), 225.3703f),
                       new SpawnPoint(new Vector3(-2203.772f, 4253.059f, 47.54842f), 308.1809f),
                       new SpawnPoint(new Vector3(-2201.302f, 4262.101f, 47.94815f), 252.5457f),
                       new SpawnPoint(new Vector3(-2195.581f, 4271.023f, 48.57129f), 214.4651f),
                   },
                   17,
                   false
               ),


            new ArsonSpawn
               (
                   new Vector3(153.1696f, 6627.505f, 31.74917f),
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(184.1789f, 6618.953f, 31.77566f), 66.61541f),
                       new SpawnPoint(new Vector3(132.6906f, 6630.165f, 31.68507f), 310.952f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(181.0123f, 6616.944f, 31.80508f), 154.4962f),
                       new SpawnPoint(new Vector3(173.2645f, 6612.214f, 31.86246f), 25.98388f),
                       new SpawnPoint(new Vector3(163.4122f, 6609.803f, 31.88388f), 41.98464f),
                       new SpawnPoint(new Vector3(143.1544f, 6623.822f, 31.72866f), 312.4217f),
                   },
                   16
               ),


            new ArsonSpawn
               (
                   new Vector3(175.4573f, 6603.493f, 31.84854f),
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(180.0033f, 6571.556f, 31.84474f), 108.2267f),
                       new SpawnPoint(new Vector3(223.5172f, 6575.465f, 31.79309f), 102.2704f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(178.8146f, 6574.608f, 31.84458f), 24.37193f),
                       new SpawnPoint(new Vector3(207.8766f, 6582.354f, 31.7575f), 63.09464f),
                       new SpawnPoint(new Vector3(206.3268f, 6596.02f, 31.78126f), 81.17664f),
                   },
                   15
               ),


            new ArsonSpawn
               (
                   new Vector3(1687.454f, 4929.722f, 42.07814f),
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(1681.943f, 4959.889f, 42.7037f), 118.6374f),
                       new SpawnPoint(new Vector3(1696.836f, 4954.489f, 43.20039f), 297.1062f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(1687.962f, 4962.854f, 43.0379f), 192.6182f),
                       new SpawnPoint(new Vector3(1683.405f, 4946.247f, 42.41428f), 198.8785f),
                       new SpawnPoint(new Vector3(1671.958f, 4924.716f, 42.02889f), 270.6012f),
                       new SpawnPoint(new Vector3(1672.756f, 4929.719f, 42.08586f), 257.7f),
                   },
                   16
               ),


            new ArsonSpawn
               (
                   new Vector3(-91.88204f, 6417.593f, 31.47524f),
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-115.4957f, 6408.642f, 31.3183f), 313.3191f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-111.759f, 6404.544f, 31.47083f), 110.1791f),
                       new SpawnPoint(new Vector3(-107.3429f, 6407.219f, 31.49036f), 316.7961f),
                   },
                   13
               ),


            new ArsonSpawn
               (
                   new Vector3(1978.612f, 6212.157f, 42.21416f),
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(1956.852f, 6224.245f, 44.40628f), 196.4267f),
                       new SpawnPoint(new Vector3(1978.589f, 6253.031f, 45.83932f), 22.24693f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(1957.394f, 6230.799f, 44.19337f), 17.14087f),
                       new SpawnPoint(new Vector3(1975.321f, 6251.911f, 45.78214f), 132.5295f),
                       new SpawnPoint(new Vector3(1966.177f, 6206.216f, 44.88279f), 337.9195f),
                   },
                   17
               ),


            new ArsonSpawn
               (
                   new Vector3(2683.499f, 3263.614f, 55.24052f),
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(2708.951f, 3293.06f, 55.69302f), 126.591f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(2695.596f, 3297.428f, 55.70875f), 306.5284f),
                       new SpawnPoint(new Vector3(2692.772f, 3292.198f, 55.33187f), 155.1643f),
                       new SpawnPoint(new Vector3(2698.414f, 3281.899f, 55.24052f), 157.9178f),
                   },
                   16
               ),


            new ArsonSpawn
               (
                   new Vector3(1242.897f, -1481.482f, 34.69257f),
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(1234.076f, -1451.779f, 34.80425f), 270.4175f),
                       new SpawnPoint(new Vector3(1259.124f, -1471.433f, 35.64579f), 197.4496f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(1233.492f, -1460.501f, 34.92789f), 356.0125f),
                       new SpawnPoint(new Vector3(1241.042f, -1465.709f, 34.69253f), 181.6271f),
                       new SpawnPoint(new Vector3(1238.266f, -1468.125f, 34.69253f), 197.3885f),
                       new SpawnPoint(new Vector3(1238.981f, -1498.999f, 34.69253f), 333.364f),
                   },
                   15
               ),


            new ArsonSpawn
               (
                   new Vector3(1136.555f, -981.8674f, 46.41585f),
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(1170.25f, -964.5442f, 47.28435f), 0.1960548f),
                       new SpawnPoint(new Vector3(1149.175f, -996.8082f, 45.35197f), 185.5112f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(1143.791f, -988.0569f, 45.83471f), 279.2314f),
                       new SpawnPoint(new Vector3(1147.291f, -983.4611f, 46.05341f), 53.69696f),
                       new SpawnPoint(new Vector3(1146.589f, -977.4393f, 46.35995f), 125.4572f),
                   },
                   13,
                   false
               ),


            new ArsonSpawn
               (
                   new Vector3(1182.876f, -332.9431f, 69.17554f),
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(1198.1f, -313.8068f, 68.95084f), 201.3235f),
                       new SpawnPoint(new Vector3(1178.592f, -359.2221f, 68.47437f), 76.74512f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(1196.751f, -346.636f, 69.09191f), 274.9541f),
                       new SpawnPoint(new Vector3(1171.123f, -351.1111f, 67.93813f), 344.6655f),
                       new SpawnPoint(new Vector3(1189.707f, -311.3068f, 69.15419f), 153.0552f),
                   },
                   15
               ),


            new ArsonSpawn
               (
                   new Vector3(246.3602f, 220.6236f, 106.2868f),
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(259.4393f, 188.3379f, 104.7517f), 69.06964f),
                       new SpawnPoint(new Vector3(247.2453f, 193.0573f, 104.9719f), 68.89834f),
                       new SpawnPoint(new Vector3(224.1902f, 225.2886f, 105.3946f), 339.1912f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(229.2606f, 216.6435f, 105.5455f), 121.3705f),
                       new SpawnPoint(new Vector3(231.1845f, 212.8991f, 105.4793f), 341.4797f),
                       new SpawnPoint(new Vector3(256.5509f, 199.9048f, 105.0012f), 319.0016f),
                       new SpawnPoint(new Vector3(259.5184f, 199.5384f, 104.9516f), 357.9875f),
                   },
                   18,
                   false
               ),


            new ArsonSpawn
               (
                   new Vector3(-983.9022f, 306.6472f, 70.34543f),
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-989.1078f, 278.0238f, 67.78836f), 86.35748f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-976.0743f, 276.2818f, 68.44834f), 166.6194f),
                       new SpawnPoint(new Vector3(-980.4664f, 281.5879f, 68.31503f), 27.37496f),
                       new SpawnPoint(new Vector3(-974.5696f, 285.1952f, 68.74044f), 0.8333878f),
                   },
                   15,
                   false
               ),


            new ArsonSpawn
               (
                   new Vector3(-1801.447f, 800.2391f, 138.5142f),
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-1805.844f, 773.3328f, 136.6488f), 305.2906f),
                       new SpawnPoint(new Vector3(-1827.008f, 812.1385f, 139.1488f), 308.2357f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-1821.494f, 773.2589f, 136.8209f), 313.9324f),
                       new SpawnPoint(new Vector3(-1798.857f, 782.9647f, 137.4931f), 20.57072f),
                       new SpawnPoint(new Vector3(-1777.182f, 802.5223f, 139.6007f), 81.3399f),
                       new SpawnPoint(new Vector3(-1797.917f, 824.7526f, 139.7663f), 156.3466f),
                   },
                   16
               ),


            new ArsonSpawn
               (
                   new Vector3(-1824.712f, 791.4137f, 138.1938f),
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-1816.664f, 773.252f, 136.5541f), 348.3116f),
                       new SpawnPoint(new Vector3(-1801.292f, 775.3354f, 136.9485f), 122.8993f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-1803.31f, 778.2418f, 137.0293f), 35.74083f),
                       new SpawnPoint(new Vector3(-1807.029f, 782.7617f, 137.4272f), 77.12473f),
                       new SpawnPoint(new Vector3(-1820.932f, 776.4834f, 137.2589f), 318.3899f),
                   },
                   13,
                   false
               ),


            new ArsonSpawn
               (
                   new Vector3(-1652.3f, 4252.817f, 83.44783f),
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-1669.156f, 4222.517f, 81.91128f), 216.5364f),
                       new SpawnPoint(new Vector3(-1687.379f, 4257.114f, 76.83432f), 195.8886f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-1688.074f, 4264.266f, 75.68282f), 288.7847f),
                       new SpawnPoint(new Vector3(-1680.479f, 4262.131f, 76.63839f), 259.3781f),
                       new SpawnPoint(new Vector3(-1677.222f, 4251.35f, 77.9614f), 288.8887f),
                       new SpawnPoint(new Vector3(-1667.472f, 4233.674f, 80.92253f), 335.9399f),
                   },
                   15
               ),


            new ArsonSpawn
               (
                   new Vector3(-2332.898f, 4202.372f, 39.47134f),
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-2302.482f, 4201.151f, 40.41446f), 149.2975f),
                       new SpawnPoint(new Vector3(-2318.297f, 4174.931f, 38.92216f), 150.4779f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-2329.381f, 4156.191f, 38.14571f), 336.3613f),
                       new SpawnPoint(new Vector3(-2330.177f, 4166.889f, 38.6376f), 39.73767f),
                       new SpawnPoint(new Vector3(-2317.576f, 4187.782f, 39.39056f), 33.57357f),
                   },
                   14
               ),


            new ArsonSpawn
               (
                   new Vector3(-3244.956f, 1005.468f, 12.83071f),
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-3235.222f, 1013.718f, 12.07085f), 181.0852f),
                       new SpawnPoint(new Vector3(-3224.445f, 979.7897f, 12.64517f), 1.015815f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-3223.807f, 996.065f, 12.30026f), 71.68853f),
                       new SpawnPoint(new Vector3(-3236.74f, 1010.488f, 12.33289f), 168.7264f),
                       new SpawnPoint(new Vector3(-3235.427f, 993.7936f, 12.47003f), 81.96809f),
                   },
                   13,
                   false
               ),


            new ArsonSpawn
               (
                   new Vector3(2564.855f, 2576.975f, 37.86159f),
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(2586.705f, 2572.098f, 33.72396f), 196.7283f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(2586.734f, 2561.575f, 33.00461f), 291.697f),
                       new SpawnPoint(new Vector3(2584.243f, 2558.544f, 32.98616f), 62.60347f),
                       new SpawnPoint(new Vector3(2571.328f, 2589.001f, 36.96453f), 168.6976f),
                   },
                   14
               ),


            new ArsonSpawn
               (
                   new Vector3(2007.303f, 3772.333f, 32.18078f),
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(2034.234f, 3782.002f, 32.26469f), 27.59234f),
                       new SpawnPoint(new Vector3(2007.162f, 3744.533f, 32.39537f), 293.3316f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(2022.998f, 3767.865f, 32.18078f), 249.3337f),
                       new SpawnPoint(new Vector3(2018.056f, 3784.048f, 32.18078f), 121.1375f),
                       new SpawnPoint(new Vector3(2009.92f, 3760.844f, 32.18073f), 26.42454f),
                       new SpawnPoint(new Vector3(1983.885f, 3769.983f, 32.18084f), 288.2069f),
                   },
                   15
               ),


            new ArsonSpawn
               (
                   new Vector3(1390.77f, 3613.654f, 38.94194f),
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(1374.509f, 3609.94f, 34.89352f), 353.1674f),
                       new SpawnPoint(new Vector3(1410.642f, 3598.146f, 34.87332f), 106.4864f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(1369.9f, 3612.789f, 34.89311f), 284.4608f),
                       new SpawnPoint(new Vector3(1392.714f, 3626f, 35.11681f), 178.5711f),
                       new SpawnPoint(new Vector3(1385.457f, 3619.849f, 38.92145f), 202.4054f),
                       new SpawnPoint(new Vector3(1398.367f, 3597.748f, 34.85577f), 56.83043f),
                   },
                   16,
                   false
               ),


            new ArsonSpawn
               (
                   new Vector3(1374.123f, 3621.438f, 34.88636f),
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(1366.451f, 3578.938f, 34.99452f), 290.4886f),
                       new SpawnPoint(new Vector3(1397.932f, 3579.118f, 34.94115f), 106.7952f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(1389.374f, 3591.375f, 34.92831f), 104.0419f),
                       new SpawnPoint(new Vector3(1385.37f, 3595.682f, 34.89382f), 41.42356f),
                       new SpawnPoint(new Vector3(1374.657f, 3596.98f, 34.89544f), 2.84428f),
                   },
                   14
               ),
#endregion
        };



        public enum EArsonState
        {
            EnRoute,
            OnScene,
            Searching,
            Found,
            End,
        }
    }

    internal class ArsonSpawn
    {
        public Vector3 FirePosition;

        public List<SpawnPoint> FiretruckSpawnPositions;

        public List<SpawnPoint> FirefightersSpawnPositions;

        public List<Vehicle> Firetruks;
        public List<Ped> Firefighters;
        public List<ScriptedFire> Fires;
        private int _numOfFires;
        public bool CanSpawnCar;

        public ArsonSpawn() { }

        public ArsonSpawn(Vector3 firePos, List<SpawnPoint> firetruckSpawnPos, List<SpawnPoint> firefighterSpawnPos, int numOfFires)
        {
            this.FirePosition = firePos;
            this.FiretruckSpawnPositions = firetruckSpawnPos;

            this.FirefightersSpawnPositions = firefighterSpawnPos;

            Firetruks = new List<Vehicle>();
            Firefighters = new List<Ped>();

            Fires = new List<ScriptedFire>();

            _numOfFires = numOfFires;
            CanSpawnCar = true;
        }
        public ArsonSpawn(Vector3 firePos, List<SpawnPoint> firetruckSpawnPos, List<SpawnPoint> firefighterSpawnPos, int numOfFires, bool canSpawnCar)
        {
            this.FirePosition = firePos;
            this.FiretruckSpawnPositions = firetruckSpawnPos;

            this.FirefightersSpawnPositions = firefighterSpawnPos;

            Firetruks = new List<Vehicle>();
            Firefighters = new List<Ped>();

            Fires = new List<ScriptedFire>();

            _numOfFires = numOfFires;
            CanSpawnCar = canSpawnCar;
        }

        public void CreateFire()
        {
            for (int i = 0; i < _numOfFires; i++)
            {
                Fires.Add(new ScriptedFire(FirePosition.AroundPosition(10.0f).ToGroundUsingRaycasting(Game.LocalPlayer.Character), Globals.Random.Next(13, 20), Globals.Random.Next(101) < 50));
            }
        }

        public void Create(bool createFire)
        {
            if (createFire) CreateFire();

            #region Relationships
            RelationshipGroup firefighterRSG = new RelationshipGroup("ARSONFIREMANS");
            Game.SetRelationshipBetweenRelationshipGroups(firefighterRSG, "PLAYER", Relationship.Companion);
            Game.SetRelationshipBetweenRelationshipGroups(firefighterRSG, "COP", Relationship.Companion);
            Game.SetRelationshipBetweenRelationshipGroups("COP", firefighterRSG, Relationship.Companion);
            Game.SetRelationshipBetweenRelationshipGroups("CIVMALE", firefighterRSG, Relationship.Companion);
            Game.SetRelationshipBetweenRelationshipGroups("CIVFEMALE", firefighterRSG, Relationship.Companion);
            Game.SetRelationshipBetweenRelationshipGroups("SECURITY_GUARD", firefighterRSG, Relationship.Companion);
            Game.SetRelationshipBetweenRelationshipGroups("FIREMAN", firefighterRSG, Relationship.Companion);
            Game.SetRelationshipBetweenRelationshipGroups("GANG_1", firefighterRSG, Relationship.Companion);
            Game.SetRelationshipBetweenRelationshipGroups("GANG_2", firefighterRSG, Relationship.Companion);
            Game.SetRelationshipBetweenRelationshipGroups("GANG_1", firefighterRSG, Relationship.Companion);
            Game.SetRelationshipBetweenRelationshipGroups("GANG_9", firefighterRSG, Relationship.Companion);
            Game.SetRelationshipBetweenRelationshipGroups("GANG_10", firefighterRSG, Relationship.Companion);
            Game.SetRelationshipBetweenRelationshipGroups("AMBIENT_GANG_LOST", firefighterRSG, Relationship.Companion);
            Game.SetRelationshipBetweenRelationshipGroups("AMBIENT_GANG_MEXICAN", firefighterRSG, Relationship.Companion);
            Game.SetRelationshipBetweenRelationshipGroups("AMBIENT_GANG_FAMILY", firefighterRSG, Relationship.Companion);
            Game.SetRelationshipBetweenRelationshipGroups("AMBIENT_GANG_BALLAS", firefighterRSG, Relationship.Companion);
            Game.SetRelationshipBetweenRelationshipGroups("AMBIENT_GANG_MARABUNTE", firefighterRSG, Relationship.Companion);
            Game.SetRelationshipBetweenRelationshipGroups("AMBIENT_GANG_CULT", firefighterRSG, Relationship.Companion);
            Game.SetRelationshipBetweenRelationshipGroups("AMBIENT_GANG_SALVA", firefighterRSG, Relationship.Companion);
            Game.SetRelationshipBetweenRelationshipGroups("AMBIENT_GANG_WEICHENG", firefighterRSG, Relationship.Companion);
            Game.SetRelationshipBetweenRelationshipGroups("AMBIENT_GANG_HILLBILLY", firefighterRSG, Relationship.Companion);
            Game.SetRelationshipBetweenRelationshipGroups("DEALER", firefighterRSG, Relationship.Companion);
            Game.SetRelationshipBetweenRelationshipGroups("ARMY", firefighterRSG, Relationship.Companion);
            Game.SetRelationshipBetweenRelationshipGroups("GUARD_DOG", firefighterRSG, Relationship.Companion);
            Game.SetRelationshipBetweenRelationshipGroups("MEDIC", firefighterRSG, Relationship.Companion);
            #endregion

            foreach (SpawnPoint pedSP in FirefightersSpawnPositions)
            {
                Ped p = new Ped("s_m_y_fireman_01", pedSP.Position, pedSP.Heading);
                p.Inventory.GiveNewWeapon("WEAPON_FIREEXTINGUISHER", 100, true);
                p.RelationshipGroup = firefighterRSG;
                Firefighters.Add(p);
            }
            foreach (Ped ped in Firefighters)
            {
                if (ped.Exists())
                {
                    if (ped == Firefighters[0])
                    {
                        NativeFunction.CallByName<uint>("SET_PED_PROP_INDEX", ped, 0, 0, 2, 2);
                        ped.IsInvincible = true;
                        ped.BlockPermanentEvents = true;
                    }
                    else
                    {
                        NativeFunction.CallByName<uint>("SET_PED_COMPONENT_VARIATION", ped, 8, 2, 0, 2);
                        NativeFunction.CallByName<uint>("SET_PED_PROP_INDEX", ped, 0, 0, 0, 2);
                    }
                }
            }
            foreach (SpawnPoint truckSP in FiretruckSpawnPositions)
            {
                Vehicle fT = new Vehicle("firetruk", truckSP.Position, truckSP.Heading);
                fT.IsSirenOn = true;
                Firetruks.Add(fT);
            }
        }

        public void Delete()
        {
            foreach (ScriptedFire fire in Fires)
            {
                fire.Remove();
            }
            foreach (Ped ped in Firefighters)
            {
                if (ped.Exists()) ped.Delete();
            }
            foreach (Vehicle veh in Firetruks)
            {
                if (veh.Exists()) veh.Delete();
            }
        }

        public void Dismiss()
        {
            foreach (ScriptedFire fire in Fires)
            {
                fire.Remove();
            }
            foreach (Ped ped in Firefighters)
            {
                if (ped.Exists()) ped.Dismiss();
            }
            foreach (Vehicle veh in Firetruks)
            {
                if (veh.Exists()) veh.Dismiss();
            }
        }
    }
}