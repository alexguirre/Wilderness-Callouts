namespace WildernessCallouts.Callouts
{
    using Rage;
    using Rage.Native;
    using LSPD_First_Response.Mod.API;
    using LSPD_First_Response.Mod.Callouts;
    using LSPD_First_Response.Engine.Scripting.Entities;
    using LSPD_First_Response;
    using WildernessCallouts.Types;

    [CalloutInfo("RecklessDriver", CalloutProbability.Medium)]
    internal class RecklessDriver : CalloutBase
    {
        private Ped recklessDriver;
        private Vehicle vehicle;
        private Vehicle possibleTrailer;
        private LHandle pursuit;
        private Vector3 spawnPoint;

        private static string[] vehicleModel = { "monster"   , "marshall"  , "boxville"  , "hotknife"   , "utillitruck" , "utillitruck2", "ztype"     , "brawler"  , "insurgent2", "slamvan2"  , "guardian" ,
                                                 "sandking"  , "sandking2" , "casco"     , "lectro"     , "guardian"    , "kuruma"      , "kuruma2"   , "adder"    , "zentorno"  , "akuma"     , "baller"   , 
                                                 "banshee"   , "dune"      , "dune2"     , "bagger"     , "blazer"      , "blazer3"     , "bobcatxl"  , "handler"  , "dloader"   , "buccaneer2", "chino2"   ,
                                                 "faction"   , "faction2"  , "moonbeam"  , "moonbeam2"  , "primo2"      , "voodoo"      , "panto"     , "prairie"  , "emperor"   , "exemplar"  , "lurcher"  ,
                                                 "rhapsody"  , "pigalle"   , "warrener"  , "blade"      , "glendale"    , "huntley"     , "massacro"  , "thrust"   , "alpha"     , "jester"    , "turismor" ,
                                                 "bifta"     , "kalahari"  , "paradise"  , "furoregt"   , "innovation"  , "coquette2"   , "feltzer3"  , "osiris"   , "windsor"   , "brawler"   , "chino"    ,
                                                 "coquette3" , "vindicator", "t20"       , "btype2"     , "phantom"     , "docktug"     , "packer"    , "hauler"   , "taco"      , "barracks2" , "airbus"   ,
                                                 "bus"       , "rentalbus" , "tourbus"   , "coach"      , "rocoto"      , "comet2"      , "coquette"  , "phantom"  , "packer"    , "hauler"    , "bison"    ,
                                                 "bullet"    , "caddy"     , "caddy2"    , "packer"     , "hauler"      , "barracks2"   , "cheetah"   , "rebel"    , "rebel2"    , "vigero"    , "youga"    ,
                                                 "zion"      , "zion2"     , "washington", "voltic"     , "virgo"       , "bati"        , "phantom"   , "tampa"    , "baller3"   , "baller4"   , "baller5"  ,
                                                 "baller6"   , "cog55"     , "cog552"    , "cognoscenti", "cognoscenti2", "mamba"       , "nightshade", "schafter3", "schafter4" , "schafter5" , "schafter6",
                                                 "verlierer2", "f620"      , "elegy2"    , "surano"     , "primo"       , "sultanrs"    , "banshee2"  , "nemesis"  , "lectro"    , "pcj"       , "carbonrs" ,
                                                 "daemon"    , "double"    , "enduro"    , "ruffian"    , "faction3"    , "minivan2"    , "sabregt2"  , "slamvan3" , "tornado5"  , "virgo2"    , "virgo3"
                                               };

        private static string[] trailerModel = { "armytanker", "armytrailer", "armytrailer2", "docktrailer", "freighttrailer", "tr2", "tr4", "trailers", "trailers2", "trailers3", "trailerlogs", "tvtrailer", "tanker" }; 

        private bool hasStartedPursuit = false;

        public override bool OnBeforeCalloutDisplayed()
        {
            spawnPoint = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.Around(300f));

            if (Vector3.Distance(Game.LocalPlayer.Character.Position, spawnPoint) < 30.0f) return false;

            recklessDriver = new Ped(Vector3.Zero);
            if (!recklessDriver.Exists()) return false;

            string vModel = vehicleModel.GetRandomElement(true);
            for (int i = 0; i < 5; i++)
            {
                if (new Model(vModel).IsValid)
                    break;
                else
                {
                    Logger.LogTrivial(this.GetType().Name, "Vehicle Model < " + vModel + " > is invalid. Choosing new model...");
                    vModel = vehicleModel.GetRandomElement(false);
                }
            }
            if (!new Model(vModel).IsValid)
            {
                Logger.LogTrivial(this.GetType().Name, "Aborting: Final Vehicle Model < " + vModel + " > is invalid");
                return false;
            }
            vehicle = new Vehicle(vModel, spawnPoint, spawnPoint.GetClosestVehicleNodeHeading());
            if (!vehicle.Exists()) return false;

            if (vehicle.Model == new Model("phantom") || vehicle.Model == new Model("docktug") || vehicle.Model == new Model("packer") || vehicle.Model == new Model("hauler") || vehicle.Model == new Model("barracks2"))
            {
                vehicle.TopSpeed = MathHelper.GetRandomSingle(vehicle.TopSpeed + 25, vehicle.TopSpeed + 250f);
                vehicle.DriveForce = MathHelper.GetRandomSingle(vehicle.DriveForce, vehicle.DriveForce + 25f);
                if (Globals.Random.Next(5) <= 3)
                {
                    possibleTrailer = new Vehicle(trailerModel.GetRandomElement(true), vehicle.GetOffsetPosition(new Vector3(0f, -6.0f, 0f)), vehicle.Heading);
                    vehicle.Trailer = possibleTrailer;
                }
            }

            //vehicle.Heading = vehicle.Position.GetClosestVehicleNodeHeading();

            recklessDriver.WarpIntoVehicle(vehicle, -1);
            recklessDriver.BlockPermanentEvents = true;


            if (Globals.Random.Next(5) <= 3) vehicle.InstallRandomMods();

            // Show the user where the pursuit is about to happen and block very close peds.
            this.ShowCalloutAreaBlipBeforeAccepting(spawnPoint, 45f);
            this.AddMinimumDistanceCheck(10f, vehicle.Position);

            // Set up our callout message and location
            this.CalloutMessage = "Reckless driver";
            this.CalloutPosition = spawnPoint;

            Functions.PlayScannerAudioUsingPosition("ATTENTION_ALL_UNITS CRIME_RECKLESS_DRIVER IN_OR_ON_POSITION UNITS_RESPOND_CODE_03", spawnPoint);

            return base.OnBeforeCalloutDisplayed();
        }

        public override bool OnCalloutAccepted()
        {
            pursuit = Functions.CreatePursuit();
            Functions.AddPedToPursuit(pursuit, recklessDriver);
            hasStartedPursuit = true;
            Functions.RequestBackup(spawnPoint.Around(20.0f), EBackupResponseType.Pursuit, EBackupUnitType.LocalUnit);
            //monsterTruck.DriveForce = 5.0f;
            NativeFunction.CallByName<uint>("SET_DRIVER_ABILITY", recklessDriver, MathHelper.GetRandomSingle(0.0f, 100.0f));
            VehicleDrivingFlags driveFlags = VehicleDrivingFlags.None;
            switch (Globals.Random.Next(3))
            {
                case 0:
                    driveFlags = (VehicleDrivingFlags)20;
                    break;
                case 1:
                    driveFlags = (VehicleDrivingFlags)786468;
                    break;
                case 2:
                    if (!vehicle.Model.IsBike || !vehicle.Model.IsBicycle) driveFlags = (VehicleDrivingFlags)1076;
                    else driveFlags = (VehicleDrivingFlags)786468;
                    break;
                default:
                    break;
            }
            if (vehicle.Model.IsBike || vehicle.Model.IsBicycle) recklessDriver.GiveHelmet(false, HelmetTypes.RegularMotorcycleHelmet, -1);
            recklessDriver.Tasks.CruiseWithVehicle(vehicle, 200.0f, driveFlags);


            return base.OnCalloutAccepted();
        }

        public override void OnCalloutNotAccepted()
        {
            base.OnCalloutNotAccepted();

            if (recklessDriver.Exists()) recklessDriver.Delete();
            if (vehicle.Exists()) vehicle.Delete();
            if (possibleTrailer.Exists()) possibleTrailer.Delete();
            if (hasStartedPursuit) if (Functions.IsPursuitStillRunning(pursuit)) Functions.ForceEndPursuit(pursuit);
        }

        public override void Process()
        {
            base.Process();

            if (!recklessDriver.Exists() || (!Functions.IsPursuitStillRunning(pursuit) || recklessDriver.IsDead || Functions.IsPedArrested(recklessDriver)))
                this.End();
        }


        public override void End()
        {
            base.End();

            if (recklessDriver.Exists()) recklessDriver.Dismiss();
            if (vehicle.Exists()) vehicle.Dismiss();
            if (possibleTrailer.Exists()) possibleTrailer.Dismiss();
            if (hasStartedPursuit && Functions.IsPursuitStillRunning(pursuit)) Functions.ForceEndPursuit(pursuit);
        }

        protected override void CleanUp()
        {
            // TODO: implement CleanUp()
        }
    }
}
