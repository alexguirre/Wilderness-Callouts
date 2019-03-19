namespace WildernessCallouts.Callouts
{
    using Rage;
    using Rage.Native;
    using System.Collections.Generic;
    using System;
    using System.Linq;
    using LSPD_First_Response.Mod.Callouts;
    using LSPD_First_Response.Mod.API;
    using WildernessCallouts.Types;
    using RAGENativeUI.Elements;
    using System.Drawing;
    using System.Windows.Forms;

    [CalloutInfo("Demonstration", CalloutProbability.Medium)]
    internal class Demonstration : CalloutBase
    {
        DemonstrationSpawn spawnUsed;
        Blip blip;
        EDemonstrationStates state;
        EDecision decision;

        public override bool OnBeforeCalloutDisplayed()
        {
            spawnUsed = DemonstrationSpawn.GetSpawn();
            for (int i = 0; i < 20; i++)
            {
                Logger.LogTrivial(this.GetType().Name, "Get spawn attempt #" + i);
                if (spawnUsed.CommandingOfficerPedSpawnPoint.Position.DistanceTo(Game.LocalPlayer.Character) < 1500f &&
                    spawnUsed.CommandingOfficerPedSpawnPoint.Position.DistanceTo(Game.LocalPlayer.Character) > 40f)
                    break;
                spawnUsed = DemonstrationSpawn.GetSpawn();
            }
            if (spawnUsed.CommandingOfficerPedSpawnPoint.Position.DistanceTo(Game.LocalPlayer.Character) > 1500f)
            {
                Logger.LogTrivial(this.GetType().Name, "Aborting: Spawn too far");
                return false;
            }
            else if (spawnUsed.CommandingOfficerPedSpawnPoint.Position.DistanceTo(Game.LocalPlayer.Character) < 40f)
            {
                Logger.LogTrivial(this.GetType().Name, "Aborting: Spawn too close");
                return false;
            }

            //for (int i = 0; i < 15; i++)
            //{
            //    spawnUsed = DemonstrationSpawn.GetSpawn();
            //    if (Game.LocalPlayer.Character.Position.DistanceTo(spawnUsed.CommandingOfficerPedSpawnPoint.Position) < 1250.0f && Game.LocalPlayer.Character.Position.DistanceTo(spawnUsed.CommandingOfficerPedSpawnPoint.Position) > 50f) break;
            //}
            //if (Game.LocalPlayer.Character.Position.DistanceTo(spawnUsed.CommandingOfficerPedSpawnPoint.Position) < 1250.0f || Game.LocalPlayer.Character.Position.DistanceTo(spawnUsed.CommandingOfficerPedSpawnPoint.Position) < 50f)
            //    return false;

            this.CalloutMessage = "Demonstration";
            this.CalloutPosition = spawnUsed.CommandingOfficerPedSpawnPoint.Position;

            this.ShowCalloutAreaBlipBeforeAccepting(this.CalloutPosition, 20.0f);

            return spawnUsed.Create();
        }

        public override bool OnCalloutAccepted()
        {
            state = EDemonstrationStates.EnRoute;
            blip = new Blip(spawnUsed.CommandingOfficerPedSpawnPoint.Position);
            blip.EnableRoute(blip.Color);
            blip.SetName("Demonstration: Commanding Officer");
            Game.DisplayHelp("Press " + Controls.ForceCalloutEnd.ToUserFriendlyName() + " to end the callout", 12500);
            return base.OnCalloutAccepted();
        }

        public override void OnCalloutNotAccepted()
        {
            state = EDemonstrationStates.End;
            spawnUsed.Delete();
            if (blip.Exists()) blip.Delete();
            base.OnCalloutNotAccepted();
        }

        bool arePedsPrepared = false;
        bool areHateRelationshipSet = false;
        ulong tickCountAtDecision;
        ulong tickUntilAction = (ulong)Globals.Random.Next(2250, 37500);

        public override void Process()
        {
            if (state == EDemonstrationStates.EnRoute && Game.LocalPlayer.Character.DistanceTo(spawnUsed.CiviliansSpawnPoints.First().Position) < 60.0f)
            {
                state = EDemonstrationStates.OnScene;
                Game.DisplayHelp("Approach the ~y~commanding officer", 10000);
            }

            if (!arePedsPrepared && state >= EDemonstrationStates.OnScene)
            {
                spawnUsed.PrepareCivilians();
                arePedsPrepared = true;
                GameFiber.Sleep(500);
            }
            if (state == EDemonstrationStates.OnScene || state == EDemonstrationStates.DecisionActionMade || state == EDemonstrationStates.DecisionMade || state == EDemonstrationStates.TalkedToOfficer || state == EDemonstrationStates.WaitingPlayerDecision || state == EDemonstrationStates.End)
            {
                CheckPedsToDetachObjects();
            }

            if (state == EDemonstrationStates.OnScene && !areHateRelationshipSet && Game.LocalPlayer.Character.DistanceTo2D(spawnUsed.CommandingOfficerPed) < 5.75f)
            {
                Game.DisplayHelp("Press ~b~" + Controls.PrimaryAction.ToUserFriendlyName() + "~s~ to talk", 5000);
                if (Controls.PrimaryAction.IsJustPressed())
                {
                    spawnUsed.CommandingOfficerPed.Tasks.AchieveHeading(spawnUsed.CommandingOfficerPed.GetHeadingTowards(Game.LocalPlayer.Character)).WaitForCompletion(1000);
                    spawnUsed.CommandingOfficerPed.PlayAmbientSpeech(Speech.GENERIC_HI);
                    Game.DisplaySubtitle("~b~Commanding Officer:~s~ What should we do?", 5000);
                    GameFiber.Sleep(2000);
                    state = EDemonstrationStates.WaitingPlayerDecision;
                }
            }

            if (state == EDemonstrationStates.WaitingPlayerDecision)
            {
                DrawDecisionPanel();
                if (Game.IsKeyDown(Keys.D1))
                {
                    decision = EDecision.Wait;
                    Game.DisplaySubtitle("~b~You:~s~ We should wait and see what they do", 5000);
                    Logger.LogTrivial(this.GetType().Name, "Decision: " + decision);
                    GameFiber.Sleep(1500);
                    state = EDemonstrationStates.DecisionMade;
                }
                else if (Game.IsKeyDown(Keys.D2))
                {
                    decision = EDecision.TearGas;
                    Game.DisplaySubtitle("~b~You:~s~ We should throw tear gas", 5000);
                    Logger.LogTrivial(this.GetType().Name, "Decision: " + decision);
                    GameFiber.Sleep(1500);
                    state = EDemonstrationStates.DecisionMade;
                }
                else if (Game.IsKeyDown(Keys.D3))
                {
                    decision = EDecision.UseShotguns;
                    Game.DisplaySubtitle("~b~You:~s~ We should use our shotguns", 5000);
                    Logger.LogTrivial(this.GetType().Name, "Decision: " + decision);
                    GameFiber.Sleep(1500);
                    state = EDemonstrationStates.DecisionMade;
                }
                else if (Game.IsKeyDown(Keys.D4))
                {
                    decision = EDecision.TryDisperse;
                    Game.DisplaySubtitle("~b~You:~s~ I'm gonna try to disperse them", 5000);
                    Logger.LogTrivial(this.GetType().Name, "Decision: " + decision);
                    GameFiber.Sleep(1500);
                    state = EDemonstrationStates.DecisionMade;
                }

                if (areHateRelationshipSet)
                {
                    state = EDemonstrationStates.DecisionMade;
                }
            }

            if (state == EDemonstrationStates.DecisionMade)
            {
                switch (decision)
                {
                    case EDecision.Wait:
                        GameFiber.StartNew(delegate
                        {
                            try
                            {
                                tickCountAtDecision = Game.TickCount;
                                while (true)
                                {
                                    GameFiber.Yield();

                                    if (state == EDemonstrationStates.End) break;

                                    if ((Game.TickCount - tickCountAtDecision) > tickUntilAction)
                                    {
                                        if (Globals.Random.Next(101) >= 50)
                                        {
                                            Game.DisplaySubtitle("~b~Protester:~s~ Fuck you all!", 7000);
                                            spawnUsed.Civilians.GetRandomElement().Tasks.FightAgainst(Globals.Random.Next(4) <= 2 ? spawnUsed.Police.GetRandomElement() : Game.LocalPlayer.Character);
                                        }
                                        else
                                        {
                                            Game.DisplaySubtitle("~b~Protester:~s~ We leave", 7000);
                                            spawnUsed.Civilians.ForEach(p =>
                                            {
                                                p.Tasks.Clear();
                                            });
                                            CheckPedsToDetachObjects();
                                            spawnUsed.Civilians.ForEach(p =>
                                            {
                                                p.Dismiss();
                                            });
                                        }
                                        break;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.LogException(ex);
                            }
                        });
                        break;
                    case EDecision.TearGas:
                        GameFiber.StartNew(delegate
                        {
                            try
                            {

                                Game.LocalPlayer.Character.Inventory.GiveNewWeapon(EWeaponHash.Smoke_Grenade, 5, true);
                                Ped[] policePeds = spawnUsed.Police.ToArray();
                                policePeds.Shuffle();
                                foreach (Ped ped in policePeds)
                                {
                                    ped.Inventory.GiveNewWeapon(EWeaponHash.Smoke_Grenade, 10, true);
                                }
                                GameFiber.Sleep(1500);
                                foreach (Ped ped in spawnUsed.Civilians)
                                {
                                    ped.BlockPermanentEvents = true;
                                    ped.Health -= 20;
                                }
                                foreach (Ped ped in policePeds)
                                {
                                    GameFiber.StartNew(delegate
                                    {
                                        Vector3 posToThrow = spawnUsed.CiviliansSpawnPoints.GetRandomElement().Position.AroundPosition(2.0f);
                                        ped.Tasks.AchieveHeading(ped.GetHeadingTowards(posToThrow)).WaitForCompletion(1500);
                                        NativeFunction.Natives.TASK_THROW_PROJECTILE(ped, posToThrow.X, posToThrow.Y, posToThrow.Z);
                                        GameFiber.Sleep(3500);
                                        NativeFunction.Natives.REMOVE_WEAPON_FROM_PED(ped, (uint)EWeaponHash.Smoke_Grenade);
                                    });
                                }
                                GameFiber.Sleep(5750);
                                foreach (Ped ped in spawnUsed.Civilians)
                                {
                                    ped.BlockPermanentEvents = false;
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.LogException(ex);
                            }
                        });
                        break;
                    case EDecision.UseShotguns:
                        GameFiber.StartNew(delegate
                        {
                            Game.LocalPlayer.Character.Inventory.GiveNewWeapon(EWeaponHash.Pump_Shotgun, 25, true);
                            foreach (Ped ped in spawnUsed.Police)
                            {
                                ped.Inventory.GiveNewWeapon(EWeaponHash.Pump_Shotgun, 200, true);
                                GameFiber.StartNew(delegate
                                {
                                    Ped pedToAttack = spawnUsed.Civilians.GetRandomElement();
                                    ped.Tasks.AchieveHeading(ped.GetHeadingTowards(pedToAttack)).WaitForCompletion(1500);
                                    NativeFunction.Natives.TASK_AIM_GUN_AT_ENTITY(ped, pedToAttack, -1, true);
                                    GameFiber.Sleep(5250);
                                    NativeFunction.Natives.TASK_SHOOT_AT_ENTITY(ped, pedToAttack, 1675, (uint)Rage.FiringPattern.SingleShot);
                                });
                                GameFiber.Sleep(310);
                            }
                        });
                        break;
                    case EDecision.TryDisperse:
                        GameFiber.StartNew(delegate
                        {
                            Game.DisplayHelp("Press ~b~" + Controls.PrimaryAction.ToUserFriendlyName() + "~s~ to try to disperse near civilians", 10000);
                            while (true)
                            {
                                GameFiber.Yield();

                                if (state == EDemonstrationStates.End || areHateRelationshipSet)
                                    break;

                                if (Controls.PrimaryAction.IsJustPressed())
                                {
                                    Game.LocalPlayer.Character.PlayAmbientSpeech(null, new string[] { Speech.BLOCKED_GENERIC, Speech.PROVOKE_TRESPASS, Speech.BLOCKED_GENERIC, Speech.PROVOKE_TRESPASS, Speech.BLOCKED_IN_PURSUIT }.GetRandomElement(), 0, SpeechModifier.Shouted);
                                    List<Ped> nearPeds = GetNearestCivilians(Game.LocalPlayer.Character.Position, Globals.Random.Next(3, 7));
                                    foreach (Ped ped in nearPeds)
                                    {
                                        if (ped.Exists())
                                        {
                                            if (Globals.Random.Next(101) < Globals.Random.Next(85, 100))
                                            {
                                                ped.Tasks.Clear();
                                                CheckPedsToDetachObjects();
                                                spawnUsed.Civilians.Remove(ped);
                                                ped.Dismiss();
                                                if(Globals.Random.Next(2) == 1)
                                                    ped.PlayAmbientSpeech(null, Globals.Random.Next(2) == 1 ? Speech.GENERIC_WHATEVER : Speech.GENERIC_INSULT_MED, 0, SpeechModifier.Standard);
                                            }
                                            else
                                            {
                                                ped.PlayAmbientSpeech(null, Globals.Random.Next(2) == 1 ? Globals.Random.Next(2) == 1 ? Speech.GENERIC_INSULT_HIGH : Speech.GENERIC_INSULT_MED : Speech.GENERIC_FUCK_YOU, 0, SpeechModifier.Shouted);
                                                ped.Tasks.FightAgainst(Game.LocalPlayer.Character);
                                            }
                                        }
                                    }

                                    if(spawnUsed.Civilians.Count <= 0)
                                    {
                                        Game.DisplayHelp("All civilians dispersed");
                                        break;
                                    }
                                }
                            }
                        });
                        break;
                    default:
                        break;
                }
                state = EDemonstrationStates.DecisionActionMade;
            }


            if (!areHateRelationshipSet)
            {
                for (int i = 0; i < spawnUsed.Civilians.Count; i++)
                {
                    if(spawnUsed.Civilians[i] != null && spawnUsed.Civilians[i].Exists() && (spawnUsed.Civilians[i].IsInMeleeCombat || spawnUsed.Civilians[i].IsShooting))
                    {
                        spawnUsed.SetRelationships(Relationship.Hate);
                        areHateRelationshipSet = true;
                    }
                }
            }

            //WildernessCallouts.Common.StopAllNonEmergencyVehicles(spawnUsed.CommandingOfficerPedSpawnPoint.Position, 140f);

            base.Process();
        }


        private void CheckPedsToDetachObjects()
        {
            for (int i = 0; i < spawnUsed.GuitarCivilians.Count; i++)
            {
                if (spawnUsed.GuitarCivilians[i].Item1.Exists())
                {
                    if (!spawnUsed.GuitarCivilians[i].Item1.IsPlayingAnimation(DemonstrationSpawn.GuitarAnimation.Item1, DemonstrationSpawn.GuitarAnimation.Item2) ||
                        spawnUsed.GuitarCivilians[i].Item1.IsDead ||
                        spawnUsed.GuitarCivilians[i].Item1.IsFleeing ||
                        spawnUsed.GuitarCivilians[i].Item1.IsInMeleeCombat ||
                        spawnUsed.GuitarCivilians[i].Item1.IsShooting ||
                        spawnUsed.GuitarCivilians[i].Item1.IsSwimming ||
                        spawnUsed.GuitarCivilians[i].Item1.IsRagdoll ||
                        spawnUsed.GuitarCivilians[i].Item1.IsJacking ||
                        spawnUsed.GuitarCivilians[i].Item1.IsJumping ||
                        spawnUsed.GuitarCivilians[i].Item1.IsInAnyVehicle(false))
                    {
                        if (spawnUsed.GuitarCivilians[i].Item2.Exists())
                        {
                            //spawnUsed.GuitarCivilians[i].Item1.Tasks.Clear();
                            if (spawnUsed.GuitarCivilians[i].Item1.IsPlayingAnimation(DemonstrationSpawn.GuitarAnimation.Item1, DemonstrationSpawn.GuitarAnimation.Item2))
                                spawnUsed.GuitarCivilians[i].Item1.Tasks.ClearSecondary();
                            spawnUsed.GuitarCivilians[i].Item2.Detach();
                            spawnUsed.GuitarCivilians[i].Item2.Dismiss();
                            spawnUsed.GuitarCivilians.RemoveAt(i);
                        }
                    }
                }
                else
                {
                    if (spawnUsed.GuitarCivilians[i].Item2.Exists())
                        spawnUsed.GuitarCivilians[i].Item2.Dismiss();
                    spawnUsed.GuitarCivilians.RemoveAt(i);
                }
            }

            for (int i = 0; i < spawnUsed.BongosCivilians.Count; i++)
            {
                if (spawnUsed.BongosCivilians[i].Item1.Exists())
                {
                    if (!spawnUsed.BongosCivilians[i].Item1.IsPlayingAnimation(DemonstrationSpawn.BongosAnimation.Item1, DemonstrationSpawn.BongosAnimation.Item2) ||
                        spawnUsed.BongosCivilians[i].Item1.IsDead ||
                        spawnUsed.BongosCivilians[i].Item1.IsFleeing ||
                        spawnUsed.BongosCivilians[i].Item1.IsInMeleeCombat ||
                        spawnUsed.BongosCivilians[i].Item1.IsShooting ||
                        spawnUsed.BongosCivilians[i].Item1.IsSwimming ||
                        spawnUsed.BongosCivilians[i].Item1.IsRagdoll ||
                        spawnUsed.BongosCivilians[i].Item1.IsJacking ||
                        spawnUsed.BongosCivilians[i].Item1.IsJumping ||
                        spawnUsed.BongosCivilians[i].Item1.IsInAnyVehicle(false))
                    {
                        if (spawnUsed.BongosCivilians[i].Item2.Exists())
                        {
                            //spawnUsed.BongosCivilians[i].Item1.Tasks.Clear();
                            if (spawnUsed.BongosCivilians[i].Item1.IsPlayingAnimation(DemonstrationSpawn.BongosAnimation.Item1, DemonstrationSpawn.BongosAnimation.Item2))
                                spawnUsed.BongosCivilians[i].Item1.Tasks.ClearSecondary();
                            spawnUsed.BongosCivilians[i].Item2.Detach();
                            spawnUsed.BongosCivilians[i].Item2.Dismiss();
                            spawnUsed.BongosCivilians.RemoveAt(i);
                        }
                    }
                }
                else
                {
                    if (spawnUsed.BongosCivilians[i].Item2.Exists())
                        spawnUsed.BongosCivilians[i].Item2.Dismiss();
                    spawnUsed.BongosCivilians.RemoveAt(i);
                }
            }

            for (int i = 0; i < spawnUsed.ProtestSignCivilians.Count; i++)
            {
                if (spawnUsed.ProtestSignCivilians[i].Item1.Exists())
                {
                    if (!spawnUsed.ProtestSignCivilians[i].Item1.IsPlayingAnimation(DemonstrationSpawn.ProtestSignAnimation.Item1, DemonstrationSpawn.ProtestSignAnimation.Item2) ||
                        spawnUsed.ProtestSignCivilians[i].Item1.IsDead ||
                        spawnUsed.ProtestSignCivilians[i].Item1.IsFleeing ||
                        spawnUsed.ProtestSignCivilians[i].Item1.IsInMeleeCombat ||
                        spawnUsed.ProtestSignCivilians[i].Item1.IsShooting ||
                        spawnUsed.ProtestSignCivilians[i].Item1.IsSwimming ||
                        spawnUsed.ProtestSignCivilians[i].Item1.IsRagdoll ||
                        spawnUsed.ProtestSignCivilians[i].Item1.IsJacking ||
                        spawnUsed.ProtestSignCivilians[i].Item1.IsJumping ||
                        spawnUsed.ProtestSignCivilians[i].Item1.IsInAnyVehicle(false))
                    {
                        if (spawnUsed.ProtestSignCivilians[i].Item2.Exists())
                        {
                            //spawnUsed.ProtestSignCivilians[i].Item1.Tasks.Clear();
                            if (spawnUsed.ProtestSignCivilians[i].Item1.IsPlayingAnimation(DemonstrationSpawn.ProtestSignAnimation.Item1, DemonstrationSpawn.ProtestSignAnimation.Item2))
                                spawnUsed.ProtestSignCivilians[i].Item1.Tasks.ClearSecondary();
                            spawnUsed.ProtestSignCivilians[i].Item2.Detach();
                            spawnUsed.ProtestSignCivilians[i].Item2.Dismiss();
                            spawnUsed.ProtestSignCivilians.RemoveAt(i);
                        }
                    }
                }
                else
                {
                    if (spawnUsed.ProtestSignCivilians[i].Item2.Exists())
                        spawnUsed.ProtestSignCivilians[i].Item2.Dismiss();
                    spawnUsed.ProtestSignCivilians.RemoveAt(i);
                }
            }
        }


        public override void End()
        {
            state = EDemonstrationStates.End;
            for (int i = 0; i < spawnUsed.Civilians.Count; i++)
            { 
                if(spawnUsed.Civilians[i] != null && spawnUsed.Civilians[i].Exists()) spawnUsed.Civilians[i].Tasks.Clear();
            }
            CheckPedsToDetachObjects();
            spawnUsed.Dismiss();
            if (blip.Exists()) blip.Delete();
            base.End();
        }

        protected override void CleanUp()
        {
            // TODO: implement CleanUp()
        }

        private void DrawDecisionPanel()
        {
            new ResText("[~b~1~s~] You: \"" + "We should wait and see what they do" + "\"", new Point(20, 30), 0.25f, Color.White, RAGENativeUI.Common.EFont.ChaletLondon, ResText.Alignment.Left).Draw();
            new ResText("[~b~2~s~] You: \"" + "We should throw tear gas" + "\"", new Point(20, 60), 0.25f, Color.White, RAGENativeUI.Common.EFont.ChaletLondon, ResText.Alignment.Left).Draw();
            new ResText("[~b~3~s~] You: \"" + "We should use our shotguns" + "\"", new Point(20, 90), 0.25f, Color.White, RAGENativeUI.Common.EFont.ChaletLondon, ResText.Alignment.Left).Draw();
            new ResText("[~b~4~s~] You: \"" + "I'm gonna try to disperse them" + "\"", new Point(20, 120), 0.25f, Color.White, RAGENativeUI.Common.EFont.ChaletLondon, ResText.Alignment.Left).Draw();

            new ResRectangle(new Point(10, 20), new Size(480, 130), Color.FromArgb(150, Color.DarkGray)).Draw();
        }


        private List<Ped> GetNearestCivilians(Vector3 position, int count)
        {
            List<Ped> allPeds = new List<Ped>(spawnUsed.Civilians);
            return allPeds.OrderBy(p => position.DistanceTo(p)).ToList().GetRange(0, count);
        }


        public enum EDemonstrationStates
        {
            EnRoute,
            OnScene,
            TalkedToOfficer,
            WaitingPlayerDecision,
            DecisionMade,
            DecisionActionMade,
            End,
        }
        public enum EDecision
        {
            Wait,
            TearGas,
            UseShotguns,
            TryDisperse,
        }
    }

    internal class DemonstrationSpawn
    {
        public uint SpeedZoneHandle;

        public RelationshipGroup CiviliansRelationshipGroup;
        public RelationshipGroup SwatsRelationshipGroup;

        public List<SpawnPoint> CiviliansSpawnPoints;
        public List<Ped> Civilians;
        public List<Rage.Object> Objects;
        public List<Tuple<Ped, Rage.Object>> ProtestSignCivilians;
        public List<Tuple<Ped, Rage.Object>> GuitarCivilians;
        public List<Tuple<Ped, Rage.Object>> BongosCivilians;

        public List<SpawnPoint> PoliceSpawnPoints;
        public List<Ped> Police;

        public List<SpawnPoint> BarriersSpawnPoints;
        public List<Rage.Object> Barriers;

        public SpawnPoint CommandingOfficerVehicleSpawnPoint;
        public Vehicle CommandingOfficerVehicle;

        public SpawnPoint CommandingOfficerPedSpawnPoint;
        public Ped CommandingOfficerPed;


        public DemonstrationSpawn(SpawnPoint commandingOfficerVehicleSpawnPoint,
                                  SpawnPoint commandingOfficerPedSpawnPoint,
                                  List<SpawnPoint> civiliansSpawnPoints,
                                  List<SpawnPoint> policeSpawnPoints,
                                  List<SpawnPoint> barriersSpawnPoints)
        {
            this.CiviliansRelationshipGroup = new RelationshipGroup("DEMOSTRATIONCIVILIANS");
            this.SwatsRelationshipGroup = new RelationshipGroup("DEMOSTRATIONSWATS");

            SetRelationships(Relationship.Neutral);

            this.CommandingOfficerVehicleSpawnPoint = commandingOfficerVehicleSpawnPoint;
            this.CommandingOfficerPedSpawnPoint = commandingOfficerPedSpawnPoint;

            this.CiviliansSpawnPoints = civiliansSpawnPoints;
            this.Civilians = new List<Ped>();

            this.Objects = new List<Rage.Object>();
            this.ProtestSignCivilians = new List<Tuple<Ped, Rage.Object>>();
            this.GuitarCivilians = new List<Tuple<Ped, Rage.Object>>();
            this.BongosCivilians = new List<Tuple<Ped, Rage.Object>>();

            this.PoliceSpawnPoints = policeSpawnPoints;
            this.Police = new List<Ped>();

            this.BarriersSpawnPoints = barriersSpawnPoints;
            this.Barriers = new List<Rage.Object>();
        }

        public bool Create(bool playSound = true)
        {
            SpeedZoneHandle = World.AddSpeedZone(CiviliansSpawnPoints.First().Position, 100f, 0f);

            CommandingOfficerVehicle = CreateCommandingOfficerVehicle(CommandingOfficerVehicleSpawnPoint);
            if (!CommandingOfficerVehicle.Exists()) return false;

            CommandingOfficerPed = CreateCommandingOfficerPed(CommandingOfficerPedSpawnPoint);
            if (!CommandingOfficerPed.Exists()) return false;

            foreach (SpawnPoint sp in CiviliansSpawnPoints)
            {
                for (int i = 0; i < Globals.Random.Next(1, 4); i++)
                {
                    Ped p = CreateCivilianPed(sp);
                    if (!p.Exists()) return false;
                }
            }
            foreach (SpawnPoint sp in PoliceSpawnPoints)
            {
                Ped p = CreatePolicePed(sp);
                if (!p.Exists()) return false;
            }
            foreach (SpawnPoint sp in BarriersSpawnPoints)
            {
                Rage.Object o = new Rage.Object(barrierModel, sp.Position, sp.Heading);
                if (!o.Exists()) return false;
                NativeFunction.Natives.SET_ACTIVATE_OBJECT_PHYSICS_AS_SOON_AS_IT_IS_UNFROZEN(o, true);
                Barriers.Add(o);
            }

            if (playSound)
            {
                string reportSound = Globals.Random.Next(2) == 1 ? Globals.Random.Next(2) == 1 ? Globals.Random.Next(2) == 1 ? "WE_HAVE" : "UNITS_REPORTING" : "CITIZENS_REPORT" : "OFFICERS_REPORT";
                Functions.PlayScannerAudioUsingPosition(reportSound + " CRIME_DISTURBANCE IN_OR_ON_POSITION UNITS_RESPOND_CODE_02", CommandingOfficerPedSpawnPoint.Position);
            }

            return true;
        }

        public void Dismiss()
        {
            if (CommandingOfficerPed.Exists()) CommandingOfficerPed.Dismiss();
            if (CommandingOfficerVehicle.Exists()) CommandingOfficerVehicle.Dismiss();

            foreach (Ped p in Civilians)
            {
                if (p.Exists()) p.Dismiss();
            }
            foreach (Ped p in Police)
            {
                if (p.Exists()) p.Dismiss();
            }
            foreach (Rage.Object o in Objects)
            {
                if (o.Exists()) o.Dismiss();
            }
            foreach (Rage.Object o in Barriers)
            {
                if (o.Exists()) o.Dismiss();
            }
            
            Civilians.Clear();
            Police.Clear();
            Barriers.Clear();
            Objects.Clear();
            ProtestSignCivilians.Clear();
            GuitarCivilians.Clear();
            BongosCivilians.Clear();
            CiviliansSpawnPoints.Clear();
            PoliceSpawnPoints.Clear();
            BarriersSpawnPoints.Clear();

            World.RemoveSpeedZone(SpeedZoneHandle);
        }

        public void Delete()
        {
            if (CommandingOfficerPed.Exists()) CommandingOfficerPed.Delete();
            if (CommandingOfficerVehicle.Exists()) CommandingOfficerVehicle.Delete();

            foreach (Ped p in Civilians)
            {
                if (p.Exists()) p.Delete();
            }
            foreach (Ped p in Police)
            {
                if (p.Exists()) p.Delete();
            }
            foreach (Rage.Object o in Objects)
            {
                if (o.Exists()) o.Delete();
            }
            foreach (Rage.Object o in Barriers)
            {
                if (o.Exists()) o.Delete();
            }
            
            Civilians.Clear();
            Police.Clear();
            Barriers.Clear();
            Objects.Clear();
            ProtestSignCivilians.Clear();
            GuitarCivilians.Clear();
            BongosCivilians.Clear();
            CiviliansSpawnPoints.Clear();
            PoliceSpawnPoints.Clear();
            BarriersSpawnPoints.Clear();

            World.RemoveSpeedZone(SpeedZoneHandle);
        }


        public void PrepareCivilians()
        {
            foreach (Ped ped in Civilians)
            {
                switch (Globals.Random.Next(7))
                {
                    case 0:
                        ped.Tasks.PlayAnimation(ProtestSignAnimation.Item1, ProtestSignAnimation.Item2, 5.0f, AnimationFlags.UpperBodyOnly | AnimationFlags.SecondaryTask | AnimationFlags.Loop);
                        Rage.Object obj1 = new Rage.Object(protestSignModel, Vector3.Zero);
                        obj1.AttachToEntity(ped, ped.GetBoneIndex(PedBoneId.RightPhHand), Vector3.Zero, Rotator.Zero);
                        Objects.Add(obj1);
                        ProtestSignCivilians.Add(new Tuple<Ped, Rage.Object>(ped, obj1));
                        break;
                    case 1:
                        ped.Tasks.PlayAnimation(GuitarAnimation.Item1, GuitarAnimation.Item2, 5.0f, AnimationFlags.UpperBodyOnly | AnimationFlags.SecondaryTask | AnimationFlags.Loop);
                        Rage.Object obj2 = new Rage.Object(guitarModel, Vector3.Zero);
                        obj2.AttachToEntity(ped, ped.GetBoneIndex(PedBoneId.LeftPhHand), Vector3.Zero, Rotator.Zero);
                        Objects.Add(obj2);
                        GuitarCivilians.Add(new Tuple<Ped, Rage.Object>(ped, obj2));
                        //Scenario.StartInPlace(ped, Scenario.WORLD_HUMAN_MUSICIAN, false);
                        break;
                    case 2:
                        ped.Tasks.PlayAnimation(BongosAnimation.Item1, BongosAnimation.Item2, 5.0f, AnimationFlags.UpperBodyOnly | AnimationFlags.SecondaryTask | AnimationFlags.Loop);
                        Rage.Object obj3 = new Rage.Object(bongosModel, Vector3.Zero);
                        obj3.AttachToEntity(ped, ped.GetBoneIndex(PedBoneId.LeftPhHand), Vector3.Zero, Rotator.Zero);
                        Objects.Add(obj3);
                        BongosCivilians.Add(new Tuple<Ped, Rage.Object>(ped, obj3));
                        //Scenario.StartInPlace(ped, Scenario.WORLD_HUMAN_MUSICIAN, false);
                        break;
                    case 3:
                        if(ped.IsMale || Globals.Random.Next(2) == 1)
                            ped.Tasks.PlayAnimation(MalePicnicAnimations.Item1, MalePicnicAnimations.Item2.GetRandomElement(), 1.0f, AnimationFlags.SecondaryTask | AnimationFlags.Loop);
                        else
                            ped.Tasks.PlayAnimation(FemalePicnicAnimations.Item1, FemalePicnicAnimations.Item2.GetRandomElement(), 1.0f, AnimationFlags.SecondaryTask | AnimationFlags.Loop);
                        break;
                    case 4:
                        ped.Tasks.PlayAnimation(UpperVAnimation.Item1, UpperVAnimation.Item2, 1.0f, AnimationFlags.UpperBodyOnly | AnimationFlags.SecondaryTask | AnimationFlags.Loop);
                        break;
                    case 5:
                        ped.Tasks.PlayAnimation(UpperFingerAnimations.Item1, UpperFingerAnimations.Item2.GetRandomElement(), 1.0f, AnimationFlags.UpperBodyOnly | AnimationFlags.SecondaryTask | AnimationFlags.Loop);
                        break;
                    case 6:
                        break;
                    default:
                        break;
                }
                if (Globals.Random.Next(3) == 1)
                {
                    EWeaponHash[] weapons = { EWeaponHash.Bat, EWeaponHash.Bottle, EWeaponHash.Crowbar, EWeaponHash.Dagger, EWeaponHash.Flare_Gun, EWeaponHash.Flashlight, EWeaponHash.Golf_Club, EWeaponHash.Hammer, EWeaponHash.Knife, EWeaponHash.Knuckle_Dusters, EWeaponHash.Machete, EWeaponHash.Switchblade, EWeaponHash.Unarmed };
                    ped.Inventory.GiveNewWeapon(weapons.GetRandomElement(), 9999, false);
                }
                ped.RelationshipGroup = CiviliansRelationshipGroup;
                ped.WanderInArea(ped.Position, MathHelper.GetRandomSingle(5f, 12.5f), MathHelper.GetRandomSingle(3.75f, 8f), MathHelper.GetRandomSingle(0.5f, 5f));
            }
        }

        public void SetRelationships(Relationship relation)
        {
            Game.SetRelationshipBetweenRelationshipGroups(CiviliansRelationshipGroup, Game.LocalPlayer.Character.RelationshipGroup, relation);
            Game.SetRelationshipBetweenRelationshipGroups(CiviliansRelationshipGroup, SwatsRelationshipGroup, relation);
            Game.SetRelationshipBetweenRelationshipGroups(SwatsRelationshipGroup, CiviliansRelationshipGroup, relation);
            Game.SetRelationshipBetweenRelationshipGroups(SwatsRelationshipGroup, Game.LocalPlayer.Character.RelationshipGroup, Relationship.Companion);
            Game.SetRelationshipBetweenRelationshipGroups(SwatsRelationshipGroup, "COP", Relationship.Companion);
            Game.SetRelationshipBetweenRelationshipGroups("COP", SwatsRelationshipGroup, Relationship.Companion);
        }




        private Ped CreateCivilianPed(SpawnPoint spawnPoint, float aroundPosition = 5.0f)
        {
            Ped ped = new Ped(spawnPoint.Position.AroundPosition(aroundPosition));
            if (!ped.Exists()) return null;
            ped.Heading = MathHelper.GetRandomSingle(0f, 360f);
            ped.RelationshipGroup = CiviliansRelationshipGroup;
            Civilians.Add(ped);

            return ped;
        }

        private Ped CreatePolicePed(SpawnPoint spawnPoint)
        {
            Ped ped = new Ped(GetPolicePedModelForPosition(spawnPoint.Position), spawnPoint.Position, spawnPoint.Heading);
            if (!ped.Exists()) return null;

            if (ped.Model == new Model("s_m_y_swat_01"))
            {
                ped.RelationshipGroup = SwatsRelationshipGroup;
                if (Globals.Random.Next(3) <= 1)
                    NativeFunction.Natives.SET_PED_PROP_INDEX(ped, 0, 0, 0, 2);
            }
            else if (ped.Model == new Model("s_m_y_cop_01"))
            {
                if (Globals.Random.Next(3) <= 1)
                    NativeFunction.Natives.SET_PED_PROP_INDEX(ped, 0, 0, 0, 2);

                NativeFunction.Natives.SET_PED_COMPONENT_VARIATION(ped, 9, 2, 0, 2);

            }
            else if (ped.Model == new Model("s_f_y_cop_01"))
            {
                if (Globals.Random.Next(3) <= 1)
                    NativeFunction.Natives.SET_PED_PROP_INDEX(ped, 0, 0, 0, 2);
            }
            else if (ped.Model == new Model("s_m_y_sheriff_01"))
            {
                if (Globals.Random.Next(3) <= 1)
                    NativeFunction.Natives.SET_PED_PROP_INDEX(ped, 0, Globals.Random.Next(2), 0, 2);

                NativeFunction.Natives.SET_PED_COMPONENT_VARIATION(ped, 9, 2, 0, 2);
            }
            else if (ped.Model == new Model("s_f_y_sheriff_01"))
            {
                if (Globals.Random.Next(3) <= 1)
                    NativeFunction.Natives.SET_PED_PROP_INDEX(ped, 0, Globals.Random.Next(2), 0, 2);
            }

            ped.Health += 50;
            ped.Armor += 25;
            ped.Accuracy = Globals.Random.Next(75, 101);
            ped.Inventory.GiveNewWeapon(Globals.Random.Next(2) == 1 ? EWeaponHash.Stun_Gun : EWeaponHash.Nightstick, 999, true);
            Police.Add(ped);

            return ped;
        }

        private Vehicle CreateCommandingOfficerVehicle(SpawnPoint spawnPoint)
        {
            Model usedModel = "police";
            switch (spawnPoint.Position.GetArea())
            {
                case EWorldArea.Los_Santos:
                    Model[] losSantosModels = { "police", "police2", "police3", "police4", "policet", "riot" };
                    usedModel = losSantosModels.GetRandomElement();
                    break;
                case EWorldArea.Blaine_County:
                    Model[] countyModels = { "sheriff", "sheriff2", "police4", "riot" };
                    usedModel = countyModels.GetRandomElement();
                    break;
                default:
                    Model[] defaultModels = { "police", "police2", "police3", "police4", "policet", "riot" };
                    usedModel = defaultModels.GetRandomElement();
                    break;
            }
            Vehicle veh = new Vehicle(usedModel, spawnPoint.Position, spawnPoint.Heading);
            if (!veh.Exists()) return null;
            veh.IsSirenOn = true;
            return veh;
        }

        private Ped CreateCommandingOfficerPed(SpawnPoint spawnPoint)
        {
            Ped ped = new Ped(GetPolicePedModelForPosition(spawnPoint.Position), spawnPoint.Position, spawnPoint.Heading);
            if (!ped.Exists()) return null;

            if (ped.Model == new Model("s_m_y_swat_01"))
            {
                ped.RelationshipGroup = SwatsRelationshipGroup;
                NativeFunction.Natives.SET_PED_PROP_INDEX(ped, 0, 0, 0, 2);
            }
            else if (ped.Model == new Model("s_m_y_cop_01"))
            {
                NativeFunction.Natives.SET_PED_COMPONENT_VARIATION(ped, 9, 2, 0, 2);
                NativeFunction.Natives.SET_PED_PROP_INDEX(ped, 0, 0, 0, 2);
            }
            else if (ped.Model == new Model("s_f_y_cop_01"))
            {
                NativeFunction.Natives.SET_PED_PROP_INDEX(ped, 0, 0, 0, 2);
            }
            else if (ped.Model == new Model("s_m_y_sheriff_01"))
            {
                NativeFunction.Natives.SET_PED_PROP_INDEX(ped, 0, Globals.Random.Next(2), 0, 2);
                NativeFunction.Natives.SET_PED_COMPONENT_VARIATION(ped, 9, 2, 0, 2);
            }
            else if (ped.Model == new Model("s_f_y_sheriff_01"))
            {
                NativeFunction.Natives.SET_PED_PROP_INDEX(ped, 0, Globals.Random.Next(2), 0, 2);
            }

            ped.Health += 50;
            ped.Armor += 50;
            ped.Accuracy = 100;
            EWeaponHash[] weapons = { EWeaponHash.Stun_Gun, EWeaponHash.Nightstick, EWeaponHash.Nightstick };
            ped.Inventory.GiveNewWeapon(weapons.GetRandomElement(), 999, false);
            return ped;
        }

        private Model GetPoliceVehicleModelForPosition(Vector3 position)
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

        private Model GetPolicePedModelForPosition(Vector3 position)
        {
            switch (position.GetArea())
            {
                case EWorldArea.Los_Santos:
                    return Globals.Random.Next(2) == 1 ? Globals.Random.Next(2) == 1 ? "s_m_y_swat_01" : "s_m_y_cop_01" : Globals.Random.Next(2) == 1 ? "s_m_y_cop_01" : "s_f_y_cop_01";
                case EWorldArea.Blaine_County:
                    return Globals.Random.Next(2) == 1 ? Globals.Random.Next(2) == 1 ? "s_m_y_swat_01" : "s_m_y_sheriff_01" : Globals.Random.Next(2) == 1 ? "s_m_y_sheriff_01" : "s_f_y_sheriff_01";
                default:
                    return Globals.Random.Next(2) == 1 ? Globals.Random.Next(2) == 1 ? "s_m_y_swat_01" : "s_m_y_cop_01" : Globals.Random.Next(2) == 1 ? "s_m_y_cop_01" : "s_f_y_cop_01";
            }
        }

        public static DemonstrationSpawn GetSpawn()
        {
            DemonstrationSpawn[] spawns =
                {
            new DemonstrationSpawn
               (
                   new SpawnPoint(new Vector3(281.65f, -360.68f, 45.09f), 71.91f),
                   new SpawnPoint(new Vector3(279.86f, -363.17f, 44.99f), 157.75f),
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(250.0726f, -370.2138f, 44.46361f), 212.0943f),
                       new SpawnPoint(new Vector3(251.2177f, -372.9369f, 44.45588f), 227.8473f),
                       new SpawnPoint(new Vector3(255.5713f, -374.6367f, 44.54084f), 307.9193f),
                       new SpawnPoint(new Vector3(256.3924f, -370.9944f, 44.576f), 348.4533f),
                       new SpawnPoint(new Vector3(255.1483f, -366.9717f, 44.65506f), 47.83242f),
                       new SpawnPoint(new Vector3(252.7959f, -366.7871f, 44.58767f), 105.0114f),
                       new SpawnPoint(new Vector3(246.7802f, -369.1461f, 44.41951f), 95.08547f),
                       new SpawnPoint(new Vector3(242.719f, -368.3153f, 44.33935f), 44.63741f),
                       new SpawnPoint(new Vector3(241.3328f, -364.9142f, 44.36309f), 353.6055f),
                       new SpawnPoint(new Vector3(243.3767f, -361.5242f, 44.4943f), 294.2503f),
                       new SpawnPoint(new Vector3(247.2309f, -362.6769f, 44.53778f), 226.3781f),
                       new SpawnPoint(new Vector3(251.3047f, -364.9932f, 44.59462f), 292.1879f),
                       new SpawnPoint(new Vector3(254.6318f, -364.1624f, 44.64594f), 281.8137f),
                       new SpawnPoint(new Vector3(257.9822f, -362.1849f, 44.71126f), 2.320381f),
                       new SpawnPoint(new Vector3(250.4442f, -361.509f, 44.6079f), 352.8967f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(284.3677f, -365.4362f, 45.07622f), 61.85228f),
                       new SpawnPoint(new Vector3(282.7736f, -368.767f, 45.17087f), 62.63355f),
                       new SpawnPoint(new Vector3(279.7666f, -375.2906f, 45.12696f), 69.52566f),
                       new SpawnPoint(new Vector3(277.4583f, -380.9303f, 44.99598f), 66.81892f),
                       new SpawnPoint(new Vector3(234.7528f, -397.9717f, 47.92434f), 341.2893f),
                       new SpawnPoint(new Vector3(237.9592f, -399.4912f, 47.92434f), 315.9031f),
                       new SpawnPoint(new Vector3(241.7297f, -401.0565f, 47.92434f), 338.5065f),
                       new SpawnPoint(new Vector3(245.5128f, -402.1897f, 47.92434f), 325.3241f),
                       new SpawnPoint(new Vector3(230.6385f, -384.8007f, 45.9716f), 310.6206f),
                       new SpawnPoint(new Vector3(224.8659f, -363.4324f, 44.16754f), 243.3501f),
                       new SpawnPoint(new Vector3(227.8238f, -357.3618f, 44.29055f), 255.0684f),
                       new SpawnPoint(new Vector3(229.7249f, -352.3038f, 44.33649f), 251.6612f),
                       new SpawnPoint(new Vector3(231.0187f, -347.7847f, 44.30553f), 249.0173f),
                       new SpawnPoint(new Vector3(235.9374f, -344.9409f, 44.33801f), 219.1019f),
                       new SpawnPoint(new Vector3(248.9706f, -398.0126f, 47.15707f), 354.0943f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(223.9564f, -363.2319f, 44.1515f), 68.77076f),
                       new SpawnPoint(new Vector3(225.6525f, -360.2345f, 44.29238f), 69.86156f),
                       new SpawnPoint(new Vector3(226.7557f, -356.6018f, 44.28579f), 65.02778f),
                       new SpawnPoint(new Vector3(227.4293f, -353.7767f, 44.31422f), 68.49522f),
                       new SpawnPoint(new Vector3(228.8091f, -350.9733f, 44.32382f), 68.62331f),
                       new SpawnPoint(new Vector3(229.2705f, -348.4893f, 44.31383f), 70.35162f),
                       new SpawnPoint(new Vector3(230.4067f, -345.6077f, 44.21911f), 71.83844f),
                       new SpawnPoint(new Vector3(276.8154f, -384.5386f, 44.81588f), 232.3401f),
                       new SpawnPoint(new Vector3(278.1017f, -381.8962f, 44.98238f), 249.6511f),
                       new SpawnPoint(new Vector3(279.2973f, -379.3252f, 45.11865f), 252.7228f),
                       new SpawnPoint(new Vector3(280.4171f, -376.4494f, 45.13783f), 247.9618f),
                       new SpawnPoint(new Vector3(281.338f, -374.1419f, 45.15129f), 253.2567f),
                       new SpawnPoint(new Vector3(282.2971f, -371.537f, 45.16479f), 241.5197f),
                       new SpawnPoint(new Vector3(283.8345f, -368.5864f, 45.18749f), 246.9349f),
                       new SpawnPoint(new Vector3(285.3086f, -365.4015f, 45.07378f), 247.7767f),
                       new SpawnPoint(new Vector3(245.6638f, -401.0016f, 47.92437f), 349.7011f),
                       new SpawnPoint(new Vector3(242.0295f, -399.7638f, 47.92437f), 344.9642f),
                       new SpawnPoint(new Vector3(238.3564f, -398.1996f, 47.92434f), 325.8663f),
                       new SpawnPoint(new Vector3(234.953f, -396.6996f, 47.92434f), 336.8607f),
                   }
               ),


            new DemonstrationSpawn
               (
                   new SpawnPoint(new Vector3(300.8f, 145.66f, 103.77f), 249.12f),
                   new SpawnPoint(new Vector3(300.82f, 149.07f, 103.9f), 36.89f),
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(284.3333f, 164.6893f, 104.3685f), 307.4852f),
                       new SpawnPoint(new Vector3(288.1428f, 167.27f, 104.2939f), 307.3034f),
                       new SpawnPoint(new Vector3(292.8574f, 169.0301f, 104.2398f), 258.7818f),
                       new SpawnPoint(new Vector3(296.2926f, 164.9467f, 104.1535f), 166.5579f),
                       new SpawnPoint(new Vector3(293.261f, 161.4678f, 104.1778f), 104.9244f),
                       new SpawnPoint(new Vector3(289.2672f, 161.492f, 104.2347f), 89.95926f),
                       new SpawnPoint(new Vector3(284.8307f, 159.8766f, 104.3118f), 118.5978f),
                       new SpawnPoint(new Vector3(281.6831f, 158.3879f, 104.3208f), 90.13654f),
                       new SpawnPoint(new Vector3(278.2017f, 159.2802f, 104.3874f), 40.43284f),
                       new SpawnPoint(new Vector3(275.9403f, 162.5781f, 104.4777f), 19.56788f),
                       new SpawnPoint(new Vector3(275.4504f, 165.9924f, 104.482f), 2.53225f),
                       new SpawnPoint(new Vector3(277.3918f, 169.2386f, 104.4884f), 326.4842f),
                       new SpawnPoint(new Vector3(280.0703f, 171.2332f, 104.4293f), 294.1989f),
                       new SpawnPoint(new Vector3(284.0938f, 172.0268f, 104.3787f), 280.0593f),
                       new SpawnPoint(new Vector3(287.7258f, 172.2166f, 104.2906f), 270.4894f),
                       new SpawnPoint(new Vector3(291.8546f, 172.5995f, 104.1899f), 270.1574f),
                       new SpawnPoint(new Vector3(296.4876f, 170.0394f, 104.1536f), 156.2799f),
                       new SpawnPoint(new Vector3(290.7563f, 165.1217f, 104.2571f), 106.6253f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(311.9982f, 157.3752f, 103.8314f), 71.94012f),
                       new SpawnPoint(new Vector3(309.2562f, 150.835f, 103.8352f), 73.30489f),
                       new SpawnPoint(new Vector3(306.6738f, 145.9586f, 103.7188f), 77.28276f),
                       new SpawnPoint(new Vector3(307.6534f, 138.8567f, 103.7439f), 67.57016f),
                       new SpawnPoint(new Vector3(313.9714f, 164.8213f, 103.7887f), 68.8592f),
                       new SpawnPoint(new Vector3(314.8671f, 169.965f, 103.821f), 64.59859f),
                       new SpawnPoint(new Vector3(316.5568f, 176.0671f, 103.8199f), 69.73351f),
                       new SpawnPoint(new Vector3(301.9076f, 188.7497f, 104.0472f), 148.9962f),
                       new SpawnPoint(new Vector3(298.5759f, 190.0674f, 104.1987f), 161.9031f),
                       new SpawnPoint(new Vector3(294.1998f, 191.6962f, 104.327f), 159.0839f),
                       new SpawnPoint(new Vector3(290.8543f, 192.9232f, 104.3727f), 145.5498f),
                       new SpawnPoint(new Vector3(260.2685f, 162.839f, 104.6376f), 265.6091f),
                       new SpawnPoint(new Vector3(262.3403f, 167.9644f, 104.7579f), 247.883f),
                       new SpawnPoint(new Vector3(266.2859f, 175.0909f, 104.7362f), 244.6792f),
                       new SpawnPoint(new Vector3(268.4643f, 182.4393f, 104.626f), 249.946f),
                       new SpawnPoint(new Vector3(272.7305f, 190.6324f, 104.6833f), 246.3107f),
                       new SpawnPoint(new Vector3(271.3248f, 147.8537f, 104.4489f), 330.3111f),
                       new SpawnPoint(new Vector3(268.1558f, 148.9462f, 104.5061f), 337.5022f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(316.1956f, 165.8052f, 103.6409f), 250.5788f),
                       new SpawnPoint(new Vector3(316.6425f, 168.0714f, 103.7761f), 270.4326f),
                       new SpawnPoint(new Vector3(316.836f, 170.6261f, 103.7971f), 251.5828f),
                       new SpawnPoint(new Vector3(317.5037f, 173.6281f, 103.7972f), 255.993f),
                       new SpawnPoint(new Vector3(317.7876f, 176.1308f, 103.7674f), 254.3695f),
                       new SpawnPoint(new Vector3(318.4767f, 177.8637f, 103.6443f), 251.4778f),
                       new SpawnPoint(new Vector3(314.6101f, 162.6712f, 103.8106f), 248.8365f),
                       new SpawnPoint(new Vector3(313.4622f, 159.695f, 103.8211f), 276.8515f),
                       new SpawnPoint(new Vector3(312.7055f, 156.4623f, 103.8126f), 252.172f),
                       new SpawnPoint(new Vector3(312.0292f, 153.4029f, 103.8034f), 246.4267f),
                       new SpawnPoint(new Vector3(310.3764f, 150.4931f, 103.8122f), 242.3852f),
                       new SpawnPoint(new Vector3(309.3667f, 147.9004f, 103.7744f), 250.8723f),
                       new SpawnPoint(new Vector3(308.6383f, 144.7345f, 103.6527f), 292.0865f),
                       new SpawnPoint(new Vector3(309.7167f, 141.736f, 103.5854f), 293.1629f),
                       new SpawnPoint(new Vector3(309.5039f, 139.1552f, 103.7119f), 256.1192f),
                       new SpawnPoint(new Vector3(308.7666f, 136.3912f, 103.7075f), 235.7013f),
                       new SpawnPoint(new Vector3(290.3703f, 191.5685f, 104.3662f), 161.126f),
                       new SpawnPoint(new Vector3(293.0282f, 190.893f, 104.3326f), 160.8747f),
                       new SpawnPoint(new Vector3(296.0884f, 189.8654f, 104.2621f), 162.0607f),
                       new SpawnPoint(new Vector3(298.9452f, 188.4857f, 104.1407f), 160.5048f),
                       new SpawnPoint(new Vector3(301.6555f, 187.2547f, 104.0252f), 158.4867f),
                       new SpawnPoint(new Vector3(270.0298f, 187.803f, 104.7111f), 225.0569f),
                       new SpawnPoint(new Vector3(271.3452f, 190.8597f, 104.7075f), 251.1686f),
                       new SpawnPoint(new Vector3(272.0956f, 194.2466f, 104.7156f), 252.8798f),
                       new SpawnPoint(new Vector3(267.6219f, 184.5918f, 104.5863f), 253.0376f),
                       new SpawnPoint(new Vector3(266.284f, 181.342f, 104.7429f), 232.3467f),
                       new SpawnPoint(new Vector3(265.0675f, 178.492f, 104.7769f), 250.6662f),
                       new SpawnPoint(new Vector3(264.7458f, 175.4149f, 104.764f), 246.8688f),
                       new SpawnPoint(new Vector3(263.2553f, 173.0255f, 104.774f), 245.0639f),
                       new SpawnPoint(new Vector3(262.0021f, 169.6164f, 104.7738f), 246.7471f),
                       new SpawnPoint(new Vector3(260.2393f, 166.918f, 104.7863f), 236.5927f),
                       new SpawnPoint(new Vector3(258.4905f, 164.3484f, 104.7198f), 236.7343f),
                       new SpawnPoint(new Vector3(257.159f, 161.121f, 104.6218f), 247.4179f),
                       new SpawnPoint(new Vector3(256.7849f, 158.0517f, 104.7508f), 248.9293f),
                       new SpawnPoint(new Vector3(267.8452f, 148.1485f, 104.4542f), 338.3409f),
                       new SpawnPoint(new Vector3(270.7504f, 146.8703f, 104.3958f), 338.0408f),
                   }
               ),


            new DemonstrationSpawn
               (
                   new SpawnPoint(new Vector3(-180.89f, -2010.41f, 26.35f), 262.25f),
                   new SpawnPoint(new Vector3(-179.47f, -2007.57f, 26.1f), 347.96f),
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-202.3891f, -2002.769f, 27.62041f), 250.5695f),
                       new SpawnPoint(new Vector3(-219.6008f, -1998.097f, 27.75544f), 306.2256f),
                       new SpawnPoint(new Vector3(-215.9667f, -1995.889f, 27.7506f), 295.451f),
                       new SpawnPoint(new Vector3(-211.0189f, -1993.789f, 27.74751f), 313.6122f),
                       new SpawnPoint(new Vector3(-206.9815f, -1990.87f, 27.63526f), 273.4941f),
                       new SpawnPoint(new Vector3(-203.6229f, -1991.34f, 27.56732f), 259.1091f),
                       new SpawnPoint(new Vector3(-199.9718f, -1991.975f, 27.57882f), 260.2392f),
                       new SpawnPoint(new Vector3(-196.561f, -1994.026f, 27.5997f), 219.0529f),
                       new SpawnPoint(new Vector3(-194.3669f, -1998.442f, 27.60726f), 190.4825f),
                       new SpawnPoint(new Vector3(-195.023f, -2002.716f, 27.60473f), 142.7566f),
                       new SpawnPoint(new Vector3(-197.3333f, -2003.948f, 27.54576f), 121.8114f),
                       new SpawnPoint(new Vector3(-200.1755f, -2005.55f, 27.56693f), 124.5632f),
                       new SpawnPoint(new Vector3(-202.5753f, -2006.537f, 27.54479f), 88.22512f),
                       new SpawnPoint(new Vector3(-205.0257f, -2005.636f, 27.54207f), 42.08016f),
                       new SpawnPoint(new Vector3(-204.6753f, -2003.372f, 27.57187f), 296.2219f),
                       new SpawnPoint(new Vector3(-202.6132f, -2003.305f, 27.52708f), 272.8516f),
                       new SpawnPoint(new Vector3(-200.9043f, -2003.027f, 27.52258f), 328.3063f),
                       new SpawnPoint(new Vector3(-201.0059f, -2000.805f, 27.52924f), 26.72514f),
                       new SpawnPoint(new Vector3(-202.3571f, -1998.635f, 27.51954f), 32.52327f),
                       new SpawnPoint(new Vector3(-205.4951f, -1996.484f, 27.60136f), 121.6201f),
                       new SpawnPoint(new Vector3(-207.0493f, -1997.556f, 27.4807f), 121.8716f),
                       new SpawnPoint(new Vector3(-211.947f, -1997.987f, 27.7163f), 60.70497f),
                       new SpawnPoint(new Vector3(-213.5981f, -1997.183f, 27.61402f), 74.65579f),
                       new SpawnPoint(new Vector3(-214.7955f, -2000.657f, 27.75267f), 198.6063f),
                       new SpawnPoint(new Vector3(-214.9007f, -2002.932f, 27.66892f), 135.122f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-208.0089f, -1983.928f, 27.62041f), 167.5707f),
                       new SpawnPoint(new Vector3(-201.7693f, -1985.233f, 27.62041f), 160.6722f),
                       new SpawnPoint(new Vector3(-193.7757f, -1988.767f, 27.62041f), 144.7655f),
                       new SpawnPoint(new Vector3(-190.0086f, -1991.82f, 27.62039f), 132.3311f),
                       new SpawnPoint(new Vector3(-186.6725f, -1997.275f, 27.75821f), 105.7784f),
                       new SpawnPoint(new Vector3(-187.797f, -2002.496f, 27.62042f), 79.03901f),
                       new SpawnPoint(new Vector3(-188.0032f, -2007.499f, 27.62042f), 82.81763f),
                       new SpawnPoint(new Vector3(-188.9685f, -2012.167f, 27.75075f), 42.66145f),
                       new SpawnPoint(new Vector3(-220.6706f, -1988.173f, 27.75543f), 182.9176f),
                       new SpawnPoint(new Vector3(-223.3832f, -2009.26f, 27.75544f), 336.2667f),
                       new SpawnPoint(new Vector3(-230.1917f, -1991.345f, 29.94604f), 242.9984f),
                       new SpawnPoint(new Vector3(-230.895f, -1995.257f, 29.94604f), 254.6015f),
                       new SpawnPoint(new Vector3(-231.673f, -2001.194f, 29.94604f), 280.2846f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-186.2769f, -2000.507f, 27.75542f), 78.3184f),
                       new SpawnPoint(new Vector3(-186.2531f, -2003.306f, 27.61233f), 82.13985f),
                       new SpawnPoint(new Vector3(-186.6079f, -2006.635f, 27.60295f), 81.23599f),
                       new SpawnPoint(new Vector3(-187.1649f, -2010.236f, 27.74389f), 80.0511f),
                       new SpawnPoint(new Vector3(-186.2923f, -1995.55f, 27.75028f), 121.5277f),
                       new SpawnPoint(new Vector3(-187.6597f, -1993.091f, 27.62041f), 128.9751f),
                       new SpawnPoint(new Vector3(-189.3102f, -1990.912f, 27.62039f), 130.155f),
                       new SpawnPoint(new Vector3(-191.0048f, -1988.667f, 27.62041f), 153.3309f),
                       new SpawnPoint(new Vector3(-194.2164f, -1987.321f, 27.62041f), 147.7469f),
                       new SpawnPoint(new Vector3(-197.0177f, -1985.666f, 27.62041f), 162.3736f),
                       new SpawnPoint(new Vector3(-200.1343f, -1984.33f, 27.62041f), 191.095f),
                       new SpawnPoint(new Vector3(-202.6701f, -1983.993f, 27.62041f), 170.0447f),
                       new SpawnPoint(new Vector3(-206.0715f, -1983.098f, 27.62041f), 162.3363f),
                       new SpawnPoint(new Vector3(-189.174f, -2014.072f, 27.75788f), 32.56069f),
                       new SpawnPoint(new Vector3(-193.0629f, -2015.318f, 27.62042f), 22.28253f),
                       new SpawnPoint(new Vector3(-197.0345f, -2017.256f, 27.62042f), 9.547528f),
                       new SpawnPoint(new Vector3(-200.6218f, -2017.943f, 27.62042f), 356.9887f),
                       new SpawnPoint(new Vector3(-203.9736f, -2017.755f, 27.62041f), 351.7933f),
                       new SpawnPoint(new Vector3(-208.0893f, -2017.638f, 27.62041f), 342.5891f),
                       new SpawnPoint(new Vector3(-211.7391f, -2016.806f, 27.62041f), 332.3945f),
                       new SpawnPoint(new Vector3(-220.3211f, -2007.436f, 27.75541f), 342.3142f),
                       new SpawnPoint(new Vector3(-223.3477f, -2006.515f, 27.75541f), 341.8715f),
                       new SpawnPoint(new Vector3(-221.5999f, -1989.444f, 27.75543f), 183.1904f),
                       new SpawnPoint(new Vector3(-218.4856f, -1989.262f, 27.75543f), 183.1665f),
                       new SpawnPoint(new Vector3(-230.4289f, -2003.931f, 29.94606f), 256.3372f),
                       new SpawnPoint(new Vector3(-229.9963f, -2000.78f, 29.94604f), 260.8306f),
                       new SpawnPoint(new Vector3(-229.5854f, -1997.195f, 29.94604f), 263.9594f),
                       new SpawnPoint(new Vector3(-229.2566f, -1994.373f, 29.94604f), 283.074f),
                       new SpawnPoint(new Vector3(-228.8051f, -1991.398f, 29.94604f), 277.1746f),
                   }
               ),


            new DemonstrationSpawn
               (
                   new SpawnPoint(new Vector3(1849.39f, 3652.46f, 34.12f), 295.82f),
                   new SpawnPoint(new Vector3(1851.43f, 3655.62f, 34.17f), 81.7f),
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(1863.769f, 3670.661f, 33.88819f), 29.94298f),
                       new SpawnPoint(new Vector3(1861.821f, 3672.163f, 33.8862f), 113.5691f),
                       new SpawnPoint(new Vector3(1860.136f, 3670.832f, 33.92746f), 174.1599f),
                       new SpawnPoint(new Vector3(1860.032f, 3667.606f, 33.97652f), 204.485f),
                       new SpawnPoint(new Vector3(1863.387f, 3665.368f, 33.93288f), 273.446f),
                       new SpawnPoint(new Vector3(1866.256f, 3667.502f, 33.86279f), 3.823329f),
                       new SpawnPoint(new Vector3(1866.101f, 3671.836f, 33.8228f), 14.90917f),
                       new SpawnPoint(new Vector3(1865.201f, 3674.418f, 33.74585f), 24.00767f),
                       new SpawnPoint(new Vector3(1866.72f, 3677.316f, 33.63698f), 253.767f),
                       new SpawnPoint(new Vector3(1870.328f, 3676.365f, 33.68481f), 236.7634f),
                       new SpawnPoint(new Vector3(1870.62f, 3671.057f, 33.77471f), 143.5087f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(1880.455f, 3670.552f, 33.4925f), 115.0563f),
                       new SpawnPoint(new Vector3(1878.216f, 3675.782f, 33.59385f), 113.0655f),
                       new SpawnPoint(new Vector3(1874.86f, 3684.072f, 33.46976f), 139.9587f),
                       new SpawnPoint(new Vector3(1855.722f, 3682.261f, 34.26752f), 211.5938f),
                       new SpawnPoint(new Vector3(1854.703f, 3657.322f, 34.11756f), 308.6372f),
                       new SpawnPoint(new Vector3(1850.684f, 3663.43f, 34.1456f), 299.9738f),
                       new SpawnPoint(new Vector3(1848.015f, 3668.697f, 33.74445f), 297.3994f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(1870.76f, 3687.788f, 33.7021f), 169.8572f),
                       new SpawnPoint(new Vector3(1873.601f, 3685.998f, 33.58517f), 167.84f),
                       new SpawnPoint(new Vector3(1876.575f, 3683.917f, 33.39678f), 123.7607f),
                       new SpawnPoint(new Vector3(1878.119f, 3680.168f, 33.53139f), 114.3513f),
                       new SpawnPoint(new Vector3(1879.337f, 3676.421f, 33.56717f), 116.9357f),
                       new SpawnPoint(new Vector3(1880.343f, 3673.559f, 33.55283f), 126.2109f),
                       new SpawnPoint(new Vector3(1881.426f, 3670.815f, 33.4537f), 110.9581f),
                       new SpawnPoint(new Vector3(1856.548f, 3680.763f, 34.13387f), 213.259f),
                       new SpawnPoint(new Vector3(1843.865f, 3672.067f, 33.68f), 304.8364f),
                       new SpawnPoint(new Vector3(1845.997f, 3668.891f, 33.68627f), 298.0322f),
                       new SpawnPoint(new Vector3(1847.665f, 3665.89f, 34.04644f), 296.2897f),
                       new SpawnPoint(new Vector3(1849.519f, 3662.425f, 34.16813f), 301.2576f),
                       new SpawnPoint(new Vector3(1851.283f, 3659.948f, 34.17668f), 298.9954f),
                       new SpawnPoint(new Vector3(1852.661f, 3657.736f, 34.15123f), 300.6812f),
                       new SpawnPoint(new Vector3(1854.702f, 3655.301f, 34.03717f), 310.8134f),
                   }
               ),


            new DemonstrationSpawn
               (
                   new SpawnPoint(new Vector3(-1068.234f, -2701.066f, 13.68679f), 228.166f),
                   new SpawnPoint(new Vector3(-1071.809f, -2697.293f, 13.70532f), 232.9962f),
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-1051.223f, -2698.368f, 13.8181f), 301.8253f),
                       new SpawnPoint(new Vector3(-1049.017f, -2696.619f, 13.74645f), 318.7805f),
                       new SpawnPoint(new Vector3(-1046.516f, -2694.017f, 13.71594f), 299.712f),
                       new SpawnPoint(new Vector3(-1042.587f, -2692.743f, 13.5825f), 275.5289f),
                       new SpawnPoint(new Vector3(-1038.565f, -2692.872f, 13.59626f), 270.1721f),
                       new SpawnPoint(new Vector3(-1035.564f, -2693.706f, 13.65006f), 247.5862f),
                       new SpawnPoint(new Vector3(-1031.895f, -2695.251f, 13.80924f), 232.6555f),
                       new SpawnPoint(new Vector3(-1028.785f, -2698.331f, 13.57897f), 211.6449f),
                       new SpawnPoint(new Vector3(-1026.93f, -2701.043f, 13.55388f), 219.2373f),
                       new SpawnPoint(new Vector3(-1024.422f, -2703.777f, 13.58364f), 223.7642f),
                       new SpawnPoint(new Vector3(-1021.144f, -2706.671f, 13.6599f), 216.3152f),
                       new SpawnPoint(new Vector3(-1020.19f, -2708.152f, 13.6471f), 154.5038f),
                       new SpawnPoint(new Vector3(-1021.363f, -2708.285f, 13.72888f), 125.5157f),
                       new SpawnPoint(new Vector3(-1025.093f, -2709.981f, 13.78511f), 108.0308f),
                       new SpawnPoint(new Vector3(-1028.721f, -2709.42f, 13.7803f), 67.4427f),
                       new SpawnPoint(new Vector3(-1031.883f, -2706.917f, 13.7792f), 30.58269f),
                       new SpawnPoint(new Vector3(-1032.725f, -2702.329f, 13.74882f), 15.23339f),
                       new SpawnPoint(new Vector3(-1036.416f, -2697.695f, 13.66136f), 75.35214f),
                       new SpawnPoint(new Vector3(-1040.387f, -2695.944f, 13.67413f), 90.40205f),
                       new SpawnPoint(new Vector3(-1044.602f, -2697.459f, 13.78761f), 136.8506f),
                       new SpawnPoint(new Vector3(-1043.995f, -2703.346f, 13.81757f), 231.4583f),
                       new SpawnPoint(new Vector3(-1036.592f, -2706.073f, 13.81726f), 265.5997f),
                       new SpawnPoint(new Vector3(-1033.185f, -2713.394f, 13.81814f), 182.9526f),
                       new SpawnPoint(new Vector3(-1029.559f, -2718.385f, 13.80955f), 276.8981f),
                       new SpawnPoint(new Vector3(-1025.176f, -2716.135f, 13.80608f), 308.1021f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-1009.187f, -2707.278f, 13.76918f), 151.2855f),
                       new SpawnPoint(new Vector3(-1005.971f, -2708.967f, 13.79089f), 150.8187f),
                       new SpawnPoint(new Vector3(-1022.575f, -2731.807f, 13.75663f), 32.87785f),
                       new SpawnPoint(new Vector3(-1031.777f, -2692.665f, 13.82977f), 137.4573f),
                       new SpawnPoint(new Vector3(-1058.644f, -2682.168f, 13.67598f), 219.1861f),
                       new SpawnPoint(new Vector3(-1063.909f, -2685.161f, 13.81072f), 223.8775f),
                       new SpawnPoint(new Vector3(-1067.062f, -2688.579f, 13.8181f), 230.5371f),
                       new SpawnPoint(new Vector3(-1070.406f, -2692.269f, 13.80493f), 247.4668f),
                       new SpawnPoint(new Vector3(-1046.374f, -2721.082f, 13.75664f), 335.2363f),
                       new SpawnPoint(new Vector3(-1039.679f, -2724.483f, 13.75664f), 332.0488f),
                       new SpawnPoint(new Vector3(-1024.605f, -2730.997f, 13.75664f), 76.08624f),
                       new SpawnPoint(new Vector3(-1039.96f, -2741.475f, 13.90151f), 327.2997f),
                       new SpawnPoint(new Vector3(-1018.02f, -2697.104f, 13.9803f), 147.8848f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-1060.476f, -2679.883f, 13.66616f), 35.4282f),
                       new SpawnPoint(new Vector3(-1063.226f, -2681.66f, 13.77806f), 39.6836f),
                       new SpawnPoint(new Vector3(-1065.728f, -2683.998f, 13.81434f), 40.66214f),
                       new SpawnPoint(new Vector3(-1068.067f, -2685.74f, 13.8181f), 44.42037f),
                       new SpawnPoint(new Vector3(-1070.481f, -2688.179f, 13.8181f), 43.22093f),
                       new SpawnPoint(new Vector3(-1073.164f, -2691.25f, 13.94778f), 42.28749f),
                       new SpawnPoint(new Vector3(-1075.471f, -2692.834f, 13.72104f), 36.95736f),
                       new SpawnPoint(new Vector3(-1077.983f, -2694.487f, 13.7566f), 32.45769f),
                       new SpawnPoint(new Vector3(-1079.753f, -2696.295f, 13.75663f), 43.94822f),
                       new SpawnPoint(new Vector3(-1081.706f, -2698.164f, 13.75663f), 32.02998f),
                       new SpawnPoint(new Vector3(-1083.935f, -2700.248f, 13.75663f), 29.65316f),
                       new SpawnPoint(new Vector3(-1086.192f, -2702.641f, 13.87317f), 45.21233f),
                       new SpawnPoint(new Vector3(-1088.137f, -2705.294f, 14.10391f), 47.35549f),
                       new SpawnPoint(new Vector3(-1009.193f, -2705.726f, 13.79082f), 327.3674f),
                       new SpawnPoint(new Vector3(-1004.786f, -2708.252f, 13.76593f), 327.4135f),
                       new SpawnPoint(new Vector3(-1006.958f, -2706.985f, 13.82981f), 333.241f),
                       new SpawnPoint(new Vector3(-1005.096f, -2715.666f, 13.66639f), 248.2796f),
                       new SpawnPoint(new Vector3(-1007.09f, -2718.856f, 13.78779f), 235.7447f),
                       new SpawnPoint(new Vector3(-1009.011f, -2721.347f, 13.81583f), 230.9262f),
                       new SpawnPoint(new Vector3(-1011.534f, -2723.436f, 13.8181f), 228.2197f),
                       new SpawnPoint(new Vector3(-1014.423f, -2725.902f, 13.8181f), 213.7817f),
                       new SpawnPoint(new Vector3(-1017.787f, -2728.282f, 13.7852f), 214.1283f),
                       new SpawnPoint(new Vector3(-1020.784f, -2730.74f, 13.66607f), 219.9528f),
                       new SpawnPoint(new Vector3(-1039.092f, -2743.069f, 13.92541f), 325.916f),
                       new SpawnPoint(new Vector3(-1041.466f, -2741.924f, 13.93046f), 331.0538f),
                       new SpawnPoint(new Vector3(-1027.617f, -2693.751f, 13.81865f), 334.9142f),
                       new SpawnPoint(new Vector3(-1030.161f, -2692.578f, 13.79966f), 328.8084f),
                       new SpawnPoint(new Vector3(-1033.102f, -2690.666f, 13.81833f), 317.7942f),
                       new SpawnPoint(new Vector3(-1016.596f, -2697.957f, 13.9836f), 153.3891f),
                       new SpawnPoint(new Vector3(-1018.59f, -2695.836f, 13.98265f), 146.6696f),
                   }
               ),


            new DemonstrationSpawn
               (
                   new SpawnPoint(new Vector3(-859.9564f, -160.7957f, 37.96943f), 23.73507f),
                   new SpawnPoint(new Vector3(-859.4442f, -162.7749f, 37.90058f), 295.3745f),
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-869.6453f, -124.8564f, 37.93447f), 35.89096f),
                       new SpawnPoint(new Vector3(-871.3318f, -124.6503f, 37.95089f), 192.5693f),
                       new SpawnPoint(new Vector3(-869.5718f, -128.6172f, 37.95929f), 176.911f),
                       new SpawnPoint(new Vector3(-872.122f, -132.9066f, 37.97584f), 127.9991f),
                       new SpawnPoint(new Vector3(-874.5174f, -135.6745f, 37.93827f), 165.6411f),
                       new SpawnPoint(new Vector3(-876.4261f, -139.0976f, 37.96532f), 107.9658f),
                       new SpawnPoint(new Vector3(-880.0952f, -137.9786f, 37.90468f), 42.64764f),
                       new SpawnPoint(new Vector3(-883.5823f, -134.8711f, 37.83149f), 31.28144f),
                       new SpawnPoint(new Vector3(-884.6543f, -130.6787f, 37.88575f), 341.2733f),
                       new SpawnPoint(new Vector3(-882.8676f, -128.0572f, 37.93213f), 311.5691f),
                       new SpawnPoint(new Vector3(-879.4948f, -125.2925f, 37.93608f), 308.9913f),
                       new SpawnPoint(new Vector3(-876.8294f, -122.8856f, 37.91781f), 321.6975f),
                       new SpawnPoint(new Vector3(-874.4273f, -119.4801f, 37.92648f), 325.1976f),
                       new SpawnPoint(new Vector3(-872.8852f, -115.3564f, 37.96659f), 355.3868f),
                       new SpawnPoint(new Vector3(-873.1026f, -110.9946f, 37.95527f), 9.361859f),
                       new SpawnPoint(new Vector3(-874.3203f, -107.7045f, 37.93913f), 49.94761f),
                       new SpawnPoint(new Vector3(-877.0671f, -109.634f, 37.96063f), 156.7f),
                       new SpawnPoint(new Vector3(-878.499f, -113.6963f, 37.94532f), 158.5619f),
                       new SpawnPoint(new Vector3(-880.2311f, -117.897f, 37.96088f), 144.6594f),
                       new SpawnPoint(new Vector3(-883.365f, -120.8955f, 37.92368f), 127.2769f),
                       new SpawnPoint(new Vector3(-888.1102f, -122.4133f, 37.96951f), 81.00019f),
                       new SpawnPoint(new Vector3(-891.6493f, -119.8737f, 37.95771f), 25.68211f),
                       new SpawnPoint(new Vector3(-891.2631f, -115.1302f, 37.97071f), 327.8353f),
                       new SpawnPoint(new Vector3(-887.0197f, -111.4852f, 37.95422f), 297.5931f),
                       new SpawnPoint(new Vector3(-881.2155f, -108.6004f, 37.95714f), 300.7043f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-880.2131f, -97.18749f, 37.83112f), 203.562f),
                       new SpawnPoint(new Vector3(-887.3519f, -100.6197f, 37.94862f), 203.9494f),
                       new SpawnPoint(new Vector3(-896.3052f, -105.0186f, 37.92086f), 205.2029f),
                       new SpawnPoint(new Vector3(-898.3087f, -116.7269f, 37.96803f), 299.9489f),
                       new SpawnPoint(new Vector3(-892.4568f, -126.0644f, 37.97486f), 300.3347f),
                       new SpawnPoint(new Vector3(-868.4583f, -97.69872f, 37.85798f), 110.9243f),
                       new SpawnPoint(new Vector3(-865.0942f, -105.3281f, 37.96661f), 113.3108f),
                       new SpawnPoint(new Vector3(-862.3135f, -110.9935f, 37.95604f), 117.1632f),
                       new SpawnPoint(new Vector3(-851.3653f, -125.7389f, 37.61986f), 113.1572f),
                       new SpawnPoint(new Vector3(-849.5636f, -148.4586f, 37.73494f), 75.20413f),
                       new SpawnPoint(new Vector3(-872.8907f, -159.308f, 37.67494f), 7.927553f),
                       new SpawnPoint(new Vector3(-884.2585f, -142.0271f, 37.90861f), 287.956f),
                       new SpawnPoint(new Vector3(-850.8181f, -161.8426f, 37.8447f), 23.44151f),
                       new SpawnPoint(new Vector3(-862.8326f, -168.661f, 37.86113f), 29.16376f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-891.3881f, -131.0951f, 37.81744f), 115.594f),
                       new SpawnPoint(new Vector3(-893.6023f, -127.3334f, 37.962f), 122.5186f),
                       new SpawnPoint(new Vector3(-895.3818f, -124.2595f, 37.97451f), 115.1723f),
                       new SpawnPoint(new Vector3(-897.1336f, -121.3359f, 37.97455f), 115.7855f),
                       new SpawnPoint(new Vector3(-898.8707f, -118.4792f, 37.97446f), 116.3374f),
                       new SpawnPoint(new Vector3(-900.8683f, -115.4968f, 37.91592f), 122.6872f),
                       new SpawnPoint(new Vector3(-900.1586f, -104.9639f, 37.80007f), 28.52667f),
                       new SpawnPoint(new Vector3(-896.633f, -103.16f, 37.91982f), 23.83512f),
                       new SpawnPoint(new Vector3(-893.5775f, -101.6352f, 37.93981f), 29.36631f),
                       new SpawnPoint(new Vector3(-889.9876f, -99.866f, 37.93365f), 25.02391f),
                       new SpawnPoint(new Vector3(-886.2987f, -97.92921f, 37.93266f), 23.23673f),
                       new SpawnPoint(new Vector3(-882.951f, -96.42991f, 37.86589f), 30.06923f),
                       new SpawnPoint(new Vector3(-867.2435f, -96.3996f, 37.82745f), 295.7683f),
                       new SpawnPoint(new Vector3(-865.4646f, -100.0098f, 37.95848f), 295.9795f),
                       new SpawnPoint(new Vector3(-863.6684f, -104.3063f, 37.96188f), 298.5573f),
                       new SpawnPoint(new Vector3(-861.6394f, -107.2487f, 37.962f), 291.7267f),
                       new SpawnPoint(new Vector3(-860.2211f, -110.8676f, 37.93264f), 298.8516f),
                       new SpawnPoint(new Vector3(-847.4372f, -147.5813f, 37.75887f), 297.0665f),
                       new SpawnPoint(new Vector3(-845.067f, -152.0902f, 37.84971f), 297.0062f),
                       new SpawnPoint(new Vector3(-848.0649f, -162.7811f, 37.71802f), 212.073f),
                       new SpawnPoint(new Vector3(-851.2274f, -164.7354f, 37.90324f), 203.7017f),
                       new SpawnPoint(new Vector3(-855.1328f, -166.3096f, 37.89122f), 209.6232f),
                       new SpawnPoint(new Vector3(-858.3616f, -168.4724f, 37.89169f), 208.7789f),
                       new SpawnPoint(new Vector3(-861.9405f, -170.6664f, 37.86783f), 206.7836f),
                       new SpawnPoint(new Vector3(-871.8089f, -167.9159f, 37.77697f), 116.8985f),
                       new SpawnPoint(new Vector3(-874.5198f, -163.9431f, 37.7845f), 117.3336f),
                       new SpawnPoint(new Vector3(-851.2224f, -123.8569f, 37.63912f), 113.5125f),
                       new SpawnPoint(new Vector3(-849.3983f, -126.6047f, 37.55997f), 115.4325f),
                       new SpawnPoint(new Vector3(-883.6548f, -145.5832f, 37.93048f), 300.994f),
                       new SpawnPoint(new Vector3(-886.3746f, -139.8417f, 37.94837f), 292.3376f),
                   }
               ),


            new DemonstrationSpawn
               (
                   new SpawnPoint(new Vector3(-2157.376f, -325.7636f, 13.04354f), 112.9413f),
                   new SpawnPoint(new Vector3(-2154.854f, -326.098f, 13.15376f), 162.2646f),
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-2187.388f, -342.4048f, 13.21092f), 266.7718f),
                       new SpawnPoint(new Vector3(-2184.137f, -342.6575f, 13.14451f), 266.4654f),
                       new SpawnPoint(new Vector3(-2180.99f, -342.6167f, 13.15446f), 273.0675f),
                       new SpawnPoint(new Vector3(-2179.167f, -342.474f, 13.03154f), 276.5886f),
                       new SpawnPoint(new Vector3(-2174.146f, -341.9524f, 13.17064f), 277.3995f),
                       new SpawnPoint(new Vector3(-2171.088f, -341.4191f, 13.13445f), 276.0532f),
                       new SpawnPoint(new Vector3(-2167.375f, -342.0444f, 13.1632f), 239.4812f),
                       new SpawnPoint(new Vector3(-2165.036f, -344.9563f, 13.15152f), 191.8293f),
                       new SpawnPoint(new Vector3(-2164.331f, -348.4409f, 13.14731f), 207.1439f),
                       new SpawnPoint(new Vector3(-2162.873f, -351.4105f, 13.12113f), 194.5365f),
                       new SpawnPoint(new Vector3(-2163.473f, -355.1237f, 13.13797f), 139.4013f),
                       new SpawnPoint(new Vector3(-2167.68f, -357.5638f, 13.14344f), 95.12186f),
                       new SpawnPoint(new Vector3(-2171.173f, -355.6242f, 13.14497f), 44.11245f),
                       new SpawnPoint(new Vector3(-2173.074f, -351.7131f, 13.14489f), 4.775362f),
                       new SpawnPoint(new Vector3(-2172.984f, -347.7545f, 13.1409f), 8.833803f),
                       new SpawnPoint(new Vector3(-2174.837f, -345.1463f, 13.13862f), 53.57795f),
                       new SpawnPoint(new Vector3(-2178.01f, -344.0805f, 13.13665f), 93.98047f),
                       new SpawnPoint(new Vector3(-2181.063f, -345.1604f, 13.13712f), 121.6667f),
                       new SpawnPoint(new Vector3(-2183.636f, -347.6174f, 13.14114f), 149.6432f),
                       new SpawnPoint(new Vector3(-2185.612f, -351.6093f, 13.15805f), 143.8939f),
                       new SpawnPoint(new Vector3(-2190.516f, -352.7542f, 13.18991f), 54.15552f),
                       new SpawnPoint(new Vector3(-2191.942f, -348.7565f, 13.20354f), 337.4638f),
                       new SpawnPoint(new Vector3(-2188.63f, -344.9242f, 13.20996f), 309.1482f),
                       new SpawnPoint(new Vector3(-2162.397f, -340.7852f, 13.18593f), 139.1014f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-2179.238f, -327.3327f, 13.11138f), 169.0527f),
                       new SpawnPoint(new Vector3(-2172.339f, -327.9568f, 13.1766f), 171.6501f),
                       new SpawnPoint(new Vector3(-2165.509f, -329.7085f, 13.19061f), 177.0582f),
                       new SpawnPoint(new Vector3(-2155.329f, -334.0883f, 13.11502f), 118.25f),
                       new SpawnPoint(new Vector3(-2152.424f, -343.8017f, 13.21054f), 77.68647f),
                       new SpawnPoint(new Vector3(-2155.548f, -355.6142f, 13.18162f), 69.81168f),
                       new SpawnPoint(new Vector3(-2159.802f, -365.2679f, 12.94976f), 28.22777f),
                       new SpawnPoint(new Vector3(-2215.742f, -357.2591f, 13.12848f), 259.9494f),
                       new SpawnPoint(new Vector3(-2214.968f, -348.3726f, 13.34071f), 268.9339f),
                       new SpawnPoint(new Vector3(-2214.479f, -341.8015f, 13.35453f), 265.8049f),
                       new SpawnPoint(new Vector3(-2213.526f, -332.8854f, 13.2645f), 268.4235f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-2178.331f, -325.3505f, 13.06746f), 346.0456f),
                       new SpawnPoint(new Vector3(-2173.42f, -326.4604f, 13.15999f), 350.3216f),
                       new SpawnPoint(new Vector3(-2168.508f, -327.3649f, 13.17105f), 350.6579f),
                       new SpawnPoint(new Vector3(-2164.098f, -328.4747f, 13.18267f), 351.5441f),
                       new SpawnPoint(new Vector3(-2159.244f, -328.894f, 13.09824f), 345.1938f),
                       new SpawnPoint(new Vector3(-2150.5f, -339.6446f, 13.20372f), 250.6651f),
                       new SpawnPoint(new Vector3(-2151.495f, -343.4033f, 13.21286f), 251.8734f),
                       new SpawnPoint(new Vector3(-2152.437f, -346.8095f, 13.20439f), 263.5552f),
                       new SpawnPoint(new Vector3(-2153.123f, -350.8652f, 13.19501f), 254.4016f),
                       new SpawnPoint(new Vector3(-2154.492f, -355.683f, 13.18313f), 255.4445f),
                       new SpawnPoint(new Vector3(-2155.81f, -360.3063f, 13.17171f), 254.3303f),
                       new SpawnPoint(new Vector3(-2156.912f, -365.079f, 12.99409f), 258.0347f),
                       new SpawnPoint(new Vector3(-2216.175f, -356.6192f, 13.1685f), 82.3817f),
                       new SpawnPoint(new Vector3(-2217.425f, -352.8673f, 13.33539f), 88.86289f),
                       new SpawnPoint(new Vector3(-2217.153f, -348.7555f, 13.36145f), 83.58617f),
                       new SpawnPoint(new Vector3(-2216.776f, -344.2909f, 13.37145f), 82.63287f),
                       new SpawnPoint(new Vector3(-2216.399f, -340.674f, 13.36888f), 85.39821f),
                       new SpawnPoint(new Vector3(-2216.256f, -336.1933f, 13.36929f), 82.63505f),
                       new SpawnPoint(new Vector3(-2215.721f, -331.6572f, 13.20411f), 85.12634f),
                   }
               ),


            new DemonstrationSpawn
               (
                   new SpawnPoint(new Vector3(-404.7824f, 6011.367f, 31.50809f), 25.75595f),
                   new SpawnPoint(new Vector3(-405.5533f, 6007.256f, 31.57928f), 359.1183f),
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-413.2715f, 6020.455f, 31.47065f), 58.87473f),
                       new SpawnPoint(new Vector3(-415.3916f, 6022.167f, 31.40784f), 61.56553f),
                       new SpawnPoint(new Vector3(-417.2393f, 6024.665f, 31.36911f), 42.01382f),
                       new SpawnPoint(new Vector3(-418.8384f, 6028.099f, 31.3758f), 39.24468f),
                       new SpawnPoint(new Vector3(-421.8551f, 6030.972f, 31.34354f), 38.49681f),
                       new SpawnPoint(new Vector3(-424.6846f, 6033.783f, 31.28941f), 40.21727f),
                       new SpawnPoint(new Vector3(-428.2608f, 6036.738f, 31.2637f), 30.83075f),
                       new SpawnPoint(new Vector3(-429.2199f, 6040.178f, 31.22799f), 13.16793f),
                       new SpawnPoint(new Vector3(-429.2632f, 6045.784f, 31.36023f), 354.3864f),
                       new SpawnPoint(new Vector3(-430.537f, 6048.236f, 31.34616f), 28.03854f),
                       new SpawnPoint(new Vector3(-431.885f, 6050.801f, 31.38338f), 344.3614f),
                       new SpawnPoint(new Vector3(-430.424f, 6052.493f, 31.40744f), 296.721f),
                       new SpawnPoint(new Vector3(-427.0396f, 6054.297f, 31.4814f), 291.3555f),
                       new SpawnPoint(new Vector3(-424.1404f, 6054.077f, 31.40616f), 225.6371f),
                       new SpawnPoint(new Vector3(-423.1952f, 6050.769f, 31.42444f), 172.3385f),
                       new SpawnPoint(new Vector3(-423.1351f, 6045.955f, 31.3581f), 189.3706f),
                       new SpawnPoint(new Vector3(-422.7099f, 6041.803f, 31.28932f), 190.6561f),
                       new SpawnPoint(new Vector3(-420.8582f, 6037.581f, 31.32649f), 202.1779f),
                       new SpawnPoint(new Vector3(-418.2525f, 6034.159f, 31.28883f), 212.8704f),
                       new SpawnPoint(new Vector3(-414.7176f, 6030.335f, 31.2589f), 213.9228f),
                       new SpawnPoint(new Vector3(-412.0536f, 6026.401f, 31.29496f), 207.3326f),
                       new SpawnPoint(new Vector3(-409.2716f, 6021.655f, 31.36604f), 218.6425f),
                       new SpawnPoint(new Vector3(-405.9003f, 6022.925f, 31.273f), 359.9898f),
                       new SpawnPoint(new Vector3(-405.7372f, 6028.136f, 31.21581f), 9.533973f),
                       new SpawnPoint(new Vector3(-410.3603f, 6033.448f, 31.26156f), 36.08694f),
                       new SpawnPoint(new Vector3(-414.9319f, 6039.257f, 31.3329f), 21.29747f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-452.0027f, 6027.515f, 31.34055f), 303.3492f),
                       new SpawnPoint(new Vector3(-454.5273f, 6030.626f, 31.34055f), 306.6631f),
                       new SpawnPoint(new Vector3(-442.8625f, 6081.82f, 31.3582f), 169.339f),
                       new SpawnPoint(new Vector3(-437.737f, 6081.186f, 31.30537f), 178.2774f),
                       new SpawnPoint(new Vector3(-431.4466f, 6081.213f, 31.44714f), 181.2626f),
                       new SpawnPoint(new Vector3(-424.9212f, 6080.957f, 31.23985f), 180.1857f),
                       new SpawnPoint(new Vector3(-395.6963f, 6016.42f, 31.2847f), 35.34209f),
                       new SpawnPoint(new Vector3(-391.796f, 6021.241f, 31.48179f), 43.24233f),
                       new SpawnPoint(new Vector3(-386.9276f, 6026.639f, 31.61133f), 34.80069f),
                       new SpawnPoint(new Vector3(-400.0576f, 6010.346f, 31.65855f), 42.73357f),
                       new SpawnPoint(new Vector3(-417.6177f, 6003.352f, 31.52228f), 1.042644f),
                       new SpawnPoint(new Vector3(-411.8575f, 6004.283f, 31.59145f), 1.868564f),
                       new SpawnPoint(new Vector3(-440.093f, 6019.477f, 31.49013f), 313.6013f),
                       new SpawnPoint(new Vector3(-448.4293f, 6073.339f, 31.38744f), 269.2969f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-454.0319f, 6029.158f, 31.34055f), 308.7563f),
                       new SpawnPoint(new Vector3(-452.9721f, 6027.154f, 31.34055f), 310.6669f),
                       new SpawnPoint(new Vector3(-425.3651f, 6082.587f, 31.23114f), 183.0556f),
                       new SpawnPoint(new Vector3(-430.1242f, 6082.666f, 31.38953f), 186.2083f),
                       new SpawnPoint(new Vector3(-434.4521f, 6082.566f, 31.43857f), 180.1868f),
                       new SpawnPoint(new Vector3(-438.6128f, 6082.549f, 31.29737f), 176.8661f),
                       new SpawnPoint(new Vector3(-441.8669f, 6082.849f, 31.36079f), 173.1449f),
                       new SpawnPoint(new Vector3(-444.5242f, 6083.125f, 31.47046f), 167.3905f),
                       new SpawnPoint(new Vector3(-388.7236f, 6023.057f, 31.44731f), 222.6373f),
                       new SpawnPoint(new Vector3(-391.0382f, 6020.44f, 31.49405f), 37.25472f),
                       new SpawnPoint(new Vector3(-393.9663f, 6017.253f, 31.3807f), 47.86916f),
                       new SpawnPoint(new Vector3(-396.6924f, 6013.682f, 31.34017f), 61.05246f),
                       new SpawnPoint(new Vector3(-386.3993f, 6025.598f, 31.61298f), 236.8849f),
                       new SpawnPoint(new Vector3(-384.4978f, 6027.443f, 31.63871f), 224.7072f),
                       new SpawnPoint(new Vector3(-409.0986f, 6002.499f, 31.6368f), 352.5547f),
                       new SpawnPoint(new Vector3(-412.748f, 6002.226f, 31.57255f), 0.09999922f),
                       new SpawnPoint(new Vector3(-417.7263f, 6001.705f, 31.52612f), 353.8369f),
                       new SpawnPoint(new Vector3(-440.7894f, 6018.821f, 31.49825f), 315.2404f),
                       new SpawnPoint(new Vector3(-448.7999f, 6069.219f, 31.39025f), 269.5816f),
                       new SpawnPoint(new Vector3(-449.5422f, 6073.215f, 31.38943f), 278.3502f),
                       new SpawnPoint(new Vector3(-449.4172f, 6076.758f, 31.36615f), 262.9354f),
                   }
               ),


            new DemonstrationSpawn
               (
                   new SpawnPoint(new Vector3(108.6681f, 6504.904f, 31.32797f), 163.5022f),
                   new SpawnPoint(new Vector3(112.1094f, 6506.896f, 31.39203f), 134.7219f),
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(109.5226f, 6498.707f, 31.46083f), 233.4723f),
                       new SpawnPoint(new Vector3(112.7898f, 6496.897f, 31.37815f), 229.212f),
                       new SpawnPoint(new Vector3(115.4504f, 6493.193f, 31.39684f), 199.3945f),
                       new SpawnPoint(new Vector3(116.5576f, 6488.374f, 31.34536f), 183.2331f),
                       new SpawnPoint(new Vector3(115.7439f, 6484.267f, 31.34986f), 158.665f),
                       new SpawnPoint(new Vector3(113.2242f, 6481.245f, 31.3511f), 99.80386f),
                       new SpawnPoint(new Vector3(110.2667f, 6483.922f, 31.33461f), 17.47429f),
                       new SpawnPoint(new Vector3(110.064f, 6488.11f, 31.36445f), 5.718596f),
                       new SpawnPoint(new Vector3(107.2347f, 6493.033f, 31.40833f), 65.33159f),
                       new SpawnPoint(new Vector3(103.0068f, 6492.632f, 31.41814f), 120.515f),
                       new SpawnPoint(new Vector3(100.2336f, 6489.825f, 31.38943f), 139.1404f),
                       new SpawnPoint(new Vector3(100.1897f, 6486.391f, 31.39099f), 250.73f),
                       new SpawnPoint(new Vector3(104.4215f, 6484.765f, 31.34962f), 235.254f),
                       new SpawnPoint(new Vector3(107.5437f, 6481.549f, 31.33312f), 211.4991f),
                       new SpawnPoint(new Vector3(109.1592f, 6477.482f, 31.34386f), 184.6081f),
                       new SpawnPoint(new Vector3(108.5269f, 6474.102f, 31.30273f), 148.7699f),
                       new SpawnPoint(new Vector3(105.7134f, 6471.272f, 31.32263f), 118.488f),
                       new SpawnPoint(new Vector3(102.6401f, 6471.28f, 31.32027f), 41.34328f),
                       new SpawnPoint(new Vector3(102.7145f, 6474.896f, 31.30688f), 348.8906f),
                       new SpawnPoint(new Vector3(103.08f, 6479.316f, 31.33341f), 2.788634f),
                       new SpawnPoint(new Vector3(101.0284f, 6483.036f, 31.36045f), 82.15087f),
                       new SpawnPoint(new Vector3(97.26588f, 6482.688f, 31.35162f), 79.62557f),
                       new SpawnPoint(new Vector3(93.2723f, 6480.493f, 31.40582f), 206.7434f),
                       new SpawnPoint(new Vector3(96.39698f, 6475.619f, 31.35151f), 219.7615f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(132.1741f, 6493.24f, 31.4136f), 134.1478f),
                       new SpawnPoint(new Vector3(128.7278f, 6496.878f, 31.44181f), 121.9618f),
                       new SpawnPoint(new Vector3(123.9285f, 6502.652f, 31.46151f), 130.872f),
                       new SpawnPoint(new Vector3(119.2229f, 6508.094f, 31.50627f), 129.9419f),
                       new SpawnPoint(new Vector3(98.50429f, 6450.395f, 31.35403f), 315.783f),
                       new SpawnPoint(new Vector3(93.68356f, 6456.098f, 31.31306f), 318.1833f),
                       new SpawnPoint(new Vector3(88.05531f, 6461.389f, 31.31448f), 316.0994f),
                       new SpawnPoint(new Vector3(82.69536f, 6466.291f, 31.33936f), 312.515f),
                       new SpawnPoint(new Vector3(77.31812f, 6472.1f, 31.40131f), 288.3182f),
                       new SpawnPoint(new Vector3(136.0285f, 6488.731f, 31.30335f), 106.5668f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(117.4163f, 6512.448f, 31.40874f), 311.0296f),
                       new SpawnPoint(new Vector3(120.344f, 6509.619f, 31.51503f), 308.3085f),
                       new SpawnPoint(new Vector3(122.4517f, 6506.688f, 31.47613f), 304.5812f),
                       new SpawnPoint(new Vector3(126.0549f, 6502.51f, 31.46145f), 310.4376f),
                       new SpawnPoint(new Vector3(129.2078f, 6498.998f, 31.44066f), 305.7795f),
                       new SpawnPoint(new Vector3(131.5353f, 6496.333f, 31.43547f), 307.8622f),
                       new SpawnPoint(new Vector3(134.2966f, 6493.244f, 31.41166f), 305.0616f),
                       new SpawnPoint(new Vector3(75.41985f, 6472.146f, 31.40383f), 122.1784f),
                       new SpawnPoint(new Vector3(77.93002f, 6469.105f, 31.3935f), 312.9915f),
                       new SpawnPoint(new Vector3(81.52843f, 6465.653f, 31.34521f), 313.7057f),
                       new SpawnPoint(new Vector3(85.00739f, 6462.2f, 31.33967f), 311.91f),
                       new SpawnPoint(new Vector3(87.78275f, 6459.383f, 31.31885f), 317.0174f),
                       new SpawnPoint(new Vector3(90.84454f, 6456.407f, 31.32842f), 305.994f),
                       new SpawnPoint(new Vector3(93.66614f, 6454.164f, 31.31981f), 320.7986f),
                       new SpawnPoint(new Vector3(96.37669f, 6451.702f, 31.46758f), 318.0905f),
                       new SpawnPoint(new Vector3(136.6173f, 6490.264f, 31.33874f), 132.904f),
                   }
               ),


            new DemonstrationSpawn
               (
                   new SpawnPoint(new Vector3(-535.4867f, -139.1462f, 38.49355f), 120.6478f),
                   new SpawnPoint(new Vector3(-536.1093f, -137.6824f, 38.61147f), 110.9289f),
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-543.3497f, -146.2293f, 38.42762f), 104.3295f),
                       new SpawnPoint(new Vector3(-546.7789f, -147.2379f, 38.32582f), 133.5542f),
                       new SpawnPoint(new Vector3(-549.4993f, -152.3404f, 38.30219f), 166.432f),
                       new SpawnPoint(new Vector3(-553.4601f, -158.37f, 38.22802f), 121.4228f),
                       new SpawnPoint(new Vector3(-558.714f, -159.9031f, 38.16471f), 76.0466f),
                       new SpawnPoint(new Vector3(-557.7736f, -152.512f, 38.19732f), 319.2181f),
                       new SpawnPoint(new Vector3(-562.4178f, -153.2348f, 38.11868f), 143.5397f),
                       new SpawnPoint(new Vector3(-565.6887f, -161.1049f, 38.09121f), 182.1434f),
                       new SpawnPoint(new Vector3(-571.5016f, -163.2932f, 38.01649f), 41.90931f),
                       new SpawnPoint(new Vector3(-573.0749f, -156.1189f, 37.90938f), 293.9764f),
                       new SpawnPoint(new Vector3(-579.3661f, -158.7258f, 37.8724f), 137.3413f),
                       new SpawnPoint(new Vector3(-584.921f, -163.182f, 37.93738f), 135.3568f),
                       new SpawnPoint(new Vector3(-581.7789f, -164.2348f, 37.96258f), 281.5518f),
                       new SpawnPoint(new Vector3(-577.1948f, -164.0261f, 37.93034f), 266.7299f),
                       new SpawnPoint(new Vector3(-568.9927f, -157.5402f, 38.05513f), 299.7701f),
                       new SpawnPoint(new Vector3(-554.8773f, -146.8951f, 38.08408f), 300.8213f),
                       new SpawnPoint(new Vector3(-566.621f, -165.563f, 38.05911f), 110.9123f),
                       new SpawnPoint(new Vector3(-581.2032f, -169.3235f, 37.88371f), 101.5192f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-520.8372f, -145.4117f, 38.54924f), 101.5208f),
                       new SpawnPoint(new Vector3(-522.0755f, -141.4737f, 38.69037f), 113.031f),
                       new SpawnPoint(new Vector3(-523.683f, -137.9062f, 38.69287f), 118.7715f),
                       new SpawnPoint(new Vector3(-526.9545f, -134.8376f, 38.71683f), 138.0366f),
                       new SpawnPoint(new Vector3(-603.1848f, -163.4202f, 37.98526f), 287.3073f),
                       new SpawnPoint(new Vector3(-600.4707f, -168.9938f, 38.03326f), 280.7021f),
                       new SpawnPoint(new Vector3(-597.5139f, -174.2202f, 37.81398f), 288.8064f),
                       new SpawnPoint(new Vector3(-595.2874f, -178.7944f, 37.876f), 308.4246f),
                       new SpawnPoint(new Vector3(-561.6043f, -143.3504f, 38.37634f), 200.4444f),
                       new SpawnPoint(new Vector3(-553.2458f, -139.9449f, 38.46033f), 202.7817f),
                       new SpawnPoint(new Vector3(-539.6207f, -148.3338f, 38.47179f), 177.3744f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-519.3166f, -145.9066f, 38.60562f), 269.6146f),
                       new SpawnPoint(new Vector3(-520.8281f, -141.5162f, 38.70683f), 293.7381f),
                       new SpawnPoint(new Vector3(-521.8105f, -138.1767f, 38.75854f), 296.1263f),
                       new SpawnPoint(new Vector3(-524.8747f, -134.6328f, 38.72994f), 316.8374f),
                       new SpawnPoint(new Vector3(-529.0384f, -132.2143f, 38.81743f), 326.3212f),
                       new SpawnPoint(new Vector3(-520.4464f, -150.8508f, 38.69207f), 244.3041f),
                       new SpawnPoint(new Vector3(-596.4412f, -179.6881f, 37.87894f), 128.5861f),
                       new SpawnPoint(new Vector3(-598.7047f, -176.151f, 37.6919f), 111.0178f),
                       new SpawnPoint(new Vector3(-600.2548f, -172.4879f, 37.97786f), 109.661f),
                       new SpawnPoint(new Vector3(-601.9487f, -169.5434f, 38.01443f), 120.857f),
                       new SpawnPoint(new Vector3(-603.3534f, -166.2978f, 37.86875f), 114.5827f),
                       new SpawnPoint(new Vector3(-604.8237f, -163.0695f, 38.00855f), 110.94f),
                       new SpawnPoint(new Vector3(-562.1962f, -142.4703f, 38.36739f), 197.5574f),
                       new SpawnPoint(new Vector3(-553.6176f, -138.8344f, 38.44429f), 198.5192f),
                   }
               ),


            new DemonstrationSpawn
               (
                   new SpawnPoint(new Vector3(-782.9558f, 201.8584f, 75.94096f), 40.6152f),
                   new SpawnPoint(new Vector3(-782.2431f, 199.4456f, 75.98138f), 93.53304f),
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-804.4327f, 223.5503f, 75.76568f), 254.9048f),
                       new SpawnPoint(new Vector3(-798.5284f, 225.1687f, 75.98777f), 291.1853f),
                       new SpawnPoint(new Vector3(-789.4806f, 225.0121f, 76.06284f), 213.8983f),
                       new SpawnPoint(new Vector3(-786.8334f, 220.2587f, 76.0358f), 205.5788f),
                       new SpawnPoint(new Vector3(-782.0793f, 209.4457f, 75.95454f), 238.0148f),
                       new SpawnPoint(new Vector3(-777.35f, 210.5634f, 75.80207f), 5.132719f),
                       new SpawnPoint(new Vector3(-782.3892f, 214.3594f, 76.02335f), 83.23071f),
                       new SpawnPoint(new Vector3(-788.0437f, 210.1259f, 76.10785f), 122.51f),
                       new SpawnPoint(new Vector3(-796.1829f, 208.0561f, 76.14155f), 82.79429f),
                       new SpawnPoint(new Vector3(-803.5668f, 210.9775f, 75.88181f), 354.0257f),
                       new SpawnPoint(new Vector3(-808.7257f, 220.677f, 75.54436f), 60.08505f),
                       new SpawnPoint(new Vector3(-819.1612f, 224.1747f, 74.72632f), 74.92085f),
                       new SpawnPoint(new Vector3(-827.8629f, 224.2031f, 74.27534f), 126.1018f),
                       new SpawnPoint(new Vector3(-832.3237f, 219.7244f, 74.17316f), 166.5791f),
                       new SpawnPoint(new Vector3(-833.9147f, 214.2273f, 74.19411f), 180.3154f),
                       new SpawnPoint(new Vector3(-833.4835f, 208.5788f, 74.21877f), 204.7668f),
                       new SpawnPoint(new Vector3(-829.9114f, 206.3136f, 74.26825f), 278.927f),
                       new SpawnPoint(new Vector3(-826.6158f, 208.5635f, 74.43385f), 329.5859f),
                       new SpawnPoint(new Vector3(-825.1116f, 213.0537f, 74.47854f), 341.1461f),
                       new SpawnPoint(new Vector3(-822.8264f, 218.4109f, 74.55835f), 317.2358f),
                       new SpawnPoint(new Vector3(-816.8519f, 216.8261f, 74.97497f), 201.7719f),
                       new SpawnPoint(new Vector3(-815.8834f, 210.0029f, 75.10199f), 214.4582f),
                       new SpawnPoint(new Vector3(-811.8755f, 206.9961f, 75.3921f), 259.7938f),
                       new SpawnPoint(new Vector3(-806.465f, 205.399f, 75.72653f), 265.8063f),
                       new SpawnPoint(new Vector3(-795.7169f, 217.5184f, 76.33067f), 345.4293f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-774.8906f, 229.4007f, 75.5139f), 91.80251f),
                       new SpawnPoint(new Vector3(-774.0237f, 223.7654f, 75.74294f), 99.21131f),
                       new SpawnPoint(new Vector3(-772.6355f, 215.6f, 75.7428f), 101.6956f),
                       new SpawnPoint(new Vector3(-771.72f, 209.0748f, 75.73657f), 101.7522f),
                       new SpawnPoint(new Vector3(-770.6781f, 199.0058f, 75.5762f), 71.46509f),
                       new SpawnPoint(new Vector3(-787.6005f, 234.832f, 75.73973f), 184.7651f),
                       new SpawnPoint(new Vector3(-793.493f, 234.9181f, 75.67242f), 191.3043f),
                       new SpawnPoint(new Vector3(-841.4586f, 200.9511f, 74.04887f), 295.877f),
                       new SpawnPoint(new Vector3(-841.2157f, 209.9436f, 73.9163f), 259.7341f),
                       new SpawnPoint(new Vector3(-840.1833f, 216.7456f, 73.95924f), 258.2814f),
                       new SpawnPoint(new Vector3(-838.9466f, 224.3545f, 73.92421f), 261.362f),
                       new SpawnPoint(new Vector3(-838.5245f, 230.9272f, 73.72411f), 260.1306f),
                       new SpawnPoint(new Vector3(-838.4919f, 236.7985f, 73.76786f), 252.1679f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-769.8548f, 205.4625f, 75.58411f), 275.669f),
                       new SpawnPoint(new Vector3(-770.6078f, 209.745f, 75.72838f), 285.0641f),
                       new SpawnPoint(new Vector3(-771.2139f, 214.1039f, 75.72684f), 273.0236f),
                       new SpawnPoint(new Vector3(-771.8939f, 218.3438f, 75.72693f), 275.6735f),
                       new SpawnPoint(new Vector3(-772.446f, 222.6005f, 75.72422f), 275.1049f),
                       new SpawnPoint(new Vector3(-773.148f, 226.6529f, 75.68864f), 277.895f),
                       new SpawnPoint(new Vector3(-774.4882f, 233.8036f, 75.70338f), 274.3184f),
                       new SpawnPoint(new Vector3(-773.9341f, 229.8947f, 75.50667f), 266.0018f),
                       new SpawnPoint(new Vector3(-769.4148f, 201.226f, 75.59757f), 276.4433f),
                       new SpawnPoint(new Vector3(-770.0287f, 197.0206f, 75.57623f), 241.9877f),
                       new SpawnPoint(new Vector3(-793.2507f, 235.7624f, 75.76563f), 191.0547f),
                       new SpawnPoint(new Vector3(-788.7588f, 236.1567f, 75.87006f), 179.1277f),
                       new SpawnPoint(new Vector3(-840.6738f, 231.7881f, 73.61796f), 80.86154f),
                       new SpawnPoint(new Vector3(-841.0286f, 227.9156f, 73.79537f), 84.32993f),
                       new SpawnPoint(new Vector3(-841.5347f, 223.31f, 73.84393f), 82.44351f),
                       new SpawnPoint(new Vector3(-842.1217f, 219.1743f, 73.86338f), 100.0876f),
                       new SpawnPoint(new Vector3(-842.5686f, 215.8524f, 73.87935f), 80.16692f),
                       new SpawnPoint(new Vector3(-843.1177f, 212.7005f, 73.89764f), 78.7252f),
                       new SpawnPoint(new Vector3(-843.6206f, 208.7014f, 73.7784f), 78.19115f),
                       new SpawnPoint(new Vector3(-844.1271f, 205.0241f, 73.90728f), 96.5784f),
                       new SpawnPoint(new Vector3(-843.0417f, 201.208f, 73.99111f), 102.3836f),
                       new SpawnPoint(new Vector3(-840.0854f, 235.4108f, 73.75157f), 261.6746f),
                   }
               ),
                };

            return spawns.GetRandomElement();
        }



        public static Model barrierModel = 4151651686;
        private static Model guitarModel = "prop_acc_guitar_01";
        private static Model protestSignModel = "prop_cs_protest_sign_01";
        private static Model bongosModel = "prop_bongos_01";

        public static Tuple<AnimationDictionary, string> ProtestSignAnimation = new Tuple<AnimationDictionary, string>("special_ped@griff@monologue_1@monologue_1e", "iamnotaracist_4");
        public static Tuple<AnimationDictionary, string> GuitarAnimation = new Tuple<AnimationDictionary, string>("amb@world_human_musician@guitar@male@base", "base");
        public static Tuple<AnimationDictionary, string> BongosAnimation = new Tuple<AnimationDictionary, string>("amb@world_human_musician@bongos@male@idle_a", "idle_a");
        public static Tuple<AnimationDictionary, string[]> FemalePicnicAnimations = new Tuple<AnimationDictionary, string[]>("amb@world_human_picnic@female@idle_a", new string[] { "idle_a", "idle_b", "idle_c" });
        public static Tuple<AnimationDictionary, string[]> MalePicnicAnimations = new Tuple<AnimationDictionary, string[]>("amb@world_human_picnic@male@idle_a", new string[] { "idle_a", "idle_b", "idle_c" });
        public static Tuple<AnimationDictionary, string> UpperVAnimation = new Tuple<AnimationDictionary, string>("mp_player_int_upperv_sign", "mp_player_int_v_sign");
        public static Tuple<AnimationDictionary, string[]> UpperFingerAnimations = new Tuple<AnimationDictionary, string[]>("mp_player_int_upperfinger", new string[] { "mp_player_int_finger_02", "mp_player_int_finger_01" });
    }
}
