namespace WildernessCallouts.Callouts
{
    using Rage;
    using Rage.Native;
    using WildernessCallouts.Types;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Windows.Forms;
    using LSPD_First_Response.Mod.Callouts;
    using LSPD_First_Response.Mod.API;
    using RAGENativeUI.Elements;


    [CalloutInfo("SuicideAttempt", CalloutProbability.Medium)]
    internal class SuicideAttempt : CalloutBase
    {
        SuicideAttemptSpawn spawnUsed;
        Blip suicidePedBlip;
        Blip polSergeantPedBlip;
        ESuicideAttemptState state;
        SuicideDecisionController decisionController;

        public override bool OnBeforeCalloutDisplayed()
        {
            spawnUsed = SuicideAttemptSpawn.Spawns.GetRandomElement(true);
            for (int i = 0; i < 20; i++)
            {
                Logger.LogTrivial(this.GetType().Name, "Get spawn attempt #" + i);
                if (spawnUsed.PoliceSergeantPedSpawnPosition.Position.DistanceTo(Game.LocalPlayer.Character) < 1500f &&
                    spawnUsed.PoliceSergeantPedSpawnPosition.Position.DistanceTo(Game.LocalPlayer.Character) > 40f)
                    break;
                spawnUsed = SuicideAttemptSpawn.Spawns.GetRandomElement(true);
            }
            if (spawnUsed.PoliceSergeantPedSpawnPosition.Position.DistanceTo(Game.LocalPlayer.Character) > 1500f)
            {
                Logger.LogTrivial(this.GetType().Name, "Aborting: Spawn too far");
                return false;
            }
            else if (spawnUsed.PoliceSergeantPedSpawnPosition.Position.DistanceTo(Game.LocalPlayer.Character) < 40f)
            {
                Logger.LogTrivial(this.GetType().Name, "Aborting: Spawn too close");
                return false;
            }

            this.CalloutMessage = "Suicide attempt";
            this.CalloutPosition = spawnUsed.SuicidePedSpawnPosition.Position;

            this.ShowCalloutAreaBlipBeforeAccepting(this.CalloutPosition, 45f);

            return spawnUsed.Create();
        }

        public override bool OnCalloutAccepted()
        {
            state = ESuicideAttemptState.EnRoute;

            suicidePedBlip = new Blip(spawnUsed.SuicidePed);
            suicidePedBlip.Sprite = BlipSprite.Friend;
            suicidePedBlip.Color = Color.Yellow;
            suicidePedBlip.SetName("Suicide Attempt: Suicide");

            polSergeantPedBlip = new Blip(spawnUsed.PoliceSergeantPed.Position);
            polSergeantPedBlip.EnableRoute(polSergeantPedBlip.Color);
            polSergeantPedBlip.SetName("Suicide Attempt: Commanding Officer");

            decisionController = new SuicideDecisionController(spawnUsed.SuicidePed);

            return base.OnCalloutAccepted();
        }

        public override void OnCalloutNotAccepted()
        {
            state = ESuicideAttemptState.End;

            spawnUsed.Delete();
            if (suicidePedBlip.Exists()) suicidePedBlip.Delete();
            if (polSergeantPedBlip.Exists()) polSergeantPedBlip.Delete();
            if (decisionController != null) decisionController.Stop();
            base.OnCalloutNotAccepted();
        }

        //bool shouldPedFollowPlayer = false;
        public override void Process()
        {
            //if (Vector3.Distance2D(Game.LocalPlayer.Character.Position, spawnUsed.SuicidePed.Position) < 50.0f)
            //{
            //      NativeFunction.CallByName<uint>("SET_ALL_RANDOM_PEDS_FLEE_THIS_FRAME", Game.LocalPlayer);
            //      Vector3 p = Game.LocalPlayer.Character.Position;
            //      NativeFunction.CallByName<uint>("REMOVE_VEHICLES_FROM_GENERATORS_IN_AREA", p.X + 30f, p.Y + 30f, p.Z + 30f, p.X - 30f, p.Y - 30f, p.Z - 30f);
            //      Vector3 p = spawnUsed.PoliceSergeantPed.Position;
            //      NativeFunction.CallByName<uint>("CLEAR_AREA_OF_VEHICLES", p.X, p.Y, p.Z, 45.0f, false, false, false, false, false);
            //}

            if (state == ESuicideAttemptState.EnRoute && Vector3.Distance(spawnUsed.PoliceSergeantPedSpawnPosition.Position, Game.LocalPlayer.Character.Position) < 20.0f)
            {
                Game.DisplayHelp("Approach the ~y~commanding officer", 10000);
                state = ESuicideAttemptState.OnScene;
            }

            if (state == ESuicideAttemptState.OnScene && Vector3.Distance(spawnUsed.PoliceSergeantPedSpawnPosition.Position, Game.LocalPlayer.Character.Position) < 3.25f)
            {
                state = ESuicideAttemptState.TalkingToPolice;
                float initialHeading = spawnUsed.PoliceSergeantPed.Heading;
                spawnUsed.PoliceSergeantPed.Tasks.AchieveHeading(spawnUsed.PoliceSergeantPed.GetHeadingTowards(Game.LocalPlayer.Character)).WaitForCompletion(1000);
                spawnUsed.PoliceSergeantPed.PlayAmbientSpeech(Speech.GENERIC_HI);
                foreach (string dialogueLine in GetPoliceSergeantDialogue())
                {
                    Game.DisplaySubtitle("~b~Commanding Officer:~s~ " + dialogueLine, 4600);
                    GameFiber.Sleep(3900);
                }
                Scenario.StartInPlace(spawnUsed.PoliceSergeantPed, Globals.Random.Next(2) == 1 ? Globals.Random.Next(2) == 1 ? Scenario.WORLD_HUMAN_STAND_IMPATIENT_UPRIGHT : Scenario.WORLD_HUMAN_AA_COFFEE : Scenario.WORLD_HUMAN_CLIPBOARD, true);
                spawnUsed.PoliceSergeantPed.Tasks.AchieveHeading(initialHeading).WaitForCompletion(1500);
                state = ESuicideAttemptState.WaitingToTalkToSuicide;
            }

            if (state == ESuicideAttemptState.WaitingToTalkToSuicide && Vector3.Distance(spawnUsed.SuicidePed.Position, Game.LocalPlayer.Character.Position) < 9.0f)
            {
                Game.DisplayHelp("Press ~b~" + Controls.PrimaryAction.ToUserFriendlyName() + "~s~ to start talking to the potential suicide");
                if (Controls.PrimaryAction.IsJustPressed())
                {
                    state = ESuicideAttemptState.TalkingToSuicide;
                    decisionController.Start();
                }
            }

            if (state == ESuicideAttemptState.TalkingToSuicide && decisionController.IsDecisionMade)
            {
                state = ESuicideAttemptState.DecisionMade;
                switch (decisionController.FinalDecision)
                {
                    case SuicideDecisionController.DecisionTypes.NotSuicide:
                        Game.DisplaySubtitle("~b~Suicide:~s~ " + (Globals.Random.Next(2) == 1 ? "I'm not gonna do it, I'm going down" : "I can't do it, I'm gonna get down"), 6500);
                        GameFiber.Sleep(1750);
                        TeleportSuicideAndPlayerToSergeant();
                        break;
                    case SuicideDecisionController.DecisionTypes.Suicide:
                        int suiceMethodRnd = Globals.Random.Next(4);
                        if (suiceMethodRnd == 0)
                        {
                            string[] weaponsAssets = { "WEAPON_PISTOL", "WEAPON_PISTOL50", "WEAPON_HEAVYPISTOL", "WEAPON_COMBATPISTOL" };
                            spawnUsed.SuicidePed.SuicideWeapon(weaponsAssets.GetRandomElement());
                        }
                        else if (suiceMethodRnd >= 1)
                        {
                            spawnUsed.SuicidePed.Health -= 50;
                            spawnUsed.SuicidePed.Tasks.Jump();
                        }
                        GameFiber.Sleep(2750);
                        Game.LocalPlayer.Character.PlayAmbientSpeech(Globals.Random.Next(2) == 1 ? Speech.GENERIC_SHOCKED_HIGH : Speech.GENERIC_SHOCKED_MED);
                        break;
                    case SuicideDecisionController.DecisionTypes.AttackPlayer:
                        WeaponAsset[] weaponsAssets2 = { (uint)EWeaponHash.Pistol, (uint)EWeaponHash.Pistol_50, (uint)EWeaponHash.Heavy_Pistol, (uint)EWeaponHash.Combat_Pistol, (uint)EWeaponHash.Knife, (uint)EWeaponHash.Hammer, (uint)EWeaponHash.Knuckle_Dusters, (uint)EWeaponHash.Machete, (uint)EWeaponHash.Machine_Pistol, (uint)EWeaponHash.Dagger };
                        spawnUsed.SuicidePed.Inventory.GiveNewWeapon(weaponsAssets2.GetRandomElement(), 666, true);
                        spawnUsed.SuicidePed.AttackPed(Game.LocalPlayer.Character);
                        spawnUsed.SuicidePed.KeepTasks = true;
                        spawnUsed.SuicidePed.PlayAmbientSpeech(Globals.Random.Next(2) == 1 ? Globals.Random.Next(2) == 1 ? Globals.Random.Next(2) == 1 ? Globals.Random.Next(2) == 1 ? Globals.Random.Next(2) == 1 ? Globals.Random.Next(2) == 1 ? Speech.GENERIC_INSULT_HIGH : Speech.GENERIC_INSULT_MED : Speech.GENERIC_CURSE_HIGH : Speech.GENERIC_CURSE_MED : Speech.GENERIC_FUCK_YOU : Speech.GENERIC_SHOCKED_HIGH : Speech.GENERIC_SHOCKED_MED);
                        GameFiber.Sleep(Globals.Random.Next(1250, 2500));
                        Game.LocalPlayer.Character.PlayAmbientSpeech(Globals.Random.Next(2) == 1 ? Globals.Random.Next(2) == 1 ? Globals.Random.Next(2) == 1 ? Globals.Random.Next(2) == 1 ? Globals.Random.Next(2) == 1 ? Globals.Random.Next(2) == 1 ? Speech.GENERIC_INSULT_HIGH : Speech.GENERIC_INSULT_MED : Speech.GENERIC_CURSE_HIGH : Speech.GENERIC_CURSE_MED : Speech.GENERIC_FUCK_YOU : Speech.GENERIC_SHOCKED_HIGH : Speech.GENERIC_SHOCKED_MED);
                        break;
                    default:
                        break;
                }
                if (decisionController.FinalDecision == SuicideDecisionController.DecisionTypes.AttackPlayer || decisionController.FinalDecision == SuicideDecisionController.DecisionTypes.Suicide)
                    Game.DisplayHelp("Go back to talk to the commanding officer", 17500);
            }

            if (state == ESuicideAttemptState.DecisionMade && Vector3.Distance(spawnUsed.PoliceSergeantPedSpawnPosition.Position, Game.LocalPlayer.Character.Position) < 3.25f)
            {
                spawnUsed.PoliceSergeantPed.Tasks.AchieveHeading(spawnUsed.PoliceSergeantPed.GetHeadingTowards(Game.LocalPlayer.Character)).WaitForCompletion(600);
                switch (decisionController.FinalDecision)
                {
                    case SuicideDecisionController.DecisionTypes.NotSuicide:
                        Game.DisplaySubtitle("~b~Commanding Officer:~s~ Good job!", 4600);
                        spawnUsed.SuicidePed.PlayAmbientSpeech(Speech.GENERIC_THANKS);
                        break;
                    case SuicideDecisionController.DecisionTypes.Suicide:
                        spawnUsed.SuicidePed.PlayAmbientSpeech(Globals.Random.Next(2) == 1 ? Speech.GENERIC_SHOCKED_HIGH : Speech.GENERIC_SHOCKED_MED);
                        Game.DisplaySubtitle("~b~Commanding Officer:~s~ You did all you could", 4600);
                        break;
                    case SuicideDecisionController.DecisionTypes.AttackPlayer:
                        spawnUsed.SuicidePed.PlayAmbientSpeech(Globals.Random.Next(2) == 1 ? Speech.GENERIC_SHOCKED_HIGH : Speech.GENERIC_SHOCKED_MED);
                        Game.DisplaySubtitle("~b~Commanding Officer:~s~ That didn't went very well", 4600);
                        break;
                    default:
                        break;
                }
                GameFiber.Sleep(6000);
                this.End();
            }

            //WildernessCallouts.Common.StopAllNonEmergencyVehicles(spawnUsed.PoliceSergeantPedSpawnPosition.Position, 140f);

            base.Process();
        }


        public void TeleportSuicideAndPlayerToSergeant()
        {
            Game.FadeScreenOut(2500, true);
            spawnUsed.SuicidePed.Position = spawnUsed.PoliceSergeantPed.GetOffsetPosition(new Vector3(Globals.Random.Next(2) == 1 ? -1.5f : 1.5f, MathHelper.GetRandomSingle(0.5f, 2.0f), 0f));
            spawnUsed.SuicidePed.Heading = spawnUsed.SuicidePed.GetHeadingTowards(spawnUsed.PoliceSergeantPed);

            Game.LocalPlayer.Character.Position = spawnUsed.PoliceSergeantPed.GetOffsetPosition(new Vector3(0f, 1.5f, 0f));
            Game.LocalPlayer.Character.Heading = Game.LocalPlayer.Character.GetHeadingTowards(spawnUsed.PoliceSergeantPed);
            GameFiber.Sleep(800);
            Game.FadeScreenIn(2500, true);
        }



        public override void End()
        {
            state = ESuicideAttemptState.End;

            //if (Settings.Despawn == 2)
            //{
            spawnUsed.SmoothCleanUp();
            //}
            //else
            //{
            //    spawnUsed.Dismiss();
            //}

            if (suicidePedBlip.Exists()) suicidePedBlip.Delete();
            if (polSergeantPedBlip.Exists()) polSergeantPedBlip.Delete();
            if (decisionController != null) decisionController.Stop();
            base.End();
        }


        protected override void CleanUp()
        {
            // TODO: implement CleanUp()
        }

        public enum ESuicideAttemptState
        {
            EnRoute,
            OnScene,
            TalkingToPolice,
            WaitingToTalkToSuicide,
            TalkingToSuicide,
            DecisionMade,
            End,
        }

        string[] GetPoliceSergeantDialogue()
        {
            string[][] dialogues = new string[][]
            {
                new string[]
                {
                    "Hi, it was about time you got here",
                    "You need to find a way to reach up there and",
                    "try to persuade " + (spawnUsed.SuicidePed.IsMale ? "him" : "her") + " not to suicide",
                    "Good luck!",
                },

                new string[]
                {
                    "Thank goodness you are here",
                    "We need you to go up there fast",
                    "and convince " + (spawnUsed.SuicidePed.IsMale ? "him" : "her") + " to not commit suicide",
                    "Come on!",
                },
            };

            return dialogues.GetRandomElement();
        }
    }


    internal class SuicideDecisionController
    {
        public Ped Ped;
        public int Decision;
        public SuicideDecisionController.UI UserInterface;
        public SuicideDecisionController.Option[][] Options;
        public int CurrentOptionIndex;
        public bool IsDecisionMade;
        public DecisionTypes FinalDecision;

        public SuicideDecisionController(Ped ped)
        {
            Ped = ped;
            Decision = Globals.Random.Next(-30, 30);
            Options = GetPossibleOptions();
            Options.Shuffle();
            CurrentOptionIndex = 0;
            Options[CurrentOptionIndex].Shuffle();
            UserInterface = new UI(Options[CurrentOptionIndex][0], Options[CurrentOptionIndex][1], Options[CurrentOptionIndex][2]);
            UpdateFiber = new GameFiber(delegate
                {
                    //while (true)
                    //{
                    //    GameFiber.Yield();
                    Update();
                    //}
                }, "Suicide Attempt Decision Controller Update Fiber");
            IsDecisionMade = false;
            StopUpdateLoop = false;
        }

        public void Start()
        {
            UpdateFiber.Start();
            UserInterface.Visible = true;
        }
        public void Stop()
        {
            UpdateFiber.Abort();
            UserInterface.Visible = false;
        }

        public void Controller()
        {
            if (Game.IsKeyDown(Keys.D1))
            {
                Logger.LogTrivial("Option Selected: 1");
                Decision += UserInterface.Option1Data.Value;
                Logger.LogTrivial("Adding to decision: " + UserInterface.Option1Data.Value);
                Game.DisplaySubtitle("~b~You:~s~ " + UserInterface.Option1Data.Phrase, 6500);
                GameFiber.Sleep(3900);
                Game.DisplaySubtitle("~b~Suicide:~s~ " + UserInterface.Option1Data.Response, 6500);
                GameFiber.Sleep(3900);
                if ((CurrentOptionIndex > 2 && Globals.Random.Next(3) <= 1) || (CurrentOptionIndex >= Options.Length - 1))
                {
                    StopUpdateLoop = true;
                }
                else
                {
                    CurrentOptionIndex++;
                    Options[CurrentOptionIndex].Shuffle();
                    UserInterface.ChangeOptions(Options[CurrentOptionIndex][0], Options[CurrentOptionIndex][1], Options[CurrentOptionIndex][2]);
                }
            }
            else if (Game.IsKeyDown(Keys.D2))
            {
                Logger.LogTrivial("Option Selected: 2");
                Decision += UserInterface.Option2Data.Value;
                Logger.LogTrivial("Adding to decision: " + UserInterface.Option2Data.Value);
                Game.DisplaySubtitle("~b~You:~s~ " + UserInterface.Option2Data.Phrase, 6500);
                GameFiber.Sleep(3900);
                Game.DisplaySubtitle("~b~Suicide:~s~ " + UserInterface.Option2Data.Response, 6500);
                GameFiber.Sleep(3900);
                if ((CurrentOptionIndex > 2 && Globals.Random.Next(3) <= 1) || (CurrentOptionIndex >= Options.Length - 1))
                {
                    StopUpdateLoop = true;
                }
                else
                {
                    CurrentOptionIndex++;
                    Options[CurrentOptionIndex].Shuffle();
                    UserInterface.ChangeOptions(Options[CurrentOptionIndex][0], Options[CurrentOptionIndex][1], Options[CurrentOptionIndex][2]);
                }
            }
            else if (Game.IsKeyDown(Keys.D3))
            {
                Logger.LogTrivial("Option Selected: 3");
                Decision += UserInterface.Option3Data.Value;
                Logger.LogTrivial("Adding to decision: " + UserInterface.Option3Data.Value);
                Game.DisplaySubtitle("~b~You:~s~ " + UserInterface.Option3Data.Phrase, 6500);
                GameFiber.Sleep(3900);
                Game.DisplaySubtitle("~b~Suicide:~s~ " + UserInterface.Option3Data.Response, 6500);
                GameFiber.Sleep(3900);
                if ((CurrentOptionIndex > 2 && Globals.Random.Next(3) <= 1) || (CurrentOptionIndex >= Options.Length - 1))
                {
                    StopUpdateLoop = true;
                }
                else
                {
                    CurrentOptionIndex++;
                    Options[CurrentOptionIndex].Shuffle();
                    UserInterface.ChangeOptions(Options[CurrentOptionIndex][0], Options[CurrentOptionIndex][1], Options[CurrentOptionIndex][2]);
                }
            }
        }


        public void MakeDecision()
        {
            Logger.LogTrivial("Final Decision: " + Decision);
            if (Decision > 0)
            {
                int middlePoint = Globals.Random.Next(20, 86);
                if (Decision > middlePoint)
                {
                    FinalDecision = DecisionTypes.NotSuicide;
                    Logger.LogTrivial("Final DecisionType: " + FinalDecision);
                    //not suicide
                }
                else if (Decision < middlePoint)
                {
                    FinalDecision = DecisionTypes.Suicide;
                    Logger.LogTrivial("Final DecisionType: " + FinalDecision);
                    //suicide/suicide by player
                }
            }
            else if (Decision < 0)
            {
                int middlePoint = Globals.Random.Next(-80, -55);
                if (Decision > middlePoint)
                {
                    FinalDecision = DecisionTypes.Suicide;
                    Logger.LogTrivial("Final DecisionType: " + FinalDecision);
                    //suicide
                }
                else if (Decision < middlePoint)
                {
                    FinalDecision = DecisionTypes.AttackPlayer;
                    Logger.LogTrivial("Final DecisionType: " + FinalDecision);
                    //Attack player
                }
            }
            IsDecisionMade = true;
            Stop();
        }


        public GameFiber UpdateFiber;
        private bool StopUpdateLoop;
        public void Update()
        {
            while (true)
            {
                GameFiber.Yield();
                Controller();
                UserInterface.Draw();
                if (StopUpdateLoop) break;
            }
            MakeDecision();
        }


        public SuicideDecisionController.Option[][] GetPossibleOptions()
        {
            bool isMale = Ped.IsMale;
            bool isWifeOrHusbandDead = Globals.Random.Next(2) == 1;

            return new SuicideDecisionController.Option[][]
            {
                new SuicideDecisionController.Option[]
                {
                    new SuicideDecisionController.Option("You're totally useless", Globals.Random.Next(2) == 1 ? "..." : "I know... I have lost everything I loved", Globals.Random.Next(-65, -50)),
                    new SuicideDecisionController.Option("You don't have to do this", Globals.Random.Next(2) == 1 ? "You don't know what happened to me" : "Nobody understands me...", Globals.Random.Next(20, 60)),
                    new SuicideDecisionController.Option("You need to come down with me and talk there", "No, I want to stay here", Globals.Random.Next(-5, 50)),
                },

                new SuicideDecisionController.Option[]
                {
                    new SuicideDecisionController.Option("Your " + (isMale ? "wife" : "husband") + " is waiting for you, don't do it", isWifeOrHusbandDead ? Globals.Random.Next(2) == 1 ? (isMale ? "She" : "He") + " died yesterday in a car accident" : "Do you think you're funny? " + (isMale ? "She's" : "He's") + "... dead" : "Oh, yeah... I remember the first time I met " + (isMale ? "her" : "him") + ", good times...", isWifeOrHusbandDead ? Globals.Random.Next(-70, -30) : Globals.Random.Next(15, 65)),
                    new SuicideDecisionController.Option("I promise you the way you're feeling will change", Globals.Random.Next(2) == 1 ? Globals.Random.Next(2) == 1 ?  "Are you saying the truth?" : "I don't think so..." : "I can't see any way out", Globals.Random.Next(-5, 50)),
                    new SuicideDecisionController.Option("You'll get better", "I can't see any way out",  Globals.Random.Next(-15, 35)),
                },

                new SuicideDecisionController.Option[]
                {
                    new SuicideDecisionController.Option("You are not alone in this. Your family is with you", isWifeOrHusbandDead ? "Who? The only person I loved, my " + (isMale ? "wife" : "husband") + ", is dead..." : "Yeah, they always try to understand me", isWifeOrHusbandDead ? Globals.Random.Next(-65, -35) : Globals.Random.Next(15, 50)),
                    new SuicideDecisionController.Option("I want to help you. Tell me what I can do to support you.", Globals.Random.Next(2) == 1 ? "There's nothing you can do..." : "Go fuck yourself!", Globals.Random.Next(-25, 30)),
                    new SuicideDecisionController.Option("You're a real psycho.", Globals.Random.Next(2) == 1 ? "No... All you are the psychos!" : "Go fuck yourself!", Globals.Random.Next(-70, -20)),
                },

                //new SuicideDecisionController.Option[]
                //{
                //    new SuicideDecisionController.Option("Suicide is a permanent solution to a temporary problem", isWifeOrHusbandDead ? "My " + (isMale ? "wife" : "husband") + " is dead, that ain't a temporary problem..." : "", isWifeOrHusbandDead ? Globals.Random.Next(-70, -35) : 999999999),
                //    new SuicideDecisionController.Option("[ADD]", "[ADD RESPONSE]", -50),
                //    new SuicideDecisionController.Option("[ADD]", "[ADD RESPONSE]", 0),
                //},

                //new SuicideDecisionController.Option[]
                //{
                //    new SuicideDecisionController.Option("[ADD]", "[ADD RESPONSE]", 50),
                //    new SuicideDecisionController.Option("[ADD]", "[ADD RESPONSE]", -50),
                //    new SuicideDecisionController.Option("[ADD]", "[ADD RESPONSE]", 0),
                //},

                //new SuicideDecisionController.Option[]
                //{
                //    new SuicideDecisionController.Option("[ADD]", "[ADD RESPONSE]", 50),
                //    new SuicideDecisionController.Option("[ADD]", "[ADD RESPONSE]", -50),
                //    new SuicideDecisionController.Option("[ADD]", "[ADD RESPONSE]", 0),
                //},
            };

        }

        public enum DecisionTypes
        {
            NotSuicide,
            Suicide,
            AttackPlayer,
        }

        public class UI
        {
            public ResRectangle Background;

            public ResText Option1Label;
            public SuicideDecisionController.Option Option1Data;

            public ResText Option2Label;
            public SuicideDecisionController.Option Option2Data;

            public ResText Option3Label;
            public SuicideDecisionController.Option Option3Data;

            public bool Visible;

            public UI(SuicideDecisionController.Option opt1, SuicideDecisionController.Option opt2, SuicideDecisionController.Option opt3)
            {
                Visible = true;

                Option1Data = opt1;
                Option2Data = opt2;
                Option3Data = opt3;

                Option1Label = new ResText("[~b~1~s~] You: \"" + opt1.Phrase + "\"", new Point(20, 30), 0.25f, Color.White, RAGENativeUI.Common.EFont.ChaletLondon, ResText.Alignment.Left);
                Option2Label = new ResText("[~b~2~s~] You: \"" + opt2.Phrase + "\"", new Point(20, 60), 0.25f, Color.White, RAGENativeUI.Common.EFont.ChaletLondon, ResText.Alignment.Left);
                Option3Label = new ResText("[~b~3~s~] You: \"" + opt3.Phrase + "\"", new Point(20, 90), 0.25f, Color.White, RAGENativeUI.Common.EFont.ChaletLondon, ResText.Alignment.Left);

                Background = new ResRectangle(new Point(10, 20), new Size(480, 110), Color.FromArgb(150, Color.DarkGray));
            }

            public void ChangeOptions(SuicideDecisionController.Option opt1, SuicideDecisionController.Option opt2, SuicideDecisionController.Option opt3)
            {
                Option1Data = opt1;
                Option2Data = opt2;
                Option3Data = opt3;

                Option1Label.Caption = "[~b~1~s~] You: \"" + opt1.Phrase + "\"";
                Option2Label.Caption = "[~b~2~s~] You: \"" + opt2.Phrase + "\"";
                Option3Label.Caption = "[~b~3~s~] You: \"" + opt3.Phrase + "\"";
            }


            public void Draw()
            {
                if (!Visible) return;

                Background.Draw();
                Option1Label.Draw();
                Option2Label.Draw();
                Option3Label.Draw();
            }
        }

        public class Option
        {
            public string Phrase;
            public string Response;
            public int Value;

            public Option(string phrase, string response, int value)
            {
                Phrase = phrase;
                Response = response;
                Value = value;
            }
        }
    }





    internal class SuicideAttemptSpawn
    {
        public uint SpeedZoneHandle;

        public SpawnPoint SuicidePedSpawnPosition;
        public Ped SuicidePed;

        public SpawnPoint PoliceSergeantPedSpawnPosition;
        public Ped PoliceSergeantPed;

        public List<SpawnPoint> PoliceVehiclesSpawnPositions;
        public List<Vehicle> PoliceVehicles;

        public List<SpawnPoint> PolicePedsSpawnPositions;
        public List<Ped> PolicePeds;

        public List<SpawnPoint> PoliceBarriersSpawnPositions;
        public List<Rage.Object> PoliceBarriers;

        public List<SpawnPoint> AmbulancesSpawnPositions;
        public List<Vehicle> Ambulances;

        public List<SpawnPoint> ParamedicsSpawnPositions;
        public List<Ped> Paramedics;

        public List<SpawnPoint> FiretrucksSpawnPositions;
        public List<Vehicle> Firetrucks;

        public List<SpawnPoint> FirefightersSpawnPositions;
        public List<Ped> Firefighters;

        public SuicideAttemptSpawn(SpawnPoint suicidePedSpawnPos,
                                   SpawnPoint policeSergeantPedSpawnPos,
                                   List<SpawnPoint> policeVehiclesSpawnPositions,
                                   List<SpawnPoint> policePedsSpawnPositions,
                                   List<SpawnPoint> policeBarriersSpawnPositions,
                                   List<SpawnPoint> ambulancesSpawnPositions,
                                   List<SpawnPoint> paramedicsSpawnPositions,
                                   List<SpawnPoint> firetrucksSpawnPositions,
                                   List<SpawnPoint> firefightersSpawnPositions)
        {
            this.SuicidePedSpawnPosition = suicidePedSpawnPos;
            this.PoliceSergeantPedSpawnPosition = policeSergeantPedSpawnPos;
            this.PoliceVehiclesSpawnPositions = policeVehiclesSpawnPositions;
            this.PolicePedsSpawnPositions = policePedsSpawnPositions;
            this.PoliceBarriersSpawnPositions = policeBarriersSpawnPositions;
            this.AmbulancesSpawnPositions = ambulancesSpawnPositions;
            this.ParamedicsSpawnPositions = paramedicsSpawnPositions;
            this.FiretrucksSpawnPositions = firetrucksSpawnPositions;
            this.FirefightersSpawnPositions = firefightersSpawnPositions;

            this.PoliceVehicles = new List<Vehicle>();
            this.PolicePeds = new List<Ped>();
            this.PoliceBarriers = new List<Rage.Object>();

            this.Ambulances = new List<Vehicle>();
            this.Paramedics = new List<Ped>();

            this.Firetrucks = new List<Vehicle>();
            this.Firefighters = new List<Ped>();
        }

        public bool Create(bool playScannerAudio = true)
        {
            SpeedZoneHandle = World.AddSpeedZone(PoliceSergeantPedSpawnPosition.Position, 100f, 0f);

            /*SUICIDE PED*/
            SuicidePed = new Ped(SuicidePedSpawnPosition.Position);
            if (!SuicidePed.Exists()) return false;
            SuicidePed.Heading = SuicidePedSpawnPosition.Heading;
            SuicidePed.BlockPermanentEvents = true;


            /*POLICE SERGEANT*/
            PoliceSergeantPed = new Ped(GetPedModelForPosition(PoliceSergeantPedSpawnPosition.Position), PoliceSergeantPedSpawnPosition.Position, PoliceSergeantPedSpawnPosition.Heading);
            if (!PoliceSergeantPed.Exists()) return false;
            NativeFunction.CallByName<uint>("SET_PED_PROP_INDEX", PoliceSergeantPed, 0, 0, 0, 0);
            PoliceSergeantPed.IsInvincible = true;
            PoliceSergeantPed.BlockPermanentEvents = true;
            PoliceSergeantPed.RelationshipGroup = "COP";
            Scenario.StartInPlace(PoliceSergeantPed, Globals.Random.Next(2) == 1 ? Globals.Random.Next(2) == 1 ? Globals.Random.Next(2) == 1 ? Scenario.WORLD_HUMAN_STAND_IMPATIENT_UPRIGHT : Scenario.WORLD_HUMAN_AA_COFFEE : Scenario.WORLD_HUMAN_CLIPBOARD : Scenario.WORLD_HUMAN_STAND_IMPATIENT, false);


            /*POLICE*/
            foreach (SpawnPoint sp in PoliceVehiclesSpawnPositions)
            {
                Vehicle v = new Vehicle(GetVehicleModelForPosition(sp.Position), sp.Position, sp.Heading);
                if (!v.Exists()) return false;
                if (v.HasSiren) v.IsSirenOn = true;
                PoliceVehicles.Add(v);
            }
            foreach (SpawnPoint sp in PolicePedsSpawnPositions)
            {
                Ped p = new Ped(GetPedModelForPosition(sp.Position), sp.Position, sp.Heading);
                if (!p.Exists()) return false;
                p.BlockPermanentEvents = true;
                System.Tuple<AnimationDictionary, string> anim = PoliceWaitingAnims.GetRandomElement();
                p.Tasks.PlayAnimation(anim.Item1, anim.Item2, 2.0f, AnimationFlags.Loop);
                PolicePeds.Add(p);
            }
            foreach (SpawnPoint sp in PoliceBarriersSpawnPositions)
            {
                Rage.Object o = new Rage.Object(BarrierModel, sp.Position, sp.Heading);
                if (!o.Exists()) return false;
                NativeFunction.CallByName<uint>("SET_ACTIVATE_OBJECT_PHYSICS_AS_SOON_AS_IT_IS_UNFROZEN", o, true);
                PoliceBarriers.Add(o);
            }

            /*PARAMEDICS*/
            foreach (SpawnPoint sp in AmbulancesSpawnPositions)
            {
                Vehicle v = new Vehicle("ambulance", sp.Position, sp.Heading);
                if (!v.Exists()) return false;
                if (v.HasSiren) v.IsSirenOn = true;
                Ambulances.Add(v);
            }
            foreach (SpawnPoint sp in ParamedicsSpawnPositions)
            {
                Ped p = new Ped("s_m_m_paramedic_01", sp.Position, sp.Heading);
                if (!p.Exists()) return false;
                p.BlockPermanentEvents = true;
                Scenario.StartInPlace(p, Globals.Random.Next(2) == 1 ? Globals.Random.Next(2) == 1 ? Globals.Random.Next(2) == 1 ? Scenario.WORLD_HUMAN_STAND_IMPATIENT_UPRIGHT : Scenario.WORLD_HUMAN_AA_COFFEE : Globals.Random.Next(2) == 1 ? Scenario.CODE_HUMAN_MEDIC_TIME_OF_DEATH : Scenario.WORLD_HUMAN_CLIPBOARD : Scenario.WORLD_HUMAN_STAND_IMPATIENT, false);
                Paramedics.Add(p);
            }

            /*FIREFIGHTERS*/
            foreach (SpawnPoint sp in FiretrucksSpawnPositions)
            {
                Vehicle v = new Vehicle("firetruk", sp.Position, sp.Heading);
                if (!v.Exists()) return false;
                if (v.HasSiren) v.IsSirenOn = true;
                Firetrucks.Add(v);
            }
            foreach (SpawnPoint sp in FirefightersSpawnPositions)
            {
                Ped p = new Ped("s_m_y_fireman_01", sp.Position, sp.Heading);
                if (!p.Exists()) return false;
                p.BlockPermanentEvents = true;
                Scenario.StartInPlace(p, Globals.Random.Next(2) == 1 ? Globals.Random.Next(2) == 1 ? Globals.Random.Next(2) == 1 ? Scenario.WORLD_HUMAN_STAND_IMPATIENT_UPRIGHT : Scenario.WORLD_HUMAN_AA_COFFEE : Globals.Random.Next(2) == 1 ? Scenario.CODE_HUMAN_MEDIC_TIME_OF_DEATH : Scenario.WORLD_HUMAN_CLIPBOARD : Scenario.WORLD_HUMAN_STAND_IMPATIENT, false);
                Firefighters.Add(p);
            }

            if (playScannerAudio)
                Functions.PlayScannerAudioUsingPosition("ATTENTION_ALL_UNITS CRIME_SUICIDE_ATTEMPT IN_OR_ON_POSITION UNITS_RESPOND_CODE_02", this.SuicidePedSpawnPosition.Position);
            return true;
        }


        public void Dismiss()
        {
            if (SuicidePed != null && SuicidePed.Exists()) SuicidePed.Dismiss();

            if (PoliceSergeantPed != null && PoliceSergeantPed.Exists())
            {
                PoliceSergeantPed.IsInvincible = false;
                PoliceSergeantPed.Dismiss();
            }

            foreach (Vehicle v in PoliceVehicles)
            {
                if (v != null && v.Exists()) v.Dismiss();
            }
            PoliceVehicles.Clear();
            foreach (Ped p in PolicePeds)
            {
                if (p != null && p.Exists()) p.Dismiss();
            }
            PolicePeds.Clear();
            foreach (Rage.Object o in PoliceBarriers)
            {
                if (o != null && o.Exists()) o.Dismiss();
            }
            PoliceBarriers.Clear();

            foreach (Vehicle v in Ambulances)
            {
                if (v != null && v.Exists()) v.Dismiss();
            }
            Ambulances.Clear();
            foreach (Ped p in Paramedics)
            {
                if (p != null && p.Exists()) p.Dismiss();
            }
            Paramedics.Clear();

            foreach (Vehicle v in Firetrucks)
            {
                if (v != null && v.Exists()) v.Dismiss();
            }
            Firetrucks.Clear();
            foreach (Ped p in Firefighters)
            {
                if (p != null && p.Exists()) p.Dismiss();
            }
            Firefighters.Clear();

            World.RemoveSpeedZone(SpeedZoneHandle);
        }

        public void Delete()
        {
            if (SuicidePed != null && SuicidePed.Exists()) SuicidePed.Delete();

            if (PoliceSergeantPed != null && PoliceSergeantPed.Exists())
            {
                PoliceSergeantPed.IsInvincible = false;
                PoliceSergeantPed.Delete();
            }

            foreach (Vehicle v in PoliceVehicles)
            {
                if (v != null && v.Exists()) v.Delete();
            }
            foreach (Ped p in PolicePeds)
            {
                if (p != null && p.Exists()) p.Delete();
            }
            foreach (Rage.Object o in PoliceBarriers)
            {
                if (o != null && o.Exists()) o.Delete();
            }

            foreach (Vehicle v in Ambulances)
            {
                if (v != null && v.Exists()) v.Delete();
            }
            foreach (Ped p in Paramedics)
            {
                if (p != null && p.Exists()) p.Delete();
            }

            foreach (Vehicle v in Firetrucks)
            {
                if (v != null && v.Exists()) v.Delete();
            }
            foreach (Ped p in Firefighters)
            {
                if (p != null && p.Exists()) p.Delete();
            }

            World.RemoveSpeedZone(SpeedZoneHandle);
        }

        public void SmoothCleanUp()
        {
            World.RemoveSpeedZone(SpeedZoneHandle);
            if (SuicidePed.Exists())
                SuicidePed.Dismiss();
            if (PoliceSergeantPed.Exists())
            {
                PoliceSergeantPed.IsInvincible = false;
                PoliceSergeantPed.Dismiss();
            }
            foreach (Ped p in PolicePeds)
            {
                if (p.Exists()) p.Delete();
            }
            foreach (Rage.Object o in PoliceBarriers)
            {
                if (o.Exists()) o.Delete();
            }
            foreach (Ped p in Paramedics)
            {
                if (p.Exists()) p.Delete();
            }
            foreach (Ped p in Firefighters)
            {
                if (p.Exists()) p.Delete();
            }
            foreach (Vehicle v in PoliceVehicles)
            {
                if (v.Exists())
                {
                    Ped p = new Ped(GetPedModelForPosition(v.Position), Vector3.Zero, 0f);
                    p.WarpIntoVehicle(v, -1);
                    if (v.Exists()) v.Dismiss();
                    if (p.Exists()) p.Dismiss();
                }
            }
            foreach (Vehicle v in Ambulances)
            {
                if (v.Exists())
                {
                    Ped p = new Ped("s_m_m_paramedic_01", Vector3.Zero, 0f);
                    p.WarpIntoVehicle(v, -1);
                    if (v.Exists()) v.Dismiss();
                    if (p.Exists()) p.Dismiss();
                }
            }
            foreach (Vehicle v in Firetrucks)
            {
                if (v.Exists())
                {
                    Ped p = new Ped("s_m_y_fireman_01", Vector3.Zero, 0f);
                    p.WarpIntoVehicle(v, -1);
                    if (v.Exists()) v.Dismiss();
                    if (p.Exists()) p.Dismiss();
                }
            }
        }



        //public void MoveSuicideToPoliceSergeant()
        //{
        //    NativeFunction.CallByName<uint>("SET_PED_PATH_CAN_USE_CLIMBOVERS", this.SuicidePed, true);
        //    NativeFunction.CallByName<uint>("SET_PED_PATH_CAN_USE_LADDERS", this.SuicidePed, true);
        //    NativeFunction.CallByName<uint>("SET_PED_PATH_CAN_DROP_FROM_HEIGHT", this.SuicidePed, true);
        //    Vector3 p = this.PoliceSergeantPed.GetOffsetPosition(new Vector3(MathHelper.GetRandomSingle(-1.5f, 1.5f), 2.325f, 0f));

        //    if (this.ShouldSuicideMoveBack)
        //    {
        //        Vector3 p2 = this.SuicidePed.GetOffsetPosition(new Vector3(0f, this.MoveBackDistance, 0f));
        //        NativeFunction.CallByName<uint>("TASK_PED_SLIDE_TO_COORD", this.SuicidePed, p2.X, p2.Y, p2.Z, this.SuicidePed.Heading - 180, 1.0f);
        //        while (Vector3.Distance(p2, this.SuicidePed.Position) > 1.4375f)
        //            GameFiber.Yield();
        //        //GameFiber.Sleep(this.WaitTimeToMove);
        //    }

        //    if (this.ShouldSuicideTeleport)
        //    {
        //        this.SuicidePed.Position = this.PositionToTeleport;
        //        GameFiber.Sleep(750);
        //    }

        //    if (Vector3.Distance(p, this.SuicidePed.Position) < 7.1225f)
        //    {
        //        NativeFunction.CallByName<uint>("TASK_PED_SLIDE_TO_COORD", this.SuicidePed, p.X, p.Y, p.Z, this.PoliceSergeantPed.Heading, 1.0f);
        //    }
        //    else
        //    {
        //        this.SuicidePed.Tasks.Clear();
        //        this.SuicidePed.Tasks.FollowNavigationMeshToPosition(p, this.PoliceSergeantPed.Heading - 180, 2.0f).WaitForCompletion();
        //    }
        //}



        public static Model GetPedModelForPosition(Vector3 position)
        {
            switch (position.GetArea())
            {
                case EWorldArea.Los_Santos:
                    return Globals.Random.Next(2) == 1 ? "s_m_y_cop_01" : "s_f_y_cop_01";
                case EWorldArea.Blaine_County:
                    return Globals.Random.Next(2) == 1 ? "s_m_y_sheriff_01" : "s_f_y_sheriff_01";
                default:
                    return Globals.Random.Next(2) == 1 ? "s_m_y_cop_01" : "s_f_y_cop_01";
            }
        }
        public static Model GetVehicleModelForPosition(Vector3 position)
        {
            switch (position.GetArea())
            {
                case EWorldArea.Los_Santos:
                    Model[] losSantosModels = { "police", "police2", "police3", "police4", "policet" };
                    return losSantosModels.GetRandomElement();
                case EWorldArea.Blaine_County:
                    Model[] countyModels = { "sheriff", "sheriff2", "police4" };
                    return countyModels.GetRandomElement();
                default:
                    Model[] defaultModels = { "police", "police2", "police3", "police4", "policet" };
                    return defaultModels.GetRandomElement();
            }
        }

        public const uint BarrierModel = 4151651686;

        public static readonly System.Tuple<AnimationDictionary, string>[] PoliceWaitingAnims =
        {
            new System.Tuple<AnimationDictionary, string>("missbigscore2aig_4", "wait_idle_a"),
            new System.Tuple<AnimationDictionary, string>("missbigscore2aig_4", "wait_idle_b"),
            new System.Tuple<AnimationDictionary, string>("missbigscore2aig_4", "wait_idle_c"),
            new System.Tuple<AnimationDictionary, string>("missbigscore2aig_4", "wait_idle_d"),
            new System.Tuple<AnimationDictionary, string>("amb@world_human_cop_idles@female@idle_a", "idle_a"),
            new System.Tuple<AnimationDictionary, string>("amb@world_human_cop_idles@female@idle_a", "idle_b"),
            new System.Tuple<AnimationDictionary, string>("amb@world_human_cop_idles@female@idle_a", "idle_c"),
            new System.Tuple<AnimationDictionary, string>("amb@world_human_cop_idles@female@idle_b", "idle_d"),
            new System.Tuple<AnimationDictionary, string>("amb@world_human_cop_idles@female@idle_b", "idle_e"),
            new System.Tuple<AnimationDictionary, string>("amb@world_human_cop_idles@male@idle_a", "idle_a"),
            new System.Tuple<AnimationDictionary, string>("amb@world_human_cop_idles@male@idle_a", "idle_b"),
            new System.Tuple<AnimationDictionary, string>("amb@world_human_cop_idles@male@idle_a", "idle_c"),
            new System.Tuple<AnimationDictionary, string>("amb@world_human_cop_idles@male@idle_b", "idle_d"),
            new System.Tuple<AnimationDictionary, string>("amb@world_human_cop_idles@male@idle_b", "idle_e"),
        };



        public static readonly List<SuicideAttemptSpawn> Spawns = new List<SuicideAttemptSpawn>()
        {
            new SuicideAttemptSpawn
               (
                   new SpawnPoint(new Vector3(271.74f, -964.8f, 46.19f), 71.96f),
                   new SpawnPoint(new Vector3(261.92f, -939.65f, 29.38f), 161.08f),
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(270.6234f, -939.7982f, 29.23949f), 69.37986f),
                       new SpawnPoint(new Vector3(262.9968f, -936.9088f, 29.34532f), 68.99387f),
                       new SpawnPoint(new Vector3(253.6845f, -933.1361f, 29.29592f), 69.97971f),
                       new SpawnPoint(new Vector3(248.4121f, -931.2336f, 29.13869f), 70.09674f),
                       new SpawnPoint(new Vector3(231.916f, -978.0114f, 29.23469f), 249.1237f),
                       new SpawnPoint(new Vector3(237.6274f, -979.8881f, 29.34378f), 250.3701f),
                       new SpawnPoint(new Vector3(242.8819f, -981.7286f, 29.34741f), 251.0784f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(267.1731f, -938.8556f, 29.35483f), 342.4554f),
                       new SpawnPoint(new Vector3(258.2957f, -935.1522f, 29.31993f), 339.1576f),
                       new SpawnPoint(new Vector3(246.2506f, -933.3785f, 29.07826f), 343.8368f),
                       new SpawnPoint(new Vector3(233.5707f, -979.9465f, 29.30314f), 154.8773f),
                       new SpawnPoint(new Vector3(252.659f, -983.1212f, 29.32556f), 157.9172f),
                       new SpawnPoint(new Vector3(248.804f, -981.0358f, 29.36958f), 155.8492f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(267.8272f, -936.8886f, 29.32886f), 337.587f),
                       new SpawnPoint(new Vector3(258.7303f, -934.1235f, 29.30431f), 336.7054f),
                       new SpawnPoint(new Vector3(233.2384f, -980.7596f, 29.29563f), 159.4941f),
                       new SpawnPoint(new Vector3(246.8908f, -983.0792f, 29.34968f), 163.1995f),
                       new SpawnPoint(new Vector3(250.5963f, -984.1644f, 29.34697f), 163.8834f),
                       new SpawnPoint(new Vector3(253.2688f, -984.9531f, 29.27607f), 163.5759f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(240.5332f, -957.3229f, 29.24478f), 337.7023f),
                       new SpawnPoint(new Vector3(272.8584f, -947.8049f, 29.2904f), 89.71594f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(243.4209f, -958.6646f, 29.30581f), 257.9059f),
                       new SpawnPoint(new Vector3(242.4532f, -961.6384f, 29.31486f), 204.0442f),
                       new SpawnPoint(new Vector3(269.4796f, -950.5032f, 29.35453f), 130.8792f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(279.4013f, -955.1249f, 29.36228f), 267.1602f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(279.6703f, -952.3378f, 29.45086f), 77.3074f),
                       new SpawnPoint(new Vector3(273.9909f, -954.6843f, 29.32272f), 64.63637f),
                   }/*,
                   true,
                   -20.0f*/
               ),


            new SuicideAttemptSpawn
               (
                   new SpawnPoint(new Vector3(1083.732f, 3502.219f, 43.85477f), 290.85477f),
                   new SpawnPoint(new Vector3(1114.01f, 3540.37f, 34.61f), 143.95f),
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(1133.992f, 3529.674f, 34.67006f), 348.0882f),
                       new SpawnPoint(new Vector3(1134.874f, 3537.844f, 34.79809f), 3.047149f),
                       new SpawnPoint(new Vector3(1065.598f, 3539.664f, 34.24424f), 175.6629f),
                       new SpawnPoint(new Vector3(1065.472f, 3532.304f, 34.23598f), 197.3133f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(1134.291f, 3532.909f, 34.77217f), 273.3415f),
                       new SpawnPoint(new Vector3(1133.918f, 3542.395f, 34.75695f), 265.7285f),
                       new SpawnPoint(new Vector3(1065.422f, 3536.701f, 34.26758f), 86.70991f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(1136.114f, 3532.994f, 34.78696f), 266.0533f),
                       new SpawnPoint(new Vector3(1135.336f, 3542.224f, 34.76804f), 263.8348f),
                       new SpawnPoint(new Vector3(1063.504f, 3536.684f, 34.25344f), 98.43102f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(1118.431f, 3540.222f, 34.65281f), 88.43156f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(1125.077f, 3539.969f, 34.70615f), 267.9563f),
                       new SpawnPoint(new Vector3(1091.548f, 3529.774f, 34.35072f), 85.37493f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(1079.363f, 3532.943f, 34.33982f), 268.3911f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(1088.615f, 3529.945f, 34.34096f), 266.9522f),
                       new SpawnPoint(new Vector3(1097.318f, 3530.075f, 34.41354f), 167.268f),
                   }
               ),


            new SuicideAttemptSpawn
               (
                   new SpawnPoint(new Vector3(-20.7869f, -976.4839f, 84.44525f), 84.44525f),
                   new SpawnPoint(new Vector3(-47.09078f, -930.098f, 29.22165f), 203.22165f),
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-39.69394f, -1001.624f, 29.10065f), 70.01686f),
                       new SpawnPoint(new Vector3(-46.32954f, -1000.048f, 29.1921f), 63.63835f),
                       new SpawnPoint(new Vector3(-52.9702f, -997.4535f, 29.19412f), 248.1365f),
                       new SpawnPoint(new Vector3(-13.54217f, -933.3598f, 29.37387f), 63.99614f),
                       new SpawnPoint(new Vector3(-20.64973f, -930.9491f, 29.4172f), 69.80614f),
                       new SpawnPoint(new Vector3(-27.16018f, -928.6075f, 29.4172f), 71.11063f),
                       new SpawnPoint(new Vector3(-39.47828f, -925.1661f, 29.22274f), 71.38931f),
                       new SpawnPoint(new Vector3(-49.3251f, -925.1212f, 29.11295f), 111.9863f),
                       new SpawnPoint(new Vector3(-54.84451f, -930.4643f, 29.2632f), 160.4504f),
                       new SpawnPoint(new Vector3(-59.12399f, -940.8177f, 29.4172f), 165.6188f),
                       new SpawnPoint(new Vector3(-61.18012f, -949.3058f, 29.34956f), 164.7155f),
                       new SpawnPoint(new Vector3(9.174678f, -969.3935f, 29.40579f), 341.6735f),
                       new SpawnPoint(new Vector3(11.1996f, -962.7617f, 29.40764f), 342.3596f),
                       new SpawnPoint(new Vector3(13.89194f, -954.6477f, 29.33252f), 342.2302f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-42.82894f, -1000.467f, 29.19941f), 155.8051f),
                       new SpawnPoint(new Vector3(-59.23001f, -994.5653f, 29.1682f), 157.1017f),
                       new SpawnPoint(new Vector3(-16.37209f, -931.7226f, 29.41642f), 336.7121f),
                       new SpawnPoint(new Vector3(-32.92838f, -927.0485f, 29.32876f), 338.4154f),
                       new SpawnPoint(new Vector3(-57.96926f, -935.2714f, 29.39009f), 73.94067f),
                       new SpawnPoint(new Vector3(-56.93399f, -949.0325f, 29.41684f), 62.13193f),
                       new SpawnPoint(new Vector3(10.0569f, -966.5076f, 29.40765f), 254.8455f),
                       new SpawnPoint(new Vector3(12.44489f, -958.3407f, 29.40415f), 252.8618f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-43.65666f, -1002.307f, 29.17943f), 155.6246f),
                       new SpawnPoint(new Vector3(-58.02437f, -997.1574f, 29.17921f), 157.9923f),
                       new SpawnPoint(new Vector3(-59.86773f, -996.0349f, 29.1497f), 155.8298f),
                       new SpawnPoint(new Vector3(-62.05977f, -995.264f, 29.05816f), 156.4137f),
                       new SpawnPoint(new Vector3(-15.98287f, -930.3256f, 29.40964f), 340.0176f),
                       new SpawnPoint(new Vector3(-32.23555f, -925.2239f, 29.34758f), 339.9944f),
                       new SpawnPoint(new Vector3(-58.95671f, -934.9686f, 29.38544f), 78.12878f),
                       new SpawnPoint(new Vector3(11.08934f, -966.8492f, 29.40765f), 251.6223f),
                       new SpawnPoint(new Vector3(14.18916f, -958.8799f, 29.40435f), 252.9917f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-50.83898f, -967.0535f, 29.30835f), 164.7854f),
                       new SpawnPoint(new Vector3(-24.63019f, -944.1398f, 29.42329f), 270.2834f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-52.04509f, -962.8849f, 29.21607f), 315.808f),
                       new SpawnPoint(new Vector3(-24.54063f, -946.6265f, 29.41438f), 188.2253f),
                       new SpawnPoint(new Vector3(-25.9215f, -946.8087f, 29.41193f), 195.909f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-43.66814f, -945.9635f, 29.33866f), 120.7608f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-40.82911f, -947.1974f, 29.38603f), 222.7936f),
                       new SpawnPoint(new Vector3(-37.18732f, -943.8989f, 29.42451f), 252.6364f),
                   }
               ),


            new SuicideAttemptSpawn
               (
                   new SpawnPoint(new Vector3(119.0198f, -1026.516f, 58.21211f), 58.21211f),
                   new SpawnPoint(new Vector3(104.9626f, -995.3035f, 29.39822f), 201.39822f),
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(98.22874f, -1050.06f, 29.30739f), 64.06969f),
                       new SpawnPoint(new Vector3(91.19386f, -1047.634f, 29.42953f), 68.65026f),
                       new SpawnPoint(new Vector3(86.0829f, -1045.723f, 29.44862f), 69.80267f),
                       new SpawnPoint(new Vector3(74.94658f, -1040.638f, 29.23669f), 40.53274f),
                       new SpawnPoint(new Vector3(77.45786f, -999.6675f, 29.36807f), 339.8116f),
                       new SpawnPoint(new Vector3(80.02969f, -993.0753f, 29.4074f), 341.31f),
                       new SpawnPoint(new Vector3(82.0621f, -987.0724f, 29.40683f), 340.9262f),
                       new SpawnPoint(new Vector3(105.6237f, -971.9337f, 29.36112f), 250.7463f),
                       new SpawnPoint(new Vector3(110.6223f, -973.6111f, 29.40733f), 251.7328f),
                       new SpawnPoint(new Vector3(122.4525f, -977.7812f, 29.40728f), 251.8277f),
                       new SpawnPoint(new Vector3(127.1964f, -978.2524f, 29.2531f), 286.9878f),
                       new SpawnPoint(new Vector3(136.6604f, -997.9412f, 29.26472f), 151.6795f),
                       new SpawnPoint(new Vector3(135.0116f, -1003f, 29.40547f), 162.1717f),
                       new SpawnPoint(new Vector3(132.3531f, -1010.223f, 29.40576f), 164.2663f),
                       new SpawnPoint(new Vector3(129.4606f, -1018.863f, 29.35829f), 161.9267f),
                       new SpawnPoint(new Vector3(127.2993f, -1022.824f, 29.35739f), 90.39933f),
                       new SpawnPoint(new Vector3(104.0095f, -993.1033f, 29.40505f), 114.0775f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(94.42356f, -1049.421f, 29.40852f), 158.7487f),
                       new SpawnPoint(new Vector3(81.9058f, -1044.206f, 29.46425f), 154.6453f),
                       new SpawnPoint(new Vector3(77.0412f, -995.1761f, 29.4074f), 65.63057f),
                       new SpawnPoint(new Vector3(85.36751f, -978.5147f, 29.22831f), 66.42845f),
                       new SpawnPoint(new Vector3(113.7281f, -975.4913f, 29.40733f), 334.321f),
                       new SpawnPoint(new Vector3(125.0332f, -980.8431f, 29.31144f), 348.3965f),
                       new SpawnPoint(new Vector3(133.4646f, -1006.623f, 29.40578f), 249.0251f),
                       new SpawnPoint(new Vector3(131.2821f, -1014.189f, 29.40565f), 253.6667f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(94.97308f, -1051.18f, 29.3931f), 161.8713f),
                       new SpawnPoint(new Vector3(81.48351f, -1045.127f, 29.46331f), 155.8531f),
                       new SpawnPoint(new Vector3(78.70168f, -1043.717f, 29.44893f), 159.1675f),
                       new SpawnPoint(new Vector3(76.98779f, -996.7546f, 29.4074f), 74.615f),
                       new SpawnPoint(new Vector3(82.39878f, -982.6156f, 29.40715f), 80.92319f),
                       new SpawnPoint(new Vector3(83.48409f, -980.1898f, 29.34459f), 67.94225f),
                       new SpawnPoint(new Vector3(84.51179f, -977.1669f, 29.20541f), 72.85156f),
                       new SpawnPoint(new Vector3(114.5614f, -973.1564f, 29.40733f), 341.4777f),
                       new SpawnPoint(new Vector3(117.2336f, -974.3077f, 29.40733f), 340.942f),
                       new SpawnPoint(new Vector3(119.5869f, -975.1624f, 29.4073f), 342.9489f),
                       new SpawnPoint(new Vector3(134.8037f, -1007.068f, 29.40546f), 250.6362f),
                       new SpawnPoint(new Vector3(132.4148f, -1014.542f, 29.40539f), 252.0202f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(116.4893f, -998.5737f, 29.41288f), 347.5028f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(114.4554f, -998.2001f, 29.40161f), 91.71223f),
                       new SpawnPoint(new Vector3(115.6762f, -1003.084f, 29.39991f), 159.9469f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(93.89365f, -1005.948f, 29.40054f), 12.31949f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(96.32065f, -1005.437f, 29.39587f), 282.9556f),
                       new SpawnPoint(new Vector3(96.83714f, -1006.417f, 29.39766f), 329.4973f),
                   }/*,
                   false,
                   0.0f,
                   true,
                   128.15f, -1040.02f, 29.43f*/
               ),


            new SuicideAttemptSpawn
               (
                   new SpawnPoint(new Vector3(1849.311f, 3598.142f, 46.32079f), 116.32079f),
                   new SpawnPoint(new Vector3(1872.626f, 3615.871f, 34.44375f), 208.4375f),
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(1810.081f, 3578.841f, 35.42976f), 227.555f),
                       new SpawnPoint(new Vector3(1814.045f, 3572.468f, 35.42075f), 236.314f),
                       new SpawnPoint(new Vector3(1887.9f, 3615.904f, 34.26773f), 32.65285f),
                       new SpawnPoint(new Vector3(1884.38f, 3622.584f, 34.26191f), 210.0835f),
                       new SpawnPoint(new Vector3(1871.554f, 3617.673f, 34.38578f), 119.5493f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(1809.472f, 3576.861f, 35.45952f), 121.9177f),
                       new SpawnPoint(new Vector3(1810.762f, 3574.649f, 35.47264f), 122.1065f),
                       new SpawnPoint(new Vector3(1887.137f, 3620.66f, 34.26444f), 305.1773f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(1808.512f, 3576.213f, 35.47359f), 123.6629f),
                       new SpawnPoint(new Vector3(1809.585f, 3574.075f, 35.49082f), 121.0358f),
                       new SpawnPoint(new Vector3(1811.067f, 3572.595f, 35.46623f), 116.0192f),
                       new SpawnPoint(new Vector3(1888.119f, 3621.267f, 34.24995f), 303.5455f),
                       new SpawnPoint(new Vector3(1886.658f, 3623.127f, 34.23857f), 304.9216f),
                       new SpawnPoint(new Vector3(1889.957f, 3618.06f, 34.23864f), 304.3893f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(1824.804f, 3574.586f, 35.18347f), 299.2397f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(1821.276f, 3572.568f, 35.23397f), 128.014f),
                       new SpawnPoint(new Vector3(1825.086f, 3577.477f, 35.26767f), 22.90918f),
                   },
                   new List<SpawnPoint>()
                   {
                   },
                   new List<SpawnPoint>()
                   {
                   }/*,
                   false,
                   0f,
                   true,
                   1844.82f, 3608.79f, 49.28f*/
               ),


            new SuicideAttemptSpawn
               (
                   new SpawnPoint(new Vector3(277.6679f, -1447.092f, 47.50466f), 104.94046f),
                   new SpawnPoint(new Vector3(286.9048f, -1472.473f, 29.28451f), 92.28451f),
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(298.8864f, -1478.981f, 29.19185f), 143.5584f),
                       new SpawnPoint(new Vector3(293.9483f, -1485.908f, 29.3403f), 142.8433f),
                       new SpawnPoint(new Vector3(288.7631f, -1492.786f, 29.31304f), 323.3939f),
                       new SpawnPoint(new Vector3(284.1262f, -1498.597f, 29.23709f), 119.1618f),
                       new SpawnPoint(new Vector3(259.5056f, -1428.539f, 29.28238f), 39.67736f),
                       new SpawnPoint(new Vector3(253.0761f, -1423.759f, 29.23102f), 255.0058f),
                       new SpawnPoint(new Vector3(237.9239f, -1429.719f, 29.25204f), 140.2831f),
                       new SpawnPoint(new Vector3(233.4375f, -1436.159f, 29.3387f), 144.3449f),
                       new SpawnPoint(new Vector3(229.6524f, -1441.422f, 29.34957f), 144.1084f),
                       new SpawnPoint(new Vector3(224.0874f, -1450.824f, 29.13164f), 152.9987f),
                       new SpawnPoint(new Vector3(289.0432f, -1472.224f, 29.23178f), 185.8143f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(296.483f, -1482.065f, 29.31543f), 232.6091f),
                       new SpawnPoint(new Vector3(287.0646f, -1496.582f, 29.24908f), 233.9487f),
                       new SpawnPoint(new Vector3(257.8248f, -1425.202f, 29.38787f), 323.6023f),
                       new SpawnPoint(new Vector3(235.3594f, -1430.454f, 29.30274f), 54.97972f),
                       new SpawnPoint(new Vector3(227.2905f, -1444.807f, 29.33733f), 57.9184f),
                       new SpawnPoint(new Vector3(226.1789f, -1446.884f, 29.31133f), 52.7778f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(297.9085f, -1483.183f, 29.3132f), 223.8143f),
                       new SpawnPoint(new Vector3(292.4085f, -1490.451f, 29.3332f), 232.1231f),
                       new SpawnPoint(new Vector3(288.0776f, -1497.459f, 29.21832f), 231.3051f),
                       new SpawnPoint(new Vector3(258.3309f, -1424.509f, 29.38973f), 323.062f),
                       new SpawnPoint(new Vector3(234.0691f, -1432.676f, 29.33878f), 53.94652f),
                       new SpawnPoint(new Vector3(226.5198f, -1444.277f, 29.33769f), 54.80314f),
                       new SpawnPoint(new Vector3(224.6123f, -1445.706f, 29.31078f), 53.14878f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(266.5324f, -1471.515f, 29.49491f), 49.85345f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(267.7396f, -1470.153f, 29.49803f), 317.6189f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(250.6861f, -1471.351f, 29.19982f), 230.45f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(254.8463f, -1471.835f, 29.29989f), 320.1566f),
                       new SpawnPoint(new Vector3(256.7516f, -1472.892f, 29.30956f), 32.05061f),
                   }
               ),


            new SuicideAttemptSpawn
               (
                   new SpawnPoint(new Vector3(162.0944f, -1118.299f, 46.34744f), 179.24744f),
                   new SpawnPoint(new Vector3(171.9635f, -1134.943f, 29.13021f), 35.14723f),
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(184.5954f, -1125.741f, 29.20774f), 187.6072f),
                       new SpawnPoint(new Vector3(185.8277f, -1134.162f, 29.19665f), 39.56058f),
                       new SpawnPoint(new Vector3(172.7413f, -1136.563f, 29.28962f), 301.2073f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(186.4348f, -1128.349f, 29.2884f), 272.1137f),
                       new SpawnPoint(new Vector3(186.5769f, -1130.579f, 29.29697f), 274.5847f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(187.4253f, -1128.308f, 29.27896f), 272.929f),
                       new SpawnPoint(new Vector3(187.9433f, -1131.063f, 29.28496f), 272.0396f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(147.1777f, -1133.509f, 29.20039f), 88.15668f),
                       new SpawnPoint(new Vector3(134.3687f, -1125.746f, 29.23577f), 261.7572f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(151.5641f, -1134.809f, 29.14128f), 339.1706f),
                       new SpawnPoint(new Vector3(152.2673f, -1133.15f, 29.23094f), 155.4177f),
                       new SpawnPoint(new Vector3(134.2338f, -1128.566f, 29.32765f), 180.0095f),
                   },
                   new List<SpawnPoint>()
                   {
                   },
                   new List<SpawnPoint>()
                   {
                   }
               ),


            new SuicideAttemptSpawn
               (
                   new SpawnPoint(new Vector3(2865.856f, 4403.022f, 72.18858f), 34.766f),
                   new SpawnPoint(new Vector3(2869.232f, 4438.023f, 48.74938f), 175.2702f),
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(2823.049f, 4404.975f, 49.07747f), 52.8846f),
                       new SpawnPoint(new Vector3(2818.461f, 4412.398f, 48.96371f), 17.26469f),
                       new SpawnPoint(new Vector3(2817.137f, 4420.274f, 48.8239f), 12.92659f),
                       new SpawnPoint(new Vector3(2819.448f, 4426.163f, 48.68415f), 136.0907f),
                       new SpawnPoint(new Vector3(2896.655f, 4438.431f, 48.27343f), 186.6653f),
                       new SpawnPoint(new Vector3(2893.37f, 4444.58f, 48.28753f), 30.42498f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(2819.924f, 4407.604f, 49.04737f), 141.3946f),
                       new SpawnPoint(new Vector3(2816.306f, 4415.296f, 48.91829f), 104.8881f),
                       new SpawnPoint(new Vector3(2816.544f, 4427.675f, 48.66705f), 102.5704f),
                       new SpawnPoint(new Vector3(2898.418f, 4440.382f, 48.23846f), 285.6473f),
                       new SpawnPoint(new Vector3(2896.882f, 4444.354f, 48.23772f), 292.2128f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(2819.166f, 4406.721f, 49.06727f), 133.8465f),
                       new SpawnPoint(new Vector3(2816.789f, 4412.592f, 48.96766f), 103.0901f),
                       new SpawnPoint(new Vector3(2815.389f, 4424.572f, 48.74162f), 60.6801f),
                       new SpawnPoint(new Vector3(2814.826f, 4417.957f, 48.87339f), 106.2196f),
                       new SpawnPoint(new Vector3(2897.822f, 4444.776f, 48.21637f), 293.881f),
                       new SpawnPoint(new Vector3(2899.302f, 4442.158f, 48.22256f), 292.7034f),
                       new SpawnPoint(new Vector3(2899.982f, 4439.004f, 48.20078f), 290.8482f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(2876.932f, 4442.305f, 48.45464f), 107.1736f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(2875.929f, 4439.298f, 48.64866f), 199.5438f),
                       new SpawnPoint(new Vector3(2877.937f, 4439.82f, 48.61309f), 171.3005f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(2882.015f, 4431.099f, 48.5986f), 291.6251f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(2881.069f, 4434.001f, 48.61708f), 104.1982f),
                       new SpawnPoint(new Vector3(2879.103f, 4433.342f, 48.65812f), 288.21f),
                   }
               ),


            new SuicideAttemptSpawn
               (
                   new SpawnPoint(new Vector3(2820.258f, 4971.733f, 60.58047f), 117.2164f),
                   new SpawnPoint(new Vector3(2797.95f, 4944.451f, 33.65012f), 48.80604f),
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(2793.823f, 4929.85f, 33.84959f), 134.2087f),
                       new SpawnPoint(new Vector3(2787.872f, 4924.099f, 33.85421f), 308.9123f),
                       new SpawnPoint(new Vector3(2776.431f, 4926.283f, 33.65951f), 69.87094f),
                       new SpawnPoint(new Vector3(2772.37f, 4931.568f, 33.66686f), 76.95502f),
                       new SpawnPoint(new Vector3(2786.956f, 4952.821f, 33.65735f), 312.2325f),
                       new SpawnPoint(new Vector3(2793.5f, 4959.305f, 33.65531f), 321.399f),
                       new SpawnPoint(new Vector3(2883.433f, 5027.7f, 31.7505f), 28.22051f),
                       new SpawnPoint(new Vector3(2880.87f, 5035.603f, 31.73543f), 193.3336f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(2791.423f, 4926.907f, 33.89067f), 224.7142f),
                       new SpawnPoint(new Vector3(2772.376f, 4928.749f, 33.68498f), 133.2034f),
                       new SpawnPoint(new Vector3(2787.009f, 4956.998f, 33.68106f), 49.50779f),
                       new SpawnPoint(new Vector3(2789.203f, 4959.954f, 33.67617f), 47.98467f),
                       new SpawnPoint(new Vector3(2883.592f, 5030.899f, 31.77097f), 288.0025f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(2793.384f, 4926.375f, 33.95535f), 223.3639f),
                       new SpawnPoint(new Vector3(2791.358f, 4924.602f, 33.95624f), 220.163f),
                       new SpawnPoint(new Vector3(2771.783f, 4927.182f, 33.69216f), 134.7913f),
                       new SpawnPoint(new Vector3(2769.842f, 4928.407f, 33.67304f), 153.8398f),
                       new SpawnPoint(new Vector3(2788.431f, 4958.26f, 33.69028f), 49.46712f),
                       new SpawnPoint(new Vector3(2884.707f, 5031.341f, 31.77167f), 292.4826f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(2785.79f, 4943.684f, 33.67175f), 133.6409f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(2787.543f, 4940.265f, 33.67414f), 314.2813f),
                       new SpawnPoint(new Vector3(2788.902f, 4941.611f, 33.67429f), 133.7348f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(2836.144f, 4987.05f, 33.38501f), 315.1639f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(2829.072f, 4977.984f, 33.62553f), 54.57702f),
                       new SpawnPoint(new Vector3(2827.645f, 4979.143f, 33.63948f), 230.7039f),
                   }
               ),


            new SuicideAttemptSpawn
               (
                   new SpawnPoint(new Vector3(108.3534f, 6368.369f, 45.33775f), 27.65498f),
                   new SpawnPoint(new Vector3(96.93967f, 6417.793f, 31.33926f), 174.6147f),
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(75.2521f, 6421.302f, 31.28833f), 308.2036f),
                       new SpawnPoint(new Vector3(84.29472f, 6428.005f, 31.33839f), 311.8462f),
                       new SpawnPoint(new Vector3(126.257f, 6389.932f, 31.25258f), 231.5939f),
                       new SpawnPoint(new Vector3(83.40215f, 6383.226f, 31.22905f), 162.2452f),
                       new SpawnPoint(new Vector3(82.0599f, 6377.256f, 31.23126f), 191.535f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(76.61074f, 6424.725f, 31.33901f), 45.42081f),
                       new SpawnPoint(new Vector3(79.59496f, 6428.059f, 31.34645f), 43.76426f),
                       new SpawnPoint(new Vector3(130.8679f, 6384.758f, 31.20673f), 307.5251f),
                       new SpawnPoint(new Vector3(82.2685f, 6372.832f, 31.23152f), 107.9485f),
                       new SpawnPoint(new Vector3(84.49995f, 6366.878f, 31.22801f), 95.39876f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(74.31832f, 6424.306f, 31.30355f), 45.50809f),
                       new SpawnPoint(new Vector3(77.05202f, 6427.166f, 31.50456f), 47.73585f),
                       new SpawnPoint(new Vector3(80.0294f, 6429.685f, 31.34096f), 46.48602f),
                       new SpawnPoint(new Vector3(132.086f, 6386.19f, 31.19231f), 321.8024f),
                       new SpawnPoint(new Vector3(80.82352f, 6372.393f, 31.23324f), 97.80984f),
                       new SpawnPoint(new Vector3(82.18304f, 6368.78f, 31.23034f), 109.2174f),
                       new SpawnPoint(new Vector3(83.85863f, 6365.79f, 31.22746f), 113.5532f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(93.41562f, 6391.687f, 31.22569f), 313.1449f),
                       new SpawnPoint(new Vector3(115.2304f, 6396.284f, 31.28495f), 43.74176f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(95.89371f, 6389.64f, 31.22569f), 223.2811f),
                       new SpawnPoint(new Vector3(94.67014f, 6388.049f, 31.22569f), 278.2572f),
                       new SpawnPoint(new Vector3(118.4508f, 6391.644f, 31.23275f), 268.2202f),
                       new SpawnPoint(new Vector3(119.474f, 6392.77f, 31.23738f), 124.5327f),
                   },
                   new List<SpawnPoint>()
                   {
                   },
                   new List<SpawnPoint>()
                   {
                   }
               ),


            new SuicideAttemptSpawn
               (
                   new SpawnPoint(new Vector3(330.7576f, 170.884f, 138.4827f), 124.9321f),
                   new SpawnPoint(new Vector3(313.1795f, 156.2044f, 103.8021f), 249.2539f),
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(347.3171f, 154.5414f, 102.9778f), 162.1866f),
                       new SpawnPoint(new Vector3(344.8656f, 146.8037f, 103.1304f), 160.5143f),
                       new SpawnPoint(new Vector3(342.8067f, 141.6563f, 103.1381f), 158.8926f),
                       new SpawnPoint(new Vector3(217.8505f, 178.5048f, 105.4093f), 337.0254f),
                       new SpawnPoint(new Vector3(221.9299f, 189.4845f, 105.5336f), 148.9367f),
                       new SpawnPoint(new Vector3(225.7491f, 198.3952f, 105.3745f), 337.156f),
                       new SpawnPoint(new Vector3(308.3029f, 158.3493f, 103.9036f), 242.9854f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(346.2168f, 150.7808f, 103.1236f), 255.2704f),
                       new SpawnPoint(new Vector3(341.0863f, 136.5048f, 103.1205f), 246.3986f),
                       new SpawnPoint(new Vector3(339.5732f, 131.9447f, 102.9587f), 253.1732f),
                       new SpawnPoint(new Vector3(219.3977f, 182.3181f, 105.5351f), 68.28085f),
                       new SpawnPoint(new Vector3(220.2045f, 185.5325f, 105.55f), 66.62826f),
                       new SpawnPoint(new Vector3(227.2632f, 202.4962f, 105.4685f), 73.67058f),
                       new SpawnPoint(new Vector3(336.5249f, 127.9352f, 103.1308f), 233.7339f),
                       new SpawnPoint(new Vector3(351.0498f, 162.8085f, 103.0943f), 234.0858f),
                       new SpawnPoint(new Vector3(354.2745f, 170.9133f, 103.0976f), 254.1941f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(347.9216f, 150.2519f, 103.0774f), 252.935f),
                       new SpawnPoint(new Vector3(343.2876f, 137.6688f, 103.0924f), 247.9998f),
                       new SpawnPoint(new Vector3(341.6062f, 133.207f, 102.9934f), 243.4883f),
                       new SpawnPoint(new Vector3(340.7392f, 130.7249f, 102.9272f), 247.3801f),
                       new SpawnPoint(new Vector3(217.4946f, 182.6479f, 105.5898f), 67.25607f),
                       new SpawnPoint(new Vector3(218.6303f, 185.4108f, 105.5785f), 44.08645f),
                       new SpawnPoint(new Vector3(221.8213f, 193.2683f, 105.5823f), 71.25563f),
                       new SpawnPoint(new Vector3(225.5885f, 203.1307f, 105.4971f), 69.23617f),
                       new SpawnPoint(new Vector3(228.2539f, 206.0958f, 105.4786f), 42.45189f),
                       new SpawnPoint(new Vector3(230.5835f, 207.9768f, 105.4498f), 33.90849f),
                       new SpawnPoint(new Vector3(213.875f, 171.2748f, 105.4987f), 74.6042f),
                       new SpawnPoint(new Vector3(335.2865f, 126.4341f, 103.1409f), 206.5435f),
                       new SpawnPoint(new Vector3(338.124f, 127.7657f, 103.1065f), 238.4419f),
                       new SpawnPoint(new Vector3(350.709f, 160.9299f, 103.0948f), 254.2354f),
                       new SpawnPoint(new Vector3(351.9355f, 163.8985f, 103.0854f), 257.0153f),
                       new SpawnPoint(new Vector3(354.4156f, 169.7514f, 103.0885f), 255.4751f),
                       new SpawnPoint(new Vector3(356.2815f, 172.1989f, 103.0713f), 252.2326f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(263.2309f, 169.6682f, 104.7537f), 247.0455f),
                       new SpawnPoint(new Vector3(318.2849f, 142.4598f, 103.5205f), 69.54686f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(267.9384f, 167.5686f, 104.6624f), 254.3455f),
                       new SpawnPoint(new Vector3(318.5559f, 145.6131f, 103.6428f), 342.2027f),
                       new SpawnPoint(new Vector3(321.0806f, 145.5443f, 103.6001f), 15.60574f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(292.2164f, 172.5164f, 104.225f), 69.73193f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(290.2231f, 170.2133f, 104.3004f), 156.7752f),
                       new SpawnPoint(new Vector3(287.4923f, 170.7325f, 104.3511f), 224.1202f),
                   }
               ),


            new SuicideAttemptSpawn
               (
                   new SpawnPoint(new Vector3(-488.022f, -980.8337f, 52.76935f), 316.0883f),
                   new SpawnPoint(new Vector3(-523.3178f, -959.164f, 23.54115f), 231.3549f),
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-531.9332f, -987.4395f, 23.15899f), 93.15408f),
                       new SpawnPoint(new Vector3(-540.5856f, -987.3848f, 23.35711f), 248.7968f),
                       new SpawnPoint(new Vector3(-551.0029f, -965.7678f, 23.42498f), 31.29212f),
                       new SpawnPoint(new Vector3(-551.8945f, -958.5146f, 23.50056f), 26.90772f),
                       new SpawnPoint(new Vector3(-538.2984f, -942.0574f, 23.77691f), 61.11602f),
                       new SpawnPoint(new Vector3(-532.8286f, -944.4274f, 23.68018f), 240.7207f),
                       new SpawnPoint(new Vector3(-524.0172f, -948.098f, 23.38776f), 241.6471f),
                       new SpawnPoint(new Vector3(-522.5598f, -957.3256f, 23.54908f), 223.1211f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-536.5705f, -987.8738f, 23.34185f), 180.6582f),
                       new SpawnPoint(new Vector3(-545.5087f, -986.6236f, 23.26682f), 173.508f),
                       new SpawnPoint(new Vector3(-552.0057f, -954.3661f, 23.38855f), 83.85408f),
                       new SpawnPoint(new Vector3(-552.7899f, -961.7868f, 23.48784f), 76.3795f),
                       new SpawnPoint(new Vector3(-529.1766f, -946.4301f, 23.59148f), 333.2577f),
                       new SpawnPoint(new Vector3(-521.9363f, -951.463f, 23.51649f), 331.5432f),
                       new SpawnPoint(new Vector3(-543.1967f, -940.4855f, 23.84133f), 330.4341f),
                       new SpawnPoint(new Vector3(-552.319f, -948.127f, 23.46941f), 93.49747f),
                       new SpawnPoint(new Vector3(-555.3041f, -968.9105f, 23.4387f), 82.02389f),
                       new SpawnPoint(new Vector3(-550.7807f, -986.2552f, 23.29917f), 179.0373f),
                       new SpawnPoint(new Vector3(-527.2454f, -986.8978f, 23.30792f), 177.3163f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-534.8383f, -989.2034f, 23.29871f), 178.5461f),
                       new SpawnPoint(new Vector3(-544.2579f, -988.6384f, 23.31411f), 173.1338f),
                       new SpawnPoint(new Vector3(-547.1477f, -987.7692f, 23.17203f), 171.1519f),
                       new SpawnPoint(new Vector3(-554.1518f, -961.6959f, 23.48137f), 76.60278f),
                       new SpawnPoint(new Vector3(-553.6132f, -953.8079f, 23.37682f), 78.89326f),
                       new SpawnPoint(new Vector3(-528.4441f, -944.7684f, 23.62101f), 327.0723f),
                       new SpawnPoint(new Vector3(-542.4929f, -939.3054f, 23.8722f), 329.6849f),
                       new SpawnPoint(new Vector3(-553.7062f, -948.2505f, 23.47344f), 90.85017f),
                       new SpawnPoint(new Vector3(-556.976f, -968.5121f, 23.47548f), 78.27766f),
                       new SpawnPoint(new Vector3(-550.8876f, -987.8222f, 23.29421f), 176.6771f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-542.9094f, -972.1572f, 23.53268f), 25.82064f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-541.1431f, -971.2797f, 23.52636f), 303.8152f),
                       new SpawnPoint(new Vector3(-540.0581f, -974.0665f, 23.50714f), 342.2905f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-540.5156f, -954.9861f, 23.55484f), 265.6229f),
                       new SpawnPoint(new Vector3(-529.1574f, -966.2684f, 23.53911f), 305.6036f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-535.4346f, -955.1219f, 23.56619f), 231.2895f),
                       new SpawnPoint(new Vector3(-535.7892f, -956.5049f, 23.54426f), 278.0438f),
                       new SpawnPoint(new Vector3(-529.1877f, -963.8044f, 23.54836f), 33.0513f),
                       new SpawnPoint(new Vector3(-524.6174f, -964.5449f, 23.5477f), 257.0279f),
                   }
               ),


            new SuicideAttemptSpawn
               (
                   new SpawnPoint(new Vector3(-1101.288f, -851.5521f, 38.7044f), 221.6626f),
                   new SpawnPoint(new Vector3(-1060.014f, -863.8378f, 4.933522f), 104.734f),
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-1137.803f, -862.239f, 13.45063f), 133.0729f),
                       new SpawnPoint(new Vector3(-1127.391f, -878.0392f, 10.60736f), 267.2324f),
                       new SpawnPoint(new Vector3(-1055.235f, -871.1297f, 5.157265f), 264.1165f),
                       new SpawnPoint(new Vector3(-1064.463f, -875.4673f, 5.015927f), 167.3337f),
                       new SpawnPoint(new Vector3(-1058.824f, -862.843f, 4.925654f), 22.21939f),
                       new SpawnPoint(new Vector3(-1112.717f, -864.5632f, 8.120633f), 128.3884f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-1142.45f, -865.9733f, 13.61712f), 19.29858f),
                       new SpawnPoint(new Vector3(-1059.853f, -869.6917f, 5.086468f), 209.6405f),
                       new SpawnPoint(new Vector3(-1065.469f, -871.7625f, 4.986918f), 229.6836f),
                       new SpawnPoint(new Vector3(-1112.126f, -867.2162f, 8.336651f), 120.0017f),
                       new SpawnPoint(new Vector3(-1122.689f, -877.6669f, 10.60736f), 45.22285f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-1142.887f, -864.6218f, 13.63888f), 17.30429f),
                       new SpawnPoint(new Vector3(-1059.792f, -870.9772f, 5.097466f), 193.2055f),
                       new SpawnPoint(new Vector3(-1113.113f, -867.8578f, 8.563097f), 122.4614f),
                       new SpawnPoint(new Vector3(-1123.459f, -876.8987f, 10.60736f), 39.68614f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-1076.847f, -882.3956f, 4.665352f), 89.76911f),
                       new SpawnPoint(new Vector3(-1060.878f, -854.4304f, 4.869049f), 294.7552f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-1078.102f, -880.5876f, 4.683756f), 359.5392f),
                       new SpawnPoint(new Vector3(-1075.955f, -880.5353f, 4.702432f), 45.4593f),
                       new SpawnPoint(new Vector3(-1065.429f, -855.9905f, 4.86749f), 131.5941f),
                       new SpawnPoint(new Vector3(-1064.65f, -857.0073f, 4.867586f), 95.72523f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-1075.343f, -868.9801f, 4.843805f), 124.1851f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-1079.317f, -872.2593f, 4.801081f), 115.0638f),
                       new SpawnPoint(new Vector3(-1080.086f, -870.7634f, 4.832372f), 76.28505f),
                   }
               ),



            new SuicideAttemptSpawn
               (
                   new SpawnPoint(new Vector3(-144.835f, -107.5535f, 94.81061f), 343.8866f),
                   new SpawnPoint(new Vector3(-121.0495f, -106.7616f, 56.68805f), 359.8477f),
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-228.3889f, -70.05643f, 49.63566f), 14.3881f),
                       new SpawnPoint(new Vector3(-226.5342f, -63.89933f, 49.76894f), 14.99535f),
                       new SpawnPoint(new Vector3(-226.6118f, -52.32307f, 49.6781f), 131.6754f),
                       new SpawnPoint(new Vector3(-223.1387f, -46.35701f, 49.6573f), 346.7068f),
                       new SpawnPoint(new Vector3(-93.33922f, -91.33922f, 57.73879f), 155.0816f),
                       new SpawnPoint(new Vector3(-96.50531f, -98.63019f, 57.80209f), 153.7456f),
                       new SpawnPoint(new Vector3(-102.9712f, -114.8548f, 57.66936f), 327.4388f),
                       new SpawnPoint(new Vector3(-121.9904f, -108.1966f, 56.60099f), 61.00433f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-228.4421f, -65.68147f, 49.71582f), 73.43626f),
                       new SpawnPoint(new Vector3(-227.267f, -75.89429f, 49.80537f), 75.15601f),
                       new SpawnPoint(new Vector3(-225.0392f, -49.20914f, 49.70654f), 62.23419f),
                       new SpawnPoint(new Vector3(-220.7493f, -40.9796f, 49.74672f), 66.05988f),
                       new SpawnPoint(new Vector3(-101.6993f, -109.4037f, 57.73512f), 250.9579f),
                       new SpawnPoint(new Vector3(-98.72923f, -103.037f, 57.76857f), 248.099f),
                       new SpawnPoint(new Vector3(-94.46798f, -95.12363f, 57.81102f), 241.6126f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-229.6941f, -65.37851f, 49.65539f), 75.33883f),
                       new SpawnPoint(new Vector3(-228.2414f, -75.58265f, 49.77684f), 77.83814f),
                       new SpawnPoint(new Vector3(-228.7111f, -59.66917f, 49.63424f), 90.1688f),
                       new SpawnPoint(new Vector3(-228.0758f, -56.08979f, 49.65386f), 69.18683f),
                       new SpawnPoint(new Vector3(-226.1602f, -48.7415f, 49.66788f), 70.51457f),
                       new SpawnPoint(new Vector3(-222.2965f, -41.45062f, 49.69379f), 70.05981f),
                       new SpawnPoint(new Vector3(-221.3499f, -38.47011f, 49.7066f), 71.3563f),
                       new SpawnPoint(new Vector3(-97.28152f, -103.4936f, 57.83467f), 248.6536f),
                       new SpawnPoint(new Vector3(-98.68603f, -106.6908f, 57.84082f), 235.1432f),
                       new SpawnPoint(new Vector3(-100.3963f, -110.3703f, 57.79728f), 237.4894f),
                       new SpawnPoint(new Vector3(-93.47193f, -95.56768f, 57.84706f), 245.0598f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-113.2377f, -85.43033f, 56.8363f), 40.762f),
                       new SpawnPoint(new Vector3(-161.5972f, -82.84329f, 53.99609f), 237.3475f),
                       new SpawnPoint(new Vector3(-119.6123f, -94.24738f, 56.67811f), 245.5502f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-116.3413f, -84.18882f, 56.6404f), 101.2462f),
                       new SpawnPoint(new Vector3(-162.2715f, -84.66953f, 53.83499f), 176.4057f),
                       new SpawnPoint(new Vector3(-163.2655f, -84.76654f, 53.77077f), 222.5136f),
                       new SpawnPoint(new Vector3(-115.2227f, -95.72778f, 56.93668f), 250.3127f),
                       new SpawnPoint(new Vector3(-115.7671f, -96.86688f, 56.92376f), 305.327f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-164.803f, -92.96269f, 53.62223f), 67.08161f),
                       new SpawnPoint(new Vector3(-159.0705f, -68.85629f, 53.64927f), 70.45621f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-159.6427f, -93.78274f, 54.02866f), 248.6857f),
                       new SpawnPoint(new Vector3(-160.6926f, -96.11295f, 54.10279f), 260.4659f),
                       new SpawnPoint(new Vector3(-154.6222f, -70.65466f, 53.98808f), 213.053f),
                       new SpawnPoint(new Vector3(-155.7911f, -71.65115f, 53.98499f), 247.4144f),
                       new SpawnPoint(new Vector3(-160.8799f, -69.96001f, 53.61446f), 158.5718f),
                   }
               ),

            new SuicideAttemptSpawn
               (
                   new SpawnPoint(new Vector3(776.4448f, -1296.83f, 48.07039f), 252.5659f),
                   new SpawnPoint(new Vector3(805.3906f, -1299.608f, 26.29626f), 79.5387f),
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(807.2925f, -1299.94f, 26.23132f), 345.2682f),
                       new SpawnPoint(new Vector3(789.875f, -1329.576f, 26.2128f), 91.22973f),
                       new SpawnPoint(new Vector3(801.8932f, -1329.627f, 26.27048f), 269.9019f),
                       new SpawnPoint(new Vector3(807.3218f, -1329.864f, 26.17175f), 311.1649f),
                       new SpawnPoint(new Vector3(795.6609f, -1265.808f, 26.42508f), 260.9825f),
                       new SpawnPoint(new Vector3(807.9796f, -1267.616f, 26.34997f), 260.6899f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(794.1865f, -1328.043f, 26.27552f), 183.0633f),
                       new SpawnPoint(new Vector3(804.0535f, -1327.501f, 26.26383f), 175.9335f),
                       new SpawnPoint(new Vector3(812.3188f, -1327.395f, 26.21065f), 179.1338f),
                       new SpawnPoint(new Vector3(785.2324f, -1268.097f, 26.40779f), 358.8934f),
                       new SpawnPoint(new Vector3(813.7949f, -1270.507f, 26.36569f), 0.1336545f),
                       new SpawnPoint(new Vector3(803.1942f, -1267.805f, 26.46327f), 2.028894f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(784.9933f, -1329.455f, 26.16064f), 166.0747f),
                       new SpawnPoint(new Vector3(794.4849f, -1330.296f, 26.29007f), 176.8057f),
                       new SpawnPoint(new Vector3(798.2026f, -1329.813f, 26.43394f), 179.2942f),
                       new SpawnPoint(new Vector3(812.3785f, -1328.579f, 26.21822f), 180.6612f),
                       new SpawnPoint(new Vector3(790.1775f, -1266.074f, 26.31467f), 12.84253f),
                       new SpawnPoint(new Vector3(785.3401f, -1266.816f, 26.39177f), 354.313f),
                       new SpawnPoint(new Vector3(799.973f, -1265.73f, 26.43534f), 359.8794f),
                       new SpawnPoint(new Vector3(803.569f, -1266.432f, 26.46356f), 341.9168f),
                       new SpawnPoint(new Vector3(813.9077f, -1269.065f, 26.33918f), 350.246f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(791.6596f, -1276.562f, 26.40128f), 310.5128f),
                       new SpawnPoint(new Vector3(800.6523f, -1314.968f, 26.24656f), 213.2741f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(791.9745f, -1278.647f, 26.40171f), 219.7789f),
                       new SpawnPoint(new Vector3(801.4211f, -1312.436f, 26.25421f), 25.15751f),
                       new SpawnPoint(new Vector3(802.2321f, -1311.387f, 26.2583f), 71.06003f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(804.2877f, -1281.315f, 26.41314f), 66.82616f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(802.2251f, -1282.561f, 26.39591f), 208.0656f),
                       new SpawnPoint(new Vector3(804.0251f, -1283.285f, 26.393f), 101.7474f),
                       new SpawnPoint(new Vector3(802.7093f, -1284.575f, 26.37658f), 337.0999f),
                   }
               ),


            new SuicideAttemptSpawn
               (
                   new SpawnPoint(new Vector3(787.3685f, -1766.866f, 53.28949f), 314.6884f),
                   new SpawnPoint(new Vector3(825.9943f, -1750.034f, 29.47405f), 113.4121f),
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(748.2349f, -1751.014f, 29.32727f), 5.613099f),
                       new SpawnPoint(new Vector3(748.4354f, -1742.342f, 29.47464f), 172.6455f),
                       new SpawnPoint(new Vector3(749.5963f, -1733.42f, 29.43648f), 351.6477f),
                       new SpawnPoint(new Vector3(813.4019f, -1730.141f, 29.18249f), 260.0532f),
                       new SpawnPoint(new Vector3(823.46f, -1731.348f, 29.35668f), 81.80263f),
                       new SpawnPoint(new Vector3(830.0418f, -1731.945f, 29.35759f), 76.16367f),
                       new SpawnPoint(new Vector3(840.0202f, -1732.887f, 29.37374f), 93.1096f),
                       new SpawnPoint(new Vector3(852.1328f, -1745.5f, 29.5075f), 165.2521f),
                       new SpawnPoint(new Vector3(849.7987f, -1754.463f, 29.53893f), 341.4178f),
                       new SpawnPoint(new Vector3(845.1887f, -1768.422f, 29.15558f), 147.2237f),
                       new SpawnPoint(new Vector3(826.8991f, -1748.309f, 29.48708f), 55.94612f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(749.9985f, -1737.897f, 29.47887f), 72.34666f),
                       new SpawnPoint(new Vector3(752.1688f, -1725.617f, 29.42767f), 86.57404f),
                       new SpawnPoint(new Vector3(749.7268f, -1756.37f, 29.26284f), 81.76888f),
                       new SpawnPoint(new Vector3(818.3455f, -1732.028f, 29.34976f), 355.6464f),
                       new SpawnPoint(new Vector3(836.0327f, -1734.313f, 29.41279f), 352.8556f),
                       new SpawnPoint(new Vector3(842.0338f, -1734.685f, 29.37562f), 1.379172f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(747.9692f, -1759.519f, 29.36866f), 117.7877f),
                       new SpawnPoint(new Vector3(747.8995f, -1755.391f, 29.24239f), 77.31458f),
                       new SpawnPoint(new Vector3(747.3664f, -1746.682f, 29.43551f), 84.78155f),
                       new SpawnPoint(new Vector3(748.1441f, -1738.017f, 29.47079f), 83.74291f),
                       new SpawnPoint(new Vector3(750.1846f, -1728.757f, 29.40621f), 82.60764f),
                       new SpawnPoint(new Vector3(750.6683f, -1724.772f, 29.42493f), 94.09386f),
                       new SpawnPoint(new Vector3(818.4669f, -1730.856f, 29.33764f), 2.128431f),
                       new SpawnPoint(new Vector3(835.5903f, -1732.792f, 29.37543f), 347.5792f),
                       new SpawnPoint(new Vector3(845.2075f, -1732.663f, 29.19951f), 23.9518f),
                       new SpawnPoint(new Vector3(850.726f, -1729.533f, 29.26747f), 15.00023f),
                       new SpawnPoint(new Vector3(853.4224f, -1736.012f, 29.17417f), 266.5632f),
                       new SpawnPoint(new Vector3(853.0251f, -1740.042f, 29.42911f), 280.6255f),
                       new SpawnPoint(new Vector3(851.4307f, -1750.072f, 29.57176f), 251.1914f),
                       new SpawnPoint(new Vector3(849.7119f, -1758.888f, 29.43718f), 265.6445f),
                       new SpawnPoint(new Vector3(849.1758f, -1762.573f, 29.30588f), 246.7318f),
                       new SpawnPoint(new Vector3(840.6828f, -1768.883f, 29.10835f), 162.6979f),
                       new SpawnPoint(new Vector3(835.7004f, -1768.218f, 29.23069f), 170.0688f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(773.1996f, -1742.047f, 29.55799f), 299.527f),
                       new SpawnPoint(new Vector3(828.2696f, -1759.001f, 29.31774f), 135.8113f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(776.5237f, -1740.849f, 29.57281f), 252.7906f),
                       new SpawnPoint(new Vector3(827.3958f, -1757.407f, 29.34078f), 53.21704f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(809.2177f, -1730.792f, 29.21758f), 354.2262f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(809.3374f, -1735.896f, 29.198f), 198.7213f),
                       new SpawnPoint(new Vector3(810.7834f, -1735.278f, 29.21396f), 180.933f),
                   }
               ),
        };
    }
}
