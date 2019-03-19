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
    internal class Vet
    {
        private static List<Ped> _takenAnimals = new List<Ped>();

        public static List<Ped> TakenAnimals => _takenAnimals;

        public Ped VetPed { get; }
        public Vehicle Vehicle { get; }
        private Blip _vehBlip;

        public Ped Animal { get; }
        private Ped _fakeAnimal;

        private static readonly string[] animalAliveDialog =
        {
            "It's alive.",
            "The animal appears to still be alive.",
            "Looks like this one is going to make it.",
            "It looks like this one might make it."
        };

        private static readonly string[] animalDeadDialog =
        {
            "End of the road for this one, buddy.",
            "No, this one is dead.",
            "I'm afraid there's nothing I can do for this one."
        };

        private static readonly string[] noAnimalsFoundDialog =
        {
            "I can't see any animals here.",
            "What? I can't find any animals. Are you wasting my time?",
            "You know I have to charge the department for a callout even if I can't find any animals!",
            "Is this a prank call? I don't see anything here!"
        };

        public Vet(Ped animalToTake)
        {
            _takenAnimals.Add(animalToTake);
            Animal = animalToTake;

            SpawnPoint spawnPoint = GetSpawnPoint();

            Vehicle = new Vehicle(Settings.Vet.VehModel, spawnPoint.Position, spawnPoint.Heading);
            if (Vehicle.HasSiren)
            {
                Vehicle.IsSirenOn = true;
                Vehicle.IsSirenSilent = true;
            }

            if (Vehicle.Model == new Model("DUBSTA3"))
            {
                // Vehicle color to white, randomizes between mettalic/classic and matte
                if (MathHelper.GetRandomInteger(101) < 50)
                    Vehicle.SetColors(EPaint.Ice_White, EPaint.Ice_White);
                else
                    Vehicle.SetColors(EPaint.Matte_Ice_White, EPaint.Matte_Ice_White);
            }

            Blip blip = new Blip(Vehicle); // Sets the blip
            blip.Sprite = BlipSprite.ArmoredVan;

            VetPed = new Ped(GetVetPedModel(), Vector3.Zero, 0.0f);
            VetPed.BlockPermanentEvents = true; // Blocks the ped so he won't flee
            VetPed.RelationshipGroup =
                new RelationshipGroup("VET"); // Sets the ped relation so he won't be scared or attacked

            Game.SetRelationshipBetweenRelationshipGroups("VET", "PLAYER", Relationship.Companion);
            Game.SetRelationshipBetweenRelationshipGroups("VET", "COP", Relationship.Companion);
            Game.SetRelationshipBetweenRelationshipGroups("COP", "VET", Relationship.Companion);
            Game.SetRelationshipBetweenRelationshipGroups("VET", "FIREMAN", Relationship.Companion);
            Game.SetRelationshipBetweenRelationshipGroups("VET", "MEDIC", Relationship.Companion);

            NativeFunction.Natives.SET_DRIVER_ABILITY(VetPed, 100.0f);
            NativeFunction.Natives.SET_DRIVER_AGGRESSIVENESS(VetPed, 0f);

            VetPed.WarpIntoVehicle(Vehicle, -1); // Teleports the vet inside his vehicle
        }

        public void Start(bool cleanUpOnEnd = true)
        {
            GameFiber.StartNew(delegate // Start new gamefiber so the dialogue won't slow down the vet
            {
                Game.DisplayNotification("~b~" + Settings.General.Name + ":~w~ Dispatch, requesting a " +
                                         Settings.Vet.Name.ToLower() + " , animal injured"); // Dialogue
                GameFiber.Wait(2000);
                Game.DisplayNotification("~b~Dispatch:~w~ Affirmative, " + Settings.Vet.Name.ToLower() + " en route");
            });

            Vector3 posToDrive = Animal.Position; // Assigns task drive to player position
            VetPed.Tasks
                .DriveToPosition(posToDrive, 15.0f, VehicleDrivingFlags.Emergency /*(DriveToPositionFlags)262199*/,
                    17.5f).WaitForCompletion(120000);

            //NativeFunction.CallByName<uint>("TASK_VEHICLE_DRIVE_TO_COORD", this, this.CurrentVehicle, posToDrive.X, posToDrive.Y, posToDrive.Z,
            //                                    13.75f, 0, this.CurrentVehicle.Model.Hash, 262199, 10.0f, 0);

            //GameFiber.Sleep(120000);

            if (Vector3.Distance(Vehicle.Position, posToDrive) > 25.0f
            ) // If the timeout end and the vet isn't near, he's teleported near the player
            {
                Vehicle.Position = posToDrive.AroundPosition(5.0f);
            }

            VetPed.Tasks.LeaveVehicle(LeaveVehicleFlags.LeaveDoorOpen)
                .WaitForCompletion(); // The vet leaves the vehicle
            VetPed.PlayAmbientSpeech(Globals.Random.Next(2) == 1 ? Speech.GENERIC_HI : Speech.GENERIC_HOWS_IT_GOING);

            if (Animal.Exists() && Animal.IsDead)
            {
                if (Animal.Exists())
                {
                    Vector3 posToWalk = Animal.Position.AroundPosition(0.75f);
                    VetPed.Tasks
                        .FollowNavigationMeshToPosition(posToWalk, posToWalk.GetHeadingTowards(Animal), 2.0f, 0.5f)
                        .WaitForCompletion(); //...if the animal exists vet go to animal 
                }

                GameFiber.Wait(75);

                if (Animal.Exists())
                {
                    VetPed.Tasks.PlayAnimation(new AnimationDictionary("amb@medic@standing@tendtodead@idle_a"),
                        "idle_a", 1.0f, AnimationFlags.Loop); // Vet plays the animation

                    Game.DisplaySubtitle("~b~" + Settings.Vet.Name + ": ~w~Let's see...", 3500); //Vet talks
                }

                GameFiber.Wait(6000); // Waits

                bool alive = true; // MathHelper.GetChance(20);       // Randomizes if the animal is dead or alive
                if (Animal.Exists())
                {
                    if (Animal.Model == new Model("a_c_mtlion")) // never revive a mountain lion
                        alive = false;

                    if (alive)
                    {
                        Game.DisplaySubtitle("~b~" + Settings.Vet.Name + $": ~w~ {animalAliveDialog[MathHelper.GetRandomInteger(0, animalAliveDialog.Length-1)]}", 3500);
                    }
                    else
                    {
                        Game.DisplaySubtitle("~b~" + Settings.Vet.Name + $": ~w~ {animalDeadDialog[MathHelper.GetRandomInteger(0, animalAliveDialog.Length-1)]}", 3500);
                    }
                }

                VetPed.Tasks.Clear(); // Clears the vet tasks to stop playing the animation

                //if (alive)
                //{
                //    if (Animal.Exists())
                //    {
                //        NativeFunction.Natives.REVIVE_INJURED_PED(Animal);
                //        NativeFunction.Natives.SET_ENTITY_HEALTH(Animal, 200);
                //        NativeFunction.Natives.RESURRECT_PED(Animal);
                //        Animal.Tasks.ClearImmediately();
                //    }
                //}
                //else
                //{
                if (Animal.Exists())
                {
                    if (Vehicle.Model == new Model("DUBSTA3"))
                    {
                        Animal.AttachToEntity(Vehicle, 60309, new Vector3(0.0f, -2.5f, 1.120f), Rotator.Zero);

                        _fakeAnimal = Animal.Clone(0.0f);
                        GameFiber.Wait(75);
                        if (Animal.Exists())
                            Animal.Delete();
                    }
                }

                //}
            }
            else if (!Animal.Exists() || (Animal.Exists() && Animal.IsAlive))
            {
                VetPed.Tasks.AchieveHeading(
                    VetPed.GetHeadingTowards(Game.LocalPlayer.Character)); // Looks at the player

                GameFiber.Wait(250);

                Game.DisplaySubtitle(
                    "~b~" + Settings.Vet.Name + $": ~w~{noAnimalsFoundDialog[MathHelper.GetRandomInteger(0,noAnimalsFoundDialog.Length-1)]}", 5000); //Vet talks

                //VetPed.Tasks.PlayAnimation("missmic2@goon2", "goon_pushcow_beef", 2.0f, AnimationFlags.None).WaitForCompletion();

                GameFiber.Wait(250);
            }

            VetPed.PlayAmbientSpeech(Speech.GENERIC_BYE);

            GameFiber.Wait(185);

            NativeFunction.Natives.TASK_GO_TO_ENTITY(VetPed, Vehicle, -1, 5.0f, 1.0f, 0, 0);
            while (Vector3.Distance(VetPed.Position, Vehicle.Position) > 6.0f)
                GameFiber.Yield();


            VetPed.Tasks.EnterVehicle(Vehicle, -1).WaitForCompletion(10000); // The vet enters his vehicle

            if (!VetPed.IsInVehicle(Vehicle, false)) // If the vet isn't in the vehicle, is warped into the vehicle
            {
                VetPed.WarpIntoVehicle(Vehicle, -1);
            }

            VetPed.Tasks.CruiseWithVehicle(20.0f, VehicleDrivingFlags.Normal); // The vets drive away

            GameFiber.Wait(2500);

            if (cleanUpOnEnd)
            {
                CleanUp();
            }
        }

        public void CleanUp()
        {
            if (VetPed.Exists())
                VetPed.Dismiss();
            if (Vehicle.Exists())
                Vehicle.Dismiss();
            if (_vehBlip.Exists())
                _vehBlip.Delete();
            if (_fakeAnimal.Exists())
                _fakeAnimal.Dismiss();
        }

        public static Model GetVetPedModel()
        {
            return Settings.Vet.PedModels.GetRandomElement();
        }

        public static SpawnPoint GetSpawnPoint()
        {
            Vector3 vetSpawnPos =
                World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.AroundPosition(190.0f));
            while (vetSpawnPos.DistanceTo(Game.LocalPlayer.Character.Position) < 52.5f)
            {
                vetSpawnPos = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.AroundPosition(190.0f));
                GameFiber.Yield();
            }

            return new SpawnPoint(vetSpawnPos, vetSpawnPos.GetClosestVehicleNodeHeading());
        }
    }
}