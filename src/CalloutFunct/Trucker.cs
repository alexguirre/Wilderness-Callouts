using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rage;
using Rage.Native;
using WildernessCallouts.Types;

namespace WildernessCallouts.Peds
{
    internal class Trucker : Ped
    {
        static string[] truckModel = { "DUMP", "BIFF", "TIPTRUCK", "TIPTRUCK2", "BIFF", "BIFF", "TIPTRUCK", "TIPTRUCK2", "BIFF", "BIFF", "TIPTRUCK", "TIPTRUCK2", "BIFF" };   // Truck model

        public Trucker(Model model, Vector3 position, float heading) : base(model, position, heading)
        {
        }

        public void Job(Vector3 posToDrive, Vector3 positionToWalk)
        {
            GameFiber.StartNew(delegate
            {
                GameFiber.StartNew(delegate         // Start new gamefiber so the dialogue won't slow down the vet
                {
                    Game.DisplayNotification("~b~" + Settings.General.Name + ":~w~ Requesting a truck to remove the rocks");
                    GameFiber.Wait(2000);
                    Game.DisplayNotification("~b~Dispatch:~w~ Affirmative, truck en route");
                });

                Ped player = Game.LocalPlayer.Character;

                this.BlockPermanentEvents = true;       // Blocks the ped so he won't flee

                this.RelationshipGroup = new RelationshipGroup("TRUCKER");       // Sets the ped relation so he won't be scared or attacked

                Game.SetRelationshipBetweenRelationshipGroups("TRUCKER", "PLAYER", Relationship.Companion);
                Game.SetRelationshipBetweenRelationshipGroups("TRUCKER", "COP", Relationship.Companion);
                Game.SetRelationshipBetweenRelationshipGroups("COP", "TRUCKER", Relationship.Companion);
                Game.SetRelationshipBetweenRelationshipGroups("TRUCKER", "FIREMAN", Relationship.Companion);
                Game.SetRelationshipBetweenRelationshipGroups("TRUCKER", "MEDIC", Relationship.Companion);

                Vehicle veh = new Vehicle(truckModel.GetRandomElement(), this.Position);     // Creates the vet vehicle

                veh.Heading = veh.Position.GetClosestVehicleNodeHeading();     // Sets the vehicle heading the same as the road heading
                veh.TopSpeed = veh.TopSpeed + 15.0f;

                Blip blip = new Blip(veh);              // Sets the blip
                blip.Sprite = BlipSprite.ArmoredVan;

                this.WarpIntoVehicle(veh, -1);      // Teleports the vet inside his vehicle

                GameFiber.Wait(50);

                this.Tasks.DriveToPosition(posToDrive, 30.0f, VehicleDrivingFlags.Emergency/*(DriveToPositionFlags)262199*/, 17.5f).WaitForCompletion(120000);
                //NativeFunction.CallByName<uint>("TASK_VEHICLE_DRIVE_TO_COORD", this, this.CurrentVehicle, posToDrive.X, posToDrive.Y, posToDrive.Z,
                //                                    16.0f, 0, this.CurrentVehicle.Model.Hash, 262199, 4.645f, 0);

                if (Vector3.Distance(veh.Position, posToDrive) > 22.5f)  // If the timeout end and the vet isn't near, he's teleported near the player
                {
                    veh.Position = posToDrive.AroundPosition(1.0f);
                }

                this.Tasks.LeaveVehicle(LeaveVehicleFlags.None).WaitForCompletion();   // The ped leaves the vehicle

                this.PlayAmbientSpeech(Globals.Random.Next(2) == 1 ? Speech.GENERIC_HI : Speech.GENERIC_HOWS_IT_GOING);

                this.Tasks.FollowNavigationMeshToPosition(positionToWalk.AroundPosition(1.5f), 0.0f, 13.5f, 25000).WaitForCompletion();

                this.Tasks.PlayAnimation("amb@medic@standing@kneel@base", "base", 2.0f, AnimationFlags.Loop);

                GameFiber.Wait(4000);

                this.Tasks.Clear();

                this.PlayAmbientSpeech(Speech.GENERIC_BYE); 

                GameFiber.Wait(200);

                NativeFunction.CallByName<uint>("TASK_GO_TO_ENTITY", this, veh, -1, 5.0f, 1.0f, 0, 0);
                while (Vector3.Distance(this.Position, veh.Position) > 6.0f)
                    GameFiber.Yield();

                this.Tasks.EnterVehicle(veh, -1).WaitForCompletion(10000);       // The ped enters his vehicle
                if (!this.IsInVehicle(veh, false))      // If ped vet isn't in the vehicle, is warped into the vehicle
                {
                    this.WarpIntoVehicle(veh, -1);
                }

                NativeFunction.CallByName<uint>("TASK_VEHICLE_DRIVE_WANDER", this, veh, 15.0f, 262199);      // The ped drive away

                GameFiber.Wait(2000);

                if (this.Exists()) this.Dismiss();          // Dismiss/delete all
                if (veh.Exists()) veh.Dismiss();
                if (blip.Exists()) blip.Delete();
            });
        }
    }
}
