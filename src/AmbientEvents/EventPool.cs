namespace WildernessCallouts.AmbientEvents
{
    using Rage;
    using WildernessCallouts.Types;

    internal static class EventPool
    {
        public static bool IsAnyEventRunning = false;

        public static void EventsController()
        {
            GameFiber.StartNew(delegate
            {
                Logger.LogTrivial(typeof(EventPool).Name, "Events controller started");

                while (true)
                {
                    GameFiber.Sleep(Globals.Random.Next(Settings.AmbientEvents.MinTimeAmbientEvent * 1000, Settings.AmbientEvents.MaxTimeAmbientEvent * 1000));
                    if (!EventPool.IsAnyEventRunning && !LSPD_First_Response.Mod.API.Functions.IsCalloutRunning())
                    {
                        EventPool.CreateEvent();
                    }
                }
            });
        }

        public static void CreateEvent()
        {
            if (IsAnyEventRunning)
            {
                Logger.LogTrivial(typeof(EventPool).Name, "Another event running. Aborting new event...");
                return;
            }
            GameFiber.StartNew(delegate
            {
                IEvent ambientEvent = GetRandomEvent();
                currentEvent = ambientEvent;
                if (ambientEvent.CanBeSpawned)
                {
                    if (ambientEvent.Create())
                    {
                        ambientEvent.Action();
                    }
                }
            });
        }

        private static IEvent currentEvent;
        public static void EndCurrentEvent()
        {
            if(currentEvent != null)
                currentEvent.CleanUp();
        }

        public static IEvent GetRandomEvent()
        {
            IEvent rndEvent;
            switch (Globals.Random.Next(0, 2))
            {
                case 0:
                    rndEvent = new HuntingEvent();
                    break;
                case 1:
                    rndEvent = new IntoxicatedPersonEvent();
                    break;
                default:
                    rndEvent = new IntoxicatedPersonEvent();
                    break;
            }
            return rndEvent;
        }

        public static Vector3 GetSpawnPosition()
        {
            Vector3 result = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.AroundPosition(200.0f));
            int counter = 0;

            while (result.DistanceTo(Game.LocalPlayer.Character.Position) < 62.5f || counter < 150)
            {
                result = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.AroundPosition(200.0f));
                counter++;
                GameFiber.Yield();
            }

            return result;
        }
    }
}
