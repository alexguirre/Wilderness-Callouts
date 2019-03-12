namespace WildernessCallouts.AmbientEvents
{
    using Rage;
    using System.Collections.Generic;
    using System.Linq;
    using WildernessCallouts.Types;

    internal class IntoxicatedPersonEvent : EventBase
    {
        public Ped Ped;
        static string[] _drunkAnimationSets = { "move_m@drunk@verydrunk", "move_m@drunk@moderatedrunk_head_up", "move_m@drunk@moderatedrunk" };

        public override bool IsRunning { get { return base.IsRunning; } set { base.IsRunning = value; } }
        public override bool CanBeSpawned { get { return true; } }
        public override List<Entity> SpawnedEntities { get { return base.SpawnedEntities; } set { base.SpawnedEntities = value; } }
        public override List<Blip> Blips { get { return base.Blips; } set { base.Blips = value; } }
        public override Vector3 SpawnPosition { get { return base.SpawnPosition; } set { base.SpawnPosition = value; } }

        public IntoxicatedPersonEvent() : base() { }

        public override bool Create()
        {
            try
            {
                Ped[] possiblePedsNearInWorld = World.GetAllPeds().Where(x => !x.IsInAnyVehicle(false) && x.IsAlive && x.IsHuman && !x.IsPersistent && Vector3.Distance(x.Position, Game.LocalPlayer.Character.Position) < 120.0f).ToArray();
                possiblePedsNearInWorld.Shuffle();
                Ped = possiblePedsNearInWorld.FirstOrDefault();

                if (Ped == null || !Ped.Exists())
                {
                    Logger.LogTrivial(this.GetType().Name, " Couldn't find any near ped. Aborting event...");
                    return false;
                }
                Ped.MakePersistent();
                SpawnedEntities.Add(Ped);

                return base.Create();
            }
            catch (System.Exception e)
            {
                Logger.LogException(this.GetType().Name, e);
                return false;
            }
        }

        public override void Action()
        {
            base.Action();

            if (this.Ped.Exists())
            {
                Ped.SetMovementAnimationSet(_drunkAnimationSets.GetRandomElement());
                Ped.Armor = 69;//compatibility with breathalyzer
                Ped.Tasks.Wander();
            }
        }

        public override void Process()
        {
            if (!this.Ped.Exists() || this.Ped.IsDead || this.Ped.Position.DistanceTo(Game.LocalPlayer.Character.Position) > 350.0f) this.CleanUp();
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
