namespace WildernessCallouts.Menus
{
    // System
    using System.Drawing;
    using System.Linq;
    using System.Collections.Generic;

    // RPH
    using Rage;

    // RNUI
    using RAGENativeUI;
    using RAGENativeUI.Elements;

    // WildernessCallouts
    using WildernessCallouts.CalloutFunct;
    using WildernessCallouts.Peds;
    using WildernessCallouts.AmbientEvents;
    using WildernessCallouts.Callouts;

    internal static class InteractionMenu
    {
        public static UIMenu MainMenu;

        public static UIMenuItem AskHuntingLicenseItem;
        public static UIMenuItem AskFishingLicenseItem;
        public static UIMenuItem CallAirAmbulanceItem;
        public static UIMenuItem CallVetItem;
        public static UIMenuItem CreateEventItem;
        public static UIMenuItem StartCalloutItem;

        public static List<Ped> StaticPeds { get; } = new List<Ped>();

#if DEBUG
        public static UIMenu CalloutsStartSubMenu;
#endif

        public static void Initialize()
        {
            if(MainMenu != null)
            {
                Logger.LogTrivial("Interaction menu already initialized, aborting initialization");
                return;
            }
            
            MainMenu = new UIMenu("Wilderness Callouts", "~g~Interaction menu");
            MainMenu.AllowCameraMovement = true;
            MainMenu.Title.Color = Color.Black;

            MainMenu.SetBannerType(new ResRectangle(new Point(), new Size(), Color.FromArgb(190, 0, 140, 0)));
            MainMenu.MouseControlsEnabled = false;
           
            MainMenu.AddItem(AskHuntingLicenseItem = new UIMenuItem("~g~Hunting License", "~g~Ask for the hunting license"));
            MainMenu.AddItem(AskFishingLicenseItem = new UIMenuItem("~g~Fishing License", "~g~Ask for the fishing license"));
            MainMenu.AddItem(CallAirAmbulanceItem = new UIMenuItem("~g~Air Ambulance", "~g~Request an air ambulance"));
            MainMenu.AddItem(CallVetItem = new UIMenuItem("~g~" + Settings.Vet.Name, "~g~Request a " + Settings.Vet.Name.ToLower()));
#if DEBUG
            MainMenu.AddItem(CreateEventItem = new UIMenuItem("~g~Create Ambient Event", "~g~Creates a random ambient event, debug purposes"));
            MainMenu.AddItem(StartCalloutItem = new UIMenuItem("~g~Start Callout", "~g~Debug purposes"));
            CalloutsStartSubMenu = new UIMenu("Wilderness Callouts", "~g~Start callout");
            CalloutsStartSubMenu.AllowCameraMovement = true;
            CalloutsStartSubMenu.MouseControlsEnabled = false;
            CalloutsStartSubMenu.Title.Color = Color.Black;
            CalloutsStartSubMenu.SetBannerType(new ResRectangle(new Point(), new Size(), Color.FromArgb(190, 0, 140, 0)));
            string[] calloutsNames = { "AircraftCrash", "AnimalAttack", "Arson", "AttackedPoliceStation", "Demonstration", "HostageSituation", "IllegalHunting", "MissingPerson", "MurderInvestigation", "OfficerNeedsTransport", "PublicDisturbance", "RecklessDriver", "RocksBlockingRoad", "SuicideAttempt", "WantedFelonInVehicle" };
            foreach (string calloutName in calloutsNames)
            {
                CalloutsStartSubMenu.AddItem(new UIMenuItem(calloutName));
            }
            MainMenu.BindMenuToItem(CalloutsStartSubMenu, StartCalloutItem);
            CalloutsStartSubMenu.RefreshIndex();
            CalloutsStartSubMenu.OnItemSelect += StartCalloutSubMenuOnItemSelected;
            MenuCommon.Pool.Add(CalloutsStartSubMenu);
#endif

            MainMenu.RefreshIndex();

            MainMenu.OnItemSelect += MainMenuOnItemSelected;
            MainMenu.OnMenuClose += MainMenuOnMenuClose;

            MenuCommon.Pool.Add(MainMenu);

            Logger.LogTrivial("Interaction menu initialized");
        }

        public static void MainMenuOnItemSelected(UIMenu sender, UIMenuItem selectedItem, int index)
        {
            GameFiber.StartNew(delegate
            {
                if (selectedItem == AskFishingLicenseItem || selectedItem == AskHuntingLicenseItem)
                {
                    Ped closestPed = World.GetAllPeds().Where(x => x.IsAlive && x != Game.LocalPlayer.Character && x.IsHuman && !x.IsInAnyVehicle(false) && Vector3.Distance(x.Position, Game.LocalPlayer.Character.Position) < 12.5f).OrderBy(x => x.Position.DistanceTo(Game.LocalPlayer.Character.Position)).FirstOrDefault();
                    if (closestPed.Exists())
                    {
                        closestPed.Tasks.AchieveHeading(closestPed.GetHeadingTowards(Game.LocalPlayer.Character)).WaitForCompletion(2000);
                        Object id = new Object("hei_prop_hei_id_bank", Vector3.Zero);
                        closestPed.Tasks.PlayAnimation("mp_common", "givetake1_a", 2.5f, AnimationFlags.None);
                        id.AttachToEntity(closestPed, closestPed.GetBoneIndex(PedBoneId.RightPhHand), Vector3.Zero, new Rotator(0.0f, 180.0f, 0.0f));
                        GameFiber.Sleep(1200);
                        if (selectedItem == AskFishingLicenseItem) WildernessCallouts.Common.FishingLicense(closestPed);
                        else if (selectedItem == AskHuntingLicenseItem) WildernessCallouts.Common.HuntingLicense(closestPed);
                        id.Delete();
                        StaticPeds.Add(closestPed);
                    }
                }
                else if (selectedItem == CallAirAmbulanceItem)
                {
                    Ped pedToRescue = World.GetAllPeds().Where(x => (x.IsDead || x.Health <= 20 || x == MissingPerson.CurrentMissingPed) && x != Game.LocalPlayer.Character && x.IsHuman && Vector3.Distance(x.Position, Game.LocalPlayer.Character.Position) < 15.0f && !AirParamedic.RescuedPeds.Contains(x)).OrderBy(x => x.Position.DistanceTo(Game.LocalPlayer.Character.Position)).FirstOrDefault();
                    AirParamedic airpara = new AirParamedic(pedToRescue, "You can leave, we will take care of him, thanks", "You can leave, we will take care of her, thanks");
                    airpara.Start();
                }
                else if (selectedItem == CallVetItem)
                {
                    Ped animal = WildernessCallouts.Common.GetClosestAnimal(Game.LocalPlayer.Character.Position, 30.0f);

                    int timesToLoop = 0;
                    while (timesToLoop < 20)
                    {
                        if (animal.Exists() && animal.IsDead && !Vet.TakenAnimals.Contains(animal))
                            break;
                        animal = WildernessCallouts.Common.GetClosestAnimal(Game.LocalPlayer.Character.Position, 35.0f);
                        timesToLoop++;
                    }           // Gets the closest dead animal
                    if (!animal.Exists() || animal.IsAlive || Vet.TakenAnimals.Contains(animal))
                        return;

                    Vet vet = new Vet(animal);
                    vet.Start();
                }
#if DEBUG
                else if (selectedItem == CreateEventItem)
                {
                    EventPool.CreateEvent();
                }
#endif
            });
        }

        public static void StartCalloutSubMenuOnItemSelected(UIMenu sender, UIMenuItem selectedItem, int index)
        {
            GameFiber.StartNew(delegate
            {
                LSPD_First_Response.Mod.API.Functions.StartCallout(selectedItem.Text);
            });
        }

        public static void MainMenuOnMenuClose(UIMenu sender)
        {
            foreach (Ped p in StaticPeds)
            {
                if(p.Exists())
                    p.Tasks.Clear();
            }   
            StaticPeds.Clear();
        }


        public static void DisEnable()
        {
            if (!MenuCommon.Pool.IsAnyMenuOpen() && !MainMenu.Visible)
                MainMenu.Visible = true;
            else if (MainMenu.Visible) 
                MainMenu.Visible = false;
        }
    }
}
