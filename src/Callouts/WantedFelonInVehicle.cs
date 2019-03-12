namespace WildernessCallouts.Callouts
{
    using Rage;
    using LSPD_First_Response.Mod.API;
    using LSPD_First_Response.Mod.Callouts;
    using System.Drawing;
    using WildernessCallouts.Peds;
    using WildernessCallouts.Types;

    [CalloutInfo("WantedFelonInVehicle", CalloutProbability.Medium)]
    internal class WantedFelonInVehicle : CalloutBase
    {
        private Vehicle pedVehicle; // a rage vehicle
        private Ped ped; // a rage ped
        private Vector3 spawnPoint; // a Vector3
        private Blip searchZoneBlip; // a rage blip
        private Blip notFleeBlip; // a rage blip
        private LHandle pursuit; // an API pursuit handle

        private HeliPilot heliPilot;

        private static string[] carModel = { "adder"    , "zentorno"  , "akuma"   , "baller"  , "banshee"   , "bati"       , "bjxl"        , "buccaneer" , "buffalo"   , "cheetah"   , "cogcabrio", "dominator", 
                                             "entityxf" , "gauntlet"  , "granger" , "hotknife", "infernus"  , "ztype"      , "casco"       , "lectro"    , "guardian"  , "kuruma"    , "kuruma2"  , "chino"    ,
                                             "dukes"    , "stalion"   , "furoregt", "slamvan2", "enduro"    , "blista"     , "asea"        , "daemon"    , "peyote"    , "primo"     , "sandking" , "sandking2",
                                             "bagger"   , "blazer"    , "blazer3" , "bobcatxl", "buccaneer2", "chino2"     , "faction"     , "faction2"  , "moonbeam"  , "moonbeam2" , "primo2"   , "voodoo"   ,
                                             "panto"    , "prairie"   , "emperor" , "exemplar", "ztype"     , "lurcher"    , "rhapsody"    , "pigalle"   , "warrener"  , "blade"     , "glendale" , "huntley"  ,  
                                             "massacro" , "thrust"    , "alpha"   , "jester"  , "turismor"  , "bifta"      , "kalahari"    , "paradise"  , "furoregt"  , "innovation", "coquette2", "feltzer3" ,
                                             "osiris"   , "windsor"   , "brawler" , "chino"   , "coquette3" , "vindicator" , "t20"         , "btype2"    , "rocoto"    , "comet2"    , "coquette" , "bison"    ,
                                             "bullet"   , "rebel"     , "rebel2"  , "vigero"  , "youga"     , "zion"       , "zion2"       , "washington", "voltic"    , "virgo"     , "tampa"    , "baller3"  ,
                                             "baller4"  , "baller5"   , "baller6" , "cog55"   , "cog552"    , "cognoscenti", "cognoscenti2", "mamba"     , "nightshade", "schafter3" , "schafter4", "schafter5",
                                             "schafter6", "verlierer2", "f620"    , "elegy2"  , "surano"    , "sultanrs"   , "banshee2"    , "nemesis"   , "lectro"    , "pcj"       , "carbonrs" , "daemon"   ,
                                             "double"   , "enduro"    , "ruffian" , "faction3", "minivan2"  , "sabregt2"   , "slamvan3"    , "tornado5"  , "virgo2"    , "virgo3"
                                           }; 

        private static string[] weaponAsset = { "WEAPON_PISTOL"      , "WEAPON_COMBATPISTOL", "WEAPON_APPISTOL"      , "WEAPON_PISTOL50", "WEAPON_MICROSMG"  , "WEAPON_SMG"  , "WEAPON_ASSAULTRIFLE", 
                                                "WEAPON_CARBINERIFLE", "WEAPON_PUMPSHOTGUN" , "WEAPON_SAWNOFFSHOTGUN", "WEAPON_COMBATMG", "WEAPON_ASSAULTSMG", "WEAPON_KNIFE", "WEAPON_GOLFCLUB"    , 
                                                "WEAPON_CROWBAR"     , "WEAPON_HAMMER"      , "WEAPON_SNIPERRIFLE"
                                              };

        private static string[] specialModels = { "player_zero", "player_one", "player_two" };

        private bool hasPursuitStarted = false;

        private bool shouldAttack = false;

        private EWantedFelonInVehicleState state;

        /// <summary>
        /// OnBeforeCalloutDisplayed is where we create a blip for the user to see where the pursuit is happening, we initiliaize any variables above and set
        /// the callout message and position for the API to display
        /// </summary>
        /// <returns></returns>
        public override bool OnBeforeCalloutDisplayed()
        {
            //Set our spawn point to be on a street around 300f (distance) away from the player.
            spawnPoint = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.AroundPosition(425f));

            if (Vector3.Distance(Game.LocalPlayer.Character.Position, spawnPoint) < 15.0f) return false;
            //Create our ped in the world
            int rndModelSpecial = Globals.Random.Next(101);
            if (rndModelSpecial < 95) ped = new Ped(spawnPoint);
            else if (rndModelSpecial >= 95) ped = new Ped(specialModels.GetRandomElement(), spawnPoint, 0.0f);
            if (!ped.Exists()) return false;

            string vModel = carModel.GetRandomElement(true);
            for (int i = 0; i < 5; i++)
            {
                if (new Model(vModel).IsValid)
                    break;
                else
                {
                    Logger.LogTrivial(this.GetType().Name, "Vehicle Model < " + vModel + " > is invalid. Choosing new model...");
                    vModel = carModel.GetRandomElement(false);
                }
            }
            if (!new Model(vModel).IsValid)
            {
                Logger.LogTrivial(this.GetType().Name, "Aborting: Final Vehicle Model < " + vModel + " > is invalid");
                return false;
            }
            //Create the vehicle for our ped
            pedVehicle = new Vehicle(vModel, spawnPoint);
            if (!pedVehicle.Exists()) return false;

            if (pedVehicle.Exists()) pedVehicle.Heading = pedVehicle.Position.GetClosestVehicleNodeHeading();

            ped.RelationshipGroup = new RelationshipGroup("FELON");
            ped.BlockPermanentEvents = false;
            
            //If we made it this far both exist so let's warp the ped into the driver seat
            if (ped.Exists()) ped.WarpIntoVehicle(pedVehicle, -1);

            if (ped.Exists()) ped.Inventory.GiveNewWeapon(weaponAsset.GetRandomElement(true), 666, false);

            LSPD_First_Response.Engine.Scripting.Entities.Persona pedPersona = Functions.GetPersonaForPed(ped);               // Sets the ped persona as wanted         
            Functions.SetPersonaForPed(ped, new LSPD_First_Response.Engine.Scripting.Entities.Persona(ped, pedPersona.Gender, pedPersona.BirthDay, pedPersona.Citations, pedPersona.Forename, pedPersona.Surname, pedPersona.LicenseState,
                                       pedPersona.TimesStopped, true, pedPersona.IsAgent, pedPersona.IsCop));

            if (Globals.Random.Next(2) == 1) pedVehicle.InstallRandomMods();

            // Show the user where the pursuit is about to happen and block very close peds.
            this.ShowCalloutAreaBlipBeforeAccepting(spawnPoint, 110f);
            this.AddMinimumDistanceCheck(15f, ped.Position);

            // Set up our callout message and location
            this.CalloutMessage = "Wanted felon in vehicle";
            this.CalloutPosition = spawnPoint;

            VehicleDrivingFlags driveFlags = VehicleDrivingFlags.None;
            switch (Globals.Random.Next(3))
            {
                case 0:
                    driveFlags = (VehicleDrivingFlags)5;
                    break;
                case 1:
                    driveFlags = (VehicleDrivingFlags)786468;
                    break;
                case 2:
                    driveFlags = (VehicleDrivingFlags)20;
                    break;
                default:
                    break;
            }
            if (pedVehicle.Model.IsBike || pedVehicle.Model.IsBicycle) ped.GiveHelmet(false, HelmetTypes.RegularMotorcycleHelmet, -1);
            if (ped.Exists()) ped.Tasks.CruiseWithVehicle(pedVehicle, 16.0f, driveFlags);

            int rndAudioNum = Globals.Random.Next(0, 5);
            if (rndAudioNum == 0) LSPD_First_Response.Mod.API.Functions.PlayScannerAudioUsingPosition("WE_HAVE CRIME_WANTED_FELON_ON_THE_LOOSE SUSPECT_LAST_SEEN IN_OR_ON_POSITION UNITS_RESPOND_CODE_02", spawnPoint);
            else if (rndAudioNum == 1) LSPD_First_Response.Mod.API.Functions.PlayScannerAudioUsingPosition("CITIZENS_REPORT CRIME_WANTED_FELON_ON_THE_LOOSE IN_OR_ON_POSITION UNITS_RESPOND_CODE_02", spawnPoint);
            else if (rndAudioNum == 2) LSPD_First_Response.Mod.API.Functions.PlayScannerAudioUsingPosition("OFFICERS_REPORT CRIME_WANTED_FELON_ON_THE_LOOSE SUSPECT_LAST_SEEN IN_OR_ON_POSITION UNITS_RESPOND_CODE_02", spawnPoint);
            else if (rndAudioNum == 3) LSPD_First_Response.Mod.API.Functions.PlayScannerAudioUsingPosition("OFFICERS_REPORT CRIME_WANTED_FELON_ON_THE_LOOSE IN_OR_ON_POSITION UNITS_RESPOND_CODE_02", spawnPoint);
            else if (rndAudioNum == 4) LSPD_First_Response.Mod.API.Functions.PlayScannerAudioUsingPosition("CITIZENS_REPORT CRIME_WANTED_FELON_ON_THE_LOOSE SUSPECT_LAST_SEEN IN_OR_ON_POSITION UNITS_RESPOND_CODE_02", spawnPoint);

            return base.OnBeforeCalloutDisplayed();
        }


        /// <summary>
        /// OnCalloutAccepted is where we begin our callout's logic. In this instance we create our pursuit and add our ped from eariler to the pursuit as well
        /// </summary>
        /// <returns></returns>
        public override bool OnCalloutAccepted()
        {
            //We accepted the callout, so lets initilize our blip from before and attach it to our ped so we know where he is.
            searchZoneBlip = new Blip(ped.Position, 320.0f);
            searchZoneBlip.Color = Color.FromArgb(100, Color.Yellow);

            //Game.SetRelationshipBetweenRelationshipGroups(ped.RelationshipGroup, "COP", Relationship.Dislike);
            //Game.SetRelationshipBetweenRelationshipGroups(ped.RelationshipGroup, "PLAYER", Relationship.Dislike);




            state = EWantedFelonInVehicleState.Searching;

            GameFiber.StartNew(delegate
            {
                Vector3 position = ped.Position;
                Game.DisplayNotification("~b~Dispatch: ~w~Suspect last seen in a ~b~" + pedVehicle.Model.Name + "~w~, color ~b~" + pedVehicle.GetPrimaryColor().ToFriendlyName().ToLower());
                Game.DisplayNotification("~b~Dispatch: ~w~At ~b~" + position.GetStreetName() + "~w~, in ~b~" + position.GetZoneName() + "~w~, ~b~" + position.GetAreaName());
                Game.DisplayNotification("~b~Dispatch: ~w~Air unit deployed");

                GameFiber.Wait(Globals.Random.Next(1250, 2750));
                Game.DisplayNotification("~b~" + Settings.General.Name + ": ~w~10-4");
            });

            UpdateSearchArea();

            return base.OnCalloutAccepted();
        }

        /// <summary>
        /// If you don't accept the callout this will be called, we clear anything we spawned here to prevent it staying in the game
        /// </summary>
        public override void OnCalloutNotAccepted()
        {
            state = EWantedFelonInVehicleState.End;

            if (ped.Exists()) ped.Delete();
            if (pedVehicle.Exists()) pedVehicle.Delete();
            if (searchZoneBlip.Exists()) searchZoneBlip.Delete();
            if (notFleeBlip.Exists()) notFleeBlip.Delete();
            if (heliPilot.Exists()) heliPilot.CleanUpHeliPilot();

            base.OnCalloutNotAccepted();
        }

        //This is where it all happens, run all of your callouts logic here
        public override void Process()
        {
            if (ped.Exists() && Vector3.Distance(Game.LocalPlayer.Character.Position, ped.Position) < 17.5f && state == EWantedFelonInVehicleState.Searching)
            {
                state = EWantedFelonInVehicleState.Found;
                SuspectFound();
            }

            if (state == EWantedFelonInVehicleState.Found)
            {
                if (ped.Exists() && shouldAttack && !ped.IsInAnyVehicle(false) && !Functions.IsPedGettingArrested(ped) && !Functions.IsPedArrested(ped)) 
                    ped.AttackPed(Game.LocalPlayer.Character);
            }

            base.Process();
            
            if (!ped.Exists() || ped.IsDead || Functions.IsPedArrested(ped))
                this.End();

        }

        /// <summary>
        /// More cleanup, when we call end you clean away anything left over
        /// This is also important as this will be called if a callout gets aborted (for example if you force a new callout)
        /// </summary>
        public override void End()
        {
            state = EWantedFelonInVehicleState.End;

            if (searchZoneBlip.Exists()) searchZoneBlip.Delete();
            if (notFleeBlip.Exists()) notFleeBlip.Delete();
            if (ped.Exists()) ped.Dismiss();
            if (pedVehicle.Exists()) pedVehicle.Dismiss();
            if (hasPursuitStarted) Functions.ForceEndPursuit(pursuit);
            if (heliPilot.Exists()) heliPilot.CleanUpHeliPilot();

            base.End();
        }


        protected override void CleanUp()
        {
            // TODO: implement CleanUp()
        }

        public void SuspectFound()
        {
            GameFiber.StartNew(delegate
            {
                Logger.LogTrivial(this.GetType().Name, "SuspectFound()");

                if (searchZoneBlip.Exists()) searchZoneBlip.Delete();

                int rndBehaviour = Globals.Random.Next(101);

                if (rndBehaviour < 60) /* FLEE */
                {
                    Logger.LogTrivial(this.GetType().Name, "Scenario: Flee");

                    Game.DisplayNotification("~b~" + Settings.General.Name + ": ~w~Dispatch, suspect located, fleeing. Moving to engage");
                    LSPD_First_Response.Mod.API.Functions.PlayScannerAudio("REPORT_SUSPECT_SPOTTED");
                    pursuit = LSPD_First_Response.Mod.API.Functions.CreatePursuit();
                    hasPursuitStarted = true;
                    LSPD_First_Response.Mod.API.Functions.AddPedToPursuit(pursuit, ped);
                    shouldAttack = true;
                    if (ped.IsInAnyVehicle(false))
                    {
                        VehicleDrivingFlags driveFlags = VehicleDrivingFlags.None;
                        switch (Globals.Random.Next(3))
                        {
                            case 0:
                                driveFlags = (VehicleDrivingFlags)20;
                                break;
                            case 1:
                                driveFlags = (VehicleDrivingFlags)786468;
                                break;
                            case 2:
                                if (!pedVehicle.Model.IsBike || !pedVehicle.Model.IsBicycle) driveFlags = (VehicleDrivingFlags)1076;
                                else driveFlags = (VehicleDrivingFlags)786468;
                                break;
                            default:
                                break;
                        }
                        ped.Tasks.CruiseWithVehicle(pedVehicle, 200.0f, driveFlags);
                    }
                    else if (shouldAttack && ped.Exists() && !ped.IsInAnyVehicle(false) && !LSPD_First_Response.Mod.API.Functions.IsPedGettingArrested(ped) && !LSPD_First_Response.Mod.API.Functions.IsPedArrested(ped))
                        ped.AttackPed(Game.LocalPlayer.Character);
                    //while (true)
                    //{
                    //    if (breakForceEnd) break;

                    //    if (!ped.Exists())
                    //    {
                    //        this.End();
                    //        break;
                    //    }
                    //    if (ped.IsDead)
                    //    {
                    //        this.End();
                    //        break;
                    //    }
                    //    if (!LSPD_First_Response.Mod.API.Functions.IsPursuitStillRunning(pursuit))
                    //    {
                    //        this.End();
                    //        break;
                    //    }

                    //    if (!ped.IsInAnyVehicle(false) && !LSPD_First_Response.Mod.API.Functions.IsPedGettingArrested(ped) && !LSPD_First_Response.Mod.API.Functions.IsPedArrested(ped)) ped.Tasks.FightAgainstClosestHatedTarget(30.0f);

                    //    GameFiber.Yield();
                    //}
                }
                else if (rndBehaviour >= 60) /* NOT FLEE */
                {
                    Logger.LogTrivial(this.GetType().Name, "Scenario: NotFlee");

                    notFleeBlip = new Blip(ped);
                    notFleeBlip.Color = Color.FromArgb(255, Color.Yellow);

                    shouldAttack = Globals.Random.Next(2) == 1;

                    Game.DisplayNotification("~b~" + Settings.General.Name + ": ~w~Dispatch, suspect located, not fleeing. Approaching for traffic stop");
                    LSPD_First_Response.Mod.API.Functions.PlayScannerAudio("REPORT_SUSPECT_SPOTTED");
                    GameFiber.Wait(1500);
                    Game.DisplayNotification("~b~Dispatch: ~w~Roger, proceed");

                    //while (true)
                    //{
                    //    if (breakForceEnd) break;

                    //    if (!ped.Exists())
                    //    {
                    //        this.End();
                    //        break;
                    //    }
                    //    if (ped.IsDead)
                    //    {
                    //        this.End();
                    //        break;
                    //    }
                    //    if (Functions.IsPedArrested(ped))
                    //    {
                    //        this.End();
                    //        break;
                    //    }

                    //    GameFiber.Yield();
                    //}
                }
            });
        }


        public void UpdateSearchArea()
        {
            Logger.LogTrivial(this.GetType().Name, "UpdateSearchArea()");

            GameFiber.StartNew(delegate
            {
                GameFiber.Sleep(200);

                heliPilot = new HeliPilot(Vector3.Zero, 0.0f);
                heliPilot.JobFollow(ped);

                while (true)
                {
                    GameFiber.Wait(15000);

                    if (state != EWantedFelonInVehicleState.Searching) break;
                    if (!searchZoneBlip.Exists()) break;

                    if (Vector3.Distance2D(ped.Position, searchZoneBlip.Position) > 415.0f)
                    {
                        if (searchZoneBlip.Exists())
                        {
                            Functions.PlayScannerAudio("REQUEST_GUIDANCE_DISPATCH");

                            Logger.LogTrivial(this.GetType().Name, "SearchArea Updated");

                            GameFiber.Wait(4900);
                            
                            Vector3 position = ped.Position;

                            WildernessCallouts.Common.PlayHeliAudioWithEntityCardinalDirection("OFFICER_INTRO", ped);

                            if (searchZoneBlip.Exists()) searchZoneBlip.Position = ped.Position;

                            if (ped.IsInAnyVehicle(false))
                            {
                                if (searchZoneBlip.Exists()) Game.DisplayNotification("~b~Air support: ~w~Suspect vehicle model is ~b~" + ped.CurrentVehicle.Model.Name + "~w~, color ~b~" + ped.CurrentVehicle.GetPrimaryColor().ToFriendlyName().ToLower());
                                if (searchZoneBlip.Exists()) Game.DisplayNotification("~b~Air support: ~w~Near ~b~" + position.GetStreetName() + "~w~, in ~b~" + position.GetZoneName());
                            }
                            else
                            {
                                if (searchZoneBlip.Exists()) Game.DisplayNotification("~b~Air support: ~w~Suspect is ~b~on foot");
                                if (searchZoneBlip.Exists()) Game.DisplayNotification("~b~Air support: ~w~Near ~b~" + position.GetStreetName() + "~w~, in ~b~" + position.GetZoneName());
                            }
                        }
                    }

                    GameFiber.Yield();
                }
            });
        }


        public enum EWantedFelonInVehicleState
        {
            Searching,
            Found,
            End,
        }
    }
}
