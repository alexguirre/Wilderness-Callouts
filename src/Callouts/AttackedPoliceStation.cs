namespace WildernessCallouts.Callouts
{
    using Rage;
    using Rage.Native;
    using WildernessCallouts.Types;
    using System.Collections.Generic;
    using LSPD_First_Response.Mod.Callouts;
    using LSPD_First_Response.Mod.API;
    using System;
    using System.Linq;

    [CalloutInfo("AttackedPoliceStation", CalloutProbability.Medium)]
    internal class AttackedPoliceStation : CalloutBase
    {
        AttackedPoliceStationSpawn spawnUsed;
        Blip blip;

        public override bool OnBeforeCalloutDisplayed()
        {
            spawnUsed = AttackedPoliceStationSpawn.GetSpawn();
            for (int i = 0; i < 20; i++)
            {
                Logger.LogTrivial(this.GetType().Name, "Get spawn attempt #" + i);
                if (spawnUsed.AttackersSpawnPoints.First().Position.DistanceTo(Game.LocalPlayer.Character) < 1825f &&
                    spawnUsed.AttackersSpawnPoints.First().Position.DistanceTo(Game.LocalPlayer.Character) > 62.5f)
                    break;
                spawnUsed = AttackedPoliceStationSpawn.GetSpawn();
            }
            if (spawnUsed.AttackersSpawnPoints.First().Position.DistanceTo(Game.LocalPlayer.Character) > 1825f)
            {
                Logger.LogTrivial(this.GetType().Name, "Aborting: Spawn too far");
                return false;
            }
            else if (spawnUsed.AttackersSpawnPoints.First().Position.DistanceTo(Game.LocalPlayer.Character) < 62.5f)
            {
                Logger.LogTrivial(this.GetType().Name, "Aborting: Spawn too close");
                return false;
            }

            this.CalloutMessage = "Attacked police station";
            this.CalloutPosition = spawnUsed.AttackersSpawnPoints.First().Position;
            this.ShowCalloutAreaBlipBeforeAccepting(this.CalloutPosition, 65f);

            return spawnUsed.Create();
        }

        public override bool OnCalloutAccepted()
        {
            blip = new Blip(this.CalloutPosition);
            blip.EnableRoute(blip.Color);
            GameFiber.StartNew(delegate
            {
                while (true)
                {
                    GameFiber.Yield();
                    if (Game.LocalPlayer.Character.Position.DistanceTo(this.CalloutPosition) < 105.0f)
                    {
                        foreach (Ped swat in spawnUsed.Swats)
                        {
                            NativeFunction.CallByName<uint>("TASK_AIM_GUN_AT_COORD", swat, this.CalloutPosition.X, this.CalloutPosition.Y, this.CalloutPosition.Z, -1, false, false);
                        }
                        foreach (Ped ped in World.GetAllPeds())
                        {
                            if (!ped.IsPersistent)
                                ped.ReactAndFlee(spawnUsed.Attackers.GetRandomElement());
                        }
                        break;
                    }
                }
            });
            Game.DisplayHelp("Press " + Controls.ForceCalloutEnd.ToUserFriendlyName() + " to end the callout");
            return base.OnCalloutAccepted();
        }

        public override void OnCalloutNotAccepted()
        {
            spawnUsed.Delete();
            if (blip.Exists()) blip.Delete();
            base.OnCalloutNotAccepted();
        }

        bool movedIn = false;
        bool allSuspectsKilled = false;
        public override void Process()
        {
            for (int i = 0; i < spawnUsed.RetainedPoliceAnimTasks.Count; i++)
            {
                if (spawnUsed.RetainedPoliceAnimTasks[i] != null)
                {
                    if (!spawnUsed.RetainedPoliceAnimTasks[i].IsPlaying || !spawnUsed.RetainedPoliceAnimTasks[i].IsActive)
                    {
                        NativeFunction.CallByName<uint>("TASK_GO_TO_ENTITY", spawnUsed.RetainedPoliceAnimTasks[i].Ped, spawnUsed.SwatVehicles.GetRandomElement(), -1, 7.5f, 2.0f, 0, 0);
                        spawnUsed.RetainedPoliceAnimTasks.RemoveAt(i);
                    }
                }
            }

            if (!movedIn && Game.LocalPlayer.Character.DistanceTo(this.CalloutPosition) < 100.0f)
            {
                Game.DisplayHelp("Press ~b~" + Controls.PrimaryAction.ToUserFriendlyName() + "~s~ to move in with the SWAT team");
                if (Controls.PrimaryAction.IsJustPressed())
                {
                    movedIn = true;
                    Game.LocalPlayer.Character.Tasks.PlayAnimation("swat", "come", 1.25f, AnimationFlags.SecondaryTask | AnimationFlags.UpperBodyOnly);
                    IList<Ped> swats = spawnUsed.Swats.GetRandomNumberOfElements(Globals.Random.Next(5, 8));
                    foreach (Ped swat in swats)
                    {
                        if (swat.Exists())
                        {
                            GameFiber.StartNew(delegate
                            {
                                Ped attackerToAim = spawnUsed.Attackers.GetRandomElement();
                                if (attackerToAim.Exists())
                                {
                                    swat.Tasks.GoToWhileAiming(attackerToAim, attackerToAim, 8.5f, 1.0f, true, FiringPattern.BurstFire).WaitForCompletion();
                                }
                            });
                        }
                    }
                    spawnUsed.BlockSwatsAndAttackersPermanentEvent(false);
                }
                else
                {
                    foreach (Ped attacker in spawnUsed.Attackers)
                    {
                        if(attacker != null && attacker.Exists() && attacker.IsShooting)
                            movedIn = true;
                    }

                    foreach (Ped swat in spawnUsed.Swats)
                    {
                        if (swat != null && swat.Exists() && swat.IsShooting)
                            movedIn = true;
                    }

                    if(movedIn)
                        spawnUsed.BlockSwatsAndAttackersPermanentEvent(false);
                }
            }



            if (!allSuspectsKilled && spawnUsed.Attackers.Any(p => p != null && p.Exists() && p.IsAlive && !Functions.IsPedArrested(p))) { }
            else
            {
                Game.DisplayHelp("~b~All suspects neutralized.~s~ Press " + Controls.ForceCalloutEnd.ToUserFriendlyName() + " to end the callout");
                allSuspectsKilled = true;
            }

            //WildernessCallouts.Common.StopAllNonEmergencyVehicles(spawnUsed.AttackersSpawnPoints[0].Position, 132.5f);

            base.Process();
        }

        public override void End()
        {
            //if (Settings.Despawn == 2)
            //{
                spawnUsed.SmoothCleanUp();
            //}
            //else
            //{
            //    spawnUsed.Dismiss();
            //}
            if (blip.Exists()) blip.Delete();
            base.End();
        }

        protected override void CleanUp()
        {
            // TODO: implement CleanUp()
        }
    }


    internal class AttackedPoliceStationSpawn
    {
        public uint SpeedZoneHandle;

        public RelationshipGroup AttackersRelationshipGroup;
        public RelationshipGroup SwatsRelationshipGroup;

        public List<SpawnPoint> AttackersSpawnPoints;
        public List<Ped> Attackers;

        public List<SpawnPoint> RetainedPoliceSpawnpoints;
        public List<Ped> RetainedPolice;
        public List<AnimationTask> RetainedPoliceAnimTasks;

        public List<SpawnPoint> SwatsSpawnPoints;
        public List<Ped> Swats;
        public List<SpawnPoint> SwatVehiclesSpawnPoints;
        public List<Vehicle> SwatVehicles;

        public List<SpawnPoint> PoliceSpawnPoints;
        public List<Ped> Polices;
        public List<SpawnPoint> PoliceVehiclesSpawnPoints;
        public List<Vehicle> PoliceVehicles;

        public List<SpawnPoint> PoliceBarriersSpawnPoints;
        public List<Rage.Object> PoliceBarriers;

        public AttackerType AttackersType;


        public AttackedPoliceStationSpawn(List<SpawnPoint> attackersSpawnPoints,
                                          List<SpawnPoint> retainedPoliceSpawnPoints,
                                          List<SpawnPoint> swatPedsSpawnPoints,
                                          List<SpawnPoint> swatVehiclesSpawnPoints,
                                          List<SpawnPoint> policeSpawnPoints,
                                          List<SpawnPoint> policeVehiclesSpawnPoints,
                                          List<SpawnPoint> policeBarriersSpawnPositions,
                                          AttackerType attackersType)
        {
            this.AttackersRelationshipGroup = new RelationshipGroup("ATTACKERS");
            this.SwatsRelationshipGroup = new RelationshipGroup("SWATS");

            SetRelationships(Relationship.Hate);

            this.AttackersType = attackersType;

            this.AttackersSpawnPoints = attackersSpawnPoints;
            this.RetainedPoliceSpawnpoints = retainedPoliceSpawnPoints;
            this.SwatsSpawnPoints = swatPedsSpawnPoints;
            this.SwatVehiclesSpawnPoints = swatVehiclesSpawnPoints;
            this.PoliceSpawnPoints = policeSpawnPoints;
            this.PoliceVehiclesSpawnPoints = policeVehiclesSpawnPoints;
            this.PoliceBarriersSpawnPoints = policeBarriersSpawnPositions;

            this.Attackers = new List<Ped>();
            this.RetainedPolice = new List<Ped>();
            this.RetainedPoliceAnimTasks = new List<AnimationTask>();
            this.Swats = new List<Ped>();
            this.SwatVehicles = new List<Vehicle>();
            this.Polices = new List<Ped>();
            this.PoliceVehicles = new List<Vehicle>();
            this.PoliceBarriers = new List<Rage.Object>();
        }

        public AttackedPoliceStationSpawn(List<SpawnPoint> attackersSpawnPoints,
                                          List<SpawnPoint> retainedPoliceSpawnPoints,
                                          List<SpawnPoint> swatPedsSpawnPoints,
                                          List<SpawnPoint> swatVehiclesSpawnPoints,
                                          List<SpawnPoint> policeSpawnPoints,
                                          List<SpawnPoint> policeVehiclesSpawnPoints,
                                          List<SpawnPoint> policeBarriersSpawnPositions)
            : this(attackersSpawnPoints, retainedPoliceSpawnPoints, swatPedsSpawnPoints, swatVehiclesSpawnPoints, policeSpawnPoints, policeVehiclesSpawnPoints, policeBarriersSpawnPositions, default(AttackerType).GetRandomElement<AttackerType>())
        { }


        public bool Create(bool playSound = true)
        {
            SpeedZoneHandle = World.AddSpeedZone(AttackersSpawnPoints[0].Position, 125f, 0f);

            foreach (SpawnPoint spawnPoint in AttackersSpawnPoints)
            {
                Ped ped = new Ped(GetAttackerPedModel(AttackersType), spawnPoint.Position, spawnPoint.Heading);
                if (!ped.Exists()) return false;
                ped.Health += 60;
                ped.Armor += 80;
                ped.Accuracy = Globals.Random.Next(75, 101);
                ped.BlockPermanentEvents = true;
                ped.RandomizeVariation();
                //ped.GiveNewWeapon(GetAttackerWeaponAsset(), 2000, true);
                GiveAttackerWeapon(ped);
                ped.RelationshipGroup = AttackersRelationshipGroup;
                if(Globals.Random.Next(2) == 0)
                {
                    Tuple<AnimationDictionary, string, AnimationFlags> anim = GetAttackerAnimation();
                    ped.Tasks.PlayAnimation(anim.Item1, anim.Item2, 1.0f, anim.Item3);
                }
                else if(Globals.Random.Next(3) == 1)
                {
                    ped.SetMovementAnimationSet("move_ped_crouched");
                    ped.SetStrafeAnimationSet("move_ped_crouched_strafing");
                }
                //ped.VisionRange = 9f;
                //ped.HearingRange = 9f;
                Attackers.Add(ped);
            }

            foreach (SpawnPoint spawnPoint in RetainedPoliceSpawnpoints)
            {
                Ped ped = new Ped(GetPolicePedModelForPosition(spawnPoint.Position), spawnPoint.Position, spawnPoint.Heading);
                if (!ped.Exists()) return false;
                ped.Health -= Globals.Random.Next(30, 80);
                ped.BlockPermanentEvents = true;
                Tuple<AnimationDictionary, string> anim = GetRetainedPoliceAnimation();
                //ped.Tasks.PlayAnimation(anim.Item1, anim.Item2, 1.0f, AnimationFlags.Loop);
                ped.RandomizeVariation();
                RetainedPoliceAnimTasks.Add(ped.Tasks.PlayAnimation(anim.Item1, anim.Item2, 1.0f, AnimationFlags.Loop));
                RetainedPolice.Add(ped);
            }

            foreach (SpawnPoint spawnPoint in SwatsSpawnPoints)
            {
                Ped ped = new Ped(GetSwatPedModel(), spawnPoint.Position, spawnPoint.Heading);
                if (!ped.Exists()) return false;
                ped.Health += 50;
                ped.Armor += 75;
                ped.Accuracy = Globals.Random.Next(50, 101);
                ped.BlockPermanentEvents = true;
                //EWeaponHash weaponHash = GetSwatWeaponAsset();
                //ped.GiveNewWeapon(weaponHash, 2000, true);
                GiveSwatWeapon(ped);
                if (Globals.Random.Next(3) <= 1)
                    NativeFunction.CallByName<uint>("SET_PED_PROP_INDEX", ped, 0, 0, 0, 2);
                ped.RelationshipGroup =  SwatsRelationshipGroup;
                //ped.VisionRange = 13.75f;
                //ped.HearingRange = 13.75f;
                 if (Globals.Random.Next(3) <= 1)
                {
                    ped.SetMovementAnimationSet("move_ped_crouched");
                    ped.SetStrafeAnimationSet("move_ped_crouched_strafing");
                }
                Swats.Add(ped);
            }

            foreach (SpawnPoint spawnPoint in SwatVehiclesSpawnPoints)
            {
                Vehicle veh = new Vehicle(GetSwatVehicleModel(), spawnPoint.Position, spawnPoint.Heading);
                if (!veh.Exists()) return false;
                veh.IsSirenOn = true;
                SwatVehicles.Add(veh);
            }

            foreach (SpawnPoint spawnPoint in PoliceSpawnPoints)
            {
                Ped ped = new Ped(GetPolicePedModelForPosition(spawnPoint.Position), spawnPoint.Position, spawnPoint.Heading);
                if (!ped.Exists()) return false;
                ped.Health += 10;
                ped.Armor += 30;
                ped.BlockPermanentEvents = true;
                ped.Accuracy = Globals.Random.Next(50, 101);
                ped.Inventory.GiveNewWeapon(GetPoliceWeaponAsset(), 2000, true);
                if (ped.Model == new Model("s_m_y_cop_01") || ped.Model == new Model("s_m_y_sheriff_01"))
                {
                    if (Globals.Random.Next(3) <= 1)
                        NativeFunction.CallByName<uint>("SET_PED_PROP_INDEX", ped, 0, Globals.Random.Next(2), 0, 2);

                    NativeFunction.CallByName<uint>("SET_PED_COMPONENT_VARIATION", ped, 9, 2, 0, 2);
                }
                Tuple<AnimationDictionary, string> anim = GetPoliceWaitingAnimation();
                ped.Tasks.PlayAnimation(anim.Item1, anim.Item2, 1.0f, AnimationFlags.Loop);
                Polices.Add(ped);
            }

            foreach (SpawnPoint spawnPoint in PoliceVehiclesSpawnPoints)
            {
                Vehicle veh = new Vehicle(GetPoliceVehicleModelForPosition(spawnPoint.Position), spawnPoint.Position, spawnPoint.Heading);
                if (!veh.Exists()) return false;
                veh.IsSirenOn = true;
                PoliceVehicles.Add(veh);
            }

            foreach (SpawnPoint spawnPoint in PoliceBarriersSpawnPoints)
            {
                Rage.Object obj = new Rage.Object(BarrierModel, spawnPoint.Position, spawnPoint.Heading);
                if (!obj.Exists()) return false;
                NativeFunction.CallByName<uint>("SET_ACTIVATE_OBJECT_PHYSICS_AS_SOON_AS_IT_IS_UNFROZEN", obj, true);
                PoliceBarriers.Add(obj);
            }

            if (playSound)
            {
                string gangName;
                switch (this.AttackersType)
                {
                    case AttackerType.Professional:
                        gangName = "SUSPECTS_ARE_MEMBERS_OF PROFESSIONAL ";
                        break;
                    case AttackerType.Vagos:
                        gangName = "SUSPECTS_ARE_MEMBERS_OF VAGOS ";
                        break;
                    case AttackerType.Ballas:
                        gangName = "SUSPECTS_ARE_MEMBERS_OF AFRICAN_AMERICAN ";
                        break;
                    case AttackerType.Families:
                        gangName = "SUSPECTS_ARE_MEMBERS_OF AFRICAN_AMERICAN ";
                        break;
                    case AttackerType.Lost:
                        gangName = "SUSPECTS_ARE_MEMBERS_OF LOST ";
                        break;
                    case AttackerType.Armenian:
                        gangName = "";
                        break;
                    case AttackerType.Aztecas:
                        gangName = "SUSPECTS_ARE_MEMBERS_OF MEXICAN ";
                        break;
                    case AttackerType.MarabuntaGrande:
                        gangName = "";
                        break;
                    case AttackerType.Clowns:
                        gangName = "SUSPECTS_ARE_MEMBERS_OF PROFESSIONAL ";
                        break;
                    case AttackerType.Aliens:
                        gangName = "SUSPECTS_ARE_MEMBERS_OF PROFESSIONAL ";
                        break;
                    default:
                        gangName = "";
                        break;
                }
                Functions.PlayScannerAudioUsingPosition("ATTENTION_ALL_UNITS ASSISTANCE_REQUIRED WE_HAVE " + (Globals.Random.Next(2) == 1 ? "CRIME_STATION_EMERGENCY" : "CRIME_SHOTS_FIRED") + " CRIME_MULTIPLE_OFFICERS_DOWN IN_OR_ON_POSITION " + gangName + "UNITS_RESPOND_CODE_99", Attackers.First().Position);
            }

            return true;
        }


        public void Delete()
        {
            foreach (Ped ped in Attackers)
                if (ped.Exists()) ped.Delete();

            foreach (Ped ped in RetainedPolice)
                if (ped.Exists()) ped.Delete();

            foreach (Ped ped in Swats)
                if (ped.Exists()) ped.Delete();

            foreach (Vehicle veh in SwatVehicles)
                if (veh.Exists()) veh.Delete();

            foreach (Ped ped in Polices)
                if (ped.Exists()) ped.Delete();

            foreach (Vehicle veh in PoliceVehicles)
                if (veh.Exists()) veh.Delete();

            foreach (Rage.Object obj in PoliceBarriers)
                if (obj.Exists()) obj.Delete();

            World.RemoveSpeedZone(SpeedZoneHandle);
        }


        public void Dismiss()
        {
            foreach (Ped ped in Attackers)
                if (ped.Exists()) ped.Dismiss();

            foreach (Ped ped in RetainedPolice)
                if (ped.Exists()) ped.Dismiss();

            foreach (Ped ped in Swats)
                if (ped.Exists()) ped.Dismiss();

            foreach (Vehicle veh in SwatVehicles)
                if (veh.Exists()) veh.Dismiss();

            foreach (Ped ped in Polices)
                if (ped.Exists()) ped.Dismiss();

            foreach (Vehicle veh in PoliceVehicles)
                if (veh.Exists()) veh.Dismiss();

            foreach (Rage.Object obj in PoliceBarriers)
                if (obj.Exists()) obj.Dismiss();

            World.RemoveSpeedZone(SpeedZoneHandle);
        }

        public void SmoothCleanUp()
        {
            World.RemoveSpeedZone(SpeedZoneHandle);
            foreach (Ped p in Attackers)
            {
                if (p != null && p.Exists()) p.Dismiss();
            }
            foreach (Ped p in RetainedPolice)
            {
                if (p != null && p.Exists()) p.Dismiss();
            }
            foreach (Rage.Object o in PoliceBarriers)
            {
                if (o != null && o.Exists()) o.Delete();
            }
            foreach (Ped p in Polices)
            {
                if (p != null && p.Exists())
                {
                    if (p.IsAlive)
                        p.Delete();
                    else
                        p.Dismiss();
                }
            }
            foreach (Ped p in Swats)
            {
                if (p != null && p.Exists())
                {
                    if (p.IsAlive)
                        p.Delete();
                    else
                        p.Dismiss();
                }
            }
            foreach (Vehicle v in PoliceVehicles)
            {
                Ped p = new Ped(GetPolicePedModelForPosition(v.Position), Vector3.Zero, 0f);
                p.WarpIntoVehicle(v, -1);
                if (v != null && v.Exists()) v.Dismiss();
                if (p != null && p.Exists()) p.Dismiss();
            }
            foreach (Vehicle v in SwatVehicles)
            {
                Ped p = new Ped(GetSwatPedModel(), Vector3.Zero, 0f);
                p.WarpIntoVehicle(v, -1);
                if (v != null && v.Exists()) v.Dismiss();
                if (p != null && p.Exists()) p.Dismiss();
            }
        }

        public void SetRelationships(Relationship relation)
        {
            Game.SetRelationshipBetweenRelationshipGroups(AttackersRelationshipGroup, Game.LocalPlayer.Character.RelationshipGroup, relation);
            Game.SetRelationshipBetweenRelationshipGroups(AttackersRelationshipGroup, SwatsRelationshipGroup, relation);
            Game.SetRelationshipBetweenRelationshipGroups(SwatsRelationshipGroup, AttackersRelationshipGroup, relation);
            Game.SetRelationshipBetweenRelationshipGroups(SwatsRelationshipGroup, Game.LocalPlayer.Character.RelationshipGroup, Relationship.Companion);
            Game.SetRelationshipBetweenRelationshipGroups(SwatsRelationshipGroup, "COP", Relationship.Companion);
            Game.SetRelationshipBetweenRelationshipGroups("COP", SwatsRelationshipGroup, Relationship.Companion);
        }

        public void BlockSwatsAndAttackersPermanentEvent(bool block)
        {
            foreach (Ped attacker in Attackers)
            {
                attacker.BlockPermanentEvents = block;
            }
            foreach (Ped swat in Swats)
            {
                swat.BlockPermanentEvents = block;
            }
        }



        public const uint BarrierModel = 4151651686;

        public static Tuple<AnimationDictionary, string>[] RetainedPoliceAnimations =
            {
                new Tuple<AnimationDictionary, string>("random@arrests", "kneeling_arrest_idle"),
                new Tuple<AnimationDictionary, string>("random@arrests@busted", "idle_a"),
                new Tuple<AnimationDictionary, string>("random@arrests@busted", "idle_b"),
                new Tuple<AnimationDictionary, string>("random@arrests@busted", "idle_c"),
                new Tuple<AnimationDictionary, string>("rcmminute2", "kneeling_arrest_idle"),
                new Tuple<AnimationDictionary, string>("combat@damage@rb_writhe", "rb_writhe_loop"),
                new Tuple<AnimationDictionary, string>("combat@damage@writhe", "writhe_loop"),
                new Tuple<AnimationDictionary, string>("combat@damage@writheidle_a", "writhe_idle_a"),
                new Tuple<AnimationDictionary, string>("combat@damage@writheidle_a", "writhe_idle_b"),
                new Tuple<AnimationDictionary, string>("combat@damage@writheidle_a", "writhe_idle_c"),
                new Tuple<AnimationDictionary, string>("combat@damage@writheidle_b", "writhe_idle_d"),
                new Tuple<AnimationDictionary, string>("combat@damage@writheidle_b", "writhe_idle_e"),
                new Tuple<AnimationDictionary, string>("combat@damage@writheidle_b", "writhe_idle_f"),
                new Tuple<AnimationDictionary, string>("combat@damage@writheidle_c", "writhe_idle_g"),
            };

        public static Tuple<AnimationDictionary, string> GetRetainedPoliceAnimation()
        {
            return RetainedPoliceAnimations.GetRandomElement();
        }

        public static Tuple<AnimationDictionary, string> GetPoliceWaitingAnimation()
        {
            Tuple<AnimationDictionary, string>[] policeWaitingAnims =
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
            return policeWaitingAnims.GetRandomElement();
        }

        public static Tuple<AnimationDictionary, string, AnimationFlags> GetAttackerAnimation()
        {
            Tuple<AnimationDictionary, string, AnimationFlags>[] anims =
            {
                        new Tuple<AnimationDictionary, string, AnimationFlags>("missexile2", "crouching_idle_a", AnimationFlags.StayInEndFrame),
                        new Tuple<AnimationDictionary, string, AnimationFlags>("missexile2", "crouching_idle_b", AnimationFlags.StayInEndFrame),
                        new Tuple<AnimationDictionary, string, AnimationFlags>("missexile2", "crouching_idle_c", AnimationFlags.StayInEndFrame),
                        new Tuple<AnimationDictionary, string, AnimationFlags>("move_stealth@generic@core", "idle", AnimationFlags.Loop),
            };
            return anims.GetRandomElement();
        }

        public static AttackedPoliceStationSpawn GetSpawn()
        {
            List<AttackedPoliceStationSpawn> spawns = new List<AttackedPoliceStationSpawn>()
            {
            //new AttackedPoliceStationSpawn
            //   (
            //       new List<SpawnPoint>()
            //       {
            //           new SpawnPoint(new Vector3(436.6623f, -986.863f, 30.68966f), 330.8286f),
            //           new SpawnPoint(new Vector3(440.3943f, -976.3862f, 30.68966f), 241.1066f),
            //           new SpawnPoint(new Vector3(442.93f, -975.5139f, 30.68966f), 165.7192f),
            //           new SpawnPoint(new Vector3(444.1697f, -988.9327f, 30.68966f), 345.0877f),
            //           new SpawnPoint(new Vector3(450.8627f, -979.3975f, 30.68966f), 185.4222f),
            //           new SpawnPoint(new Vector3(452.1799f, -979.0597f, 30.68966f), 160.886f),
            //           new SpawnPoint(new Vector3(460.0169f, -986.02f, 30.68963f), 88.75278f),
            //           new SpawnPoint(new Vector3(468.7693f, -987.501f, 30.68966f), 2.19288f),
            //           new SpawnPoint(new Vector3(467.6851f, -982.2445f, 35.06125f), 201.6023f),
            //           new SpawnPoint(new Vector3(470.8329f, -984.6691f, 37.89198f), 89.19719f),
            //           new SpawnPoint(new Vector3(469.8322f, -982.5004f, 37.89198f), 160.4029f),
            //           new SpawnPoint(new Vector3(465.7173f, -983.3814f, 39.89196f), 322.4502f),
            //           new SpawnPoint(new Vector3(464.9882f, -986.4318f, 43.6919f), 193.0286f),
            //           new SpawnPoint(new Vector3(466.1064f, -980.5866f, 43.69181f), 97.68919f),
            //           new SpawnPoint(new Vector3(458.9639f, -982.9713f, 43.6919f), 263.3564f),
            //       },
            //       new List<SpawnPoint>()
            //       {
            //           new SpawnPoint(new Vector3(437.2939f, -984.4728f, 30.68966f), 185.0349f),
            //           new SpawnPoint(new Vector3(436.7341f, -980.5298f, 30.68966f), 21.0857f),
            //           new SpawnPoint(new Vector3(441.899f, -976.7963f, 30.68966f), 159.7155f),
            //           new SpawnPoint(new Vector3(451.3734f, -980.3738f, 30.68966f), 196.9419f),
            //           new SpawnPoint(new Vector3(456.8322f, -985.9755f, 30.68966f), 261.5491f),
            //           new SpawnPoint(new Vector3(468.2917f, -985.0775f, 30.68966f), 269.2032f),
            //           new SpawnPoint(new Vector3(469.4477f, -984.7515f, 37.89205f), 92.64952f),
            //           new SpawnPoint(new Vector3(465.3507f, -984.8627f, 39.89192f), 347.6629f),
            //           new SpawnPoint(new Vector3(460.4086f, -983.5534f, 43.6919f), 170.6487f),
            //       },
            //       new List<SpawnPoint>()
            //       {
            //           new SpawnPoint(new Vector3(431.1196f, -957.8232f, 29.16844f), 183.1692f),
            //           new SpawnPoint(new Vector3(425.6425f, -957.6705f, 29.25843f), 191.12f),
            //           new SpawnPoint(new Vector3(422.4581f, -958.7859f, 29.25376f), 206.332f),
            //           new SpawnPoint(new Vector3(412.9815f, -970.7454f, 29.45987f), 238.8255f),
            //           new SpawnPoint(new Vector3(411.2886f, -974.2121f, 29.42495f), 231.2271f),
            //           new SpawnPoint(new Vector3(410.0948f, -976.2499f, 29.4202f), 245.9001f),
            //           new SpawnPoint(new Vector3(409.4464f, -978.6313f, 29.26689f), 267.2143f),
            //           new SpawnPoint(new Vector3(409.355f, -982.9523f, 29.26752f), 276.6988f),
            //           new SpawnPoint(new Vector3(409.3022f, -985.1205f, 29.26643f), 267.1706f),
            //           new SpawnPoint(new Vector3(410.5418f, -990.7231f, 29.26679f), 292.3515f),
            //           new SpawnPoint(new Vector3(401.6042f, -974.7772f, 29.39095f), 252.8463f),
            //           new SpawnPoint(new Vector3(398.9699f, -980.4969f, 29.39561f), 275.4959f),
            //       },
            //       new List<SpawnPoint>()
            //       {
            //           new SpawnPoint(new Vector3(437.0532f, -958.5436f, 29.00258f), 83.91307f),
            //           new SpawnPoint(new Vector3(401.9706f, -970.5714f, 29.41397f), 159.1244f),
            //           new SpawnPoint(new Vector3(400.9364f, -985.5635f, 29.4384f), 175.1434f),
            //           new SpawnPoint(new Vector3(401.7344f, -998.0091f, 29.4108f), 201.6676f),
            //           new SpawnPoint(new Vector3(396.429f, -975.683f, 29.29009f), 5.890408f),
            //       },
            //       new List<SpawnPoint>()
            //       {
            //           new SpawnPoint(new Vector3(401.9053f, -1032.393f, 29.3772f), 182.0199f),
            //           new SpawnPoint(new Vector3(396.9883f, -1030.307f, 29.46629f), 156.7717f),
            //           new SpawnPoint(new Vector3(393.5987f, -1029.263f, 29.33487f), 178.0375f),
            //           new SpawnPoint(new Vector3(392.9706f, -955.8152f, 29.34177f), 99.22005f),
            //           new SpawnPoint(new Vector3(402.9738f, -944.7916f, 29.45936f), 358.4386f),
            //           new SpawnPoint(new Vector3(491.4264f, -955.6074f, 27.24535f), 273.217f),
            //           new SpawnPoint(new Vector3(491.4718f, -947.8725f, 27.01381f), 290.6429f),
            //           new SpawnPoint(new Vector3(411.248f, -975.5434f, 29.42402f), 254.5661f),
            //       },
            //       new List<SpawnPoint>()
            //       {
            //           new SpawnPoint(new Vector3(391.4874f, -1030.81f, 29.2871f), 253.6873f),
            //           new SpawnPoint(new Vector3(403.8847f, -1034.658f, 29.25077f), 89.94921f),
            //           new SpawnPoint(new Vector3(397.8625f, -1033.094f, 29.4792f), 64.95369f),
            //           new SpawnPoint(new Vector3(392.7881f, -960.3142f, 29.31052f), 7.208962f),
            //           new SpawnPoint(new Vector3(399.1774f, -943.3303f, 29.32905f), 269.5867f),
            //           new SpawnPoint(new Vector3(406.2994f, -943.5059f, 29.38624f), 270.5184f),
            //           new SpawnPoint(new Vector3(491.4026f, -951.4388f, 27.11779f), 208.6954f),
            //           new SpawnPoint(new Vector3(490.0321f, -957.2998f, 27.24946f), 214.2032f),
            //       },
            //       new List<SpawnPoint>()
            //       {
            //           new SpawnPoint(new Vector3(394.0874f, -1033.327f, 29.36267f), 159.2673f),
            //           new SpawnPoint(new Vector3(400.3203f, -1036.1f, 29.41126f), 168.2882f),
            //           new SpawnPoint(new Vector3(391.1473f, -954.0898f, 29.32382f), 103.8725f),
            //           new SpawnPoint(new Vector3(391.4529f, -956.3496f, 29.36847f), 95.55065f),
            //           new SpawnPoint(new Vector3(493.0846f, -960.3256f, 27.1436f), 261.1202f),
            //           new SpawnPoint(new Vector3(493.1128f, -956.4491f, 27.21273f), 268.9622f),
            //           new SpawnPoint(new Vector3(494.0509f, -948.3645f, 26.96673f), 277.3596f),
            //           new SpawnPoint(new Vector3(402.9321f, -941.3825f, 29.45687f), 356.8088f),
            //           new SpawnPoint(new Vector3(418.5597f, -1002.476f, 29.23066f), 287.1441f),
            //           new SpawnPoint(new Vector3(416.6737f, -999.6423f, 29.29818f), 301.2873f),
            //           new SpawnPoint(new Vector3(414.741f, -996.3874f, 29.37891f), 290.5032f),
            //           new SpawnPoint(new Vector3(412.857f, -992.9702f, 29.40125f), 281.1572f),
            //           new SpawnPoint(new Vector3(411.47f, -989.3522f, 29.41443f), 280.6288f),
            //           new SpawnPoint(new Vector3(410.8432f, -985.1386f, 29.26694f), 285.1821f),
            //           new SpawnPoint(new Vector3(410.9813f, -982.817f, 29.42576f), 267.0931f),
            //           new SpawnPoint(new Vector3(410.8059f, -978.9704f, 29.41672f), 267.1415f),
            //           new SpawnPoint(new Vector3(411.7662f, -975.7675f, 29.42683f), 232.6518f),
            //           new SpawnPoint(new Vector3(414.1124f, -971.4795f, 29.44825f), 240.1805f),
            //           new SpawnPoint(new Vector3(416.7765f, -966.3687f, 29.44782f), 229.2529f),
            //           new SpawnPoint(new Vector3(419.6619f, -962.8959f, 29.4076f), 213.6322f),
            //           new SpawnPoint(new Vector3(423.1696f, -960.4011f, 29.19312f), 196.8682f),
            //           new SpawnPoint(new Vector3(427.1134f, -958.8483f, 29.18066f), 186.7351f),
            //           new SpawnPoint(new Vector3(431.5353f, -958.9437f, 29.10951f), 176.3036f),
            //           new SpawnPoint(new Vector3(439.6034f, -962.4333f, 29.0354f), 91.86531f),
            //           new SpawnPoint(new Vector3(439.8716f, -965.0589f, 29.03833f), 88.51136f),
            //       }
            //   ),


            new AttackedPoliceStationSpawn
               (
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(1851.032f, 3683.885f, 34.26704f), 252.0502f),
                       new SpawnPoint(new Vector3(1858.772f, 3687.24f, 34.26704f), 202.0881f),
                       new SpawnPoint(new Vector3(1856.516f, 3688.389f, 34.26704f), 140.4383f),
                       new SpawnPoint(new Vector3(1853.078f, 3689.354f, 34.26704f), 205.4573f),
                       new SpawnPoint(new Vector3(1852.043f, 3689.753f, 34.26704f), 218.6511f),
                       new SpawnPoint(new Vector3(1857.243f, 3687.353f, 34.26704f), 104.5584f),
                       new SpawnPoint(new Vector3(1848.785f, 3689.824f, 34.26709f), 207.6698f),
                       new SpawnPoint(new Vector3(1851.234f, 3688.653f, 34.26709f), 208.7993f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(1851.914f, 3690.794f, 34.26702f), 299.4998f),
                       new SpawnPoint(new Vector3(1851.268f, 3685.73f, 34.26704f), 211.3231f),
                       new SpawnPoint(new Vector3(1856.012f, 3686.711f, 34.26704f), 293.8889f),
                       new SpawnPoint(new Vector3(1849.695f, 3688.292f, 34.26709f), 215.5713f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(1859.078f, 3658.201f, 33.97068f), 15.77699f),
                       new SpawnPoint(new Vector3(1862.354f, 3659.227f, 33.88576f), 9.567956f),
                       new SpawnPoint(new Vector3(1865.318f, 3660.347f, 33.85227f), 346.6862f),
                       new SpawnPoint(new Vector3(1869.07f, 3662.706f, 33.7597f), 31.91849f),
                       new SpawnPoint(new Vector3(1871.037f, 3664.7f, 33.69482f), 29.04946f),
                       new SpawnPoint(new Vector3(1867.389f, 3660.702f, 33.84195f), 39.16327f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(1875.877f, 3668.82f, 33.66734f), 321.9545f),
                       new SpawnPoint(new Vector3(1853.236f, 3658.335f, 34.14433f), 125.4708f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(1883.949f, 3678.689f, 33.45856f), 295.6777f),
                       new SpawnPoint(new Vector3(1882.902f, 3680.379f, 33.45938f), 304.5379f),
                       new SpawnPoint(new Vector3(1844.811f, 3661.05f, 34.21537f), 120.6198f),
                       new SpawnPoint(new Vector3(1841.863f, 3667.122f, 33.67992f), 106.0338f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(1880f, 3682.965f, 33.45129f), 219.5354f),
                       new SpawnPoint(new Vector3(1884.264f, 3674.737f, 33.46067f), 197.3299f),
                       new SpawnPoint(new Vector3(1847.328f, 3655.259f, 34.22274f), 50.186f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(1877.429f, 3686.859f, 33.45501f), 123.2356f),
                       new SpawnPoint(new Vector3(1875.921f, 3689.398f, 33.47699f), 150.334f),
                       new SpawnPoint(new Vector3(1873.212f, 3690.125f, 33.56828f), 187.5453f),
                       new SpawnPoint(new Vector3(1869.63f, 3689.064f, 33.70362f), 200.269f),
                       new SpawnPoint(new Vector3(1866.071f, 3688.637f, 34.26752f), 167.4612f),
                       new SpawnPoint(new Vector3(1883.81f, 3681.495f, 33.42762f), 306.4681f),
                       new SpawnPoint(new Vector3(1884.876f, 3678.94f, 33.43597f), 284.3244f),
                       new SpawnPoint(new Vector3(1870.422f, 3665.958f, 33.79403f), 31.80882f),
                       new SpawnPoint(new Vector3(1867.824f, 3663.913f, 33.84941f), 36.18421f),
                       new SpawnPoint(new Vector3(1864.959f, 3661.844f, 33.87295f), 35.9535f),
                       new SpawnPoint(new Vector3(1861.821f, 3660.543f, 33.97791f), 12.46349f),
                       new SpawnPoint(new Vector3(1858.494f, 3659.845f, 34.04654f), 356.9864f),
                       new SpawnPoint(new Vector3(1844.297f, 3658.473f, 34.24328f), 117.9504f),
                       new SpawnPoint(new Vector3(1843.296f, 3661.234f, 34.21858f), 120.7557f),
                       new SpawnPoint(new Vector3(1841.755f, 3663.643f, 33.98872f), 112.9376f),
                       new SpawnPoint(new Vector3(1840.617f, 3665.843f, 33.68673f), 112.6167f),
                       new SpawnPoint(new Vector3(1840.059f, 3668.632f, 33.67997f), 99.3411f),
                       new SpawnPoint(new Vector3(1841.442f, 3673.655f, 34.27674f), 107.251f),
                   }
               ),


            new AttackedPoliceStationSpawn
               (
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-448.3679f, 6018.724f, 31.71639f), 202.14f),
                       new SpawnPoint(new Vector3(-449.3257f, 6017.603f, 31.71639f), 237.6015f),
                       new SpawnPoint(new Vector3(-442.2493f, 6012.767f, 31.71639f), 53.78259f),
                       new SpawnPoint(new Vector3(-448.9426f, 6012.62f, 31.71639f), 290.5265f),
                       new SpawnPoint(new Vector3(-448.1504f, 6011.785f, 31.71639f), 312.3767f),
                       new SpawnPoint(new Vector3(-449.2028f, 6015.739f, 31.71637f), 253.4126f),
                       new SpawnPoint(new Vector3(-447.7132f, 6010.91f, 31.71637f), 319.7569f),
                       new SpawnPoint(new Vector3(-446.836f, 6009.169f, 31.71637f), 324.8179f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-447.9267f, 6017.396f, 31.71639f), 238.0141f),
                       new SpawnPoint(new Vector3(-449.4382f, 6011.391f, 31.71639f), 142.1815f),
                       new SpawnPoint(new Vector3(-444.8577f, 6013.931f, 31.71637f), 337.8516f),
                       new SpawnPoint(new Vector3(-447.7678f, 6008.626f, 31.71637f), 311.2274f),
                       new SpawnPoint(new Vector3(-445.8362f, 6011.214f, 31.71637f), 284.3495f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-425.3512f, 6041.246f, 31.36176f), 152.7503f),
                       new SpawnPoint(new Vector3(-420.6257f, 6037.792f, 31.33756f), 112.1522f),
                       new SpawnPoint(new Vector3(-418.5031f, 6033.83f, 31.33375f), 108.8022f),
                       new SpawnPoint(new Vector3(-416.7851f, 6029.085f, 31.36f), 104.1697f),
                       new SpawnPoint(new Vector3(-425.7387f, 6022.056f, 31.48967f), 103.3429f),
                       new SpawnPoint(new Vector3(-423.836f, 6016.896f, 31.49114f), 88.8188f),
                       new SpawnPoint(new Vector3(-449.398f, 6042.554f, 31.49011f), 205.6251f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-423.62f, 6048.834f, 31.44567f), 228.9969f),
                       new SpawnPoint(new Vector3(-416.989f, 6037.422f, 31.23321f), 201.7012f),
                       new SpawnPoint(new Vector3(-416.0622f, 6023.662f, 31.44681f), 184.4984f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-424.8755f, 6067.267f, 31.37431f), 27.78552f),
                       new SpawnPoint(new Vector3(-430.2122f, 6065.04f, 31.496f), 23.84298f),
                       new SpawnPoint(new Vector3(-436.2278f, 6062.566f, 31.36327f), 17.0085f),
                       new SpawnPoint(new Vector3(-414.8715f, 6003.577f, 31.4332f), 190.3415f),
                       new SpawnPoint(new Vector3(-408.7881f, 6003.862f, 31.62737f), 182.025f),
                       new SpawnPoint(new Vector3(-398.2434f, 6014.582f, 31.34376f), 229.1633f),
                       new SpawnPoint(new Vector3(-391.5805f, 6022.106f, 31.47785f), 229.9987f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-393.7061f, 6023.562f, 31.42616f), 135.5327f),
                       new SpawnPoint(new Vector3(-400.3967f, 6016.703f, 31.32981f), 289.8131f),
                       new SpawnPoint(new Vector3(-406.9794f, 6009.057f, 31.5689f), 139.7295f),
                       new SpawnPoint(new Vector3(-425.6051f, 6063.43f, 31.43303f), 110.6782f),
                       new SpawnPoint(new Vector3(-436.5782f, 6059.288f, 31.39129f), 119.9935f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-441.5732f, 6061.741f, 31.48945f), 24.66955f),
                       new SpawnPoint(new Vector3(-438.1504f, 6063.457f, 31.37346f), 19.74373f),
                       new SpawnPoint(new Vector3(-434.645f, 6064.777f, 31.35868f), 19.39372f),
                       new SpawnPoint(new Vector3(-431.9732f, 6065.88f, 31.45127f), 16.91852f),
                       new SpawnPoint(new Vector3(-428.7206f, 6067.079f, 31.48954f), 23.70819f),
                       new SpawnPoint(new Vector3(-425.2982f, 6068.647f, 31.38035f), 24.94105f),
                       new SpawnPoint(new Vector3(-422.4173f, 6069.612f, 31.3312f), 19.39044f),
                       new SpawnPoint(new Vector3(-388.0042f, 6023.549f, 31.42455f), 227.9378f),
                       new SpawnPoint(new Vector3(-390.7841f, 6020.86f, 31.4923f), 231.1984f),
                       new SpawnPoint(new Vector3(-393.2885f, 6018.458f, 31.43574f), 222.9673f),
                       new SpawnPoint(new Vector3(-395.3441f, 6015.687f, 31.27658f), 229.0666f),
                       new SpawnPoint(new Vector3(-397.5353f, 6013.069f, 31.36599f), 240.9894f),
                       new SpawnPoint(new Vector3(-408.633f, 6001.824f, 31.64309f), 179.9966f),
                       new SpawnPoint(new Vector3(-411.5006f, 6001.853f, 31.60708f), 203.6282f),
                       new SpawnPoint(new Vector3(-414.2379f, 6002.267f, 31.48258f), 189.5156f),
                       new SpawnPoint(new Vector3(-419.0914f, 6031.336f, 31.39218f), 117.9084f),
                       new SpawnPoint(new Vector3(-420.2052f, 6034.342f, 31.3772f), 115.4696f),
                       new SpawnPoint(new Vector3(-422.1301f, 6037.356f, 31.37354f), 125.5225f),
                       new SpawnPoint(new Vector3(-418.1718f, 6028.281f, 31.4154f), 94.70718f),
                       new SpawnPoint(new Vector3(-425.6735f, 6039.976f, 31.36635f), 160.0769f),
                   }
               ),


            new AttackedPoliceStationSpawn
               (
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(374.2935f, -1609.047f, 29.29195f), 222.6017f),
                       new SpawnPoint(new Vector3(368.473f, -1611.874f, 29.29193f), 317.5644f),
                       new SpawnPoint(new Vector3(366.9693f, -1621.381f, 29.29193f), 262.9149f),
                       new SpawnPoint(new Vector3(351.4794f, -1600.981f, 33.29198f), 77.40508f),
                       new SpawnPoint(new Vector3(363.2527f, -1612.36f, 33.292f), 257.361f),
                       new SpawnPoint(new Vector3(353.0677f, -1611.531f, 29.29193f), 52.51406f),
                       new SpawnPoint(new Vector3(359.0094f, -1615.627f, 29.29193f), 223.6546f),
                       new SpawnPoint(new Vector3(380.6143f, -1612.442f, 29.29195f), 289.3172f),
                       new SpawnPoint(new Vector3(392.2752f, -1634.914f, 29.29195f), 36.0217f),
                       new SpawnPoint(new Vector3(365.4604f, -1621.235f, 29.29196f), 231.9385f),
                       new SpawnPoint(new Vector3(365.9709f, -1609.831f, 36.94878f), 236.7208f),
                       new SpawnPoint(new Vector3(374.6783f, -1604.803f, 36.94879f), 190.0756f),
                       new SpawnPoint(new Vector3(381.4191f, -1607.972f, 36.94878f), 189.804f),
                       new SpawnPoint(new Vector3(377.7356f, -1606.5f, 36.94878f), 162.732f),
                       new SpawnPoint(new Vector3(353.416f, -1600.43f, 36.9488f), 137.3146f),
                       new SpawnPoint(new Vector3(365.7072f, -1616.943f, 32.80798f), 278.0872f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(371.1383f, -1609.519f, 29.29196f), 54.23386f),
                       new SpawnPoint(new Vector3(364.6891f, -1615.865f, 29.29195f), 132.8049f),
                       new SpawnPoint(new Vector3(355.9309f, -1613.549f, 29.29193f), 47.72617f),
                       new SpawnPoint(new Vector3(389.9864f, -1618.516f, 29.29195f), 317.1303f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(403.5967f, -1586.981f, 29.34153f), 156.3742f),
                       new SpawnPoint(new Vector3(406.4241f, -1589.211f, 29.34152f), 143.2007f),
                       new SpawnPoint(new Vector3(409.7312f, -1591.811f, 29.34152f), 132.4034f),
                       new SpawnPoint(new Vector3(411.5991f, -1593.864f, 29.34153f), 135.0843f),
                       new SpawnPoint(new Vector3(413.9136f, -1595.516f, 29.34153f), 122.4727f),
                       new SpawnPoint(new Vector3(417.8396f, -1610.12f, 29.22687f), 89.04935f),
                       new SpawnPoint(new Vector3(417.6907f, -1607.461f, 29.30204f), 83.31256f),
                       new SpawnPoint(new Vector3(417.1078f, -1604.132f, 29.34227f), 96.52458f),
                       new SpawnPoint(new Vector3(370.3726f, -1577.024f, 29.29218f), 110.2351f),
                       new SpawnPoint(new Vector3(360.4466f, -1571.988f, 29.27127f), 182.4514f),
                       new SpawnPoint(new Vector3(393.4408f, -1584.15f, 29.34153f), 201.993f),
                       new SpawnPoint(new Vector3(389.4992f, -1585.704f, 29.28465f), 197.6916f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(364.7086f, -1574.535f, 29.25965f), 59.25272f),
                       new SpawnPoint(new Vector3(399.0367f, -1586.057f, 29.34154f), 253.2463f),
                       new SpawnPoint(new Vector3(416.2692f, -1599.511f, 29.34153f), 198.0937f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(337.9445f, -1539.68f, 29.22054f), 37.23985f),
                       new SpawnPoint(new Vector3(342.6927f, -1533.126f, 29.33681f), 46.47039f),
                       new SpawnPoint(new Vector3(345.3785f, -1529.896f, 29.33878f), 61.79781f),
                       new SpawnPoint(new Vector3(350.972f, -1523.932f, 29.14769f), 39.92362f),
                       new SpawnPoint(new Vector3(357.0216f, -1521.799f, 29.25151f), 38.47596f),
                       new SpawnPoint(new Vector3(334.4433f, -1548.55f, 29.28993f), 49.35203f),
                       new SpawnPoint(new Vector3(398.6805f, -1555.556f, 29.29158f), 305.2665f),
                       new SpawnPoint(new Vector3(384.4448f, -1541.521f, 29.29912f), 321.1136f),
                       new SpawnPoint(new Vector3(409.8868f, -1562.727f, 29.29158f), 314.1325f),
                       new SpawnPoint(new Vector3(448.2702f, -1613.312f, 29.34613f), 229.4003f),
                       new SpawnPoint(new Vector3(437.2985f, -1624.428f, 29.3288f), 233.2472f),
                       new SpawnPoint(new Vector3(442.8572f, -1618.917f, 29.34153f), 226.7523f),
                       new SpawnPoint(new Vector3(426.6026f, -1628.727f, 29.29802f), 186.2956f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(336.0379f, -1543.645f, 29.14737f), 347.9713f),
                       new SpawnPoint(new Vector3(340.0652f, -1535.798f, 29.30792f), 169.7016f),
                       new SpawnPoint(new Vector3(348.1663f, -1526.703f, 29.32561f), 315.7475f),
                       new SpawnPoint(new Vector3(413.5186f, -1565.589f, 29.29159f), 217.3645f),
                       new SpawnPoint(new Vector3(451.1183f, -1609.942f, 29.25469f), 137.3567f),
                       new SpawnPoint(new Vector3(445.137f, -1616.379f, 29.49361f), 317.5185f),
                       new SpawnPoint(new Vector3(440.8442f, -1621.606f, 29.34144f), 138.7016f),
                       new SpawnPoint(new Vector3(432.7952f, -1628.687f, 29.19026f), 116.1608f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(412.9089f, -1596.085f, 29.34154f), 140.7626f),
                       new SpawnPoint(new Vector3(410.5629f, -1594.158f, 29.34153f), 135.0978f),
                       new SpawnPoint(new Vector3(406.6809f, -1591.382f, 29.34153f), 136.4639f),
                       new SpawnPoint(new Vector3(403.808f, -1589.022f, 29.34153f), 166.2796f),
                       new SpawnPoint(new Vector3(409.0057f, -1592.626f, 29.34152f), 132.8419f),
                       new SpawnPoint(new Vector3(416.3248f, -1604.954f, 29.34152f), 92.30659f),
                       new SpawnPoint(new Vector3(416.4118f, -1607.456f, 29.27039f), 93.9753f),
                       new SpawnPoint(new Vector3(416.6122f, -1610.165f, 29.195f), 88.57597f),
                       new SpawnPoint(new Vector3(337.3935f, -1538.321f, 29.23121f), 36.60664f),
                       new SpawnPoint(new Vector3(335.5201f, -1540.203f, 29.17544f), 38.29533f),
                       new SpawnPoint(new Vector3(342.3579f, -1531.721f, 29.3331f), 47.02562f),
                       new SpawnPoint(new Vector3(344.674f, -1529.166f, 29.33544f), 48.61793f),
                       new SpawnPoint(new Vector3(352.1639f, -1522.277f, 29.14138f), 35.31634f),
                       new SpawnPoint(new Vector3(349.9187f, -1523.293f, 29.16507f), 39.12198f),
                       new SpawnPoint(new Vector3(355.8753f, -1520.144f, 29.24062f), 20.57732f),
                       new SpawnPoint(new Vector3(333.6507f, -1547.884f, 29.28993f), 50.8861f),
                       new SpawnPoint(new Vector3(369.4709f, -1577.338f, 29.29197f), 113.9404f),
                       new SpawnPoint(new Vector3(360.5413f, -1572.797f, 29.27978f), 178.2888f),
                       new SpawnPoint(new Vector3(358.0539f, -1572.334f, 29.29213f), 138.6136f),
                       new SpawnPoint(new Vector3(355.8132f, -1570.389f, 29.29215f), 134.1975f),
                       new SpawnPoint(new Vector3(353.4261f, -1568.269f, 29.29158f), 138.0123f),
                       new SpawnPoint(new Vector3(399.4236f, -1554.789f, 29.29157f), 314.5045f),
                       new SpawnPoint(new Vector3(386.0257f, -1541.866f, 29.33911f), 320.3355f),
                       new SpawnPoint(new Vector3(384.3059f, -1540.184f, 29.33914f), 322.2937f),
                       new SpawnPoint(new Vector3(410.6292f, -1562.018f, 29.26181f), 312.3065f),
                       new SpawnPoint(new Vector3(448.988f, -1614.028f, 29.34249f), 225.7607f),
                       new SpawnPoint(new Vector3(436.7654f, -1626.091f, 29.44654f), 210.0793f),
                       new SpawnPoint(new Vector3(443.8803f, -1619.857f, 29.34054f), 228.4675f),
                       new SpawnPoint(new Vector3(426.7043f, -1629.895f, 29.24697f), 183.8396f),
                       new SpawnPoint(new Vector3(393.5482f, -1584.923f, 29.34153f), 197.952f),
                       new SpawnPoint(new Vector3(390.4306f, -1586.619f, 29.28016f), 212.1211f),
                   }
               ),


            new AttackedPoliceStationSpawn
               (
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-1119.626f, -854.8896f, 19.69341f), 359.891f),
                       new SpawnPoint(new Vector3(-1108.539f, -843.8959f, 19.31697f), 89.36618f),
                       new SpawnPoint(new Vector3(-1107.299f, -845.2581f, 19.31697f), 156.3215f),
                       new SpawnPoint(new Vector3(-1127.267f, -832.555f, 13.40423f), 160.8237f),
                       new SpawnPoint(new Vector3(-1123.326f, -856.7833f, 13.5194f), 73.57937f),
                       new SpawnPoint(new Vector3(-1129.787f, -848.3841f, 13.51203f), 79.72753f),
                       new SpawnPoint(new Vector3(-1126.346f, -871.3105f, 10.78179f), 185.9914f),
                       new SpawnPoint(new Vector3(-1045.832f, -854.7257f, 4.875758f), 152.9066f),
                       new SpawnPoint(new Vector3(-1032.558f, -847.7457f, 8.319414f), 94.34621f),
                       new SpawnPoint(new Vector3(-1046.144f, -838.5722f, 10.84439f), 147.5901f),
                       new SpawnPoint(new Vector3(-1086.917f, -860.4066f, 10.58766f), 116.3587f),
                       new SpawnPoint(new Vector3(-1099.418f, -852.0315f, 13.18699f), 128.0565f),
                       new SpawnPoint(new Vector3(-1097.475f, -853.0424f, 13.18714f), 186.4164f),
                       new SpawnPoint(new Vector3(-1074.252f, -805.6449f, 31.2654f), 336.5049f),
                       new SpawnPoint(new Vector3(-1089.816f, -809.6713f, 31.26468f), 35.27623f),
                       new SpawnPoint(new Vector3(-1112.095f, -825.96f, 27.62614f), 16.58598f),
                       new SpawnPoint(new Vector3(-1099.869f, -826.0234f, 37.67538f), 35.68511f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-1096.732f, -809.0829f, 19.30742f), 121.5725f),
                       new SpawnPoint(new Vector3(-1119.848f, -828.0716f, 19.31637f), 133.739f),
                       new SpawnPoint(new Vector3(-1118.534f, -840.6846f, 19.31649f), 189.1368f),
                       new SpawnPoint(new Vector3(-1128.347f, -835.098f, 13.49516f), 131.2773f),
                       new SpawnPoint(new Vector3(-1058.043f, -856.3781f, 4.870575f), 248.3551f),
                       new SpawnPoint(new Vector3(-1045.557f, -856.0917f, 4.877672f), 264.2939f),
                       new SpawnPoint(new Vector3(-1102.096f, -829.6696f, 37.67537f), 155.7913f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-1165.171f, -828.9357f, 14.20871f), 227.3327f),
                       new SpawnPoint(new Vector3(-1162.934f, -827.0236f, 14.25027f), 240.1212f),
                       new SpawnPoint(new Vector3(-1153.841f, -817.4789f, 14.69347f), 217.1635f),
                       new SpawnPoint(new Vector3(-1151.814f, -815.9197f, 14.81753f), 216.8609f),
                       new SpawnPoint(new Vector3(-1129.418f, -797.1365f, 16.80474f), 211.9545f),
                       new SpawnPoint(new Vector3(-1131.27f, -798.6464f, 16.6724f), 216.8854f),
                       new SpawnPoint(new Vector3(-1134.038f, -801.1682f, 16.3793f), 224.506f),
                       new SpawnPoint(new Vector3(-1136.188f, -803.5994f, 16.12809f), 225.8427f),
                       new SpawnPoint(new Vector3(-1104.038f, -775.7836f, 19.24877f), 189.9002f),
                       new SpawnPoint(new Vector3(-1111.53f, -780.8326f, 18.76744f), 209.7625f),
                       new SpawnPoint(new Vector3(-1077.704f, -770.1489f, 19.33295f), 165.2781f),
                       new SpawnPoint(new Vector3(-1079.925f, -768.9001f, 19.36324f), 160.1176f),
                       new SpawnPoint(new Vector3(-1084.051f, -768.789f, 19.35451f), 172.2373f),
                       new SpawnPoint(new Vector3(-1064.199f, -778.1226f, 19.34246f), 145.929f),
                       new SpawnPoint(new Vector3(-1118.327f, -788.0053f, 18.07574f), 221.4954f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-1083.347f, -765.7906f, 19.36413f), 258.4787f),
                       new SpawnPoint(new Vector3(-1067.882f, -777.1677f, 19.34292f), 222.5787f),
                       new SpawnPoint(new Vector3(-1108.3f, -778.605f, 19.02253f), 127.2286f),
                       new SpawnPoint(new Vector3(-1122.655f, -791.4244f, 17.60853f), 126.673f),
                       new SpawnPoint(new Vector3(-1142.295f, -808.3808f, 15.54195f), 128.4692f),
                       new SpawnPoint(new Vector3(-1168.591f, -833.6693f, 14.20706f), 322.16f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-1164.385f, -864.2484f, 14.03558f), 213.1219f),
                       new SpawnPoint(new Vector3(-1179.129f, -852.1653f, 14.20555f), 135.7631f),
                       new SpawnPoint(new Vector3(-1183.507f, -847.5375f, 14.2107f), 126.145f),
                       new SpawnPoint(new Vector3(-1192.408f, -833.3651f, 14.30895f), 151.4055f),
                       new SpawnPoint(new Vector3(-1189.545f, -838.4308f, 14.20394f), 133.3376f),
                       new SpawnPoint(new Vector3(-1030.633f, -801.2432f, 17.379f), 250.5746f),
                       new SpawnPoint(new Vector3(-1024.762f, -792.3698f, 17.33986f), 239.8048f),
                       new SpawnPoint(new Vector3(-1020.749f, -785.7012f, 17.28315f), 236.8416f),
                       new SpawnPoint(new Vector3(-1066.033f, -752.2994f, 19.34918f), 296.4131f),
                       new SpawnPoint(new Vector3(-1059.654f, -759.9105f, 19.24701f), 305.6428f),
                       new SpawnPoint(new Vector3(-1074.824f, -740.6072f, 19.20185f), 301.311f),
                       new SpawnPoint(new Vector3(-1079.161f, -736.6757f, 19.24285f), 26.02705f),
                       new SpawnPoint(new Vector3(-1090.192f, -746.0747f, 19.34059f), 30.72595f),
                       new SpawnPoint(new Vector3(-1099.608f, -753.3671f, 19.32905f), 38.42085f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-1160.495f, -863.882f, 14.0764f), 290.7826f),
                       new SpawnPoint(new Vector3(-1178.132f, -856.6224f, 14.1898f), 36.37435f),
                       new SpawnPoint(new Vector3(-1181.825f, -851.8145f, 14.20796f), 190.3764f),
                       new SpawnPoint(new Vector3(-1187.941f, -842.5602f, 14.16273f), 14.38939f),
                       new SpawnPoint(new Vector3(-1030.62f, -805.178f, 17.26057f), 348.2439f),
                       new SpawnPoint(new Vector3(-1027.425f, -797.9048f, 17.27469f), 140.0471f),
                       new SpawnPoint(new Vector3(-1021.947f, -788.9042f, 17.25408f), 320.347f),
                       new SpawnPoint(new Vector3(-1067.175f, -748.618f, 19.3439f), 46.38675f),
                       new SpawnPoint(new Vector3(-1071.745f, -743.9393f, 19.34296f), 210.4201f),
                       new SpawnPoint(new Vector3(-1088.435f, -742.6924f, 19.34059f), 308.618f),
                       new SpawnPoint(new Vector3(-1093.645f, -746.9423f, 19.34059f), 122.7695f),
                       new SpawnPoint(new Vector3(-1103.245f, -754.0359f, 19.24307f), 276.3081f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(-1153.648f, -859.7277f, 14.06743f), 209.4146f),
                       new SpawnPoint(new Vector3(-1156.582f, -861.3911f, 14.02392f), 217.5228f),
                       new SpawnPoint(new Vector3(-1175.735f, -860.3557f, 14.0234f), 121.4392f),
                       new SpawnPoint(new Vector3(-1184.251f, -848.2242f, 14.20908f), 126.812f),
                       new SpawnPoint(new Vector3(-1186.075f, -846.0612f, 14.19575f), 126.0206f),
                       new SpawnPoint(new Vector3(-1190.501f, -839.6324f, 14.23284f), 133.3264f),
                       new SpawnPoint(new Vector3(-1192.04f, -837.2692f, 14.3082f), 114.9998f),
                       new SpawnPoint(new Vector3(-1193.271f, -834.1024f, 14.30817f), 105.2996f),
                       new SpawnPoint(new Vector3(-1031.489f, -809.6023f, 17.03126f), 248.6002f),
                       new SpawnPoint(new Vector3(-1029.048f, -801.2396f, 17.27113f), 241.3142f),
                       new SpawnPoint(new Vector3(-1024.573f, -794.4747f, 17.2772f), 237.2688f),
                       new SpawnPoint(new Vector3(-1023.652f, -792.4592f, 17.25379f), 241.0964f),
                       new SpawnPoint(new Vector3(-1058.741f, -759.2773f, 19.23489f), 308.7778f),
                       new SpawnPoint(new Vector3(-1061.027f, -756.9439f, 19.34054f), 307.8581f),
                       new SpawnPoint(new Vector3(-1063.223f, -754.554f, 19.35406f), 305.2883f),
                       new SpawnPoint(new Vector3(-1064.9f, -751.7651f, 19.34912f), 303.1526f),
                       new SpawnPoint(new Vector3(-1056.39f, -762.2045f, 19.22009f), 299.1905f),
                       new SpawnPoint(new Vector3(-1073.794f, -739.8788f, 19.19963f), 306.3715f),
                       new SpawnPoint(new Vector3(-1076.346f, -736.3812f, 19.28775f), 309.9072f),
                       new SpawnPoint(new Vector3(-1078.654f, -735.4063f, 19.26386f), 345.6949f),
                       new SpawnPoint(new Vector3(-1081.061f, -737.098f, 19.23464f), 29.3077f),
                       new SpawnPoint(new Vector3(-1083.348f, -738.9127f, 19.15671f), 45.54208f),
                       new SpawnPoint(new Vector3(-1085.329f, -740.5936f, 19.28601f), 32.41815f),
                       new SpawnPoint(new Vector3(-1097.29f, -749.4274f, 19.35737f), 33.95235f),
                       new SpawnPoint(new Vector3(-1099.821f, -751.6296f, 19.35694f), 42.09032f),
                       new SpawnPoint(new Vector3(-1107.305f, -757.3335f, 19.33083f), 51.00749f),
                       new SpawnPoint(new Vector3(-1163.527f, -830.2745f, 14.20871f), 237.9863f),
                       new SpawnPoint(new Vector3(-1159.994f, -825.752f, 14.35051f), 221.9667f),
                       new SpawnPoint(new Vector3(-1152.077f, -818.477f, 14.73421f), 220.6927f),
                       new SpawnPoint(new Vector3(-1135.435f, -804.9445f, 16.10798f), 223.6041f),
                       new SpawnPoint(new Vector3(-1133.332f, -802.3544f, 16.36374f), 215.8783f),
                       new SpawnPoint(new Vector3(-1129.549f, -798.9763f, 16.76862f), 216.0757f),
                       new SpawnPoint(new Vector3(-1084.071f, -769.9467f, 19.35797f), 183.6937f),
                       new SpawnPoint(new Vector3(-1079.07f, -770.6172f, 19.35055f), 149.9323f),
                       new SpawnPoint(new Vector3(-1117.437f, -788.8484f, 18.09196f), 232.7155f),
                   }
               ),

            new AttackedPoliceStationSpawn
               (
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(827.0967f, -1290.06f, 28.24066f), 88.35012f),
                       new SpawnPoint(new Vector3(844.3224f, -1268.479f, 26.41838f), 67.63682f),
                       new SpawnPoint(new Vector3(849.5961f, -1280.667f, 28.00476f), 357.2146f),
                       new SpawnPoint(new Vector3(818.1198f, -1311.485f, 26.07768f), 228.5809f),
                       new SpawnPoint(new Vector3(854.5858f, -1323.413f, 26.25509f), 89.63194f),
                       new SpawnPoint(new Vector3(841.829f, -1338.72f, 26.05435f), 46.72481f),
                       new SpawnPoint(new Vector3(833.748f, -1351.035f, 26.36743f), 32.37333f),
                       new SpawnPoint(new Vector3(820.276f, -1371.76f, 26.13812f), 6.568433f),
                       new SpawnPoint(new Vector3(847.7543f, -1376.385f, 26.1371f), 18.53539f),
                       new SpawnPoint(new Vector3(867.3687f, -1347.872f, 26.30932f), 166.1709f),
                       new SpawnPoint(new Vector3(849.5261f, -1302.835f, 36.42637f), 83.72862f),
                       new SpawnPoint(new Vector3(854.5777f, -1289.699f, 36.77719f), 56.5725f),
                       new SpawnPoint(new Vector3(821.5465f, -1280.946f, 26.28789f), 304.6478f),
                       new SpawnPoint(new Vector3(817.2485f, -1328.058f, 26.08964f), 336.5117f),
                       new SpawnPoint(new Vector3(825.6584f, -1376.813f, 34.6228f), 17.04663f),
                       new SpawnPoint(new Vector3(839.9788f, -1379.012f, 35.22409f), 351.6386f),
                       new SpawnPoint(new Vector3(869.7194f, -1351.908f, 35.14402f), 30.0312f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(820.1528f, -1272.806f, 26.33936f), 179.3201f),
                       new SpawnPoint(new Vector3(820.0471f, -1286.402f, 26.30238f), 235.2073f),
                       new SpawnPoint(new Vector3(823.8221f, -1292.119f, 28.24072f), 221.9281f),
                       new SpawnPoint(new Vector3(846.1885f, -1267.786f, 26.45474f), 14.23708f),
                       new SpawnPoint(new Vector3(818.4366f, -1323.942f, 26.08466f), 229.1131f),
                       new SpawnPoint(new Vector3(855.0613f, -1366.234f, 26.11064f), 351.7328f),
                       new SpawnPoint(new Vector3(864.574f, -1351.311f, 26.15865f), 322.1783f),
                       new SpawnPoint(new Vector3(816.5229f, -1291.582f, 26.27966f), 152.8189f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(793.3882f, -1242.088f, 26.3798f), 226.189f),
                       new SpawnPoint(new Vector3(795.4166f, -1238.536f, 26.39736f), 240.9908f),
                       new SpawnPoint(new Vector3(801.9329f, -1232.38f, 26.40076f), 193.6152f),
                       new SpawnPoint(new Vector3(789.3068f, -1324.11f, 26.14664f), 276.5475f),
                       new SpawnPoint(new Vector3(790.8867f, -1327.241f, 26.22565f), 300.7964f),
                       new SpawnPoint(new Vector3(788.9102f, -1320.36f, 26.11311f), 277.038f),
                       new SpawnPoint(new Vector3(789.1588f, -1322.213f, 26.13228f), 261.8223f),
                       new SpawnPoint(new Vector3(790.1621f, -1307.825f, 26.17395f), 276.4119f),
                       new SpawnPoint(new Vector3(790.0303f, -1297.814f, 26.16799f), 268.0436f),
                       new SpawnPoint(new Vector3(788.6297f, -1289.458f, 26.16249f), 271.884f),
                       new SpawnPoint(new Vector3(789.0687f, -1281.095f, 26.23789f), 266.4614f),
                       new SpawnPoint(new Vector3(789.1662f, -1270.869f, 26.26143f), 249.187f),
                       new SpawnPoint(new Vector3(789.8622f, -1266.893f, 26.29831f), 245.0774f),
                       new SpawnPoint(new Vector3(792.3687f, -1255.609f, 26.35761f), 244.7253f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(798.7642f, -1234.984f, 26.4026f), 150.0451f),
                       new SpawnPoint(new Vector3(793.0037f, -1249.664f, 26.36284f), 176.3228f),
                       new SpawnPoint(new Vector3(791.1322f, -1261.406f, 26.31374f), 347.1262f),
                       new SpawnPoint(new Vector3(789.2794f, -1284.618f, 26.21767f), 179.5273f),
                       new SpawnPoint(new Vector3(790.3185f, -1302.821f, 26.17201f), 178.8462f),
                       new SpawnPoint(new Vector3(793.2535f, -1331.497f, 26.29814f), 13.165f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(783.1663f, -1394.586f, 27.09278f), 175.2296f),
                       new SpawnPoint(new Vector3(794.948f, -1394.352f, 27.26447f), 176.4118f),
                       new SpawnPoint(new Vector3(807.3133f, -1397.438f, 27.10376f), 160.7159f),
                       new SpawnPoint(new Vector3(783.0241f, -1236.863f, 26.44445f), 128.8892f),
                       new SpawnPoint(new Vector3(804.9891f, -1201.001f, 27.09412f), 2.986298f),
                       new SpawnPoint(new Vector3(795.8738f, -1202.631f, 27.22686f), 354.8961f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(790.7644f, -1395.877f, 27.22033f), 263.5944f),
                       new SpawnPoint(new Vector3(800.7581f, -1398.116f, 27.2615f), 263.1152f),
                       new SpawnPoint(new Vector3(781.9579f, -1242.007f, 26.56893f), 359.4196f),
                       new SpawnPoint(new Vector3(791.7329f, -1200.749f, 27.17617f), 80.7764f),
                       new SpawnPoint(new Vector3(801.7299f, -1200.058f, 27.21004f), 112.9385f),
                   },
                   new List<SpawnPoint>()
                   {
                       new SpawnPoint(new Vector3(787.3621f, -1396.451f, 27.07084f), 175.1969f),
                       new SpawnPoint(new Vector3(794.6091f, -1396.192f, 27.26235f), 175.4546f),
                       new SpawnPoint(new Vector3(783.41f, -1396.142f, 27.13304f), 181.3202f),
                       new SpawnPoint(new Vector3(807.0626f, -1398.466f, 27.1168f), 169.4948f),
                       new SpawnPoint(new Vector3(811.0766f, -1398.124f, 27.21174f), 174.5789f),
                       new SpawnPoint(new Vector3(782.7221f, -1247.461f, 26.42515f), 91.21548f),
                       new SpawnPoint(new Vector3(781.4506f, -1236.853f, 26.47295f), 101.8256f),
                       new SpawnPoint(new Vector3(787.4384f, -1200.475f, 27.02956f), 4.999931f),
                       new SpawnPoint(new Vector3(796.5944f, -1200.248f, 27.38738f), 7.209792f),
                       new SpawnPoint(new Vector3(806.5051f, -1199.665f, 27.1097f), 7.541846f),
                       new SpawnPoint(new Vector3(796.4061f, -1239.336f, 26.40154f), 233.0925f),
                       new SpawnPoint(new Vector3(794.6336f, -1242.964f, 26.39715f), 230.0205f),
                       new SpawnPoint(new Vector3(792.2326f, -1327.436f, 26.25705f), 292.7572f),
                       new SpawnPoint(new Vector3(790.8165f, -1324.079f, 26.21449f), 283.1992f),
                       new SpawnPoint(new Vector3(790.0592f, -1321.105f, 26.17415f), 277.2707f),
                       new SpawnPoint(new Vector3(791.357f, -1307.595f, 26.21955f), 279.4735f),
                       new SpawnPoint(new Vector3(791.0765f, -1297.891f, 26.20889f), 263.0628f),
                       new SpawnPoint(new Vector3(790.1818f, -1288.445f, 26.26086f), 274.8742f),
                       new SpawnPoint(new Vector3(792.1182f, -1265.746f, 26.42829f), 249.1687f),
                       new SpawnPoint(new Vector3(790.9058f, -1268.194f, 26.38035f), 247.9261f),
                       new SpawnPoint(new Vector3(790.0505f, -1270.946f, 26.3179f), 266.7404f),
                       new SpawnPoint(new Vector3(793.2692f, -1255.896f, 26.40376f), 266.5717f),
                   }
               ),
            };

            return spawns.GetRandomElement();
        }


        public static Model GetPolicePedModelForPosition(Vector3 position)
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

        public static Model GetSwatPedModel()
        {
            //Model[] models = { "mp_m_fibsec_01", "s_m_y_swat_01", "s_m_y_swat_01" };
            return /*models.GetRandomElement()*/"s_m_y_swat_01";
        }

        public static Model GetPoliceVehicleModelForPosition(Vector3 position)
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

        public static Model GetSwatVehicleModel()
        {
            Model[] models = { "riot", "fbi2" };
            return models.GetRandomElement();
        }

        public static Model GetAttackerPedModel(AttackerType type)
        {
            switch (type)
            {
                case AttackerType.Professional:
                    Model[] models1 = { "hc_driver", "hc_gunman", "hc_hacker", "mp_g_m_pros_01", "u_m_m_jewelthief", "s_m_y_clown_01", "s_m_m_autoshop_01", "s_m_m_autoshop_02", "s_m_y_factory_01", "s_m_y_pestcont_01", "s_m_m_movalien_01", "s_m_m_movspace_01" };
                    return models1.GetRandomElement();
                case AttackerType.Vagos:
                    Model[] models2 = { "g_f_y_vagos_01", 0x26ef3426, "ig_ramp_mex" };
                    return models2.GetRandomElement();
                case AttackerType.Ballas:
                    Model[] models3 = { "g_f_y_ballas_01", "g_m_y_ballasout_01", "g_m_y_ballaorig_01", "g_m_y_ballaeast_01" };
                    return models3.GetRandomElement();
                case AttackerType.Families:
                    Model[] models4 = { "g_f_y_families_01", "g_m_y_famca_01", "g_m_y_famdnf_01", "g_m_y_famfor_01" };
                    return models4.GetRandomElement();
                case AttackerType.Lost:
                    Model[] models5 = { "g_f_y_lost_01", "g_m_y_lost_01", "g_m_y_lost_02", "g_m_y_lost_03", "ig_clay" };
                    return models5.GetRandomElement();
                case AttackerType.Armenian:
                    Model[] models6 = { "g_m_y_armgoon_02", "g_m_m_armlieut_01", "g_m_m_armgoon_01", "g_m_m_armboss_01" };
                    return models6.GetRandomElement();
                case AttackerType.Aztecas:
                    Model[] models7 = { "g_m_y_azteca_01" };
                    return models7.GetRandomElement();
                case AttackerType.MarabuntaGrande:
                    Model[] models8 = { "g_m_y_salvaboss_01", "g_m_y_salvagoon_01", "g_m_y_salvagoon_02", "g_m_y_salvagoon_03" };
                    return models8.GetRandomElement();
                case AttackerType.Clowns:
                    return "s_m_y_clown_01";
                case AttackerType.Aliens:
                    return "s_m_m_movalien_01";
                default:
                    Model[] models = { "hc_driver", "hc_gunman", "hc_hacker", "mp_g_m_pros_01", "u_m_m_jewelthief" };
                    return models.GetRandomElement();
            }
        }

        public static string GetAttackerWeaponAsset()
        {
            //EWeaponHash[] assets = { EWeaponHash.Advanced_Rifle , EWeaponHash.Assault_Rifle, EWeaponHash.AP_Pistol       , EWeaponHash.Assault_Shotgun,
            //                         EWeaponHash.Bullpup_Shotgun, EWeaponHash.Bullpup_Rifle, EWeaponHash.Carbine_Rifle   , EWeaponHash.Pistol         ,
            //                         EWeaponHash.Pistol_50      , EWeaponHash.Pump_Shotgun , EWeaponHash.Sawn_Off_Shotgun, EWeaponHash.Micro_SMG      ,
            //                         EWeaponHash.Heavy_Revolver , EWeaponHash.Combat_Pistol
            //                       };
            string[] names =       { "WEAPON_ADVANCEDRIFLE"     , "WEAPON_ASSAULTRIFLE"    , "WEAPON_APPISTOL"           , "WEAPON_ASSAULTSHOTGUN"    ,
                                     "WEAPON_BULLPUPSHOTGUN"    , "WEAPON_BULLPUPRIFLE"    , "WEAPON_CARBINERIFLE"       , "WEAPON_PISTOL"            ,
                                     "WEAPON_PISTOL50"          , "WEAPON_PUMPSHOTGUN"     , "WEAPON_SAWNOFFSHOTGUN"     , "WEAPON_MICROSMG"          ,
                                     "WEAPON_REVOLVER"          , "WEAPON_COMBATPISTOL"
                                   };
            return names.GetRandomElement();
        }

        public static string GetSwatWeaponAsset()
        {
            //EWeaponHash[] assets = { EWeaponHash.Carbine_Rifle , EWeaponHash.Pump_Shotgun, EWeaponHash.Assault_Shotgun, EWeaponHash.Pistol, EWeaponHash.Pistol_50 };
            //Tuple<EWeaponHash, string>[] weapons =
            //{
            //    new Tuple<EWeaponHash, string>(EWeaponHash.Carbine_Rifle    , "WEAPON_CARBINERIFLE"),
            //    new Tuple<EWeaponHash, string>(EWeaponHash.Pump_Shotgun     , "WEAPON_PUMPSHOTGUN"),
            //    new Tuple<EWeaponHash, string>(EWeaponHash.Assault_Shotgun  , "WEAPON_ASSAULTSHOTGUN"),
            //    new Tuple<EWeaponHash, string>(EWeaponHash.Pistol           , "WEAPON_PISTOL"),
            //    new Tuple<EWeaponHash, string>(EWeaponHash.Pistol_50        , "WEAPON_PISTOL50"),
            //    new Tuple<EWeaponHash, string>(EWeaponHash.Combat_Pistol    , "WEAPON_COMBATPISTOL"),
            //};
            string[] weapons = { "WEAPON_CARBINERIFLE", "WEAPON_PUMPSHOTGUN", "WEAPON_ASSAULTSHOTGUN", "WEAPON_CARBINERIFLE", "WEAPON_PUMPSHOTGUN", "WEAPON_PISTOL", "WEAPON_PISTOL50", "WEAPON_COMBATPISTOL", "WEAPON_SMG", "WEAPON_ASSAULTSMG", "WEAPON_SMG", "WEAPON_ASSAULTSMG" };
            return weapons.GetRandomElement();
        }

        public static EWeaponHash GetPoliceWeaponAsset()
        {
            EWeaponHash[] assets = { EWeaponHash.Pump_Shotgun, EWeaponHash.Pistol, EWeaponHash.Combat_Pistol };
            return assets.GetRandomElement();
        }

        public static WeaponDescriptor GiveSwatWeapon(Ped ped)
        {
            string weapon = GetSwatWeaponAsset();
            WeaponDescriptor w = ped.Inventory.GiveNewWeapon(weapon, 500, true);
            string[] components = { "COMPONENT_AT_RAILCOVER_01", "COMPONENT_AT_AR_AFGRIP"  , "COMPONENT_AT_PI_FLSH"  , "COMPONENT_AT_AR_FLSH", "COMPONENT_AT_SCOPE_MACRO", "COMPONENT_AT_SCOPE_SMALL",
                                    "COMPONENT_AT_SCOPE_MEDIUM", "COMPONENT_AT_SCOPE_LARGE", "COMPONENT_AT_SCOPE_MAX" };
            foreach (string component in components)
            {
                if (Globals.Random.Next(4) <= 1) ped.Inventory.AddComponentToWeapon(weapon, component);
            }
            return w;
        }

        public static WeaponDescriptor GiveAttackerWeapon(Ped ped)
        {
            string weapon = GetAttackerWeaponAsset();
            WeaponDescriptor w = ped.Inventory.GiveNewWeapon(weapon, 500, true);
            //string[] components = { "COMPONENT_AT_RAILCOVER_01", "COMPONENT_AT_AR_AFGRIP"  , "COMPONENT_AT_PI_FLSH"  , "COMPONENT_AT_AR_FLSH", "COMPONENT_AT_SCOPE_MACRO", "COMPONENT_AT_SCOPE_SMALL",
            //                        "COMPONENT_AT_SCOPE_MEDIUM", "COMPONENT_AT_SCOPE_LARGE", "COMPONENT_AT_SCOPE_MAX" };
            //foreach (string component in components)
            //{
            //    if (Globals.Random.Next(4) <= 1) ped.GiveWeaponComponent(weapon, component);
            //}
            if (Globals.Random.Next(3) == 1) NativeFunction.CallByName<uint>("SET_PED_WEAPON_TINT_INDEX", ped, Game.GetHashKey(weapon), Globals.Random.Next(NativeFunction.CallByName<int>("GET_WEAPON_TINT_COUNT", Game.GetHashKey(weapon))));
            return w;
        }

        public enum AttackerType
        {
            Professional,
            Vagos,
            Ballas,
            Families,
            Lost,
            Armenian,
            Aztecas,
            MarabuntaGrande,
            Clowns,
            Aliens,
        }
    }
}
