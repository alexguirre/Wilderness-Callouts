namespace WildernessCallouts.Callouts
{
    // System
    using System;
    using System.Drawing;
    using System.Collections.Generic;

    // RPH
    using Rage;
    using Rage.Native;

    // RNUI
    using RAGENativeUI;
    using RAGENativeUI.Elements;

    // Wilderness
    using WildernessCallouts.Types;

    internal partial class MurderInvestigation : CalloutBase
    {
        /// <summary>
        /// Victim killed with knife
        /// </summary>
        internal class Variation1 : VariationBaseClass
        {
            Ped victimPed = null;
            Ped murdererPed = null;

            List<Rage.Object> objects;

            Rage.Object cctvObject = null;
            Rage.Object knifeObject = null;

            Camera footageCamera = null;
            Ped victimPedClone = null;
            Ped murdererPedClone = null;

            Blip blipLocation = null;

            Var1State state;

            List<Ped> alreadyAskedPeds;

            Var1Scenario scenario = null;

            public override bool OnBeforeCalloutDisplayed(MurderInvestigation owner)
            {
                scenario = Var1Scenario.GetRandomScenario();
                owner.CalloutMessage = scenario.CalloutMessage;
                owner.CalloutPosition = scenario.CalloutPosition;
                owner.ShowCalloutAreaBlipBeforeAccepting(owner.CalloutPosition, 25f);
                return true;
            }


            public override bool OnCalloutAccepted(MurderInvestigation owner)
            {
                owner.HelpText = @"What if you search for a CCTV camera? Or a weapon?~n~If you found out how the suspect is you could ask a pedestrian if he saw him pressing ~b~" + Controls.SecondaryAction.ToUserFriendlyName() + "~s~";
                objects = new List<Rage.Object>();
                alreadyAskedPeds = new List<Ped>();
                state = Var1State.EnRoute;
                blipLocation = new Blip(scenario.CalloutPosition);
                blipLocation.IsRouteEnabled = true;
                return createScenario();
            }


            bool hasFoundKnife = false;

            public override void Process(MurderInvestigation owner)
            {
                if (state == Var1State.EnRoute)
                {
                    if (Game.LocalPlayer.Character.IsInRangeOf2D(victimPed, 12.5f))
                    {
                        state = Var1State.CrimeSceneReached;
                        return;
                    }
                }
                else if (state == Var1State.CrimeSceneReached)
                {
                    Game.DisplayNotification("~b~" + Settings.General.Name + ": ~w~Dispatch, I'm on scene, ~g~victim is dead");
                    GameFiber.Wait(1250);
                    Game.DisplayNotification("~b~Dispatch: ~w~Roger, investigate the scene");

                    state = Var1State.SearchingEvidences;
                    owner.IsPlayerOnScene = true;
                    return;
                }
                else if (state == Var1State.SearchingEvidences)
                {
//                    if (owner.InvestigationModeRaycastResult.Hit)
//                    {
//                        if (owner.InvestigationModeRaycastResult.HitEntity != null)
//                        {
//                            if (owner.InvestigationModeRaycastResult.HitEntity == cctvObject)
//                            {
//                                Game.DisplayHelp("~b~Investigation Mode:~s~ Press ~b~" + Controls.SecondaryAction.ToUserFriendlyName() + "~s~ to watch the ~g~CCTV camera footage~s~", 10);
//                                //cctvObjectVar1.Opacity = cctvObjectVar1.Opacity == 1.0f ? 0.33f : 1.0f;
//                                if (Controls.SecondaryAction.IsJustPressed())
//                                {
//                                    //cctvObjectVar1.Opacity = 1.0f;
//                                    watchCCTVCameraFootage(owner);
//                                }
//                            }
//                            else if (owner.InvestigationModeRaycastResult.HitEntity == knifeObject)
//                            {
//                                Game.DisplayHelp("~b~Investigation Mode:~s~ Press ~b~" + Controls.SecondaryAction.ToUserFriendlyName() + "~s~ to collect the ~g~knife~s~ as an evidence", 10);

//                                if (Controls.SecondaryAction.IsJustPressed())
//                                {
//                                    if (!hasFoundKnife)
//                                    {
//                                        if (knifeObject.Exists())
//                                            knifeObject.Delete();
//                                        owner.Report.AddTextToReport(
//    @"[" + DateTime.UtcNow.ToShortDateString() + "  " + DateTime.UtcNow.ToLongTimeString() + @"]
//Knife found.
//Has blood remains.
//Possible crime weapon.");
//                                    }
//                                    hasFoundKnife = true;
//                                }
//                            }
//                        }
//                    }

                    if (hasCCTVTextBeenAddedToReport)
                    {
                        if (Controls.SecondaryAction.IsJustPressed())
                        {
                            Ped closestPed = World.GetClosestEntity(Game.LocalPlayer.Character.Position, 5.0f, GetEntitiesFlags.ConsiderHumanPeds | GetEntitiesFlags.ExcludePlayerPed) as Ped;
                            if (closestPed.Exists())
                            {
                                if (alreadyAskedPeds.Contains(closestPed))
                                {
                                    Game.DisplayHelp("You already asked this pedestrian");
                                    return;
                                }

                                if (closestPed.IsDead)
                                {
                                    string[] phrases =
                                        {
                                            "What do you think you're doing? You can't ask a dead body...",
                                            "Ha, ha, ha! So funny... Asking a dead body...",
                                            "Mmh, I think that's a dead body, you shouldn't ask " + (closestPed.IsMale ? "him" : "her"),
                                        };
                                    Game.DisplayHelp(phrases.GetRandomElement(), 5750);
                                    return;
                                }

                                closestPed.BlockPermanentEvents = true;
                                bool askingPed = true;
                                GameFiber.StartNew(delegate
                                {
                                    while (askingPed && closestPed.Exists())
                                    {
                                        GameFiber.Yield();
                                        closestPed.Tasks.AchieveHeading(closestPed.GetHeadingTowards(Game.LocalPlayer.Character));
                                    }
                                });
                                GameFiber.Sleep(225);

                                bool pedDisrespectPolice = Globals.Random.Next(12) <= 1 ? true : false;
                                bool pedSawMurderer = Globals.Random.Next(3) == 1 ? true : false;
                                if (closestPed.IsMale)
                                {
                                    Game.DisplaySubtitle("~b~" + Settings.General.Name + ": ~w~Sir, have you seen this person?", 3250); //TODO: implement asking peds
                                }
                                else
                                {
                                    Game.DisplaySubtitle("~b~" + Settings.General.Name + ": ~w~Ma'am, have you seen this person?", 3250);
                                }
                                GameFiber.Sleep(3150);
                                if (closestPed == murdererPed)
                                {
                                    askingPed = false;
                                    murdererPed.Tasks.Clear();
                                    murdererPed.Flee(Game.LocalPlayer.Character, 1000.0f, 60000); // notfleeing
                                }
                                else
                                {
                                    if (pedSawMurderer)
                                    {
                                        if (pedDisrespectPolice)
                                        {
                                            Game.DisplaySubtitle("pedSawMurderer pedDisrespectPolice", 5000);
                                        }
                                        else
                                        {
                                            Game.DisplaySubtitle("pedSawMurderer", 5000);
                                        }
                                    }
                                    else
                                    {
                                        if (pedDisrespectPolice)
                                        {
                                            string notSawDisrespect = new string[] { "Ha, ha! So now you pigs need my help!? Go ask your speed tickets!" }.GetRandomElement();
                                            Game.DisplaySubtitle("~b~Pedestrian: ~w~" + notSawDisrespect, 3250);
                                        }
                                        else
                                        {
                                            Game.DisplaySubtitle("pedNOTSawMurderer", 5000);
                                        }
                                    }
                                }
                                askingPed = false;
                                alreadyAskedPeds.Add(closestPed);
                            }
                        }
                    }
                }
                else if (state == Var1State.SuspectFound)
                {

                }
            }


            public override void CleanUp(MurderInvestigation ownerCallout)
            {
#if DEBUG
                Game.DisplayNotification("Cleaning up var 1");
#endif
                if (ownerCallout.HasBeenAccepted)
                {
                    if (victimPed.Exists())
                        victimPed.Dismiss();
                    if (murdererPed.Exists())
                        murdererPed.Dismiss();
                    if (murdererPed.Exists())
                        murdererPed.Dismiss();
                    if (objects != null)
                    {
                        foreach (Rage.Object o in objects)
                            if (o.Exists())
                                o.Dismiss();
                    }
                }
                else
                {
                    if (victimPed.Exists())
                        victimPed.Delete();
                    if (murdererPed.Exists())
                        murdererPed.Delete();
                    if (objects != null)
                    {
                        foreach (Rage.Object o in objects)
                            if (o.Exists())
                                o.Delete();
                    }
                }
                if (footageCamera.Exists())
                    footageCamera.Delete();
                if (victimPedClone.Exists())
                    victimPedClone.Delete();
                if (murdererPedClone.Exists())
                    murdererPedClone.Delete();
                if (blipLocation.Exists())
                    blipLocation.Delete();
                Game.LocalPlayer.Character.IsVisible = true;
                Game.LocalPlayer.Character.IsPositionFrozen = false;
            }

            bool hasFoundMurderer = false;
            int cctvCameraNumber = Globals.Random.Next(1, 7);
            bool isCCTVCameraFootageActive = false;
            bool hasCCTVTextBeenAddedToReport = false;
            Tuple<string, bool> cctvFootageVictimScenario = new Tuple<string, bool>[] { new Tuple<string, bool>(Scenario.WORLD_HUMAN_AA_SMOKE, false), new Tuple<string, bool>(Scenario.WORLD_HUMAN_AA_COFFEE, true), new Tuple<string, bool>(Scenario.WORLD_HUMAN_AA_COFFEE, false) }.GetRandomElement();
            DateTime footageInitialDateTime = new DateTime(World.DateTime.Year, World.DateTime.Month, World.DateTime.Day, Globals.Random.Next(0, World.DateTime.Hour), Globals.Random.Next(0, World.DateTime.Minute), Globals.Random.Next(0, World.DateTime.Second));
            private void watchCCTVCameraFootage(MurderInvestigation owner)
            {
                // set up
                isCCTVCameraFootageActive = true;
                Game.FadeScreenOut(1750, true);


                List<Entity> invisibleEntities = new List<Entity>();
                invisibleEntities.AddRange(World.GetEntities(scenario.VictimSpawnPoint.Position, 150.0f, GetEntitiesFlags.ConsiderAllVehicles | GetEntitiesFlags.ConsiderAllPeds));
                Vector3[] invisibleEntitiesInitialPositions = new Vector3[invisibleEntities.Count];
                for (int i = 0; i < invisibleEntities.Count; i++)
                {
                    if (invisibleEntities[i].Exists())
                    {
                        invisibleEntitiesInitialPositions[i] = invisibleEntities[i].Position;
                        if (invisibleEntities[i] != victimPed && invisibleEntities[i] != murdererPed)
                            invisibleEntities[i].SetPositionZ(invisibleEntities[i].Position.X + 50.0f);
                        invisibleEntities[i].IsPositionFrozen = true;
                        invisibleEntities[i].IsVisible = false;
                        Ped asPed = invisibleEntities[i] as Ped;
                        if (asPed.Exists())
                        {
                            asPed.BlockPermanentEvents = true;
                        }
                    }
                }

                victimPedClone = victimPed.Clone(0.0f);

                NativeFunction.CallByName<uint>("REVIVE_INJURED_PED", victimPedClone);
                NativeFunction.CallByName<uint>("SET_ENTITY_HEALTH", victimPedClone, 200.0f);
                NativeFunction.CallByName<uint>("RESURRECT_PED", victimPedClone);
                victimPedClone.Tasks.ClearImmediately();

                victimPedClone.Position = scenario.VictimFootageSpawnPoint.Position;
                victimPedClone.Heading = scenario.VictimFootageSpawnPoint.Heading;
                victimPedClone.ClearBlood();
                victimPedClone.Tasks.ClearImmediately();
                victimPedClone.CanPlayAmbientAnimations = true;

                murdererPedClone = murdererPed.Clone(0.0f);
                murdererPedClone.Position = scenario.MurdererFootageSpawnPoint.Position;
                murdererPedClone.Heading = scenario.MurdererFootageSpawnPoint.Heading;


                Game.LocalPlayer.Character.IsPositionFrozen = true;
                cctvObject.IsVisible = false;

                footageCamera = new Camera(true);
                footageCamera.FOV -= 20;
                footageCamera.Position = cctvObject.Position;
                footageCamera.PointAtEntity(victimPed, Vector3.Zero, true);
                NativeFunction.CallByName<uint>("SET_NOISEOVERIDE", true);
                NativeFunction.CallByName<uint>("SET_NOISINESSOVERIDE", 0.1f);

                victimPedClone.Health = 170;
                murdererPedClone.IsInvincible = true;
                victimPedClone.IsInvincible = false;

                DateTime initialTime = World.DateTime;
                World.DateTime = footageInitialDateTime;

                Logger.LogTrivial(cctvFootageVictimScenario.Item1 + "   " + cctvFootageVictimScenario.Item2);
                Scenario.StartInPlace(victimPedClone, cctvFootageVictimScenario.Item1, cctvFootageVictimScenario.Item2);

                GameFiber.StartNew(delegate
                {

                    while (isCCTVCameraFootageActive)
                    {
                        GameFiber.Yield();

                        NativeFunction.CallByName<uint>("HIDE_HUD_AND_RADAR_THIS_FRAME");
                        new ResRectangle(new Point(0, 0), new Size(245, 195), Color.FromArgb(145, Color.Gray)).Draw();
                        new ResText("CCTV #" + cctvCameraNumber + "~n~" + DateTime.UtcNow.ToShortDateString() + "~n~" + DateTime.UtcNow.ToLongTimeString(), new Point(5, 5), 0.747f).Draw();
                    }
                });
                Game.FadeScreenIn(1750, true);

                // footage
                Task murdererMoveTask = murdererPedClone.Tasks.GoToOffsetFromEntity(victimPedClone, 1.1325f, 180.0f, 0.5f);
                NativeFunction.CallByName<uint>("SET_PED_STEALTH_MOVEMENT", murdererPedClone, 1, 0);
                murdererMoveTask.WaitForCompletion();
                murdererPedClone.Inventory.GiveNewWeapon(WeaponHash.Knife, 1, true);
                victimPedClone.Tasks.Clear();
                murdererPedClone.Tasks.FightAgainst(victimPedClone, -1).WaitForCompletion();

                GameFiber.Sleep(1200);

                murdererPedClone.Inventory.Weapons.Remove(WeaponHash.Knife);
                murdererPedClone.Tasks.FollowNavigationMeshToPosition(scenario.MurdererSpawnPoint.Position, scenario.MurdererSpawnPoint.Heading, 2.0f, 5.0f, -1).WaitForCompletion();
                murdererPedClone.Tasks.Wander();
                GameFiber.Sleep(700);

                // clean up
                Game.FadeScreenOut(1750, true);
                NativeFunction.CallByName<uint>("SET_NOISEOVERIDE", false);
                NativeFunction.CallByName<uint>("SET_NOISINESSOVERIDE", 0.0f);
                footageCamera.Delete();
                murdererPedClone.Delete();
                victimPedClone.Delete();
                Game.LocalPlayer.Character.IsPositionFrozen = false;
                cctvObject.IsVisible = true;
                for (int i = 0; i < invisibleEntities.Count; i++)
                {
                    if (invisibleEntities[i].Exists())
                    {
                        invisibleEntities[i].Position = invisibleEntitiesInitialPositions[i];
                        invisibleEntities[i].IsPositionFrozen = false;
                        invisibleEntities[i].IsVisible = true;
                        Ped asPed = invisibleEntities[i] as Ped;
                        if (asPed.Exists())
                        {
                            asPed.BlockPermanentEvents = false;
                        }
                    }
                }
                invisibleEntities.Clear();
                invisibleEntities = null;
                invisibleEntitiesInitialPositions = null;
                World.DateTime = initialTime;
                isCCTVCameraFootageActive = false;
                if (!hasCCTVTextBeenAddedToReport)
                {
                    owner.Report.AddTextToReport(
    @"[" + DateTime.UtcNow.ToShortDateString() + "  " + DateTime.UtcNow.ToLongTimeString() + @"]
CCTV Camera footage found.
Shows suspect attacking victim with a knife and fleeing the crime scene.
" + scenario.MurderDescription + "");
                    hasCCTVTextBeenAddedToReport = true;
                }
                Game.FadeScreenIn(2000, true);
            }


            private bool createScenario()
            {
                cctvObject = new Rage.Object(scenario.CCTVCameraModelPositionRotation.Item1, scenario.CCTVCameraModelPositionRotation.Item2);
                if (!cctvObject.Exists())
                    return false;
                cctvObject.Rotation = scenario.CCTVCameraModelPositionRotation.Item3;
                objects.Add(cctvObject);


                knifeObject = new Rage.Object(scenario.KnifeModelPositionRotation.Item1, scenario.KnifeModelPositionRotation.Item2);
                if (!knifeObject.Exists())
                    return false;
                knifeObject.Rotation = scenario.KnifeModelPositionRotation.Item3;
                objects.Add(knifeObject);


                victimPed = new Ped(scenario.VictimPedModel, scenario.VictimSpawnPoint.Position, scenario.VictimSpawnPoint.Heading);
                if (!victimPed.Exists())
                    return false;
                NativeFunction.CallByName<uint>("TASK_WRITHE", victimPed, Game.LocalPlayer.Character, -1, false);
                victimPed.ApplyDamagePack(DamagePack.BigHitByVehicle, MathHelper.GetRandomSingle(1.0f, 10.0f), MathHelper.GetRandomSingle(1.0f, 15.0f));

                murdererPed = new Ped(scenario.MurdererPedModel, scenario.MurdererSpawnPoint.Position, scenario.MurdererSpawnPoint.Heading);
                if (!murdererPed.Exists())
                    return false;
                murdererPed.Tasks.Wander();

                return true;
            }


            enum Var1State
            {
                EnRoute,
                CrimeSceneReached,
                SearchingEvidences,
                SuspectFound,
            }


            class Var1Scenario
            {
                public static Var1Scenario GetRandomScenario()
                {
                    return new Var1Scenario(0/*Globals.Random.Next(0, 3)*/);
                }

                public string CalloutMessage { get; }
                public Vector3 CalloutPosition { get; }

                public SpawnPoint VictimSpawnPoint { get; }
                public SpawnPoint VictimFootageSpawnPoint { get; }
                public Model VictimPedModel { get; }

                public SpawnPoint MurdererSpawnPoint { get; }
                public SpawnPoint MurdererFootageSpawnPoint { get; }
                public Model MurdererPedModel { get; }
                public string MurderDescription { get; }

                public Tuple<Model, Vector3, Rotator> CCTVCameraModelPositionRotation { get; }
                public Tuple<Model, Vector3, Rotator> KnifeModelPositionRotation { get; }


                private Var1Scenario(int index)
                {
                    CalloutMessage = "Murder Investigation";
                    MurderDescription = "[NOT IMPLEMENTED]";
                    switch (index)
                    {
                        default:
                        case 0:
                            CalloutPosition = new Vector3(115.6685f, 290.0636f, 109.974f);

                            VictimSpawnPoint = new SpawnPoint(new Vector3(115.6685f, 290.0636f, 109.974f), 1.790135f);
                            VictimFootageSpawnPoint = new SpawnPoint(new Vector3(116.59f, 291.90f, 109.97f), 158.63f);
                            VictimPedModel = new Model[] { "s_f_y_hooker_01", "a_f_y_business_01", "a_f_y_business_02", "a_f_y_business_03", "a_f_y_business_04" }.GetRandomElement();

                            MurdererSpawnPoint = new SpawnPoint(new Vector3(138.2685f, 299.19f, 110.87f), 255.76f);
                            MurdererFootageSpawnPoint = new SpawnPoint(new Vector3(106.91f, 297.36f, 109.99f), 249.91f);
                            MurdererPedModel = new Model[] { "s_m_y_chef_01", "s_m_y_garbage", "s_m_y_robber_01" }.GetRandomElement();
                            if (MurdererPedModel == new Model("s_m_y_chef_01"))
                            {
                                MurderDescription = "Suspect wears a chef uniform";
                            }
                            else if (MurdererPedModel == new Model("s_m_y_garbage"))
                            {
                                MurderDescription = "Suspect wears a garbage collector uniform";
                            }
                            else if (MurdererPedModel == new Model("s_m_y_robber_01"))
                            {
                                //MurderDescription = "Suspect wears s_m_y_robber_01";
                            }

                            CCTVCameraModelPositionRotation = new Tuple<Model, Vector3, Rotator>(3940745496, new Vector3(102.5212f, 281.4584f, 114.698f), new Rotator(0f, 0f, 156.0005f));
                            KnifeModelPositionRotation = new Tuple<Model, Vector3, Rotator>(3776622480, new Vector3(122.5029f, 301.3399f, 111.14f), new Rotator(90.91142f, 31.9574f, 58.76347f));
                            break;
                        case 1:
                            CalloutPosition = new Vector3(181.4932f, 304.668f, 105.3759f);

                            VictimSpawnPoint = new SpawnPoint(new Vector3(181.4932f, 304.668f, 105.3759f), 96.78967f);
                            VictimFootageSpawnPoint = new SpawnPoint(new Vector3(181.4932f, 304.668f, 105.3759f), 96.78967f);
                            VictimPedModel = new Model[] { "s_f_y_hooker_01", "a_f_y_business_01", "a_f_y_business_02", "a_f_y_business_03", "a_f_y_business_04" }.GetRandomElement();

                            MurdererSpawnPoint = new SpawnPoint(new Vector3(199.1882f, 292.4081f, 105.6103f), -115.9992f);
                            MurdererFootageSpawnPoint = new SpawnPoint(new Vector3(199.1882f, 292.4081f, 105.6103f), -115.9992f);
                            MurdererPedModel = new Model[] { "s_m_y_chef_01", "s_m_y_garbage", "s_m_y_robber_01" }.GetRandomElement();
                            if (MurdererPedModel == new Model("s_m_y_chef_01"))
                            {
                                MurderDescription = "Suspect wears a chef uniform";
                            }
                            else if (MurdererPedModel == new Model("s_m_y_garbage"))
                            {
                                MurderDescription = "Suspect wears a garbage collector uniform";
                            }
                            else if (MurdererPedModel == new Model("s_m_y_robber_01"))
                            {
                                //MurderDescription = "Suspect wears s_m_y_robber_01";
                            }

                            CCTVCameraModelPositionRotation = new Tuple<Model, Vector3, Rotator>(3940745496, new Vector3(197.3559f, 296.6242f, 110.2151f), new Rotator(0f, 0f, -87.99941f));
                            KnifeModelPositionRotation = new Tuple<Model, Vector3, Rotator>(3776622480, new Vector3(195.9491f, 291.6119f, 104.6099f), new Rotator(-87.93718f, -7.711276f, 19.59351f));
                            break;
                    }
                }
            }
        }
    }
}
