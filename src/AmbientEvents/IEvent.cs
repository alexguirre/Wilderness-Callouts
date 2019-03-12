namespace WildernessCallouts.AmbientEvents
{
    using Rage;
    using System.Collections.Generic;

    internal interface IEvent
    {
        bool IsRunning { get; set; }
        bool CanBeSpawned { get; set; }
        List<Entity> SpawnedEntities { get; set; }
        List<Blip> Blips { get; set; }
        Vector3 SpawnPosition { get; set; }
        void CleanUp();
        bool Create();
        void Action();
        GameFiber ProcessFiber { get; set; }
        void Process();
    }
}
