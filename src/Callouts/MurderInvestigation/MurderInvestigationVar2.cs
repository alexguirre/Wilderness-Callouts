namespace WildernessCallouts.Callouts
{
    // System
    using System;
    using System.Linq;
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
    using WildernessCallouts.Menus;
    using Common = WildernessCallouts.Common;

    internal partial class MurderInvestigation : CalloutBase
    {
        /// <summary>
        /// Victim is robbed, flees and suspect shoots
        /// </summary>
        internal class Variation2 : VariationBaseClass
        {
            public Ped VictimPed;
            public Ped MurdererPed;

            public readonly List<Rage.Object> Objects = new List<Rage.Object>();

            public Rage.Object CCTVObject;
            public Rage.Object PistolObject;

            public Camera FootageCamera;
            public Ped VictimPedClone;
            public Ped MurdererPedClone;

            public Blip BlipLocation;

            public Var1State State;

            public readonly List<Ped> AlreadyAskedPeds = new List<Ped>();

            public readonly Var2Scenario Scenario = Var2Scenario.GetRandomScenario();

            public InvestigationHandler InvestigationHandlerInstance;

            public readonly int TotalEvidencesCount = 3;
            public int CollectedEvidences = 0;


            public override bool OnBeforeCalloutDisplayed(MurderInvestigation owner)
            {
                owner.CalloutMessage = Scenario.CalloutMessage;
                owner.CalloutPosition = Scenario.CalloutPosition;
                owner.ShowCalloutAreaBlipBeforeAccepting(owner.CalloutPosition, 25f);
                return true;
            }


            public override bool OnCalloutAccepted(MurderInvestigation owner)
            {
                InvestigationHandlerInstance = new InvestigationHandler();

                owner.HelpText = @"What if you search for evidence like a CCTV camera or a pistol? ~n~If you found out how the suspect is you could ask a pedestrian if he saw him pressing ~b~" + Controls.SecondaryAction.ToUserFriendlyName() + "~s~";
                State = Var1State.EnRoute;
                BlipLocation = new Blip(Scenario.CalloutPosition);
                BlipLocation.IsRouteEnabled = true;

                return createScenario();
            }




            public override void Process(MurderInvestigation owner)
            {
                if (State == Var1State.EnRoute)
                {
                    if (Game.LocalPlayer.Character.IsInRangeOf2D(VictimPed, 12.5f))
                    {
                        State = Var1State.CrimeSceneReached;
                        return;
                    }
                }
                else if (State == Var1State.CrimeSceneReached)
                {
                    Game.DisplayNotification("~b~" + Settings.General.Name + ": ~w~Dispatch, I'm on scene, ~g~victim is dead");
                    GameFiber.Wait(1250);
                    Game.DisplayNotification("~b~Dispatch: ~w~Roger, investigate the scene");

                    State = Var1State.SearchingEvidences;
                    owner.IsPlayerOnScene = true;
                    InvestigationHandlerInstance.AddEvidence(CCTVObject, "watch the ~g~CCTV camera footage~s~", delegate
                    {
                        watchCCTVCameraFootage(owner);
                        InvestigationHandlerInstance.RemoveEvidence("CCTV");
                    }, "CCTV");
                    InvestigationHandlerInstance.AddEvidence(VictimPed, "search the ~g~dead body~s~", delegate
                    {
                        searchVictimDeadBody(owner);
                        InvestigationHandlerInstance.RemoveEvidence("VICTIMDEADBODY");
                    }, "VICTIMDEADBODY");
                    InvestigationHandlerInstance.AddEvidence(PistolObject, "collect the ~g~pistol~s~", delegate
                    {
                        collectPistol(owner);
                        InvestigationHandlerInstance.RemoveEvidence("PISTOL");
                    }, "PISTOL");
                    Game.DisplayHelp("Press ~b~" + Controls.PrimaryAction.ToUserFriendlyName() + "~s~ to enter ~b~Investigation Mode~s~", 10000);
                    return;
                }
                else if (State == Var1State.SearchingEvidences)
                {
                    if (Controls.PrimaryAction.IsJustPressed())
                    {
                        InvestigationHandlerInstance.Active = !InvestigationHandlerInstance.Active;
                    }

                    if (HasWatchedCCTVFootage && !InvestigationHandlerInstance.Active)
                    {
                        if (Controls.SecondaryAction.IsJustPressed())
                        {
                            Ped closestPed = World.GetClosestEntity(Game.LocalPlayer.Character.Position, 5.0f, GetEntitiesFlags.ConsiderHumanPeds | GetEntitiesFlags.ExcludePlayerPed) as Ped;
                            if (closestPed.Exists())
                            {
                                if (AlreadyAskedPeds.Contains(closestPed))
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
                                GameFiber.Sleep(1150);
                                if (closestPed == MurdererPed)
                                {
                                    askingPed = false;
                                    closestPed.BlockPermanentEvents = false;
                                    MurdererPed.Tasks.Clear();
                                    MurdererPed.ReactAndFlee(Game.LocalPlayer.Character); // notfleeing
                                }
                                else
                                {
                                    GameFiber.Sleep(1925);
                                    if (pedSawMurderer)
                                    {
                                        if (pedDisrespectPolice)
                                        {
                                            Game.DisplaySubtitle("pedSawMurderer pedDisrespectPolice", 4500);
                                        }
                                        else
                                        {
                                            Game.DisplaySubtitle("pedSawMurderer", 4500);
                                        }
                                        GameFiber.Sleep(4500);
                                        askingPed = false;
                                        closestPed.Tasks.Clear();
                                        closestPed.Tasks.AchieveHeading(closestPed.GetHeadingTowards(MurdererPed)).WaitForCompletion(2250);
                                        closestPed.Tasks.PlayAnimation("gestures@m@standing@casual", "gesture_point", -1, 4.0f, 0.375f, 0.0f, AnimationFlags.UpperBodyOnly);
                                    }
                                    else
                                    {
                                        if (pedDisrespectPolice)
                                        {
                                            string notSawDisrespect = new string[]
                                            {
                                                "Ha, ha! So now you pigs need my help!? Go ask your speed tickets!",
                                                "Did he steal your donuts?",
                                            }.GetRandomElement();
                                            Game.DisplaySubtitle("~b~Pedestrian: ~w~" + notSawDisrespect, 4500);
                                        }
                                        else
                                        {
                                            Game.DisplaySubtitle("pedNOTSawMurderer", 4500);
                                        }
                                    }
                                }
                                closestPed.BlockPermanentEvents = false;
                                askingPed = false;
                                AlreadyAskedPeds.Add(closestPed);
                            }
                        }
                    }
                }
                else if (State == Var1State.SuspectFound)
                {

                }

                murdererAI();

                InvestigationHandlerInstance.Process();
            }



            public override void CleanUp(MurderInvestigation ownerCallout)
            {
#if DEBUG
                Game.DisplayNotification("Cleaning up var 2");
#endif

                if (ownerCallout.HasBeenAccepted)
                {
                    DisplayInvestigationReport();

                    if (VictimPed.Exists())
                        VictimPed.Dismiss();
                    if (MurdererPed.Exists())
                        MurdererPed.Dismiss();
                    if (MurdererPed.Exists())
                        MurdererPed.Dismiss();
                    if (Objects != null)
                    {
                        foreach (Rage.Object o in Objects)
                            if (o.Exists())
                                o.Dismiss();
                    }
                }
                else
                {
                    if (VictimPed.Exists())
                        VictimPed.Delete();
                    if (MurdererPed.Exists())
                        MurdererPed.Delete();
                    if (Objects != null)
                    {
                        foreach (Rage.Object o in Objects)
                            if (o.Exists())
                                o.Delete();
                    }
                }
                if (FootageCamera.Exists())
                    FootageCamera.Delete();
                if (VictimPedClone.Exists())
                    VictimPedClone.Delete();
                if (MurdererPedClone.Exists())
                    MurdererPedClone.Delete();
                if (BlipLocation.Exists())
                    BlipLocation.Delete();
                if (InvestigationHandlerInstance != null)
                    InvestigationHandlerInstance.CleanUp();
                Game.LocalPlayer.Character.IsVisible = true;
                Game.LocalPlayer.Character.IsPositionFrozen = false;
            }


            public void DisplayInvestigationReport()
            {
                string specific = "MurderInvestigation: " + this.GetType().Name;
                Logger.LogTrivial(specific, "Collected Evidences: " + CollectedEvidences + "/" + TotalEvidencesCount);
                Logger.LogTrivial(specific, "HasMurdererNoticedPlayerPresence: " + HasMurdererNoticedPlayerPresence);
                Logger.LogTrivial(specific, "HasFoundMurder: " + HasFoundMurder);
                Logger.LogTrivial(specific, "HasWatchedCCTVFootage: " + HasWatchedCCTVFootage);
                Logger.LogTrivial(specific, "HasSearchedVictimBody: " + HasSearchedVictimBody);
                Logger.LogTrivial(specific, "HasCollectedPistol: " + HasCollectedPistol);

            //string text =
            //        "<font size=\"11\">" +
            //        "~n~Collected Evidences: " + CollectedEvidences + "/" + TotalEvidencesCount +
            //        "</font>";

            //    string text2 =
            //        "<font size=\"11\">" + 
            //        "~n~Have you watched the CCTV footage? " + (HasWatchedCCTVFootage ? "YES" : "NO") +
            //        "~n~Have you searched the dead body? " + (HasSearchedVictimBody ? "YES" : "NO") +
            //        "</font>";

            //    string text3 =
            //        "<font size=\"11\">" +
            //        "~n~Have you found the crime weapon? " + (HasCollectedPistol ? "YES" : "NO") +
            //        "</font>";

            //    Game.DisplayNotification("helicopterhud", "targetlost", "~b~<b>MURDER INVESTIGATION</b>", "~b~<b>REPORT</b>", text);
            //    Game.DisplayNotification(text2);
            //    Game.DisplayNotification(text3);

                //Game.DisplayNotification("~b~<font size=\"12\"><b>MURDER INVESTIGATION REPORT</b></font>~s~");
                //Game.DisplayNotification("Collected Evidences: " + CollectedEvidences + "/" + TotalEvidencesCount);
                //Game.DisplayNotification("Have you watched the CCTV footage? " + (hasWatchCCTVFirstTime ? "YES" : "NO"));
                //Game.DisplayNotification("Have you searched the dead body? " + (hasSearchedVictimDeadBody ? "YES" : "NO"));
                //Game.DisplayNotification("Have you found the crime weapon? " + (hasCollectedPistol ? "YES" : "NO"));
            }


            HitResult[] murdererAIRayResults = new HitResult[5];
            MurdererReaction murdererReaction;
            bool startedMurdererNoticedPlayerAI = false;
            private void murdererAI()
            {
                Ped playerPed = Game.LocalPlayer.Character;


                Vector3 rayStart = MurdererPed.GetBonePosition(PedBoneId.Head);
                Vector3 rayEnd = rayStart + MurdererPed.Direction * 40.0f;
                const float rayRadius = 4.0f;
                const TraceFlags rayFlags = TraceFlags.IntersectEverything | TraceFlags.IntersectVehicles;

                murdererAIRayResults[0] = World.TraceCapsule(rayStart,   rayEnd,                                       rayRadius,      rayFlags,       MurdererPed);
                murdererAIRayResults[1] = World.TraceCapsule(rayStart,   rayEnd + Vector3.WorldUp,                     rayRadius,      rayFlags,       MurdererPed);
                murdererAIRayResults[2] = World.TraceCapsule(rayStart,   rayEnd - Vector3.WorldUp,                     rayRadius,      rayFlags,       MurdererPed);
                murdererAIRayResults[3] = World.TraceCapsule(rayStart,   rayEnd + MurdererPed.RightVector * 1.25f,     rayRadius,      rayFlags,       MurdererPed);
                murdererAIRayResults[4] = World.TraceCapsule(rayStart,   rayEnd - MurdererPed.RightVector * 1.25f,     rayRadius,      rayFlags,       MurdererPed);

#if DEBUG
                Color rayDebugColor = Color.OrangeRed;
                Common.DrawLine(rayStart, rayEnd,                                    rayDebugColor);
                Common.DrawLine(rayStart, rayEnd + Vector3.WorldUp,                  rayDebugColor);
                Common.DrawLine(rayStart, rayEnd - Vector3.WorldUp,                  rayDebugColor);
                Common.DrawLine(rayStart, rayEnd + MurdererPed.RightVector * 1.25f,  rayDebugColor);
                Common.DrawLine(rayStart, rayEnd - MurdererPed.RightVector * 1.25f,  rayDebugColor);
#endif


                if (HasMurdererNoticedPlayerPresence)
                {
#if DEBUG
                    Game.DisplayNotification("murderer noticed player presence");
#endif
                    if (!startedMurdererNoticedPlayerAI)
                    {
                        switch (murdererReaction)
                        {
                            case MurdererReaction.LeaveAreaRunning:
                                GameFiber.StartNew(delegate
                                {
                                });
                                break;
                            case MurdererReaction.LeaveAreaWalking:
                                GameFiber.StartNew(delegate
                                {
                                });
                                break;
                            case MurdererReaction.AttackPlayer:
                                GameFiber.StartNew(delegate
                                {
                                });
                                break;
                        }
                    }
                }
                else
                {
                    if (MurdererPed.DistanceTo(playerPed) < 52.5f)
                    {
                        if (playerPed.IsInAnyVehicle(false) && playerPed.CurrentVehicle.IsSirenOn && !playerPed.CurrentVehicle.IsSirenSilent)
                        {
                            HasMurdererNoticedPlayerPresence = true;
                        }
                        
                        foreach (HitResult raycast in murdererAIRayResults)
                        {
                            if (raycast.Hit)
                            {
                                Entity hitEnt = raycast.HitEntity;
                                if (hitEnt.Exists())
                                {
                                    if (hitEnt == playerPed || hitEnt == playerPed.CurrentVehicle)
                                    {
                                        HasMurdererNoticedPlayerPresence = true;
                                        murdererReaction = default(MurdererReaction).GetRandomElement<MurdererReaction>();
                                    }
                                }
                            }
                        }
                    }
                }
            }



            int cctvCameraNumber = Globals.Random.Next(1, 7);
            bool isCCTVCameraFootageActive = false;
            DateTime footageInitialDateTime = new DateTime(World.DateTime.Year, World.DateTime.Month, World.DateTime.Day, Globals.Random.Next(0, World.DateTime.Hour), Globals.Random.Next(0, World.DateTime.Minute), Globals.Random.Next(0, World.DateTime.Second));
            string animIdle = new string[] { "idle_a", "idle_b", "idle_c" }.GetRandomElement();
            private void watchCCTVCameraFootage(MurderInvestigation owner)
            {
                // set up
                isCCTVCameraFootageActive = true;
                Game.FadeScreenOut(1750, true);


                List<Entity> invisibleEntities = new List<Entity>();
                invisibleEntities.AddRange(World.GetEntities(Scenario.VictimSpawnPoint.Position, 150.0f, GetEntitiesFlags.ConsiderAllVehicles | GetEntitiesFlags.ConsiderAllPeds));
                Vector3[] invisibleEntitiesInitialPositions = new Vector3[invisibleEntities.Count];
                for (int i = 0; i < invisibleEntities.Count; i++)
                {
                    if (invisibleEntities[i].Exists())
                    {
                        invisibleEntitiesInitialPositions[i] = invisibleEntities[i].Position;
                        if (invisibleEntities[i] != VictimPed && invisibleEntities[i] != MurdererPed)
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

                VictimPedClone = VictimPed.Clone(0.0f);

                NativeFunction.CallByName<uint>("REVIVE_INJURED_PED", VictimPedClone);
                NativeFunction.CallByName<uint>("SET_ENTITY_HEALTH", VictimPedClone, 200.0f);
                NativeFunction.CallByName<uint>("RESURRECT_PED", VictimPedClone);
                VictimPedClone.Tasks.ClearImmediately();

                VictimPedClone.Position = Scenario.VictimFootageSpawnPoint.Position;
                VictimPedClone.Heading = Scenario.VictimFootageSpawnPoint.Heading;
                VictimPedClone.ClearBlood();
                VictimPedClone.Tasks.ClearImmediately();
                VictimPedClone.BlockPermanentEvents = true;
                VictimPedClone.CanPlayAmbientAnimations = true;

                MurdererPedClone = MurdererPed.Clone(0.0f);
                MurdererPedClone.Position = Scenario.MurdererFootageSpawnPoint.Position;
                MurdererPedClone.Heading = Scenario.MurdererFootageSpawnPoint.Heading;
                MurdererPedClone.BlockPermanentEvents = true;


                Game.LocalPlayer.Character.IsPositionFrozen = true;
                CCTVObject.IsVisible = false;

                FootageCamera = new Camera(true);
                FootageCamera.FOV -= 20;
                FootageCamera.Position = CCTVObject.Position;
                //footageCamera.PointAtEntity(victimPed, Vector3.Zero, true);
                NativeFunction.CallByName<uint>("POINT_CAM_AT_COORD", FootageCamera, Scenario.VictimFootageSpawnPoint.Position.X, Scenario.VictimFootageSpawnPoint.Position.Y, Scenario.VictimFootageSpawnPoint.Position.Z);
                NativeFunction.CallByName<uint>("SET_NOISEOVERIDE", true);
                NativeFunction.CallByName<uint>("SET_NOISINESSOVERIDE", 0.1f);

                VictimPedClone.Health = 170;
                MurdererPedClone.IsInvincible = true;
                VictimPedClone.IsInvincible = false;

                DateTime initialTime = World.DateTime;
                World.DateTime = footageInitialDateTime;

                VictimPedClone.Tasks.PlayAnimation("amb@prop_human_atm@female@idle_a", "idle_a", 1.0f, AnimationFlags.Loop);

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
                Task murdererMoveTask = MurdererPedClone.Tasks.GoToOffsetFromEntity(VictimPedClone, Scenario.MurderFootagePositionOffsetFromVictim, Scenario.MurderFootageAngleOffsetFromVictim, 0.5f);
                NativeFunction.CallByName<uint>("SET_PED_STEALTH_MOVEMENT", MurdererPedClone, 1, 0);
                murdererMoveTask.WaitForCompletion();
                MurdererPedClone.Inventory.GiveNewWeapon((WeaponHash)EWeaponHash.Heavy_Pistol, 999, true);
                VictimPedClone.Tasks.Clear();
                MurdererPedClone.Tasks.AimWeaponAt(VictimPedClone, -1);
                VictimPedClone.Tasks.PutHandsUp(-1, MurdererPedClone);

                MurdererPedClone.PlayAmbientSpeech(null, new string[] { Speech.GENERIC_FUCK_YOU, Speech.GENERIC_INSULT_HIGH }.GetRandomElement(), 0, SpeechModifier.Shouted);
                GameFiber.Sleep(800);
                VictimPedClone.PlayAmbientSpeech(null, new string[] { Speech.GENERIC_SHOCKED_HIGH, Speech.GENERIC_CURSE_HIGH, Speech.SCREAM }.GetRandomElement(), 0, SpeechModifier.Shouted);

                GameFiber.Sleep(6500);

                VictimPedClone.ReactAndFlee(MurdererPedClone);

                GameFiber.Sleep(2500);

                MurdererPedClone.Tasks.FireWeaponAt(VictimPedClone, 5250, FiringPattern.FullAutomatic);

                GameFiber.Sleep(5250);

                MurdererPedClone.PlayAmbientSpeech(null, Speech.GENERIC_CURSE_HIGH, 0, SpeechModifier.Shouted);
                MurdererPedClone.Tasks.FollowNavigationMeshToPosition(Scenario.MurdererSpawnPoint.Position, Scenario.MurdererSpawnPoint.Heading, 2.0f, 5.0f, -1).WaitForCompletion();
                MurdererPedClone.Tasks.Wander();
                MurdererPedClone.Inventory.Weapons.Remove((WeaponHash)EWeaponHash.Heavy_Pistol);
                GameFiber.Sleep(500);

                // clean up
                Game.FadeScreenOut(1750, true);
                NativeFunction.CallByName<uint>("SET_NOISEOVERIDE", false);
                NativeFunction.CallByName<uint>("SET_NOISINESSOVERIDE", 0.0f);
                FootageCamera.Delete();
                if (InvestigationHandlerInstance.IsCamLookingAtObject)
                    InvestigationHandlerInstance.Cam.Active = true;
                MurdererPedClone.Delete();
                VictimPedClone.Delete();
                Game.LocalPlayer.Character.IsPositionFrozen = false;
                CCTVObject.IsVisible = true;
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
                if (!HasWatchedCCTVFootage)
                {
                    owner.Report.AddTextToReport(
    @"[" + DateTime.UtcNow.ToShortDateString() + "  " + DateTime.UtcNow.ToLongTimeString() + @"]
CCTV Camera footage found.
Shows suspect stealing at gunpoint the victim, victim fleeing and suspect shooting at victim.
" + Scenario.MurderDescriptor.Invoke(MurdererPed) + "");
                    owner.Menu.AddEvidence("CCTV Footage", "Rewatch the camera footage", delegate { watchCCTVCameraFootage(owner); });
                    CollectedEvidences++;
                    HasWatchedCCTVFootage = true;
                }
                Game.FadeScreenIn(2000, true);
                Game.DisplayNotification("~b~<font size=\"13\"><b>EVIDENCE: CCTV</b></font>~s~");
                Game.DisplayNotification("Shows suspect stealing at gunpoint the victim, victim fleeing and suspect shooting at victim.");
                Game.DisplayNotification(Scenario.MurderDescriptor.Invoke(MurdererPed));
            }


            private void searchVictimDeadBody(MurderInvestigation owner)
            {
                Vector3 posToWalk = VictimPed.Position.AroundPosition(0.75f);
                Game.LocalPlayer.Character.Tasks.FollowNavigationMeshToPosition(posToWalk, posToWalk.GetHeadingTowards(VictimPed), 2.0f, 0.5f).WaitForCompletion();

                GameFiber.Wait(75);
                Game.LocalPlayer.Character.Tasks.PlayAnimation(new AnimationDictionary("amb@medic@standing@tendtodead@idle_a"), "idle_a", 1.0f, AnimationFlags.Loop);

                GameFiber.Sleep(2500);

                Game.LocalPlayer.Character.Tasks.Clear();

                string name = LSPD_First_Response.Mod.API.Functions.GetPersonaForPed(VictimPed).FullName;

                Game.DisplayNotification("~b~<font size=\"13\"><b>EVIDENCE: Dead Body</b></font>~s~");
                Game.DisplayNotification("Has multiple bullet holes and " + name + " ID card.");

                if (!HasSearchedVictimBody)
                {
                    owner.Report.AddTextToReport(
    @"[" + DateTime.UtcNow.ToShortDateString() + "  " + DateTime.UtcNow.ToLongTimeString() + @"]
Searched victim's dead body.
Multiple bullet holes.
Found " + name + " ID card");
                    owner.Menu.AddEvidence("Dead body", "", delegate
                    {
                        Game.DisplayNotification("~b~<font size=\"13\"><b>EVIDENCE: Dead Body</b></font>~s~");
                        Game.DisplayNotification("Has multiple bullet holes and " + name + " ID card.");
                    });
                    CollectedEvidences++;
                    HasSearchedVictimBody = true;
                }
            }


            private void collectPistol(MurderInvestigation owner)
            {
                GameFiber.Sleep(1000);

                if (PistolObject.Exists())
                    PistolObject.Delete();

                Game.DisplayNotification("~b~<font size=\"13\"><b>EVIDENCE: Pistol</b></font>~s~");
                Game.DisplayNotification("It has been fired multiple times.");

                if (!HasCollectedPistol)
                {
                    owner.Report.AddTextToReport(
    @"[" + DateTime.UtcNow.ToShortDateString() + "  " + DateTime.UtcNow.ToLongTimeString() + @"]
Collected a pistol.
It has been fired multiple times.");
                    owner.Menu.AddEvidence("Pistol", "", delegate
                    {
                        Game.DisplayNotification("~b~<font size=\"13\"><b>EVIDENCE: Pistol</b></font>~s~");
                        Game.DisplayNotification("It has been fired multiple times.");
                    });
                    CollectedEvidences++;
                    HasCollectedPistol = true;
                }
            }




            private bool createScenario()
            {
                CCTVObject = new Rage.Object(Scenario.CCTVCameraModelPositionRotation.Item1, Scenario.CCTVCameraModelPositionRotation.Item2);
                if (!CCTVObject.Exists())
                    return false;
                CCTVObject.Rotation = Scenario.CCTVCameraModelPositionRotation.Item3;
                Objects.Add(CCTVObject);

                PistolObject = new Rage.Object(Scenario.PistolModelPositionRotation.Item1, Scenario.PistolModelPositionRotation.Item2);
                if (!PistolObject.Exists())
                    return false;
                PistolObject.Rotation = Scenario.PistolModelPositionRotation.Item3;
                NativeFunction.CallByName<uint>("SET_ACTIVATE_OBJECT_PHYSICS_AS_SOON_AS_IT_IS_UNFROZEN", PistolObject, true);
                Objects.Add(PistolObject);

                VictimPed = new Ped(Scenario.VictimPedModel, Scenario.VictimSpawnPoint.Position, Scenario.VictimSpawnPoint.Heading);
                if (!VictimPed.Exists())
                    return false;
                NativeFunction.CallByName<uint>("TASK_WRITHE", VictimPed, Game.LocalPlayer.Character, -1, false);
                VictimPed.ApplyDamagePack(DamagePack.BigHitByVehicle, MathHelper.GetRandomSingle(1.0f, 100.0f), MathHelper.GetRandomSingle(1.0f, 150.0f));
                VictimPed.Health = 0;

                MurdererPed = new Ped(Scenario.MurdererPedModel, Scenario.MurdererSpawnPoint.Position, Scenario.MurdererSpawnPoint.Heading);
                if (!MurdererPed.Exists())
                    return false;
                MurdererPed.Inventory.GiveNewWeapon(MathHelper.GetChance(20) ? (WeaponHash)EWeaponHash.Machete : (WeaponHash)EWeaponHash.Switchblade, 999, false);
                MurdererPed.Tasks.Wander();

                return true;
            }


            public enum Var1State
            {
                EnRoute,
                CrimeSceneReached,
                SearchingEvidences,
                SuspectFound,
            }


            public bool HasMurdererNoticedPlayerPresence;
            public bool HasFoundMurder;
            public bool HasWatchedCCTVFootage;
            public bool HasSearchedVictimBody;
            public bool HasCollectedPistol;



            public class InvestigationHandler
            {
                private const string SCREEN_EFFECT_IN_NAME = "FocusIn";
                private const string SCREEN_EFFECT_OUT_NAME = "FocusOut";


                public Entity CurrentEntityLookingAt { get; private set; }

                public Camera Cam { get; }
                public Camera InterpolationTempCam { get; }

                public bool IsCamLookingAtObject { get; private set; }

                public bool IsEvidenceActionRunning { get; private set; }

                private bool _active;
                public bool Active
                {
                    get
                    {
                        return _active;
                    }
                    set
                    {
                        _active = value;

                        if(value == true)
                        {
                            //void _START_SCREEN_EFFECT(char* effectName, int duration, BOOL looped)
                            //// 0x2206BF9A37B7F724 0x1D980479
                            NativeFunction.CallByHash<uint>(0x2206bf9a37b7f724, SCREEN_EFFECT_IN_NAME, 0, true);
                        }
                        else if (value == false)
                        {
                            //void _STOP_SCREEN_EFFECT(char * effectName) 
                            //// 0x068E835A1D0DC0E3 0x06BB5CDA
                            if (NativeFunction.CallByHash<bool>(0x36ad3e690da5aceb, SCREEN_EFFECT_IN_NAME))
                                NativeFunction.CallByHash<uint>(0x068e835a1d0dc0e3, SCREEN_EFFECT_IN_NAME);
                            NativeFunction.CallByHash<uint>(0x2206bf9a37b7f724, SCREEN_EFFECT_OUT_NAME, 1000, false);

                            if (IsCamLookingAtObject)
                                StopLookAt();
                        }
                    }
                }


                private bool _isInterpolating;

                private List<Evidence> Evidences { get; }

                private readonly Vector3 camOffsetFromPlayer = /*new Vector3(-0.31125f, 0.0f, 0.5f)*/Game.LocalPlayer.Character.GetPositionOffset(Game.LocalPlayer.Character.GetBonePosition(PedBoneId.Head)) + new Vector3(0.485f, 0.0f, -0.01225f);
 

                public InvestigationHandler()
                {
                    Cam = new Camera(false);
                    InterpolationTempCam = new Camera(false);
                    CurrentEntityLookingAt = null;
                    Evidences = new List<Evidence>();
                }

                public void Process()
                {
                    if (!Active)
                        return;

                    if (!IsEvidenceActionRunning)
                    {
                        Evidence closestEvidence = Evidences.OrderBy(e => e.Entity.DistanceTo2D(Game.LocalPlayer.Character)).FirstOrDefault();

                        if (closestEvidence.Entity.Exists())
                        {
                            float distance = closestEvidence.Entity.DistanceTo2D(Game.LocalPlayer.Character);
#if DEBUG
                            new RAGENativeUI.Elements.ResText("Closest Evidence Distance: " + distance, new Point(475, 35), 0.65f, Color.White, RAGENativeUI.Common.EFont.ChaletLondon, ResText.Alignment.Centered) { DropShadow = true, Outline = true }.Draw();
#endif
                            if (distance <= 3.725f /*&& Game.LocalPlayer.Character.IsHeadingTowardsWithTolerance(closestEvidence.Item1, 20.0f)*/)
                            {
                                if (!IsCamLookingAtObject || CurrentEntityLookingAt != closestEvidence.Entity)
                                {
                                    LookAt(closestEvidence.Entity);
                                }

                                Game.DisplayHelp("~b~Investigation Mode:~s~ Press ~b~" + Controls.SecondaryAction.ToUserFriendlyName() + "~s~ to " + closestEvidence.HelpText, 10);
                                if (IsCamLookingAtObject)
                                {
                                    Cam.AttachToEntity(Game.LocalPlayer.Character, Game.LocalPlayer.Character.GetPositionOffset(Game.LocalPlayer.Character.GetBonePosition(PedBoneId.Head)) + new Vector3(0.485f, 0.0f, -0.01225f), true);
                                    if (Controls.SecondaryAction.IsJustPressed())
                                    {
                                        GameFiber.StartNew(delegate
                                        {
                                            IsEvidenceActionRunning = true;
                                            closestEvidence.Action.Invoke();
                                            IsEvidenceActionRunning = false;
                                        });
                                    }
                                }
                            }
                            else
                            {
                                if (IsCamLookingAtObject || CurrentEntityLookingAt == closestEvidence.Entity)
                                {
                                    StopLookAt();
                                }
                            }
                        }



                        if (Game.IsScreenFadedIn)
                        {
                            if (!NativeFunction.CallByHash<bool>(0x36ad3e690da5aceb, SCREEN_EFFECT_IN_NAME))
                                NativeFunction.CallByHash<uint>(0x2206bf9a37b7f724, SCREEN_EFFECT_IN_NAME, 0, true);
                        }
                    }
                    else
                    {
                        if (Game.IsScreenFadingOut)
                        {

                            if (NativeFunction.CallByHash<bool>(0x36ad3e690da5aceb, SCREEN_EFFECT_IN_NAME))
                            {
                                NativeFunction.CallByHash<uint>(0x068e835a1d0dc0e3, SCREEN_EFFECT_IN_NAME);
                                NativeFunction.CallByHash<uint>(0x2206bf9a37b7f724, SCREEN_EFFECT_OUT_NAME, 1000, false);
                            }
                        }
                    }
                }

                public void LookAt(Entity entToLookAt)
                {
                    CurrentEntityLookingAt = entToLookAt;
                    Cam.AttachToEntity(Game.LocalPlayer.Character, camOffsetFromPlayer, true);
                    Cam.PointAtEntity(CurrentEntityLookingAt, Vector3.Zero, false);
                    Cam.FOV = NativeFunction.CallByName<float>("GET_GAMEPLAY_CAM_FOV");
                    InterpolationTempCam.FOV = NativeFunction.CallByName<float>("GET_GAMEPLAY_CAM_FOV");
                    InterpolationTempCam.Position = WildernessCallouts.Common.GetGameplayCameraPosition();
                    Vector3 r = WildernessCallouts.Common.GetGameplayCameraRotation();
                    InterpolationTempCam.Rotation = new Rotator(r.X, r.Y, r.Z);
                    InterpolationTempCam.Active = true;
                    _isInterpolating = true;
                    Game.LocalPlayer.Character.IsPositionFrozen = true;
                    InterpolationTempCam.Interpolate(Cam, 1500, true, true, true);
                    Game.LocalPlayer.Character.IsPositionFrozen = false;
                    //int interpolationTime = 1500;
                    //while (true)
                    //{
                    //    GameFiber.Yield();
                    //    if(interpolationTime <= 25)
                    //    {
                    //        InterpolationTempCam.Interpolate(Cam, interpolationTime, true, true, true);
                    //        break;
                    //    }
                    //    InterpolationTempCam.Interpolate(Cam, interpolationTime, true, true, false);
                    //    GameFiber.Sleep(25);
                    //    interpolationTime -= 25;
                    //    Cam.AttachToEntity(Game.LocalPlayer.Character, camOffsetFromPlayer, true);

                    //}
                    _isInterpolating = false;
                    Cam.Active = true;
                    IsCamLookingAtObject = true;
                }

                public void StopLookAt()
                {
                    Cam.AttachToEntity(Game.LocalPlayer.Character, camOffsetFromPlayer, true);
                    Cam.PointAtEntity(CurrentEntityLookingAt, Vector3.Zero, false);
                    Cam.FOV = NativeFunction.CallByName<float>("GET_GAMEPLAY_CAM_FOV");
                    InterpolationTempCam.FOV = NativeFunction.CallByName<float>("GET_GAMEPLAY_CAM_FOV");
                    InterpolationTempCam.Position = WildernessCallouts.Common.GetGameplayCameraPosition();
                    Vector3 r = WildernessCallouts.Common.GetGameplayCameraRotation();
                    InterpolationTempCam.Rotation = new Rotator(r.X, r.Y, r.Z);
                    Cam.Active = true;
                    _isInterpolating = true;
                    Cam.Interpolate(InterpolationTempCam, 1500, true, true, true);
                    _isInterpolating = false;
                    InterpolationTempCam.Active = false;
                    Cam.Active = false;
                    IsCamLookingAtObject = false;
                    CurrentEntityLookingAt = null;
                }

                public void AddEvidence(Entity evidenceEntity, string helpText, Action action, string keyName)
                {
                    Logger.LogTrivial("MurderInvestigation", " InvestigationHandler: Added evidence " + keyName);
                    Evidences.Add(new Evidence(evidenceEntity, helpText, action, keyName));
                }

                public void RemoveEvidence(string keyName)
                {
                    Logger.LogTrivial("MurderInvestigation", " InvestigationHandler: Remove evidence " + keyName);
                    Evidence toRemove = Evidences.FirstOrDefault(e => e.KeyName == keyName);
                    if (Evidences.Contains(toRemove))
                        Evidences.Remove(toRemove);
                    if (toRemove.Entity == CurrentEntityLookingAt)
                        StopLookAt();
                }

                public void CleanUp()
                {
                    //int _GET_SCREEN_EFFECT_IS_ACTIVE(char * effectName)
                    //// 0x36AD3E690DA5ACEB 0x089D5921
                    //void _STOP_SCREEN_EFFECT(char * effectName) 
                    //// 0x068E835A1D0DC0E3 0x06BB5CDA
                    if (NativeFunction.CallByHash<bool>(0x36ad3e690da5aceb, SCREEN_EFFECT_IN_NAME))
                        NativeFunction.CallByHash<uint>(0x068e835a1d0dc0e3, SCREEN_EFFECT_IN_NAME);
                    if (NativeFunction.CallByHash<bool>(0x36ad3e690da5aceb, SCREEN_EFFECT_OUT_NAME))
                        NativeFunction.CallByHash<uint>(0x068e835a1d0dc0e3, SCREEN_EFFECT_OUT_NAME);

                    if (Cam.Exists())
                    {
                        Cam.Active = false;
                        Cam.Delete();
                    }
                    if (InterpolationTempCam.Exists())
                    {
                        InterpolationTempCam.Active = false;
                        InterpolationTempCam.Delete();
                    }
                    CurrentEntityLookingAt = null;
                    Evidences.Clear();
                }


                struct Evidence
                {
                    public Entity Entity { get; }
                    public string HelpText { get; }
                    public Action Action { get; }
                    public string KeyName { get; }

                    public Evidence(Entity evidenceEntity, string helpText, Action action, string keyName)
                    {
                        Entity = evidenceEntity;
                        HelpText = helpText;
                        Action = action;
                        KeyName = keyName;
                    }
                }
            }



            public class Var2Scenario
            {
                public static Var2Scenario GetRandomScenario()
                {
                    return new Var2Scenario(Globals.Random.Next(0, 3));
                }

                public string CalloutMessage { get; }
                public Vector3 CalloutPosition { get; }

                public SpawnPoint VictimSpawnPoint { get; }
                public SpawnPoint VictimFootageSpawnPoint { get; }
                public Model VictimPedModel { get; }

                public SpawnPoint MurdererSpawnPoint { get; }
                public SpawnPoint MurdererFootageSpawnPoint { get; }
                public float MurderFootagePositionOffsetFromVictim { get; }
                public float MurderFootageAngleOffsetFromVictim { get; }
                public Model MurdererPedModel { get; }
                public Func<Ped, string> MurderDescriptor { get; }

                public Tuple<Model, Vector3, Rotator> CCTVCameraModelPositionRotation { get; }
                public Tuple<Model, Vector3, Rotator> PistolModelPositionRotation { get; }


                private Var2Scenario(int index)
                {
                    CalloutMessage = "Murder Investigation";
                    switch (index)
                    {
                        default:
                        case 0:
                            CalloutPosition = new Vector3(-336.17f, -779.53f, 43.61f);

                            VictimSpawnPoint = new SpawnPoint(new Vector3(-323.80f, -774.93f, 43.61f), 298.78f);
                            VictimFootageSpawnPoint = new SpawnPoint(new Vector3(-329.7471f, -780.4505f, 43.60579f), 221.5018f);
                            VictimPedModel = new Model[] { "s_f_y_hooker_01", "a_f_y_business_01", "a_f_y_business_02", "a_f_y_business_03", "a_f_y_business_04" }.GetRandomElement();

                            MurdererSpawnPoint = new SpawnPoint(new Vector3(-359.4019f, -720.4965f, 41.20446f), -43.9999f);
                            MurdererFootageSpawnPoint = new SpawnPoint(new Vector3(-351.52f, -799.95f, 42.89f), 358.56f);
                            MurderFootagePositionOffsetFromVictim = 2.425f;
                            MurderFootageAngleOffsetFromVictim = 180f;
                            MurdererPedModel = new Model[] { "s_m_y_robber_01", "s_m_m_ups_01", "s_m_m_ups_02" }.GetRandomElement();

                            CCTVCameraModelPositionRotation = new Tuple<Model, Vector3, Rotator>(4121760380, new Vector3(-335.0724f, -786.02f, 46.18375f), new Rotator(0f, 0f, 143.2495f));
                            PistolModelPositionRotation = new Tuple<Model, Vector3, Rotator>("w_pi_heavypistol", new Vector3(-338.53f, -761.64f, 43.61f), new Rotator(0.0f, 90.0f, 180f));
                            break;


                        case 1:
                            CalloutPosition = new Vector3(-1204.904f, -326.2822f, 37.83374f);

                            VictimSpawnPoint = new SpawnPoint(new Vector3(-1209.79f, -319.61f, 37.78f), 57.35f);
                            VictimFootageSpawnPoint = new SpawnPoint(new Vector3(-1204.904f, -326.2822f, 37.83374f), 127.7109f);
                            VictimPedModel = new Model[] { "s_f_y_hooker_01", "a_f_y_business_01", "a_f_y_business_02", "a_f_y_business_03", "a_f_y_business_04" }.GetRandomElement();

                            MurdererSpawnPoint = new SpawnPoint(new Vector3(-1186.653f, -359.4948f, 36.68341f), -166.2362f);
                            MurdererFootageSpawnPoint = new SpawnPoint(new Vector3(-1186.653f, -359.4948f, 36.68341f), -166.2362f);
                            MurderFootagePositionOffsetFromVictim = 3.325f;
                            MurderFootageAngleOffsetFromVictim = 180f;
                            MurdererPedModel = new Model[] { "s_m_y_robber_01", "s_m_m_ups_01", "s_m_m_ups_02" }.GetRandomElement();

                            CCTVCameraModelPositionRotation = new Tuple<Model, Vector3, Rotator>(2954561821, new Vector3(-1199.601f, -336.8708f, 40.564785f), new Rotator(0f, 0f, 0f));
                            PistolModelPositionRotation = new Tuple<Model, Vector3, Rotator>("w_pi_heavypistol", new Vector3(-1195.52f, -340.9f, 37.76f), new Rotator(0.0f, 90.0f, 176.04f));
                            break;


                        case 2:
                            CalloutPosition = new Vector3(292.5f, -893.78f, 29.11f);

                            VictimSpawnPoint = new SpawnPoint(new Vector3(299.33f, -885.21f, 29.24f), 315.45f);
                            VictimFootageSpawnPoint = new SpawnPoint(new Vector3(296.4968f, -894.1632f, 29.22771f), -99.77525f);
                            VictimPedModel = new Model[] { "s_f_y_hooker_01", "a_f_y_business_01", "a_f_y_business_02", "a_f_y_business_03", "a_f_y_business_04" }.GetRandomElement();

                            MurdererSpawnPoint = new SpawnPoint(new Vector3(306.5733f, -873.5287f, 29.29159f), -96.99937f);
                            MurdererFootageSpawnPoint = new SpawnPoint(new Vector3(306.5733f, -873.5287f, 29.29159f), 84.2643f);
                            MurderFootagePositionOffsetFromVictim = 3.325f;
                            MurderFootageAngleOffsetFromVictim = 180f;
                            MurdererPedModel = new Model[] { "s_m_y_robber_01", "s_m_m_ups_01", "s_m_m_ups_02" }.GetRandomElement();

                            CCTVCameraModelPositionRotation = new Tuple<Model, Vector3, Rotator>(2452560208, new Vector3(303.583f, -875.5649f, 31.275f), new Rotator(0f, 0f, 0f));
                            PistolModelPositionRotation = new Tuple<Model, Vector3, Rotator>("w_pi_heavypistol", new Vector3(305.85f, -874.72f, 29.29f), new Rotator(0.0f, 90.0f, 176.04f));
                            break;
                    }

                    MurderDescriptor = getDescriptorForPedModel(MurdererPedModel);
                }


                private Func<Ped, string> getDescriptorForPedModel(Model model)
                {
                    Func<Ped, string> descriptor = null;

                    if (model == new Model("s_m_y_robber_01"))
                    {
                        descriptor = (murderPed) =>
                        {
                            int headDrawVar = NativeFunction.CallByName<int>("GET_PED_DRAWABLE_VARIATION", murderPed, 0);
                            string race = headDrawVar == 0 ? "white" : "black";

                            int upperPartDrawVar = NativeFunction.CallByName<int>("GET_PED_DRAWABLE_VARIATION", murderPed, 3); // 0 t-shit   1 jacket
                            int upperPartTextureVar = NativeFunction.CallByName<int>("GET_PED_TEXTURE_VARIATION", murderPed, 3);

                            string upperDesc = "";

                            if (upperPartDrawVar == 0)  // t-shirt
                            {
                                if (upperPartTextureVar == 0) // black
                                {
                                    upperDesc = "a black T-shirt";
                                }
                                else if (upperPartTextureVar == 1) // grey
                                {
                                    upperDesc = "a grey T-shirt";
                                }
                                else if (upperPartTextureVar == 2) // white
                                {
                                    upperDesc = "a white T-shirt";
                                }
                                else if (upperPartTextureVar == 3) // dark grey
                                {
                                    upperDesc = "a dark grey T-shirt";
                                }
                            }
                            else     // jacket
                            {
                                if (upperPartTextureVar == 0) // grey with black t-shirt
                                {
                                    upperDesc = "a grey jacket with a black T-shirt";
                                }
                                else if (upperPartTextureVar == 1) // blue with white t-shirt
                                {
                                    upperDesc = "a blue jacket with a white T-shirt";
                                }
                                else if (upperPartTextureVar == 2) // black with purple t-shirt
                                {
                                    upperDesc = "a black jacket with a purple T-shirt";
                                }
                            }

                            return "Suspect is a " + race + " male and wears " + upperDesc + ".";
                        };
                    }
                    else if (model == new Model("s_m_m_ups_01") || model == new Model("s_m_m_ups_02"))
                    {
                        descriptor = (murderPed) =>
                        {
                            int headDrawVar = NativeFunction.CallByName<int>("GET_PED_DRAWABLE_VARIATION", murderPed, 0);
                            string race = headDrawVar == 0 ? "white" : "black";
                            return "Suspect is a " + race + " male and wears a Post OP uniform.";
                        };
                    }
                    else
                    {
                        descriptor = (murderPed) => { return "MurderDescription [NOT IMPLEMENTED]"; };
                    }

                    return descriptor;
                }
            }




            public enum MurdererReaction
            {
                LeaveAreaRunning,
                LeaveAreaWalking,
                AttackPlayer,
            }
        }
    }
}