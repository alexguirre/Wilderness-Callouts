namespace WildernessCallouts.Callouts
{
    using Rage;
    using Rage.Native;
    using LSPD_First_Response.Mod.Callouts;
    using WildernessCallouts.Types;
    using System;
    using System.Linq;
    using System.Collections.Generic;

    [CalloutInfo("OfficerNeedsTransport", CalloutProbability.Medium)]
    internal class OfficerNeedsTransport : CalloutBase
    {
        static Tuple<AnimationDictionary, string>[] WaitingAnims =
        {
            new Tuple<AnimationDictionary, string>("oddjobs@assassinate@construction@", "unarmed_fold_arms"),
            new Tuple<AnimationDictionary, string>("oddjobs@bailbond_surf_farm", "base"),
            new Tuple<AnimationDictionary, string>("random@car_thief@victimpoints_ig_3", "arms_waving"),
            new Tuple<AnimationDictionary, string>("friends@frt@ig_1", "trevor_impatient_wait_1"),
            new Tuple<AnimationDictionary, string>("friends@frt@ig_1", "trevor_impatient_wait_2"),
            new Tuple<AnimationDictionary, string>("friends@frt@ig_1", "trevor_impatient_wait_3"),
            new Tuple<AnimationDictionary, string>("friends@frt@ig_1", "trevor_impatient_wait_4"),
            new Tuple<AnimationDictionary, string>("missbigscore2aig_4", "wait_idle_a"),
            new Tuple<AnimationDictionary, string>("missbigscore2aig_4", "wait_idle_b"),
            new Tuple<AnimationDictionary, string>("missbigscore2aig_4", "wait_idle_c"),
            new Tuple<AnimationDictionary, string>("missbigscore2aig_4", "wait_idle_d"),
        };

        Tuple<AnimationDictionary, string> animUsed = null;
        Tuple<AnimationDictionary, string> animUsed2 = null;
        SpawnPoint spawnUsed = null;
        Vehicle policeVehicle = null;
        Ped policePed = null;
        Ped policePed2 = null;
        bool createSecondPed = false;
        Blip policePedBlip = null;
        Blip policeStationBlip = null;
        Vehicle playerVeh = null;
        string voiceUsed = "s_m_y_cop_01_white_full_01";
        string voiceUsed2 = "s_m_y_cop_01_white_full_01";
        EOfficerNeedsTransportState state;

        public override bool OnBeforeCalloutDisplayed()
        {
            spawnUsed = VehicleSpawns.GetRandomElement(true);
            int timesToTryGetSP = 75;
            while ((Vector3.Distance(spawnUsed.Position, Game.LocalPlayer.Character.Position) > 1250.0f || Vector3.Distance(spawnUsed.Position, Game.LocalPlayer.Character.Position) < 30.0f) && timesToTryGetSP > 0)
            {
                GameFiber.Yield();
                spawnUsed = VehicleSpawns.GetRandomElement();
                timesToTryGetSP--;
            }
            if (Vector3.Distance(spawnUsed.Position, Game.LocalPlayer.Character.Position) > 1250.0f || Vector3.Distance(spawnUsed.Position, Game.LocalPlayer.Character.Position) < 30.0f) return false;

            policeVehicle = SpawnCorrectPoliceVehicle(spawnUsed.Position, spawnUsed.Heading);
            if (policeVehicle == null || !policeVehicle.Exists()) return false;

            for (int i = 0; i < Globals.Random.Next(5, 25); i++)
                policeVehicle.Deform(new Vector3(MathHelper.GetRandomSingle(-1.5f, 1.5f), MathHelper.GetRandomSingle(-1.5f, 1.5f), MathHelper.GetRandomSingle(-1.5f, 1.5f)), 6.75f, 2175.0f);
            policeVehicle.IsEngineOn = true;
            policeVehicle.EngineHealth = MathHelper.GetRandomSingle(390.0f, 10.0f);
            policeVehicle.DirtLevel = MathHelper.GetRandomSingle(0.0f, 15.0f);
            policeVehicle.IndicatorLightsStatus = VehicleIndicatorLightsStatus.Both;
            if (Globals.Random.Next(2) == 1) NativeFunction.Natives.SET_VEHICLE_DOOR_OPEN(policeVehicle, 4, false, true);
            if (Globals.Random.Next(3) == 1) policeVehicle.IsSirenOn = true;


            createSecondPed = (policeVehicle.Model != (Model)"policeb" && Globals.Random.Next(11) <= 6);

            if (createSecondPed)
            {
                policePed = SpawnCorrectPedForVehicle(policeVehicle, new Vector3(0.95f, -5.225f, 0f));
                policePed2 = SpawnCorrectPedForVehicle(policeVehicle, new Vector3(-1.0f, -5.225f, 0f));
                if (policePed == null || !policePed.Exists()) return false;
                if (policePed2 == null || !policePed2.Exists()) return false;
                animUsed2 = WaitingAnims.GetRandomElement();
                voiceUsed2 = GetCorrectVoiceForPedModel(policePed2.Model);
                //Game.DisplayNotification("Ped2 Voice: " + voiceUsed2);
                policePed2.Voice = voiceUsed2;
                policePed2.RelationshipGroup = "COP";
                policePed2.BlockPermanentEvents = true;
            }
            else 
            {
                policePed = SpawnCorrectPedForVehicle(policeVehicle, new Vector3(0f, -5.225f, 0f));
                if (policePed == null || !policePed.Exists()) return false;
            }

            animUsed = WaitingAnims.GetRandomElement();

            voiceUsed = GetCorrectVoiceForPedModel(policePed.Model);
            policePed.Voice = voiceUsed;
            //Game.DisplayNotification("Ped Voice: " + voiceUsed);
            policePed.RelationshipGroup = "COP";
            policePed.BlockPermanentEvents = true;

            this.CalloutMessage = "Officer needs transport";
            this.CalloutPosition = policeVehicle.Position;
            this.ShowCalloutAreaBlipBeforeAccepting(this.CalloutPosition, 50.0f);
            this.AddMinimumDistanceCheck(20.0f, policeVehicle.Position);

            LSPD_First_Response.Mod.API.Functions.PlayScannerAudioUsingPosition("WE_HAVE CRIME_OFFICER_NEEDS_TRANSPORT IN_OR_ON_POSITION UNITS_RESPOND_CODE_02", this.CalloutPosition);

            return base.OnBeforeCalloutDisplayed();
        }

        public override bool OnCalloutAccepted()
        {
            state = EOfficerNeedsTransportState.EnRoute;
            policePed.Tasks.PlayAnimation(animUsed.Item1, animUsed.Item2, 2.0f, AnimationFlags.Loop);
            if (createSecondPed) policePed2.Tasks.PlayAnimation(animUsed2.Item1, animUsed2.Item2, 2.0f, AnimationFlags.Loop);
            policePedBlip = new Blip(policePed);
            if (policePedBlip == null || !policePedBlip.Exists()) return false;

            policePedBlip.Sprite = BlipSprite.Friend;
            policePedBlip.Color = System.Drawing.Color.LightBlue;
            policePedBlip.EnableRoute(System.Drawing.Color.LightBlue);

            return base.OnCalloutAccepted();
        }

        public override void OnCalloutNotAccepted()
        {
            if (policePed != null && policePed.Exists()) policePed.Delete();
            if (policePed2 != null && policePed2.Exists()) policePed2.Delete();
            if (policeVehicle != null && policeVehicle.Exists()) policeVehicle.Delete();
            if (policePedBlip != null && policePedBlip.Exists()) policePedBlip.Delete();
            if (policeStationBlip != null && policeStationBlip.Exists()) policeStationBlip.Delete();

            base.OnCalloutNotAccepted();
        }

        public override void Process()
        {
            if (Vector3.Distance(Game.LocalPlayer.Character.Position, policePed.Position) < 37.5f && !policePed.IsPlayingAnimation(animUsed.Item1, animUsed.Item2) && state == EOfficerNeedsTransportState.EnRoute)
                policePed.Tasks.PlayAnimation(animUsed.Item1, animUsed.Item2, 2.0f, AnimationFlags.Loop);

            if (Vector3.Distance(Game.LocalPlayer.Character.Position, policePed.Position) < 37.5f && createSecondPed && !policePed2.IsPlayingAnimation(animUsed2.Item1, animUsed2.Item2) && state == EOfficerNeedsTransportState.EnRoute)
                policePed2.Tasks.PlayAnimation(animUsed2.Item1, animUsed2.Item2, 2.0f, AnimationFlags.Loop);

            if (state == EOfficerNeedsTransportState.EnRoute && Vector3.Distance(Game.LocalPlayer.Character.Position, policePed.Position) < 10.0f)
            {
                state = EOfficerNeedsTransportState.OfficerWaitingToEnter;
                policePed.Tasks.Clear();
                policePed.Tasks.AchieveHeading(policePed.GetHeadingTowards(Game.LocalPlayer.Character)).WaitForCompletion(1000);
                policePed.PlayAmbientSpeech(null, Speech.GENERIC_HI, 0, SpeechModifier.Standard);
                if (createSecondPed)
                {
                    policePed2.Tasks.Clear();
                    policePed2.Tasks.AchieveHeading(policePed2.GetHeadingTowards(Game.LocalPlayer.Character)).WaitForCompletion(1000);
                    policePed2.PlayAmbientSpeech(null, Speech.GENERIC_HI, 0, SpeechModifier.Standard);
                }
                Game.DisplayHelp("Press ~b~" + Controls.PrimaryAction.ToUserFriendlyName() + "~s~ while in your vehicle to tell the officer to enter", 10000);
            }

            if (state == EOfficerNeedsTransportState.OfficerWaitingToEnter && Controls.PrimaryAction.IsJustPressed() && Game.LocalPlayer.Character.IsInAnyVehicle(false))
            {
                GetSeatsAndMakePedsEnter();
            }
            if (state == EOfficerNeedsTransportState.OfficerWaitingToEnter)
                policePed.Tasks.AchieveHeading(policePed.GetHeadingTowards(Game.LocalPlayer.Character));

            if (state == EOfficerNeedsTransportState.OfficerWaitingToEnter && createSecondPed)
                policePed2.Tasks.AchieveHeading(policePed2.GetHeadingTowards(Game.LocalPlayer.Character));

            if (state == EOfficerNeedsTransportState.OfficerEntering && policePed.IsInAnyVehicle(false))
            {
                state = EOfficerNeedsTransportState.OfficerOnVehicle;
                policeStationBlip = new Blip(GetClosestPoliceStation(Game.LocalPlayer.Character.Position));
                policeStationBlip.EnableRoute(policeStationBlip.Color);
                GameFiber.StartNew(delegate { ConversationsController(); });
                Game.DisplaySubtitle("Go to the ~y~closest station", 7500);
            }

            if (state == EOfficerNeedsTransportState.OfficerOnVehicle && Vector3.Distance(Game.LocalPlayer.Character.Position, policeStationBlip.Position) < 12.5f && Game.LocalPlayer.Character.Speed < 0.5f)
            {
                state = EOfficerNeedsTransportState.AtPoliceStation;
                if (createSecondPed)
                {
                    GameFiber.StartNew(delegate
                    {
                        policePed2.Tasks.LeaveVehicle(LeaveVehicleFlags.None).WaitForCompletion();
                        policePed2.Tasks.AchieveHeading(policePed2.GetHeadingTowards(Game.LocalPlayer.Character)).WaitForCompletion(1250);
                        policePed2.PlayAmbientSpeech(null, Speech.GENERIC_BYE, 0, SpeechModifier.Standard);
                        GameFiber.Sleep(1300);
                        policePed2.Tasks.Wander();
                    });
                }
                policePed.Tasks.LeaveVehicle(LeaveVehicleFlags.None).WaitForCompletion();
                policePed.Tasks.AchieveHeading(policePed.GetHeadingTowards(Game.LocalPlayer.Character)).WaitForCompletion(1250);
                policePed.PlayAmbientSpeech(null, Speech.GENERIC_BYE, 0, SpeechModifier.Standard);
                GameFiber.Sleep(1300);
                policePed.Tasks.Wander();
                GameFiber.Sleep(1750);
                this.End();
            }

            
            //add logic

            base.Process();
        }

        public override void End()
        {
            if (policePed != null && policePed.Exists())
            {
                if (policePed.IsInAnyVehicle(false))
                {
                    policePed.Tasks.LeaveVehicle(LeaveVehicleFlags.None).WaitForCompletion();
                    policePed.Tasks.Wander();
                }
                policePed.Dismiss();
            }
            if (policePed2 != null && policePed2.Exists())
            {
                if (policePed2.IsInAnyVehicle(false))
                {
                    policePed2.Tasks.LeaveVehicle(LeaveVehicleFlags.None).WaitForCompletion();
                    policePed2.Tasks.Wander();
                }
                
                policePed2.Dismiss();
            }
            if (policeVehicle != null && policeVehicle.Exists()) policeVehicle.Dismiss();
            if (policePedBlip != null && policePedBlip.Exists()) policePedBlip.Delete();
            if (policeStationBlip != null && policeStationBlip.Exists()) policeStationBlip.Delete();

            base.End();
        }


        protected override void CleanUp()
        {
            // TODO: implement CleanUp()
        }

        public void GetSeatsAndMakePedsEnter()
        {
            playerVeh = Game.LocalPlayer.Character.CurrentVehicle;

            if (playerVeh.FreePassengerSeatsCount < 1)
            {
                if (createSecondPed)
                {
                    Game.DisplayHelp("You need a vehicle with at least two free seats", 7500);
                    return;
                }
                else
                {
                    Game.DisplayHelp("You need a vehicle with at least one free seat", 7500);
                    return;
                }
            }
            else if (playerVeh.FreePassengerSeatsCount < 2 && createSecondPed)
            {
                Game.DisplayHelp("You need a vehicle with at least two free seats", 7500);
                return;
            }

            state = EOfficerNeedsTransportState.OfficerEntering;
            GameFiber.StartNew(delegate
            {
                policePed.PlayAmbientSpeech(null, Speech.GENERIC_THANKS, 0, SpeechModifier.Standard);
                List<EVehicleSeats> freeSeats = playerVeh.GetFreeSeats();
                //int seat = playerVeh.GetFreePassengerSeatIndex().GetValueOrDefault(0);
                EVehicleSeats seat = freeSeats[0];

                if (createSecondPed)
                {
                    GameFiber.StartNew(delegate
                    {
                        //int seat2 = seat + 1;
                        EVehicleSeats seat2 = freeSeats[1];
                        policePed2.PlayAmbientSpeech(null, Speech.GENERIC_THANKS, 0, SpeechModifier.Standard);

                        NativeFunction.Natives.TASK_GO_TO_ENTITY(policePed2, playerVeh, -1, 5.0f, 1.0f, 0, 0);
                        while (Vector3.Distance(policePed2.Position, playerVeh.Position) > 6.0f)
                            GameFiber.Yield();

                        policePed2.Tasks.Clear();
                        GameFiber.Sleep(200);
                        policePed2.Tasks.EnterVehicle(playerVeh, (int)seat2).WaitForCompletion(17500);
                        if (!policePed2.IsInVehicle(playerVeh, false))
                            policePed2.WarpIntoVehicle(playerVeh, (int)seat2);
                    });
                }

                NativeFunction.Natives.TASK_GO_TO_ENTITY(policePed, playerVeh, -1, 5.0f, 1.0f, 0, 0);
                while (Vector3.Distance(policePed.Position, playerVeh.Position) > 6.0f)
                    GameFiber.Yield();

                policePed.Tasks.Clear();
                GameFiber.Sleep(200);
                policePed.Tasks.EnterVehicle(playerVeh, (int)seat).WaitForCompletion(17500);
                if (!policePed.IsInVehicle(playerVeh, false))
                    policePed.WarpIntoVehicle(playerVeh, (int)seat);
            });
        }

        public void ConversationsController()
        {
            while (state == EOfficerNeedsTransportState.OfficerOnVehicle)
            {
                GameFiber.Sleep(Globals.Random.Next(4875, 27500));

                if (state != EOfficerNeedsTransportState.OfficerOnVehicle) break;

                if (createSecondPed)
                {
                    if (Globals.Random.Next(21) <= 5)
                    {
                        //Game.DisplayNotification("Speech: policePed");
                        policePed.PlayAmbientSpeech(null, Speech.CHAT_STATE, 0, SpeechModifier.Standard);
                    }
                    else if (Globals.Random.Next(16) <= 4)
                    {
                        //Game.DisplayNotification("Speech: policePed2");
                        policePed2.PlayAmbientSpeech(null, Speech.CHAT_STATE, 0, SpeechModifier.Standard);
                    }
                }
                else
                {
                    if (Globals.Random.Next(18) <= 5)
                    {
                        //Game.DisplayNotification("Speech: policePed");
                        policePed.PlayAmbientSpeech(null, Speech.CHAT_STATE, 0, SpeechModifier.Standard);
                    }
                }
            }
        }


        #region StaticFunctions
        public static Ped SpawnCorrectPedForVehicle(Vehicle vehicle, Vector3 vehOffset)
        {
            Model model = "s_m_y_cop_01";
            if (vehicle.Model == new Model("police") || vehicle.Model == new Model("police2") || vehicle.Model == new Model("police3") || vehicle.Model == new Model("policet"))
                model = Globals.Random.Next(2) == 1 ? "s_m_y_cop_01" : "s_f_y_cop_01";
            else if (vehicle.Model == new Model("police4") || vehicle.Model == new Model("fbi") || vehicle.Model == new Model("fbi2"))
                model = Globals.Random.Next(2) == 1 ? Globals.Random.Next(2) == 1 ? (Model)0x4baf381c : (Model)0x7b8b434b : Globals.Random.Next(2) == 1 ? (Model)0x621e6bfd : "s_m_m_ciasec_01";
            else if (vehicle.Model == new Model("riot"))
                model = "s_m_y_swat_01";
            else if (vehicle.Model == new Model("policeb"))
                model = "s_m_y_hwaycop_01";
            else if (vehicle.Model == new Model("sheriff") || vehicle.Model == new Model("sheriff2"))
                model = Globals.Random.Next(2) == 1 ? "s_m_y_sheriff_01" : "s_f_y_sheriff_01";
            else if (vehicle.Model == new Model("pranger"))
                model = Globals.Random.Next(2) == 1 ? "s_m_y_ranger_01" : "s_f_y_ranger_01";
            //else if (vehicle.Model == new Model("polmav"))
            //    model = Globals.Random.Next(2) == 1 ? "s_m_y_pilot_01" : "s_m_m_pilot_02";

            return new Ped(model, vehicle.GetOffsetPosition(vehOffset), vehicle.Heading - 180);
        }

        public static Vehicle SpawnCorrectPoliceVehicle(Vector3 pos, float heading)
        {
            return new Vehicle(GetVehicleModelForPosition(pos), pos, heading);
        }


        public static Model GetVehicleModelForPosition(Vector3 position)
        {
            Model[] losSantosModels = { "police", "police2", "police3", "police4", "riot", "policeb", "policet", "fbi", "fbi2" };
            Model[] countyModels = { "sheriff", "sheriff2", "police4", "riot", "policeb", "pranger", "fbi", "fbi2" };

            switch (position.GetArea())
            {
                case EWorldArea.Los_Santos:
                    return losSantosModels.GetRandomElement(true);
                case EWorldArea.Blaine_County:
                    return countyModels.GetRandomElement(true);
                default:
                    return losSantosModels.GetRandomElement(true);
            }
        }

        public static string GetCorrectVoiceForPedModel(Model model)
        {
            /*MALES*/
            if (model == new Model("s_m_y_cop_01"))
            {
                string[] possibleVoices = { "s_m_y_cop_01_black_full_01", "s_m_y_cop_01_black_full_02", "s_m_y_cop_01_white_full_01", "s_m_y_cop_01_white_full_02" };
                return possibleVoices.GetRandomElement();
            }
            else if (model == new Model("s_m_y_hwaycop_01"))
            {
                string[] possibleVoices = { "s_m_y_hwaycop_01_black_full_01", "s_m_y_hwaycop_01_black_full_02", "s_m_y_hwaycop_01_white_full_01", "s_m_y_hwaycop_01_white_full_02" };
                return possibleVoices.GetRandomElement();
            }
            else if (model == new Model("s_m_y_ranger_01"))
            {
                string[] possibleVoices = { "s_m_y_ranger_01_latino_full_01", "s_m_y_ranger_01_white_full_01" };
                return possibleVoices.GetRandomElement();
            }
            else if (model == new Model("s_m_y_sheriff_01"))
            {
                string[] possibleVoices = { "s_m_y_sheriff_01_white_full_01", "s_m_y_sheriff_01_white_full_02" };
                return possibleVoices.GetRandomElement();
            }
            else if (model == new Model("s_m_y_swat_01"))
            {
                string[] possibleVoices = { "s_m_y_swat_01_white_full_01", "s_m_y_swat_01_white_full_02", "s_m_y_swat_01_white_full_03", "s_m_y_swat_01_white_full_04" };
                return possibleVoices.GetRandomElement();
            }
            else if (model == new Model("s_m_m_ciasec_01"))
            {
                string[] possibleVoices = { "s_m_m_ciasec_01_black_mini_01", "s_m_m_ciasec_01_black_mini_02", "s_m_m_ciasec_01_white_mini_01", "s_m_m_ciasec_01_white_mini_02" };
                return possibleVoices.GetRandomElement();
            }
            else if (model == new Model(0x7b8b434b) || model == new Model(0x621e6bfd))
            {
                return maleVoices.GetRandomElement();
            }
            /*FEMALES*/
            else if (model == new Model("s_f_y_cop_01"))
            {
                string[] possibleVoices = { "s_f_y_cop_01_black_full_01", "s_f_y_cop_01_black_full_02", "s_f_y_cop_01_white_full_01", "s_f_y_cop_01_white_full_02" };
                return possibleVoices.GetRandomElement();
            }
            else if (model == new Model("s_f_y_ranger_01"))
            {
                string[] possibleVoices = { "s_f_y_ranger_01_white_mini_01" };
                return possibleVoices.GetRandomElement();
            }
            else if (model == new Model("s_f_y_sheriff_01"))
            {
                string[] possibleVoices = { "s_f_y_ranger_01_white_mini_01", "s_f_y_cop_01_black_full_01", "s_f_y_cop_01_black_full_02", "s_f_y_cop_01_white_full_01", "s_f_y_cop_01_white_full_02" };
                return possibleVoices.GetRandomElement();
            }
            else if (model == new Model(0x4baf381c))
            {
                return femaleVoices.GetRandomElement();
            }
            /*DEFAULT*/
            else return maleVoices.GetRandomElement();
        }

        public static string[] maleVoices = 
        {
            "s_m_y_cop_01_black_full_01",
            "s_m_y_cop_01_black_full_02",
            "s_m_y_cop_01_white_full_01",
            "s_m_y_cop_01_white_full_02",
            "s_m_y_hwaycop_01_black_full_01",
            "s_m_y_hwaycop_01_black_full_02",
            "s_m_y_hwaycop_01_white_full_01",
            "s_m_y_hwaycop_01_white_full_02",
            "s_m_y_ranger_01_latino_full_01",
            "s_m_y_ranger_01_white_full_01",
            "s_m_y_sheriff_01_white_full_01",
            "s_m_y_sheriff_01_white_full_02",
            "s_m_y_swat_01_white_full_01",
            "s_m_y_swat_01_white_full_02",
            "s_m_y_swat_01_white_full_03",
            "s_m_y_swat_01_white_full_04",
            "s_m_m_ciasec_01_black_mini_01",
            "s_m_m_ciasec_01_black_mini_02",
            "s_m_m_ciasec_01_white_mini_01",
            "s_m_m_ciasec_01_white_mini_02",
        };
        public static string[] femaleVoices = 
        {
            "s_f_y_cop_01_black_full_01",
            "s_f_y_cop_01_black_full_02",
            "s_f_y_cop_01_white_full_01",
            "s_f_y_cop_01_white_full_02",
            "s_f_y_ranger_01_white_mini_01",
        };

        public static List<SpawnPoint> VehicleSpawns = new List<SpawnPoint>()
        {
#region Spawns
new SpawnPoint(new Vector3(29.04396f, -1485.481f, 29.21863f), 230.0004f),
new SpawnPoint(new Vector3(124.8446f, -1484.164f, 28.79203f), 213.0123f),
new SpawnPoint(new Vector3(376.826f, -1295.398f, 32.19772f), 288.7279f),
new SpawnPoint(new Vector3(1605.79f, -2416.795f, 88.40086f), 358.6046f),
new SpawnPoint(new Vector3(1671.878f, -2214.356f, 110.2897f), 11.16918f),
new SpawnPoint(new Vector3(1727.121f, -1936.658f, 116.3609f), 334.7698f),
new SpawnPoint(new Vector3(1901.372f, -1381.177f, 136.0604f), 332.3181f),
new SpawnPoint(new Vector3(2012.148f, -985.7313f, 83.69803f), 323.799f),
new SpawnPoint(new Vector3(2240.254f, -712.1997f, 67.5676f), 335.4474f),
new SpawnPoint(new Vector3(2283.318f, -728.0878f, 66.59523f), 270.2435f),
new SpawnPoint(new Vector3(2321.014f, -708.1335f, 65.31657f), 307.1065f),
new SpawnPoint(new Vector3(2511.224f, -672.16f, 60.20497f), 276.2107f),
new SpawnPoint(new Vector3(2507.32f, -667.0079f, 61.17017f), 310.5197f),
new SpawnPoint(new Vector3(2525.703f, -548.4584f, 67.03138f), 20.47106f),
new SpawnPoint(new Vector3(2478.161f, -135.37f, 89.19698f), 336.1118f),
new SpawnPoint(new Vector3(2605.919f, 313.5399f, 107.3027f), 351.6353f),
new SpawnPoint(new Vector3(2595.979f, 341.6381f, 108.0272f), 358.8599f),
new SpawnPoint(new Vector3(2557.665f, 461.6347f, 108.1058f), 181.5113f),
new SpawnPoint(new Vector3(2496.684f, 731.0614f, 99.82991f), 193.5587f),
new SpawnPoint(new Vector3(2493.382f, 718.7096f, 100.9585f), 163.3792f),
new SpawnPoint(new Vector3(2467.011f, 832.7526f, 93.39806f), 175.0691f),
new SpawnPoint(new Vector3(2433.067f, 1003.608f, 85.27689f), 38.96442f),
new SpawnPoint(new Vector3(2410.525f, 1044.156f, 81.68114f), 5.103662f),
new SpawnPoint(new Vector3(2386.606f, 1065.723f, 81.21111f), 75.07388f),
new SpawnPoint(new Vector3(135.1332f, 1670.49f, 228.2199f), 28.12401f),
new SpawnPoint(new Vector3(-12.80577f, 1772.023f, 218.4007f), 18.00996f),
new SpawnPoint(new Vector3(119.2413f, 1978.951f, 160.2898f), 338.6121f),
new SpawnPoint(new Vector3(95.19112f, 2084.32f, 144.3904f), 262.4541f),
new SpawnPoint(new Vector3(188.2441f, 2110.473f, 122.5814f), 83.8672f),
new SpawnPoint(new Vector3(-1149.642f, 2644.764f, 16.06059f), 132.9647f),
new SpawnPoint(new Vector3(-1100.78f, 2667.777f, 18.4646f), 304.7761f),
new SpawnPoint(new Vector3(-884.762f, 2738.511f, 23.08664f), 236.9763f),
new SpawnPoint(new Vector3(-844.2887f, 2764.271f, 22.47485f), 20.85813f),
new SpawnPoint(new Vector3(-729.0281f, 2792.451f, 25.88508f), 301.8732f),
new SpawnPoint(new Vector3(-497.962f, 2852.601f, 33.47964f), 40.81293f),
new SpawnPoint(new Vector3(-233.4895f, 2876.186f, 46.26438f), 251.2714f),
new SpawnPoint(new Vector3(-249.7884f, 2761.724f, 52.32357f), 164.0709f),
new SpawnPoint(new Vector3(2065.241f, 5028.95f, 40.70932f), 20.33019f),
new SpawnPoint(new Vector3(1975.74f, 5069.816f, 39.20626f), 91.65002f),
new SpawnPoint(new Vector3(1957.832f, 5086.071f, 39.51029f), 40.32328f),
new SpawnPoint(new Vector3(1980.324f, 5131.103f, 42.37849f), 342.4062f),
new SpawnPoint(new Vector3(1841.531f, 5090.44f, 54.0841f), 128.3201f),
new SpawnPoint(new Vector3(1680.384f, 4942.263f, 41.82703f), 317.112f),
new SpawnPoint(new Vector3(1665.132f, 4837.94f, 41.60987f), 185.9371f),
new SpawnPoint(new Vector3(1679.206f, 4821.904f, 41.61614f), 182.3615f),
new SpawnPoint(new Vector3(1685.082f, 4715.888f, 42.54462f), 191.9872f),
new SpawnPoint(new Vector3(-725.7723f, -1203.488f, 10.20165f), 224.8459f),
new SpawnPoint(new Vector3(-633.7048f, -1349.948f, 10.23088f), 345.5819f),
new SpawnPoint(new Vector3(-451.5506f, -1396.968f, 31.65788f), 179.5257f),
new SpawnPoint(new Vector3(-288.1896f, -1192.225f, 36.73891f), 86.4196f),
new SpawnPoint(new Vector3(-324.3005f, -1188.825f, 36.82469f), 101.4439f),
new SpawnPoint(new Vector3(-404.0313f, -950.6277f, 36.66675f), 2.446798f),
new SpawnPoint(new Vector3(-420.5948f, -1007.387f, 36.75966f), 179.1773f),
new SpawnPoint(new Vector3(-435.9019f, -1196.913f, 54.36501f), 197.4193f),
new SpawnPoint(new Vector3(-441.5934f, -1190.251f, 54.54845f), 167.2692f),
new SpawnPoint(new Vector3(-326.1908f, -1222.712f, 45.79908f), 270.2148f),
new SpawnPoint(new Vector3(-117.4079f, -77.78635f, 56.06562f), 43.2281f),
new SpawnPoint(new Vector3(-222.807f, 52.21105f, 62.17604f), 350.524f),
new SpawnPoint(new Vector3(-376.435f, 252.4943f, 83.56103f), 90.89108f),
new SpawnPoint(new Vector3(-237.9239f, -208.0929f, 48.68306f), 86.93172f),
new SpawnPoint(new Vector3(-254.0214f, -176.5492f, 40.25671f), 248.5311f),
new SpawnPoint(new Vector3(-164.0971f, -190.2886f, 43.22535f), 263.3002f),
new SpawnPoint(new Vector3(-146.4457f, -214.7682f, 44.89463f), 23.44977f),
new SpawnPoint(new Vector3(-248.1629f, -215.1508f, 48.70239f), 81.49278f),
new SpawnPoint(new Vector3(-3.382632f, -356.9661f, 40.96278f), 159.4294f),
new SpawnPoint(new Vector3(-366.7174f, -644.8232f, 31.00922f), 63.87502f),
new SpawnPoint(new Vector3(-356.9233f, -702.692f, 32.07237f), 169.1718f),
new SpawnPoint(new Vector3(-250.3326f, -1008.58f, 28.70393f), 143.2913f),
new SpawnPoint(new Vector3(-234.5211f, -1059.884f, 26.3529f), 340.3673f),
new SpawnPoint(new Vector3(-254.0708f, -1114.42f, 22.78612f), 340.3092f),
new SpawnPoint(new Vector3(-248.9776f, -1151.877f, 22.612f), 266.5405f),
new SpawnPoint(new Vector3(60.77801f, -1201.188f, 29.06853f), 182.1884f),
new SpawnPoint(new Vector3(42.74577f, -1239.801f, 28.92982f), 140.153f),
new SpawnPoint(new Vector3(76.77934f, -1233.649f, 28.77388f), 358.4226f),
new SpawnPoint(new Vector3(266.3373f, -1237.735f, 37.78271f), 272.0429f),
new SpawnPoint(new Vector3(559.3621f, -1220.001f, 41.71746f), 270.3095f),
new SpawnPoint(new Vector3(578.1407f, -1202.58f, 41.58027f), 99.02876f),
new SpawnPoint(new Vector3(665.8121f, -1216.322f, 42.12179f), 273.8979f),
new SpawnPoint(new Vector3(813.3646f, -1210.317f, 45.15012f), 272.9738f),
new SpawnPoint(new Vector3(963.4483f, -1221.394f, 42.19288f), 264.0011f),
new SpawnPoint(new Vector3(1051.037f, -1618.542f, 28.88126f), 181.1558f),
new SpawnPoint(new Vector3(1119.693f, -1774.807f, 28.92708f), 24.52319f),
new SpawnPoint(new Vector3(1856.562f, 1629.843f, 81.97262f), 271.6229f),
new SpawnPoint(new Vector3(1760.03f, 1573.766f, 84.0602f), 326.9832f),
new SpawnPoint(new Vector3(1659.009f, 1410.863f, 89.19391f), 161.5303f),
new SpawnPoint(new Vector3(1477.268f, 845.1575f, 80.13512f), 163.4723f),
new SpawnPoint(new Vector3(1501.652f, 792.0765f, 77.10122f), 322.9877f),
new SpawnPoint(new Vector3(1547.424f, 860.352f, 77.0838f), 331.3596f),
new SpawnPoint(new Vector3(1655.002f, 1043.519f, 114.4042f), 309.2246f),
new SpawnPoint(new Vector3(1706.258f, 1100.666f, 122.8426f), 4.590456f),
new SpawnPoint(new Vector3(1804.667f, 1276.984f, 141.3939f), 262.1319f),
new SpawnPoint(new Vector3(1897.264f, 1316.111f, 151.983f), 138.4582f),
new SpawnPoint(new Vector3(1953.892f, 1278.384f, 173.4774f), 183.5537f),
new SpawnPoint(new Vector3(1988.6f, 1217.391f, 181.5562f), 16.48735f),
new SpawnPoint(new Vector3(1941.105f, 593.7348f, 175.234f), 184.6023f),
new SpawnPoint(new Vector3(1879.009f, 402.7355f, 160.777f), 174.1655f),
new SpawnPoint(new Vector3(1882.315f, 368.3681f, 162.1227f), 313.8433f),
new SpawnPoint(new Vector3(1864.043f, 330.9031f, 161.1839f), 189.624f),
new SpawnPoint(new Vector3(1891.222f, 307.0795f, 161.2942f), 283.0193f),
new SpawnPoint(new Vector3(1916.313f, 331.7111f, 161.0889f), 303.7202f),
new SpawnPoint(new Vector3(1825.914f, 245.1235f, 172.0796f), 327.1946f),
new SpawnPoint(new Vector3(1841.585f, 297.9098f, 160.7706f), 33.818f),
new SpawnPoint(new Vector3(1838.827f, 349.4413f, 161.1915f), 334.4989f),
new SpawnPoint(new Vector3(1678.478f, -60.9827f, 173.4199f), 215.4017f),
new SpawnPoint(new Vector3(1582.567f, -64.98247f, 160.8069f), 48.07694f),
new SpawnPoint(new Vector3(1701.597f, -72.51482f, 175.7228f), 46.15903f),
new SpawnPoint(new Vector3(1703.732f, -86.78227f, 177.4064f), 229.0073f),
new SpawnPoint(new Vector3(1670.911f, -57.99936f, 173.4241f), 22.20172f),
new SpawnPoint(new Vector3(1892.125f, -96.8222f, 192.5648f), 265.9779f),
new SpawnPoint(new Vector3(1999.272f, -45.9687f, 205.0816f), 2.980156f),
new SpawnPoint(new Vector3(2110f, 33.97027f, 215.7622f), 91.82505f),
new SpawnPoint(new Vector3(2333.994f, 1474.367f, 57.02406f), 170.0988f),
new SpawnPoint(new Vector3(2454.431f, 1506.833f, 34.60661f), 263.1316f),
new SpawnPoint(new Vector3(2548.27f, 1631.982f, 29.02765f), 1.333076f),
new SpawnPoint(new Vector3(2534.238f, 1750.836f, 25.00777f), 182.5242f),
new SpawnPoint(new Vector3(2546.519f, 2060.312f, 19.36282f), 0.3434792f),
new SpawnPoint(new Vector3(2560.931f, 2246.142f, 18.98199f), 154.8321f),
new SpawnPoint(new Vector3(2603.023f, 2297.717f, 24.03425f), 17.9502f),
new SpawnPoint(new Vector3(2622.519f, 2426.626f, 23.88212f), 339.4941f),
new SpawnPoint(new Vector3(2593.338f, 2543.794f, 31.38319f), 192.9967f),
new SpawnPoint(new Vector3(2613.614f, 3064.689f, 46.05962f), 315.8682f),
new SpawnPoint(new Vector3(2704.189f, 3210.351f, 53.53482f), 331.1164f),
new SpawnPoint(new Vector3(2664.592f, 3163.694f, 51.31176f), 138.8382f),
new SpawnPoint(new Vector3(2684.116f, 3253.822f, 54.88565f), 150.3126f),
new SpawnPoint(new Vector3(2700.842f, 3303.984f, 55.38952f), 133.9565f),
new SpawnPoint(new Vector3(2796.314f, 3410.129f, 55.39362f), 335.8336f),
new SpawnPoint(new Vector3(2934.646f, 3758.042f, 52.25245f), 344.7517f),
new SpawnPoint(new Vector3(2949.911f, 3944.667f, 51.47417f), 8.38462f),
new SpawnPoint(new Vector3(2947.3f, 4060.581f, 53.54372f), 46.37241f),
new SpawnPoint(new Vector3(2880.192f, 4215.511f, 49.66055f), 18.09651f),
new SpawnPoint(new Vector3(2707.297f, 4535.186f, 40.91897f), 5.540591f),
new SpawnPoint(new Vector3(2679.617f, 4602.607f, 40.45101f), 224.2506f),
new SpawnPoint(new Vector3(2593.338f, 4651.41f, 33.27987f), 136.4588f),
new SpawnPoint(new Vector3(2500.613f, 4552.884f, 33.519f), 312.6866f),
new SpawnPoint(new Vector3(2211.947f, 4747.69f, 40.08329f), 77.74499f),
new SpawnPoint(new Vector3(2087.933f, 4720.822f, 40.514f), 116.3099f),
new SpawnPoint(new Vector3(2033.572f, 4646.154f, 40.76891f), 314.6067f),
new SpawnPoint(new Vector3(1969.777f, 4598.229f, 39.97559f), 284.6905f),
new SpawnPoint(new Vector3(1874.735f, 4593.28f, 35.7034f), 85.1673f),
new SpawnPoint(new Vector3(1471.563f, 4532.414f, 51.80446f), 107.7142f),
new SpawnPoint(new Vector3(1353.834f, 4503.134f, 56.64389f), 122.5443f),
new SpawnPoint(new Vector3(1242.828f, 4462.275f, 53.8812f), 281.2545f),
new SpawnPoint(new Vector3(1014.123f, 4459.392f, 50.9925f), 83.87487f),
new SpawnPoint(new Vector3(932.6806f, 4460.784f, 52.16051f), 95.44364f),
new SpawnPoint(new Vector3(819.9769f, 4472.787f, 52.06711f), 14.89402f),
new SpawnPoint(new Vector3(858.3472f, 4222.806f, 50.26554f), 155.032f),
new SpawnPoint(new Vector3(823.2271f, 4236.297f, 52.43561f), 95.91883f),
new SpawnPoint(new Vector3(822.5779f, 4247.563f, 53.80591f), 217.6942f),
new SpawnPoint(new Vector3(694.2901f, 4251.355f, 54.76221f), 94.79592f),
new SpawnPoint(new Vector3(470.5067f, 4324.216f, 60.49507f), 54.09849f),
new SpawnPoint(new Vector3(452.1397f, 4342.789f, 63.05926f), 231.4869f),
new SpawnPoint(new Vector3(-52.39759f, 4555.731f, 120.5995f), 197.7329f),
new SpawnPoint(new Vector3(-54.60715f, 4572.207f, 122.4283f), 47.90086f),
new SpawnPoint(new Vector3(-181.5883f, 4665.316f, 130.0501f), 46.85508f),
new SpawnPoint(new Vector3(-361.1438f, 4818.895f, 142.665f), 240.2303f),
new SpawnPoint(new Vector3(-498.116f, 4937.672f, 146.8303f), 26.68508f),
new SpawnPoint(new Vector3(-538.6077f, 5034.943f, 128.6003f), 242.6904f),
new SpawnPoint(new Vector3(-408.6992f, 4896.396f, 190.8407f), 76.88344f),
new SpawnPoint(new Vector3(-371.9269f, 4981.102f, 204.542f), 30.64798f),
new SpawnPoint(new Vector3(-311.3697f, 4958.877f, 249.908f), 223.8936f),
new SpawnPoint(new Vector3(-297.0015f, 4972.801f, 244.7134f), 22.99679f),
new SpawnPoint(new Vector3(-426.4435f, 4888.722f, 191.024f), 73.92558f),
new SpawnPoint(new Vector3(-429.2366f, 4740.836f, 251.8253f), 75.22681f),
new SpawnPoint(new Vector3(-593.3362f, 5462.709f, 58.92444f), 85.5067f),
new SpawnPoint(new Vector3(-594.7555f, 5525.435f, 49.49923f), 310.0009f),
new SpawnPoint(new Vector3(-562.174f, 5593.432f, 46.27748f), 182.8555f),
new SpawnPoint(new Vector3(-664.9291f, 5732.294f, 23.55846f), 75.60056f),
new SpawnPoint(new Vector3(-761.4993f, 5708.808f, 20.43642f), 160.9527f),
new SpawnPoint(new Vector3(1512.644f, 6417.311f, 22.73421f), 250.519f),
new SpawnPoint(new Vector3(1625.642f, 6426.538f, 26.83821f), 70.18908f),
new SpawnPoint(new Vector3(1685.875f, 6401.303f, 31.21611f), 74.06255f),
new SpawnPoint(new Vector3(-624.7286f, 695.3298f, 150.7578f), 73.56187f),
new SpawnPoint(new Vector3(-648.9553f, 763.937f, 176.8826f), 78.44299f),
new SpawnPoint(new Vector3(-567.9982f, 769.8909f, 187.0515f), 199.2162f),
new SpawnPoint(new Vector3(-715.5521f, 846.3237f, 219.9354f), 319.4315f),
new SpawnPoint(new Vector3(-709.5059f, 921.8023f, 232.8687f), 188.9494f),
new SpawnPoint(new Vector3(-590.6293f, 855.7262f, 207.3725f), 229.7534f),
new SpawnPoint(new Vector3(-509.0522f, 664.3965f, 140.3845f), 80.83772f),
new SpawnPoint(new Vector3(-496.87f, 566.45f, 119.6506f), 224.9165f),
new SpawnPoint(new Vector3(-188.1538f, 513.199f, 135.0643f), 107.2679f),
new SpawnPoint(new Vector3(-115.2772f, 527.8898f, 144.69f), 289.7317f),
new SpawnPoint(new Vector3(135.3553f, 581.9155f, 183.7886f), 301.0012f),
new SpawnPoint(new Vector3(160.2515f, 636.8734f, 202.0961f), 318.7306f),
new SpawnPoint(new Vector3(144.8403f, 676.6776f, 207.482f), 208.3124f),
new SpawnPoint(new Vector3(115.8298f, 737.5058f, 209.0989f), 8.635352f),
new SpawnPoint(new Vector3(127.6282f, 774.2349f, 210.5833f), 320.2177f),
new SpawnPoint(new Vector3(254.1413f, 806.7555f, 195.8352f), 308.5601f),
new SpawnPoint(new Vector3(313.1024f, 846.2394f, 192.64f), 307.2505f),
new SpawnPoint(new Vector3(560.7838f, 795.5341f, 200.7292f), 231.4433f),
new SpawnPoint(new Vector3(660.8334f, 789.5416f, 205.1345f), 94.75607f),
new SpawnPoint(new Vector3(895.1952f, 980.9785f, 236.7711f), 270.7769f),
new SpawnPoint(new Vector3(1029.885f, 963.9506f, 225.7702f), 6.055867f),
new SpawnPoint(new Vector3(998.1264f, 1042.998f, 256.0514f), 16.41237f),
new SpawnPoint(new Vector3(903.0964f, 81.29275f, 78.5006f), 141.5989f),
new SpawnPoint(new Vector3(1047.585f, 212.8247f, 80.50257f), 327.8469f),
new SpawnPoint(new Vector3(1099.176f, 267.5126f, 80.56857f), 114.7493f),
new SpawnPoint(new Vector3(1156.911f, 375.7399f, 90.95883f), 43.49021f),
new SpawnPoint(new Vector3(1273.06f, 589.2339f, 80.05874f), 131.4242f),
new SpawnPoint(new Vector3(-1777.223f, -717.079f, 10.0287f), 227.2491f),
new SpawnPoint(new Vector3(-1720.39f, -729.894f, 9.869843f), 319.1174f),
new SpawnPoint(new Vector3(-1806.609f, -687.9351f, 10.06041f), 51.98821f),
new SpawnPoint(new Vector3(-1821.137f, -681.2333f, 9.970448f), 51.04442f),
new SpawnPoint(new Vector3(-1856.677f, -612.4247f, 10.96847f), 322.7834f),
new SpawnPoint(new Vector3(-1872.932f, -616.6786f, 11.31133f), 84.36921f),
new SpawnPoint(new Vector3(-1918.372f, -596.2253f, 11.28551f), 52.77471f),
new SpawnPoint(new Vector3(-1996.281f, -531.2833f, 11.36596f), 225.6093f),
new SpawnPoint(new Vector3(-2001.988f, -475.6722f, 11.26016f), 51.79802f),
new SpawnPoint(new Vector3(-2048.041f, -437.0989f, 11.24985f), 230.2861f),
new SpawnPoint(new Vector3(-2032.126f, -469.5184f, 10.99609f), 230.5785f),
new SpawnPoint(new Vector3(-2025.667f, -488.2158f, 11.43054f), 231.5256f),
new SpawnPoint(new Vector3(-1581.514f, -888.175f, 9.590529f), 47.18215f),
new SpawnPoint(new Vector3(-1646.547f, -828.3318f, 9.737471f), 139.9628f),
new SpawnPoint(new Vector3(-1677.498f, -951.3454f, 7.320974f), 253.4563f),
new SpawnPoint(new Vector3(-1624.422f, -951.8485f, 7.903121f), 317.9203f),
new SpawnPoint(new Vector3(-1644.51f, -977.8652f, 7.300429f), 223.9693f),
new SpawnPoint(new Vector3(-1463.05f, -994.9666f, 6.205635f), 325.9745f),
new SpawnPoint(new Vector3(-1243.524f, -1514.737f, 4.01816f), 78.55869f),
new SpawnPoint(new Vector3(-1328.392f, -1600.75f, 4.018931f), 207.3288f),
new SpawnPoint(new Vector3(-1227.588f, -1640.461f, 3.919664f), 36.66148f),
new SpawnPoint(new Vector3(-1205.586f, -1671.775f, 3.898374f), 215.3929f),
new SpawnPoint(new Vector3(-1163.206f, -1770.322f, 3.590057f), 302.2296f),
new SpawnPoint(new Vector3(-1105.924f, -1720.967f, 3.898926f), 125.4404f),
new SpawnPoint(new Vector3(-1005.031f, -1568.842f, 4.689188f), 349.1566f),
new SpawnPoint(new Vector3(-693.989f, -1405.134f, 4.649162f), 141.0479f),
new SpawnPoint(new Vector3(-771.7642f, -1436.53f, 4.584848f), 58.63589f),
new SpawnPoint(new Vector3(-461.4443f, -1769.726f, 20.34299f), 56.95299f),
new SpawnPoint(new Vector3(-604.3918f, -1729.211f, 22.9777f), 174.4493f),
new SpawnPoint(new Vector3(-644.8967f, -1748.971f, 24.02233f), 259.0234f),
new SpawnPoint(new Vector3(-637.7601f, -1706.008f, 24.17494f), 358.8419f),
new SpawnPoint(new Vector3(-375.0086f, -1720.21f, 18.30354f), 306.6443f),
new SpawnPoint(new Vector3(-291.2116f, -1788.182f, 5.000602f), 215.5784f),
new SpawnPoint(new Vector3(-73.91474f, -1880.443f, 8.188592f), 203.3324f),
new SpawnPoint(new Vector3(20.62313f, -1937.546f, 14.70661f), 231.5854f),
new SpawnPoint(new Vector3(49.92082f, -1969.056f, 17.51919f), 192.5603f),
new SpawnPoint(new Vector3(84.41433f, -2020.21f, 17.75337f), 70.89703f),
new SpawnPoint(new Vector3(-6.8228f, -1853.197f, 24.13408f), 226.7338f),
new SpawnPoint(new Vector3(89.84401f, -1936.454f, 20.26726f), 211.2226f),
new SpawnPoint(new Vector3(116.6184f, -1931.033f, 20.41232f), 348.8004f),
new SpawnPoint(new Vector3(113.8603f, -1853.41f, 24.89705f), 293.2385f),
new SpawnPoint(new Vector3(35.54089f, -1804.704f, 25.73001f), 87.73309f),
new SpawnPoint(new Vector3(47.94305f, -1701.777f, 29.1048f), 39.76486f),
new SpawnPoint(new Vector3(132.2981f, -1686.661f, 29.00174f), 292.4089f),
new SpawnPoint(new Vector3(421.6746f, -1804.972f, 28.16232f), 195.2522f),
new SpawnPoint(new Vector3(626.8259f, -1810.457f, 14.75343f), 20.3466f),
new SpawnPoint(new Vector3(653.9578f, -1760.181f, 9.254526f), 339.8936f),
new SpawnPoint(new Vector3(648.7483f, -1495.306f, 10.15881f), 8.738436f),
new SpawnPoint(new Vector3(670.5413f, -1505.755f, 10.13347f), 182.2476f),
new SpawnPoint(new Vector3(634.9514f, -1169.03f, 11.75596f), 326.4341f),
new SpawnPoint(new Vector3(750.0828f, -1164.614f, 24.43878f), 112.5558f),
new SpawnPoint(new Vector3(799.2974f, -807.0757f, 25.8315f), 341.8732f),
new SpawnPoint(new Vector3(714.5635f, -400.9041f, 39.72113f), 209.163f),
new SpawnPoint(new Vector3(997.2584f, -283.5595f, 66.52881f), 285.0026f),
new SpawnPoint(new Vector3(1034.057f, -265.2961f, 57.798f), 227.6107f),
new SpawnPoint(new Vector3(1022.835f, -245.9451f, 43.71384f), 44.44073f),
new SpawnPoint(new Vector3(1088.954f, -202.5872f, 55.46607f), 315.3874f),
new SpawnPoint(new Vector3(1062.119f, -263.041f, 53.01335f), 183.2964f),
new SpawnPoint(new Vector3(1108.849f, -214.2204f, 55.47533f), 310.1254f),
new SpawnPoint(new Vector3(1094.705f, -207.2776f, 55.45605f), 15.44939f),
new SpawnPoint(new Vector3(983.5478f, -401.9276f, 49.79877f), 148.719f),
new SpawnPoint(new Vector3(-55.50956f, -735.1712f, 33.23854f), 81.77991f),
new SpawnPoint(new Vector3(-84.41789f, -745.9321f, 43.65948f), 207.081f),
new SpawnPoint(new Vector3(50.99417f, -782.3421f, 43.61289f), 262.8107f),
new SpawnPoint(new Vector3(50.58746f, -781.9059f, 43.78002f), 305.0128f),
new SpawnPoint(new Vector3(195.1799f, -612.0969f, 41.80472f), 251.2187f),
new SpawnPoint(new Vector3(307.6984f, -619.7291f, 43.01325f), 253.7594f),
new SpawnPoint(new Vector3(79.14017f, -548.2419f, 33.11264f), 217.4518f),
new SpawnPoint(new Vector3(69.94379f, -546.8152f, 33.0838f), 21.71207f),
new SpawnPoint(new Vector3(-61.70403f, -542.5186f, 31.61689f), 246.4725f),
new SpawnPoint(new Vector3(-82.98533f, -537.4492f, 39.74993f), 74.4258f),
new SpawnPoint(new Vector3(331.3347f, -537.4774f, 33.39565f), 262.0074f),
new SpawnPoint(new Vector3(341.1961f, -537.5885f, 33.32623f), 196.905f),
new SpawnPoint(new Vector3(766.9247f, -667.434f, 38.17251f), 193.1206f),
new SpawnPoint(new Vector3(882.246f, -715.4648f, 41.90453f), 243.6711f),
new SpawnPoint(new Vector3(865.3566f, -698.6223f, 42.31934f), 60.75742f),
new SpawnPoint(new Vector3(993.204f, 194.5238f, 80.84f), 342.3577f),
new SpawnPoint(new Vector3(1298.582f, 565.5021f, 80.19177f), 314.1034f),
new SpawnPoint(new Vector3(1295.279f, 843.0751f, 108.8632f), 359.228f),
new SpawnPoint(new Vector3(1259.827f, 944.8932f, 133.472f), 277.2864f),
new SpawnPoint(new Vector3(1192.393f, 1187.247f, 158.5868f), 287.9743f),
new SpawnPoint(new Vector3(1097.884f, 1485.002f, 164.5627f), 28.18264f),
new SpawnPoint(new Vector3(346.5468f, 2644.388f, 44.36217f), 272.9839f),
new SpawnPoint(new Vector3(368.4028f, 2650.256f, 44.86501f), 257.1857f),
new SpawnPoint(new Vector3(334.9758f, 2660.49f, 44.30087f), 93.40306f),
new SpawnPoint(new Vector3(691.597f, 2896.733f, 50.73219f), 75.17198f),
new SpawnPoint(new Vector3(829.95f, 2824.923f, 58.46243f), 249.9318f),
new SpawnPoint(new Vector3(975.5601f, 2832.097f, 47.50877f), 111.8123f),
new SpawnPoint(new Vector3(1026.697f, 2826.527f, 46.64118f), 292.6873f),
new SpawnPoint(new Vector3(1139.187f, 2871.688f, 38.7191f), 312.0844f),
new SpawnPoint(new Vector3(1094.512f, 2976.431f, 40.23582f), 94.64748f),
new SpawnPoint(new Vector3(941.9748f, 3085.838f, 40.95648f), 266.5726f),
new SpawnPoint(new Vector3(824.6891f, 3105.296f, 40.65287f), 79.54267f),
new SpawnPoint(new Vector3(754.9409f, 3112.14f, 45.3271f), 128.1267f),
new SpawnPoint(new Vector3(626.3986f, 3060.875f, 42.63816f), 289.2528f),
new SpawnPoint(new Vector3(-449.0146f, 4309.651f, 60.9469f), 218.345f),
new SpawnPoint(new Vector3(-397.5767f, 4295.874f, 54.24733f), 83.40171f),
new SpawnPoint(new Vector3(-470.1157f, 4339.789f, 61.38598f), 30.28156f),
new SpawnPoint(new Vector3(-524.9158f, 4372.42f, 66.62307f), 51.40295f),
new SpawnPoint(new Vector3(-554.176f, 4360.46f, 62.40427f), 325.7979f),
new SpawnPoint(new Vector3(-575.3702f, 4357.814f, 56.23116f), 251.5513f),
new SpawnPoint(new Vector3(-614.3051f, 4378.185f, 44.60941f), 29.21466f),
new SpawnPoint(new Vector3(851.3321f, 6482.741f, 21.73653f), 266.026f),
new SpawnPoint(new Vector3(1120.464f, 6464.657f, 22.26066f), 217.9752f),
new SpawnPoint(new Vector3(1250.133f, 6502.895f, 20.29046f), 90.01067f),
new SpawnPoint(new Vector3(1020.472f, 6501.598f, 20.59785f), 88.76454f),
new SpawnPoint(new Vector3(1815.683f, 6327.647f, 38.38193f), 264.8736f),
new SpawnPoint(new Vector3(1937.124f, 6282.643f, 41.97283f), 248.7363f),
new SpawnPoint(new Vector3(1981.787f, 6227.387f, 44.86609f), 88.57024f),
new SpawnPoint(new Vector3(1998.984f, 6262.201f, 45.81403f), 3.580007f),
new SpawnPoint(new Vector3(2006.952f, 6176.372f, 46.37933f), 139.3211f),
new SpawnPoint(new Vector3(2026.798f, 6146.904f, 46.59505f), 161.0323f),
new SpawnPoint(new Vector3(2091.807f, 6086.288f, 48.83874f), 130.6233f),
new SpawnPoint(new Vector3(2073.611f, 6101.034f, 48.16724f), 70.24954f),
new SpawnPoint(new Vector3(2011.532f, 6140.847f, 45.38628f), 228.9981f),
new SpawnPoint(new Vector3(2103.141f, 6057.93f, 48.1056f), 299.4081f),
new SpawnPoint(new Vector3(2236.565f, 5989.143f, 49.41386f), 81.20243f),
new SpawnPoint(new Vector3(2518.932f, 5562.415f, 44.49249f), 23.35608f),
new SpawnPoint(new Vector3(2482.737f, 5368.205f, 56.40266f), 27.54713f),
new SpawnPoint(new Vector3(2539.844f, 5223.242f, 51.2173f), 187.6634f),
new SpawnPoint(new Vector3(2442.286f, 5143.424f, 48.926f), 63.62177f),
new SpawnPoint(new Vector3(-1169.915f, 5029.813f, 156.0962f), 116.9029f),
new SpawnPoint(new Vector3(-1245.34f, 5014.146f, 153.6963f), 107.9199f),
new SpawnPoint(new Vector3(-1289.372f, 4942.341f, 151.7623f), 352.1118f),
new SpawnPoint(new Vector3(-1329.978f, 4847.276f, 141.6443f), 131.5833f),
new SpawnPoint(new Vector3(-1361.796f, 4793.129f, 129.171f), 245.4341f),
new SpawnPoint(new Vector3(-1410.117f, 4796.061f, 109.0014f), 207.6931f),
new SpawnPoint(new Vector3(-1434.195f, 4785.597f, 101.8956f), 163.9442f),
new SpawnPoint(new Vector3(-1521.98f, 4750.351f, 52.45594f), 131.0158f),
new SpawnPoint(new Vector3(-1465.309f, 4697.669f, 38.60336f), 273.3253f),
new SpawnPoint(new Vector3(-1570.616f, 4952.553f, 61.17482f), 113.2782f),
new SpawnPoint(new Vector3(-1932.337f, 4575.08f, 56.70912f), 314.9873f),
new SpawnPoint(new Vector3(-2180.305f, 4455.37f, 61.8029f), 83.22718f),
new SpawnPoint(new Vector3(-2198.666f, 4477.987f, 35.68839f), 335.8883f),
new SpawnPoint(new Vector3(-2042.255f, 4526.901f, 27.96933f), 73.83117f),
new SpawnPoint(new Vector3(-1995.926f, 2676.988f, 0.4020417f), 235.9092f),
new SpawnPoint(new Vector3(-1950.173f, 2657.223f, 2.507495f), 239.5646f),
new SpawnPoint(new Vector3(-1900.048f, 2653.195f, 0.3629823f), 307.6616f),
new SpawnPoint(new Vector3(-1875.089f, 2631.869f, 0.411633f), 232.6406f),
new SpawnPoint(new Vector3(-1874.863f, 2631.689f, 0.409649f), 233.1292f),
new SpawnPoint(new Vector3(-1690.704f, 2634.277f, 0.3474641f), 77.17109f),
new SpawnPoint(new Vector3(-1649.063f, 2619.269f, -0.08617833f), 222.2051f),
new SpawnPoint(new Vector3(-1612.566f, 2684.768f, 0.5589656f), 340.7559f),
new SpawnPoint(new Vector3(-1552.561f, 2714.839f, 4.236357f), 56.87048f),
new SpawnPoint(new Vector3(-1445.265f, 2758.625f, 13.85989f), 13.63659f),
new SpawnPoint(new Vector3(-1587.679f, 2959.909f, 32.82166f), 189.928f),
new SpawnPoint(new Vector3(-1265.737f, 2753.156f, 11.42738f), 304.2107f),
#endregion
        };

        public static List<Vector3> PoliceStationsPositions = new List<Vector3>()
        {
#region PoliceStationsPositions
            new Vector3(392.1373f, -1620.031f, 28.94114f),
            new Vector3(420.5259f, -1021.735f, 28.64523f),
            new Vector3(830.5783f, -1266.321f, 25.92902f),
            new Vector3(-856.9446f, -2413.526f, 13.59407f),
            new Vector3(-1107.434f, -800.1794f, 17.70872f),
            new Vector3(-552.6508f, -143.7225f, 37.86123f),
            new Vector3(620.5485f, 27.10435f, 88.14573f),
            new Vector3(1854.193f, 3675.762f, 33.38328f),
            new Vector3(-434.8029f, 6031.415f, 30.98967f),
#endregion
        };
        public static Vector3 GetClosestPoliceStation(Vector3 position)
        {
            return PoliceStationsPositions.Where(x => x.GetArea() == position.GetArea()).OrderBy(x => Vector3.TravelDistance(x, position)).First();
        }

        #endregion

        public enum EOfficerNeedsTransportState
        {
            EnRoute,
            OfficerWaitingToEnter,
            OfficerEntering,
            OfficerOnVehicle,
            AtPoliceStation,
        }
    }
}
