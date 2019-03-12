namespace WildernessCallouts
{
    // RPH
    using Rage;
    using Rage.Native;

    // LSPDFR
    using LSPD_First_Response.Mod.API;

    internal enum Controls
    {
        PrimaryAction,
        SecondaryAction,

        ForceCalloutEnd,

        ToggleBinoculars,
        ToggleBinocularsHeliCamNightVision,
        ToggleBinocularsHeliCamThermalVision,

        ToggleInteractionMenu,

        ToggleHeliCam,
        HeliCamScan,

        ToggleCalloutsMenu,
    }

    internal static class ControlsExtensions
    {
        public static bool IsUsingController
        {
            get
            {
                return !NativeFunction.CallByHash<bool>(0xa571d46727e2b718, 2);
            }
        }

        public static bool IsJustPressed(this Controls control)
        {
            if (Functions.IsPoliceComputerActive())
                return false;

            bool modifierValue = false;
            bool controlValue = false;

            if (Settings.ControlsKeys.ModifierKey == System.Windows.Forms.Keys.None)
            {
                modifierValue = true;
            }
            else
            {
                modifierValue = Game.IsKeyDownRightNow(Settings.ControlsKeys.ModifierKey);
            }

            switch (control)
            {
                case Controls.PrimaryAction:
                    controlValue = Game.IsKeyDown(Settings.ControlsKeys.PrimaryActionKey);
                    break;
                case Controls.SecondaryAction:
                    controlValue = Game.IsKeyDown(Settings.ControlsKeys.SecondaryActionKey);
                    break;

                case Controls.ForceCalloutEnd:
                    controlValue = Game.IsKeyDown(Settings.ControlsKeys.ForceCalloutEndKey);
                    break;

                case Controls.ToggleBinoculars:
                    controlValue = Game.IsKeyDown(Settings.ControlsKeys.ToggleBinocularsKey);
                    break;

                case Controls.ToggleBinocularsHeliCamNightVision:
                    controlValue = Game.IsKeyDown(Settings.ControlsKeys.ToggleNightVisionKey) || Game.IsControllerButtonDown(Settings.ControlsKeys.ToggleNightVisionButton);
                    break;
                case Controls.ToggleBinocularsHeliCamThermalVision:
                    controlValue = Game.IsKeyDown(Settings.ControlsKeys.ToggleThermalVisionKey) || Game.IsControllerButtonDown(Settings.ControlsKeys.ToggleThermalVisionButton);
                    break;

                case Controls.ToggleInteractionMenu:
                    controlValue = Game.IsKeyDown(Settings.ControlsKeys.OpenInteractionMenuKey);
                    break;

                case Controls.ToggleHeliCam:
                    controlValue = Game.IsKeyDown(Settings.ControlsKeys.ToggleHeliCameraKey);
                    break;
                case Controls.HeliCamScan:
                    controlValue = Game.IsKeyDown(Settings.ControlsKeys.HeliCameraScanKey) || Game.IsControllerButtonDown(Settings.ControlsKeys.HeliCameraScanButton);
                    break;

                case Controls.ToggleCalloutsMenu:
                    controlValue = Game.IsKeyDown(Settings.ControlsKeys.OpenCalloutsMenuKey);
                    break;
            }

            return modifierValue && controlValue;
        }

        public static bool IsPressed(this Controls control)
        {
            if (Functions.IsPoliceComputerActive())
                return false;

            bool modifierValue = false;
            bool controlValue = false;

            if (Settings.ControlsKeys.ModifierKey == System.Windows.Forms.Keys.None)
            {
                modifierValue = true;
            }
            else
            {
                modifierValue = Game.IsKeyDownRightNow(Settings.ControlsKeys.ModifierKey);
            }

            switch (control)
            {
                case Controls.PrimaryAction:
                    controlValue = Game.IsKeyDownRightNow(Settings.ControlsKeys.PrimaryActionKey);
                    break;
                case Controls.SecondaryAction:
                    controlValue = Game.IsKeyDownRightNow(Settings.ControlsKeys.SecondaryActionKey);
                    break;

                case Controls.ForceCalloutEnd:
                    controlValue = Game.IsKeyDownRightNow(Settings.ControlsKeys.ForceCalloutEndKey);
                    break;

                case Controls.ToggleBinoculars:
                    controlValue = Game.IsKeyDownRightNow(Settings.ControlsKeys.ToggleBinocularsKey);
                    break;

                case Controls.ToggleBinocularsHeliCamNightVision:
                    controlValue = Game.IsKeyDownRightNow(Settings.ControlsKeys.ToggleNightVisionKey) || Game.IsControllerButtonDownRightNow(Settings.ControlsKeys.ToggleNightVisionButton);
                    break;
                case Controls.ToggleBinocularsHeliCamThermalVision:
                    controlValue = Game.IsKeyDownRightNow(Settings.ControlsKeys.ToggleThermalVisionKey) || Game.IsControllerButtonDownRightNow(Settings.ControlsKeys.ToggleThermalVisionButton);
                    break;

                case Controls.ToggleInteractionMenu:
                    controlValue = Game.IsKeyDownRightNow(Settings.ControlsKeys.OpenInteractionMenuKey);
                    break;

                case Controls.ToggleHeliCam:
                    controlValue = Game.IsKeyDownRightNow(Settings.ControlsKeys.ToggleHeliCameraKey);
                    break;
                case Controls.HeliCamScan:
                    controlValue = Game.IsKeyDownRightNow(Settings.ControlsKeys.HeliCameraScanKey) || Game.IsControllerButtonDownRightNow(Settings.ControlsKeys.HeliCameraScanButton);
                    break;

                case Controls.ToggleCalloutsMenu:
                    controlValue = Game.IsKeyDownRightNow(Settings.ControlsKeys.OpenCalloutsMenuKey);
                    break;
            }

            return modifierValue && controlValue;
        }

        public static string ToUserFriendlyName(this Controls control)
        {
            string name = "";
            if (Settings.ControlsKeys.ModifierKey != System.Windows.Forms.Keys.None && !IsUsingController)
            {
                name = Settings.ControlsKeys.ModifierKey.ToString() + " + ";
            }

            switch (control)
            {
                case Controls.PrimaryAction:
                    name += Settings.ControlsKeys.PrimaryActionKey.ToString();
                    break;
                case Controls.SecondaryAction:
                    name += Settings.ControlsKeys.SecondaryActionKey.ToString();
                    break;
                case Controls.ForceCalloutEnd:
                    name += Settings.ControlsKeys.ForceCalloutEndKey.ToString();
                    break;
                case Controls.ToggleBinoculars:
                    name += Settings.ControlsKeys.ToggleBinocularsKey.ToString();
                    break;
                case Controls.ToggleBinocularsHeliCamNightVision:
                    name += IsUsingController ? Settings.ControlsKeys.ToggleNightVisionKey.ToString() : Settings.ControlsKeys.ToggleNightVisionButton.ToString();
                    break;
                case Controls.ToggleBinocularsHeliCamThermalVision:
                    name += IsUsingController ? Settings.ControlsKeys.ToggleThermalVisionKey.ToString() : Settings.ControlsKeys.ToggleThermalVisionButton.ToString();
                    break;
                case Controls.ToggleInteractionMenu:
                    name += Settings.ControlsKeys.OpenInteractionMenuKey.ToString();
                    break;
                case Controls.ToggleHeliCam:
                    name += Settings.ControlsKeys.ToggleHeliCameraKey.ToString();
                    break;
                case Controls.HeliCamScan:
                    name += IsUsingController ? Settings.ControlsKeys.HeliCameraScanKey.ToString() : Settings.ControlsKeys.HeliCameraScanButton.ToString();
                    break;
                case Controls.ToggleCalloutsMenu:
                    name += Settings.ControlsKeys.OpenCalloutsMenuKey.ToString();
                    break;
            }
            return name;
        }
    }
}
