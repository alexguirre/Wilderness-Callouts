namespace WildernessCallouts.AmbientEvents
{
    using Rage;
    using Rage.Native;
    using System.Collections.Generic;
    using WildernessCallouts.Types;

    internal class HuntingEvent : EventBase
    {
        private static string[] _animalsModels = { "a_c_coyote", "a_c_boar", "a_c_chimp", "a_c_deer", "a_c_cormorant", "a_c_pig", "a_c_deer", "a_c_coyote", "a_c_boar", "a_c_rhesus" };

        private static string[] _huntersModels = { "csb_cletus"       , "ig_clay"         , "ig_oneil"       , "a_m_m_tramp_01", "ig_old_man1a"      , "ig_hunter"        , "player_two"        , 
                                                   "mp_m_exarmy_01"   , "ig_clay"         , "ig_oneil"       , "ig_old_man2"   , "ig_old_man1a"      , "ig_hunter"        , "ig_russiandrunk"   , 
                                                   "ig_clay"          , "mp_m_exarmy_01"  , "ig_old_man2"    , "ig_old_man1a"  , "ig_hunter"         , "s_m_y_armymech_01", "s_m_m_ammucountry" ,
                                                   "s_m_m_trucker_01" , "ig_ortega"       , "ig_russiandrunk", "a_m_m_tramp_01", "a_m_m_trampbeac_01", "a_m_m_rurmeth_01" , "mp_m_exarmy_01"    ,
                                                   "g_m_y_pologoon_01", "g_m_y_mexgoon_03", "g_m_y_lost_03"  , "g_m_y_lost_02" , "g_m_y_pologoon_02" , "g_m_m_armboss_01" , "u_m_o_taphillbilly", 
                                                   "u_m_o_taphillbilly"
                                                 };

        private static WeaponAsset[] _hunterWeapons = { "WEAPON_PUMPSHOTGUN", "WEAPON_HEAVYSNIPER", "WEAPON_PISTOL50"   , "WEAPON_PUMPSHOTGUN", "WEAPON_PISTOL"     , 
                                                        "WEAPON_HEAVYSNIPER", "WEAPON_SNIPERRIFLE", "WEAPON_HEAVYSNIPER", "WEAPON_SNIPERRIFLE", "WEAPON_SNIPERRIFLE", 
                                                        "WEAPON_PUMPSHOTGUN", "WEAPON_HEAVYSNIPER", "WEAPON_SNIPERRIFLE", "WEAPON_PISTOL50"   , "WEAPON_HEAVYSNIPER", 
                                                        "WEAPON_PUMPSHOTGUN", "WEAPON_HEAVYSNIPER", "WEAPON_HEAVYSNIPER", "WEAPON_SNIPERRIFLE", "WEAPON_SNIPERRIFLE", 
                                                        "WEAPON_SNIPERRIFLE", "WEAPON_HEAVYSNIPER", "WEAPON_SNIPERRIFLE", "WEAPON_HEAVYSNIPER", "WEAPON_HEAVYSNIPER" 
                                                      };

        private static string[] tendToDeadIdles = { "idle_a", "idle_b", "idle_c" };
        
        public Ped Hunter;
        public Ped Animal;

        public override bool IsRunning { get { return base.IsRunning; } set { base.IsRunning = value; } }
        public override bool CanBeSpawned { get { return this.SpawnPosition.GetArea() == EWorldArea.Blaine_County; } set { base.CanBeSpawned = value; } }
        public override List<Entity> SpawnedEntities { get { return base.SpawnedEntities; } set { base.SpawnedEntities = value; } }
        public override List<Blip> Blips { get { return base.Blips; } set { base.Blips = value; } }
        public override Vector3 SpawnPosition { get { return base.SpawnPosition; } set { base.SpawnPosition = value; } }

        public HuntingEvent() : base() { }

        public override bool Create()
        {
            try
            {
                Vector3 hunterspawnPos = this.SpawnPosition.AroundPosition(35.0f).ToGround();
                this.Hunter = new Ped(_huntersModels.GetRandomElement(true), hunterspawnPos, MathHelper.GetRandomSingle(0.0f, 359.0f));

                Vector3 animspawnPos = this.Hunter.Position.AroundPosition(27.5f).ToGround();
                this.Animal = new Ped(_animalsModels.GetRandomElement(true), animspawnPos, 0.0f);

                if (this.Animal.Exists()) this.Animal.Health = 1250;
                if (this.Hunter.Exists()) this.Hunter.Inventory.GiveNewWeapon(_hunterWeapons.GetRandomElement(true), 9999, true);

                SpawnedEntities.Add(this.Hunter);
                SpawnedEntities.Add(this.Animal);

                return base.Create();
            }
            catch(System.Exception e)
            {
                Logger.LogException(this.GetType().Name, e);
                return false;
            }
        }

        public override void Action()
        {
            GameFiber.StartNew(delegate
            {
                try
                {
                    base.Action();

                    if (this.Animal.Exists()) this.Animal.ReactAndFlee(this.Hunter);
                    if (this.Animal.Exists()) this.Animal.KeepTasks = true;
                    if (this.Hunter.Exists()) this.Hunter.AttackPed(this.Animal);
                    if (this.Hunter.Exists()) this.Hunter.KeepTasks = true;

                    while (this.Hunter.Exists() && this.Hunter.IsAlive && this.Animal.Exists() && this.Animal.IsAlive)
                        GameFiber.Yield();

                    if (this.Hunter.Exists() && this.Animal.Exists()) NativeFunction.CallByName<uint>("TASK_SHOOT_AT_ENTITY", this.Hunter, this.Animal, 6000, (uint)Rage.FiringPattern.BurstFire);

                    GameFiber.Sleep(5000);

                    if (this.Hunter.Exists() && this.Animal.Exists()) this.Hunter.Tasks.FollowNavigationMeshToPosition(this.Animal.Position.AroundPosition(0.75f), this.Hunter.Heading, 20.0f).WaitForCompletion();
                    if (this.Hunter.Exists() && this.Animal.Exists()) this.Hunter.Tasks.AchieveHeading(this.Hunter.GetHeadingTowards(this.Animal)).WaitForCompletion(2250);
                    if (this.Hunter.Exists()) this.Hunter.Tasks.PlayAnimation("amb@medic@standing@tendtodead@idle_a", tendToDeadIdles.GetRandomElement(), 2.0f, AnimationFlags.Loop);

                    GameFiber.Sleep(60000);

                    this.CleanUp();

                }
                catch (System.Exception e)
                {
                    Logger.LogException(this.GetType().Name, e);
                }
            });
        }

        public override void Process()
        {
            if (!this.Hunter.Exists() || this.Hunter.IsDead || this.Hunter.Position.DistanceTo(Game.LocalPlayer.Character.Position) > 400.0f) this.CleanUp(); 
            base.Process();
        }

        public override void CleanUp()
        {
            try
            {
                base.CleanUp();
            }
            catch (System.Exception e)
            {
                Logger.LogExceptionDebug(this.GetType().Name, e);
            }
        }
    }
}
