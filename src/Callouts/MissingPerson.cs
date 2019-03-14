namespace WildernessCallouts.Callouts
{
    // System
    using System;
    using System.Drawing;
    using System.Collections.Generic;

    // RPH
    using Rage;
    using Rage.Native;

    // LSPDFR
    using LSPD_First_Response.Mod.API;
    using LSPD_First_Response.Mod.Callouts;

    // WildernessCallouts
    using WildernessCallouts.Types;
    using WildernessCallouts.CalloutFunct;
    using WildernessCallouts.Dialogues;

    [CalloutInfo("MissingPerson", CalloutProbability.Medium)]
    internal class MissingPerson : CalloutBase
    {
        public static Ped CurrentMissingPed;

        private Ped missingPed;
        private Blip searchAreaBlip;
        private Blip pedBlip;
        private Vector3 spawnPoint;

        private EMissingPersonState state;

        private EMissingPersonScenario scenario;

        private SpawnPoint spPoint;

        private AirParamedic airParamedic;

        private Blip debugBlip;

        private const float searchAreaRadius = 210.0f;//225.0f

        private int waitTimeBetweenDialogue = 50;

        private bool isUsingGPS = false;

        private Tuple<AnimationDictionary, string> writheAnimUsed;
        private Tuple<AnimationDictionary, string> cowerAnimUsed;

        public override bool OnBeforeCalloutDisplayed()
        {
            EWorldArea spawnZone = WorldZone.GetArea(Game.LocalPlayer.Character.Position);
            if (spawnZone == EWorldArea.Los_Santos) return false;

            spPoint = Spawns.GetRandomElement();
            for (int i = 0; i < 15; i++)
            {
                if (Vector3.Distance(Game.LocalPlayer.Character.Position, spPoint.Position) < 1250.0f) break;
                spPoint = Spawns.GetRandomElement();
            }
            if (Vector3.Distance(Game.LocalPlayer.Character.Position, spPoint.Position) > 1250.0f) return false;

            spawnPoint = spPoint.Position;

            missingPed = new Ped(spawnPoint);
            if (missingPed.Exists()) missingPed.Heading = Globals.Random.Next(0, 360);

            GameFiber.Wait(250);

            if (!missingPed.Exists()) return false;

            writheAnimUsed = WritheAnims.GetRandomElement();
            cowerAnimUsed = CowerAnims.GetRandomElement();

            this.ShowCalloutAreaBlipBeforeAccepting(spawnPoint, 50.0f);
            this.AddMinimumDistanceCheck(25.0f, missingPed.Position);

            this.CalloutMessage = "Missing person";
            this.CalloutPosition = spawnPoint;

            int rndAudio = Globals.Random.Next(1, 3);
            if (rndAudio == 1) LSPD_First_Response.Mod.API.Functions.PlayScannerAudioUsingPosition("WE_HAVE CRIME_CIVILIAN_REQUIRING_ASSISTANCE IN_OR_ON_POSITION UNITS_RESPOND_CODE_02", spawnPoint);
            else if (rndAudio == 2) LSPD_First_Response.Mod.API.Functions.PlayScannerAudioUsingPosition("WE_HAVE SOS_CALL IN_OR_ON_POSITION UNITS_RESPOND_CODE_02", spawnPoint);

            return base.OnBeforeCalloutDisplayed();
        }

        public override bool OnCalloutAccepted()
        {
            CurrentMissingPed = missingPed;

            Game.DisplayHelp("Press " + Controls.ForceCalloutEnd.ToUserFriendlyName() + " to finish the callout");

            int pedActionRnd = Globals.Random.Next(0, 7);
            if (pedActionRnd <= 1)
            {
                missingPed.BlockPermanentEvents = true;
                NativeFunction.Natives.TASK_WRITHE(missingPed, Game.LocalPlayer.Character, -1, false);
                scenario = EMissingPersonScenario.Dead;
                if (Globals.Random.Next(5) <= 3) SetPedBlood(missingPed);
            }
            else if (pedActionRnd == 2)
            {
                if (Globals.Random.Next(0, 3) == 2) missingPed.SetMovementAnimationSet("move_m@drunk@moderatedrunk_head_up");
                else missingPed.SetMovementAnimationSet("move_m@drunk@verydrunk");
                scenario = EMissingPersonScenario.Drunk;
            }
            else if (pedActionRnd == 3)
            {
                missingPed.BlockPermanentEvents = true;
                missingPed.Tasks.PlayAnimation(cowerAnimUsed.Item1, cowerAnimUsed.Item2, 2.0f, AnimationFlags.Loop);
                scenario = EMissingPersonScenario.Scared;
            }
            else if (pedActionRnd == 4)
            {
                missingPed.BlockPermanentEvents = true;
                missingPed.Tasks.PlayAnimation(writheAnimUsed.Item1, writheAnimUsed.Item2, 2.0f, AnimationFlags.Loop);
                scenario = EMissingPersonScenario.Injured;
            }
            else if (pedActionRnd == 5)
            {
                missingPed.BlockPermanentEvents = true;
                missingPed.Tasks.PlayAnimation(writheAnimUsed.Item1, writheAnimUsed.Item2, 2.0f, AnimationFlags.Loop);
                scenario = EMissingPersonScenario.VeryInjured;
                SetPedBlood(missingPed);
            }
            else scenario = EMissingPersonScenario.Ok;

            Logger.LogTrivial(this.GetType().Name, "Scenario: " + scenario);

            searchAreaBlip = new Blip(spawnPoint.AroundPosition(searchAreaRadius - 17.5f), searchAreaRadius);
            searchAreaBlip.Color = Color.FromArgb(100, Color.ForestGreen);
            searchAreaBlip.EnableRoute(Color.ForestGreen);

            if (Settings.General.IsDebugBuild) debugBlip = new Blip(missingPed);
            
            Vector3 position = missingPed.Position;

            if (Globals.Random.Next(5) <= 3)
            {
                Game.DisplayNotification("~b~Dispatch: ~w~Missing person last seen around ~b~" + position.GetZoneName() + "~w~, ~b~" + position.GetAreaName());
                Game.DisplayNotification("~b~Dispatch: ~w~His mobile phone last known GPS position has been sent to your tracker");
                GameFiber.Wait(750);
                Game.DisplayNotification("~b~" + Settings.General.Name + ": ~w~Roger");
                isUsingGPS = true;
            }
            else
            {
                Game.DisplayNotification("~b~Dispatch: ~w~Missing person last seen around ~b~" + position.GetZoneName() + "~w~, ~b~" + position.GetAreaName());
                Game.DisplayNotification("~b~Dispatch: ~w~No GPS signal detected");
                GameFiber.Wait(750);
                Game.DisplayNotification("~b~" + Settings.General.Name + ": ~w~Roger");
                isUsingGPS = false;
            }

            state = EMissingPersonState.EnRoute;

            return base.OnCalloutAccepted();
        }

        public override void OnCalloutNotAccepted()
        {
            CurrentMissingPed = null;

            if (missingPed.Exists()) missingPed.Delete();
            if (searchAreaBlip.Exists()) searchAreaBlip.Delete();
            if (pedBlip.Exists()) pedBlip.Delete();
            if (airParamedic != null)
                airParamedic.CleanUp();
            if (Settings.General.IsDebugBuild) if (debugBlip.Exists()) debugBlip.Delete();

            base.OnCalloutNotAccepted();
        }

        public override void Process()
        {
            if (!missingPed.Exists())
                this.End();

            if (missingPed.Exists() && missingPed.IsAlive && Functions.IsPedArrested(missingPed))
                this.End();

            if (state == EMissingPersonState.EnRoute && Vector3.Distance(Game.LocalPlayer.Character.Position, searchAreaBlip.Position) < searchAreaRadius - 5.0f)
            {
                GameFiber.StartNew(delegate
                {
                    Game.DisplayNotification("~b~" + Settings.General.Name + ": ~w~Dispatch, I'm in the area, ~g~code 6");
                    GameFiber.Wait(1250);
                    Game.DisplayNotification("~b~Dispatch: ~w~Roger");
                    state = EMissingPersonState.Searching;
                });
            }

            if (state == EMissingPersonState.Searching)
            {
                if (Vector3.Distance(Game.LocalPlayer.Character.Position, missingPed.Position) < 7.5f) Found();
                else if (isUsingGPS) Game.DisplaySubtitle("Signal Strength: " + Tracker(missingPed.Position, searchAreaRadius + 25.0f), 100);
            }

            if (state == EMissingPersonState.Found)
            {
                if (Controls.PrimaryAction.IsJustPressed())
                {
                    GameFiber.StartNew(delegate
                    {
                        state = EMissingPersonState.AirParamedicCalled;

                        airParamedic = new AirParamedic(missingPed, "You can leave, we will take care of him, thanks", "You can leave, we will take care of her, thanks");
                        airParamedic.Start();
                        
                        this.End();
                    });
                }
            }

            base.Process();
        }

        public override void End()
        {
            CurrentMissingPed = null;

            if (missingPed.Exists()) missingPed.Dismiss();
            if (searchAreaBlip.Exists()) searchAreaBlip.Delete();
            if (pedBlip.Exists()) pedBlip.Delete();
            if (airParamedic != null) airParamedic.CleanUp();
            if (Settings.General.IsDebugBuild) if (debugBlip.Exists()) debugBlip.Delete();

            base.End();
        }

        protected override void CleanUp()
        {
            // TODO: implement CleanUp()
        }

        public void Found()
        {
            Logger.LogTrivial(this.GetType().Name, "Found()");

            state = EMissingPersonState.Found;
            GameFiber.StartNew(delegate
            {
                pedBlip = new Blip(missingPed);
                pedBlip.Color = Color.ForestGreen;
                if (searchAreaBlip.Exists()) searchAreaBlip.Delete();

                Game.DisplayNotification("~b~" + Settings.General.Name + ": ~w~Dispatch, missing person found, making contact");
                GameFiber.Wait(1250);
                Game.DisplayNotification("~b~Dispatch: ~w~Roger, proceed");
            });

            Game.DisplayHelp("Press ~b~" + Controls.PrimaryAction.ToUserFriendlyName() + "~w~ to call an air ambulance, if you think it is needed", 6500);

            if (scenario != EMissingPersonScenario.Dead && missingPed.IsAlive)
            {
                GameFiber.StartNew(delegate
                {
                    string[] dialogue = null;

                    if (scenario == EMissingPersonScenario.Ok)
                    {
                        missingPed.Tasks.AchieveHeading(missingPed.GetHeadingTowards(Game.LocalPlayer.Character));
                        dialogue = MissingPersonDialogues.PersonIsOk[Globals.Random.Next(MissingPersonDialogues.PersonIsOk.Length)];
                        missingPed.PlayAmbientSpeech(Globals.Random.Next(2) == 1 ? Speech.GENERIC_HOWS_IT_GOING : Speech.GENERIC_HI);
                    }
                    else if (scenario == EMissingPersonScenario.Injured)
                    {
                        dialogue = MissingPersonDialogues.PersonIsInjured[Globals.Random.Next(MissingPersonDialogues.PersonIsInjured.Length)];
                        missingPed.PlayAmbientSpeech(Globals.Random.Next(2) == 1 ? Speech.DYING_HELP : Speech.DYING_MOAN);
                    }
                    else if (scenario == EMissingPersonScenario.VeryInjured)
                    {
                        dialogue = MissingPersonDialogues.PersonIsVeryInjured[Globals.Random.Next(MissingPersonDialogues.PersonIsVeryInjured.Length)];
                        missingPed.PlayAmbientSpeech(Globals.Random.Next(2) == 1 ? Speech.DYING_HELP : Speech.DYING_MOAN);
                    }
                    else if (scenario == EMissingPersonScenario.Drunk)
                    {
                        dialogue = MissingPersonDialogues.PersonIsDrunk[Globals.Random.Next(MissingPersonDialogues.PersonIsDrunk.Length)];
                        missingPed.PlayAmbientSpeech(Globals.Random.Next(2) == 1 ? Globals.Random.Next(2) == 1 ? Speech.GENERIC_FUCK_YOU : Speech.GENERIC_INSULT_HIGH : Speech.GENERIC_INSULT_MED);
                    }
                    else if (scenario == EMissingPersonScenario.Scared)
                    {
                        dialogue = MissingPersonDialogues.PersonIsScared[Globals.Random.Next(MissingPersonDialogues.PersonIsScared.Length)];
                        missingPed.PlayAmbientSpeech(Globals.Random.Next(2) == 1 ? Speech.GENERIC_FRIGHTENED_HIGH : Speech.GENERIC_FRIGHTENED_MED);
                    }
                    foreach (string str in dialogue)
                    {
                        GameFiber.Sleep(waitTimeBetweenDialogue);
                        Game.DisplaySubtitle(str, 4500);
                        waitTimeBetweenDialogue = 4375;
                    }
                });
            }
        }

        private static void SetPedBlood(Ped ped)
        {
            string[] allDmgPacksNames = { DamagePack.BigHitByVehicle, DamagePack.Explosion_Med, DamagePack.SCR_Finale_Michael, DamagePack.SCR_Finale_Michael_Face, DamagePack.SCR_Franklin_finb, DamagePack.SCR_Franklin_finb2, DamagePack.SCR_Torture, DamagePack.Skin_Melee_0, DamagePack.SCR_TrevorTreeBang, DamagePack.SCR_TracySplash };
            List<string> selectedDmgPacks = new List<string>();
            for (int i = 0; i < Globals.Random.Next(1, 5); i++)
            {
                string dmgPack = allDmgPacksNames.GetRandomElement(i == 0);
                if (!selectedDmgPacks.Contains(dmgPack)) selectedDmgPacks.Add(dmgPack);
            }

            foreach (string dmgPack in allDmgPacksNames)
                ped.ApplyDamagePack(dmgPack, MathHelper.GetRandomSingle(20.0f, 100.0f), MathHelper.GetRandomSingle(20.0f, 100.0f));
        }


        private static string Tracker(Vector3 position, float maximunDistance)
        {
            float distanceTo = Vector3.Distance(position, Game.LocalPlayer.Character.Position);
            int percentage = -Convert.ToInt32(((distanceTo - maximunDistance) * 100) / maximunDistance);
            if (percentage < 0) return "~r~Too Low";
            else return percentage.ToString() + "%";
        }

        public enum EMissingPersonState
        {
            EnRoute,
            OnScene,
            Searching,
            Found,
            AirParamedicCalled
        }

        public enum EMissingPersonScenario
        {
            Dead,
            Drunk,
            Scared,
            Injured,
            VeryInjured,
            Ok,
        }

        public static List<Tuple<AnimationDictionary, string>> WritheAnims = new List<Tuple<AnimationDictionary, string>>()
        {
            new Tuple<AnimationDictionary, string>("combat@damage@rb_writhe", "rb_writhe_loop"),
            new Tuple<AnimationDictionary, string>("combat@damage@writhe", "writhe_loop"),
            new Tuple<AnimationDictionary, string>("combat@damage@writheidle_a", "writhe_idle_a"),
            new Tuple<AnimationDictionary, string>("combat@damage@writheidle_a", "writhe_idle_b"),
            new Tuple<AnimationDictionary, string>("combat@damage@writheidle_a", "writhe_idle_c"),
            new Tuple<AnimationDictionary, string>("combat@damage@writheidle_b", "writhe_idle_d"),
            new Tuple<AnimationDictionary, string>("combat@damage@writheidle_b", "writhe_idle_e"),
            new Tuple<AnimationDictionary, string>("combat@damage@writheidle_b", "writhe_idle_f"),
            new Tuple<AnimationDictionary, string>("combat@damage@writheidle_c", "writhe_idle_g"),
        };
        public static List<Tuple<AnimationDictionary, string>> CowerAnims = new List<Tuple<AnimationDictionary, string>>()
        {
            new Tuple<AnimationDictionary, string>("amb@code_human_cower@male@idle_a", "idle_a"),
            new Tuple<AnimationDictionary, string>("amb@code_human_cower@male@idle_a", "idle_b"),
            new Tuple<AnimationDictionary, string>("amb@code_human_cower@male@idle_a", "idle_c"),
            new Tuple<AnimationDictionary, string>("amb@code_human_cower@male@idle_b", "idle_d"),
        };

        public static List<SpawnPoint> Spawns = new List<SpawnPoint>()
        {
#region Spawns
new SpawnPoint(new Vector3(-1568.15149f, 1805.81592f, 132.723389f),40.9344139f),
new SpawnPoint(new Vector3(-1589.56873f, 1737.448f, 133.924316f),93.1964f),
new SpawnPoint(new Vector3(-1606.82434f, 1735.76172f, 139.2008f),105.13916f),
new SpawnPoint(new Vector3(-1635.37109f, 1735.78381f, 143.6918f),85.2566147f),
new SpawnPoint(new Vector3(-1656.05579f, 1735.37158f, 153.3582f),73.11108f),
new SpawnPoint(new Vector3(-1662.23145f, 1743.198f, 159.328537f),21.0681286f),
new SpawnPoint(new Vector3(-1665.022f, 1756.2428f, 164.875534f),8.381955f),
new SpawnPoint(new Vector3(-1649.01624f, 1784.0116f, 169.612976f),306.025452f),
new SpawnPoint(new Vector3(-1614.386f, 1819.4292f, 160.211517f),327.808929f),
new SpawnPoint(new Vector3(-1578.56519f, 1846.03113f, 143.014511f),274.568f),
new SpawnPoint(new Vector3(-1552.74133f, 1851.57922f, 129.504364f),275.3905f),
new SpawnPoint(new Vector3(-1527.94763f, 1854.60693f, 113.546906f),277.6605f),
new SpawnPoint(new Vector3(-1528.47266f, 1878.63159f, 112.186676f),9.558469f),
new SpawnPoint(new Vector3(-1550.49146f, 1916.74451f, 112.327782f),28.8665981f),
new SpawnPoint(new Vector3(-1588.59656f, 1938.13977f, 100.990952f),70.4323654f),
new SpawnPoint(new Vector3(-1619.52014f, 1930.17236f, 108.701607f),108.512146f),
new SpawnPoint(new Vector3(-1652.391f, 1902.089f, 121.8999f),153.70314f),
new SpawnPoint(new Vector3(-1667.99634f, 1853.67859f, 134.538986f),133.7799f),
new SpawnPoint(new Vector3(-1702.74353f, 1862.06653f, 161.808731f),47.41602f),
new SpawnPoint(new Vector3(-1669.69666f, 1916.18591f, 134.157181f),317.862823f),
new SpawnPoint(new Vector3(-1611.3064f, 1951.42761f, 114.24971f),271.072723f),
new SpawnPoint(new Vector3(-1592.17688f, 1955.88928f, 95.00484f),233.348068f),
new SpawnPoint(new Vector3(-1600.30969f, 1939.55066f, 99.82192f),134.313248f),
new SpawnPoint(new Vector3(-1610.10278f, 1930.84924f, 104.847321f),152.338623f),
new SpawnPoint(new Vector3(-1603.052f, 1926.37732f, 106.671806f),277.841919f),
new SpawnPoint(new Vector3(-1590.89673f, 1933.5116f, 103.203766f),312.44458f),
new SpawnPoint(new Vector3(-1579.54065f, 1940.99915f, 103.140236f),307.2021f),
new SpawnPoint(new Vector3(-1571.54431f, 1962.49939f, 95.92256f),0.324401528f),
new SpawnPoint(new Vector3(-1572.55737f, 1989.46973f, 85.05661f),339.260681f),
new SpawnPoint(new Vector3(-1576.30054f, 2010.81848f, 77.42047f),37.7919922f),
new SpawnPoint(new Vector3(-1567.47681f, 2014.5813f, 76.14383f),231.551422f),
new SpawnPoint(new Vector3(-1550.99158f, 2010.90771f, 78.14143f),274.783264f),
new SpawnPoint(new Vector3(-1538.22363f, 2026.54932f, 73.495224f),324.654633f),
new SpawnPoint(new Vector3(-1540.9635f, 2034.256f, 74.53374f),22.2963867f),
new SpawnPoint(new Vector3(-1547.30042f, 2050.02246f, 61.4757156f),7.607499f),
new SpawnPoint(new Vector3(-1526.61926f, 2059.82031f, 57.3861656f),248.183472f),
new SpawnPoint(new Vector3(-1518.187f, 2063.42871f, 58.0608864f),354.340851f),
new SpawnPoint(new Vector3(-1521.41882f, 2074.3728f, 51.2339745f),359.3594f),
new SpawnPoint(new Vector3(-1509.03271f, 2084.45532f, 51.26393f),307.63858f),
new SpawnPoint(new Vector3(-1493.19629f, 2088.26733f, 53.9305267f),291.8213f),
new SpawnPoint(new Vector3(-1486.72046f, 2106.61377f, 48.9275932f),351.162079f),
new SpawnPoint(new Vector3(-1472.49316f, 2123.98267f, 47.610775f),238.855255f),
new SpawnPoint(new Vector3(-1456.20459f, 2115.21558f, 43.58847f),183.315f),
new SpawnPoint(new Vector3(-1421.67273f, 2138.89233f, 31.02729f),250.939789f),
new SpawnPoint(new Vector3(-1392.74487f, 2134.63379f, 49.2282f),2.38495612f),
new SpawnPoint(new Vector3(-1405.77917f, 2126.16553f, 47.3835449f),69.044426f),
new SpawnPoint(new Vector3(-1418.59253f, 2125.49365f, 45.5601f),93.5508f),
new SpawnPoint(new Vector3(-1432.46533f, 2113.718f, 43.78973f),150.9223f),
new SpawnPoint(new Vector3(-1433.05688f, 2102.957f, 46.10661f),174.41333f),
new SpawnPoint(new Vector3(-1372.207f, 2125.28076f, 57.34819f),263.907166f),
new SpawnPoint(new Vector3(-1355.48145f, 2121.63818f, 66.45073f),240.488037f),
new SpawnPoint(new Vector3(-1330.14539f, 2115.93384f, 71.29884f),279.076874f),
new SpawnPoint(new Vector3(-1314.82678f, 2115.853f, 78.34764f),210.0889f),
new SpawnPoint(new Vector3(-1302.14136f, 2106.72754f, 87.25592f),256.061951f),
new SpawnPoint(new Vector3(-1277.74951f, 2115.093f, 80.5776138f),313.305267f),
new SpawnPoint(new Vector3(-1269.14282f, 2138.32642f, 72.84579f),17.986496f),
new SpawnPoint(new Vector3(-1256.67114f, 2138.88037f, 74.09411f),265.251556f),
new SpawnPoint(new Vector3(-1233.29858f, 2132.22339f, 82.74641f),349.536682f),
new SpawnPoint(new Vector3(-1242.02185f, 2106.253f, 90.71203f),152.523117f),
new SpawnPoint(new Vector3(-1250.51062f, 2102.796f, 97.3618851f),42.7013054f),
new SpawnPoint(new Vector3(-1227.03748f, 2085.48779f, 108.030479f),312.355743f),
new SpawnPoint(new Vector3(-1211.366f, 2074.61133f, 112.554688f),229.918289f),
new SpawnPoint(new Vector3(-1207.15723f, 2058.056f, 117.475891f),197.832886f),
new SpawnPoint(new Vector3(-1184.10071f, 2063.46362f, 120.281219f),291.965454f),
new SpawnPoint(new Vector3(-1156.11572f, 2083.04175f, 128.788818f),336.085876f),
new SpawnPoint(new Vector3(-1152.424f, 2117.81445f, 126.413574f),6.10234451f),
new SpawnPoint(new Vector3(-1142.57788f, 2117.20679f, 116.792389f),326.976868f),
new SpawnPoint(new Vector3(-1129.98792f, 2144.05127f, 100.665474f),288.672821f),
new SpawnPoint(new Vector3(-1109.08557f, 2135.61914f, 88.39546f),246.589813f),
new SpawnPoint(new Vector3(-1090.41113f, 2135.06519f, 79.78167f),331.3106f),
new SpawnPoint(new Vector3(-1085.49829f, 2149.69214f, 78.45565f),14.368598f),
new SpawnPoint(new Vector3(-1095.1f, 2172.529f, 77.92876f),356.788849f),
new SpawnPoint(new Vector3(-1092.69263f, 2202.719f, 72.9881439f),20.27713f),
new SpawnPoint(new Vector3(-1080.098f, 2234.84033f, 85.1995f),359.610962f),
new SpawnPoint(new Vector3(-1100.35815f, 2247.6416f, 76.6421661f),76.64629f),
new SpawnPoint(new Vector3(-1158.4353f, 2242.27563f, 68.44573f),105.544456f),
new SpawnPoint(new Vector3(-1180.1001f, 2251.82666f, 73.29781f),43.2638f),
new SpawnPoint(new Vector3(-1195.543f, 2260.56543f, 78.7295151f),63.3812828f),
new SpawnPoint(new Vector3(-1199.27881f, 2269.161f, 74.3953f),1.24271584f),
new SpawnPoint(new Vector3(-1211.50256f, 2270.41553f, 70.05684f),126.950851f),
new SpawnPoint(new Vector3(-1212.18225f, 2253.54541f, 67.0860443f),197.075455f),
new SpawnPoint(new Vector3(-1206.99072f, 2233.55957f, 60.29406f),192.651062f),
new SpawnPoint(new Vector3(-1205.76392f, 2221.162f, 64.2517f),177.4063f),
new SpawnPoint(new Vector3(-1229.88672f, 2217.51733f, 60.134716f),92.7676849f),
new SpawnPoint(new Vector3(-1250.41064f, 2221.59863f, 54.86316f),60.328434f),
new SpawnPoint(new Vector3(-1271.806f, 2236.93115f, 56.6580467f),53.51797f),
new SpawnPoint(new Vector3(-1298.45447f, 2244.94019f, 55.8361626f),111.098053f),
new SpawnPoint(new Vector3(-1317.96619f, 2245.00269f, 54.0103836f),126.136932f),
new SpawnPoint(new Vector3(-1324.73193f, 2230.09375f, 52.8806839f),131.537247f),
new SpawnPoint(new Vector3(-1334.70386f, 2236.91821f, 50.3567657f),28.516571f),
new SpawnPoint(new Vector3(-1317.48169f, 2268.38965f, 56.2370567f),340.7369f),
new SpawnPoint(new Vector3(-1304.53223f, 2295.523f, 61.8028f),332.6044f),
new SpawnPoint(new Vector3(-1262.81738f, 2329.789f, 77.6061859f),271.7388f),
new SpawnPoint(new Vector3(-1249.99487f, 2348.005f, 76.12917f),342.097534f),
new SpawnPoint(new Vector3(-1224.494f, 2376.12866f, 76.81364f),300.8654f),
new SpawnPoint(new Vector3(-1199.70251f, 2414.51367f, 76.31603f),320.127838f),
new SpawnPoint(new Vector3(-1178.6842f, 2432.59863f, 78.53267f),183.121674f),
new SpawnPoint(new Vector3(-1177.47961f, 2429.02734f, 82.73397f),203.515137f),
new SpawnPoint(new Vector3(-1170.91724f, 2419.7478f, 85.40418f),211.3017f),
new SpawnPoint(new Vector3(-1168.67371f, 2414.579f, 88.3017349f),209.600372f),
new SpawnPoint(new Vector3(-1166.54272f, 2404.04126f, 93.19092f),235.295959f),
new SpawnPoint(new Vector3(-1148.95776f, 2385.04785f, 94.55837f),220.535324f),
new SpawnPoint(new Vector3(-1132.89172f, 2388.9458f, 84.45085f),304.537567f),
new SpawnPoint(new Vector3(-1106.576f, 2392.55322f, 77.71368f),276.902435f),
new SpawnPoint(new Vector3(-1053.067f, 2396.37329f, 66.3798141f),263.996857f),
new SpawnPoint(new Vector3(-1032.55652f, 2399.675f, 64.2918f),218.419891f),
new SpawnPoint(new Vector3(-1020.33258f, 2400.52344f, 67.5943f),295.477081f),
new SpawnPoint(new Vector3(-17.3238258f, 4983.55566f, 423.574341f),24.49474f),
new SpawnPoint(new Vector3(-44.49712f, 4962.519f, 403.271454f),152.463623f),
new SpawnPoint(new Vector3(-64.3459549f, 4937.642f, 394.9339f),87.16081f),
new SpawnPoint(new Vector3(-89.36766f, 4949.33643f, 385.3245f),66.7755356f),
new SpawnPoint(new Vector3(-101.852348f, 4947.05957f, 377.461578f),104.571587f),
new SpawnPoint(new Vector3(-130.419266f, 4937.267f, 357.163116f),105.677422f),
new SpawnPoint(new Vector3(-139.617081f, 4934.09863f, 346.412445f),129.4759f),
new SpawnPoint(new Vector3(-155.503235f, 4925.29541f, 332.3326f),97.0726547f),
new SpawnPoint(new Vector3(-182.925858f, 4937.796f, 311.0382f),47.0801239f),
new SpawnPoint(new Vector3(-201.631439f, 4942.706f, 302.545929f),27.5141773f),
new SpawnPoint(new Vector3(-220.395935f, 4926.42236f, 302.837128f),94.14835f),
new SpawnPoint(new Vector3(-245.385269f, 4926.46631f, 292.91925f),100.503868f),
new SpawnPoint(new Vector3(-279.3522f, 4929.38f, 285.275574f),45.0660133f),
new SpawnPoint(new Vector3(-287.15506f, 4937.951f, 274.452454f),15.1605806f),
new SpawnPoint(new Vector3(-294.586273f, 4960.641f, 253.043823f),353.9249f),
new SpawnPoint(new Vector3(-297.701477f, 4979.934f, 235.848312f),15.7024364f),
new SpawnPoint(new Vector3(-307.0523f, 5002.77441f, 214.6631f),27.8120365f),
new SpawnPoint(new Vector3(-317.648346f, 5031.46875f, 193.130966f),0.233565778f),
new SpawnPoint(new Vector3(-356.13028f, 5049.18848f, 185.455856f),79.96744f),
new SpawnPoint(new Vector3(-440.070038f, 5040.913f, 159.364563f),113.506935f),
new SpawnPoint(new Vector3(-461.7886f, 5003.24f, 153.674561f),237.191513f),
new SpawnPoint(new Vector3(-456.77948f, 4961.62f, 157.86734f),105.4212f),
new SpawnPoint(new Vector3(-484.358154f, 4968.279f, 148.328018f),70.32703f),
new SpawnPoint(new Vector3(-491.524078f, 4973.884f, 145.324051f),49.65514f),
new SpawnPoint(new Vector3(-512.3249f, 5040.344f, 138.548248f),14.5983677f),
new SpawnPoint(new Vector3(-526.313965f, 5055.941f, 130.57991f),85.0179f),
new SpawnPoint(new Vector3(-540.53595f, 5059.179f, 127.160706f),73.17911f),
new SpawnPoint(new Vector3(-540.391052f, 5079.0127f, 123.090584f),351.315338f),
new SpawnPoint(new Vector3(-543.662842f, 5101.08154f, 117.129532f),14.31512f),
new SpawnPoint(new Vector3(-543.2406f, 5111.053f, 112.312019f),349.5798f),
new SpawnPoint(new Vector3(-536.2225f, 5116.059f, 109.626274f),293.6477f),
new SpawnPoint(new Vector3(-522.545166f, 5133.72656f, 90.6089f),297.8189f),
new SpawnPoint(new Vector3(-520.6877f, 5146.26172f, 90.4727f),330.6993f),
new SpawnPoint(new Vector3(-522.8469f, 5157.751f, 89.9282455f),22.23947f),
new SpawnPoint(new Vector3(-497.010468f, 5166.28564f, 87.97265f),220.452927f),
new SpawnPoint(new Vector3(-491.359436f, 5181.97754f, 91.01167f),1.49186993f),
new SpawnPoint(new Vector3(-494.856964f, 5211.47266f, 88.5982742f),350.292145f),
new SpawnPoint(new Vector3(-472.043f, 5244.69629f, 86.8926239f),334.690765f),
new SpawnPoint(new Vector3(-474.5978f, 5193.816f, 97.32246f),293.0384f),
new SpawnPoint(new Vector3(-463.996277f, 5193.75635f, 105.78157f),257.706573f),
new SpawnPoint(new Vector3(-450.1168f, 5203.475f, 111.137566f),288.8467f),
new SpawnPoint(new Vector3(-430.880585f, 5220.348f, 120.805305f),317.7087f),
new SpawnPoint(new Vector3(-428.938477f, 5253.594f, 122.398911f),350.6251f),
new SpawnPoint(new Vector3(-420.580261f, 5276.34326f, 123.137512f),328.100525f),
new SpawnPoint(new Vector3(-399.180359f, 5278.054f, 127.835991f),241.573517f),
new SpawnPoint(new Vector3(-387.851135f, 5300.549f, 120.542961f),322.338348f),
new SpawnPoint(new Vector3(-389.777161f, 5343.236f, 113.009666f),25.8084755f),
new SpawnPoint(new Vector3(-403.283051f, 5354.76074f, 110.355896f),38.0911179f),
new SpawnPoint(new Vector3(-385.126526f, 5375.20654f, 115.000618f),320.514221f),
new SpawnPoint(new Vector3(-348.2758f, 5389.77051f, 129.58313f),280.501038f),
new SpawnPoint(new Vector3(-340.975922f, 5384.807f, 135.681732f),234.751617f),
new SpawnPoint(new Vector3(-295.551727f, 5382.645f, 168.4615f),254.593292f),
new SpawnPoint(new Vector3(-271.241547f, 5369.919f, 190.4684f),204.578217f),
new SpawnPoint(new Vector3(-256.680542f, 5302.137f, 201.5506f),193.178055f),
new SpawnPoint(new Vector3(-253.358536f, 5282.70361f, 199.323135f),231.011337f),
new SpawnPoint(new Vector3(-284.705933f, 5297.34375f, 171.952377f),66.69539f),
new SpawnPoint(new Vector3(-320.8964f, 5299.20947f, 148.995575f),59.32325f),
new SpawnPoint(new Vector3(-331.7852f, 5284.71631f, 156.4301f),144.424088f),
new SpawnPoint(new Vector3(-374.467224f, 5291.062f, 133.058167f),41.4329643f),
new SpawnPoint(new Vector3(-388.729248f, 5289.466f, 126.962318f),74.1078949f),
new SpawnPoint(new Vector3(-413.8156f, 5292.38f, 122.836716f),126.029236f),
new SpawnPoint(new Vector3(-427.446533f, 5283.24658f, 120.229965f),122.823349f),
new SpawnPoint(new Vector3(-420.9117f, 5255.179f, 125.0037f),214.215256f),
new SpawnPoint(new Vector3(-390.62854f, 5213.36133f, 135.999237f),215.8429f),
new SpawnPoint(new Vector3(-221.326736f, 5209.136f, 248.9127f),350.752441f),
new SpawnPoint(new Vector3(-197.301926f, 5224.802f, 257.836853f),298.271759f),
new SpawnPoint(new Vector3(-181.893646f, 5208.604f, 267.9176f),198.343933f),
new SpawnPoint(new Vector3(-186.829269f, 5210.60645f, 268.5113f),63.97286f),
new SpawnPoint(new Vector3(-201.5777f, 4119.615f, 32.6752243f),335.5581f),
new SpawnPoint(new Vector3(-183.899765f, 4150.82275f, 33.7705f),338.671173f),
new SpawnPoint(new Vector3(-195.139557f, 4186.501f, 42.26423f),1.954058f),
new SpawnPoint(new Vector3(-217.176636f, 4245.557f, 33.20371f),0.160702586f),
new SpawnPoint(new Vector3(-240.803726f, 4251.94971f, 32.05776f),87.69461f),
new SpawnPoint(new Vector3(-269.5717f, 4247.8584f, 31.620491f),89.48447f),
new SpawnPoint(new Vector3(-269.724243f, 4283.352f, 33.1301537f),342.2935f),
new SpawnPoint(new Vector3(-283.504059f, 4302.483f, 33.2627449f),84.0722046f),
new SpawnPoint(new Vector3(-344.675873f, 4351.702f, 56.5382652f),34.07033f),
new SpawnPoint(new Vector3(-364.122223f, 4353.155f, 56.45234f),118.20636f),
new SpawnPoint(new Vector3(-389.2351f, 4333.70264f, 55.828186f),146.55925f),
new SpawnPoint(new Vector3(-414.4059f, 4322.827f, 59.177372f),102.696533f),
new SpawnPoint(new Vector3(-437.872467f, 4322.381f, 59.4795341f),99.32971f),
new SpawnPoint(new Vector3(-440.7893f, 4346.387f, 61.719677f),4.90874958f),
new SpawnPoint(new Vector3(-454.6255f, 4369.124f, 51.9051437f),75.68551f),
new SpawnPoint(new Vector3(-471.290344f, 4383.477f, 34.2159958f),46.3635979f),
new SpawnPoint(new Vector3(-482.4824f, 4383.76953f, 31.4154816f),99.89023f),
new SpawnPoint(new Vector3(-491.137268f, 4399.013f, 32.47297f),37.29892f),
new SpawnPoint(new Vector3(-502.2845f, 4401.278f, 32.24706f),59.1245651f),
new SpawnPoint(new Vector3(-529.6079f, 4399.665f, 33.3618279f),141.6755f),
new SpawnPoint(new Vector3(-540.1357f, 4404.135f, 34.20221f),42.54764f),
new SpawnPoint(new Vector3(-557.5044f, 4390.67432f, 28.5136452f),118.994774f),
new SpawnPoint(new Vector3(-570.564453f, 4392.77246f, 20.779932f),65.89974f),
new SpawnPoint(new Vector3(-587.501343f, 4400.55029f, 16.515461f),70.4312f),
new SpawnPoint(new Vector3(-603.9873f, 4396.56543f, 16.6725883f),103.478516f),
new SpawnPoint(new Vector3(-641.8885f, 4407.103f, 15.8946486f),122.713921f),
new SpawnPoint(new Vector3(-672.2763f, 4406.698f, 17.6778355f),82.07145f),
new SpawnPoint(new Vector3(-699.513f, 4412.93262f, 18.403904f),64.01897f),
new SpawnPoint(new Vector3(-714.8458f, 4426.23828f, 16.0174389f),55.2200165f),
new SpawnPoint(new Vector3(-868.5808f, 2174.23242f, 131.5719f),2.07890916f),
new SpawnPoint(new Vector3(-362.472229f, 4292.828f, 51.3251648f),313.618073f),
new SpawnPoint(new Vector3(-402.0669f, 4349.889f, 55.0911636f),28.1602955f),
new SpawnPoint(new Vector3(-433.835724f, 4353.46533f, 57.74774f),69.3013153f),
new SpawnPoint(new Vector3(-452.65448f, 4342.59375f, 67.73237f),179.809f),
new SpawnPoint(new Vector3(-462.636383f, 4337.65625f, 59.8160172f),38.08983f),
new SpawnPoint(new Vector3(-474.277771f, 4353.175f, 54.25783f),243.873383f),
new SpawnPoint(new Vector3(-476.8974f, 4360.977f, 54.38122f),38.2752571f),
new SpawnPoint(new Vector3(-479.936523f, 4362.53f, 55.5310631f),101.103554f),
new SpawnPoint(new Vector3(-485.928284f, 4384.982f, 31.0160847f),11.954627f),
new SpawnPoint(new Vector3(-489.6944f, 4397.07129f, 31.8130112f),44.7017365f),
new SpawnPoint(new Vector3(-495.440247f, 4398.659f, 32.60377f),84.0916f),
new SpawnPoint(new Vector3(-503.108368f, 4400.954f, 31.9748154f),72.03482f),
new SpawnPoint(new Vector3(-529.608032f, 4399.7666f, 33.32637f),121.332268f),
new SpawnPoint(new Vector3(-537.66156f, 4401.69043f, 34.3985558f),40.6079063f),
new SpawnPoint(new Vector3(-551.147034f, 4407.68652f, 32.0752144f),55.3432045f),
new SpawnPoint(new Vector3(-566.3237f, 4391.212f, 22.7538643f),92.94028f),
new SpawnPoint(new Vector3(-579.9479f, 4398.433f, 17.9258137f),77.51895f),
new SpawnPoint(new Vector3(-603.484131f, 4394.555f, 16.9287148f),98.87479f),
new SpawnPoint(new Vector3(-618.426453f, 4389.154f, 22.115633f),80.17779f),
new SpawnPoint(new Vector3(-648.6145f, 4399.35547f, 17.6709728f),66.64243f),
new SpawnPoint(new Vector3(-665.9337f, 4408.497f, 16.91055f),15.0148659f),
new SpawnPoint(new Vector3(-672.345764f, 4410.048f, 16.9033566f),97.1868f),
new SpawnPoint(new Vector3(-674.045959f, 4402.209f, 18.6739941f),206.679169f),
new SpawnPoint(new Vector3(-684.979736f, 4413.00635f, 17.7378731f),48.1730843f),
new SpawnPoint(new Vector3(-708.3544f, 4425.893f, 16.19395f),70.75669f),
new SpawnPoint(new Vector3(-714.8319f, 4420.606f, 16.95405f),177.322144f),
new SpawnPoint(new Vector3(-720.9496f, 4411.20654f, 22.1429768f),70.57729f),
new SpawnPoint(new Vector3(-764.1362f, 4391.568f, 28.1494122f),125.887756f),
new SpawnPoint(new Vector3(-766.610046f, 4380.11963f, 32.17879f),341.037f),
new SpawnPoint(new Vector3(-757.2106f, 4293.69971f, 144.213089f),336.1758f),
new SpawnPoint(new Vector3(-758.33075f, 4311.415f, 143.483246f),353.700775f),
new SpawnPoint(new Vector3(-754.9842f, 4318.6377f, 141.885559f),338.56488f),
new SpawnPoint(new Vector3(-743.808655f, 4323.103f, 141.189758f),283.635254f),
new SpawnPoint(new Vector3(-751.5855f, 4329.022f, 141.879089f),50.98787f),
new SpawnPoint(new Vector3(-752.603f, 4360.458f, 44.739994f),256.3932f),
new SpawnPoint(new Vector3(-740.089844f, 4365.66162f, 53.2742767f),311.654236f),
new SpawnPoint(new Vector3(-739.4895f, 4389.466f, 42.5683479f),37.9574242f),
new SpawnPoint(new Vector3(-753.0135f, 4421.231f, 17.8956718f),29.10144f),
new SpawnPoint(new Vector3(-768.920166f, 4432.47949f, 16.085001f),40.1716156f),
new SpawnPoint(new Vector3(-787.637268f, 4429.49658f, 15.8562365f),135.293869f),
new SpawnPoint(new Vector3(-800.028931f, 4420.95068f, 17.7286224f),111.224213f),
new SpawnPoint(new Vector3(-841.1247f, 4425.685f, 16.3260345f),52.96052f),
new SpawnPoint(new Vector3(-857.891235f, 4420.47168f, 15.5045919f),121.764771f),
new SpawnPoint(new Vector3(-872.645142f, 4408.232f, 18.5825481f),114.983894f),
new SpawnPoint(new Vector3(-877.4487f, 4406.76f, 17.9403286f),77.65484f),
new SpawnPoint(new Vector3(-896.8284f, 4387.84961f, 11.7084141f),163.4579f),
new SpawnPoint(new Vector3(-894.6335f, 4365.997f, 20.2782917f),246.160324f),
new SpawnPoint(new Vector3(-900.3865f, 4357.99f, 24.5569763f),140.993164f),
new SpawnPoint(new Vector3(-884.758667f, 4367.554f, 21.67118f),210.241425f),
new SpawnPoint(new Vector3(-881.339f, 4369.00439f, 22.7255516f),275.11322f),
new SpawnPoint(new Vector3(-908.9486f, 4381.55371f, 15.8804379f),56.6860847f),
new SpawnPoint(new Vector3(-931.724365f, 4370.64063f, 14.1097507f),55.6222153f),
new SpawnPoint(new Vector3(-950.8683f, 4343.96338f, 13.8634758f),167.061752f),
new SpawnPoint(new Vector3(-959.006836f, 4337.528f, 13.818615f),171.605453f),
new SpawnPoint(new Vector3(-965.49884f, 4327.42432f, 18.28374f),132.682739f),
new SpawnPoint(new Vector3(-973.149963f, 4323.865f, 18.7860031f),99.18115f),
new SpawnPoint(new Vector3(-976.7588f, 4310.141f, 23.9507637f),182.484314f),
new SpawnPoint(new Vector3(-982.2202f, 4337.39355f, 15.863554f),27.4491577f),
new SpawnPoint(new Vector3(-975.4334f, 4392.27441f, 15.7022686f),345.463135f),
new SpawnPoint(new Vector3(-974.566345f, 4388.29f, 13.845356f),229.34996f),
new SpawnPoint(new Vector3(-965.6984f, 4390.833f, 14.16617f),291.802551f),
new SpawnPoint(new Vector3(-961.653442f, 4399.136f, 14.2418728f),334.068359f),
new SpawnPoint(new Vector3(-956.6964f, 4407.668f, 16.1859112f),347.7815f),
new SpawnPoint(new Vector3(-954.034546f, 4433.53467f, 18.22622f),8.051124f),
new SpawnPoint(new Vector3(-959.5437f, 4436.17041f, 21.80348f),119.941765f),
new SpawnPoint(new Vector3(-960.215637f, 4447.263f, 26.60853f),166.553619f),
new SpawnPoint(new Vector3(-1034.95313f, 4417.65137f, 26.1542149f),309.619171f),
new SpawnPoint(new Vector3(-1065.77417f, 4410.7666f, 14.6262684f),90.51211f),
new SpawnPoint(new Vector3(-1101.80823f, 4425.109f, 12.4079971f),67.98993f),
new SpawnPoint(new Vector3(-1116.54553f, 4412.6084f, 11.7707462f),214.1185f),
new SpawnPoint(new Vector3(-1130.67932f, 4413.181f, 10.8602209f),89.12177f),
new SpawnPoint(new Vector3(-1141.37842f, 4411.90137f, 11.6742134f),86.267395f),
new SpawnPoint(new Vector3(-1147.01746f, 4414.69775f, 12.7667761f),94.8933f),
new SpawnPoint(new Vector3(-1162.61792f, 4412.21533f, 10.321599f),124.822166f),
new SpawnPoint(new Vector3(-1166.57166f, 4410.09766f, 8.926364f),106.9642f),
new SpawnPoint(new Vector3(-1169.01355f, 4405.27f, 8.016118f),184.663773f),
new SpawnPoint(new Vector3(-1182.78064f, 4411.01172f, 8.612381f),32.12381f),
new SpawnPoint(new Vector3(-1196.09143f, 4410.6626f, 7.0998373f),72.80156f),
new SpawnPoint(new Vector3(-1201.49707f, 4412.063f, 8.957866f),63.12158f),
new SpawnPoint(new Vector3(-1207.05884f, 4411.31836f, 9.984347f),134.157669f),
new SpawnPoint(new Vector3(-1210.42908f, 4411.725f, 10.0789356f),60.28359f),
new SpawnPoint(new Vector3(-1222.34155f, 4410.79736f, 9.464966f),100.516609f),
new SpawnPoint(new Vector3(-1241.20569f, 4416.976f, 8.081937f),47.91264f),
new SpawnPoint(new Vector3(-1250.92407f, 4423.404f, 5.927222f),30.7534275f),
new SpawnPoint(new Vector3(-1253.30383f, 4431.816f, 6.21182f),3.67506671f),
new SpawnPoint(new Vector3(-1258.95813f, 4448.398f, 7.684009f),44.85951f),
new SpawnPoint(new Vector3(-1266.41687f, 4453.96436f, 8.292778f),51.1026421f),
new SpawnPoint(new Vector3(-1274.02942f, 4458.228f, 10.0147018f),95.17624f),
new SpawnPoint(new Vector3(-1282.63232f, 4457.71826f, 12.3389168f),99.47585f),
new SpawnPoint(new Vector3(-1285.83008f, 4460.828f, 13.6773491f),4.742505f),
new SpawnPoint(new Vector3(-1289.19617f, 4463.633f, 14.82387f),95.65889f),
new SpawnPoint(new Vector3(-1298.172f, 4468.32568f, 17.2959461f),355.9334f),
new SpawnPoint(new Vector3(-1295.53137f, 4473.44873f, 18.6966648f),328.1984f),
new SpawnPoint(new Vector3(-1278.57642f, 4476.324f, 10.7894936f),256.369629f),
new SpawnPoint(new Vector3(-1272.08191f, 4476.103f, 10.0624809f),308.0018f),
new SpawnPoint(new Vector3(-1267.59229f, 4483.598f, 12.3847456f),323.26474f),
new SpawnPoint(new Vector3(-1252.02393f, 4486.724f, 23.35584f),178.537384f),
new SpawnPoint(new Vector3(-1276.27722f, 4506.608f, 20.1131687f),94.72134f),
new SpawnPoint(new Vector3(-1276.373f, 4496.252f, 19.8854218f),307.30722f),
new SpawnPoint(new Vector3(-1290.08142f, 4502.656f, 16.1377544f),167.2636f),
new SpawnPoint(new Vector3(-1292.63708f, 4497.41455f, 15.1863976f),146.27095f),
new SpawnPoint(new Vector3(-1289.5304f, 4492.531f, 13.6970339f),303.7402f),
new SpawnPoint(new Vector3(-1292.44666f, 4508.69336f, 17.5253315f),21.6735363f),
new SpawnPoint(new Vector3(-1296.43f, 4514.947f, 19.4087811f),58.75427f),
new SpawnPoint(new Vector3(-1305.758f, 4511.429f, 23.4671459f),125.440269f),
new SpawnPoint(new Vector3(-1312.9176f, 4505.129f, 26.1528168f),153.146225f),
new SpawnPoint(new Vector3(-1318.32471f, 4506.3f, 29.1028767f),46.7987137f),
new SpawnPoint(new Vector3(-1321.99255f, 4518.133f, 32.3517838f),12.8666153f),
new SpawnPoint(new Vector3(-1327.033f, 4533.4375f, 37.1273155f),40.6836357f),
new SpawnPoint(new Vector3(-1335.71375f, 4528.00342f, 41.4431458f),130.908676f),
new SpawnPoint(new Vector3(-1354.03967f, 4518.39258f, 44.92411f),100.89183f),
new SpawnPoint(new Vector3(-1358.94031f, 4528.11865f, 51.4811821f),344.1784f),
new SpawnPoint(new Vector3(-1356.93347f, 4545.103f, 60.4435577f),6.74595737f),
new SpawnPoint(new Vector3(-1360.5072f, 4559.032f, 69.808876f),14.9581976f),
new SpawnPoint(new Vector3(-1367.68848f, 4555.997f, 70.04159f),116.076912f),
new SpawnPoint(new Vector3(-1375.59546f, 4542.603f, 63.83474f),117.548584f),
new SpawnPoint(new Vector3(-1382.69836f, 4544.23242f, 64.97695f),71.1667938f),
new SpawnPoint(new Vector3(-1387.93408f, 4552.42236f, 68.56229f),4.32784271f),
new SpawnPoint(new Vector3(-1378.84106f, 4570.966f, 76.2731f),322.61972f),
new SpawnPoint(new Vector3(-1373.93872f, 4581.435f, 81.87867f),12.3456478f),
new SpawnPoint(new Vector3(-1376.46289f, 4596.238f, 83.06201f),7.51052666f),
new SpawnPoint(new Vector3(-1387.77661f, 4605.71045f, 76.85422f),311.427551f),
new SpawnPoint(new Vector3(-1379.97314f, 4609.57861f, 81.14659f),278.5006f),
new SpawnPoint(new Vector3(-1402.21155f, 4606.67041f, 68.18013f),126.99041f),
new SpawnPoint(new Vector3(-1414.608f, 4598.71f, 58.4543762f),122.561546f),
new SpawnPoint(new Vector3(-1422.52478f, 4591.105f, 53.41595f),152.563461f),
new SpawnPoint(new Vector3(-1427.8595f, 4592.553f, 52.97085f),55.2390938f),
new SpawnPoint(new Vector3(-1436.858f, 4587.34229f, 49.9291229f),195.753448f),
new SpawnPoint(new Vector3(-1426.70129f, 4572.05f, 46.9402847f),223.458771f),
new SpawnPoint(new Vector3(-1420.20679f, 4564.75439f, 52.23947f),189.895233f),
new SpawnPoint(new Vector3(-1427.73425f, 4553.296f, 54.62248f),139.6964f),
new SpawnPoint(new Vector3(-1444.74219f, 4535.49561f, 58.185894f),128.765045f),
new SpawnPoint(new Vector3(-1481.97388f, 4529.091f, 53.4694061f),85.63921f),
new SpawnPoint(new Vector3(-1511.15527f, 4528.621f, 46.0702324f),58.54708f),
new SpawnPoint(new Vector3(-1530.60583f, 4523.68262f, 43.90338f),41.671833f),
new SpawnPoint(new Vector3(-1533.74109f, 4531.905f, 48.06427f),22.6416073f),
new SpawnPoint(new Vector3(-1537.83655f, 4531.02148f, 46.8093567f),9.617023f),
new SpawnPoint(new Vector3(-1540.10657f, 4536.121f, 47.4582748f),4.88089848f),
new SpawnPoint(new Vector3(-1534.90784f, 4541.40869f, 47.3380928f),302.9284f),
new SpawnPoint(new Vector3(-1536.86389f, 4551.93555f, 44.7338867f),16.1760426f),
new SpawnPoint(new Vector3(-1537.66541f, 4557.58838f, 39.4513855f),34.2353668f),
new SpawnPoint(new Vector3(-1537.52832f, 4562.64063f, 39.6439362f),304.5548f),
new SpawnPoint(new Vector3(-1532.99768f, 4570.504f, 39.8856163f),344.8659f),
new SpawnPoint(new Vector3(-1532.51929f, 4586.982f, 30.8602448f),17.4409332f),
new SpawnPoint(new Vector3(-1529.0155f, 4595.25244f, 28.2892532f),316.316772f),
new SpawnPoint(new Vector3(-1518.55811f, 4595.704f, 31.4604912f),235.763458f),
new SpawnPoint(new Vector3(-1508.43f, 4595.336f, 36.36076f),333.110352f),
new SpawnPoint(new Vector3(-1503.57068f, 4598.63965f, 38.9368019f),323.149841f),
new SpawnPoint(new Vector3(-1495.99426f, 4601.62646f, 41.7757034f),225.435562f),
new SpawnPoint(new Vector3(-1490.77f, 4597.75244f, 42.283577f),243.173889f),
new SpawnPoint(new Vector3(-1481.99939f, 4591.83936f, 43.5485458f),227.753632f),
new SpawnPoint(new Vector3(-1477.10693f, 4591.6377f, 44.752697f),8.037369f),
new SpawnPoint(new Vector3(-1476.10144f, 4597.29639f, 46.2260323f),300.488617f),
new SpawnPoint(new Vector3(-1473.029f, 4599.456f, 47.08444f),312.885681f),
new SpawnPoint(new Vector3(-1468.56262f, 4611.946f, 49.7115135f),8.649246f),
new SpawnPoint(new Vector3(-1485.73157f, 4623.2627f, 47.6014328f),84.16783f),
new SpawnPoint(new Vector3(-1500.16821f, 4624.76025f, 42.00501f),84.3837662f),
new SpawnPoint(new Vector3(-1526.82642f, 4623.46436f, 32.29062f),96.2678452f),
new SpawnPoint(new Vector3(-1569.4248f, 4615.092f, 25.9932728f),128.598877f),
new SpawnPoint(new Vector3(-1578.05676f, 4608.776f, 27.950304f),90.67453f),
new SpawnPoint(new Vector3(-1590.00415f, 4609.30371f, 33.11342f),82.2120743f),
new SpawnPoint(new Vector3(-1596.20215f, 4617.734f, 41.48804f),359.3555f),
new SpawnPoint(new Vector3(-1582.69214f, 4636.54834f, 44.0523338f),307.9357f),
new SpawnPoint(new Vector3(-1584.415f, 4649.63525f, 46.9295769f),104.254585f),
new SpawnPoint(new Vector3(-1597.00427f, 4652.387f, 48.3825531f),70.93297f),
new SpawnPoint(new Vector3(-1607.72949f, 4619.998f, 44.8215637f),174.00589f),
new SpawnPoint(new Vector3(-1611.088f, 4608.926f, 41.5539246f),150.835724f),
new SpawnPoint(new Vector3(-1611.78284f, 4597.65f, 40.8527031f),183.046692f),
new SpawnPoint(new Vector3(-1618.63269f, 4592.92432f, 42.4209175f),123.316528f),
new SpawnPoint(new Vector3(-1625.57434f, 4584.12842f, 43.25633f),153.917664f),
new SpawnPoint(new Vector3(-1636.638f, 4567.87744f, 43.0585175f),87.06563f),
new SpawnPoint(new Vector3(-1645.46509f, 4570.922f, 42.91766f),57.93814f),
new SpawnPoint(new Vector3(-1655.299f, 4569.53369f, 41.75973f),158.358887f),
new SpawnPoint(new Vector3(-1650.28027f, 4558.953f, 43.2020073f),263.137482f),
new SpawnPoint(new Vector3(-1639.384f, 4558.07129f, 43.33862f),260.376526f),
new SpawnPoint(new Vector3(-1628.28845f, 4553.612f, 43.4484f),233.930725f),
new SpawnPoint(new Vector3(-1622.02563f, 4537.88f, 44.6438026f),187.119339f),
new SpawnPoint(new Vector3(-1619.60046f, 4538.01172f, 44.2385635f),270.999146f),
new SpawnPoint(new Vector3(-1625.87549f, 4538.87646f, 44.0648079f),81.562355f),
new SpawnPoint(new Vector3(-1635.44531f, 4538.58936f, 40.72878f),129.4664f),
new SpawnPoint(new Vector3(-1636.46021f, 4533.84668f, 41.73032f),177.245209f),
new SpawnPoint(new Vector3(-1639.54846f, 4531.89844f, 41.933815f),124.231422f),
new SpawnPoint(new Vector3(-1646.1803f, 4524.84961f, 40.30443f),215.068146f),
new SpawnPoint(new Vector3(-1654.019f, 4521.97559f, 34.50522f),165.736969f),
new SpawnPoint(new Vector3(-1662.268f, 4517.7207f, 33.2722244f),206.329117f),
new SpawnPoint(new Vector3(-1660.235f, 4493.607f, 1.7742666f),181.956558f),
new SpawnPoint(new Vector3(-1657.852f, 4478.49951f, 1.11343622f),223.0086f),
new SpawnPoint(new Vector3(-1667.32129f, 4462.46875f, 1.33442187f),226.2502f),
new SpawnPoint(new Vector3(-1654.33313f, 4455.27051f, 1.39461362f),250.527649f),
new SpawnPoint(new Vector3(-1632.89063f, 4435.6377f, 1.384381f),220.57692f),
new SpawnPoint(new Vector3(-1640.8916f, 4412.562f, 4.35024261f),156.332291f),
new SpawnPoint(new Vector3(-1652.32825f, 4400.03564f, 11.0813894f),161.064468f),
new SpawnPoint(new Vector3(-1648.1687f, 4391.443f, 10.3354406f),201.002777f),
new SpawnPoint(new Vector3(-1644.82373f, 4378.805f, 11.4578962f),190.548843f),
new SpawnPoint(new Vector3(-1645.46753f, 4361.67236f, 13.2748251f),196.673615f),
new SpawnPoint(new Vector3(-1633.82581f, 4358.03271f, 11.098835f),255.946747f),
new SpawnPoint(new Vector3(-1627.66211f, 4360.857f, 7.09107065f),331.558838f),
new SpawnPoint(new Vector3(-1621.12915f, 4365.11426f, 4.530747f),275.506836f),
new SpawnPoint(new Vector3(-1613.10767f, 4362.61f, 4.19468737f),253.8762f),
new SpawnPoint(new Vector3(-1599.95544f, 4333.68945f, 8.211993f),133.092163f),
new SpawnPoint(new Vector3(-1608.64246f, 4322.33f, 18.9347134f),165.813385f),
new SpawnPoint(new Vector3(-1606.64136f, 4319.116f, 19.6973114f),239.736862f),
new SpawnPoint(new Vector3(-1599.20142f, 4321.42334f, 17.2586422f),315.381256f),
new SpawnPoint(new Vector3(-1584.09106f, 4329.40332f, 6.773141f),273.903748f),
new SpawnPoint(new Vector3(-1564.06677f, 4322.85449f, 5.638284f),249.171448f),
new SpawnPoint(new Vector3(-1553.10217f, 4339.524f, 2.01343536f),14.8072519f),
new SpawnPoint(new Vector3(-1545.90625f, 4340.9834f, 2.05140042f),277.747345f),
new SpawnPoint(new Vector3(-1535.99231f, 4341.078f, 1.02650416f),255.040817f),
new SpawnPoint(new Vector3(-1576.03186f, 4347.705f, 1.87255609f),24.689291f),
new SpawnPoint(new Vector3(-1597.79626f, 4372.42773f, 3.15482044f),352.9447f),
new SpawnPoint(new Vector3(-1592.56787f, 4367.91f, 1.34281933f),310.346039f),
new SpawnPoint(new Vector3(-1576.91821f, 4374.57129f, 1.643138f),232.835556f),
new SpawnPoint(new Vector3(-1571.66675f, 4370.288f, 1.38241649f),259.594025f),
new SpawnPoint(new Vector3(-1569.20337f, 4375.037f, 3.130733f),332.71344f),
new SpawnPoint(new Vector3(-1564.61157f, 4378.77051f, 4.595024f),277.447266f),
new SpawnPoint(new Vector3(-1553.006f, 4377.03125f, 4.274426f),245.0235f),
new SpawnPoint(new Vector3(-1542.69434f, 4368.61133f, 2.07590246f),270.711f),
new SpawnPoint(new Vector3(-1534.3894f, 4367.55664f, 2.41084576f),214.111221f),
new SpawnPoint(new Vector3(-1526.592f, 4365.91943f, 2.44477916f),285.5891f),
new SpawnPoint(new Vector3(-1517.3053f, 4366.599f, 2.41839147f),272.757751f),
new SpawnPoint(new Vector3(-1504.3689f, 4369.19873f, 2.354988f),353.970978f),
new SpawnPoint(new Vector3(-1508.38147f, 4372.04834f, 3.87656236f),84.10689f),
new SpawnPoint(new Vector3(-1513.77112f, 4375.33154f, 4.013439f),30.87677f),
new SpawnPoint(new Vector3(-1525.26379f, 4376.97852f, 3.93164635f),51.6357841f),
new SpawnPoint(new Vector3(-1532.76941f, 4378.01758f, 5.40418339f),45.7014923f),
new SpawnPoint(new Vector3(-1529.0791f, 4379.48f, 6.097272f),241.704987f),
new SpawnPoint(new Vector3(-1526.01868f, 4383.17627f, 9.469782f),11.5804882f),
new SpawnPoint(new Vector3(-1531.75269f, 4382.09863f, 8.874977f),120.701866f),
new SpawnPoint(new Vector3(-1538.90649f, 4385.055f, 8.122932f),40.76235f),
new SpawnPoint(new Vector3(-1546.76636f, 4391.529f, 6.58644056f),32.38812f),
new SpawnPoint(new Vector3(-1552.21875f, 4398.8374f, 6.81211758f),80.84772f),
new SpawnPoint(new Vector3(-1613.80688f, 4174.13965f, 135.168945f),123.149124f),
new SpawnPoint(new Vector3(-1619.7561f, 4167.81543f, 138.774963f),134.64798f),
new SpawnPoint(new Vector3(-1626.9668f, 4167.54053f, 138.7731f),80.77808f),
new SpawnPoint(new Vector3(-1642.615f, 4163.82275f, 137.64769f),90.30015f),
new SpawnPoint(new Vector3(-1652.88281f, 4170.297f, 129.018188f),47.7878036f),
new SpawnPoint(new Vector3(-1656.309f, 4185.461f, 116.021324f),0.0384954847f),
new SpawnPoint(new Vector3(-1657.22583f, 4183.73242f, 115.601295f),80.2998352f),
new SpawnPoint(new Vector3(-1659.78088f, 4208.527f, 85.32514f),86.71511f),
new SpawnPoint(new Vector3(-1669.0575f, 4206.72656f, 91.76259f),114.339378f),
new SpawnPoint(new Vector3(-1676.457f, 4202.092f, 95.10025f),137.212677f),
new SpawnPoint(new Vector3(-1681.73047f, 4193.542f, 99.17797f),143.678665f),
new SpawnPoint(new Vector3(-1684.00122f, 4172.153f, 115.79055f),295.170471f),
new SpawnPoint(new Vector3(-1680.247f, 4177.499f, 115.335342f),334.0059f),
new SpawnPoint(new Vector3(-1678.51868f, 4217.22949f, 88.92625f),346.644348f),
new SpawnPoint(new Vector3(-1687.75659f, 4249.313f, 77.3535156f),58.33309f),
new SpawnPoint(new Vector3(-1694.02246f, 4255.2627f, 75.21133f),13.2911158f),
new SpawnPoint(new Vector3(-1696.64148f, 4264.078f, 73.40851f),19.3102837f),
new SpawnPoint(new Vector3(-1704.03809f, 4285.49463f, 71.47756f),11.7235994f),
new SpawnPoint(new Vector3(-1704.869f, 4287.931f, 71.1605f),36.8894844f),
new SpawnPoint(new Vector3(-1716.61035f, 4310.7666f, 63.7059364f),58.7575874f),
new SpawnPoint(new Vector3(-1717.02917f, 4309.98f, 63.84686f),145.445633f),
new SpawnPoint(new Vector3(-1714.82227f, 4302.06641f, 65.53671f),208.471924f),
new SpawnPoint(new Vector3(-1697.19409f, 4313.86133f, 67.22859f),283.80304f),
new SpawnPoint(new Vector3(-1698.967f, 4316.623f, 65.83455f),53.3556061f),
new SpawnPoint(new Vector3(-1709.91125f, 4327.35449f, 61.2585f),161.382751f),
new SpawnPoint(new Vector3(-1704.52051f, 4333.426f, 58.19642f),352.2389f),
new SpawnPoint(new Vector3(-1704.48022f, 4350.506f, 53.28352f),336.147278f),
new SpawnPoint(new Vector3(-1698.21826f, 4365.2876f, 48.4799271f),282.205261f),
new SpawnPoint(new Vector3(-1689.08435f, 4367.28125f, 52.3374367f),296.04364f),
new SpawnPoint(new Vector3(-1679.548f, 4361.022f, 56.5096f),262.720673f),
new SpawnPoint(new Vector3(-1676.44214f, 4356.733f, 55.3234253f),223.673569f),
new SpawnPoint(new Vector3(-1682.82251f, 4345.883f, 57.7299538f),151.323624f),
new SpawnPoint(new Vector3(-1658.07715f, 4331.935f, 56.3890266f),252.104736f),
new SpawnPoint(new Vector3(-1656.44421f, 4329.84961f, 57.9256172f),203.835526f),
new SpawnPoint(new Vector3(-1645.2262f, 4313.902f, 69.8772354f),214.312988f),
new SpawnPoint(new Vector3(-1643.387f, 4308.33154f, 73.58614f),153.172546f),
new SpawnPoint(new Vector3(-1647.00049f, 4303.152f, 75.88202f),151.37854f),
new SpawnPoint(new Vector3(-1642.69043f, 4290.502f, 79.03901f),209.139359f),
new SpawnPoint(new Vector3(-1001.85895f, 4155.85742f, 120.246109f),33.45876f),
new SpawnPoint(new Vector3(-1009.52856f, 4159.028f, 123.3549f),45.1963921f),
new SpawnPoint(new Vector3(-1011.45319f, 4166.01172f, 124.044273f),332.5536f),
new SpawnPoint(new Vector3(-1010.10022f, 4177.07568f, 123.740807f),6.30140162f),
new SpawnPoint(new Vector3(-1016.49329f, 4188.43848f, 120.989288f),36.7142029f),
new SpawnPoint(new Vector3(-1018.52179f, 4201.86133f, 120.446007f),349.29248f),
new SpawnPoint(new Vector3(-1017.71027f, 4214.7334f, 117.0741f),346.521149f),
new SpawnPoint(new Vector3(-999.3989f, 4233.691f, 107.672821f),324.601532f),
new SpawnPoint(new Vector3(-1000.209f, 4241.945f, 108.10714f),32.82647f),
new SpawnPoint(new Vector3(-1007.43463f, 4245.94238f, 107.628586f),96.60792f),
new SpawnPoint(new Vector3(-1015.90918f, 4248.912f, 110.575775f),49.25867f),
new SpawnPoint(new Vector3(-1011.83112f, 4267.14551f, 98.01731f),75.5582047f),
new SpawnPoint(new Vector3(-1022.43512f, 4271.42627f, 103.983055f),18.61822f),
new SpawnPoint(new Vector3(-1022.66119f, 4276.837f, 104.097435f),354.040833f),
new SpawnPoint(new Vector3(-1027.11743f, 4278.60742f, 104.196915f),147.7256f),
new SpawnPoint(new Vector3(-1035.65784f, 4293.852f, 104.154518f),357.060547f),
new SpawnPoint(new Vector3(-1049.40552f, 4338.06152f, 33.1880264f),102.331604f),
new SpawnPoint(new Vector3(-1046.114f, 4345.352f, 26.4875679f),182.655136f),
new SpawnPoint(new Vector3(-193.32576f, 3203.84277f, 48.3679581f),334.352631f),
new SpawnPoint(new Vector3(-184.937515f, 3216.972f, 47.7668457f),328.575653f),
new SpawnPoint(new Vector3(-178.6678f, 3227.96387f, 52.6448479f),320.760742f),
new SpawnPoint(new Vector3(-167.630127f, 3233.50928f, 56.9041824f),297.928955f),
new SpawnPoint(new Vector3(-159.74202f, 3242.02612f, 60.17851f),326.868042f),
new SpawnPoint(new Vector3(-161.788742f, 3252.71362f, 64.2609f),11.4351254f),
new SpawnPoint(new Vector3(-159.483856f, 3261.87671f, 67.12134f),347.287231f),
new SpawnPoint(new Vector3(-159.610382f, 3268.592f, 70.3764954f),357.512817f),
new SpawnPoint(new Vector3(-167.333359f, 3277.44678f, 75.5171661f),41.7085342f),
new SpawnPoint(new Vector3(-158.350021f, 3289.1814f, 80.36592f),328.962067f),
new SpawnPoint(new Vector3(-158.264984f, 3292.98315f, 83.10189f),26.6294861f),
new SpawnPoint(new Vector3(-160.582352f, 3297.24072f, 85.2011261f),18.6824512f),
new SpawnPoint(new Vector3(-159.54f, 3309.33228f, 89.68954f),355.629456f),
new SpawnPoint(new Vector3(-159.9118f, 3319.72681f, 92.47461f),6.820024f),
new SpawnPoint(new Vector3(-168.575317f, 3323.282f, 91.5208054f),63.8563766f),
new SpawnPoint(new Vector3(-184.708954f, 3329.812f, 99.70596f),71.62216f),
new SpawnPoint(new Vector3(-195.1921f, 3342.01343f, 99.49357f),68.98555f),
new SpawnPoint(new Vector3(-189.18985f, 3353.79932f, 86.60961f),61.9117f),
new SpawnPoint(new Vector3(-195.321823f, 3362.02246f, 88.18792f),49.5292435f),
new SpawnPoint(new Vector3(-202.337189f, 3361.28149f, 90.53697f),97.6295f),
new SpawnPoint(new Vector3(-208.108154f, 3357.474f, 94.58475f),116.39695f),
new SpawnPoint(new Vector3(-208.672546f, 3343.11938f, 101.561516f),192.00621f),
new SpawnPoint(new Vector3(-199.487076f, 3330.2207f, 107.773659f),230.895218f),
new SpawnPoint(new Vector3(-196.645981f, 3327.79443f, 106.505623f),228.652542f),
new SpawnPoint(new Vector3(-197.706833f, 3324.76123f, 105.976006f),65.60189f),
new SpawnPoint(new Vector3(-200.428787f, 3322.20239f, 106.377663f),188.1276f),
new SpawnPoint(new Vector3(-204.474762f, 3308.2915f, 99.7883f),165.343445f),
new SpawnPoint(new Vector3(-228.7545f, 3305.723f, 96.52344f),130.808533f),
new SpawnPoint(new Vector3(-242.1026f, 3292.061f, 91.3172f),111.323593f),
new SpawnPoint(new Vector3(-250.933838f, 3268.51123f, 82.3503342f),156.070969f),
new SpawnPoint(new Vector3(-243.652161f, 3247.28735f, 78.91347f),209.841125f),
new SpawnPoint(new Vector3(-240.9141f, 3227.19165f, 74.93809f),186.259f),
new SpawnPoint(new Vector3(-263.4589f, 3175.14648f, 64.80913f),150.625366f),
new SpawnPoint(new Vector3(-268.485565f, 3165.62769f, 68.22313f),152.409317f),
new SpawnPoint(new Vector3(-266.22464f, 3151.29761f, 64.3650055f),196.516617f),
new SpawnPoint(new Vector3(-265.326538f, 3137.59644f, 58.49243f),170.901169f),
new SpawnPoint(new Vector3(-274.968781f, 3141.8f, 59.3896065f),58.0679626f),
new SpawnPoint(new Vector3(-285.706146f, 3148.55957f, 60.72538f),114.518051f),
new SpawnPoint(new Vector3(-295.1461f, 3136.96948f, 58.5268745f),98.05432f),
new SpawnPoint(new Vector3(-301.447357f, 3135.06665f, 59.77411f),109.024727f),
new SpawnPoint(new Vector3(-314.329773f, 3130.40723f, 62.1363831f),98.23997f),
new SpawnPoint(new Vector3(-319.504242f, 3141.852f, 68.16419f),317.783447f),
new SpawnPoint(new Vector3(-326.267853f, 3149.33423f, 67.36079f),100.141449f),
new SpawnPoint(new Vector3(-333.853516f, 3142.774f, 63.89256f),181.582184f),
new SpawnPoint(new Vector3(-335.430878f, 3124.49756f, 59.417942f),178.217575f),
new SpawnPoint(new Vector3(-327.903534f, 3103.21948f, 42.5752563f),206.540253f),
new SpawnPoint(new Vector3(-315.8415f, 3097.44482f, 40.1881676f),266.562134f),
new SpawnPoint(new Vector3(-279.6582f, 3075.15942f, 33.3797646f),212.226517f),
new SpawnPoint(new Vector3(-262.754f, 3067.93848f, 31.1679459f),269.905273f),
new SpawnPoint(new Vector3(-249.89975f, 3062.463f, 30.3775921f),198.094849f),
new SpawnPoint(new Vector3(-246.223f, 3040.81372f, 25.0796967f),179.6528f),
new SpawnPoint(new Vector3(-244.353439f, 3028.03833f, 22.3305569f),254.056381f),
new SpawnPoint(new Vector3(-233.191772f, 3032.60059f, 24.659296f),310.224731f),
new SpawnPoint(new Vector3(-228.04126f, 3033.04126f, 26.7134342f),286.90448f),
new SpawnPoint(new Vector3(-223.7591f, 3035.4126f, 26.7016354f),321.867432f),
new SpawnPoint(new Vector3(-216.654037f, 3038.51074f, 26.813427f),338.489166f),
new SpawnPoint(new Vector3(-210.3225f, 3047.42725f, 27.3821926f),314.159973f),
new SpawnPoint(new Vector3(-201.931076f, 3058.271f, 24.11861f),319.425f),
new SpawnPoint(new Vector3(-193.07782f, 3068.53418f, 19.3780975f),319.6656f),
new SpawnPoint(new Vector3(-177.594681f, 3081.66235f, 20.56429f),274.477631f),
new SpawnPoint(new Vector3(-173.338867f, 3080.72632f, 21.9047432f),335.168823f),
new SpawnPoint(new Vector3(-172.3364f, 3082.30786f, 23.7138786f),352.6154f),
new SpawnPoint(new Vector3(-157.0132f, 3070.21631f, 19.92666f),243.759277f),
new SpawnPoint(new Vector3(-156.076736f, 3072.665f, 19.2846375f),339.049622f),
new SpawnPoint(new Vector3(-162.677643f, 3073.57935f, 18.74448f),64.90936f),
new SpawnPoint(new Vector3(-165.023911f, 3065.29053f, 18.8925743f),213.433212f),
new SpawnPoint(new Vector3(-163.2016f, 3061.64258f, 19.9151f),194.479324f),
new SpawnPoint(new Vector3(-158.538071f, 3050.93213f, 22.913105f),229.3732f),
new SpawnPoint(new Vector3(-150.100769f, 3055.77344f, 23.42013f),318.5952f),
new SpawnPoint(new Vector3(-146.406677f, 3068.93359f, 20.7398148f),346.016632f),
new SpawnPoint(new Vector3(-127.167557f, 3079.689f, 23.9515247f),263.130554f),
new SpawnPoint(new Vector3(-88.81288f, 3084.03955f, 28.2099953f),284.314758f),
new SpawnPoint(new Vector3(-61.2567749f, 3088.37476f, 32.11486f),263.679962f),
new SpawnPoint(new Vector3(-45.4968567f, 3064.13354f, 35.647995f),212.781723f),
new SpawnPoint(new Vector3(-27.80827f, 3069.38843f, 36.4495239f),303.4396f),
new SpawnPoint(new Vector3(-23.69915f, 3083.08252f, 33.9602242f),345.2754f),
new SpawnPoint(new Vector3(-12.5523758f, 3093.10913f, 31.0903625f),282.134979f),
new SpawnPoint(new Vector3(8.165873f, 3078.391f, 36.9507332f),274.993835f),
new SpawnPoint(new Vector3(46.9612f, 3037.23853f, 40.9915733f),238.860352f),
new SpawnPoint(new Vector3(55.230896f, 3036.35547f, 40.7154541f),285.6387f),
new SpawnPoint(new Vector3(65.84758f, 3032.35522f, 43.2090263f),213.3983f),
new SpawnPoint(new Vector3(67.01942f, 3022.3042f, 46.6615829f),154.38063f),
new SpawnPoint(new Vector3(45.00612f, 3022.57422f, 49.3090363f),84.99142f),
new SpawnPoint(new Vector3(22.960392f, 3002.02051f, 48.84494f),146.904877f),
new SpawnPoint(new Vector3(11.6848288f, 2993.04883f, 49.5754929f),127.184387f),
new SpawnPoint(new Vector3(12.4255142f, 2987.19849f, 49.3843155f),212.689f),
new SpawnPoint(new Vector3(11.9161663f, 2972.09546f, 52.5089531f),169.032455f),
new SpawnPoint(new Vector3(-3.748573f, 2955.46973f, 53.66341f),111.139458f),
new SpawnPoint(new Vector3(-19.9352818f, 2957.06421f, 49.1399879f),60.47247f),
new SpawnPoint(new Vector3(-34.7921677f, 2958.88623f, 51.3995934f),71.8959045f),
new SpawnPoint(new Vector3(-75.4676f, 2965.27368f, 46.1885948f),62.6481857f),
new SpawnPoint(new Vector3(-88.469574f, 2958.86133f, 45.8072128f),133.675156f),
new SpawnPoint(new Vector3(-96.3135147f, 2947.89282f, 44.863533f),143.6217f),
new SpawnPoint(new Vector3(-100.362617f, 2946.962f, 44.34853f),65.5356f),
new SpawnPoint(new Vector3(-111.311378f, 2948.76221f, 42.3461838f),358.128357f),
new SpawnPoint(new Vector3(-112.778542f, 2944.34766f, 42.58452f),192.83696f),
new SpawnPoint(new Vector3(-124.8995f, 2935.92822f, 40.26932f),72.82476f),
new SpawnPoint(new Vector3(-136.515427f, 2937.31934f, 36.17302f),84.8953552f),
new SpawnPoint(new Vector3(-151.812775f, 2905.15649f, 38.7506638f),125.297371f),
new SpawnPoint(new Vector3(-150.642639f, 2891.83325f, 42.2637978f),149.4915f),
new SpawnPoint(new Vector3(-156.653183f, 2879.62085f, 42.8497238f),172.984467f),
new SpawnPoint(new Vector3(-149.461426f, 2864.15283f, 43.6552048f),185.302277f),
new SpawnPoint(new Vector3(-152.123062f, 2860.00928f, 42.3219223f),130.324249f),
new SpawnPoint(new Vector3(-155.051346f, 2854.53345f, 38.8556252f),154.362991f),
new SpawnPoint(new Vector3(-189.017334f, 2855.58838f, 32.103653f),95.673996f),
new SpawnPoint(new Vector3(-198.246033f, 2852.45044f, 35.6677055f),83.79016f),
new SpawnPoint(new Vector3(-202.1909f, 2854.40161f, 39.41023f),38.36054f),
new SpawnPoint(new Vector3(-209.438889f, 2860.64624f, 41.7637367f),42.7376442f),
new SpawnPoint(new Vector3(-223.861557f, 2868.20581f, 44.62544f),75.19066f),
new SpawnPoint(new Vector3(-259.718781f, 2908.47974f, 43.5259628f),19.0850887f),
new SpawnPoint(new Vector3(-263.789825f, 2918.46753f, 41.9372063f),18.0224075f),
new SpawnPoint(new Vector3(-271.5654f, 2925.71167f, 40.6667976f),66.85762f),
new SpawnPoint(new Vector3(-286.367828f, 2920.75952f, 40.08243f),116.899422f),
new SpawnPoint(new Vector3(-297.1707f, 2916.933f, 38.6587753f),108.063179f),
new SpawnPoint(new Vector3(-306.5801f, 2917.93481f, 37.8898277f),98.8573151f),
new SpawnPoint(new Vector3(-323.658356f, 2915.79541f, 37.11485f),99.32773f),
new SpawnPoint(new Vector3(-361.424469f, 2934.00317f, 30.53524f),74.5791855f),
new SpawnPoint(new Vector3(-372.431f, 2937.98315f, 27.223999f),119.237595f),
new SpawnPoint(new Vector3(-381.390167f, 2937.64038f, 23.67693f),61.08916f),
new SpawnPoint(new Vector3(-394.728851f, 2949.83569f, 18.312603f),4.78518f),
new SpawnPoint(new Vector3(-401.366241f, 2954.15015f, 15.2452316f),116.4434f),
new SpawnPoint(new Vector3(-415.041321f, 2930.43335f, 15.6349268f),148.2523f),
new SpawnPoint(new Vector3(-431.8371f, 2927.01587f, 14.027339f),98.9588f),
new SpawnPoint(new Vector3(-437.465363f, 2933.19922f, 13.6917667f),52.39057f),
new SpawnPoint(new Vector3(-454.176422f, 2924.671f, 13.830677f),133.180679f),
new SpawnPoint(new Vector3(-468.414642f, 2922.148f, 14.4635563f),90.77882f),
new SpawnPoint(new Vector3(-480.1407f, 2921.3103f, 14.7380409f),120.011482f),
new SpawnPoint(new Vector3(-496.00354f, 2915.64429f, 13.4602194f),45.6842537f),
new SpawnPoint(new Vector3(-511.7159f, 2894.446f, 13.5285568f),184.83107f),
new SpawnPoint(new Vector3(-514.0886f, 2879.09058f, 15.25943f),148.991669f),
new SpawnPoint(new Vector3(-518.9137f, 2871.62939f, 17.19218f),18.20025f),
new SpawnPoint(new Vector3(-525.826843f, 2880.156f, 19.5094986f),41.1475143f),
new SpawnPoint(new Vector3(-532.3786f, 2891.89551f, 20.29961f),28.5550423f),
new SpawnPoint(new Vector3(-544.7468f, 2897.035f, 19.8653164f),120.398376f),
new SpawnPoint(new Vector3(-555.927f, 2896.93335f, 20.11632f),43.8631935f),
new SpawnPoint(new Vector3(-642.468567f, 3453.88379f, 207.838074f),237.18808f),
new SpawnPoint(new Vector3(-621.345154f, 3435.22681f, 200.079254f),231.156418f),
new SpawnPoint(new Vector3(-602.604248f, 3445.76318f, 194.542191f),310.7384f),
new SpawnPoint(new Vector3(-588.8923f, 3439.987f, 176.8182f),200.518036f),
new SpawnPoint(new Vector3(-589.628f, 3432.15527f, 172.497f),241.381546f),
new SpawnPoint(new Vector3(-589.1691f, 3432.03613f, 172.698242f),174.591492f),
new SpawnPoint(new Vector3(-583.5788f, 3440.70532f, 179.0148f),322.6121f),
new SpawnPoint(new Vector3(-572.018066f, 3461.129f, 192.28978f),352.748077f),
new SpawnPoint(new Vector3(-561.9236f, 3479.53223f, 204.89566f),302.830048f),
new SpawnPoint(new Vector3(-542.482849f, 3474.66675f, 208.597015f),253.435165f),
new SpawnPoint(new Vector3(-516.793335f, 3465.50415f, 214.0837f),252.797989f),
new SpawnPoint(new Vector3(-511.030945f, 3471.045f, 217.535278f),339.117279f),
new SpawnPoint(new Vector3(-503.429474f, 3472.40723f, 211.963547f),54.1530075f),
new SpawnPoint(new Vector3(-502.962372f, 3472.66748f, 212.144913f),290.636841f),
new SpawnPoint(new Vector3(-489.9647f, 3476.91187f, 208.861145f),287.953766f),
new SpawnPoint(new Vector3(-476.8451f, 3481.8855f, 210.068527f),293.5161f),
new SpawnPoint(new Vector3(-458.2474f, 3484.91919f, 216.878189f),247.236511f),
new SpawnPoint(new Vector3(-453.949463f, 3471.95337f, 212.396683f),174.277954f),
new SpawnPoint(new Vector3(-452.274719f, 3451.70313f, 203.409317f),194.92955f),
new SpawnPoint(new Vector3(-438.783569f, 3422.7168f, 179.165665f),189.171387f),
new SpawnPoint(new Vector3(-426.292969f, 3414.1062f, 172.483414f),242.3406f),
new SpawnPoint(new Vector3(-417.3648f, 3409.16748f, 171.178879f),202.796524f),
new SpawnPoint(new Vector3(-417.242f, 3399.21362f, 170.1191f),156.157059f),
new SpawnPoint(new Vector3(-417.606537f, 3384.06323f, 167.847656f),188.903641f),
new SpawnPoint(new Vector3(-421.001221f, 3369.04956f, 161.9271f),168.218582f),
new SpawnPoint(new Vector3(-417.858765f, 3364.47461f, 162.177979f),241.3837f),
new SpawnPoint(new Vector3(-410.787354f, 3364.1272f, 159.508591f),270.79303f),
new SpawnPoint(new Vector3(-372.840729f, 3369.49854f, 151.503f),283.715271f),
new SpawnPoint(new Vector3(-345.158783f, 3393.82861f, 147.689346f),317.274841f),
new SpawnPoint(new Vector3(-314.168518f, 3421.48022f, 146.989334f),253.711029f),
new SpawnPoint(new Vector3(-268.225159f, 3381.28687f, 145.264618f),321.5798f),
new SpawnPoint(new Vector3(-267.601929f, 3391.12451f, 144.65062f),355.5782f),
new SpawnPoint(new Vector3(-244.144348f, 3405.90869f, 124.0421f),272.886719f),
new SpawnPoint(new Vector3(-233.28595f, 3406.97437f, 121.089264f),282.059082f),
new SpawnPoint(new Vector3(-236.726746f, 3417.68481f, 123.709244f),3.43641043f),
new SpawnPoint(new Vector3(-233.687576f, 3423.21582f, 124.325043f),307.3181f),
new SpawnPoint(new Vector3(-240.64241f, 3424.349f, 124.495384f),110.10051f),
new SpawnPoint(new Vector3(-258.703369f, 3444.57227f, 115.78653f),33.22559f),
new SpawnPoint(new Vector3(-280.7979f, 3482.84521f, 98.87371f),13.2386847f),
new SpawnPoint(new Vector3(-295.540558f, 3507.55371f, 89.7753f),107.718506f),
new SpawnPoint(new Vector3(-314.053955f, 3509.637f, 94.98841f),64.18706f),
new SpawnPoint(new Vector3(-322.194946f, 3522.95874f, 90.09423f),2.59515834f),
new SpawnPoint(new Vector3(-317.533051f, 3537.66138f, 87.5001755f),338.8628f),
new SpawnPoint(new Vector3(-307.741852f, 3553.67773f, 82.29303f),257.932739f),
new SpawnPoint(new Vector3(-299.8414f, 3547.189f, 75.6694946f),231.04892f),
new SpawnPoint(new Vector3(-287.022583f, 3536.95215f, 75.09053f),231.3555f),
new SpawnPoint(new Vector3(-273.4417f, 3529.6228f, 74.0272f),266.3331f),
new SpawnPoint(new Vector3(-267.234467f, 3536.997f, 69.64376f),15.8026581f),
new SpawnPoint(new Vector3(-267.093567f, 3551.91968f, 64.47165f),347.5948f),
new SpawnPoint(new Vector3(-268.15863f, 3560.91528f, 63.6187439f),14.7303247f),
new SpawnPoint(new Vector3(-253.387421f, 3602.31763f, 59.8356628f),6.72484636f),
new SpawnPoint(new Vector3(-246.11882f, 3605.88965f, 60.87132f),215.679916f),
new SpawnPoint(new Vector3(-219.172577f, 3589.90112f, 59.34888f),255.905228f),
new SpawnPoint(new Vector3(-207.518082f, 3594.834f, 55.69935f),305.659515f),
new SpawnPoint(new Vector3(-203.19548f, 3603.52637f, 53.4663429f),344.0268f),
new SpawnPoint(new Vector3(-173.500214f, 3632.98218f, 45.4234123f),324.75528f),
new SpawnPoint(new Vector3(-166.644318f, 3636.4397f, 43.13353f),291.61615f),
new SpawnPoint(new Vector3(-152.900421f, 3644.46582f, 40.70916f),324.940918f),
new SpawnPoint(new Vector3(-151.0763f, 3653.91724f, 38.7352524f),344.311432f),
new SpawnPoint(new Vector3(-143.2434f, 3657.91553f, 37.0733147f),289.966034f),
new SpawnPoint(new Vector3(-128.997482f, 3675.846f, 32.8303452f),327.298126f),
new SpawnPoint(new Vector3(-125.053062f, 3696.547f, 30.858984f),358.983276f),
new SpawnPoint(new Vector3(-115.113846f, 3743.5625f, 31.4177017f),333.483154f),
new SpawnPoint(new Vector3(-126.3833f, 3772.26831f, 33.1225f),30.476265f),
new SpawnPoint(new Vector3(-127.383995f, 3784.533f, 32.9981079f),6.861243f),
new SpawnPoint(new Vector3(-134.765747f, 3789.07178f, 34.65098f),73.1913f),
new SpawnPoint(new Vector3(-145.509613f, 3790.53857f, 36.05727f),109.002083f),
new SpawnPoint(new Vector3(-152.073746f, 3786.08325f, 37.59525f),101.885315f),
new SpawnPoint(new Vector3(-165.416245f, 3788.673f, 37.8956871f),47.1043358f),
new SpawnPoint(new Vector3(-171.740311f, 3804.56226f, 36.7571945f),354.91626f),
new SpawnPoint(new Vector3(-164.683578f, 3812.303f, 34.2977257f),298.017975f),
new SpawnPoint(new Vector3(-150.55275f, 3816.21533f, 33.0185661f),304.804047f),
new SpawnPoint(new Vector3(-131.125443f, 3831.03955f, 31.412117f),320.8016f),
new SpawnPoint(new Vector3(-132.796661f, 3842.73438f, 30.7300568f),29.6775188f),
new SpawnPoint(new Vector3(-141.846039f, 3848.49487f, 30.4705811f),91.2227249f),
new SpawnPoint(new Vector3(-154.598145f, 3845.52759f, 31.08433f),106.95578f),
new SpawnPoint(new Vector3(-190.278763f, 3844.52637f, 31.2321854f),86.48433f),
new SpawnPoint(new Vector3(-209.5667f, 3851.23828f, 30.5556526f),45.6302719f),
new SpawnPoint(new Vector3(-219.7724f, 3859.04639f, 31.1581554f),52.9693069f),
new SpawnPoint(new Vector3(-227.997208f, 3857.19336f, 31.5319424f),154.985f),
new SpawnPoint(new Vector3(-241.675262f, 3859.5083f, 33.2844162f),57.7110863f),
new SpawnPoint(new Vector3(-249.380142f, 3895.309f, 41.2088432f),75.60024f),
new SpawnPoint(new Vector3(-285.890228f, 3901.53174f, 48.4163628f),75.16809f),
new SpawnPoint(new Vector3(-307.061f, 3904.4895f, 51.77426f),96.14505f),
new SpawnPoint(new Vector3(-336.970245f, 3884.3064f, 53.0183258f),134.1635f),
new SpawnPoint(new Vector3(-352.384369f, 3873.47144f, 57.52664f),131.340424f),
new SpawnPoint(new Vector3(-358.539825f, 3857.58838f, 53.8652153f),170.677734f),
new SpawnPoint(new Vector3(-372.025055f, 3853.54028f, 55.21439f),92.87327f),
new SpawnPoint(new Vector3(-376.772644f, 3836.61963f, 55.63738f),163.08165f),
new SpawnPoint(new Vector3(-395.289459f, 3827.91772f, 58.13859f),115.144363f),
new SpawnPoint(new Vector3(-398.597473f, 3824.11084f, 59.00021f),174.395477f),
new SpawnPoint(new Vector3(-402.2585f, 3819.07129f, 60.62358f),134.697281f),
new SpawnPoint(new Vector3(-401.822845f, 3805.77124f, 63.3032074f),201.340912f),
new SpawnPoint(new Vector3(-388.305756f, 3791.33618f, 65.41766f),216.356873f),
new SpawnPoint(new Vector3(-387.574677f, 3782.21069f, 71.05326f),184.298752f),
new SpawnPoint(new Vector3(-390.23114f, 3759.6936f, 84.325264f),216.541946f),
new SpawnPoint(new Vector3(-386.258881f, 3753.67065f, 87.12237f),205.814575f),
new SpawnPoint(new Vector3(-382.526947f, 3745.978f, 89.1034241f),204.3976f),
new SpawnPoint(new Vector3(-384.299377f, 3739.33f, 92.50489f),159.110229f),
new SpawnPoint(new Vector3(-387.960754f, 3732.56714f, 96.51551f),119.876236f),
new SpawnPoint(new Vector3(-401.3038f, 3738.6582f, 91.61274f),39.023777f),
new SpawnPoint(new Vector3(-406.0021f, 3744.465f, 92.1655f),44.66226f),
new SpawnPoint(new Vector3(-409.383118f, 3749.933f, 93.11116f),31.6284389f),
new SpawnPoint(new Vector3(-415.426666f, 3755.20264f, 94.52221f),96.50382f),
new SpawnPoint(new Vector3(-421.4394f, 3755.93652f, 98.85743f),76.38492f),
new SpawnPoint(new Vector3(-432.2365f, 3768.4165f, 101.707481f),29.14645f),
new SpawnPoint(new Vector3(-422.132629f, 3786.35522f, 87.28529f),351.435364f),
new SpawnPoint(new Vector3(-420.360657f, 3792.3562f, 86.03187f),7.167991f),
new SpawnPoint(new Vector3(-421.5363f, 3797.47363f, 81.07018f),355.568939f),
new SpawnPoint(new Vector3(-422.302277f, 3805.35254f, 77.01251f),26.9097366f),
new SpawnPoint(new Vector3(-434.935181f, 3823.8894f, 70.409256f),34.804985f),
new SpawnPoint(new Vector3(1374.6781f, 4701.481f, 124.648262f),241.980469f),
new SpawnPoint(new Vector3(1392.26672f, 4701.396f, 118.377083f),266.640533f),
new SpawnPoint(new Vector3(1402.89319f, 4708.61133f, 120.123985f),306.746643f),
new SpawnPoint(new Vector3(1413.06519f, 4711.157f, 118.472153f),235.837341f),
new SpawnPoint(new Vector3(1419.047f, 4717.83643f, 118.673019f),341.5425f),
new SpawnPoint(new Vector3(1425.616f, 4725.233f, 123.687027f),292.5207f),
new SpawnPoint(new Vector3(1433.55139f, 4716.907f, 124.89547f),209.33989f),
new SpawnPoint(new Vector3(1440.589f, 4710.827f, 121.514961f),228.045776f),
new SpawnPoint(new Vector3(1450.59509f, 4698.37158f, 109.989609f),220.442413f),
new SpawnPoint(new Vector3(1455.63416f, 4692.617f, 107.5687f),227.670319f),
new SpawnPoint(new Vector3(1467.78394f, 4685.131f, 103.556511f),207.28064f),
new SpawnPoint(new Vector3(1474.68555f, 4669.72852f, 92.93727f),170.155746f),
new SpawnPoint(new Vector3(1467.04639f, 4655.80225f, 82.00537f),151.753052f),
new SpawnPoint(new Vector3(1452.08362f, 4638.246f, 70.3072739f),123.075317f),
new SpawnPoint(new Vector3(1446.16479f, 4633.92f, 63.9580879f),130.619919f),
new SpawnPoint(new Vector3(1447.44275f, 4627.146f, 61.81901f),234.561356f),
new SpawnPoint(new Vector3(1451.124f, 4619.19531f, 60.954525f),193.564117f),
new SpawnPoint(new Vector3(1454.52332f, 4610.64648f, 59.97634f),230.807373f),
new SpawnPoint(new Vector3(1459.78577f, 4604.27539f, 57.97716f),214.661545f),
new SpawnPoint(new Vector3(1477.85522f, 4593.96533f, 54.1076126f),241.382172f),
new SpawnPoint(new Vector3(1486.55823f, 4590.791f, 53.1784439f),255.477539f),
new SpawnPoint(new Vector3(1487.12122f, 4584.62f, 53.44206f),155.500351f),
new SpawnPoint(new Vector3(1486.78186f, 4574.04f, 55.7314453f),200.361084f),
new SpawnPoint(new Vector3(1488.84863f, 4561.50342f, 58.79775f),176.984253f),
new SpawnPoint(new Vector3(1445.0647f, 4519.977f, 54.07442f),146.656311f),
new SpawnPoint(new Vector3(1438.99133f, 4514.89648f, 54.4524841f),102.876854f),
new SpawnPoint(new Vector3(1428.152f, 4511.796f, 54.7238731f),103.150826f),
new SpawnPoint(new Vector3(1417.63916f, 4510.82666f, 55.1515f),88.88424f),
new SpawnPoint(new Vector3(1410.293f, 4514.04541f, 54.0959854f),76.6586151f),
new SpawnPoint(new Vector3(1392.7113f, 4523.15967f, 54.1045952f),95.3556747f),
new SpawnPoint(new Vector3(1385.39587f, 4523.69043f, 55.2922974f),77.95797f),
new SpawnPoint(new Vector3(1381.43591f, 4528.40771f, 58.75608f),2.89429736f),
new SpawnPoint(new Vector3(1384.93152f, 4538.095f, 63.0703f),38.1576042f),
new SpawnPoint(new Vector3(1381.97144f, 4542.503f, 66.13461f),355.993042f),
new SpawnPoint(new Vector3(1388.75842f, 4552.32275f, 68.99422f),278.032745f),
new SpawnPoint(new Vector3(1392.62634f, 4558.97949f, 69.42937f),336.2688f),
new SpawnPoint(new Vector3(1385.8136f, 4573.898f, 71.83409f),341.6485f),
new SpawnPoint(new Vector3(1381.958f, 4578.07373f, 73.75159f),80.57712f),
new SpawnPoint(new Vector3(1376.48669f, 4578.931f, 76.12915f),111.584213f),
new SpawnPoint(new Vector3(1372.75549f, 4575.076f, 78.3012543f),127.065269f),
new SpawnPoint(new Vector3(1363.83752f, 4571.753f, 81.2120056f),112.895195f),
new SpawnPoint(new Vector3(1357.61487f, 4569.37158f, 84.66782f),97.30471f),
new SpawnPoint(new Vector3(1343.61707f, 4562.369f, 90.7103653f),82.34782f),
new SpawnPoint(new Vector3(1351.13049f, 4578.97852f, 90.31639f),352.76947f),
new SpawnPoint(new Vector3(1334.47192f, 4575.77441f, 103.317757f),129.5876f),
new SpawnPoint(new Vector3(1326.62451f, 4571.48242f, 109.954636f),43.6460838f),
new SpawnPoint(new Vector3(1321.58484f, 4572.75439f, 114.275719f),98.62258f),
new SpawnPoint(new Vector3(1312.57935f, 4560.616f, 111.753181f),147.661621f),
new SpawnPoint(new Vector3(1297.64026f, 4543.86475f, 94.0127f),160.685944f),
new SpawnPoint(new Vector3(1299.9917f, 4515.105f, 81.78355f),189.0864f),
new SpawnPoint(new Vector3(1282.02051f, 4502.19531f, 68.8728f),95.58881f),
new SpawnPoint(new Vector3(1247.57751f, 4503.12354f, 58.5159035f),113.843781f),
new SpawnPoint(new Vector3(1234.89624f, 4494.58936f, 50.1216049f),92.9516f),
new SpawnPoint(new Vector3(1220.56921f, 4506.37744f, 49.99072f),39.6998825f),
new SpawnPoint(new Vector3(1208.35767f, 4516.854f, 52.3566628f),41.0177231f),
new SpawnPoint(new Vector3(1197.35156f, 4519.99658f, 55.6875229f),142.44632f),
new SpawnPoint(new Vector3(1194.37024f, 4509.932f, 58.6981468f),190.790848f),
new SpawnPoint(new Vector3(1194.16663f, 4499.442f, 59.8797035f),176.960327f),
new SpawnPoint(new Vector3(1189.99243f, 4486.72559f, 61.9039f),163.659958f),
new SpawnPoint(new Vector3(1192.94312f, 4480.107f, 62.57475f),197.598831f),
new SpawnPoint(new Vector3(1193.01709f, 4471.432f, 63.23671f),151.767273f),
new SpawnPoint(new Vector3(1186.95154f, 4465.297f, 64.65295f),126.152931f),
new SpawnPoint(new Vector3(1178.163f, 4460.85645f, 64.7830048f),126.987991f),
new SpawnPoint(new Vector3(3187.446f, 4650.855f, 179.334778f),326.6985f),
new SpawnPoint(new Vector3(3200.89429f, 4656.74951f, 181.964172f),251.87355f),
new SpawnPoint(new Vector3(3230.787f, 4654.595f, 172.208435f),294.6098f),
new SpawnPoint(new Vector3(3242.25122f, 4681.60742f, 169.584412f),1.49516118f),
new SpawnPoint(new Vector3(3244.48657f, 4716.42969f, 176.212189f),350.641785f),
new SpawnPoint(new Vector3(3248.29f, 4740.23145f, 180.6284f),354.191742f),
new SpawnPoint(new Vector3(3255.139f, 4753.292f, 175.8871f),325.115784f),
new SpawnPoint(new Vector3(3259.293f, 4770.79053f, 165.6605f),12.3417778f),
new SpawnPoint(new Vector3(3260.1416f, 4781.98975f, 157.899323f),304.7453f),
new SpawnPoint(new Vector3(3271.04517f, 4783.49658f, 152.20079f),269.319763f),
new SpawnPoint(new Vector3(3298.67529f, 4782.74072f, 147.029373f),268.45163f),
new SpawnPoint(new Vector3(3309.4812f, 4785.50537f, 143.736374f),287.813019f),
new SpawnPoint(new Vector3(3321.3f, 4788.295f, 136.064377f),275.084229f),
new SpawnPoint(new Vector3(3327.88574f, 4786.301f, 132.122208f),250.848541f),
new SpawnPoint(new Vector3(3342.481f, 4781.7627f, 123.787537f),252.161774f),
new SpawnPoint(new Vector3(3370.462f, 4772.82f, 116.408852f),252.272675f),
new SpawnPoint(new Vector3(3394.37183f, 4764.34766f, 89.97465f),145.091461f),
new SpawnPoint(new Vector3(3409.03442f, 4767.30469f, 80.47177f),286.541138f),
new SpawnPoint(new Vector3(3413.23267f, 4784.22168f, 76.36381f),354.629028f),
new SpawnPoint(new Vector3(3415.35f, 4801.87158f, 71.22338f),338.283447f),
new SpawnPoint(new Vector3(3433.55249f, 4810.458f, 52.62937f),62.3511124f),
new SpawnPoint(new Vector3(3447.6582f, 4816.29248f, 34.6532249f),52.50591f),
new SpawnPoint(new Vector3(3468.645f, 4835.085f, 34.61406f),349.265045f),
new SpawnPoint(new Vector3(3461.46045f, 4851.636f, 34.8662643f),42.52468f),
new SpawnPoint(new Vector3(3444.422f, 4874.623f, 34.6482735f),30.085062f),
new SpawnPoint(new Vector3(3443.62061f, 4899.91748f, 29.1421661f),12.2082567f),
new SpawnPoint(new Vector3(3435.62549f, 4906.936f, 27.1942635f),48.25748f),
new SpawnPoint(new Vector3(3435.52954f, 4915.27637f, 23.54561f),339.169464f),
new SpawnPoint(new Vector3(3442.6582f, 4923.95459f, 18.5921383f),303.1459f),
new SpawnPoint(new Vector3(3452.90576f, 4926.80225f, 15.0557232f),275.085419f),
new SpawnPoint(new Vector3(3466.26147f, 4936.017f, 6.15421f),0.00448369933f),
new SpawnPoint(new Vector3(3457.42017f, 4943.44043f, 5.329988f),55.1949959f),
new SpawnPoint(new Vector3(3454.03979f, 4949.363f, 4.630011f),355.690247f),
new SpawnPoint(new Vector3(3461.63159f, 4954.821f, 1.28506148f),304.105957f),
new SpawnPoint(new Vector3(3468.32886f, 4952.712f, 1.74644113f),244.034225f),
new SpawnPoint(new Vector3(3474.55151f, 4945.59326f, 1.91782045f),173.513733f),
new SpawnPoint(new Vector3(3479.26f, 4937.5293f, 2.39952731f),254.0184f),
new SpawnPoint(new Vector3(3449.072f, 4927.29541f, 16.1406479f),73.91025f),
new SpawnPoint(new Vector3(3431.67969f, 4933.694f, 23.0522156f),53.30111f),
new SpawnPoint(new Vector3(3425.50171f, 4954.864f, 25.2209435f),9.144657f),
new SpawnPoint(new Vector3(3421.06055f, 4986.621f, 27.1884842f),3.23262787f),
new SpawnPoint(new Vector3(3408.546f, 4996.02441f, 30.16825f),100.08548f),
new SpawnPoint(new Vector3(3392.21362f, 5000.28125f, 30.9726334f),62.3634758f),
new SpawnPoint(new Vector3(3386.18652f, 5009.8584f, 28.4457817f),32.4571f),
new SpawnPoint(new Vector3(3361.90063f, 5013.40527f, 20.5787926f),98.06735f),
new SpawnPoint(new Vector3(3354.25317f, 5007.03125f, 22.0222034f),150.400574f),
new SpawnPoint(new Vector3(3350.912f, 4997.00537f, 23.7876167f),181.593f),
new SpawnPoint(new Vector3(3350.19751f, 4989.279f, 24.7849121f),173.25502f),
new SpawnPoint(new Vector3(3343.26758f, 4984.61328f, 26.9882717f),74.9893341f),
new SpawnPoint(new Vector3(3326.243f, 5006.316f, 23.7591553f),22.4376812f),
new SpawnPoint(new Vector3(3327.28931f, 5019.79834f, 21.2851868f),304.677063f),
new SpawnPoint(new Vector3(3320.227f, 5023.98f, 23.5111828f),105.543f),
new SpawnPoint(new Vector3(3296.16553f, 5022.05127f, 23.38557f),79.5456161f),
new SpawnPoint(new Vector3(3285.62036f, 5048.797f, 22.81722f),359.8523f),
new SpawnPoint(new Vector3(3260.15063f, 5049.577f, 21.6551571f),102.2364f),
new SpawnPoint(new Vector3(3253.39844f, 5042.528f, 21.43037f),141.393875f),
new SpawnPoint(new Vector3(3242.621f, 5038.586f, 21.0583f),92.9542542f),
new SpawnPoint(new Vector3(3220.75781f, 4998.388f, 22.7648087f),172.004974f),
new SpawnPoint(new Vector3(3199.71973f, 4993.94629f, 26.6625576f),90.38206f),
new SpawnPoint(new Vector3(3198.66f, 4982.364f, 31.69542f),245.847473f),
new SpawnPoint(new Vector3(3201.15674f, 4976.20557f, 35.0466576f),151.647171f),
new SpawnPoint(new Vector3(3202.49048f, 4967.25537f, 39.5689621f),216.546616f),
new SpawnPoint(new Vector3(3203.97632f, 4958.842f, 43.7163353f),179.090088f),
new SpawnPoint(new Vector3(3199.94f, 4941.299f, 54.6820564f),120.391151f),
new SpawnPoint(new Vector3(3192.21069f, 4936.158f, 61.47784f),123.711945f),
new SpawnPoint(new Vector3(3190.318f, 4920.617f, 74.53591f),203.046967f),
new SpawnPoint(new Vector3(3189.71313f, 4915.14746f, 78.31581f),133.27858f),
new SpawnPoint(new Vector3(3184.172f, 4907.916f, 83.85929f),141.106842f),
new SpawnPoint(new Vector3(3182.47754f, 4900.556f, 88.05094f),142.867722f),
new SpawnPoint(new Vector3(3174.39966f, 4895.34668f, 93.5040741f),98.20817f),
new SpawnPoint(new Vector3(3184.74438f, 4886.88867f, 99.0454254f),231.77507f),
new SpawnPoint(new Vector3(3195.401f, 4881.845f, 106.1709f),244.7305f),
new SpawnPoint(new Vector3(3204.83521f, 4852.19873f, 122.323135f),221.36525f),
new SpawnPoint(new Vector3(3213.15845f, 4840.998f, 130.457214f),145.5882f),
new SpawnPoint(new Vector3(3197.382f, 4841.998f, 124.8184f),88.23837f),
new SpawnPoint(new Vector3(3178.63477f, 4842.22559f, 124.782349f),89.24708f),
new SpawnPoint(new Vector3(3155.801f, 4823.92334f, 134.207214f),143.37088f),
new SpawnPoint(new Vector3(3140.28784f, 4820.69043f, 136.959686f),32.4896736f),
new SpawnPoint(new Vector3(3137.67163f, 4806.61768f, 140.408264f),199.447113f),
new SpawnPoint(new Vector3(3113.322f, 4801.63574f, 153.893631f),69.99199f),
new SpawnPoint(new Vector3(3102.8186f, 4827.734f, 152.9416f),352.176117f),
new SpawnPoint(new Vector3(3083.63184f, 4818.93f, 154.943008f),174.463287f),
new SpawnPoint(new Vector3(3123.65479f, 4775.351f, 155.434372f),236.059418f),
new SpawnPoint(new Vector3(3130.53174f, 4763.146f, 155.237076f),175.830338f),
new SpawnPoint(new Vector3(3130.25171f, 4745.274f, 158.046555f),189.788315f),
new SpawnPoint(new Vector3(3124.01831f, 4734.293f, 154.485809f),134.5958f),
new SpawnPoint(new Vector3(3117.78f, 4723.109f, 149.787018f),171.228134f),
new SpawnPoint(new Vector3(3116.52148f, 4713.231f, 145.5718f),180.861038f),
new SpawnPoint(new Vector3(3110.75537f, 4700.528f, 135.593735f),134.482f),
new SpawnPoint(new Vector3(3099.887f, 4691.13428f, 130.1633f),121.927521f),
new SpawnPoint(new Vector3(3097.14844f, 4686.16455f, 126.823982f),164.228821f),
new SpawnPoint(new Vector3(3092.759f, 4681.7207f, 123.058678f),111.306549f),
new SpawnPoint(new Vector3(3088.68726f, 4675.67f, 118.247475f),200.291718f),
new SpawnPoint(new Vector3(3098.42773f, 4670.11572f, 117.013634f),243.142639f),
new SpawnPoint(new Vector3(3113.84961f, 4658.095f, 119.053261f),211.897369f),
new SpawnPoint(new Vector3(3115.32153f, 4648.087f, 123.078011f),219.564957f),
new SpawnPoint(new Vector3(3117.7876f, 4646.11572f, 125.336464f),205.5614f),
new SpawnPoint(new Vector3(3118.79f, 4640.634f, 127.181549f),196.013977f),
new SpawnPoint(new Vector3(3119.86719f, 4636.87061f, 128.31485f),195.945221f),
new SpawnPoint(new Vector3(3121.232f, 4631.99561f, 129.519241f),195.687119f),
new SpawnPoint(new Vector3(3117.21948f, 4625.06152f, 126.607018f),145.841476f),
new SpawnPoint(new Vector3(3106.97241f, 4619.459f, 118.003906f),120.64164f),
new SpawnPoint(new Vector3(3099.55371f, 4620.259f, 112.335785f),82.0262f),
new SpawnPoint(new Vector3(3087.48853f, 4621.845f, 102.840446f),85.83012f),
new SpawnPoint(new Vector3(3077.88086f, 4608.1875f, 94.74538f),161.479767f),
new SpawnPoint(new Vector3(3066.17383f, 4597.81836f, 77.87457f),200.167465f),
new SpawnPoint(new Vector3(3058.33569f, 4590.38037f, 65.90699f),65.4158f),
new SpawnPoint(new Vector3(3056.08667f, 4580.51758f, 61.27831f),233.366852f),
new SpawnPoint(new Vector3(3054.12842f, 4570.929f, 59.1290245f),125.032578f),
new SpawnPoint(new Vector3(3037.58569f, 4569.80225f, 55.99042f),87.97818f),
new SpawnPoint(new Vector3(3032.51514f, 4573.97852f, 54.81991f),47.7586136f),
new SpawnPoint(new Vector3(3028.188f, 4579.72754f, 52.551075f),355.0632f),
new SpawnPoint(new Vector3(3032.90332f, 4589.699f, 53.11976f),318.884369f),
new SpawnPoint(new Vector3(3033.972f, 4594.59473f, 54.76556f),35.14967f),
new SpawnPoint(new Vector3(3026.86426f, 4601.15967f, 57.6077576f),65.63058f),
new SpawnPoint(new Vector3(3009.07373f, 4605.77539f, 57.3803444f),52.06647f),
#endregion
        };
    }
}
