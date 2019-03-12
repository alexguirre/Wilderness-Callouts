//namespace WildernessCallouts.Callouts
//{
//    using Rage;
//    using Rage.Native;
//    using LSPD_First_Response;
//    using LSPD_First_Response.Mod.API;
//    using LSPD_First_Response.Mod.Callouts;
//    using System;
//    using System.Windows.Forms;
//    using System.Drawing;
//    using System.Collections.Generic;
//    using WildernessCallouts.Dialogues;
//    using WildernessCallouts.Types;

//    [CalloutInfo("SuicideAttempt", CalloutProbability.Low)]
//    internal class SuicideAttempt : CalloutBase
//    {
//        private Ped suicidePed;
//        private Blip suicideBlip;
//        private LHandle pursuit;
//        private Vector3 spawnPointOnStreet;

//        private bool breakForceEnd = false;

//        private PoliceComputer computer = new PoliceComputer();

//        private static string[] weaponsAssets = { "WEAPON_PISTOL", "WEAPON_PISTOL50", "WEAPON_HEAVYPISTOL", "WEAPON_COMBATPISTOL" };

//        private static string[] drunkSetAnimNames = { "move_m@drunk@a", "move_m@drunk@moderatedrunk", "move_m@drunk@moderatedrunk_head_up", "move_m@drunk@slightlydrunk", "move_m@drunk@verydrunk" };
//        private static string[] drunkIdleAnimNames = { "fidget_01", "fidget_02", "fidget_03", "fidget_04", "fidget_05", "fidget_06", "fidget_07", "fidget_08", "fidget_09", "fidget_10" };

//        private ESuicideAttemptState state;

//        private int waitTimeBetweenDialogue = 50;

//        private bool isSuicideByJump = false;

//        private SpawnPoint positionForJump;

//        public override bool OnBeforeCalloutDisplayed()
//        {
//            int randJumpPos = Globals.Random.Next(1, 3);

//            if (randJumpPos == 1)
//            {
//                float f = 400f;
//                for (int i = 0; i < 20; i++)
//                {
//                    GameFiber.Yield();

//                    spawnPointOnStreet = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.AroundPosition(f + 200f)).GetSafeCoordinatesForPed();

//                    if (spawnPointOnStreet != Vector3.Zero)
//                    {
//                        break;
//                    }
//                }
//                if (spawnPointOnStreet == Vector3.Zero) spawnPointOnStreet = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.AroundPosition(500f));

//                isSuicideByJump = false;
//            }
//            else if (randJumpPos == 2)
//            {
//                positionForJump = JumpSpawns.GetRandomElement();

//                for (int i = 0; i < 15; i++)
//                {
//                    if (Vector3.Distance(Game.LocalPlayer.Character.Position, positionForJump.Position) < 850.0f) break;
//                    positionForJump = JumpSpawns.GetRandomElement();
//                }

//                spawnPointOnStreet = positionForJump.Position;

//                if (Vector3.Distance(Game.LocalPlayer.Character.Position, positionForJump.Position) > 1000.0f) return false;

//                isSuicideByJump = true;
//            }

//            if (Vector3.Distance(Game.LocalPlayer.Character.Position, spawnPointOnStreet) < 30.0f) return false;

//            suicidePed = new Ped(spawnPointOnStreet);

//            if (!suicidePed.Exists()) return false;

//            if (suicidePed.Exists()) suicidePed.BlockPermanentEvents = true;

//            if (isSuicideByJump) suicidePed.Heading = positionForJump.Heading;

//            LSPD_First_Response.Mod.API.Functions.PlayScannerAudioUsingPosition("ATTENTION_ALL_UNITS CRIME_SUICIDE_ATTEMPT IN_OR_ON_POSITION UNITS_RESPOND_CODE_02", spawnPointOnStreet);

//            if (suicidePed.Exists()) suicidePed.RelationshipGroup = new RelationshipGroup("SUICIDE");




//            if (suicidePed.Exists()) suicidePed.SetAnimationSet(drunkSetAnimNames.GetRandomElement(true));
//            if (suicidePed.Exists()) suicidePed.Tasks.PlayAnimation("move_m@drunk@verydrunk_idles@", drunkIdleAnimNames.GetRandomElement(true), 4.0f, AnimationFlags.Loop);


//            this.ShowCalloutAreaBlipBeforeAccepting(spawnPointOnStreet, 45f);
//            this.AddMinimumDistanceCheck(20f, spawnPointOnStreet);

//            // Set up our callout message and location
//            this.CalloutMessage = "Suicide attempt";
//            this.CalloutPosition = spawnPointOnStreet;

//            return base.OnBeforeCalloutDisplayed();
//        }

//        public override bool OnCalloutAccepted()
//        {
//            Logger.LogTrivial(this.GetType().Name, "SuicideIsJumpAttempt: " + isSuicideByJump);

//            suicideBlip = new Blip(suicidePed);
//            suicideBlip.Sprite = BlipSprite.Snitch;
//            suicideBlip.Color = Color.Yellow;
//            suicideBlip.EnableRoute(Color.Yellow);

//            state = ESuicideAttemptState.EnRoute;

//            if (isSuicideByJump) Game.DisplayNotification("~b~Dispatch: ~w~Witnesses report suicide person is about to jump from a high zone, find a way up");

//            return base.OnCalloutAccepted();
//        }

//        public override void OnCalloutNotAccepted()
//        {
//            if (suicideBlip.Exists()) suicideBlip.Delete();
//            if (suicidePed.Exists()) suicidePed.Delete();

//            breakForceEnd = true;

//            base.OnCalloutNotAccepted();
//        }

//        public override void Process()
//        {
//            if (state == ESuicideAttemptState.EnRoute && Vector3.Distance(Game.LocalPlayer.Character.Position, suicidePed.Position) < 15.0f)
//                state = ESuicideAttemptState.OnScene;

//            if (state == ESuicideAttemptState.OnScene)
//            {
//                Game.DisplayHelp("Press ~b~" + Controls.PrimaryAction.ToUserFriendlyName() + " ~w~to talk");
//                if (Game.IsKeyDown(Settings.ActionKey1))
//                    CreateDecision();
//            }

//            base.Process();
//        }

//        public override void End()
//        {
//            if (suicideBlip.Exists()) suicideBlip.Delete();
//            if (suicidePed.Exists()) suicidePed.Dismiss();

//            breakForceEnd = true;

//            base.End();
//        }

//        public void CreateDecision()
//        {
//            int randDecision = Globals.Random.Next(1, 101);

//            state = ESuicideAttemptState.DecisionMade;

//            if (randDecision < 10)  /* ATTACK */
//            {
//                Logger.LogTrivial(this.GetType().Name, "Decision: Attack");

//                string[] dialogue = null;
//                if (suicidePed.IsMale) dialogue = SuicideAttemptDialogues.AttackMale.GetRandomElement();
//                else if (suicidePed.IsFemale) dialogue = SuicideAttemptDialogues.AttackFemale.GetRandomElement();

//                foreach (string str in dialogue)
//                {
//                    GameFiber.Sleep(waitTimeBetweenDialogue);
//                    Game.DisplaySubtitle(str, 4500);
//                    waitTimeBetweenDialogue = 4375;
//                }

//                pursuit = LSPD_First_Response.Mod.API.Functions.CreatePursuit();
//                LSPD_First_Response.Mod.API.Functions.AddPedToPursuit(pursuit, suicidePed);

//                suicidePed.GiveNewWeapon(weaponsAssets.GetRandomElement(), 666, true);

//                Game.SetRelationshipBetweenRelationshipGroups("SUICIDE", "PLAYER", Relationship.Hate);
//                Game.SetRelationshipBetweenRelationshipGroups("SUICIDE", "COP", Relationship.Hate);

//                if (!LSPD_First_Response.Mod.API.Functions.IsPedGettingArrested(suicidePed) && !LSPD_First_Response.Mod.API.Functions.IsPedArrested(suicidePed)) suicidePed.AttackPed(Game.LocalPlayer.Character);

//                while (suicidePed.IsAlive && !Functions.IsPedGettingArrested(suicidePed) && !Functions.IsPedArrested(suicidePed))
//                    GameFiber.Yield();

//                this.End();
//            }
//            else if (randDecision >= 10 && randDecision < 25)   /* SUICIDE BY PLAYER */
//            {
//                Logger.LogTrivial(this.GetType().Name, "Decision: SuicideByPlayer");

//                string[] dialogue = null;
//                if (suicidePed.IsMale) dialogue = SuicideAttemptDialogues.SuicideByCopMale.GetRandomElement();
//                else if (suicidePed.IsFemale) dialogue = SuicideAttemptDialogues.SuicideByCopFemale.GetRandomElement();

//                foreach (string str in dialogue)
//                {
//                    GameFiber.Sleep(waitTimeBetweenDialogue);
//                    Game.DisplaySubtitle(str, 4500);
//                    waitTimeBetweenDialogue = 4375;
//                }

//                suicidePed.Tasks.AchieveHeading(suicidePed.GetHeadingTowards(Game.LocalPlayer.Character));

//                while (!breakForceEnd && suicidePed.IsAlive && !Functions.IsPedGettingArrested(suicidePed) && !Functions.IsPedArrested(suicidePed))
//                    GameFiber.Yield();

//                GameFiber.Sleep(2000);
//                this.End();
//            }
//            else if (randDecision >= 25 && randDecision < 60)   /* SUICIDE */
//            {
//                Logger.LogTrivial(this.GetType().Name, "Decision: Suicide");

//                string[] dialogue = null;
//                if (suicidePed.IsMale) dialogue = SuicideAttemptDialogues.SuicideMale.GetRandomElement();
//                else if (suicidePed.IsFemale) dialogue = SuicideAttemptDialogues.SuicideFemale.GetRandomElement();

//                foreach (string str in dialogue)
//                {
//                    GameFiber.Sleep(waitTimeBetweenDialogue);
//                    Game.DisplaySubtitle(str, 4500);
//                    waitTimeBetweenDialogue = 4375;
//                }

//                if (!isSuicideByJump)
//                {
//                    int suiceMethodRnd = Globals.Random.Next(0, 101);
//                    if (suiceMethodRnd < 35) suicidePed.SuicidePill();
//                    else if (suiceMethodRnd >= 35) suicidePed.SuicideWeapon(weaponsAssets.GetRandomElement());
//                    GameFiber.Sleep(2750);
//                }
//                else if (isSuicideByJump)
//                {
//                    suicidePed.Health = suicidePed.Health - 40;
//                    suicidePed.Tasks.Jump();
//                    GameFiber.Sleep(2750);
//                }

//                this.End();
//            }
//            else if (randDecision >= 60)     /* NO SUICIDE */
//            {
//                Logger.LogTrivial(this.GetType().Name, "Decision: NoSuicide");

//                string[] dialogue = null;
//                if (suicidePed.IsMale) dialogue = SuicideAttemptDialogues.NoSuicideMale.GetRandomElement();
//                else if (suicidePed.IsFemale) dialogue = SuicideAttemptDialogues.NoSuicideFemale.GetRandomElement();

//                foreach (string str in dialogue)
//                {
//                    GameFiber.Sleep(waitTimeBetweenDialogue);
//                    Game.DisplaySubtitle(str, 4500);
//                    waitTimeBetweenDialogue = 4375;
//                }

//                if (!LSPD_First_Response.Mod.API.Functions.IsPedGettingArrested(suicidePed) && !LSPD_First_Response.Mod.API.Functions.IsPedArrested(suicidePed)) suicidePed.Tasks.PutHandsUp(120000, Game.LocalPlayer.Character);

//                GameFiber.Sleep(2500);
//                this.End();
//            }
//        }


//        public enum ESuicideAttemptState
//        {
//            EnRoute,
//            OnScene,
//            DecisionMade,
//        }

//        public static List<SpawnPoint> JumpSpawns = new List<SpawnPoint>()
//        {
//#region JumpSpawns
//new SpawnPoint(new Vector3(281.523743f, -1028.38013f, 62.6035767f),180.000015f),
//new SpawnPoint(new Vector3(258.19342f, -1028.38123f, 62.6031456f),180f),
//new SpawnPoint(new Vector3(250.681534f, -1027.201f, 62.6039467f),120.022385f),
//new SpawnPoint(new Vector3(252.957809f, -1015.56329f, 62.603756f),69.9887161f),
//new SpawnPoint(new Vector3(260.000916f, -996.370239f, 62.6014671f),49.9256935f),
//new SpawnPoint(new Vector3(263.137238f, -987.868164f, 46.18976f),48.68751f),
//new SpawnPoint(new Vector3(267.5691f, -975.517761f, 46.1921768f),69.99907f),
//new SpawnPoint(new Vector3(272.2659f, -964.0206f, 46.1929245f),360f),
//new SpawnPoint(new Vector3(296.811737f, -964.0211f, 46.19306f),360f),
//new SpawnPoint(new Vector3(284.5722f, -986.7584f, 49.5677757f),176.471222f),
//new SpawnPoint(new Vector3(281.1294f, -1012.08423f, 52.05328f),352.588745f),
//new SpawnPoint(new Vector3(326.160156f, -1027.428f, 67.5512543f),210.240524f),
//new SpawnPoint(new Vector3(311.330566f, -1027.37415f, 67.55282f),175.47699f),
//new SpawnPoint(new Vector3(326.5716f, -1006.22577f, 67.55242f),323.526062f),
//new SpawnPoint(new Vector3(334.965576f, -1080.37732f, 61.77119f),269.568848f),
//new SpawnPoint(new Vector3(335.0145f, -1071.74268f, 61.7713928f),314.544647f),
//new SpawnPoint(new Vector3(316.5131f, -1071.62012f, 61.77111f),359.979065f),
//new SpawnPoint(new Vector3(315.8719f, -1072.73889f, 61.7712669f),89.92889f),
//new SpawnPoint(new Vector3(315.852631f, -1084.9635f, 61.7707977f),89.91618f),
//new SpawnPoint(new Vector3(315.829376f, -1099.71655f, 61.77116f),89.9248047f),
//new SpawnPoint(new Vector3(322.7784f, -1101.5791f, 61.7710838f),180.3971f),
//new SpawnPoint(new Vector3(315.8268f, -1101.35291f, 61.7711372f),89.9248352f),
//new SpawnPoint(new Vector3(316.742859f, -1102.29163f, 61.77067f),178.291885f),
//new SpawnPoint(new Vector3(267.514862f, -1120.586f, 83.00964f),182.255f),
//new SpawnPoint(new Vector3(285.49118f, -1120.365f, 83.00966f),180.58252f),
//new SpawnPoint(new Vector3(292.361755f, -1120.41748f, 83.0096359f),253.15918f),
//new SpawnPoint(new Vector3(292.3954f, -1103.40015f, 83.0096359f),269.462433f),
//new SpawnPoint(new Vector3(290.884216f, -1094.24133f, 83.0096359f),271.706726f),
//new SpawnPoint(new Vector3(292.641144f, -1088.10083f, 83.0096741f),241.317291f),
//new SpawnPoint(new Vector3(290.910675f, -1069.13745f, 83.00954f),359.405029f),
//new SpawnPoint(new Vector3(264.198f, -1071.64172f, 83.00935f),90.9712143f),
//new SpawnPoint(new Vector3(149.897339f, -1115.23535f, 46.349f),169.765961f),
//new SpawnPoint(new Vector3(162.821136f, -1118.26477f, 46.3492546f),179.999985f),
//new SpawnPoint(new Vector3(188.921478f, -1108.18335f, 46.3474426f),272.8661f),
//new SpawnPoint(new Vector3(188.59375f, -1102.1908f, 50.33251f),269.9902f),
//new SpawnPoint(new Vector3(188.593658f, -1099.91138f, 50.3323f),269.990173f),
//new SpawnPoint(new Vector3(188.593674f, -1092.7865f, 50.33224f),269.999939f),
//new SpawnPoint(new Vector3(131.189713f, -1087.16711f, 46.35364f),346.7224f),
//new SpawnPoint(new Vector3(127.356346f, -1087.37512f, 46.3557472f),75.659996f),
//new SpawnPoint(new Vector3(151.998f, -1063.40149f, 42.57152f),149.854828f),
//new SpawnPoint(new Vector3(171.34938f, -1062.822f, 72.70806f),160.0745f),
//new SpawnPoint(new Vector3(157.59819f, -1058.14929f, 73.33783f),167.509735f),
//new SpawnPoint(new Vector3(160.574722f, -1041.22925f, 72.7086258f),339.950256f),
//new SpawnPoint(new Vector3(194.138016f, -1054.1167f, 72.70889f),261.6413f),
//new SpawnPoint(new Vector3(188.152237f, -1086.36047f, 69.25286f),219.834137f),
//new SpawnPoint(new Vector3(128.22403f, -1052.07361f, 58.2137451f),165.5767f),
//new SpawnPoint(new Vector3(142.322235f, -1035.08484f, 58.2144775f),338.9472f),
//new SpawnPoint(new Vector3(118.677536f, -1027.15308f, 58.21476f),72.79766f),
//new SpawnPoint(new Vector3(117.763756f, -1048.01685f, 58.2062836f),165.835464f),
//new SpawnPoint(new Vector3(67.77572f, -1038.6123f, 80.73938f),157.974945f),
//new SpawnPoint(new Vector3(67.69117f, -1038.62659f, 80.73938f),249.046234f),
//new SpawnPoint(new Vector3(77.60502f, -1012.31366f, 80.73009f),338.2811f),
//new SpawnPoint(new Vector3(52.4600868f, -1002.30237f, 80.72993f),338.290863f),
//new SpawnPoint(new Vector3(42.6446953f, -1025.30786f, 81.39331f),71.2571945f),
//new SpawnPoint(new Vector3(27.6458454f, -1021.33362f, 84.3106155f),249.649063f),
//new SpawnPoint(new Vector3(13.85497f, -1016.78094f, 84.31065f),159.553482f),
//new SpawnPoint(new Vector3(-5.7816453f, -981.6951f, 84.44696f),339.999268f),
//new SpawnPoint(new Vector3(-21.1173782f, -976.7438f, 84.4467545f),69.99955f),
//new SpawnPoint(new Vector3(-29.5140858f, -1001.41693f, 84.44482f),109.185829f),
//new SpawnPoint(new Vector3(-7.745464f, -1008.84772f, 90.18445f),159.5342f),
//new SpawnPoint(new Vector3(-6.986083f, -1004.59039f, 90.18468f),69.54919f),
//new SpawnPoint(new Vector3(3.97882223f, -1013.22662f, 90.18411f),159.534576f),
//new SpawnPoint(new Vector3(-114.375656f, -1033.11523f, 72.4767761f),282.5763f),
//new SpawnPoint(new Vector3(-108.835518f, -1054.71619f, 74.27214f),204.431641f),
//new SpawnPoint(new Vector3(-107.587189f, -1053.90991f, 74.7943039f),297.903442f),
//new SpawnPoint(new Vector3(-115.927612f, -1034.40735f, 83.39315f),290.007141f),
//new SpawnPoint(new Vector3(-122.65464f, -1096.27417f, 36.13935f),260.325562f),
//new SpawnPoint(new Vector3(-131.147186f, -1092.68372f, 36.1393623f),341.454468f),
//new SpawnPoint(new Vector3(-128.080765f, -1111.02881f, 36.1393623f),250.949158f),
//new SpawnPoint(new Vector3(-124.852242f, -1102.325f, 36.1393623f),243.284836f),
//new SpawnPoint(new Vector3(-188.5179f, -1098.34912f, 42.13923f),73.84277f),
//new SpawnPoint(new Vector3(-176.7524f, -1054.76062f, 42.1392365f),21.5871162f),
//new SpawnPoint(new Vector3(-187.915619f, -1097.08826f, 42.13925f),74.18954f),
//new SpawnPoint(new Vector3(-248.502625f, -1296.42676f, 41.4182472f),88.6702f),
//new SpawnPoint(new Vector3(-210.734283f, -1343.74915f, 43.4275322f),179.5942f),
//new SpawnPoint(new Vector3(-210.466385f, -1310.44653f, 43.42798f),4.47692251f),
//new SpawnPoint(new Vector3(-194.883011f, -1310.7876f, 43.3496323f),350.710938f),
//new SpawnPoint(new Vector3(-458.937622f, -1076.38525f, 35.2966576f),231.098953f),
//new SpawnPoint(new Vector3(-473.2982f, -1069.77734f, 40.9954529f),139.530334f),
//new SpawnPoint(new Vector3(-477.172f, -1064.7937f, 41.0608444f),57.1569366f),
//new SpawnPoint(new Vector3(-509.477844f, -1012.30524f, 41.2726f),104.481163f),
//new SpawnPoint(new Vector3(-508.358521f, -980.455f, 47.0060844f),0.420633942f),
//new SpawnPoint(new Vector3(-488.148529f, -980.9233f, 52.7693367f),321.121643f),
//new SpawnPoint(new Vector3(-491.84375f, -980.5606f, 52.7673378f),353.485077f),
//new SpawnPoint(new Vector3(-475.942f, -992.2404f, 50.3633f),181.777069f),
//new SpawnPoint(new Vector3(-497.640961f, -1040.96448f, 52.47619f),120.48822f),
//new SpawnPoint(new Vector3(-466.5181f, -992.242554f, 48.2768974f),239.538452f),
//new SpawnPoint(new Vector3(-465.5856f, -989.7029f, 48.2768974f),264.303467f),
//new SpawnPoint(new Vector3(-471.003235f, -960.962952f, 47.9796181f),128.094284f),
//new SpawnPoint(new Vector3(-463.0437f, -961.203369f, 47.9796181f),220.368149f),
//new SpawnPoint(new Vector3(-449.699463f, -960.8732f, 47.97959f),175.898315f),
//new SpawnPoint(new Vector3(-472.127167f, -931.709167f, 47.8318939f),87.85297f),
//new SpawnPoint(new Vector3(-472.019318f, -903.40094f, 47.8319473f),92.88794f),
//new SpawnPoint(new Vector3(-468.341278f, -878.443054f, 47.8336525f),358.132568f),
//new SpawnPoint(new Vector3(-472.277039f, -878.545654f, 47.9792824f),43.0413971f),
//new SpawnPoint(new Vector3(-471.446625f, -904.960266f, 38.6835976f),95.38634f),
//new SpawnPoint(new Vector3(-355.865051f, -717.6063f, 54.5214958f),55.44045f),
//new SpawnPoint(new Vector3(-355.310059f, -708.0777f, 54.62193f),0.000775332446f),
//new SpawnPoint(new Vector3(-338.965637f, -708.1196f, 54.62119f),0.000789314567f),
//new SpawnPoint(new Vector3(-278.534546f, -778.811768f, 54.5054131f),237.877563f),
//new SpawnPoint(new Vector3(-278.5344f, -778.8117f, 54.5054131f),237.1507f),
//new SpawnPoint(new Vector3(-288.948975f, -783.967468f, 54.6034546f),243.146408f),
//new SpawnPoint(new Vector3(-259.83252f, -191.761658f, 79.23743f),89.9999847f),
//new SpawnPoint(new Vector3(-240.509415f, -198.648987f, 79.2373657f),156.635742f),
//new SpawnPoint(new Vector3(-259.9073f, -153.16243f, 86.3294754f),106.939857f),
//new SpawnPoint(new Vector3(-210.408844f, -84.93959f, 86.12112f),341.0032f),
//new SpawnPoint(new Vector3(-242.778091f, -84.84866f, 86.12165f),49.9630852f),
//new SpawnPoint(new Vector3(-145.3093f, -107.767418f, 94.81063f),336.928345f),
//new SpawnPoint(new Vector3(-109.834587f, -123.759727f, 94.60001f),305.3925f),
//new SpawnPoint(new Vector3(-118.092392f, -195.562469f, 94.8469238f),237.481384f),
//new SpawnPoint(new Vector3(-123.43251f, -213.495087f, 82.53072f),250.994934f),
//new SpawnPoint(new Vector3(244.0002f, -51.0216827f, 84.4162445f),99.60801f),
//new SpawnPoint(new Vector3(255.133713f, -57.1682549f, 84.51809f),155.7816f),
//new SpawnPoint(new Vector3(353.113861f, -74.03187f, 100.618858f),195.292831f),
//new SpawnPoint(new Vector3(331.45932f, -2.20614386f, 100.619957f),51.3847122f),
//new SpawnPoint(new Vector3(439.2443f, 3442.696f, 51.4559059f),45.1524925f),
//new SpawnPoint(new Vector3(446.155121f, 3451.01172f, 51.4559059f),53.2588043f),
//new SpawnPoint(new Vector3(849.889f, 3500.9292f, 48.0326233f),297.782043f),
//new SpawnPoint(new Vector3(846.0236f, 3506.90259f, 48.0326233f),301.5652f),
//new SpawnPoint(new Vector3(1085.06213f, 3495.81958f, 43.85475f),278.294739f),
//new SpawnPoint(new Vector3(1083.40015f, 3502.56787f, 43.85475f),283.415466f),
//new SpawnPoint(new Vector3(1082.6825f, 3505.32813f, 43.85475f),294.7611f),
//new SpawnPoint(new Vector3(1290.7688f, 3478.484f, 47.5174332f),278.155548f),
//new SpawnPoint(new Vector3(1288.84387f, 3486.67554f, 47.5174332f),307.405151f),
//new SpawnPoint(new Vector3(1361.01672f, 3473.52271f, 46.4228134f),287.641022f),
//new SpawnPoint(new Vector3(1360.03247f, 3476.99268f, 46.4228134f),287.493622f),
//new SpawnPoint(new Vector3(1358.83154f, 3482.24219f, 46.4228134f),282.135468f),
//new SpawnPoint(new Vector3(1845.89172f, 3604.571f, 46.2868958f),117.548653f),
//new SpawnPoint(new Vector3(1849.7251f, 3598.155f, 46.32165f),115.616f),
//new SpawnPoint(new Vector3(1850.982f, 3595.82471f, 46.3339653f),130.718384f),
//new SpawnPoint(new Vector3(2861.91968f, 4392.703f, 72.18857f),107.685974f),
//new SpawnPoint(new Vector3(2884.64014f, 4349.1084f, 72.1892f),185.636475f),
//new SpawnPoint(new Vector3(2881.24268f, 4349.64258f, 72.1892f),115.658813f),
//new SpawnPoint(new Vector3(2877.71436f, 4357.786f, 72.18919f),104.206024f),
//new SpawnPoint(new Vector3(2878.74341f, 4356.11572f, 72.18919f),132.0969f),
//new SpawnPoint(new Vector3(2875.79688f, 4362.30469f, 72.18919f),116.501396f),
//new SpawnPoint(new Vector3(2919.643f, 4263.00342f, 92.95155f),93.27457f),
//new SpawnPoint(new Vector3(2923.42f, 4257.385f, 92.9434738f),150.940186f),
//new SpawnPoint(new Vector3(2931.92358f, 4269.89844f, 92.9434738f),342.246674f),
//new SpawnPoint(new Vector3(2928.17749f, 4271.32129f, 92.9434738f),353.6317f),
//new SpawnPoint(new Vector3(2885.85083f, 4336.86133f, 92.30756f),53.2266541f),
//new SpawnPoint(new Vector3(2900.62573f, 4342.248f, 92.3119049f),337.278534f),
//new SpawnPoint(new Vector3(2897.86279f, 4342.13525f, 92.3119049f),20.25213f),
//new SpawnPoint(new Vector3(2905.72949f, 4333.55859f, 92.3119f),289.629181f),
//new SpawnPoint(new Vector3(2908.80957f, 4323.896f, 92.3119f),240.294769f),
//new SpawnPoint(new Vector3(2895.054f, 4316.85938f, 92.3119049f),159.709885f),
//new SpawnPoint(new Vector3(2306.36816f, 5759.01758f, 142.636749f),224.17366f),
//new SpawnPoint(new Vector3(1282.76453f, 5831.77f, 490.3092f),353.963776f),
//new SpawnPoint(new Vector3(1234.71265f, 5831.955f, 507.110138f),5.165181f),
//new SpawnPoint(new Vector3(1223.16064f, 5828.238f, 513.8361f),3.55022717f),
//new SpawnPoint(new Vector3(617.6433f, 5690.71533f, 745.543945f),340.1424f),
//new SpawnPoint(new Vector3(502.4308f, 5632.62549f, 792.2829f),23.5985146f),
//new SpawnPoint(new Vector3(492.698761f, 5626.552f, 793.3864f),10.2373438f),
//new SpawnPoint(new Vector3(423.215271f, 5612.61f, 766.8335f),121.55191f),
//new SpawnPoint(new Vector3(423.30896f, 5614.00732f, 766.788269f),73.97381f),
//new SpawnPoint(new Vector3(413.560455f, 5572.68164f, 779.6017f),88.66989f),
//new SpawnPoint(new Vector3(428.219177f, 5571.87744f, 775.7194f),170.532227f),
//new SpawnPoint(new Vector3(427.707275f, 5572.98975f, 775.6545f),358.683044f),
//new SpawnPoint(new Vector3(441.563324f, 5581.185f, 793.5736f),73.1816254f),
//new SpawnPoint(new Vector3(441.3246f, 5575.65137f, 793.4948f),83.40293f),
//new SpawnPoint(new Vector3(441.40094f, 5562.768f, 793.5158f),140.375183f),
//new SpawnPoint(new Vector3(443.260529f, 5562.725f, 794.1303f),177.883209f),
//new SpawnPoint(new Vector3(-841.260742f, 4733.344f, 278.589539f),245.281616f),
//new SpawnPoint(new Vector3(-844.045f, 4729.36963f, 278.859253f),214.9878f),
//new SpawnPoint(new Vector3(-876.500549f, 4659.987f, 251.640152f),234.885818f),
//new SpawnPoint(new Vector3(-1141.83167f, 4660.26855f, 241.102921f),93.3069839f),
//new SpawnPoint(new Vector3(-1136.28809f, 4643.206f, 228.939346f),185.563477f),
//new SpawnPoint(new Vector3(-1545.894f, -467.488861f, 47.36901f),180.615723f),
//new SpawnPoint(new Vector3(-1537.1604f, -462.9984f, 47.3704834f),214.186554f),
//new SpawnPoint(new Vector3(-1575.64221f, -452.257538f, 49.79482f),318.592224f),
//new SpawnPoint(new Vector3(-1590.83557f, -466.6962f, 52.9546051f),179.315475f),
//new SpawnPoint(new Vector3(-1602.13623f, -457.498077f, 54.1521034f),137.856125f),
//new SpawnPoint(new Vector3(-1606.54028f, -453.820831f, 54.14815f),143.135132f),
//new SpawnPoint(new Vector3(-1614.85083f, -442.184937f, 55.153347f),55.73643f),
//new SpawnPoint(new Vector3(-1616.56323f, -445.325226f, 54.9542f),138.55513f),
//new SpawnPoint(new Vector3(-1614.91919f, -433.096344f, 55.15213f),55.44439f),
//new SpawnPoint(new Vector3(-1573.54846f, -425.937531f, 57.70627f),229.998245f),
//new SpawnPoint(new Vector3(-1574.48181f, -424.4097f, 57.70642f),320.00177f),
//new SpawnPoint(new Vector3(-1588.89966f, -412.312225f, 57.7061729f),320.00177f),
//new SpawnPoint(new Vector3(-1595.62964f, -410.283173f, 56.9529076f),34.63475f),
//new SpawnPoint(new Vector3(-1602.16956f, -417.9106f, 56.95383f),46.8494225f),
//new SpawnPoint(new Vector3(-1605.71167f, -422.485474f, 55.9530144f),54.6607628f),
//new SpawnPoint(new Vector3(-1586.07007f, -440.8518f, 57.7081642f),230.015228f),
//new SpawnPoint(new Vector3(2761.14136f, 1468.45081f, 48.3348923f),154.174f),
//new SpawnPoint(new Vector3(2358.524f, 4928.026f, 66.5629654f),132.626419f),
//new SpawnPoint(new Vector3(2362.14966f, 4927.50049f, 65.13156f),196.744919f),
//new SpawnPoint(new Vector3(2287.34155f, 4813.737f, 55.5826378f),223.574432f),
//new SpawnPoint(new Vector3(2291.70728f, 4814.10059f, 51.6821327f),169.651169f),
//new SpawnPoint(new Vector3(2295.215f, 4814.46826f, 51.72743f),215.397888f),
//new SpawnPoint(new Vector3(2298.27539f, 4818.19043f, 51.6533623f),263.560516f),
//new SpawnPoint(new Vector3(2291.77515f, 4824.17f, 51.6978951f),23.9202213f),
//new SpawnPoint(new Vector3(2288.3894f, 4815.647f, 54.8952446f),49.61185f),
//new SpawnPoint(new Vector3(-550.6003f, -206.904709f, 53.60504f),162.582214f),
//new SpawnPoint(new Vector3(-540.052246f, -201.140579f, 53.60504f),267.5929f),
//new SpawnPoint(new Vector3(-554.057f, -176.512466f, 54.22008f),292.3321f),
//new SpawnPoint(new Vector3(-562.1644f, -158.289886f, 54.2201576f),289.443085f),
//new SpawnPoint(new Vector3(-563.3693f, -156.513428f, 52.983242f),292.657837f),
//new SpawnPoint(new Vector3(-536.8853f, -172.687866f, 53.46394f),28.8539486f),
//new SpawnPoint(new Vector3(-566.973938f, -176.385208f, 54.2201424f),113.36412f),
//new SpawnPoint(new Vector3(-569.0218f, -169.771011f, 52.98364f),112.990532f),
//new SpawnPoint(new Vector3(-574.7573f, -156.259079f, 52.98312f),113.00032f),
//new SpawnPoint(new Vector3(-572.3173f, -162.599564f, 54.2201538f),109.660278f),
//new SpawnPoint(new Vector3(-562.875732f, -129.845856f, 53.00685f),202.913773f),
//new SpawnPoint(new Vector3(-551.655151f, -103.475372f, 55.73183f),297.726074f),
//new SpawnPoint(new Vector3(-557.283447f, -96.76135f, 55.73217f),345.7357f),
//new SpawnPoint(new Vector3(-489.400452f, 28.0376854f, 62.3618546f),178.742615f),
//new SpawnPoint(new Vector3(-492.1447f, 28.3932171f, 63.86277f),84.9999847f),
//new SpawnPoint(new Vector3(-492.184f, 28.4712334f, 63.8614159f),176.07692f),
//new SpawnPoint(new Vector3(-479.497742f, 26.9173546f, 62.3618622f),176.113876f),
//new SpawnPoint(new Vector3(-468.994263f, 26.1963139f, 62.3618546f),183.795f),
//new SpawnPoint(new Vector3(-305.9174f, 108.933853f, 81.28423f),39.6269341f),
//new SpawnPoint(new Vector3(-290.9571f, 108.211647f, 81.28897f),355.704346f),
//new SpawnPoint(new Vector3(-297.572235f, 109.14476f, 81.2889557f),358.271973f),
//new SpawnPoint(new Vector3(-285.47348f, 73.09235f, 80.90562f),230.521591f),
//new SpawnPoint(new Vector3(-306.085846f, 73.44269f, 81.0557251f),120.281281f),
//new SpawnPoint(new Vector3(-306.284454f, 77.3625946f, 81.0781f),93.31047f),
//new SpawnPoint(new Vector3(-203.945251f, 237.459824f, 103.125984f),228.142685f),
//new SpawnPoint(new Vector3(-199.62529f, 242.300659f, 103.125984f),289.173859f),
//new SpawnPoint(new Vector3(-201.4991f, 240.252762f, 103.125984f),221.2581f),
//new SpawnPoint(new Vector3(-199.2367f, 226.477325f, 102.0828f),348.367554f),
//new SpawnPoint(new Vector3(-204.567062f, 231.441f, 102.0828f),317.385864f),
//new SpawnPoint(new Vector3(277.9548f, -1446.89368f, 47.5046577f),134.967575f),
//new SpawnPoint(new Vector3(289.54126f, -1433.19458f, 47.5046577f),48.28633f),
//new SpawnPoint(new Vector3(285.9854f, -1437.2616f, 47.5046577f),45.6882133f),
//new SpawnPoint(new Vector3(81.13575f, -1730.44775f, 47.5070953f),319.2918f),
//new SpawnPoint(new Vector3(74.55717f, -1724.18f, 48.8485527f),315.147217f),
//new SpawnPoint(new Vector3(39.1368866f, -1791.10461f, 47.695282f),135.134659f),
//new SpawnPoint(new Vector3(29.00381f, -1782.87256f, 47.50741f),137.347824f),
//new SpawnPoint(new Vector3(23.962944f, -1778.99731f, 47.5077972f),64.98853f),
//new SpawnPoint(new Vector3(74.2938461f, -1724.94714f, 48.84015f),322.4359f),
//new SpawnPoint(new Vector3(484.9745f, -2251.84546f, 29.66953f),240.152466f),
//new SpawnPoint(new Vector3(477.243225f, -2245.5127f, 29.2299042f),72.93124f),
//new SpawnPoint(new Vector3(547.360168f, -2208.735f, 72.49173f),88.17523f),
//new SpawnPoint(new Vector3(547.80835f, -2205.78979f, 72.54862f),78.95248f),
//new SpawnPoint(new Vector3(-387.53006f, -2281.94019f, 31.36682f),269.6193f),
//new SpawnPoint(new Vector3(-390.584442f, -2286.14722f, 31.1057739f),180.7415f),
//new SpawnPoint(new Vector3(-388.785767f, -2276.88159f, 31.3542557f),40.86069f),
//new SpawnPoint(new Vector3(-388.105316f, -2276.77759f, 31.4265079f),359.823f),
//new SpawnPoint(new Vector3(-387.231628f, -2276.8667f, 31.3113842f),298.688538f),
//new SpawnPoint(new Vector3(338.0993f, -2755.20264f, 43.6318932f),97.33417f),
//new SpawnPoint(new Vector3(338.444031f, -2756.52222f, 43.6323051f),257.3371f),
//new SpawnPoint(new Vector3(338.120758f, -2762.2627f, 43.6318855f),85.26363f),
//new SpawnPoint(new Vector3(338.1191f, -2762.28662f, 43.631855f),273.359467f),
//new SpawnPoint(new Vector3(337.994324f, -2774.1665f, 43.6321754f),84.4224548f),
//new SpawnPoint(new Vector3(338.3043f, -2774.26514f, 43.6318436f),266.233978f),
//new SpawnPoint(new Vector3(336.962067f, -2720.959f, 45.8642769f),61.33962f),
//new SpawnPoint(new Vector3(336.8298f, -2796.62061f, 45.5247f),128.696762f),
//new SpawnPoint(new Vector3(196.155869f, -3138.172f, 43.1609039f),6.918362f),
//new SpawnPoint(new Vector3(196.203979f, -3138.14258f, 43.1609039f),81.8892746f),
//new SpawnPoint(new Vector3(196.215775f, -3138.17041f, 43.1609039f),48.0741463f),
//new SpawnPoint(new Vector3(209.575653f, -3138.49878f, 43.15829f),3.88275146f),
//new SpawnPoint(new Vector3(220.63855f, -3137.59863f, 44.36599f),359.9995f),
//new SpawnPoint(new Vector3(238.980331f, -3137.6f, 44.3651428f),359.999573f),
//new SpawnPoint(new Vector3(262.692627f, -3139.175f, 43.16133f),268.0243f),
//new SpawnPoint(new Vector3(262.2218f, -3138.22168f, 43.16133f),0.661799f),
//new SpawnPoint(new Vector3(262.452332f, -3199.81f, 43.1567078f),272.362671f),
//new SpawnPoint(new Vector3(262.1375f, -3310.09814f, 43.1617851f),180.403351f),
//new SpawnPoint(new Vector3(262.6746f, -3310.10132f, 43.1617851f),267.4616f),
//new SpawnPoint(new Vector3(232.695236f, -3311.01538f, 44.3657722f),180.001511f),
//#endregion
//        };
//    }
//}
