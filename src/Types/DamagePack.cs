namespace WildernessCallouts.Types
{
    using Rage;
    using Rage.Native;

    internal static class DamagePack
    {
        public static void ApplyDamagePack(this Ped ped, string damagePackName, float damage, float multiplier)
        {
            NativeFunction.Natives.APPLY_PED_DAMAGE_PACK(ped, damagePackName, damage, multiplier);
        }

        public const string BigHitByVehicle = "BigHitByVehicle";
        public const string SCR_Dumpster = "SCR_Dumpster";
        public const string SCR_Torture = "SCR_Torture";
        public const string SCR_TrevorTreeBang = "SCR_TrevorTreeBang";
        public const string HOSPITAL_0 = "HOSPITAL_0";
        public const string HOSPITAL_1 = "HOSPITAL_1";
        public const string HOSPITAL_2 = "HOSPITAL_2";
        public const string HOSPITAL_3 = "HOSPITAL_3";
        public const string HOSPITAL_4 = "HOSPITAL_4";
        public const string HOSPITAL_5 = "HOSPITAL_5";
        public const string HOSPITAL_6 = "HOSPITAL_6";
        public const string HOSPITAL_7 = "HOSPITAL_7";
        public const string HOSPITAL_8 = "HOSPITAL_8";
        public const string HOSPITAL_9 = "HOSPITAL_9";
        public const string SCR_Finale_Michael_Face = "SCR_Finale_Michael_Face";
        public const string SCR_Franklin_finb = "SCR_Franklin_finb";
        public const string SCR_Finale_Michael = "SCR_Finale_Michael";
        public const string SCR_Franklin_finb2 = "SCR_Franklin_finb2";
        public const string Explosion_Med = "Explosion_Med";
        public const string SCR_TracySplash = "SCR_TracySplash";
        public const string Skin_Melee_0 = "Skin_Melee_0";

        //public const string Useful_Bits = "Useful_Bits";
        //public const string Explosion_Med = "Explosion_Med";
        //public const string Explosion_Large = "Explosion_Large";
        //public const string Dirt_Dry = "Dirt_Dry";
        //public const string Dirt_Grass = "Dirt_Grass";
        //public const string Dirt_Mud = "Dirt_Mud";
        //public const string Burnt_Ped_Left_Arm = "Burnt_Ped_Left_Arm";
        //public const string Burnt_Ped_Right_Arm = "Burnt_Ped_Right_Arm";
        //public const string Burnt_Ped_Limbs = "Burnt_Ped_Limbs";
        //public const string Burnt_Ped_Head_Torso = "Burnt_Ped_Head_Torso";
        //public const string Burnt_Ped_0 = "Burnt_Ped_0";
        //public const string Car_Crash_Light = "Car_Crash_Light";
        //public const string Car_Crash_Heavy = "Car_Crash_Heavy";
        //public const string Fall_Low = "Fall_Low";
        //public const string Fall = "Fall";
        //public const string HitByVehicle = "HitByVehicle";
        //public const string BigHitByVehicle = "BigHitByVehicle";
        //public const string BigRunOverByVehicle = "BigRunOverByVehicle";
        //public const string RunOverByVehicle = "RunOverByVehicle";
        //public const string Splashback_Face_0 = "Splashback_Face_0";
        //public const string Splashback_Face_1 = "Splashback_Face_1";
        //public const string Splashback_Torso_0 = "Splashback_Torso_0";
        //public const string Splashback_Torso_1 = "Splashback_Torso_1";
        //public const string HOSPITAL_0 = "HOSPITAL_0";
        //public const string HOSPITAL_1 = "HOSPITAL_1";
        //public const string HOSPITAL_2 = "HOSPITAL_2";
        //public const string HOSPITAL_3 = "HOSPITAL_3";
        //public const string HOSPITAL_4 = "HOSPITAL_4";
        //public const string HOSPITAL_5 = "HOSPITAL_5";
        //public const string HOSPITAL_6 = "HOSPITAL_6";
        //public const string HOSPITAL_7 = "HOSPITAL_7";
        //public const string HOSPITAL_8 = "HOSPITAL_8";
        //public const string HOSPITAL_9 = "HOSPITAL_9";
        //public const string Skin_Melee_0 = "Skin_Melee_0";
        //public const string SCR_Dumpster = "SCR_Dumpster";
        //public const string SCR_Cougar = "SCR_Cougar";
        //public const string SCR_DogAttack = "SCR_DogAttack";
        //public const string SCR_TracySplash = "SCR_TracySplash";
        //public const string SCR_Finale_Michael_Face = "SCR_Finale_Michael_Face";
        //public const string SCR_Finale_Michael = "SCR_Finale_Michael";
        //public const string SCR_Shark = "SCR_Shark";
        //public const string SCR_Torture = "SCR_Torture";
        //public const string TD_KNIFE_FRONT = "TD_KNIFE_FRONT";
        //public const string TD_KNIFE_FRONT_VA = "TD_KNIFE_FRONT_VA";
        //public const string TD_KNIFE_FRONT_VB = "TD_KNIFE_FRONT_VB";
        //public const string TD_KNIFE_REAR = "TD_KNIFE_REAR";
        //public const string TD_KNIFE_REAR_VA = "TD_KNIFE_REAR_VA";
        //public const string TD_KNIFE_REAR_VB = "TD_KNIFE_REAR_VB";
        //public const string TD_KNIFE_STEALTH = "TD_KNIFE_STEALTH";
        //public const string TD_MELEE_FRONT = "TD_MELEE_FRONT";
        //public const string TD_MELEE_REAR = "TD_MELEE_REAR";
        //public const string TD_MELEE_STEALTH = "TD_MELEE_STEALTH";
        //public const string TD_MELEE_BATWAIST = "TD_MELEE_BATWAIST";
        //public const string TD_melee_face_l = "TD_melee_face_l";
        //public const string MTD_melee_face_r = "MTD_melee_face_r";
        //public const string MTD_melee_face_jaw = "MTD_melee_face_jaw";
        //public const string TD_PISTOL_FRONT = "TD_PISTOL_FRONT";
        //public const string TD_PISTOL_FRONT_KILL = "TD_PISTOL_FRONT_KILL";
        //public const string TD_PISTOL_REAR = "TD_PISTOL_REAR";
        //public const string TD_PISTOL_REAR_KILL = "TD_PISTOL_REAR_KILL";
        //public const string TD_RIFLE_FRONT_KILL = "TD_RIFLE_FRONT_KILL";
        //public const string TD_RIFLE_NONLETHAL_FRONT = "TD_RIFLE_NONLETHAL_FRONT";
        //public const string TD_RIFLE_NONLETHAL_REAR = "TD_RIFLE_NONLETHAL_REAR";
        //public const string TD_SHOTGUN_FRONT_KILL = "TD_SHOTGUN_FRONT_KILL";
        //public const string TD_SHOTGUN_REAR_KILL = "TD_SHOTGUN_REAR_KILL";
        //public const string None = "None";
        //public const string SCR_Franklin_finb = "SCR_Franklin_finb";
        //public const string SCR_Franklin_finb2 = "SCR_Franklin_finb2";
    }
}
