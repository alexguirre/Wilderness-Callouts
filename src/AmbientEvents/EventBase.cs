namespace WildernessCallouts.AmbientEvents
{
    using Rage;
    using System.Drawing;
    using System.Collections.Generic;
    using WildernessCallouts.Types;

    internal abstract class EventBase : IEvent
    {
        public virtual bool IsRunning { get; set; }
        public virtual bool CanBeSpawned { get; set; }
        public virtual List<Entity> SpawnedEntities { get; set; }
        public virtual List<Blip> Blips { get; set; }
        public virtual Vector3 SpawnPosition { get; set; }
        public virtual GameFiber ProcessFiber { get; set; }
        
        public EventBase(Vector3 spawnPosition)
        {
            Logger.LogTrivial(this.GetType().Name, " Initialized");
            this.SpawnPosition = spawnPosition;
            this.SpawnedEntities = new List<Entity>();
            this.Blips = new List<Blip>();
            this.ProcessFiber = new GameFiber(delegate
            {
                while (this.IsRunning)
                {
                    GameFiber.Yield();
                    Process();
                }
            },
            this.GetType().Name + " Proccess");
        }
        public EventBase() : this(EventPool.GetSpawnPosition()) { }

        public virtual bool Create()
        {
            Logger.LogTrivial(this.GetType().Name, "Created");

            foreach (Entity e in SpawnedEntities)
            {
                if (!e.Exists())
                {
                    this.CleanUp();
                    return false;
                }
                else
                {
                    if (Settings.AmbientEvents.ShowEventsBlips)
                    {
                        Blip b = new Blip(e);
                        b.Scale = 0.5f;
                        b.Color = Color.ForestGreen;
                        this.Blips.Add(b);
                    }
                }
            }
            return true;
        }

        public virtual void Action()
        {
            Logger.LogTrivial(this.GetType().Name, "Action started");
            EventPool.IsAnyEventRunning = true;
            this.IsRunning = true;
            ProcessFiber.Start();
        }
                
        public virtual void Process()   
        {
            Logger.LogDebug(this.GetType().Name, "Proccessed");
        }

        public virtual void CleanUp()
        {
            Logger.LogTrivial(this.GetType().Name, "Cleaned up");

            foreach (Entity e in SpawnedEntities)
                if (e != null && e.Exists()) e.Dismiss();

            foreach (Blip b in Blips)
                if (b != null && b.Exists()) b.Delete();

            EventPool.IsAnyEventRunning = false;
            this.IsRunning = false;

            ProcessFiber.Abort();
        }

    }
}
