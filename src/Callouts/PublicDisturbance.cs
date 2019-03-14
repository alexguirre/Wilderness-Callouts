namespace WildernessCallouts.Callouts
{
    using Rage;
    using Rage.Native;
    using LSPD_First_Response.Mod.API;
    using LSPD_First_Response.Mod.Callouts;
    using System;
    using System.Drawing;
    using WildernessCallouts.Types;

    [CalloutInfo("PublicDisturbance", CalloutProbability.Medium)]
    internal class PublicDisturbance : CalloutBase
    {
        //Here we declare our variables, things we need or our callout
        private Ped ped; // a rage ped
        private Vector3 spawnPoint; // a Vector3
        private Blip blip; // a rage blip

        private static WeaponAsset[] weaponAssets = { "WEAPON_MICROSMG", "WEAPON_SMG", "WEAPON_ASSAULTRIFLE", "WEAPON_APPISTOL", "WEAPON_PISTOL50", "WEAPON_PISTOL", "WEAPON_PUMPSHOTGUN", "WEAPON_COMBATPISTOL",
                                                      "WEAPON_MG", "WEAPON_COMBATMG", "WEAPON_SAWNOFFSHOTGUN", "WEAPON_FIREWORK", "WEAPON_FLAREGUN", "WEAPON_COMBATPDW", "WEAPON_MARKSMANPISTOL", "WEAPON_MARKSMANRIFLE",
                                                      "WEAPON_HEAVYSHOTGUN", "WEAPON_VINTAGEPISTOL", "WEAPON_FIREWORK", "WEAPON_FLAREGUN", "WEAPON_FIREWORK", "WEAPON_FLAREGUN"
                                                    };


        private static Model[] acultModels = { "a_m_m_acult_01", "a_m_o_acult_01", "a_m_y_acult_01", "a_m_m_acult_01", "a_m_o_acult_01", "a_m_y_acult_01" };
        private static Model toplessModels = "a_f_y_topless_01";
        private static Model[] sunbatheModels = { "a_f_y_topless_01", "a_f_m_beach_01", "a_m_m_beach_02", "a_m_m_beach_01", "csb_porndudes", "a_f_y_beach_01", "a_m_o_beach_01", "a_m_y_beach_03", "a_m_y_beach_01", "a_m_y_musclbeac_01", "a_m_y_musclbeac_02", "a_m_y_sunbathe_01", "ig_tylerdix", "u_m_y_babyd" };
        private static Model[] epsilonModels = { "ig_chrisformage", "ig_tomepsilon", "a_f_y_epsilon_01", "a_m_y_epsilon_01", "a_m_y_epsilon_02" };
        private static Model[] guitarModels = { "prop_acc_guitar_01", "prop_el_guitar_01", "prop_el_guitar_02", "prop_el_guitar_03" };
        private static Model protestSignModel = "prop_cs_protest_sign_01";
        private static Model bongosModel = "prop_bongos_01";
        private static Model[] beggersSignModels = { "prop_beggers_sign_01", "prop_beggers_sign_02", "prop_beggers_sign_03", "prop_beggers_sign_04" };
        private static Model[] trampModels = { "a_f_m_trampbeac_01", "a_f_m_tramp_01", "a_m_m_trampbeac_01", "a_m_m_tramp_01", "a_m_o_tramp_01", "u_m_o_tramp_01" };
        private static Model[] pushupsModels = { "a_f_m_bodybuild_01", "a_m_m_beach_01", "a_m_m_beach_02", "csb_porndudes", "a_m_o_beach_01", "a_m_y_beach_01", "a_m_y_beach_03", "a_m_y_musclbeac_01", "a_m_y_musclbeac_01", "a_m_y_sunbathe_01", "a_m_y_surfer_01", "mp_m_exarmy_01" };


        private static AnimationDictionary[] sunbatheMale = { "amb@world_human_sunbathe@male@back@base", "amb@world_human_sunbathe@male@front@base" };
        private static AnimationDictionary[] sunbatheFemale = { "amb@world_human_sunbathe@female@back@base", "amb@world_human_sunbathe@female@front@base" };


        private static string[] monkeyFreakOutAnimNames = { "monkey_a_freakout_loop", "monkey_b_freakout_loop", "monkey_c_freakout_loop" };


        private static string[] epsilonVoicesMale = { "a_m_y_epsilon_01_black_full_01", "a_m_y_epsilon_01_korean_full_01", "a_m_y_epsilon_01_white_full_01", "a_m_y_epsilon_02_white_mini_01" };
        private static string[] epsilonVoicesFemale = { "a_f_y_epsilon_01_white_mini_01" };


        private Rage.Object pedObj;

        private int scenario = Globals.Random.Next(1, 13);

        private bool hasEnded = false;

        /// <summary>
        /// OnBeforeCalloutDisplayed is where we create a blip for the user to see where the pursuit is happening, we initiliaize any variables above and set
        /// the callout message and position for the API to display
        /// </summary>
        /// <returns></returns>
        public override bool OnBeforeCalloutDisplayed()
        {
            //Set our spawn point to be on a street around 300f (distance) away from the player.
            spawnPoint = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.AroundPosition(230.0f));
            while (spawnPoint.DistanceTo(Game.LocalPlayer.Character.Position) < 30.0f)
            {
                spawnPoint = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.AroundPosition(230.0f)).GetSafeCoordinatesForPed();
                GameFiber.Yield();
            }
            if (spawnPoint == Vector3.Zero) spawnPoint = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.AroundPosition(195.0f));

            //Create our ped in the world
            if (scenario == 3 || scenario == 7)
            {
                ped = new Ped(acultModels.GetRandomElement(true), spawnPoint, MathHelper.GetRandomSingle(0.0f, 360.0f));
                if (ped.Model == new Model("a_m_m_acult_01") && Globals.Random.Next(1, 4) == 1)
                {
                    ped.SetVariation(9, 0, 0);
                    ped.SetVariation(2, 0, Globals.Random.Next(0, 3));
                }
            }
            else if (scenario == 2 && Globals.Random.Next(0, 20) < 1)
            {
                ped = new Ped("u_m_m_griff_01", spawnPoint, MathHelper.GetRandomSingle(0.0f, 360.0f));
            }
            else if (scenario == 5 || scenario == 11)
            {
                ped = new Ped(toplessModels, spawnPoint, 0.0f);
                if (ped.Exists()) ped.SetVariation(8, 1, 1);
            }
            else if (scenario == 6) ped = new Ped(sunbatheModels.GetRandomElement(true), spawnPoint, MathHelper.GetRandomSingle(0.0f, 360.0f));
            else if (scenario == 8) ped = new Ped(epsilonModels.GetRandomElement(true), spawnPoint, MathHelper.GetRandomSingle(0.0f, 360.0f));
            else if (scenario == 9) ped = new Ped(trampModels.GetRandomElement(true), spawnPoint, MathHelper.GetRandomSingle(0.0f, 360.0f));
            else if (scenario == 12) ped = new Ped(pushupsModels.GetRandomElement(true), spawnPoint, MathHelper.GetRandomSingle(0.0f, 360.0f));
            else ped = new Ped(spawnPoint);

            /*
            Rage.Object test = new Rage.Object(protestSignModel, Vector3.Zero);
            Rage.Object test2 = new Rage.Object(guitarModel, Vector3.Zero);
            test.Delete();
            test2.Delete();
            */
            //Now we have spawned them, check they actually exist and if not return false (preventing the callout from being accepted and aborting it)
            if (!ped.Exists()) return false;


            // Show the user where the pursuit is about to happen and block very close peds.
            this.ShowCalloutAreaBlipBeforeAccepting(spawnPoint, 17.5f);
            this.AddMinimumDistanceCheck(15.0f, ped.Position);

            // Set up our callout message and location
            this.CalloutMessage = "Public disturbance";
            this.CalloutPosition = spawnPoint;

            //Play the police scanner audio for this callout (available as of the 0.2a API)
            Functions.PlayScannerAudioUsingPosition("CITIZENS_REPORT CRIME_DISTURBANCE IN_OR_ON_POSITION UNITS_RESPOND_CODE_02", spawnPoint);

            return base.OnBeforeCalloutDisplayed();
        }


        /// <summary>
        /// OnCalloutAccepted is where we begin our callout's logic. In this instance we create our pursuit and add our ped from eariler to the pursuit as well
        /// </summary>
        /// <returns></returns>
        public override bool OnCalloutAccepted()
        {
            //We accepted the callout, so lets initilize our blip from before and attach it to our ped so we know where he is.
            blip = ped.AttachBlip();
            blip.Color = Color.OrangeRed;
            blip.EnableRoute(Color.OrangeRed);
            blip.SetName("Public disturbance");

            Game.DisplayHelp("Deal with the disturbance. Press " + Controls.ForceCalloutEnd.ToUserFriendlyName() + " to finish the callout", 13750);
            
            CreateScenario(scenario);

            return base.OnCalloutAccepted();
        }

        /// <summary>
        /// If you don't accept the callout this will be called, we clear anything we spawned here to prevent it staying in the game
        /// </summary>
        public override void OnCalloutNotAccepted()
        {
            base.OnCalloutNotAccepted();
            if (ped.Exists()) ped.Delete();
            if (blip.Exists()) blip.Delete();
            if (pedObj.Exists()) pedObj.Delete();
        }

        //This is where it all happens, run all of your callouts logic here
        public override void Process()
        {
            if (!ped.Exists() || Functions.IsPedArrested(ped) || ped.IsDead)
                this.End();

            base.Process();
        }

        /// <summary>
        /// More cleanup, when we call end you clean away anything left over
        /// This is also important as this will be called if a callout gets aborted (for example if you force a new callout)
        /// </summary>
        public override void End()
        {
            hasEnded = true;

            base.End();
            if (blip.Exists()) blip.Delete();
            if (ped.Exists())
            {
                ped.Tasks.Clear();
                ped.Dismiss();
            }
            if (pedObj.Exists())
            {
                pedObj.Detach();
                pedObj.Dismiss();
            }
        }

        protected override void CleanUp()
        {
            // TODO: implement CleanUp()
        }

        public void CreateScenario(int scenario)
        {
            GameFiber.StartNew(delegate
            {
                Logger.LogTrivial(this.GetType().Name, "Scenario: " + scenario);

                if (scenario == 1)     // SHOOT
                {
                    Logger.LogDebug(this.GetType().Name, "Scenario: Shoot");
                    ped.Inventory.GiveNewWeapon(weaponAssets.GetRandomElement(true), 9999, true);

                    Vector3 posToShoot = ped.Position + (ped.ForwardVector * MathHelper.GetRandomSingle(1.5f, 8.0f)) + (ped.UpVector * MathHelper.GetRandomSingle(8.0f, 20.0f)) + (ped.RightVector * MathHelper.GetRandomSingle(-15.0f, 15.0f));
                    NativeFunction.Natives.TASK_SHOOT_AT_COORD(ped, posToShoot.X, posToShoot.Y, posToShoot.Z, -1, (uint)Rage.FiringPattern.BurstFire);
                }
                else if (scenario == 2)     // PROTEST
                {
                    Logger.LogDebug(this.GetType().Name, "Scenario: Protest");
                    ped.Tasks.PlayAnimation("special_ped@griff@monologue_1@monologue_1e", "iamnotaracist_4", 5.0f, AnimationFlags.UpperBodyOnly | AnimationFlags.SecondaryTask | AnimationFlags.Loop);
                    ped.Tasks.Wander();
                    pedObj = new Rage.Object("prop_cs_protest_sign_01", Vector3.Zero);
                    pedObj.AttachToEntity(ped, ped.GetBoneIndex(PedBoneId.RightPhHand), Vector3.Zero, Rotator.Zero);
                    GameFiber.StartNew(delegate
                    {
                        //GameFiber.StartNew(delegate
                        //{
                        //    while (!hasEnded && ped.Exists() && ped.IsAlive && !Functions.IsPedArrested(ped))
                        //    {
                        //        if (Vector3.Distance(Game.LocalPlayer.Character.Position, ped.Position) < 17.5f)
                        //        {
                        //            if (new Random().Next(0, 151) < 20)
                        //            {
                        //                string[] protestSpeeches = { "GENERIC_CURSE_MED", "GENERIC_CURSE_HIGH", "GENERIC_FUCK_YOU" };
                        //                ped.PlayAmbientSpeech(null, protestSpeeches[new Random().Next(protestSpeeches.Length)], 0, SpeechModifier.Force);
                        //            }
                        //            GameFiber.Sleep(2500);
                        //        }
                        //        GameFiber.Yield();
                        //    }
                        //});
                        GameFiber.Sleep(125);
                        while (ped.IsPlayingAnimation("special_ped@griff@monologue_1@monologue_1e", "iamnotaracist_4") && ped.IsPersistent)
                        {
                            GameFiber.Yield();
                        }
                        if (!hasEnded && pedObj.Exists()) pedObj.Detach();
                        if (!hasEnded && pedObj.Exists()) pedObj.Dismiss();
                    });
                }
                else if (scenario == 3)        // DRUNK NUDE GUY
                {
                    Logger.LogDebug(this.GetType().Name, "Scenario: Drunk nude guy");
                    ped.SetMovementAnimationSet("move_m@drunk@verydrunk");
                    ped.Tasks.Wander();
                }
                else if (scenario == 4)     // GUITARRIST
                {
                    Logger.LogDebug(this.GetType().Name, "Scenario: Guitarrist");

                    ped.Tasks.PlayAnimation("amb@world_human_musician@guitar@male@base", "base", 5.0f, AnimationFlags.UpperBodyOnly | AnimationFlags.SecondaryTask | AnimationFlags.Loop);
                    ped.Tasks.Wander();
                    pedObj = new Rage.Object(guitarModels.GetRandomElement(true), Vector3.Zero);
                    pedObj.AttachToEntity(ped, ped.GetBoneIndex(PedBoneId.LeftPhHand), Vector3.Zero, Rotator.Zero);
                    GameFiber.StartNew(delegate
                    {
                        GameFiber.Sleep(500);
                        while (ped.IsPlayingAnimation("amb@world_human_musician@guitar@male@base", "base") && ped.IsPersistent)
                        {
                            GameFiber.Yield();
                        }
                        if (!hasEnded && pedObj.Exists()) pedObj.Detach();
                        if (!hasEnded && pedObj.Exists()) pedObj.Dismiss();
                    });
                }
                else if (scenario == 5)         // TOPLESS GIRL
                {
                    Logger.LogDebug(this.GetType().Name, "Scenario: Topless girl");
                    ped.Tasks.PlayAnimation("amb@world_human_prostitute@hooker@base", "base", 5.0f, AnimationFlags.UpperBodyOnly | AnimationFlags.SecondaryTask | AnimationFlags.Loop);
                    ped.Tasks.Wander();
                }
                else if (scenario == 6)     // SUNBATHE
                {
                    Logger.LogDebug(this.GetType().Name, "Scenario: Sunbathe");

                    AnimationDictionary maleAnimDict = sunbatheMale.GetRandomElement(true);
                    AnimationDictionary femaleAnimDict = sunbatheFemale.GetRandomElement(true);

                    if (ped.IsMale) ped.Tasks.PlayAnimation(maleAnimDict, "base", 2.0f, AnimationFlags.Loop);
                    else if (ped.IsFemale) ped.Tasks.PlayAnimation(femaleAnimDict, "base", 2.0f, AnimationFlags.Loop);

                    ped.BlockPermanentEvents = true;

                    GameFiber.StartNew(delegate
                    {
                        GameFiber.Sleep(500);
                        if (ped.IsMale)
                        {
                            while (ped.IsPlayingAnimation(maleAnimDict, "base") && ped.IsPersistent)
                            {
                                GameFiber.Yield();
                            }
                            if (!hasEnded && ped.Exists()) ped.BlockPermanentEvents = false;
                        }
                        else if (ped.IsFemale)
                        {
                            while (ped.IsPlayingAnimation(femaleAnimDict, "base") && ped.IsPersistent)
                            {
                                GameFiber.Yield();
                            }
                            if (!hasEnded && ped.Exists()) ped.BlockPermanentEvents = false;
                        }
                    });
                }
                else if (scenario == 7)        // FREAK OUT GUY
                {
                    Logger.LogDebug(this.GetType().Name, "Scenario: Freak out guy");

                    string animName = monkeyFreakOutAnimNames.GetRandomElement();
                    ped.Tasks.PlayAnimation("missfbi5ig_30monkeys", animName, 5.0f, AnimationFlags.Loop);
                    ped.BlockPermanentEvents = true;
                    GameFiber.StartNew(delegate
                    {
                        GameFiber.Sleep(500);
                        while (ped.IsPlayingAnimation("missfbi5ig_30monkeys", animName) && ped.IsPersistent&& ped.Exists())
                        {
                            GameFiber.Yield();
                        }
                        if (!hasEnded && ped.Exists()) ped.BlockPermanentEvents = false;
                    });
                }
                else if(scenario == 8)      // EPSILON
                {
                    Logger.LogDebug(this.GetType().Name, "Scenario: Epsilon");

                    ped.Tasks.PlayAnimation("rcmepsilonism3", "ep_3_rcm_marnie_meditating", 2.0f, AnimationFlags.Loop);
                    ped.BlockPermanentEvents = true;
                    GameFiber.StartNew(delegate
                    {
                        GameFiber.Sleep(500);
                        while (!hasEnded)
                        {
                            if (ped.Exists())
                            {
                                if (Vector3.Distance(Game.LocalPlayer.Character.Position, ped.Position) < 6.0f)
                                {
                                    if (ped.IsMale) ped.PlayAmbientSpeech(epsilonVoicesMale.GetRandomElement(), "KIFFLOM_GREET", 0, SpeechModifier.Force);
                                    else ped.PlayAmbientSpeech(epsilonVoicesFemale.GetRandomElement(), "KIFFLOM_GREET", 0, SpeechModifier.Force);
                                    break;
                                }
                            }
                            GameFiber.Yield();
                        }
                        if (!hasEnded && ped.Exists()) ped.BlockPermanentEvents = false;
                    });
                }
                else if (scenario == 9)     // BONGOS
                {
                    Logger.LogDebug(this.GetType().Name, "Scenario: Bongos");

                    ped.Tasks.PlayAnimation("amb@world_human_musician@bongos@male@idle_a", "idle_a", 5.0f, AnimationFlags.UpperBodyOnly | AnimationFlags.SecondaryTask | AnimationFlags.Loop);
                    ped.Tasks.Wander();
                    pedObj = new Rage.Object(bongosModel, Vector3.Zero);
                    pedObj.AttachToEntity(ped, ped.GetBoneIndex(PedBoneId.LeftPhHand), Vector3.Zero, Rotator.Zero);
                    GameFiber.StartNew(delegate
                    {
                        GameFiber.Sleep(500);
                        while (ped.IsPlayingAnimation("amb@world_human_musician@bongos@male@idle_a", "idle_a") && ped.IsPersistent && ped.Exists())
                        {
                            GameFiber.Yield();
                        }
                        if (!hasEnded && pedObj.Exists()) pedObj.Detach();
                        if (!hasEnded && pedObj.Exists()) pedObj.Dismiss();
                    });
                }
                else if(scenario == 10)     // BEGGER
                {
                    Logger.LogDebug(this.GetType().Name, "Scenario: Begger");
                    string[] idles = { "idle_a", "idle_b", "idle_c" };
                    string idleUsed = idles.GetRandomElement();
                    ped.Tasks.PlayAnimation("amb@world_human_bum_freeway@male@idle_a", idleUsed, 5.0f, AnimationFlags.UpperBodyOnly | AnimationFlags.SecondaryTask | AnimationFlags.Loop);
                    ped.Tasks.Wander();
                    pedObj = new Rage.Object(beggersSignModels.GetRandomElement(), Vector3.Zero);
                    pedObj.AttachToEntity(ped, ped.GetBoneIndex(PedBoneId.RightPhHand), Vector3.Zero, Rotator.Zero);
                    GameFiber.StartNew(delegate
                    {
                        GameFiber.Sleep(125);
                        while (ped.IsPlayingAnimation("amb@world_human_bum_freeway@male@idle_a", idleUsed) && ped.IsPersistent && ped.Exists())
                        {
                            GameFiber.Yield();
                        }
                        if (!hasEnded && pedObj.Exists()) pedObj.Detach();
                        if (!hasEnded && pedObj.Exists()) pedObj.Dismiss();
                    });
                }
                else if (scenario == 11)    // NUDE GIRL CHEERING
                {
                    Logger.LogDebug(this.GetType().Name, "Scenario: Topless girl cheering");
                    char[] letters = { 'a', 'b', 'c', 'd' };
                    ped.Tasks.PlayAnimation("amb@world_human_cheering@female_" + letters.GetRandomElement(), "base", 5.0f, AnimationFlags.Loop);
                }
                else if (scenario == 12)     // Pushups
                {
                    Logger.LogDebug(this.GetType().Name, "Scenario: Pushups");

                    Tuple<AnimationDictionary, string>[] anims = { new Tuple<AnimationDictionary, string>("amb@world_human_push_ups@male@idle_a", "idle_d"), new Tuple<AnimationDictionary, string>("amb@world_human_push_ups@male@base", "base") };
                    Tuple<AnimationDictionary, string> animUsed = anims.GetRandomElement();

                    ped.Tasks.PlayAnimation(animUsed.Item1, animUsed.Item2, 2.0f, AnimationFlags.Loop);

                    ped.BlockPermanentEvents = true;

                    GameFiber.StartNew(delegate
                    {
                        GameFiber.Sleep(500);
                        while (ped.IsPlayingAnimation(animUsed.Item1, animUsed.Item2) && ped.IsPersistent && ped.Exists())
                        {
                            GameFiber.Yield();
                        }
                        if (!hasEnded && ped.Exists()) ped.BlockPermanentEvents = false;
                    });
                }
            });

        }
    }
}
