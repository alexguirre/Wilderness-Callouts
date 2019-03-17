namespace WildernessCallouts
{
    using Rage;
    using System;
    using System.Windows.Forms;

    internal static class Settings
    {
        // TODO: clean up ini file

        public static InitializationFile INIFile = new InitializationFile(@"Plugins\LSPDFR\Wilderness Callouts Config.ini");

        public static class General
        {
            public const string SECTION_NAME = "General";

            public static readonly string Name = Settings.INIFile.ReadString(SECTION_NAME, "Your Name", "Name"); 

#if DEBUG
            public const bool IsDebugBuild = true;
#else
            public const bool IsDebugBuild = false;
#endif

            //public static bool IsVetEnable = Settings.INIFile.ReadBoolean("General", "Is Vet Enable", true);  
            public static bool IsBinocularEnable = Settings.INIFile.ReadBoolean("General", "Are Binoculars Enable", true);
            //public static bool IsAirAmbulanceEnable = Settings.INIFile.ReadBoolean("General", "Is Air Ambulance Enable", true);  
        }


        public static class Callouts
        {
            public const string SECTION_NAME = "Callouts";

            public static readonly bool IsIllegalHuntingEnable = Settings.INIFile.ReadBoolean(SECTION_NAME, "Enable Possible Illegal Hunting", true);
            public static readonly bool IsRocksBlockEnable = Settings.INIFile.ReadBoolean(SECTION_NAME, "Enable Rocks Blocking the Road", true);
            public static readonly bool IsAircraftCrashEnable = Settings.INIFile.ReadBoolean(SECTION_NAME, "Enable Aircraft Crash", true);
            public static readonly bool IsRecklessDriverEnable = Settings.INIFile.ReadBoolean(SECTION_NAME, "Enable Reckless Driver", true);
            public static readonly bool IsWantedFelonEnable = Settings.INIFile.ReadBoolean(SECTION_NAME, "Enable Wanted Felon In Vehicle", true);
            public static readonly bool IsSuicideAttemptEnable = Settings.INIFile.ReadBoolean(SECTION_NAME, "Enable Suicide Attempt", true);
            public static readonly bool IsMissingPersonEnable = Settings.INIFile.ReadBoolean(SECTION_NAME, "Enable Missing Person", true);
            public static readonly bool IsAnimalAttackEnable = Settings.INIFile.ReadBoolean(SECTION_NAME, "Enable Animal Attack", true);
            public static readonly bool IsPublicDisturbanceEnable = Settings.INIFile.ReadBoolean(SECTION_NAME, "Enable Public Disturbance", true);
            public static readonly bool IsHostageSituationEnable = Settings.INIFile.ReadBoolean(SECTION_NAME, "Enable Hostage Situation", true);
            public static readonly bool IsArsonEnable = Settings.INIFile.ReadBoolean(SECTION_NAME, "Enable Arson", true);
            public static readonly bool IsOfficerNeedsTransportEnable = Settings.INIFile.ReadBoolean(SECTION_NAME, "Enable Officer Needs Transport", true);
            public static readonly bool IsAttackedPoliceStationEnable = Settings.INIFile.ReadBoolean(SECTION_NAME, "Enable Attacked Police Station", true);
            public static readonly bool IsDemonstrationEnable = Settings.INIFile.ReadBoolean(SECTION_NAME, "Enable Demonstration", true);
            //public static readonly bool IsEscortEnable = Settings.INIFile.ReadBoolean(SECTION_NAME, "Enable Escort", true); 
            public static readonly bool IsMurderInvestigationEnable = Settings.INIFile.ReadBoolean(SECTION_NAME, "Enable Murder Insvestigation", true);
        }

        public static class AmbientEvents
        {
            public const string SECTION_NAME = "Ambient Events";

            public static readonly bool EnableAmbientEvents = Settings.INIFile.ReadBoolean(SECTION_NAME, "Enable Ambient Events", true);  
            public static readonly int MaxTimeAmbientEvent = Settings.INIFile.ReadInt32(SECTION_NAME, "Maximun Time Until Event", 360);  
            public static readonly int MinTimeAmbientEvent = Settings.INIFile.ReadInt32(SECTION_NAME, "Minimun Time Until Event", 50);  
            public static readonly bool ShowEventsBlips = Settings.INIFile.ReadBoolean(SECTION_NAME, "Show Blips", false);  
            //public static readonly bool IsHuntingAmbientEventEnable = Settings.INIFile.ReadBoolean(SECTION_NAME, "Enable Hunting Event", true);  
        }

        public static class ControlsKeys
        {
            public const string SECTION_NAME = "Keys";

            public static readonly Keys PrimaryActionKey = Settings.INIFile.ReadEnum<Keys>(SECTION_NAME, "Primary Action Key", Keys.Y);
            public static readonly Keys SecondaryActionKey = Settings.INIFile.ReadEnum<Keys>(SECTION_NAME, "Secondary Action Key", Keys.H);

            public static readonly Keys ForceCalloutEndKey = Settings.INIFile.ReadEnum<Keys>(SECTION_NAME, "End Callout Key", Keys.End);

            public static readonly Keys ModifierKey = Settings.INIFile.ReadEnum<Keys>(SECTION_NAME, "Modifier Key", Keys.LControlKey);

            public static readonly Keys ToggleBinocularsKey = Settings.INIFile.ReadEnum<Keys>(SECTION_NAME, "Toggle Binoculars", Keys.I);

            public static readonly Keys ToggleNightVisionKey = Settings.INIFile.ReadEnum<Keys>(SECTION_NAME, "Toggle Night Vision", Keys.NumPad9);
            public static readonly Keys ToggleThermalVisionKey = Settings.INIFile.ReadEnum<Keys>(SECTION_NAME, "Toggle Thermal Vision", Keys.NumPad3);
            public static readonly ControllerButtons ToggleNightVisionButton = Settings.INIFile.ReadEnum<ControllerButtons>(SECTION_NAME, "Toggle Night Vision Controller Button", ControllerButtons.B);
            public static readonly ControllerButtons ToggleThermalVisionButton = Settings.INIFile.ReadEnum<ControllerButtons>(SECTION_NAME, "Toggle Thermal Vision Controller Button", ControllerButtons.X);

            public static readonly Keys OpenInteractionMenuKey = Settings.INIFile.ReadEnum<Keys>(SECTION_NAME, "Open/Close Interaction Menu", Keys.O);

            public static readonly Keys ToggleHeliCameraKey = Settings.INIFile.ReadEnum<Keys>(SECTION_NAME, "Toggle Helicopter Camera", Keys.U);
            public static readonly Keys HeliCameraScanKey = Settings.INIFile.ReadEnum<Keys>(SECTION_NAME, "Helicopter Camera Scan", Keys.E);
            public static readonly ControllerButtons HeliCameraScanButton = Settings.INIFile.ReadEnum<ControllerButtons>(SECTION_NAME, "Helicopter Camera Scan Controller Button", ControllerButtons.Y);

            public static readonly Keys OpenCalloutsMenuKey = Settings.INIFile.ReadEnum<Keys>(SECTION_NAME, "Open/Close Callouts Menu", Keys.F8); // TODO: add OpenCalloutsMenuKey to ini
        }


        public static class Vet
        {
            public const string SECTION_NAME = "Vet";

            public static readonly string Name = Settings.INIFile.ReadString(SECTION_NAME, "Name", "Vet");
            public static readonly Model[] PedModels = Array.ConvertAll<string, Model>(Settings.INIFile.ReadString(SECTION_NAME, "Ped Models", "s_m_m_doctor_01,s_m_y_autopsy_01,s_m_m_paramedic_01,s_m_m_scientist_01,s_f_y_scrubs_01").Split(','), x => new Model(x));
            public static readonly Model VehModel = Settings.INIFile.ReadString(SECTION_NAME, "Vehicle Model", "dubsta3");
        }


        public static class AirAmbulance
        {
            public const string SECTION_NAME = "Air Ambulance";

            public static readonly string ParamedicName = Settings.INIFile.ReadString(SECTION_NAME, "Name", "Paramedic");


            public static readonly Model HeliModel = (INIFile.DoesKeyExist(SECTION_NAME, "Helicopter Model")) ?
                INIFile.ReadString(SECTION_NAME, "Helicopter Model", "polmav")
                : INIFile.ReadString(SECTION_NAME, "Helicoter Model", "polmav");


            public static readonly int HeliLiveryIndex = (INIFile.DoesKeyExist(SECTION_NAME, "Helicopter Livery Index")) ?
                INIFile.ReadInt32(SECTION_NAME, "Helicopter Livery Index", 0) :
                INIFile.ReadInt32(SECTION_NAME, "Helicoter Livery Index", 0);

            public static readonly Model[] PilotModels = Array.ConvertAll<string, Model>(Settings.INIFile.ReadString(SECTION_NAME, "Pilot Models", "s_m_m_pilot_02").Split(','), x => new Model(x));
            public static readonly Model[] ParamedicModels = Array.ConvertAll<string, Model>(Settings.INIFile.ReadString(SECTION_NAME, "Paramedic Models", "s_m_m_paramedic_01").Split(','), x => new Model(x));
        }

        public static bool CheckIniFile()
        {
            if (!Settings.INIFile.Exists() ||
                !Settings.INIFile.DoesSectionExist(Settings.AirAmbulance.SECTION_NAME) || 
                !Settings.INIFile.DoesSectionExist(Settings.AmbientEvents.SECTION_NAME) || 
                !Settings.INIFile.DoesSectionExist(Settings.Callouts.SECTION_NAME) ||
                !Settings.INIFile.DoesSectionExist(Settings.ControlsKeys.SECTION_NAME) ||
                !Settings.INIFile.DoesSectionExist(Settings.General.SECTION_NAME) ||
                !Settings.INIFile.DoesSectionExist(Settings.Vet.SECTION_NAME))
            {
                return false;
            }
            return true;
        }
    }
}
