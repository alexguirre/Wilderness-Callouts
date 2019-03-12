namespace WildernessCallouts.CalloutFunct
{
    using Rage;
    using Rage.Native;
    using WildernessCallouts.Types;

    internal class Binoculars
    {
        private static bool _isActive = false;
        private static Texture _binocTexture = Game.CreateTextureFromFile(@"Plugins\LSPDFR\WildernessCallouts\BinocularsTexture.png");
        private static bool _isTextureRenderRunning = false;

        public static void EnableBinoculars()
        {
            GameFiber.StartNew(delegate
            {
                _isActive = true;

                Rage.Object binocular = new Rage.Object("prop_binoc_01", Game.LocalPlayer.Character.Position);
                binocular.AttachToEntity(Game.LocalPlayer.Character, Game.LocalPlayer.Character.GetBoneIndex(PedBoneId.RightPhHand), Vector3.Zero, Rotator.Zero);
                Game.LocalPlayer.Character.Tasks.PlayAnimation("amb@world_human_binoculars@male@base", "base", 8f, AnimationFlags.UpperBodyOnly | AnimationFlags.SecondaryTask | AnimationFlags.Loop);

                GameFiber.Sleep(1000);

                Camera binocCam = new Camera(true);
                binocCam.AttachToEntity(binocular, new Vector3(0.0f, -0.1f, 0.0f), true);
                binocCam.Rotation = Game.LocalPlayer.Character.Rotation;

                _isTextureRenderRunning = true;
                Game.RawFrameRender += RawFrameRender;


                while (true)
                {
                    //float moveSpeed = (binocCam.FOV / 100)/* * 3.0f*/;
                    WildernessCallouts.Common.DisEnableGameControls(false, GameControl.LookUpDown, GameControl.LookLeftRight, GameControl.WeaponWheelPrev, GameControl.WeaponWheelNext, GameControl.SelectWeapon, GameControl.SelectNextWeapon, GameControl.SelectPrevWeapon, GameControl.Sprint);
                    //float leftRight = NativeFunction.CallByName<float>("GET_DISABLED_CONTROL_NORMAL", 0, (int)GameControl.LookLeftRight);
                    //float upDown = NativeFunction.CallByName<float>("GET_DISABLED_CONTROL_NORMAL", 0, (int)GameControl.LookUpDown);


                    if (Controls.ToggleBinoculars.IsJustPressed() ||
                        Game.LocalPlayer.Character.IsDead ||
                        Game.LocalPlayer.Character.IsInAnyVehicle(false))
                        break;

                    //if (/*wheelBackwards != 0*/Game.IsKeyDownRightNow(Settings.ZoomOutBinocKey) && binocCam.FOV < 75.0f) binocCam.FOV += 0.51225f;
                    //if (/*wheelForwards != 0*/Game.IsKeyDownRightNow(Settings.ZoomInBinocKey) && binocCam.FOV > 1.0f) binocCam.FOV -= 0.51225f;
                    
                    //if (/*leftRight < -0.05f*/Game.IsKeyDownRightNow(Settings.LookLeftBinocKey)) binocCam.SetRotationYaw(binocCam.Rotation.Yaw + moveSpeed);
                    //if (/*leftRight > 0.05f*/Game.IsKeyDownRightNow(Settings.LookRightBinocKey)) binocCam.SetRotationYaw(binocCam.Rotation.Yaw - moveSpeed);

                    //if (/*upDown < -0.05f*/Game.IsKeyDownRightNow(Settings.LookUpBinocKey) && binocCam.Rotation.Pitch <= 85.0f) binocCam.SetRotationPitch(binocCam.Rotation.Pitch + moveSpeed);
                    //if (/*upDown > 0.05f*/Game.IsKeyDownRightNow(Settings.LookDownBinocKey) && binocCam.Rotation.Pitch >= -85.0f) binocCam.SetRotationPitch(binocCam.Rotation.Pitch - moveSpeed);



                    float moveSpeed = (binocCam.FOV / 100) * (WildernessCallouts.Common.IsUsingController() ? 3.5f : 5.25f);

                    float upDown = NativeFunction.CallByName<float>("GET_DISABLED_CONTROL_NORMAL", 0, (int)GameControl.LookUpDown) * moveSpeed;
                    float leftRight = NativeFunction.CallByName<float>("GET_DISABLED_CONTROL_NORMAL", 0, (int)GameControl.LookLeftRight) * moveSpeed;

                    binocCam.Rotation = new Rotator(binocCam.Rotation.Pitch - upDown, binocCam.Rotation.Roll, binocCam.Rotation.Yaw - leftRight);

                    if (binocCam.Rotation.Pitch >= 85.0f) binocCam.SetRotationPitch(84.98f);
                    else if (binocCam.Rotation.Pitch <= -85.0f) binocCam.SetRotationPitch(-84.98f);


                    float wheelForwards = NativeFunction.CallByName<float>("GET_DISABLED_CONTROL_NORMAL", 0, (int)GameControl.WeaponWheelPrev) * 1.81125f;
                    float wheelBackwards = NativeFunction.CallByName<float>("GET_DISABLED_CONTROL_NORMAL", 0, (int)GameControl.WeaponWheelNext) * 1.81125f;

                    if (WildernessCallouts.Common.IsUsingController())
                    {
                        binocCam.FOV -= wheelBackwards - wheelForwards;
                    }
                    else
                    {
                        binocCam.FOV -= wheelForwards - wheelBackwards;
                    }

                    if (binocCam.FOV > 75.0f) binocCam.FOV = 75.0f;
                    else if (binocCam.FOV < 1.0f) binocCam.FOV = 1.0f;




                    if (Controls.ToggleBinocularsHeliCamNightVision.IsJustPressed() && !WildernessCallouts.Common.IsNightVisionActive() && !WildernessCallouts.Common.IsThermalVisionActive())
                    {
                        NativeFunction.CallByName<uint>("PLAY_SOUND_FRONTEND", -1, "THERMAL_VISION_GOGGLES_ON_MASTER", 0, 1);
                        WildernessCallouts.Common.SetNightVision(true);
                        GameFiber.Sleep(25);
                    }
                    else if (Controls.ToggleBinocularsHeliCamNightVision.IsJustPressed() && WildernessCallouts.Common.IsNightVisionActive())
                    {
                        NativeFunction.CallByName<uint>("PLAY_SOUND_FRONTEND", -1, "THERMAL_VISION_GOGGLES_OFF_MASTER", 0, 1);
                        WildernessCallouts.Common.SetNightVision(false);
                        GameFiber.Sleep(25);
                    }

                    if (Controls.ToggleBinocularsHeliCamThermalVision.IsJustPressed() && !WildernessCallouts.Common.IsThermalVisionActive() && !WildernessCallouts.Common.IsNightVisionActive())
                    {
                        NativeFunction.CallByName<uint>("PLAY_SOUND_FRONTEND", -1, "THERMAL_VISION_GOGGLES_ON_MASTER", 0, 1);
                        WildernessCallouts.Common.SetThermalVision(true);
                        GameFiber.Sleep(25);
                    }
                    else if (Controls.ToggleBinocularsHeliCamThermalVision.IsJustPressed() && WildernessCallouts.Common.IsThermalVisionActive())
                    {
                        NativeFunction.CallByName<uint>("PLAY_SOUND_FRONTEND", -1, "THERMAL_VISION_GOGGLES_OFF_MASTER", 0, 1);
                        WildernessCallouts.Common.SetThermalVision(false);
                        GameFiber.Sleep(25);
                    }


                    if (!Game.LocalPlayer.Character.IsPlayingAnimation("amb@world_human_binoculars@male@base", "base")) Game.LocalPlayer.Character.Tasks.PlayAnimation("amb@world_human_binoculars@male@base", "base", 8f, AnimationFlags.UpperBodyOnly | AnimationFlags.SecondaryTask | AnimationFlags.Loop);

                    Game.LocalPlayer.Character.Heading = binocCam.Rotation.Yaw;

                    //if (Settings.General.IsDebugBuild) Game.DisplaySubtitle("FOV: " + binocCam.FOV.ToString() + "  PITCH: " + binocCam.Rotation.Pitch + "  YAW: " + binocCam.Rotation.Yaw + "  MOVE SPEED: " + moveSpeed.ToString(), 1000);

                    GameFiber.Yield();
                }

                if (WildernessCallouts.Common.IsNightVisionActive())
                {
                    NativeFunction.CallByName<uint>("PLAY_SOUND_FRONTEND", -1, "THERMAL_VISION_GOGGLES_OFF_MASTER", 0, 1);
                    WildernessCallouts.Common.SetNightVision(false);
                }
                if (WildernessCallouts.Common.IsThermalVisionActive())
                {
                    NativeFunction.CallByName<uint>("PLAY_SOUND_FRONTEND", -1, "THERMAL_VISION_GOGGLES_OFF_MASTER", 0, 1);
                    WildernessCallouts.Common.SetThermalVision(false);
                }

                binocCam.Delete();
                _isTextureRenderRunning = false;
                Game.RawFrameRender -= RawFrameRender;
                Game.LocalPlayer.Character.Tasks.Clear();
                GameFiber.Sleep(250);
                binocular.Delete();

                WildernessCallouts.Common.DisEnableGameControls(true, GameControl.LookUpDown, GameControl.LookLeftRight, GameControl.WeaponWheelPrev, GameControl.WeaponWheelNext, GameControl.SelectWeapon, GameControl.SelectNextWeapon, GameControl.SelectPrevWeapon, GameControl.Sprint);

                _isActive = false;
            });
        }

        public static bool IsActive { get { return _isActive; } }
        public static Texture BinocularsTexture { get { return _binocTexture; } }
        public static bool IsTextureRenderRunning { get { return _isTextureRenderRunning; } }


        private static void RawFrameRender(object sender, GraphicsEventArgs e)
        {
            e.Graphics.DrawTexture(Binoculars.BinocularsTexture, 0.0f, 0.0f, Game.Resolution.Width, Game.Resolution.Height);
        }
    }
}
