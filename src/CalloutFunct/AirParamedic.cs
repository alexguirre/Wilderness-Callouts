namespace WildernessCallouts.CalloutFunct
{
    // System
    using System.Collections.Generic;
    using System.Drawing;

    // RPH
    using Rage;
    using Rage.Native;

    // LSPDFR
    using LSPD_First_Response.Mod.API;

    // WildernessCallouts
    using WildernessCallouts.Types;


    internal class AirParamedic
    {
        private static List<Ped> _rescuedPeds = new List<Ped>();
        public static List<Ped> RescuedPeds { get { return _rescuedPeds; } }

        private static string[] epsilonVoices = { "a_m_y_epsilon_01_black_full_01", "a_m_y_epsilon_01_korean_full_01", "a_m_y_epsilon_01_white_full_01", "a_m_y_epsilon_02_white_mini_01" };

        private static string[] tendToDeadIdles = { "idle_a", "idle_b", "idle_c" };


        public Ped Pilot { get; }

        public Ped Paramedic1 { get; }
        public Ped Paramedic2 { get; }

        public Vehicle Helicopter { get; }
        private Blip _heliBlip;
        private Rage.Object _notepad;

        public Ped PedToRescue { get; }

        public AirParamedic(Ped toRescue, string phraseToSayToThePlayerMaleVersion, string phraseToSayToThePlayerFemaleVersion)
        {
            _rescuedPeds.Add(toRescue);
            PedToRescue = toRescue;

            Vector3 position = GetSpawnPoint();

            Helicopter = new Vehicle(Settings.AirAmbulance.HeliModel, position, MathHelper.GetRandomSingle(0.0f, 360.0f));
            Helicopter.SetLivery(Settings.AirAmbulance.HeliLiveryIndex);
            NativeFunction.CallByName<uint>("SET_HELI_BLADES_FULL_SPEED", Helicopter);

            Pilot = new Ped(Settings.AirAmbulance.PilotModels.GetRandomElement(), Vector3.Zero, 0.0f);
            Pilot.BlockPermanentEvents = true;
            NativeFunction.CallByName<uint>("SET_PED_FLEE_ATTRIBUTES", Pilot, 0, 0);
            NativeFunction.CallByName<uint>("SET_PED_COMBAT_ATTRIBUTES", Pilot, 17, 1);
            Pilot.WarpIntoVehicle(Helicopter, -1);

            Paramedic1 = new Ped(Settings.AirAmbulance.ParamedicModels.GetRandomElement(), Vector3.Zero, 0.0f);
            Paramedic1.BlockPermanentEvents = true;
            Paramedic1.WarpIntoVehicle(Helicopter, 1);

            Paramedic2 = new Ped(Settings.AirAmbulance.ParamedicModels.GetRandomElement(), Vector3.Zero, 0.0f);
            Paramedic2.BlockPermanentEvents = true;
            Paramedic2.WarpIntoVehicle(Helicopter, 2);

            _heliBlip = new Blip(Helicopter);
            _heliBlip.Sprite = BlipSprite.Heli;
            _heliBlip.Color = Color.FromArgb(255, Color.Red);

            this.phraseToSayToThePlayerMaleVersion = phraseToSayToThePlayerMaleVersion;
            this.phraseToSayToThePlayerFemaleVersion = phraseToSayToThePlayerFemaleVersion;
        }

        private string phraseToSayToThePlayerMaleVersion, phraseToSayToThePlayerFemaleVersion;

        public void Start(bool cleanUpOnEnd = true)
        {
            if (PedToRescue.Exists())
            {
                Game.DisplayNotification("~b~" + Settings.General.Name + ":~w~ Dispatch, I need an air ambulance in " + Game.LocalPlayer.Character.Position.GetZoneName());
                GameFiber.Sleep(1000);
                Game.DisplayNotification("~b~Dispatch:~w~ Roger, " + Settings.AirAmbulance.ParamedicName.ToLower() + " en route");

                Functions.PlayScannerAudioUsingPosition("WE_HAVE MEDICAL_EMERGENCY IN_OR_ON_POSITION UNITS_RESPOND_CODE_99 OUTRO OFFICER_INTRO HELI_APPROACHING_DISPATCH", Game.LocalPlayer.Character.Position);

                Vector3 posToFly = PedToRescue.Position + Vector3.WorldUp * 32.5f;

                GameFiber.Sleep(100);

                NativeFunction.CallByName<uint>("TASK_HELI_MISSION", Pilot, Helicopter, 0, 0, posToFly.X, posToFly.Y, posToFly.Z, 6, 40.0f, 1.0f, 36.0f, 15, 15, -1.0f, 1);

                while (true)
                {
                    if (Helicopter.Exists())
                    {
                        if (Vector3.Distance2D(posToFly, Helicopter.Position) < 7.0f && Helicopter.Speed < 1.0f)
                        {
                            if (Paramedic1.Exists() && Paramedic1.IsInVehicle(Helicopter, false))
                                Paramedic1.Tasks.RappelFromHelicopter();
                            if (Paramedic2.Exists() && Paramedic2.IsInVehicle(Helicopter, false))
                                Paramedic2.Tasks.RappelFromHelicopter().WaitForCompletion();
                            break;
                        }
                    }
                    GameFiber.Yield();
                }

                if (PedToRescue.Exists())
                {
                    if (Paramedic1.Exists())
                        Paramedic1.Tasks.FollowNavigationMeshToPosition(Game.LocalPlayer.Character.Position + Game.LocalPlayer.Character.ForwardVector * 1.1125f, Game.LocalPlayer.Character.Heading - 180.0f, 10.0f);

                    if (Paramedic2.Exists())
                        Paramedic2.Tasks.FollowNavigationMeshToPosition(PedToRescue.Position + Vector3.RelativeRight, PedToRescue.Heading - 180, 10.0f).WaitForCompletion(4500);
                }

                if (Paramedic1.Exists())
                    Paramedic1.Tasks.AchieveHeading(Paramedic1.GetHeadingTowards(Game.LocalPlayer.Character)).WaitForCompletion(700);

                if (PedToRescue.Exists())
                {
                    if (Paramedic1.Exists() && Paramedic1.IsMale)
                        Paramedic1.PlayAmbientSpeech(epsilonVoices.GetRandomElement(), Globals.Random.Next(2) == 1 ? Speech.GENERIC_HI : Speech.KIFFLOM_GREET, 0, SpeechModifier.ForceShouted);

                    if (PedToRescue.IsMale)
                        Game.DisplaySubtitle("~b~" + Settings.AirAmbulance.ParamedicName + ":~w~ " + phraseToSayToThePlayerMaleVersion, 3000);
                    else 
                        Game.DisplaySubtitle("~b~" + Settings.AirAmbulance.ParamedicName + ":~w~ " + phraseToSayToThePlayerFemaleVersion, 3000);
                }

                GameFiber.StartNew(delegate
                {
                    if (PedToRescue.Exists())
                    {
                        if (Paramedic2.Exists())
                            Paramedic2.Tasks.AchieveHeading(Paramedic2.GetHeadingTowards(PedToRescue)).WaitForCompletion(800);

                        if (Paramedic2.Exists())
                        {
                            Paramedic2.Tasks.PlayAnimation("amb@medic@standing@tendtodead@idle_a", tendToDeadIdles.GetRandomElement(), 2.0f, AnimationFlags.Loop);

                            Paramedic2.PlayAmbientSpeech(null, Speech.GENERIC_SHOCKED_HIGH, 0, SpeechModifier.ForceShouted);
                        }
                    }
                });

                GameFiber.Sleep(1300);

                if (Paramedic1.Exists() && Paramedic2.Exists())
                    Paramedic1.Tasks.FollowNavigationMeshToPosition(Paramedic2.Position + Paramedic2.ForwardVector * 2.25f, Paramedic2.Heading - 180.0f, 10.0f).WaitForCompletion(12750);

                if (PedToRescue.Exists() && Paramedic1.Exists())
                    Paramedic1.Tasks.AchieveHeading(Paramedic1.GetHeadingTowards(PedToRescue)).WaitForCompletion(800);

                if (Paramedic1.Exists())
                    Paramedic1.Tasks.PlayAnimation("amb@medic@standing@timeofdeath@base", "base", 2.0f, AnimationFlags.Loop);

                _notepad = new Rage.Object("prop_notepad_02", Vector3.Zero);
                if (Paramedic1.Exists() && _notepad.Exists())
                    _notepad.AttachToEntity(Paramedic1, Paramedic1.GetBoneIndex(PedBoneId.LeftPhHand), Vector3.Zero, Rotator.Zero);

                GameFiber.Sleep(11750);

                if (Paramedic1.Exists())
                {
                    Paramedic1.Tasks.Clear();
                    if(Paramedic1.IsMale)
                        Paramedic1.PlayAmbientSpeech("a_m_m_beach_02_black_full_01", Speech.GENERIC_BYE, 0, SpeechModifier.ForceShouted);
                }
                if (Paramedic2.Exists())
                {
                    Paramedic2.Tasks.Clear();
                    if (Paramedic2.IsMale)
                        Paramedic2.PlayAmbientSpeech("a_m_o_genstreet_01_white_full_01", Speech.GENERIC_BYE, 0, SpeechModifier.ForceShouted);
                }

                GameFiber.Sleep(1350);

                if (PedToRescue.Exists())
                    PedToRescue.Delete();

                if (Helicopter.Exists())
                {
                    if (Paramedic1.Exists())
                        Paramedic1.WarpIntoVehicle(Helicopter, 1);
                    if (Paramedic2.Exists())
                        Paramedic2.WarpIntoVehicle(Helicopter, 2);
                }

                GameFiber.Sleep(775);
            }
            if (cleanUpOnEnd)
            {
                CleanUp();
            }
        }

        public void CleanUp()
        {
            if (Pilot.Exists())
                Pilot.Dismiss();

            if (Paramedic1.Exists())
                Paramedic1.Delete();
            if (Paramedic2.Exists())
                Paramedic2.Delete();

            if (Helicopter.Exists())
                Helicopter.Dismiss();

            if (_heliBlip.Exists())
                _heliBlip.Delete();

            if (_notepad.Exists())
                _notepad.Delete();
        }
        
        

        private static Vector3 GetSpawnPoint()
        {
            Vector3 v3 = Game.LocalPlayer.Character.Position.AroundPosition(800.0f) + Vector3.WorldUp * 450.0f;
            while (Vector3.Distance2D(Game.LocalPlayer.Character.Position, v3) < 400.0f)
            {
                v3 = Game.LocalPlayer.Character.Position.AroundPosition(800.0f) + Vector3.WorldUp * 450.0f;
                GameFiber.Yield();
            }
            return v3;
        }
    }
}
