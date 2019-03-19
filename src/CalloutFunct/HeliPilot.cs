using Rage;
using Rage.Native;
using LSPD_First_Response.Mod.API;
using WildernessCallouts.Types;

namespace WildernessCallouts.Peds
{
    internal class HeliPilot : Ped
    {
        private Vehicle _heli;
        private Blip _blipTest; // Remove this //

        public HeliPilot(Vector3 position, float heading) : base("s_m_y_pilot_01", position, heading)
        {
            this.BlockPermanentEvents = true;       // Blocks the ped so he won't flee

            this.RelationshipGroup = new RelationshipGroup("HELIPILOT");       // Sets the ped relation so he won't be scared or attacked

            Game.SetRelationshipBetweenRelationshipGroups("HELIPILOT", "PLAYER", Relationship.Companion);
            Game.SetRelationshipBetweenRelationshipGroups("HELIPILOT", "COP", Relationship.Companion);
            Game.SetRelationshipBetweenRelationshipGroups("COP", "HELIPILOT", Relationship.Companion);
            Game.SetRelationshipBetweenRelationshipGroups("HELIPILOT", "FIREMAN", Relationship.Companion);
            Game.SetRelationshipBetweenRelationshipGroups("HELIPILOT", "MEDIC", Relationship.Companion);
        }

        public void JobFollow(Entity entityToFollow)
        {
            Functions.PlayScannerAudioUsingPosition("CRIME_OFFICER_REQUESTS_AIR_SUPPORT IN_OR_ON_POSITION OUTRO OFFICER_INTRO HELI_APPROACHING_DISPATCH", Game.LocalPlayer.Character.Position);

            _heli = new Vehicle("polmav", GetSpawnPoint());
            _heli.SetLivery(0);
            _heli.IsEngineOn = true;
            NativeFunction.Natives.SET_HELI_BLADES_FULL_SPEED(_heli);
            this.WarpIntoVehicle(_heli, -1);
            _heli.Velocity = Vector3.WorldUp * 10.0f + _heli.ForwardVector * 2.0f;
            if (Settings.General.IsDebugBuild) _blipTest = new Blip(_heli);
            GameFiber.Sleep(100);
            NativeFunction.Natives.TASK_HELI_CHASE(this, entityToFollow, MathHelper.GetRandomSingle(-35.0f, 35.0f), MathHelper.GetRandomSingle(-35.0f, 35.0f), MathHelper.GetRandomSingle(90.0f, 130.0f));
        }
        public void CleanUpHeliPilot()
        {
            if (this.Exists()) this.Dismiss();
            if (_heli.Exists()) _heli.Dismiss();
            if (_blipTest.Exists()) _blipTest.Delete();
        }

        private static Vector3 GetSpawnPoint()
        {
            Vector3 v3 = Game.LocalPlayer.Character.Position.AroundPosition(800.0f) + Vector3.WorldUp * 450.0f;
            while (Vector3.Distance2D(Game.LocalPlayer.Character.Position, v3) < 400.0f)
            {
                v3 = Game.LocalPlayer.Character.Position.AroundPosition(800.0f) + Vector3.WorldUp * 450.0f;
                GameFiber.Yield();
            }
            return v3;
        }
    }
}
