namespace WildernessCallouts.Callouts
{
    using Rage;
    using Rage.Native;
    using LSPD_First_Response.Mod.API;
    using LSPD_First_Response.Mod.Callouts;
    using System.Drawing;
    using WildernessCallouts.Types;

    [CalloutInfo("IllegalHunting", CalloutProbability.Medium)]
    internal class IllegalHunting : CalloutBase
    {
        //Here we declare our variables, things we need or our callout
        private Vehicle hunterVeh; // a rage vehicle
        private Ped hunter; // a rage ped
        private Ped animal;
        private Vector3 spawnPoint; // a Vector3
        private Blip animalBlip; // a rage blip
        private LHandle pursuit; // an API pursuit handle

        private static string[] animalsModels = { "a_c_coyote", "a_c_boar", "a_c_chimp", "a_c_deer", "a_c_cormorant", "a_c_pig", "a_c_deer", "a_c_coyote", "a_c_boar", "a_c_rhesus" };

        private static string[] huntersModels = { "csb_cletus", "ig_clay", "ig_oneil", "a_m_m_tramp_01", "ig_old_man1a", "ig_hunter", "player_two", "mp_m_exarmy_01", "ig_clay", "ig_oneil", "ig_old_man2", "ig_old_man1a", "ig_hunter", 
                                                  "ig_russiandrunk", "ig_clay", "mp_m_exarmy_01", "ig_old_man2", "ig_old_man1a", "ig_hunter", "s_m_y_armymech_01", "s_m_m_ammucountry", "s_m_m_trucker_01", "ig_ortega",
                                                  "ig_russiandrunk", "a_m_m_tramp_01", "a_m_m_trampbeac_01", "a_m_m_rurmeth_01", "mp_m_exarmy_01", "g_m_y_pologoon_01", "g_m_y_mexgoon_03", "g_m_y_lost_03", "g_m_y_lost_02", 
                                                  "g_m_y_pologoon_02", "g_m_m_armboss_01", "u_m_o_taphillbilly", "u_m_o_taphillbilly"
                                                };

        private static WeaponAsset[] hunterWeapons = { "WEAPON_PUMPSHOTGUN", "WEAPON_HEAVYSNIPER", "WEAPON_PISTOL50"   , "WEAPON_PUMPSHOTGUN", "WEAPON_PISTOL"     , 
                                                       "WEAPON_HEAVYSNIPER", "WEAPON_SNIPERRIFLE", "WEAPON_HEAVYSNIPER", "WEAPON_COMBATMG"   , "WEAPON_PISTOL"     , 
                                                       "WEAPON_PUMPSHOTGUN", "WEAPON_PISTOL"     , "WEAPON_SNIPERRIFLE", "WEAPON_PISTOL50"   , "WEAPON_HEAVYSNIPER", 
                                                       "WEAPON_PUMPSHOTGUN", "WEAPON_HEAVYSNIPER", "WEAPON_PISTOL50"   , "WEAPON_SNIPERRIFLE", "WEAPON_SNIPERRIFLE", 
                                                       "WEAPON_SNIPERRIFLE", "WEAPON_HEAVYSNIPER", "WEAPON_SNIPERRIFLE", "WEAPON_HEAVYSNIPER", "WEAPON_MINIGUN" 
                                                     };

        private static string[] hunterVehicleModels = { "rebel", "rebel2", "sadler", "mesa", "mesa3", "sandking", "sandking2", "bison", "bodhi2", "bobcatxl", "dubsta", "dubsta", "landstalker", "brawler" };


        private EIllegalHuntingState state;

        private bool breakForceEnd = false;
        
        private static string[] policeGreetings = { "Hi, sir", "Hello", "Hey", "Hello, sir" };
        //private string[] policeInsults = { "Asshole!", "Hello", "Hey", "", "" };

        private static string[] policeQuestions = { "Where is your hunting license?", "Do you have any hunting license?", "Any hunting license?", "Your hunting license?", "Do you have your hunting license here?" };

        private static string[] hunterLicenseAnswers = { "Here you have", "It's here", "I've it here" };
        private static string[] hunterNoLicenseAndStayAnswers = { "I forgot it at home", "I don't have a license", "I didn't know I needed a license", "...a license?" };
        private static string[] hunterNoLicenseAndFleeAnswers = { "...a license? Not today!", "Bye! I don't need a license!" };
        private static string[] hunterNoLicenseAndAttackAnswers = { "I don't have the license, but... I can hunt you!", "Fuck you!", "Fuck the license!", "Fuck you and fuck the license, asshole!" };

        private static string[] policeLicenseGood = { "All good, goodbye", "All right, happy hunting" };
        private static string[] policeLicenseBad = { "Sir, your license is suspended", "The license is invalid", "The license is suspended" };

        private static string[] hunterLicenseSuspended = { "What?", "It can't be..", "Are you sure?" };

        private bool licenseHasBeenGiven = false;

        private bool isPursuitRunning = false;

        private int rndAimAtAnimal = MathHelper.GetRandomInteger(0, 4);

        private EScenario scenario;

        public override bool OnBeforeCalloutDisplayed()
        {
            spawnPoint = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.AroundPosition(400f));

            if (Vector3.Distance(Game.LocalPlayer.Character.Position, spawnPoint) < 40.0f) return false;

            EWorldArea spawnZone = WorldZone.GetArea(spawnPoint);
            if (spawnZone == EWorldArea.Los_Santos) return false;


            hunterVeh = new Vehicle(hunterVehicleModels.GetRandomElement(true), spawnPoint);
            if (!hunterVeh.Exists()) return false;

            Vector3 hunterSpawnPos = hunterVeh.Position.AroundPosition(9.5f);
            while (Vector3.Distance(hunterSpawnPos, hunterVeh.Position) < 4.0f)
            {
                hunterSpawnPos = hunterVeh.Position.AroundPosition(9.5f);
                GameFiber.Yield();
            }
            hunter = new Ped(huntersModels.GetRandomElement(true), hunterSpawnPos, 0.0f);
            if (!hunter.Exists()) return false;
            hunter.Inventory.GiveNewWeapon(hunterWeapons.GetRandomElement(true), 666, true);

            Vector3 spawnPos = hunter.Position.AroundPosition(0.2f);
            Vector3 spawnPos2 = spawnPos + new Vector3(MathHelper.GetRandomSingle(-0.05f, 0.05f), MathHelper.GetRandomSingle(-0.05f, 0.05f), 0.0f);
            Vector3 spawnPos3 = new Vector3(spawnPos2.X, spawnPos2.Y, spawnPos2.GetGroundZ() + 0.1525f);        /// Gets the Z position

            animal = new Ped(animalsModels.GetRandomElement(true), hunter.Position.AroundPosition(0.5f).ToGroundUsingRaycasting(hunter), 0.0f);
            if (!animal.Exists() || animal.Position.Z < 0.5f) return false;

            hunter.RelationshipGroup = new RelationshipGroup("HUNTER");
            hunter.BlockPermanentEvents = true;

            if (animal.Exists()) animal.IsRagdoll = true;

            if (hunter.Exists()) hunter.Heading = hunter.GetHeadingTowards(animal);

            if (Globals.Random.Next(60) <= 1) hunterVeh.InstallRandomMods();

            this.ShowCalloutAreaBlipBeforeAccepting(spawnPoint, 35f);
            this.AddMinimumDistanceCheck(20.0f, hunter.Position);

            
            this.CalloutMessage = "Possible illegal hunting";
            this.CalloutPosition = spawnPoint;

            int rndAudioNum = Globals.Random.Next(0, 8);
            if (rndAudioNum == 0) LSPD_First_Response.Mod.API.Functions.PlayScannerAudioUsingPosition("WE_HAVE CRIME_GUNFIRE IN_OR_ON_POSITION UNITS_RESPOND_CODE_03", spawnPoint);
            else if (rndAudioNum == 1) LSPD_First_Response.Mod.API.Functions.PlayScannerAudioUsingPosition("WE_HAVE CRIME_ANIMAL_KILL IN_OR_ON_POSITION UNITS_RESPOND_CODE_02", spawnPoint);
            else if (rndAudioNum == 2) LSPD_First_Response.Mod.API.Functions.PlayScannerAudioUsingPosition("WE_HAVE CRIME_ANIMAL_CRUELTY IN_OR_ON_POSITION UNITS_RESPOND_CODE_02", spawnPoint);
            else if (rndAudioNum == 3) LSPD_First_Response.Mod.API.Functions.PlayScannerAudioUsingPosition("WE_HAVE CRIME_UNAUTHORIZED_HUNTING IN_OR_ON_POSITION UNITS_RESPOND_CODE_02", spawnPoint);
            else if (rndAudioNum == 4) LSPD_First_Response.Mod.API.Functions.PlayScannerAudioUsingPosition("WE_HAVE CRIME_HUNTING_AN_ENDANGERED_SPECIES IN_OR_ON_POSITION UNITS_RESPOND_CODE_02", spawnPoint);
            else if (rndAudioNum == 5) LSPD_First_Response.Mod.API.Functions.PlayScannerAudioUsingPosition("WE_HAVE CRIME_HUNTING_WITHOUT_A_PERMIT IN_OR_ON_POSITION UNITS_RESPOND_CODE_02", spawnPoint);
            else if (rndAudioNum == 6) LSPD_First_Response.Mod.API.Functions.PlayScannerAudioUsingPosition("WE_HAVE CRIME_KILLING_ANIMALS IN_OR_ON_POSITION UNITS_RESPOND_CODE_02", spawnPoint);
            else if (rndAudioNum == 7) LSPD_First_Response.Mod.API.Functions.PlayScannerAudioUsingPosition("WE_HAVE CRIME_SHOOTING_AT_ANIMALS IN_OR_ON_POSITION UNITS_RESPOND_CODE_02", spawnPoint);

            return base.OnBeforeCalloutDisplayed();
        }

        
        public override bool OnCalloutAccepted()
        {
            animal.Kill();

            animalBlip = animal.AttachBlip();
            animalBlip.Sprite = BlipSprite.Hunting;
            animalBlip.Color = Color.GreenYellow;
            animalBlip.EnableRoute(Color.Green);
            animalBlip.Scale = 1.3f;

            if (rndAimAtAnimal == 1) NativeFunction.CallByName<uint>("TASK_AIM_GUN_AT_ENTITY", hunter, animal, -1, true);
            state = EIllegalHuntingState.EnRoute;

            return base.OnCalloutAccepted();
        }

        /// <summary>
        /// If you don't accept the callout this will be called, we clear anything we spawned here to prevent it staying in the game
        /// </summary>
        public override void OnCalloutNotAccepted()
        {
            breakForceEnd = true;

            base.OnCalloutNotAccepted();

            if (hunter.Exists()) hunter.Delete();
            if (hunterVeh.Exists()) hunterVeh.Delete();
            if (animal.Exists()) animal.Delete();
        }

        //This is where it all happens, run all of your callouts logic here
        public override void Process()
        {
            //Game.LogTrivial("[Wilderness Callouts | IllegalHunting] Process()");
            if (state == EIllegalHuntingState.EnRoute && Vector3.Distance(Game.LocalPlayer.Character.Position, hunter.Position) < 12.0f)
            {
                state = EIllegalHuntingState.OnScene;
                if (rndAimAtAnimal == 1) NativeFunction.CallByName<uint>("TASK_SHOOT_AT_ENTITY", hunter, animal, 1000, (uint)Rage.FiringPattern.SingleShot);
                OnScene();
            }

            if (!hunter.Exists() || hunter.IsDead || Functions.IsPedArrested(hunter))
            {
            }
            else if (state == EIllegalHuntingState.End && !isPursuitRunning)
            {
                this.End();
            }

            base.Process();
        }
        
        
        public override void End()
        {
            breakForceEnd = true;

            if (scenario == EScenario.License && ((hunter.Exists() && (hunter.IsAlive && !Functions.IsPedArrested(hunter) && !Functions.IsPedGettingArrested(hunter))) && (hunterVeh.Exists() && hunterVeh.IsAlive)))
            {
                GameFiber.StartNew(delegate 
                {
                NativeFunction.CallByName<uint>("TASK_GO_TO_ENTITY", hunter, hunterVeh, -1, 5.0f, 1.0f, 0, 0);
                while (Vector3.Distance(hunter.Position, hunterVeh.Position) > 6.0f)
                    GameFiber.Yield();

                hunter.Tasks.Clear();
                GameFiber.Sleep(200);
                hunter.Tasks.EnterVehicle(hunterVeh, -1).WaitForCompletion(10000);
                if (hunter.IsInVehicle(hunterVeh, false)) hunter.Tasks.CruiseWithVehicle(hunterVeh, 20.0f, VehicleDrivingFlags.Normal);
                if (hunter.Exists()) hunter.Dismiss();
                });
            }
            else if (hunter.Exists()) hunter.Dismiss();

            if (animalBlip.Exists()) animalBlip.Delete();
            if (animal.Exists()) animal.Dismiss();
            if (hunterVeh.Exists()) hunterVeh.Dismiss();

            base.End();
        }


        protected override void CleanUp()
        {
            // TODO: implement CleanUp()
        }


        public void OnScene() // ON SCENE, STARTS DIALOGUE
        {
            Logger.LogTrivial(this.GetType().Name, "OnScene()");

            GameFiber.StartNew(delegate
            {
                while (true)
                {
                    GameFiber.Yield();

                    Game.DisplayHelp("Press ~b~" + Controls.PrimaryAction.ToUserFriendlyName() + "~w~ to ask for the hunting license", 10);

                    if (Controls.PrimaryAction.IsJustPressed())
                    {
                        hunter.Tasks.AchieveHeading(hunter.GetHeadingTowards(Game.LocalPlayer.Character));
                        hunter.LookAtEntity(Game.LocalPlayer.Character, 60000);

                        Game.LocalPlayer.Character.PlayAmbientSpeech(Globals.Random.Next(2) == 1 ?Globals.Random.Next(2) == 1 ? Speech.KIFFLOM_GREET : Speech.GENERIC_HI : Speech.GENERIC_HOWS_IT_GOING);
                        Game.DisplaySubtitle("~b~" + Settings.General.Name + ": ~w~" + policeGreetings.GetRandomElement(), 2000);

                        GameFiber.Wait(2000);

                        Game.DisplaySubtitle("~b~" + Settings.General.Name + ": ~w~" + policeQuestions.GetRandomElement(), 2500);

                        GameFiber.Wait(2250);

                        StartScenario();

                        break;
                    }

                    if (breakForceEnd) break;

                }
            });
        }


        public void StartScenario() // STARTS THE SCENARIO
        {
            Logger.LogTrivial(this.GetType().Name, "StartScenario()");

            GameFiber.StartNew(delegate
            {
                int rndScenario = MathHelper.GetRandomInteger(101);


                if (rndScenario <= 7)       // Shoot
                {
                    Shoot();
                }
                else if (rndScenario > 7 && rndScenario < 15)    // No license and stay
                {
                    NoLicenseStay(); 
                }
                else if (rndScenario >= 15 && rndScenario < 50)   // No license and flee
                {
                    NoLicenseFlee();
                }
                else            // License
                {
                    License();
                }
            });
        }


        // SCENARIOS 

        public void Shoot()     // SHOOT SCENARIO
        {
            Logger.LogTrivial(this.GetType().Name, "Shoot()");
            scenario = EScenario.Shoot;
            GameFiber.StartNew(delegate
            {
                if (!licenseHasBeenGiven) Game.DisplaySubtitle("~b~Hunter: ~w~" + hunterNoLicenseAndAttackAnswers.GetRandomElement(), 2500);
                else if (licenseHasBeenGiven) Game.DisplaySubtitle("~b~Hunter: ~w~" + hunterLicenseSuspended.GetRandomElement(), 2500);

                hunter.PlayAmbientSpeech(Globals.Random.Next(2) == 1 ? Globals.Random.Next(2) == 1 ? Speech.GENERIC_FUCK_YOU : Speech.GENERIC_INSULT_HIGH : Speech.GENERIC_INSULT_MED);

                Game.SetRelationshipBetweenRelationshipGroups("HUNTER", "COP", Relationship.Hate);
                Game.SetRelationshipBetweenRelationshipGroups("HUNTER", "PLAYER", Relationship.Hate);

                this.pursuit = LSPD_First_Response.Mod.API.Functions.CreatePursuit();
                LSPD_First_Response.Mod.API.Functions.AddPedToPursuit(this.pursuit, this.hunter);

                isPursuitRunning = true;

                while (true)
                {
                    GameFiber.Yield();
                    if (breakForceEnd) break;

                    if (hunter.Exists() && hunter.IsAlive && !hunter.IsInAnyVehicle(false) && !LSPD_First_Response.Mod.API.Functions.IsPedGettingArrested(hunter) && !LSPD_First_Response.Mod.API.Functions.IsPedArrested(hunter))
                        hunter.AttackPed(Game.LocalPlayer.Character);

                    state = EIllegalHuntingState.End;
                }
            });
        }

        public void NoLicenseStay()     // NO LICENSE AND STAY SCENARIO
        {
            Logger.LogTrivial(this.GetType().Name, "NoLicenseStay()");
            scenario = EScenario.NoLicenseStay;
            hunter.PlayAmbientSpeech(Globals.Random.Next(2) == 1 ? Speech.GENERIC_CURSE_HIGH : Speech.GENERIC_CURSE_MED);
            GameFiber.StartNew(delegate
            {
                if (!licenseHasBeenGiven) Game.DisplaySubtitle("~b~Hunter: ~w~" + hunterNoLicenseAndStayAnswers.GetRandomElement(), 2500);
                else if (licenseHasBeenGiven) Game.DisplaySubtitle("~b~Hunter: ~w~" + hunterLicenseSuspended.GetRandomElement(), 2500);

                isPursuitRunning = false;

                Game.DisplayHelp("Press " + Controls.ForceCalloutEnd.ToUserFriendlyName() + " to finish the callout");
            });
        }
            
        public void NoLicenseFlee()     // NO LICENSE AND FLEE SCENARIO
        {
            Logger.LogTrivial(this.GetType().Name, "NoLicenseFlee()");
            scenario = EScenario.NoLicenseFlee;
            GameFiber.StartNew(delegate
            {
                Game.SetRelationshipBetweenRelationshipGroups("HUNTER", "COP", Relationship.Dislike);
                Game.SetRelationshipBetweenRelationshipGroups("HUNTER", "PLAYER", Relationship.Dislike);
                if (!licenseHasBeenGiven) Game.DisplaySubtitle("~b~Hunter: ~w~" + hunterNoLicenseAndFleeAnswers.GetRandomElement(), 2500);
                else if (licenseHasBeenGiven) Game.DisplaySubtitle("~b~Hunter: ~w~" + hunterLicenseSuspended.GetRandomElement(), 2500);

                hunter.PlayAmbientSpeech(Globals.Random.Next(2) == 1 ? Globals.Random.Next(2) == 1 ? Globals.Random.Next(2) == 1 ? Globals.Random.Next(2) == 1 ? Speech.GENERIC_CURSE_HIGH : Speech.GENERIC_CURSE_MED : Speech.GENERIC_FUCK_YOU : Speech.GENERIC_INSULT_HIGH : Speech.GENERIC_INSULT_MED);

                hunter.EnterVehicle(hunterVeh, 8000, EVehicleSeats.Driver, 2.0f, 1);

                while (true)
                {
                    GameFiber.Yield();
                    if (breakForceEnd || !hunter.Exists() ||hunter.IsInAnyVehicle(false)|| hunter.IsDead || Functions.IsPedArrested(hunter)) break;
                }

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
                        if (!hunterVeh.Model.IsBike || !hunterVeh.Model.IsBicycle) driveFlags = (VehicleDrivingFlags)1076;
                        else driveFlags = (VehicleDrivingFlags)786468;
                        break;
                    default:
                        break;
                }
                if (hunter.Exists())
                {
                    if (hunter.IsInAnyVehicle(false) && hunter.IsAlive) hunter.Tasks.CruiseWithVehicle(hunterVeh, 200.0f, driveFlags);


                    this.pursuit = LSPD_First_Response.Mod.API.Functions.CreatePursuit();
                    LSPD_First_Response.Mod.API.Functions.AddPedToPursuit(this.pursuit, this.hunter);

                    isPursuitRunning = true;
                }
                state = EIllegalHuntingState.End;
            });
        }

        public void License()       // LICENSE SCENARIO
        {
            Logger.LogTrivial(this.GetType().Name, "License()");
            scenario = EScenario.License;
            GameFiber.StartNew(delegate
            {
                Game.DisplaySubtitle("~b~Hunter: ~w~" + hunterLicenseAnswers.GetRandomElement(), 2500);

                hunter.Tasks.PlayAnimation("mp_WildernessCallouts.Common", "givetake1_a", 2.5f, AnimationFlags.None);

                isPursuitRunning = false;

                WildernessCallouts.Common.HuntingLicense(hunter);

                while (true)
                {
                    GameFiber.Yield();

                    if (breakForceEnd) break;

                    Game.DisplayHelp("Press ~b~" + Controls.PrimaryAction.ToUserFriendlyName() + "~w~ to let the hunter leave~n~Press ~r~" + Controls.SecondaryAction.ToUserFriendlyName() + "~w~ if the license is invalid");

                    if (Controls.PrimaryAction.IsJustPressed())
                    {
                        Game.DisplaySubtitle("~b~" + Settings.General.Name + ": ~w~" + policeLicenseGood.GetRandomElement(), 2500);
                        state = EIllegalHuntingState.End;
                        hunter.PlayAmbientSpeech(Globals.Random.Next(2) == 1 ? Speech.GENERIC_THANKS : Speech.GENERIC_BYE);
                        break;
                    }
                    else if (Controls.SecondaryAction.IsJustPressed())
                    {
                        Game.DisplaySubtitle("~b~" + Settings.General.Name + ": ~w~" + policeLicenseBad.GetRandomElement(), 2500);
                        licenseHasBeenGiven = true;

                        GameFiber.Wait(2250);

                        int rndState = Globals.Random.Next(101);

                        if (rndState <= 8) Shoot();
                        else if (rndState >= 30 && rndState < 55) NoLicenseFlee();
                        else NoLicenseStay();

                        break;
                    }

                }
            });
        }


        public enum EScenario
        {
            Shoot,
            NoLicenseStay,
            NoLicenseFlee,
            License,
        }



        // ENUM
        public enum EIllegalHuntingState
        {
            EnRoute,
            OnScene,
            Scenario,
            End,
        }
    }
}

