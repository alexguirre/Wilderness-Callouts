namespace WildernessCallouts
{
    // System
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Drawing;

    // RPH
    using Rage;
    using Rage.Native;

    // WildernessCallouts
    using WildernessCallouts.Types;

    internal static class Extensions
    {
        /// <summary>
        /// Gets a random position inside the defined radius
        /// </summary>
        /// <param name="v3"></param>
        /// <param name="radius">Radius</param>
        /// <returns>a random position inside the defined radius</returns>
        public static Vector3 AroundPosition(this Vector3 v3, float radius)
        {
            return v3 = v3 + new Vector3(MathHelper.GetRandomSingle(-radius, radius), MathHelper.GetRandomSingle(-radius, radius), 0.0f);
        }


        /// <summary>
        /// Returns the distance between to positions
        /// </summary>
        /// <param name="v3"></param>
        /// <param name="end">End position</param>
        /// <returns>the distance between to positions</returns>
        public static float DistanceTo(this Vector3 v3, Vector3 end)
        {
            return (end - v3).Length();
        }

        /// <summary>
        /// Returns a position in a sidewalk or similar
        /// </summary>
        /// <param name="v3"></param>
        /// <returns>a position in a sidewalk</returns>
        public static Vector3 GetSafeCoordinatesForPed(this Vector3 position)
        {
            Vector3 vector3 = Vector3.Zero;
            if (NativeFunction.Natives.GET_SAFE_COORD_FOR_PED<bool>(position.X, position.Y, position.Z, true, out vector3.X, out vector3.Y, out vector3.Z, 0))
            {
                return vector3;
            }
            else
            {
                return Vector3.Zero;
            }
        }


        /// <summary>
        /// Returns the ground Z float from a vector3
        /// </summary>
        /// <param name="v3"></param>
        /// <returns>the ground Z float from a vector3</returns>
        public static float GetGroundZ(this Vector3 v3)
        {

            float z;
            NativeFunction.Natives.GET_GROUND_Z_FOR_3D_COORD(v3.X, v3.Y, 1250.0f, out z);
            return z;

        }


        /// <summary>
        /// Returns the heading of the closest vehicle node
        /// </summary>
        /// <param name="v3"></param>
        /// <returns>the heading of the closest vehicle node</returns>
        public static float GetClosestVehicleNodeHeading(this Vector3 v3)
        {
            float outHeading;
            Vector3 outPosition;

            NativeFunction.Natives.GET_CLOSEST_VEHICLE_NODE_WITH_HEADING(v3.X, v3.Y, v3.Z, out outPosition, out outHeading, 12, 0x40400000, 0);

            return outHeading;
        }


        /// <summary>
        /// Gets the heading towards an entity
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="towardsEntity">Entity to face to</param>
        /// <returns>the heading towards an entity</returns>
        public static float GetHeadingTowards(this ISpatial spatial, ISpatial towards)
        {
            return GetHeadingTowards(spatial, towards.Position);
        }


        /// <summary>
        /// Gets the heading towards a position
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="towardsPosition">Position to face to</param>
        /// <returns>the heading towards a position</returns>
        public static float GetHeadingTowards(this ISpatial spatial, Vector3 towardsPosition)
        {
            return GetHeadingTowards(spatial.Position, towardsPosition);
        }
        
        
        /// <summary>
        /// Gets the heading towards an entity
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="towardsEntity">Entity to face to</param>
        /// <returns>the heading towards an entity</returns>
        public static float GetHeadingTowards(this Vector3 position, Vector3 towardsPosition)
        {
            Vector3 directionFromEntityToPosition = (towardsPosition - position);
            directionFromEntityToPosition.Normalize();

            float heading = MathHelper.ConvertDirectionToHeading(directionFromEntityToPosition);
            return heading;
        }

        /// <summary>
        /// Gets the heading towards an entity
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="towardsEntity">Entity to face to</param>
        /// <returns>the heading towards an entity</returns>
        public static float GetHeadingTowards(this Vector3 position, ISpatial towards)
        {
            return GetHeadingTowards(position, towards.Position);
        }

        /// <summary>
        /// Returns a value indicating if this Rage.Entity instance is playing the chosen animation
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="animationDictionary">Animation dictionary in which the animation is</param>
        /// <param name="animationName">Animation name</param>
        /// <returns>a value indicating if this Rage.Entity instance is playing the chosen animation</returns>
        public static bool IsPlayingAnimation(this Entity entity, AnimationDictionary animationDictionary, string animationName)
        {
            return NativeFunction.Natives.IS_ENTITY_PLAYING_ANIM<bool>(entity, (string)animationDictionary, animationName, 3);
        }

        /// <summary>
        /// Returns this Entity cardinal direction. Low detailed: N, E, S, W
        /// </summary>
        /// <param name="e"></param>
        /// <returns>this Entity cardinal direction</returns>
        public static string GetCardinalDirectionLowDetailed(this Entity e)
        {
            float degrees = e.Heading;
            string[] cardinals = { "N", "W", "S", "E", "N" };
            return cardinals[(int)Math.Round(((double)degrees % 360) / 90)];
        }


        /// <summary>
        /// Returns this Entity cardinal direction. Normal: N, NE, E, SE, S, SW, W, NW
        /// </summary>
        /// <param name="e"></param>
        /// <returns>this Entity cardinal direction</returns>
        public static string GetCardinalDirection(this Entity e)
        {
            float degrees = e.Heading;
            string[] cardinals = { "N", "NW", "W", "SW", "S", "SE", "E", "NE", "N" };
            return cardinals[(int)Math.Round(((double)degrees % 360) / 45)];
        }


        /// <summary>
        /// Returns this Entity cardinal direction. Detailed: N, NNE, NE, ENE, E, ESE, SE, SSE, S, SSW, SW, WSW, W, WNW, NW, NNW, N
        /// </summary>
        /// <param name="e"></param>
        /// <returns>this Entity cardinal direction</returns>
        public static string GetCardinalDirectionDetailed(this Entity e)
        {
            float degrees = e.Heading;
            degrees *= 10;

            string[] cardinals = { "N", "NNW", "NW", "WNW", "W", "WSW", "SW", "SSW", "S", "SSE", "SE", "ESE", "E", "ENE", "NE", "NNE", "N" };
            return cardinals[(int)Math.Round(((double)degrees % 3600) / 225)];
        }

        /// <summary>
        /// Attaches this Rage.Entity to another entity
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="entityToAttach">Entity to attach to</param>
        /// <param name="boneIndex">Entity bone to atttach to</param>
        /// <param name="offset">Entity to attach to offset</param>
        /// <param name="rotation">Entity rotation</param>
        public static void AttachToEntity(this Entity entity, Entity entityToAttach, int boneIndex, Vector3 offset, Rotator rotation)
        {
            NativeFunction.Natives.ATTACH_ENTITY_TO_ENTITY(entity, entityToAttach, boneIndex, offset.X, offset.Y, offset.Z, rotation.Pitch, rotation.Roll, rotation.Yaw, false, true, false, true, 2, true);
        }


        /// <summary>
        /// Returns the bone index. For peds use GetPedBoneIndex()
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boneName">The bone name</param>
        /// <returns>the bone index</returns>
        public static int GetEntityBoneIndex(this Entity e, string boneName)
        {
            return NativeFunction.Natives.xfb71170b7e76acba<int>(e, boneName);
        }

        /// <summary>
        /// Suicides this Rage.Ped instance with a weapon
        /// </summary>
        /// <param name="ped"></param>
        /// <param name="weapon">Weapon to use</param>
        public static void SuicideWeapon(this Ped ped, WeaponAsset weapon)
        {
            ped.Inventory.GiveNewWeapon(weapon, 10, true);

            ped.Tasks.PlayAnimation("mp_suicide", "pistol", 8.0f, AnimationFlags.None);

            GameFiber.Wait(700);

            if (ped.IsPlayingAnimation("mp_suicide", "pistol"))
            {
                NativeFunction.Natives.SET_PED_SHOOTS_AT_COORD(ped, 0.0f, 0.0f, 0.0f, 0);

                WildernessCallouts.Common.StartParticleFxNonLoopedOnEntity("scr_solomon3", "scr_trev4_747_blood_impact", (Entity)ped, new Vector3(0.0f, 0.0f, 0.6f), new Rotator(90.0f, 0.0f, 0.0f), 0.25f);

                GameFiber.Wait(1000);

                ped.Kill();
            }
        }


        /// <summary>
        /// Suicides this Rage.Ped instance with a pill
        /// </summary>
        /// <param name="ped"></param>
        public static void SuicidePill(this Ped ped)
        {
            ped.Tasks.PlayAnimation("mp_suicide", "pill", 1.336f, AnimationFlags.None);

            GameFiber.Wait(2250);

            if (ped.IsPlayingAnimation("mp_suicide", "pill"))
            {
                ped.Kill();
            }
        }

        /// <summary>
        /// Makes a ped run away from another ped
        /// </summary>
        /// <param name="ped"></param>
        /// <param name="fleeTarget">Ped to flee from</param>
        /// <param name="distance">Distance to flee</param>
        /// <param name="fleeTime">Time to flee</param>
        public static Task Flee(this Ped ped, Ped fleeTarget, float distance, int fleeTime)
        {
            NativeFunction.Natives.TASK_SMART_FLEE_PED(ped, fleeTarget, distance, fleeTime, true, true);
            return Task.GetTask(ped, "TASK_SMART_FLEE_PED");
        }

        public static Task ReactAndFlee(this Ped ped, Ped fleeTarget)
        {
            NativeFunction.Natives.TASK_REACT_AND_FLEE_PED(ped, fleeTarget);
            return Task.GetTask(ped, "TASK_REACT_AND_FLEE_PED");
        }

        /// <summary>
        /// Makes this Rage.Ped instance attack another Rage.Ped instance 
        /// </summary>
        /// <param name="ped"></param>
        /// <param name="targetPed">Ped to attack</param>
        public static Task AttackPed(this Ped ped, Ped targetPed)
        {
            NativeFunction.Natives.TASK_COMBAT_PED(ped, targetPed, 0, 1);
            return Task.GetTask(ped, "TASK_COMBAT_PED");
        }

        /// <summary>
        /// Makes a ped drive wandering
        /// </summary>
        /// <param name="ped">Ped to assing task</param>
        /// <param name="vehicle">Vehicle to use</param>
        /// <param name="speed">Driving speed</param>
        /// <param name="drivingStyle">Driving style
        /// 0 = Rushed
        /// 1 = Ignore Traffic Lights
        /// 2 = Fast
        /// 3 = Normal (Stop in Traffic)
        /// 4 = Fast avoid traffic
        /// 5 = Fast, stops in traffic but overtakes sometimes
        /// 6 = Fast avoids traffic extremely
        /// 786603 = Wait at traffic lights??
        /// </param>
        public static Task DriveWander(this Ped ped, Vehicle vehicle, float speed, int drivingStyle)
        {
            NativeFunction.Natives.TASK_VEHICLE_DRIVE_WANDER(ped, vehicle, speed, drivingStyle);
            return Task.GetTask(ped, "TASK_VEHICLE_DRIVE_WANDER");
        }


        /// <summary>
        /// Makes a ped look at an entity
        /// </summary>
        /// <param name="ped"></param>
        /// <param name="lookAt">Entity to look at</param>
        /// <param name="duration">Duration in ms, use -1 to look forever</param>
        public static Task LookAtEntity(this Ped ped, Entity lookAt, int duration)
        {
            NativeFunction.Natives.TASK_LOOK_AT_ENTITY(ped, lookAt, duration, 2048, 3);
            return Task.GetTask(ped, "TASK_LOOK_AT_ENTITY");
        }


        /// <summary>
        /// Makes a ped enter a vehicel
        /// </summary>
        /// <param name="ped"></param>
        /// <param name="vehicle">Vehicle to enter</param>
        /// <param name="timeout">Time to do the task</param>
        /// <param name="seat">Seat to enter</param>
        /// <param name="speed">Speed 
        ///  1.0 = walk,
        ///  2.0 = run
        /// </param>
        /// <param name="type">Enter type
        ///  1 = normal, 
        ///  3 = teleport to vehicle, 
        ///  16 = teleport directly into vehicle</param>
        public static Task EnterVehicle(this Ped ped, Vehicle vehicle, int timeout, EVehicleSeats seat, float speed, int type)
        {
            NativeFunction.Natives.TASK_ENTER_VEHICLE(ped, vehicle, timeout, (int)seat, speed, type, 0);
            return Task.GetTask(ped, "TASK_ENTER_VEHICLE");
        }


        /// <summary>
        /// Gets a value indicating this Ped is an animal
        /// </summary>
        /// <param name="ped"></param>
        /// <returns> a value indicating this Ped is an animal</returns>
        public static bool IsAnimal(this Ped ped)
        {
            if (!ped.IsHuman) return true;
            else return false;
        }


        /// <summary>
        /// Change the default walking animation from a ped
        /// </summary>
        /// <param name="ped"></param>
        /// <param name="animationSet">Animation set name</param>
        public static void SetMovementAnimationSet(this Ped ped, string animationSet)
        {
            AnimationSet moveSet = new AnimationSet(animationSet);
            moveSet.LoadAndWait();
            ped.MovementAnimationSet = moveSet;
        }

        /// <summary>
        /// Change the default walking animation from a ped
        /// </summary>
        /// <param name="ped"></param>
        /// <param name="animationSet">Animation set name</param>
        public static void SetStrafeAnimationSet(this Ped ped, string animationSet)
        {
            AnimationSet strafeSet = new AnimationSet(animationSet);
            strafeSet.LoadAndWait();
            NativeFunction.Natives.SET_PED_STRAFE_CLIPSET(ped, strafeSet.Name);
        }

        /// <summary>
        /// Clones this Rage.Ped instance
        /// </summary>
        /// <param name="ped"></param>
        /// <param name="heading">Cloned ped heading</param>
        /// <returns>a Rage.Ped clone of this Rage.Ped instance</returns>
        public static Ped Clone(this Ped ped, float heading)
        {
            Ped clonedPed = NativeFunction.Natives.ClonePed<Ped>(ped, heading, false, true);
            return clonedPed;
        }


        public static EPaint GetPrimaryColor(this Vehicle veh)
        {

            int colorPrimaryInt;
            int colorSecondaryInt;

            NativeFunction.Natives.xa19435f193e081ac(veh, out colorPrimaryInt, out colorSecondaryInt); //GetVehicleColours

            return (EPaint)colorPrimaryInt;

        }

        public static EPaint GetSecondaryColor(this Vehicle veh)
        {

            int colorPrimaryInt;
            int colorSecondaryInt;

            NativeFunction.Natives.xa19435f193e081ac(veh, out colorPrimaryInt, out colorSecondaryInt); //GetVehicleColours

            return (EPaint)colorSecondaryInt;
        }

        public static void GetColors(this Vehicle veh, out EPaint primaryColor, out EPaint secondaryColor)
        {
            int colorPrimaryInt;
            int colorSecondaryInt;


            NativeFunction.Natives.xa19435f193e081ac(veh, out colorPrimaryInt, out colorSecondaryInt); //GetVehicleColours

            primaryColor = (EPaint)colorPrimaryInt;
            secondaryColor = (EPaint)colorSecondaryInt;
        }


        /// <summary>
        /// Sets the color to this Rage.Vehicle instance
        /// </summary>
        /// <param name="v"></param>
        /// <param name="primaryColor">The primary color</param>
        /// <param name="secondaryColor">The secondary color</param>
        public static void SetColors(this Vehicle v, EPaint primaryColor, EPaint secondaryColor)
        {
            NativeFunction.Natives.SET_VEHICLE_COLOURS(v, (int)primaryColor, (int)secondaryColor);
        }

        public static string ToFriendlyName(this EPaint color)
        {
            return Enum.GetName(typeof(EPaint), color).Replace('_', ' ');
        }

        /// <summary>
        /// Sets the selected livery
        /// </summary>
        /// <param name="v"></param>
        /// <param name="liveryIndex">The livery to set</param>
        public static void SetLivery(this Vehicle v, int liveryIndex)
        {
            NativeFunction.Natives.SET_VEHICLE_LIVERY(v, liveryIndex);
        }

        /// <summary>
        /// Returns the enum of the area
        /// </summary>
        /// <param name="position"></param>
        /// <returns>the name of the area</returns>
        public static EWorldArea GetArea(this Vector3 position)
        {
            return WorldZone.GetArea(position);
        }
        /// <summary>
        /// Returns the name of the zone
        /// </summary>
        /// <param name="position"></param>
        /// <returns>the name of the zone</returns>
        public static string GetAreaName(this Vector3 position)
        {
            return WorldZone.GetAreaName(WorldZone.GetArea(position));
        }


        /// <summary>
        /// Returns the enum of the zone
        /// </summary>
        /// <param name="position"></param>
        /// <returns>the name of the zone</returns>
        public static EWorldZone GetZone(this Vector3 position)
        {
            return WorldZone.GetZone(position);
        }
        /// <summary>
        /// Returns the name of the zone
        /// </summary>
        /// <param name="position"></param>
        /// <returns>the name of the zone</returns>
        public static string GetZoneName(this Vector3 position)
        {
            return WorldZone.GetZoneName(WorldZone.GetZone(position));
        }


        /// <summary>
        /// Gets the street name
        /// </summary>
        /// <param name="position">Position</param>
        /// <returns>the street name</returns>
        public static string GetStreetName(this Vector3 position)
        {
            return World.GetStreetName(position);
        }


        ///// <summary>
        ///// Returns a ExtendedPosition
        ///// </summary>
        ///// <param name="position">Position</param>
        ///// <returns>a ExtendedPosition</returns>
        //public static ExtendedPosition GetExtendedPosition(this Vector3 position)
        //{
        //    ExtendedPosition z = new ExtendedPosition();
        //    z.Position = position;
        //    return z;
        //}

        public static Vector3 ToGround(this Vector3 position)
        {
            return new Vector3(position.X, position.Y, position.GetGroundZ());
        }

        public static Vector3 ToGroundUsingRaycasting(this Vector3 position, Entity toIgnore)
        {
            return new Vector3(position.X, position.Y, position.GetGroundHeightUsingRaycasting(toIgnore));
        }

        public static void SetName(this Blip blip, string text)
        {
            NativeFunction.Natives.BEGIN_TEXT_COMMAND_SET_BLIP_NAME("STRING");
            NativeFunction.Natives.x6c188be134e074aa(text); //AddTextComponentString
            NativeFunction.Natives.END_TEXT_COMMAND_SET_BLIP_NAME(blip);
        }

        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                int k = Globals.Random.Next(n--);
                T temp = list[n];
                list[n] = list[k];
                list[k] = temp;
            }
        }

        public static T GetRandomElement<T>(this IList<T> list, bool shuffle = false)
        {
            if (list == null || list.Count <= 0)
                return default(T);

            if (shuffle) list.Shuffle();
            return list[Globals.Random.Next(list.Count)];
        }

        public static T GetRandomElement<T>(this IEnumerable<T> enumarable, bool shuffle = false)
        {
            if (enumarable == null || enumarable.Count() <= 0)
                return default(T);

            T[] array = enumarable.ToArray();
            return GetRandomElement(array, shuffle);
        }

        public static T GetRandomElement<T>(this Enum items)
        {
            if (typeof(T).BaseType != typeof(Enum))
                throw new InvalidCastException();

            var types = Enum.GetValues(typeof(T));
            return GetRandomElement<T>(types.Cast<T>());
        }

        public static IList<T> GetRandomNumberOfElements<T>(this IList<T> list, int numOfElements, bool shuffle = false)
        {
            List<T> givenList = new List<T>(list);
            List<T> l = new List<T>();
            for (int i = 0; i < numOfElements; i++)
            {
                T t = givenList.GetRandomElement(shuffle);
                givenList.Remove(t);
                l.Add(t);
            }
            return l;
        }

        public static IEnumerable<T> GetRandomNumberOfElements<T>(this IEnumerable<T> enumarable, int numOfElements, bool shuffle = false)
        {
            List<T> givenList = new List<T>(enumarable);
            List<T> l = new List<T>();
            for (int i = 0; i < numOfElements; i++)
            {
                T t = givenList.Except(l).GetRandomElement(shuffle);
                l.Add(t);
            }
            return l;
        }

        public static void InstallRandomMods(this Vehicle vehicle)
        {
            vehicle.Mods.InstallModKit();

            for (int i = 0; i <= 100; i++)
                NativeFunction.Natives.SET_VEHICLE_MOD( vehicle, i, Globals.Random.Next(NativeFunction.Natives.GET_NUM_VEHICLE_MODS<int>(vehicle, i)), false);

            vehicle.Mods.HasTurbo = Globals.Random.Next(2) == 1;
            vehicle.Mods.HasXenonHeadlights = Globals.Random.Next(2) == 1;
            ToggleMod(vehicle, VehicleModType.TireSmoke, Globals.Random.Next(2) == 1);
            vehicle.SetTyreSmokeColor(Color.FromArgb(MathHelper.GetRandomInteger(1, 255), MathHelper.GetRandomInteger(1, 255), MathHelper.GetRandomInteger(1, 255)));

            VehicleWheelType wheelType = default(VehicleWheelType).GetRandomElement<VehicleWheelType>();
            vehicle.Mods.SetWheelMod(wheelType, Globals.Random.Next(vehicle.Mods.GetWheelModCount(wheelType)), Globals.Random.Next(2) == 1);


            EWindowTint windTint = default(EWindowTint).GetRandomElement<EWindowTint>();
            vehicle.SetWindowsTint(windTint);

            vehicle.SetColors(default(EPaint).GetRandomElement<EPaint>(), default(EPaint).GetRandomElement<EPaint>());

            vehicle.SetExtraColors(default(EPaint).GetRandomElement<EPaint>(), default(EPaint).GetRandomElement<EPaint>());

            foreach (ENeonLights neon in Enum.GetValues(typeof(ENeonLights)))
                vehicle.ToggleNeonLight(neon, Globals.Random.Next(2) == 1);

            vehicle.SetNeonLightsColor(Color.FromArgb(Globals.Random.Next(1, 256), Globals.Random.Next(1, 256), Globals.Random.Next(1, 256)));

            NativeFunction.Natives.xf40dd601a65f7f19(vehicle, (int)default(EPaint).GetRandomElement<EPaint>());//_SET_VEHICLE_INTERIOR_COLOUR
            NativeFunction.Natives.x6089cdf6a57f326c(vehicle, (int)default(EPaint).GetRandomElement<EPaint>());//_SET_VEHICLE_DASHBOARD_COLOUR
        }

        /// <summary>
        /// Toggles the neon light in a vehicle
        /// </summary>
        /// <param name="vehicle"></param>
        /// <param name="neonLight">Neon index</param>
        /// <param name="toggle">Toggle the neon</param>
        public static void ToggleNeonLight(this Vehicle vehicle, ENeonLights neonLight, bool toggle)
        {
            NativeFunction.Natives.x2aa720e4287bf269(vehicle, (int)neonLight, toggle); //SetVehicleNeonLightEnabled
        }


        /// <summary>
        /// Sets the neon light color
        /// </summary>
        /// <param name="vehicle"></param>
        /// <param name="color">Color to set</param>
        public static void SetNeonLightsColor(this Vehicle vehicle, Color color)
        {
            NativeFunction.Natives.x8e0a582209a62695(vehicle, (int)color.R, (int)color.G, (int)color.B);// SetVehicleNeonLightsColours
        }


        /// <summary>
        /// Returns true if the neon light is enabled
        /// </summary>
        /// <param name="vehicle"></param>
        /// <param name="neonLight">Neon index</param>
        /// <returns>true if the neon light is enabled</returns>
        public static bool IsNeonLightEnable(this Vehicle vehicle, ENeonLights neonLight)
        {
            //IsVehicleNeonLightEnabled
            if (NativeFunction.Natives.x8c4b92553e4766a5<bool>(vehicle, (int)neonLight)) return true;
            else if (!NativeFunction.Natives.x8c4b92553e4766a5<bool>(vehicle, (int)neonLight)) return false;
            else return false;
        }


        /// <summary>
        /// Returns the neon light color
        /// </summary>
        /// <param name="vehicle"></param>
        /// <returns>the neon light color</returns>
        public static Color GetNeonLightsColor(this Vehicle vehicle)
        {
            Color color;
            int red;
            int green;
            int blue;
            NativeFunction.Natives.x7619eee8c886757f(vehicle, out red, out green, out blue); //GetVehicleNeonLightsColour

            return color = Color.FromArgb(red, green, blue);
        }

        public static void ToggleMod(this Vehicle vehicle, VehicleModType mod, bool toggle)
        {
            NativeFunction.Natives.TOGGLE_VEHICLE_MOD(vehicle, (int)mod, toggle);
        }

        public static void SetExtraColors(this Vehicle vehicle, EPaint pearlescentColor, EPaint wheelColor)
        {
            NativeFunction.Natives.SET_VEHICLE_EXTRA_COLOURS(vehicle, (int)pearlescentColor, (int)wheelColor);
            //SET_VEHICLE_EXTRA_COLOURS(Vehicle vehicle, int pearlescentColor, int wheelColor)
        }
        public static void GetExtraColors(this Vehicle vehicle, out EPaint pearlescentColor, out EPaint wheelColor)
        {
            int pearl, wheel;
            NativeFunction.Natives.SET_VEHICLE_EXTRA_COLOURS(vehicle, out pearl, out wheel);
            pearlescentColor = (EPaint)pearl;
            wheelColor = (EPaint)wheel;
        }


        public static void SetWindowsTint(this Vehicle vehicle, EWindowTint tint)
        {
            NativeFunction.Natives.SET_VEHICLE_WINDOW_TINT(vehicle, (int)tint);
        }

        public static EWindowTint GetWindowsTint(this Vehicle vehicle)
        {
            return (EWindowTint)NativeFunction.Natives.GET_VEHICLE_WINDOW_TINT<int>(vehicle);
        }

        public static void SetTyreSmokeColor(this Vehicle vehicle, Color color)
        {
            NativeFunction.Natives.SET_VEHICLE_TYRE_SMOKE_COLOR(vehicle, (int)color.R, (int)color.G, (int)color.B);
        }
        public static Color GetTyreSmokeColor(this Vehicle vehicle)
        {
            int r, g, b;
            NativeFunction.Natives.GET_VEHICLE_TYRE_SMOKE_COLOUR(vehicle, out r, out g, out b);
            return Color.FromArgb(r, g, b);
        }


        public static float GetGroundHeightUsingRaycasting(this Entity ent)
        {
            Vector3 start = ent.Position;
            Vector3 end = start + Vector3.WorldDown * 1000f;

            HitResult hr = World.TraceLine(start, end, TraceFlags.IntersectWorld, ent);
            return hr.HitPosition.Z;
        }
        public static float GetGroundHeightUsingRaycasting(this Vector3 v3, Entity toIgnore)
        {
            Vector3 start = v3;
            Vector3 end = start + Vector3.WorldDown * 50f;

            HitResult hr = World.TraceLine(start, end, TraceFlags.IntersectWorld, toIgnore);
            return hr.HitPosition.Z;
        }


        public static void FollowPointRoute(this Ped ped, Vector3[] points, float speed)
        {
            NativeFunction.Natives.TASK_FLUSH_ROUTE();
            foreach (Vector3 v3 in points)
            {
                NativeFunction.Natives.TASK_EXTEND_ROUTE(v3.X, v3.Y, v3.Z);
            }
            NativeFunction.Natives.TASK_FOLLOW_POINT_ROUTE(ped, speed, 0);
        }

        public static WeaponDescriptor GiveNewWeapon(this PedInventory inventory, EWeaponHash weaponHash, short ammoCount, bool equipNow)
        {
            return inventory.GiveNewWeapon((uint)weaponHash, ammoCount, equipNow);
        }

        public static void WanderInArea(this Ped ped, Vector3 position, float radius, float minimalLenght, float timeBetweenWalks)
        {
            NativeFunction.Natives.TASK_WANDER_IN_AREA(ped, position.X, position.Y, position.Z, radius, minimalLenght, timeBetweenWalks);
        }

        public static bool IsPed(this Entity entity)
        {
            return NativeFunction.Natives.IS_ENTITY_A_PED<bool>(entity);
        }

        public static bool IsVehicle(this Entity entity)
        {
            return NativeFunction.Natives.IS_ENTITY_A_VEHICLE<bool>(entity);
        }

        public static bool IsObject(this Entity entity)
        {
            return NativeFunction.Natives.IS_ENTITY_AN_OBJECT<bool>(entity);
        }

        public static List<EVehicleSeats> GetFreeSeats(this Vehicle vehicle)
        {
            List<EVehicleSeats> seats = new List<EVehicleSeats>();
            foreach (EVehicleSeats seat in Enum.GetValues(typeof(EVehicleSeats)))
            {
                if (vehicle.IsSeatFree((int)seat))
                {
                    seats.Add(seat);
                }
            }
            return seats;
        }

        public static void Escort(this Ped ped, Vehicle pedVehicle, Vehicle targetVehicle, float speed, int drivingStyle, float minDistance, float ignoreRoadsDistance)
        {
            NativeFunction.Natives.TASK_VEHICLE_ESCORT(ped, pedVehicle, targetVehicle, -1, speed, drivingStyle, minDistance, -1, ignoreRoadsDistance);
        }


        public static bool IsInRangeOf(this Vector3 s, Vector3 position, float distance)
        {
            return s.DistanceTo(position) < distance;
        }
        public static bool IsInRangeOf(this ISpatial s, Vector3 position, float distance)
        {
            return s.DistanceTo(position) < distance;
        }


        public static bool IsInRangeOf(this Vector3 s, ISpatial spatial, float distance)
        {
            return s.DistanceTo(spatial) < distance;
        }
        public static bool IsInRangeOf(this ISpatial s, ISpatial spatial, float distance)
        {
            return s.DistanceTo(spatial) < distance;
        }


        public static bool IsInRangeOf2D(this Vector3 s, Vector3 position, float distance)
        {
            return s.DistanceTo2D(position) < distance;
        }
        public static bool IsInRangeOf2D(this ISpatial s, Vector3 position, float distance)
        {
            return s.DistanceTo2D(position) < distance;
        }

        public static bool IsInRangeOf2D(this Vector3 s, ISpatial spatial, float distance)
        {
            return s.DistanceTo2D(spatial) < distance;
        }
        public static bool IsInRangeOf2D(this ISpatial s, ISpatial spatial, float distance)
        {
            return s.DistanceTo2D(spatial) < distance;
        }

        /// <summary>
        /// Transition from the <see paramref="from"/> camera to the <see paramref="to"/> camera. Activates the <see paramref="to"/> camera.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="time">The time that the interpolation takes</param>
        /// <param name="easeLocation">Accelerate/decelerate cam speed during movement</param>
        /// <param name="easeRotation"></param>
        /// <param name="waitForCompletion">If true it will wait until the interpolation finishes</param>
        public static void Interpolate(this Camera from, Camera to, int time, bool easeLocation, bool easeRotation, bool waitForCompletion)
        {
            //SET_CAM_ACTIVE_WITH_INTERP(Cam camTo, Cam camFrom, int duration, BOOL easeLocation, BOOL easeRotation)
            NativeFunction.Natives.SET_CAM_ACTIVE_WITH_INTERP(to, from, time, easeLocation, easeRotation);
            if (waitForCompletion)
                GameFiber.Sleep(time);
        }


        //public static bool IsHeadingTowardsWithTolerance(this Entity ent, ISpatial towards, float tolerance)
        //{
        //    //float entHeading = ent.Heading;
        //    //float headingTowards = ent.GetHeadingTowards(towards);

        //    //Logger.LogTrivial("Entity Heading: " + entHeading);
        //    //Logger.LogTrivial("Heading Towards: " + headingTowards);
        //    //Logger.LogTrivial("Entity Heading - Tolerance: " + (entHeading - tolerance));
        //    //Logger.LogTrivial("Entity Heading + Tolerance: " + (entHeading + tolerance));

        //    //if (entHeading - tolerance <= entHeading)
        //    //    return true;
        //    //if (entHeading + tolerance >= entHeading)
        //    //    return true;
        //    //return false;

        //    return Math.Abs(ent.Heading - ent.GetHeadingTowards(towards)) <= tolerance;
        //}
        //public static bool IsHeadingTowardsWithTolerance(this Entity ent, Vector3 towards, float tolerance)
        //{
        //    //float entHeading = ent.Heading;
        //    //float headingTowards = ent.GetHeadingTowards(towards);

        //    //Logger.LogTrivial("Entity Heading: " + entHeading);
        //    //Logger.LogTrivial("Heading Towards: " + headingTowards);
        //    //Logger.LogTrivial("Entity Heading - Tolerance: " + (entHeading - tolerance));
        //    //Logger.LogTrivial("Entity Heading + Tolerance: " + (entHeading + tolerance));

        //    //if (entHeading - tolerance <= entHeading)
        //    //    return true;
        //    //if (entHeading + tolerance >= entHeading)
        //    //    return true;
        //    //return false;

        //    return Math.Abs(ent.Heading - ent.GetHeadingTowards(towards)) <= tolerance;
        //}
    }
}
