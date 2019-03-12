//namespace WildernessCallouts.Callouts
//{
//    using LSPD_First_Response.Mod.Callouts;
//    using Rage;
//    using Rage.Native;
//    using System;
//    using WildernessCallouts.Types;
//    using System.Collections.Generic;
//    using System.Linq;
//    using System.Windows.Forms;

//    [CalloutInfo("Escort", CalloutProbability.Medium)]
//    public class Escort : CalloutBase
//    {
//        EscortSpawn spawnUsed = null;
//        EscortState state;

//        public override bool OnBeforeCalloutDisplayed()
//        {
//            spawnUsed = EscortSpawn.GetSpawn();
//            for (int i = 0; i < 20; i++)
//            {
//                Logger.LogTrivial(this.GetType().Name, "Get spawn attempt #" + i);
//                if (spawnUsed.VIPVehicleSpawnPoint.Position.DistanceTo(Game.LocalPlayer.Character) < 1650f &&
//                    spawnUsed.VIPVehicleSpawnPoint.Position.DistanceTo(Game.LocalPlayer.Character) > 50.0f)
//                    break;
//                spawnUsed = EscortSpawn.GetSpawn();
//            }
//            if (spawnUsed.VIPVehicleSpawnPoint.Position.DistanceTo(Game.LocalPlayer.Character) > 1650f)
//            {
//                Logger.LogTrivial(this.GetType().Name, "Aborting: Spawn too far");
//                return false;
//            }
//            else if (spawnUsed.VIPVehicleSpawnPoint.Position.DistanceTo(Game.LocalPlayer.Character) < 50.0f)
//            {
//                Logger.LogTrivial(this.GetType().Name, "Aborting: Spawn too close");
//                return false;
//            }

//            this.CalloutMessage = (spawnUsed.Type == EscortSpawn.EscortType.VIPLimousine ? "VIP" : "Inmate Transport") + " Escort";
//            this.CalloutPosition = spawnUsed.VIPVehicleSpawnPoint.Position;
//            this.ShowCalloutAreaBlipBeforeAccepting(this.CalloutPosition, 50f);

//            return spawnUsed.Create();
//        }

//        public override bool OnCalloutAccepted()
//        {
//            state = EscortState.EnRoute;
//            spawnUsed.CreateVIPBlip();
//            return base.OnCalloutAccepted();
//        }

//        public override void OnCalloutNotAccepted()
//        {
//            spawnUsed.Delete();
//            base.OnCalloutNotAccepted();
//        }

//        public override void Process()
//        {
//            if(state == EscortState.EnRoute && Game.LocalPlayer.Character.DistanceTo(this.CalloutPosition) < 30.0f)
//            {
//                state = EscortState.OnScene;
//            } 

//            if(state == EscortState.OnScene)
//            {
//                Game.DisplayHelp("Position your vehicle in front of convoy and when you are ready press ~b~" + Settings.ActionKey1 + "~s~ to start", 10);
//                if (Game.IsKeyDown(Settings.ActionKey1))
//                {
//                    if (Game.LocalPlayer.Character.IsInAnyVehicle(false))
//                    {
//                        if (spawnUsed.VIPBlip != null && spawnUsed.VIPBlip.Exists())
//                            spawnUsed.VIPBlip.DisableRoute();
//                        spawnUsed.VIPBlip.Sprite = BlipSprite.PoliceChase;
//                        spawnUsed.VIPBlip.Scale = 0.75f;
//                        spawnUsed.VIPBlip.SetName(spawnUsed.Type == EscortSpawn.EscortType.VIPLimousine ? "VIP" : "Inmate");
//                        spawnUsed.GiveEscortTasks();
//                        spawnUsed.CreateEndBlip();
//                        spawnUsed.CreateEscortVehiclesBlips();
//                        state = EscortState.Escorting;
//                    }
//                    else
//                    {
//                        Game.DisplayHelp("You must be in a vehicle", 4500);
//                        GameFiber.Sleep(4000);
//                    }
//                }
//            }

//            if (state == EscortState.Escorting)
//            {
//                if (Game.IsKeyDown(Keys.O))
//                {
//                    Game.DisplayHelp("Re-assigning escort tasks", 5000);
//                }

//                spawnUsed.Process();
//                if (spawnUsed.IsVIPDead)
//                {
//                    Game.DisplayNotification("~r~<font size=\"13\"><b>" + (spawnUsed.Type == EscortSpawn.EscortType.VIPLimousine ? "VIP" : "Inmate") + " has died</b></font>");
//                    state = EscortState.End;
//                    doFadeScreenAtEnd = true;
//                    this.End();
//                }
//                if (spawnUsed.ReachedEnd)
//                {
//                    if(spawnUsed.IsVIPDead) Game.DisplayNotification("~r~<font size=\"13\"><b>" + (spawnUsed.Type == EscortSpawn.EscortType.VIPLimousine ? "VIP" : "Inmate") + " is dead</b></font>");
//                    else Game.DisplayNotification("~g~<font size=\"13\"><b>" + (spawnUsed.Type == EscortSpawn.EscortType.VIPLimousine ? "VIP" : "Inmate") + " is alive</b></font>");
//                    state = EscortState.End;
//                    doFadeScreenAtEnd = true;
//                    this.End();
//                }
//                if (!hasEscapeMessageBeenDisplayed && spawnUsed.HasInmateEscaped && !spawnUsed.IsVIPDead)
//                {
//                    Game.DisplayNotification("~o~<font size=\"13\"><b>" + (spawnUsed.Type == EscortSpawn.EscortType.VIPLimousine ? "VIP" : "Inmate") + " has escaped</b></font>");
//                    hasEscapeMessageBeenDisplayed = true;
//                }
//            }

//            base.Process();
//        }
//        bool hasEscapeMessageBeenDisplayed = false;

//        bool doFadeScreenAtEnd = false;
//        public override void End()
//        {
//            if (doFadeScreenAtEnd)
//            {
//                Game.FadeScreenOut(3000, true);
//                GameFiber.Sleep(300);
//                spawnUsed.Delete();
//                GameFiber.Sleep(300);
//                Game.FadeScreenIn(3000, true);
//                base.End();
//            }
//            else
//            {
//                spawnUsed.Dismiss();
//                base.End();
//            }
//        }

//        public enum EscortState
//        {
//            EnRoute,
//            OnScene,
//            Escorting,
//            End,
//        }
//    }


//    public class EscortSpawn
//    {
//        public static RelationshipGroup EscortPedsRelationshipGroup = new RelationshipGroup("ESCORTPEDS");
//        public static RelationshipGroup AttackersPedRelationshipGroup = new RelationshipGroup("VIPATTACKERSPEDS");
//        public static RelationshipGroup VIPPedRelationshipGroup = new RelationshipGroup("VIPPED");

//        public EscortType Type;

//        public SpawnPoint VIPVehicleSpawnPoint;
//        public Vehicle VIPVehicle;
//        public Ped VIP;
//        public Ped VIPDriver;
//        public Ped VIPVehicleEscort;
//        public Blip VIPBlip;

//        public Vector3 EndPosition;
//        public Blip EndBlip;

//        public List<Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>> InFrontEscortVehiclesSpawnPoints;
//        public List<EscortVehicle> InFrontEscortVehicles;

//        public List<Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>> BackEscortVehiclesSpawnPoints;
//        public List<EscortVehicle> BackEscortVehicles;

//        public bool HaveEscortTasksBeenGiven = false;
//        public bool ReachedEnd = false;
//        public bool IsVIPDead = false;
//        public bool HasInmateEscaped = false;

//        public EscortSpawn(EscortType type, SpawnPoint vipVehicleSpawnPoint, List<Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>> inFrontEscortVehicleSpawnPoints, List<Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>> backEscortVehicleSpawnPoints, Vector3[] possibleEndPosition)
//        {
//            Game.SetRelationshipBetweenRelationshipGroups(EscortPedsRelationshipGroup, Game.LocalPlayer.Character.RelationshipGroup, Relationship.Companion);
//            Game.SetRelationshipBetweenRelationshipGroups(EscortPedsRelationshipGroup, "COP", Relationship.Companion);
//            Game.SetRelationshipBetweenRelationshipGroups(EscortPedsRelationshipGroup, VIPPedRelationshipGroup, Relationship.Companion);
//            Game.SetRelationshipBetweenRelationshipGroups("COP", EscortPedsRelationshipGroup, Relationship.Companion);

//            Game.SetRelationshipBetweenRelationshipGroups(VIPPedRelationshipGroup, Game.LocalPlayer.Character.RelationshipGroup, Relationship.Companion);
//            Game.SetRelationshipBetweenRelationshipGroups(VIPPedRelationshipGroup, "COP", Relationship.Companion);
//            Game.SetRelationshipBetweenRelationshipGroups(VIPPedRelationshipGroup, EscortPedsRelationshipGroup, Relationship.Companion);
//            if(Type == EscortType.InmateTransport)
//                Game.SetRelationshipBetweenRelationshipGroups(VIPPedRelationshipGroup, AttackersPedRelationshipGroup, Relationship.Companion);
//            Game.SetRelationshipBetweenRelationshipGroups("COP", VIPPedRelationshipGroup, Relationship.Companion);

//            Game.SetRelationshipBetweenRelationshipGroups(AttackersPedRelationshipGroup, Game.LocalPlayer.Character.RelationshipGroup, Relationship.Hate);
//            Game.SetRelationshipBetweenRelationshipGroups(AttackersPedRelationshipGroup, "COP", Relationship.Hate);
//            Game.SetRelationshipBetweenRelationshipGroups(AttackersPedRelationshipGroup, EscortPedsRelationshipGroup, Relationship.Hate);
//            Game.SetRelationshipBetweenRelationshipGroups(AttackersPedRelationshipGroup, VIPPedRelationshipGroup, Type == EscortType.VIPLimousine ? Relationship.Hate : Relationship.Companion);
//            Game.SetRelationshipBetweenRelationshipGroups("COP", AttackersPedRelationshipGroup, Relationship.Hate);

//            Type = type;

//            VIPVehicleSpawnPoint = vipVehicleSpawnPoint;

//            InFrontEscortVehiclesSpawnPoints = inFrontEscortVehicleSpawnPoints;
//            BackEscortVehiclesSpawnPoints = backEscortVehicleSpawnPoints;

//            EndPosition = possibleEndPosition.GetRandomElement();

//            InFrontEscortVehicles = new List<EscortVehicle>();
//            BackEscortVehicles = new List<EscortVehicle>();
//        }

//        public bool Create()
//        {

//            VIPVehicle = new Vehicle(Type == EscortType.VIPLimousine ? "stretch" : Globals.Random.Next(2) == 1 ? "pbus" : "policet", VIPVehicleSpawnPoint.Position, VIPVehicleSpawnPoint.Heading);
//            if (!VIPVehicle.Exists()) return false;
//            VIPVehicle.TopSpeed += 15.0f;

//            VIP = new Ped(Type == EscortType.VIPLimousine ? GetVIPPedModel() : GetInmatePedModel(), Vector3.Zero, 0f);
//            if (!VIP.Exists()) return false;
//            VIP.RelationshipGroup = VIPPedRelationshipGroup;
//            VIP.Armor += 50;
//            VIP.Health += 50;
//            NativeFunction.CallByName<uint>("SET_PED_COMBAT_ATTRIBUTES", VIP, 0, false);//canUseCover
//            NativeFunction.CallByName<uint>("SET_PED_COMBAT_ATTRIBUTES", VIP, 1, false);//canUseVehicles
//            NativeFunction.CallByName<uint>("SET_PED_COMBAT_ATTRIBUTES", VIP, 2, false);//canDoDrivebys
//            NativeFunction.CallByName<uint>("SET_PED_COMBAT_ATTRIBUTES", VIP, 3, false);//canLeaveVehicle
//            NativeFunction.CallByName<uint>("SET_PED_COMBAT_ATTRIBUTES", VIP, 46, false);//alwaysFight
//            VIP.WarpIntoVehicle(VIPVehicle, Globals.Random.Next(2) == 1 ? (int)EVehicleSeats.Back_Left : (int)EVehicleSeats.Back_Right);

//            VIPDriver = new Ped(Type == EscortType.VIPLimousine ? GetSecretServicePedModel() : /*GetPolicePedModelForPosition(VIPVehicleSpawnPoint.Position)*/"s_m_m_prisguard_01", Vector3.Zero, 0f);
//            if (!VIPDriver.Exists()) return false;
//            VIPDriver.Inventory.GiveNewWeapon(new EWeaponHash[] { EWeaponHash.AP_Pistol, EWeaponHash.Combat_Pistol, EWeaponHash.Pistol_50, EWeaponHash.Pistol }.GetRandomElement(), 1500, false);
//            VIPDriver.Inventory.GiveNewWeapon(new EWeaponHash[] { EWeaponHash.SMG, EWeaponHash.Carbine_Rifle }.GetRandomElement(), 1500, true);
//            VIPDriver.RelationshipGroup = EscortPedsRelationshipGroup;
//            VIPDriver.Armor += 100;
//            VIPDriver.Health += 50;
//            VIPDriver.BlockPermanentEvents = true;
//            NativeFunction.CallByName<uint>("SET_DRIVER_ABILITY", VIPDriver, 100.0f);
//            NativeFunction.CallByName<uint>("SET_PED_COMBAT_ATTRIBUTES", VIPDriver, 0, true);//canUseCover
//            NativeFunction.CallByName<uint>("SET_PED_COMBAT_ATTRIBUTES", VIPDriver, 1, true);//canUseVehicles
//            NativeFunction.CallByName<uint>("SET_PED_COMBAT_ATTRIBUTES", VIPDriver, 2, true);//canDoDrivebys
//            NativeFunction.CallByName<uint>("SET_PED_COMBAT_ATTRIBUTES", VIPDriver, 3, false);//canLeaveVehicle
//            NativeFunction.CallByName<uint>("SET_PED_COMBAT_ATTRIBUTES", VIPDriver, 46, true);//alwaysFight
//            VIPDriver.WarpIntoVehicle(VIPVehicle, -1);

//            VIPVehicleEscort = new Ped(Type == EscortType.VIPLimousine ? GetSecretServicePedModel() : /*GetPolicePedModelForPosition(VIPVehicleSpawnPoint.Position)*/ "s_m_m_prisguard_01", Vector3.Zero, 0f);
//            if (!VIPVehicleEscort.Exists()) return false;
//            VIPVehicleEscort.Inventory.GiveNewWeapon(new EWeaponHash[] { EWeaponHash.AP_Pistol, EWeaponHash.Combat_Pistol, EWeaponHash.Pistol_50, EWeaponHash.Pistol }.GetRandomElement(), 1500, false);
//            VIPVehicleEscort.Inventory.GiveNewWeapon(new EWeaponHash[] { EWeaponHash.SMG, EWeaponHash.Carbine_Rifle }.GetRandomElement(), 1500, true);
//            VIPVehicleEscort.RelationshipGroup = EscortPedsRelationshipGroup;
//            VIPVehicleEscort.Armor += 75;
//            VIPVehicleEscort.Health += 25;
//            NativeFunction.CallByName<uint>("SET_PED_COMBAT_ATTRIBUTES", VIPVehicleEscort, 0, true);//canUseCover
//            NativeFunction.CallByName<uint>("SET_PED_COMBAT_ATTRIBUTES", VIPVehicleEscort, 1, true);//canUseVehicles
//            NativeFunction.CallByName<uint>("SET_PED_COMBAT_ATTRIBUTES", VIPVehicleEscort, 2, true);//canDoDrivebys
//            NativeFunction.CallByName<uint>("SET_PED_COMBAT_ATTRIBUTES", VIPVehicleEscort, 3, false);//canLeaveVehicle
//            NativeFunction.CallByName<uint>("SET_PED_COMBAT_ATTRIBUTES", VIPVehicleEscort, 46, true);//alwaysFight
//            VIPVehicleEscort.WarpIntoVehicle(VIPVehicle, 0);

//            foreach (Tuple<EscortVehicle.EscortVehicleType, SpawnPoint> sp in InFrontEscortVehiclesSpawnPoints)
//            {
//                EscortVehicle v = new EscortVehicle(sp.Item1, sp.Item2, Type);
//                if (!v.Vehicle.Exists()) return false;
//                foreach (Ped p in v.Peds)
//                {
//                    if (!p.Exists()) return false;
//                }
//                InFrontEscortVehicles.Add(v);
//            }
//            InFrontEscortVehicles = InFrontEscortVehicles.OrderByDescending(x => x.Vehicle.DistanceTo(VIPVehicle.Position)).ToList();

//            foreach (Tuple<EscortVehicle.EscortVehicleType, SpawnPoint> sp in BackEscortVehiclesSpawnPoints)
//            {
//                EscortVehicle v = new EscortVehicle(sp.Item1, sp.Item2, Type);
//                if (!v.Vehicle.Exists()) return false;
//                foreach (Ped p in v.Peds)
//                {
//                    if (!p.Exists()) return false;
//                }
//                BackEscortVehicles.Add(v);
//            }
//            BackEscortVehicles = BackEscortVehicles.OrderBy(x => x.Vehicle.DistanceTo(VIPVehicle.Position)).ToList();

//            return true;
//        }

//        public void Delete()
//        {
//            if (VIPVehicle.Exists()) VIPVehicle.Delete();
//            if (VIP.Exists()) VIP.Delete();
//            if (VIPDriver.Exists()) VIPDriver.Delete();
//            if (VIPVehicleEscort.Exists()) VIPVehicleEscort.Delete();
//            if (VIPBlip.Exists()) VIPBlip.Delete();
//            if (EndBlip.Exists()) EndBlip.Delete();
//            foreach (EscortVehicle e in InFrontEscortVehicles)
//                e.Delete();
//            foreach (EscortVehicle e in BackEscortVehicles)
//                e.Delete();
//            foreach (AttackersVehicle e in AttackersVehicles)
//                e.Delete();
//            VIPVehicleSpawnPoint = null;
//            InFrontEscortVehiclesSpawnPoints = null;
//            InFrontEscortVehicles = null;
//            BackEscortVehiclesSpawnPoints = null;
//            BackEscortVehicles = null;
//        }

//        public void Dismiss()
//        {
//            if (VIPVehicle.Exists()) VIPVehicle.Dismiss();
//            if (VIP.Exists()) VIP.Dismiss();
//            if (VIPDriver.Exists()) VIPDriver.Dismiss();
//            if (VIPVehicleEscort.Exists()) VIPVehicleEscort.Dismiss();
//            if (VIPBlip.Exists()) VIPBlip.Delete();
//            if (EndBlip.Exists()) EndBlip.Delete();
//            foreach (EscortVehicle e in InFrontEscortVehicles)
//                e.Dismiss();
//            foreach (EscortVehicle e in BackEscortVehicles)
//                e.Dismiss();
//            foreach (AttackersVehicle e in AttackersVehicles)
//                e.Dismiss();
//            VIPVehicleSpawnPoint = null;
//            InFrontEscortVehiclesSpawnPoints = null;
//            InFrontEscortVehicles = null;
//            BackEscortVehiclesSpawnPoints = null;
//            BackEscortVehicles = null;
//        }

//        public void CreateEscortVehiclesBlips()
//        {
//            foreach (EscortVehicle escort in InFrontEscortVehicles)
//            {
//                escort.CreateBlip();
//            }
//            foreach (EscortVehicle escort in BackEscortVehicles)
//            {
//                escort.CreateBlip();
//            }
//        }

//        public void Process()
//        {
//            //if (Game.IsKeyDown(Keys.U))
//            //{
//            //    Game.DisplayHelp("Ordering and giving escort tasks");
//            //    InFrontEscortVehicles = InFrontEscortVehicles.OrderByDescending(x => x.Vehicle.DistanceTo(VIPVehicle.Position)).ToList();
//            //    BackEscortVehicles = BackEscortVehicles.OrderBy(x => x.Vehicle.DistanceTo(VIPVehicle.Position)).ToList();
//            //    GiveEscortTasks();
//            //}

//            //for (int i = 0; i < InFrontEscortVehicles.Count; i++)
//            //{
//            //    Vector2 pos = World.ConvertWorldPositionToScreenPosition(InFrontEscortVehicles[i].Vehicle.Position);
//            //    new RAGENativeUI.Elements.ResText("IN-FRONT #" + i + "~n~Distance: " + InFrontEscortVehicles[i].Vehicle.DistanceTo(VIPVehicle.Position), new System.Drawing.Point((int)pos.X, (int)pos.Y), 0.45f).Draw();
//            //}
//            //for (int i = 0; i < BackEscortVehicles.Count; i++)
//            //{
//            //    Vector2 pos = World.ConvertWorldPositionToScreenPosition(BackEscortVehicles[i].Vehicle.Position);
//            //    new RAGENativeUI.Elements.ResText("BACK #" + i + "~n~Distance: " + BackEscortVehicles[i].Vehicle.DistanceTo(VIPVehicle.Position), new System.Drawing.Point((int)pos.X, (int)pos.Y), 0.45f).Draw();
//            //}

//            HandleAttackers();
//            SynchronizeSirens();
//            Helper.DrawMarker(EMarkerType.VerticalCylinder, EndPosition + Vector3.WorldDown * 1.25f, Vector3.Zero, Vector3.Zero, new Vector3(4f, 4f, 0.5f), System.Drawing.Color.FromArgb(150, System.Drawing.Color.Yellow), true, false, 2, false, null, null, false);
//            IsVIPDead = VIP.IsDead;
//            if (VIP.DistanceTo(EndPosition) < 12.5f && VIP.Speed < 1.5f)
//                ReachedEnd = true;

//            if(Type == EscortType.InmateTransport && !HasInmateEscaped && VIPDriver.IsDead)
//            {
//                HasInmateEscaped = true;
//                GameFiber.StartNew(delegate
//                {
//                    VIP.Tasks.LeaveVehicle(LeaveVehicleFlags.LeaveDoorOpen).WaitForCompletion(12500);
//                    if (VIP.IsInAnyVehicle(false) || VIP.IsInAnyVehicle(true))
//                        VIP.Tasks.LeaveVehicle(LeaveVehicleFlags.WarpOut).WaitForCompletion();
//                    VIP.ReactAndFlee(Game.LocalPlayer.Character);
//                });
//            }
//        }

//        bool firstTimeToGiveTasks = true;
//        public void GiveEscortTasks()
//        {
//            if (Game.LocalPlayer.Character.IsInAnyVehicle(false))
//            {
//                VehicleDrivingFlags drivingStyle = (VehicleDrivingFlags)262199/*DriveToPositionFlags.FollowTraffic | DriveToPositionFlags.YieldToCrossingPedestrians | DriveToPositionFlags.DriveAroundVehicles | DriveToPositionFlags.DriveAroundPeds | DriveToPositionFlags.DriveAroundObjects | DriveToPositionFlags.AllowMedianCrossing | DriveToPositionFlags.AllowWrongWay*/;
//                float speed = 55f;

//                List<EscortVehicle> inFrontEscortVehs = InFrontEscortVehicles.Where(escort => escort.Vehicle.IsAlive && escort.Vehicle.HasDriver && escort.Vehicle.Driver.IsAlive).ToList();
//                List<EscortVehicle> backEscortVehs = BackEscortVehicles.Where(escort => escort.Vehicle.IsAlive && escort.Vehicle.HasDriver && escort.Vehicle.Driver.IsAlive).ToList();

//                if (firstTimeToGiveTasks)
//                {
//                    inFrontEscortVehs = inFrontEscortVehs.OrderByDescending(x => x.Vehicle.DistanceTo(VIPVehicle.Position)).ToList();
//                    backEscortVehs = backEscortVehs.OrderBy(x => x.Vehicle.DistanceTo(VIPVehicle.Position)).ToList();
//                }

//                //GameFiber.StartNew(delegate
//                //{
//                //    while (true)
//                //    {
//                //        GameFiber.Yield();
//                //        for (int i = 0; i < backEscortVehs.Count; i++)
//                //        {
//                //            Vector2 pos = World.ConvertWorldPositionToScreenPosition(backEscortVehs[i].Vehicle.Position);
//                //            new RAGENativeUI.Elements.ResText("BACK #" + i, new System.Drawing.Point((int)pos.X, (int)pos.Y), 0.4f).Draw();
//                //        }
//                //        for (int i = 0; i < inFrontEscortVehs.Count; i++)
//                //        {
//                //            Vector2 pos = World.ConvertWorldPositionToScreenPosition(inFrontEscortVehs[i].Vehicle.Position);
//                //            new RAGENativeUI.Elements.ResText("IN-FRONT #" + i, new System.Drawing.Point((int)pos.X, (int)pos.Y), 0.4f).Draw();
//                //        }
//                //    }
//                //});

//                for (int i = 0; i < inFrontEscortVehs.Count; i++)
//                {
//                    if (i == 0)
//                    {
//                        inFrontEscortVehs[i].Vehicle.Driver.Escort(inFrontEscortVehs[i].Vehicle, Game.LocalPlayer.Character.CurrentVehicle, speed, (int)drivingStyle, 12f, 7.5f);
//                    }
//                    else
//                    {
//                        inFrontEscortVehs[i].Vehicle.Driver.Escort(inFrontEscortVehs[i].Vehicle, inFrontEscortVehs[i - 1].Vehicle, speed, (int)drivingStyle, 12f, 7.5f);
//                    }
//                }

//                VIPDriver.Escort(VIPVehicle, inFrontEscortVehs.Count > 0 ? inFrontEscortVehs.Last().Vehicle : Game.LocalPlayer.Character.CurrentVehicle, speed, (int)drivingStyle, 12f, 7.5f);

//                for (int i = 0; i < backEscortVehs.Count; i++)
//                {
//                    if(i == 0)
//                    {
//                        backEscortVehs[i].Vehicle.Driver.Escort(backEscortVehs[i].Vehicle, VIPVehicle, speed, (int)drivingStyle, 12f, 7.5f);
//                    }
//                    else
//                    {
//                        backEscortVehs[i].Vehicle.Driver.Escort(backEscortVehs[i].Vehicle, backEscortVehs[i - 1].Vehicle, speed, (int)drivingStyle, 12f, 7.5f);
//                    }
//                }

//                firstTimeToGiveTasks = false;
//                HaveEscortTasksBeenGiven = true;
//            }
//            else
//            {
//                HaveEscortTasksBeenGiven = false;
//            }
//        }

//        public void SynchronizeSirens()
//        {
//            if (Game.LocalPlayer.Character.IsInAnyVehicle(false))
//            {
//                Vehicle playerVeh = Game.LocalPlayer.Character.CurrentVehicle;

//                if (playerVeh.IsSirenOn && !VIPVehicle.IsSirenOn)
//                    VIPVehicle.IsSirenOn = true;

//                if (!playerVeh.IsSirenOn && VIPVehicle.IsSirenOn)
//                    VIPVehicle.IsSirenOn = false;

//                if (playerVeh.IsSirenSilent && !VIPVehicle.IsSirenSilent)
//                    VIPVehicle.IsSirenSilent = true;

//                if (!playerVeh.IsSirenSilent && VIPVehicle.IsSirenSilent)
//                    VIPVehicle.IsSirenSilent = false;

//                foreach (EscortVehicle escort in InFrontEscortVehicles)
//                {
//                    if (playerVeh.IsSirenOn && !escort.Vehicle.IsSirenOn)
//                        escort.Vehicle.IsSirenOn = true;

//                    if (!playerVeh.IsSirenOn && escort.Vehicle.IsSirenOn)
//                        escort.Vehicle.IsSirenOn = false;

//                    if (playerVeh.IsSirenSilent && !escort.Vehicle.IsSirenSilent)
//                        escort.Vehicle.IsSirenSilent = true;

//                    if (!playerVeh.IsSirenSilent && escort.Vehicle.IsSirenSilent)
//                        escort.Vehicle.IsSirenSilent = false;
//                }

//                foreach (EscortVehicle escort in BackEscortVehicles)
//                {
//                    if (playerVeh.IsSirenOn && !escort.Vehicle.IsSirenOn)
//                        escort.Vehicle.IsSirenOn = true;

//                    if (!playerVeh.IsSirenOn && escort.Vehicle.IsSirenOn)
//                        escort.Vehicle.IsSirenOn = false;

//                    if (playerVeh.IsSirenSilent && !escort.Vehicle.IsSirenSilent)
//                        escort.Vehicle.IsSirenSilent = true;

//                    if (!playerVeh.IsSirenSilent && escort.Vehicle.IsSirenSilent)
//                        escort.Vehicle.IsSirenSilent = false;
//                }
//            }
//        }

//        public List<AttackersVehicle> AttackersVehicles = new List<AttackersVehicle>();
//        //public uint spawnTime = Game.GameTime + (uint)Globals.Random.Next(10000, 32500);
//        ulong tickCount;
//        ulong tickCountUntilSpawn = (uint)Globals.Random.Next(11500, 120000);
//        public void HandleAttackers()
//        {
//            if (tickCount == default(ulong))
//            {
//                tickCount = Game.TickCount;
//            }

//            if ((Game.TickCount - tickCount) > tickCountUntilSpawn || Game.IsKeyDown(System.Windows.Forms.Keys.U))
//            {
//                Logger.LogTrivial(this.GetType().Name, "Creating attacker");
//                AttackersVehicle attackers = new AttackersVehicle(Type);
//                attackers.Controller(Type == EscortType.VIPLimousine ? VIP : VIPDriver);
//                AttackersVehicles.Add(attackers);
//                tickCount = Game.TickCount;
//                tickCountUntilSpawn = (uint)Globals.Random.Next(11500, 120000);
//            }
//        }

//        public void CreateVIPBlip()
//        {
//            VIPBlip = new Blip(VIP);
//            //VIPBlip.Color = System.Drawing.Color.CadetBlue;
//            VIPBlip.SetName(Type == EscortSpawn.EscortType.VIPLimousine ? "VIP" : "Inmate");
//            VIPBlip.EnableRoute(System.Drawing.Color.CadetBlue);
//        }

//        public void CreateEndBlip()
//        {
//            EndBlip = new Blip(EndPosition);
//            EndBlip.EnableRoute(EndBlip.Color);
//        }


//        public class EscortVehicle
//        {
//            public List<Ped> Peds { get; private set; }
//            public Vehicle Vehicle { get; private set; }
//            public Blip Blip { get; private set; }
//            public EscortVehicleType Type { get; private set; }

//            public EscortVehicle(EscortVehicleType type, SpawnPoint vehicleSP, EscortType escortType)
//            {
//                Type = type;

//                Game.SetRelationshipBetweenRelationshipGroups(EscortPedsRelationshipGroup, Game.LocalPlayer.Character.RelationshipGroup, Relationship.Companion);
//                Game.SetRelationshipBetweenRelationshipGroups(EscortPedsRelationshipGroup, "COP", Relationship.Companion);
//                Game.SetRelationshipBetweenRelationshipGroups(EscortPedsRelationshipGroup, VIPPedRelationshipGroup, Relationship.Companion);
//                Game.SetRelationshipBetweenRelationshipGroups("COP", EscortPedsRelationshipGroup, Relationship.Companion);

//                Game.SetRelationshipBetweenRelationshipGroups(VIPPedRelationshipGroup, Game.LocalPlayer.Character.RelationshipGroup, Relationship.Companion);
//                Game.SetRelationshipBetweenRelationshipGroups(VIPPedRelationshipGroup, "COP", Relationship.Companion);
//                Game.SetRelationshipBetweenRelationshipGroups(VIPPedRelationshipGroup, EscortPedsRelationshipGroup, Relationship.Companion);
//                if (escortType == EscortType.InmateTransport)
//                    Game.SetRelationshipBetweenRelationshipGroups(VIPPedRelationshipGroup, AttackersPedRelationshipGroup, Relationship.Companion);
//                Game.SetRelationshipBetweenRelationshipGroups("COP", VIPPedRelationshipGroup, Relationship.Companion);

//                Game.SetRelationshipBetweenRelationshipGroups(AttackersPedRelationshipGroup, Game.LocalPlayer.Character.RelationshipGroup, Relationship.Hate);
//                Game.SetRelationshipBetweenRelationshipGroups(AttackersPedRelationshipGroup, "COP", Relationship.Hate);
//                Game.SetRelationshipBetweenRelationshipGroups(AttackersPedRelationshipGroup, EscortPedsRelationshipGroup, Relationship.Hate);
//                Game.SetRelationshipBetweenRelationshipGroups(AttackersPedRelationshipGroup, VIPPedRelationshipGroup, escortType == EscortType.VIPLimousine ? Relationship.Hate : Relationship.Companion);
//                Game.SetRelationshipBetweenRelationshipGroups("COP", AttackersPedRelationshipGroup, Relationship.Hate);

//                Peds = new List<Ped>();

//                switch (Type)
//                {
//                    case EscortVehicleType.Police:
//                        Vehicle = new Vehicle(GetPoliceVehicleModelForPosition(vehicleSP.Position), vehicleSP.Position, vehicleSP.Heading);
//                        Ped p1 = new Ped(GetPolicePedModelForPosition(vehicleSP.Position), Vector3.Zero, 0f);
//                        p1.Inventory.GiveNewWeapon(new EWeaponHash[] { EWeaponHash.Combat_Pistol, EWeaponHash.Pistol }.GetRandomElement(), 1500, false);
//                        p1.Inventory.GiveNewWeapon(new EWeaponHash[] { EWeaponHash.SMG, EWeaponHash.Carbine_Rifle }.GetRandomElement(), 1500, true);
//                        p1.WarpIntoVehicle(Vehicle, -1);
//                        p1.Armor += 25;
//                        p1.BlockPermanentEvents = true;
//                        NativeFunction.CallByName<uint>("SET_DRIVER_ABILITY", p1, 100.0f);
//                        NativeFunction.CallByName<uint>("SET_PED_COMBAT_ATTRIBUTES", p1, 0, true);//canUseCover
//                        NativeFunction.CallByName<uint>("SET_PED_COMBAT_ATTRIBUTES", p1, 1, true);//canUseVehicles
//                        NativeFunction.CallByName<uint>("SET_PED_COMBAT_ATTRIBUTES", p1, 2, true);//canDoDrivebys
//                        NativeFunction.CallByName<uint>("SET_PED_COMBAT_ATTRIBUTES", p1, 3, false);//canLeaveVehicle
//                        NativeFunction.CallByName<uint>("SET_PED_COMBAT_ATTRIBUTES", p1, 46, true);//alwaysFight
//                        Peds.Add(p1);
//                        Ped p2 = new Ped(GetPolicePedModelForPosition(vehicleSP.Position), Vector3.Zero, 0f);
//                        p2.Inventory.GiveNewWeapon(new EWeaponHash[] { EWeaponHash.Combat_Pistol, EWeaponHash.Pistol }.GetRandomElement(), 1500, false);
//                        p2.Inventory.GiveNewWeapon(new EWeaponHash[] { EWeaponHash.SMG, EWeaponHash.Carbine_Rifle }.GetRandomElement(), 1500, true);
//                        p2.WarpIntoVehicle(Vehicle, 0);
//                        p2.Armor += 25;
//                        NativeFunction.CallByName<uint>("SET_PED_COMBAT_ATTRIBUTES", p2, 0, true);//canUseCover
//                        NativeFunction.CallByName<uint>("SET_PED_COMBAT_ATTRIBUTES", p2, 1, true);//canUseVehicles
//                        NativeFunction.CallByName<uint>("SET_PED_COMBAT_ATTRIBUTES", p2, 2, true);//canDoDrivebys
//                        NativeFunction.CallByName<uint>("SET_PED_COMBAT_ATTRIBUTES", p2, 3, false);//canLeaveVehicle
//                        NativeFunction.CallByName<uint>("SET_PED_COMBAT_ATTRIBUTES", p2, 46, true);//alwaysFight
//                        Peds.Add(p2);
//                        break;
//                    case EscortVehicleType.SecretService:
//                        Vehicle = new Vehicle(GetSecretServiceVehicleModel(), vehicleSP.Position, vehicleSP.Heading);
//                        for (int i = -1; i < 3; i++)
//                        {
//                            Ped p = new Ped(GetSecretServicePedModel(), Vector3.Zero, 0f);
//                            p.Inventory.GiveNewWeapon(new EWeaponHash[] { EWeaponHash.AP_Pistol, EWeaponHash.Combat_Pistol, EWeaponHash.Pistol_50, EWeaponHash.Pistol }.GetRandomElement(), 1500, false);
//                            p.Inventory.GiveNewWeapon(new EWeaponHash[] { EWeaponHash.SMG, EWeaponHash.Carbine_Rifle }.GetRandomElement(), 1500, true);
//                            p.WarpIntoVehicle(Vehicle, i);
//                            if (i == -1)
//                            {
//                                p.BlockPermanentEvents = true;
//                                NativeFunction.CallByName<uint>("SET_DRIVER_ABILITY", p, 100.0f);
//                            }
//                            p.RelationshipGroup = EscortPedsRelationshipGroup;
//                            p.Armor += 50;
//                            p.MaxHealth += 75;
//                            p.Health += 75;
//                            NativeFunction.CallByName<uint>("SET_PED_COMBAT_ATTRIBUTES", p, 0, true);//canUseCover
//                            NativeFunction.CallByName<uint>("SET_PED_COMBAT_ATTRIBUTES", p, 1, true);//canUseVehicles
//                            NativeFunction.CallByName<uint>("SET_PED_COMBAT_ATTRIBUTES", p, 2, true);//canDoDrivebys
//                            NativeFunction.CallByName<uint>("SET_PED_COMBAT_ATTRIBUTES", p, 3, false);//canLeaveVehicle
//                            NativeFunction.CallByName<uint>("SET_PED_COMBAT_ATTRIBUTES", p, 46, true);//alwaysFight
//                            Peds.Add(p);
//                        }
//                        break;
//                    default:
//                        throw new System.ArgumentException("Escort type " + type + " isn't valid", "type");
//                }
//            }

//            public void Delete()
//            {
//                if (Vehicle.Exists()) Vehicle.Delete();
//                if (Blip.Exists()) Blip.Delete();
//                foreach (Ped p in Peds)
//                    if (p.Exists()) p.Delete();
//                Peds = null;
//            }

//            public void Dismiss()
//            {
//                if (Vehicle.Exists()) Vehicle.Dismiss();
//                if (Blip.Exists()) Blip.Delete();
//                foreach (Ped p in Peds)
//                    if (p.Exists()) p.Dismiss();
//                Peds = null;
//            }

//            public void CreateBlip()
//            {
//                Blip = new Blip(Vehicle);
//                Blip.Sprite = BlipSprite.Police;
//                //Blip.Color = System.Drawing.Color.Blue;
//                Blip.Scale = 0.47225f;
//                Blip.SetName("Escort vehicle");
//            }

//            public bool Exists()
//            {
//                return Vehicle.Exists() && Peds.All(p => p.Exists());
//            }

//            public enum EscortVehicleType
//            {
//                Police,
//                SecretService,
//            }
//        }

//        public class AttackersVehicle
//        {
//            public List<Ped> Peds { get; private set; }
//            public Vehicle Vehicle { get; private set; }
//            public Blip Blip { get; private set; }

//            public AttackersVehicle(SpawnPoint spawnPosition, EscortType escortType)
//            {
//                Game.SetRelationshipBetweenRelationshipGroups(EscortPedsRelationshipGroup, Game.LocalPlayer.Character.RelationshipGroup, Relationship.Companion);
//                Game.SetRelationshipBetweenRelationshipGroups(EscortPedsRelationshipGroup, "COP", Relationship.Companion);
//                Game.SetRelationshipBetweenRelationshipGroups(EscortPedsRelationshipGroup, VIPPedRelationshipGroup, Relationship.Companion);
//                Game.SetRelationshipBetweenRelationshipGroups("COP", EscortPedsRelationshipGroup, Relationship.Companion);

//                Game.SetRelationshipBetweenRelationshipGroups(VIPPedRelationshipGroup, Game.LocalPlayer.Character.RelationshipGroup, Relationship.Companion);
//                Game.SetRelationshipBetweenRelationshipGroups(VIPPedRelationshipGroup, "COP", Relationship.Companion);
//                Game.SetRelationshipBetweenRelationshipGroups(VIPPedRelationshipGroup, EscortPedsRelationshipGroup, Relationship.Companion);
//                if (escortType == EscortType.InmateTransport)
//                    Game.SetRelationshipBetweenRelationshipGroups(VIPPedRelationshipGroup, AttackersPedRelationshipGroup, Relationship.Companion);
//                Game.SetRelationshipBetweenRelationshipGroups("COP", VIPPedRelationshipGroup, Relationship.Companion);

//                Game.SetRelationshipBetweenRelationshipGroups(AttackersPedRelationshipGroup, Game.LocalPlayer.Character.RelationshipGroup, Relationship.Hate);
//                Game.SetRelationshipBetweenRelationshipGroups(AttackersPedRelationshipGroup, "COP", Relationship.Hate);
//                Game.SetRelationshipBetweenRelationshipGroups(AttackersPedRelationshipGroup, EscortPedsRelationshipGroup, Relationship.Hate);
//                Game.SetRelationshipBetweenRelationshipGroups(AttackersPedRelationshipGroup, VIPPedRelationshipGroup, escortType == EscortType.VIPLimousine ? Relationship.Hate : Relationship.Companion);
//                Game.SetRelationshipBetweenRelationshipGroups("COP", AttackersPedRelationshipGroup, Relationship.Hate);
                
//                Vehicle = new Vehicle(GetAttackerVehicleModel(), spawnPosition.Position, spawnPosition.Heading);
//                Peds = new List<Ped>();
//                int seatsCount = Vehicle.FreeSeatsCount;
//                if(seatsCount >= 2)
//                {
//                    if(seatsCount >= 4 && Globals.Random.Next(3) <= 1) //four attackers
//                    {
//                        for (int i = 0; i < 4; i++)
//                        {
//                            Peds.Add(CreateAttacker());
//                        }
//                    }
//                    else //two attackers
//                    {
//                        for (int i = 0; i < 2; i++)
//                        {
//                            Peds.Add(CreateAttacker());
//                        }
//                    }
//                }
//                else //one attacker
//                {
//                    Peds.Add(CreateAttacker());
//                }

//                for (int i = 0; i < Peds.Count; i++)
//                {
//                    Peds[i].WarpIntoVehicle(Vehicle, i - 1);
//                    if (i - 1 == -1)
//                    {
//                        Peds[i].BlockPermanentEvents = true;
//                        NativeFunction.CallByName<uint>("SET_DRIVER_ABILITY", Peds[i], MathHelper.GetRandomSingle(1.0f,  100.0f));
//                        NativeFunction.CallByName<uint>("SET_DRIVER_AGGRESSIVENESS", Peds[i], 100.0f);
//                    }
//                }
//                Blip = null;
//            }

//            public AttackersVehicle(EscortType escortType) : this(GetAttackerSpawn(), escortType) { }


//            public void Controller(Ped vip)
//            {
//                GameFiber.StartNew(delegate
//                {
//                    NativeFunction.CallByName<uint>("TASK_VEHICLE_CHASE", Vehicle.Driver, vip);
//                    while (true)
//                    {
//                        GameFiber.Yield();

//                        if (vip == null || !vip.Exists())
//                            break;

//                        if (Blip == null && Vehicle.DistanceTo(vip) < 42.5f)
//                            CreateBlip();
//                        if (Peds == null || Peds.All(p => p.IsDead))
//                            break;
//                        if (Peds[0].IsDead && vip.DistanceTo(Vehicle) > 80.0f)
//                            break;
//                    }
//                    Dismiss();
//                });
//            }

//            public void CreateBlip()
//            {
//                Blip = new Blip(Vehicle);
//                Blip.Sprite = BlipSprite.Enemy;
//                Blip.Color = System.Drawing.Color.DarkRed;
//            }

//            public void Delete()
//            {
//                if (Vehicle != null && Vehicle.Exists()) Vehicle.Delete();
//                if (Blip != null && Blip.Exists()) Blip.Delete();
//                if (Peds != null)
//                {
//                    foreach (Ped p in Peds)
//                        if (p != null && p.Exists()) p.Delete();
//                }
//                Peds = null;
//            }

//            public void Dismiss()
//            {
//                if (Vehicle != null && Vehicle.Exists()) Vehicle.Dismiss();
//                if (Blip != null && Blip.Exists()) Blip.Delete();
//                if (Peds != null)
//                {
//                    foreach (Ped p in Peds)
//                        if (p != null && p.Exists()) p.Dismiss();
//                }
//                Peds = null;
//            }

//            public bool Exists()
//            {
//                return Blip.Exists() && Vehicle.Exists() && Peds != null && Peds.All(p => p.Exists());
//            }

//            public static Ped CreateAttacker()
//            {
//                Ped ped = new Ped(GetAttackerPedModel(), Vector3.Zero, 0f);
//                ped.Armor += 25;
//                ped.MaxHealth += 30;
//                ped.Health += 30;
//                ped.Inventory.GiveNewWeapon(new EWeaponHash[] { EWeaponHash.Micro_SMG, EWeaponHash.AP_Pistol, EWeaponHash.Combat_Pistol, EWeaponHash.Machine_Pistol, EWeaponHash.Pistol_50, EWeaponHash.Pistol }.GetRandomElement(), 1000, true);
//                NativeFunction.CallByName<uint>("SET_PED_COMBAT_ATTRIBUTES", ped, 0, true);//canUseCover
//                NativeFunction.CallByName<uint>("SET_PED_COMBAT_ATTRIBUTES", ped, 1, true);//canUseVehicles
//                NativeFunction.CallByName<uint>("SET_PED_COMBAT_ATTRIBUTES", ped, 2, true);//canDoDrivebys
//                NativeFunction.CallByName<uint>("SET_PED_COMBAT_ATTRIBUTES", ped, 3, false);//canLeaveVehicle
//                NativeFunction.CallByName<uint>("SET_PED_COMBAT_ATTRIBUTES", ped, 46, true);//alwaysFight
//                return ped;
//            }

//            public static SpawnPoint GetAttackerSpawn()
//            {
//                Vector3 pos = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.AroundPosition(300f));
//                while (true)
//                {
//                    if (pos.DistanceTo(Game.LocalPlayer.Character) > 32.5f) break;
//                    pos = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.AroundPosition(300f));
//                    GameFiber.Yield();
//                }
//                float h = pos.GetClosestVehicleNodeHeading();
//                return new SpawnPoint(pos, h);
//            }
//        }

//        public enum EscortType
//        {
//            VIPLimousine,
//            InmateTransport,
//        }


//        public static EscortSpawn GetSpawn()
//        {
//            return new EscortSpawn[]
//            {
//                //new EscortSpawn
//                //(
//                //    new SpawnPoint(new Vector3(1428.41f, 3164.4f, 40.47f), 285.84f),
//                //    new List<Tuple<EscortVehicle.EscortType, SpawnPoint>>
//                //    {
//                //        new Tuple<EscortVehicle.EscortType, SpawnPoint>(EscortVehicle.EscortType.SecretService, new SpawnPoint(new Vector3(1416.37f, 3161.4f, 40.47f), 280.8f)),
//                //        new Tuple<EscortVehicle.EscortType, SpawnPoint>(EscortVehicle.EscortType.SecretService, new SpawnPoint(new Vector3(1408.37f, 3161.4f, 40.47f), 280.8f)),
//                //        new Tuple<EscortVehicle.EscortType, SpawnPoint>(EscortVehicle.EscortType.Police, new SpawnPoint(new Vector3(1400.37f, 3161.4f, 40.47f), 280.8f)),
//                //    },
//                //    new Vector3(3004.84f, 3511.4f, 71.49f)
//                //),
                
//               new EscortSpawn
//               (
//                   EscortType.VIPLimousine,
//                   new SpawnPoint(new Vector3(-1658.608f, -3094.12f, 13.94476f), 237.7446f),
//                   new List<Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>>
//                   {
//                       new Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>(EscortVehicle.EscortVehicleType.Police, new SpawnPoint(new Vector3(-1650.1f, -3098.98f, 13.94f), 235.13f)),
//                   },
//                   new List<Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>>
//                   {
//                       new Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>(EscortVehicle.EscortVehicleType.SecretService, new SpawnPoint(new Vector3(-1668.14f, -3088.97f, 13.94475f), 238.9403f)),
//                       new Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>(EscortVehicle.EscortVehicleType.SecretService, new SpawnPoint(new Vector3(-1676.285f, -3084.81f, 13.94476f), 248.5226f)),
//                       new Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>(EscortVehicle.EscortVehicleType.Police, new SpawnPoint(new Vector3(-1683.163f, -3087.112f, 13.94476f), 324.9704f)),
//                       new Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>(EscortVehicle.EscortVehicleType.Police, new SpawnPoint(new Vector3(-1687.35f, -3094.248f, 13.94476f), 330.9266f)),
//                   },
//                   new Vector3[] 
//                   {
//                       new Vector3(248.01f, -372.07f, 44.41f),
//                   }
//               ),

//               new EscortSpawn
//               (
//                   EscortType.InmateTransport,
//                   new SpawnPoint(new Vector3(417.5421f, -1025.859f, 29.1572f), 88.08662f),
//                   new List<Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>>
//                   {
//                       new Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>(EscortVehicle.EscortVehicleType.Police, new SpawnPoint(new Vector3(408.199f, -1026.027f, 29.3686f), 87.28635f)),
//                   },
//                   new List<Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>>
//                   {
//                       new Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>(EscortVehicle.EscortVehicleType.Police, new SpawnPoint(new Vector3(425.2985f, -1025.501f, 29.00105f), 91.15604f)),
//                       new Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>(EscortVehicle.EscortVehicleType.Police, new SpawnPoint(new Vector3(432.7725f, -1024.889f, 28.86473f), 93.70112f)),
//                       new Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>(EscortVehicle.EscortVehicleType.Police, new SpawnPoint(new Vector3(439.8677f, -1023.725f, 28.693f), 106.3132f)),
//                       new Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>(EscortVehicle.EscortVehicleType.Police, new SpawnPoint(new Vector3(445.7719f, -1022.699f, 28.55474f), 93.91529f)),
//                   },
//                   new Vector3[]
//                   {
//                       new Vector3(1867.646f, 2605.333f, 45.67201f),
//                   }
//               ),


//               new EscortSpawn
//               (
//                   EscortType.VIPLimousine,
//                   new SpawnPoint(new Vector3(293.0565f, 175.603f, 104.1019f), 65.93702f),
//                   new List<Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>>
//                   {
//                       new Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>(EscortVehicle.EscortVehicleType.SecretService, new SpawnPoint(new Vector3(285.3752f, 178.5103f, 104.2546f), 81.90312f)),
//                   },
//                   new List<Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>>
//                   {
//                       new Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>(EscortVehicle.EscortVehicleType.SecretService, new SpawnPoint(new Vector3(301.4494f, 171.9374f, 103.9316f), 55.44388f)),
//                       new Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>(EscortVehicle.EscortVehicleType.Police, new SpawnPoint(new Vector3(308.1097f, 167.5932f, 103.8625f), 72.22524f)),
//                   },
//                   new Vector3[]
//                   {
//                       new Vector3(-2544.799f, 1907.882f, 169.1497f),
//                   }
//               ),


//               new EscortSpawn
//               (
//                   EscortType.InmateTransport,
//                   new SpawnPoint(new Vector3(-1133.862f, -856.4408f, 13.55077f), 122.1647f),
//                   new List<Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>>
//                   {
//                       new Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>(EscortVehicle.EscortVehicleType.Police, new SpawnPoint(new Vector3(-1147.334f, -851.5797f, 14.07491f), 30.39106f)),
//                       new Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>(EscortVehicle.EscortVehicleType.Police, new SpawnPoint(new Vector3(-1142.615f, -856.3313f, 13.66529f), 63.95385f)),
//                   },
//                   new List<Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>>
//                   {
//                       new Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>(EscortVehicle.EscortVehicleType.Police, new SpawnPoint(new Vector3(-1127.332f, -852.1914f, 13.51956f), 120.0448f)),
//                       new Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>(EscortVehicle.EscortVehicleType.Police, new SpawnPoint(new Vector3(-1122.678f, -848.2329f, 13.44276f), 126.4322f)),
//                   },
//                   new Vector3[]
//                   {
//                       new Vector3(1867.646f, 2605.333f, 45.67201f),
//                   }
//               ),


//               new EscortSpawn
//               (
//                   EscortType.InmateTransport,
//                   new SpawnPoint(new Vector3(-465.676f, 6010.325f, 31.34053f), 354.3252f),
//                   new List<Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>>
//                   {
//                       new Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>(EscortVehicle.EscortVehicleType.Police, new SpawnPoint(new Vector3(-463.0338f, 6017.997f, 31.34055f), 316.0414f)),
//                   },
//                   new List<Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>>
//                   {
//                       new Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>(EscortVehicle.EscortVehicleType.Police, new SpawnPoint(new Vector3(-462.7106f, 6002.634f, 31.34053f), 38.42085f)),
//                   },
//                   new Vector3[]
//                   {
//                       new Vector3(1867.646f, 2605.333f, 45.67201f),
//                   }
//               ),


//               new EscortSpawn
//               (
//                   EscortType.VIPLimousine,
//                   new SpawnPoint(new Vector3(922.7673f, 47.91166f, 80.7648f), 327.4991f),
//                   new List<Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>>
//                   {
//                       new Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>(EscortVehicle.EscortVehicleType.Police, new SpawnPoint(new Vector3(926.1609f, 57.88277f, 80.76481f), 326.3605f)),
//                   },
//                   new List<Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>>
//                   {
//                       new Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>(EscortVehicle.EscortVehicleType.SecretService, new SpawnPoint(new Vector3(915.6918f, 41.35997f, 80.7648f), 325.2641f)),
//                       new Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>(EscortVehicle.EscortVehicleType.SecretService, new SpawnPoint(new Vector3(911.3172f, 34.77338f, 80.32887f), 325.6507f)),
//                   },
//                   new Vector3[]
//                   {
//                       new Vector3(1353.64f, 1147.135f, 113.759f),
//                   }
//               ),


//               new EscortSpawn
//               (
//                   EscortType.VIPLimousine,
//                   new SpawnPoint(new Vector3(228.932f, 1177.593f, 225.46f), 189.4104f),
//                   new List<Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>>
//                   {
//                       new Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>(EscortVehicle.EscortVehicleType.SecretService, new SpawnPoint(new Vector3(234.4264f, 1171.745f, 225.46f), 265.8966f)),
//                   },
//                   new List<Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>>
//                   {
//                       new Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>(EscortVehicle.EscortVehicleType.SecretService, new SpawnPoint(new Vector3(228.722f, 1186.772f, 225.4598f), 169.8583f)),
//                       new Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>(EscortVehicle.EscortVehicleType.SecretService, new SpawnPoint(new Vector3(228.7113f, 1194.394f, 225.4599f), 190.0525f)),
//                       new Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>(EscortVehicle.EscortVehicleType.Police, new SpawnPoint(new Vector3(227.3241f, 1201.704f, 225.4599f), 190.8322f)),
//                   },
//                   new Vector3[]
//                   {
//                       new Vector3(-139.7091f, 960.8193f, 235.7045f),
//                   }
//               ),


//               new EscortSpawn
//               (
//                   EscortType.VIPLimousine,
//                   new SpawnPoint(new Vector3(922.7673f, 47.91166f, 80.7648f), 327.4991f),
//                   new List<Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>>
//                   {
//                       new Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>(EscortVehicle.EscortVehicleType.Police, new SpawnPoint(new Vector3(926.1609f, 57.88277f, 80.76481f), 326.3605f)),
//                   },
//                   new List<Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>>
//                   {
//                       new Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>(EscortVehicle.EscortVehicleType.SecretService, new SpawnPoint(new Vector3(915.6918f, 41.35997f, 80.7648f), 325.2641f)),
//                       new Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>(EscortVehicle.EscortVehicleType.SecretService, new SpawnPoint(new Vector3(911.3172f, 34.77338f, 80.32887f), 325.6507f)),
//                   },
//                   new Vector3[]
//                   {
//                       new Vector3(1353.64f, 1147.135f, 113.759f),
//                   }
//               ),


//               new EscortSpawn
//               (
//                   EscortType.VIPLimousine,
//                   new SpawnPoint(new Vector3(228.932f, 1177.593f, 225.46f), 189.4104f),
//                   new List<Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>>
//                   {
//                       new Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>(EscortVehicle.EscortVehicleType.SecretService, new SpawnPoint(new Vector3(234.4264f, 1171.745f, 225.46f), 265.8966f)),
//                   },
//                   new List<Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>>
//                   {
//                       new Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>(EscortVehicle.EscortVehicleType.SecretService, new SpawnPoint(new Vector3(228.722f, 1186.772f, 225.4598f), 169.8583f)),
//                       new Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>(EscortVehicle.EscortVehicleType.SecretService, new SpawnPoint(new Vector3(228.7113f, 1194.394f, 225.4599f), 190.0525f)),
//                       new Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>(EscortVehicle.EscortVehicleType.Police, new SpawnPoint(new Vector3(227.3241f, 1201.704f, 225.4599f), 190.8322f)),
//                   },
//                   new Vector3[]
//                   {
//                       new Vector3(-139.7091f, 960.8193f, 235.7045f),
//                   }
//               ),


//               new EscortSpawn
//               (
//                   EscortType.VIPLimousine,
//                   new SpawnPoint(new Vector3(837.9814f, 543.4494f, 125.7804f), 243.1004f),
//                   new List<Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>>
//                   {
//                   },
//                   new List<Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>>
//                   {
//                       new Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>(EscortVehicle.EscortVehicleType.SecretService, new SpawnPoint(new Vector3(830.9606f, 548.4922f, 125.7804f), 225.7779f)),
//                       new Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>(EscortVehicle.EscortVehicleType.Police, new SpawnPoint(new Vector3(825.4684f, 553.1226f, 125.7803f), 225.7519f)),
//                       new Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>(EscortVehicle.EscortVehicleType.Police, new SpawnPoint(new Vector3(819.7404f, 558.1023f, 125.7804f), 222.3851f)),
//                       new Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>(EscortVehicle.EscortVehicleType.Police, new SpawnPoint(new Vector3(845.6002f, 539.3585f, 125.7803f), 240.34f)),
//                   },
//                   new Vector3[]
//                   {
//                       new Vector3(-2297.355f, 378.6791f, 174.4668f),
//                   }
//               ),


//               new EscortSpawn
//               (
//                   EscortType.VIPLimousine,
//                   new SpawnPoint(new Vector3(-1031.913f, -2729.576f, 20.15032f), 239.3143f),
//                   new List<Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>>
//                   {
//                       new Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>(EscortVehicle.EscortVehicleType.SecretService, new SpawnPoint(new Vector3(-1024.475f, -2733.823f, 20.15223f), 238.1884f)),
//                   },
//                   new List<Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>>
//                   {
//                       new Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>(EscortVehicle.EscortVehicleType.SecretService, new SpawnPoint(new Vector3(-1040.057f, -2724.996f, 20.14491f), 239.2529f)),
//                       new Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>(EscortVehicle.EscortVehicleType.SecretService, new SpawnPoint(new Vector3(-1047.404f, -2720.579f, 20.15247f), 237.9621f)),
//                   },
//                   new Vector3[]
//                   {
//                       new Vector3(-1529.597f, -27.4378f, 57.48536f),
//                   }
//               ),


//               new EscortSpawn
//               (
//                   EscortType.VIPLimousine,
//                   new SpawnPoint(new Vector3(-1011.353f, -2988.309f, 13.94508f), 1.33426f),
//                   new List<Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>>
//                   {
//                   },
//                   new List<Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>>
//                   {
//                       new Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>(EscortVehicle.EscortVehicleType.SecretService, new SpawnPoint(new Vector3(-1012.375f, -2997.559f, 13.94507f), 339.6656f)),
//                       new Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>(EscortVehicle.EscortVehicleType.SecretService, new SpawnPoint(new Vector3(-1015.418f, -3002.964f, 13.94507f), 323.2375f)),
//                       new Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>(EscortVehicle.EscortVehicleType.SecretService, new SpawnPoint(new Vector3(-1017.71f, -3009.424f, 13.94507f), 346.0706f)),
//                       new Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>(EscortVehicle.EscortVehicleType.Police, new SpawnPoint(new Vector3(-1018.981f, -3016.657f, 13.94508f), 349.0524f)),
//                   },
//                   new Vector3[]
//                   {
//                       new Vector3(2525.061f, -383.9933f, 92.99274f),
//                   }
//               ),


//               //new EscortSpawn
//               //(
//               //    EscortType.VIPLimousine,
//               //    new SpawnPoint(new Vector3(-51.42878f, -786.4038f, 44.07159f), 240.0137f),
//               //    new List<Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>>
//               //    {
//               //        new Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>(EscortVehicle.EscortVehicleType.Police, new SpawnPoint(new Vector3(-41.58691f, -790.5136f, 44.0719f), 260.3242f)),
//               //    },
//               //    new List<Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>>
//               //    {
//               //        new Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>(EscortVehicle.EscortVehicleType.SecretService, new SpawnPoint(new Vector3(-59.81908f, -780.6443f, 44.07195f), 226.5938f)),
//               //        new Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>(EscortVehicle.EscortVehicleType.SecretService, new SpawnPoint(new Vector3(-63.80911f, -774.6998f, 44.13651f), 214.2756f)),
//               //        new Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>(EscortVehicle.EscortVehicleType.Police, new SpawnPoint(new Vector3(-67.50082f, -770.2336f, 44.13346f), 221.4809f)),
//               //        new Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>(EscortVehicle.EscortVehicleType.Police, new SpawnPoint(new Vector3(-71.06789f, -765.0978f, 44.12999f), 210.1428f)),
//               //    },
//               //    new Vector3[]
//               //    {
//               //        new Vector3(-2296.588f, 379.4977f, 174.4666f),
//               //        new Vector3(-1636.896f, -3105.123f, 13.94475f),
//               //        new Vector3(-1014.687f, -2995.901f, 13.94508f),
//               //        new Vector3(2524.789f, -384.2297f, 92.99274f),
//               //        new Vector3(1369.876f, 1148.342f, 113.759f),
//               //    }
//               //),


//               new EscortSpawn
//               (
//                   EscortType.VIPLimousine,
//                   new SpawnPoint(new Vector3(-411.9092f, 1173.189f, 325.6418f), 250.9963f),
//                   new List<Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>>
//                   {
//                       new Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>(EscortVehicle.EscortVehicleType.SecretService, new SpawnPoint(new Vector3(-399.457f, 1171.891f, 325.6458f), 278.3798f)),
//                       new Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>(EscortVehicle.EscortVehicleType.Police, new SpawnPoint(new Vector3(-405.0007f, 1171.679f, 325.7899f), 267.4945f)),
//                   },
//                   new List<Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>>
//                   {
//                       new Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>(EscortVehicle.EscortVehicleType.SecretService, new SpawnPoint(new Vector3(-419.6837f, 1176.657f, 325.6418f), 239.2318f)),
//                       new Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>(EscortVehicle.EscortVehicleType.Police, new SpawnPoint(new Vector3(-424.3204f, 1180.257f, 325.7917f), 220.4124f)),
//                   },
//                   new Vector3[]
//                   {
//                       new Vector3(-1863.35f, -354.0197f, 49.22598f),
//                       new Vector3(-1671.509f, 400.9468f, 88.99423f),
//                       new Vector3(-2296.721f, 378.4194f, 174.4666f),
//                       new Vector3(1365.93f, -579.2695f, 74.38034f),
//                   }
//               ),


//               new EscortSpawn
//               (
//                   EscortType.VIPLimousine,
//                   new SpawnPoint(new Vector3(3539.626f, 3810.961f, 30.47005f), 86.47569f),
//                   new List<Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>>
//                   {
//                   },
//                   new List<Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>>
//                   {
//                       new Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>(EscortVehicle.EscortVehicleType.SecretService, new SpawnPoint(new Vector3(3547.914f, 3810.433f, 30.46268f), 83.98183f)),
//                       new Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>(EscortVehicle.EscortVehicleType.Police, new SpawnPoint(new Vector3(3553.313f, 3809.767f, 30.43681f), 81.43362f)),
//                       new Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>(EscortVehicle.EscortVehicleType.Police, new SpawnPoint(new Vector3(3559.513f, 3809.225f, 30.39086f), 78.01917f)),
//                       new Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>(EscortVehicle.EscortVehicleType.Police, new SpawnPoint(new Vector3(3565.727f, 3808.731f, 30.3233f), 83.57477f)),
//                   },
//                   new Vector3[]
//                   {
//                       new Vector3(-2296.54f, 378.7996f, 174.4666f),
//                       new Vector3(-2545.584f, 1908.032f, 169.132f),
//                       new Vector3(2525.071f, -384.1292f, 92.99274f),
//                       new Vector3(2154.351f, 4808.651f, 41.21137f),
//                   }
//               ),


//               new EscortSpawn
//               (
//                   EscortType.InmateTransport,
//                   new SpawnPoint(new Vector3(1861.165f, 3673.535f, 33.81623f), 115.7807f),
//                   new List<Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>>
//                   {
//                       new Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>(EscortVehicle.EscortVehicleType.Police, new SpawnPoint(new Vector3(1854.418f, 3669.464f, 33.98605f), 115.7262f)),
//                   },
//                   new List<Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>>
//                   {
//                       new Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>(EscortVehicle.EscortVehicleType.Police, new SpawnPoint(new Vector3(1868.046f, 3677.055f, 33.71241f), 114.634f)),
//                       new Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>(EscortVehicle.EscortVehicleType.Police, new SpawnPoint(new Vector3(1874.267f, 3680.061f, 33.58729f), 112.7981f)),
//                       new Tuple<EscortVehicle.EscortVehicleType, SpawnPoint>(EscortVehicle.EscortVehicleType.Police, new SpawnPoint(new Vector3(1880.821f, 3683.542f, 33.42945f), 115.7155f)),
//                   },
//                   new Vector3[]
//                   {
//                       new Vector3(1862.5f, 2605.498f, 45.67202f),
//                       new Vector3(2523.909f, -384.1788f, 92.99274f),
//                       new Vector3(430.749f, -1022.242f, 28.79462f),
//                   }
//               ),
//            }.GetRandomElement();
//        }


//        public static Model GetPoliceVehicleModelForPosition(Vector3 position)
//        {
//            switch (position.GetArea())
//            {
//                case EWorldArea.Los_Santos:
//                    Model[] losSantosModels = { "police", "police2", "police3" };
//                    return losSantosModels.GetRandomElement();
//                case EWorldArea.Blaine_County:
//                    Model[] countyModels = { "sheriff", "sheriff2" };
//                    return countyModels.GetRandomElement();
//                default:
//                    Model[] defaultModels = { "police", "police2", "police3" };
//                    return defaultModels.GetRandomElement();
//            }
//        }

//        public static Model GetPolicePedModelForPosition(Vector3 position)
//        {
//            switch (position.GetArea())
//            {
//                case EWorldArea.Los_Santos:
//                    return Globals.Random.Next(2) == 1 ? "s_m_y_cop_01" : "s_f_y_cop_01";
//                case EWorldArea.Blaine_County:
//                    return Globals.Random.Next(2) == 1 ? "s_m_y_sheriff_01" : "s_f_y_sheriff_01";
//                default:
//                    return Globals.Random.Next(2) == 1 ? "s_m_y_cop_01" : "s_f_y_cop_01";
//            }
//        }

//        public static Model GetSecretServiceVehicleModel()
//        {
//            return new Model[] { "police4", "fbi", "fbi2" }.GetRandomElement();
//        }

//        public static Model GetSecretServicePedModel()
//        {
//            return new Model[] { "s_m_m_highsec_01", "s_m_m_highsec_02", "s_m_m_fiboffice_01", "s_m_m_fiboffice_02", "u_m_m_jewelsec_01", "ig_fbisuit_01" }.GetRandomElement();
//        }

//        public static Model GetAttackerVehicleModel()
//        {
//            return new Model[] { "baller"    , "baller2"   , "buffalo" , "buffalo2"  , "burrito3", "cavalcade", "cavalcade2", "felon"      , "fq2"     , "exemplar", "ruffian"     , "sabregt"  , "sanchez"  , "sanchez2",
//                                 "stanier"   , "washington", "pcj"     , "akuma"     , "bati"    , "patriot"  , "penumbra"  , "rapidgt"    , "rapidgt2", "jester"  , "alpha"       , "huntley"  , "massacro" , "panto"   ,
//                                 "dubsta3"   , "furoregt"  , "hakuchou", "innovation", "dukes"   , "dukes2"   , "stalion"   , "cognoscenti", "cog55"   , "baller3" , "baller4"     , "schafter3", "schafter4", "slamvan" ,
//                                 "ratloader2", "enduro"    , "guardian", "lectro"    , "kuruma"  , "kuruma2"  , "brawler"   , "vindicator" , "tampa"   , "blazer"  , "carbonizzare", "carbonrs" , "comet2"   , "daemon"  ,
//                                 "dominator" , "double"    , "dubsta"  , "dubsta2"   , "elegy2"  , "f620"     , "felon2"    , "feltzer2"   , "fugitive", "dune"    , "sandking"    , "sandking2"
//                               }.GetRandomElement();
//        }

//        public static Model GetAttackerPedModel()
//        {
//            return new Model[] { "mp_g_m_pros_01", "u_m_m_jewelthief" }.GetRandomElement();
//        }

//        public static Model GetVIPPedModel()
//        {
//            return new Model[] { "u_f_o_moviestar", "u_m_m_bankman", "u_m_m_promourn_01", "u_m_m_willyfist", "u_f_m_promourn_01", "u_f_m_miranda", "ig_bankman", "ig_bestmen", "ig_chrisformage" }.GetRandomElement();
//        }

//        public static Model GetInmatePedModel()
//        {
//            return new Model[] { "s_m_y_prisoner_01", "s_m_y_prismuscl_01", "u_m_y_prisoner_01" }.GetRandomElement();
//        }
//    }
//}