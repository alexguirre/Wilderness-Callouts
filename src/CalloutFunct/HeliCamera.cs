namespace WildernessCallouts.CalloutFunct
{
    using Rage;
    using WildernessCallouts.Types;
    using Rage.Native;
    using RAGENativeUI.Elements;
    using System.Drawing;
    using LSPD_First_Response.Mod.API;
    using LSPD_First_Response.Engine.Scripting.Entities;

    internal class HeliCamera
    {
        public Camera Camera;
        public Scaleform Scaleform;
        public Sound BackgroundSound;
        public Sound TurnSound;
        public Sound ZoomSound;
        public Sound SearchLoopSound;
        public Sound SearchSuccessSound;

        public HeliCamera()
        {
            BackgroundSound = new Sound();
            TurnSound = new Sound();
            ZoomSound = new Sound();
            SearchLoopSound = new Sound();
            SearchSuccessSound = new Sound();

            ManagerFiber = new GameFiber(delegate 
            {
                try
                {
                    while (true)
                    {
                        if (_canAbortManagerFiber)
                            break;
                        GameFiber.Yield();
                        Manager();
                    }
                }
                catch (System.Exception ex)
                {
                    Logger.LogExceptionDebug(this.GetType().Name, ex);
                }
            }, "HeliCamera Manager");
        }

        private int _searchCounter = 0;

        private bool _canAbortManagerFiber = false;
        public GameFiber ManagerFiber { get; }
        public void Manager()
        {
            if (Game.LocalPlayer.Character.IsInHelicopter)
            {
                if (_showHelpText)
                {
                    Game.DisplayHelp("~g~Wilderness Callouts:~s~ Press ~b~" + Controls.ToggleHeliCam.ToUserFriendlyName() + "~s~ to activate the helicopter camera", 6500);
                    _showHelpText = false;
                }

                if (Controls.ToggleHeliCam.IsJustPressed() && (Camera == null || !Camera.Exists()))
                {
                    Camera = new Camera(true);
                    Camera.SetRotationYaw(Game.LocalPlayer.Character.CurrentVehicle.Heading);
                    Scaleform = new Scaleform();
                    Scaleform.Load("heli_cam");
                    NativeFunction.CallByName<uint>("REQUEST_STREAMED_TEXTURE_DICT", "helicopterhud", true);
                    Scaleform.CallFunction("SET_CAM_LOGO", 1);
                    Camera.AttachToEntity(Game.LocalPlayer.Character.CurrentVehicle, GetCameraPositionOffsetForModel(Game.LocalPlayer.Character.CurrentVehicle.Model), true);
                    Sound.RequestAmbientAudioBank("SCRIPT\\POLICE_CHOPPER_CAM");
                    NativeFunction.CallByName<uint>("SET_NOISEOVERIDE", true);
                    NativeFunction.CallByName<uint>("SET_NOISINESSOVERIDE", 0.15f);
                }
                else if ((Controls.ToggleHeliCam.IsJustPressed() || !Game.LocalPlayer.Character.IsInHelicopter || Game.LocalPlayer.Character.CurrentVehicle.IsDead || Game.LocalPlayer.Character.IsDead) && (Camera != null && Camera.Exists()))
                {
                    NativeFunction.CallByName<uint>("SET_NOISEOVERIDE", false);
                    NativeFunction.CallByName<uint>("SET_NOISINESSOVERIDE", 0.0f);
                    Camera.Delete();
                    Camera = null;
                    Scaleform = null;
                    if (WildernessCallouts.Common.IsNightVisionActive())
                    {
                        new Sound(-1).PlayFrontend("THERMAL_VISION_GOGGLES_OFF_MASTER", null);
                        WildernessCallouts.Common.SetNightVision(false);
                    }
                    if (WildernessCallouts.Common.IsThermalVisionActive())
                    {
                        new Sound(-1).PlayFrontend("THERMAL_VISION_GOGGLES_OFF_MASTER", null);
                        WildernessCallouts.Common.SetThermalVision(false);
                    }
                }
                


                if (Camera != null && Camera.Exists() && Scaleform != null)
                {
                    if (BackgroundSound.HasFinished())
                        BackgroundSound.PlayFrontend("COP_HELI_CAM_BACKGROUND", null);

                    NativeFunction.CallByName<uint>("HIDE_HUD_AND_RADAR_THIS_FRAME");
                    WildernessCallouts.Common.DisEnableGameControls(false, GameControl.Enter, GameControl.VehicleExit, GameControl.VehicleAim, GameControl.VehicleAttack, GameControl.VehicleAttack2, GameControl.VehicleDropProjectile, GameControl.VehicleDuck/*, GameControl.VehicleFlyAttack, GameControl.VehicleFlyAttack2*/, GameControl.VehicleFlyAttackCamera, GameControl.VehicleFlyDuck, GameControl.VehicleFlySelectNextWeapon, GameControl.VehicleFlySelectPrevWeapon, GameControl.VehicleHandbrake, GameControl.VehicleJump, GameControl.LookLeftRight, GameControl.LookUpDown, GameControl.WeaponWheelPrev, GameControl.WeaponWheelNext);
                    
                    float FOVpercentage = -((Camera.FOV - 70/*max FOV*/)) / 35/*min FOV*/;
                    if (FOVpercentage > 1f) FOVpercentage = 1f;
                    else if (FOVpercentage < 0f) FOVpercentage = 0f;
                    Scaleform.CallFunction("SET_ALT_FOV_HEADING", Camera.Position.Z, FOVpercentage, Camera.Rotation.Yaw);
                    WildernessCallouts.Common.DrawScaleformMovieFullscreen(Scaleform, Color.FromArgb(0, 255, 255, 255));

                    float moveSpeed = (Camera.FOV / 100) * (WildernessCallouts.Common.IsUsingController() ? 3.5f : 5.25f);

                    //float upDown = NativeFunction.CallByName<float>("GET_DISABLED_CONTROL_NORMAL", 0, (int)GameControl.LookUpDown) * moveSpeed;
                    //float leftRight = NativeFunction.CallByName<float>("GET_DISABLED_CONTROL_NORMAL", 0, (int)GameControl.LookLeftRight) * moveSpeed;

                    //Camera.Rotation = new Rotator(Camera.Rotation.Pitch - upDown, Camera.Rotation.Roll, Camera.Rotation.Yaw - leftRight);

                    float yRotMagnitude = NativeFunction.CallByName<float>("GET_DISABLED_CONTROL_NORMAL", 0, (int)GameControl.LookUpDown) * moveSpeed;
                    float xRotMagnitude = NativeFunction.CallByName<float>("GET_DISABLED_CONTROL_NORMAL", 0, (int)GameControl.LookLeftRight) * moveSpeed;

                    float newPitch = Camera.Rotation.Pitch - yRotMagnitude;
                    float newYaw = Camera.Rotation.Yaw - xRotMagnitude;
                    Camera.Rotation = new Rotator((newPitch >= 25f || newPitch <= -70f) ? Camera.Rotation.Pitch : newPitch, /*_cam.Rotation.Roll*/0f, newYaw);


                    if (yRotMagnitude != 0f || xRotMagnitude != 0)
                    {
                        if (TurnSound.HasFinished())
                        {
                            TurnSound.PlayFrontend("COP_HELI_CAM_TURN", null);
                        }
                    }
                    else if (!TurnSound.HasFinished())
                        TurnSound.Stop();

                    //if (Camera.Rotation.Pitch <= -70f) Camera.SetRotationPitch(-69.99f);
                    //else if (Camera.Rotation.Pitch >= 25f) Camera.SetRotationPitch(24.99f);

                    if (!WildernessCallouts.Common.IsUsingController())
                    {
                        WildernessCallouts.Common.DisEnableGameControls(false, GameControl.WeaponWheelPrev, GameControl.WeaponWheelNext);
                        float wheelForwards = NativeFunction.CallByName<float>("GET_DISABLED_CONTROL_NORMAL", 0, (int)GameControl.WeaponWheelPrev) * 1.725f;
                        float wheelBackwards = NativeFunction.CallByName<float>("GET_DISABLED_CONTROL_NORMAL", 0, (int)GameControl.WeaponWheelNext) * 1.725f;

                        Camera.FOV -= wheelForwards - wheelBackwards;
                        if (Camera.FOV > 70f) Camera.FOV = 70f;
                        else if (Camera.FOV < 1f) Camera.FOV = 1f;
                    }
                    if (Game.IsControllerButtonDownRightNow(ControllerButtons.A)/* || Game.IsKeyDownRightNow(Keys.LShiftKey)*/)
                    {
                        if (ZoomSound.HasFinished())
                        {
                            ZoomSound.PlayFrontend("COP_HELI_CAM_ZOOM", null);
                        }
                        WildernessCallouts.Common.DisEnableGameControls(false, GameControl.VehicleFlyThrottleUp, GameControl.VehicleFlyThrottleDown);
                        float up = NativeFunction.CallByName<float>("GET_DISABLED_CONTROL_NORMAL", 0, (int)GameControl.VehicleFlyThrottleUp);
                        float down = NativeFunction.CallByName<float>("GET_DISABLED_CONTROL_NORMAL", 0, (int)GameControl.VehicleFlyThrottleDown);

                        Camera.FOV -= up - down;
                        if (Camera.FOV > 70f) Camera.FOV = 70f;
                        else if (Camera.FOV < 5f) Camera.FOV = 5f;
                    }
                    else
                    {
                        if (!ZoomSound.HasFinished()) ZoomSound.Stop();
                    }

                    if (Controls.ToggleBinocularsHeliCamNightVision.IsJustPressed() && !WildernessCallouts.Common.IsNightVisionActive() && !WildernessCallouts.Common.IsThermalVisionActive())
                    {
                        new Sound(-1).PlayFrontend("THERMAL_VISION_GOGGLES_ON_MASTER", null);
                        WildernessCallouts.Common.SetNightVision(true);
                    }
                    else if (Controls.ToggleBinocularsHeliCamNightVision.IsJustPressed() && WildernessCallouts.Common.IsNightVisionActive())
                    {
                        new Sound(-1).PlayFrontend("THERMAL_VISION_GOGGLES_OFF_MASTER", null);
                        WildernessCallouts.Common.SetNightVision(false);
                    }

                    if (Controls.ToggleBinocularsHeliCamThermalVision.IsJustPressed() && !WildernessCallouts.Common.IsThermalVisionActive() && !WildernessCallouts.Common.IsNightVisionActive())
                    {
                        new Sound(-1).PlayFrontend("THERMAL_VISION_GOGGLES_ON_MASTER", null);
                        WildernessCallouts.Common.SetThermalVision(true);
                    }
                    else if (Controls.ToggleBinocularsHeliCamThermalVision.IsJustPressed() && WildernessCallouts.Common.IsThermalVisionActive())
                    {
                        new Sound(-1).PlayFrontend("THERMAL_VISION_GOGGLES_OFF_MASTER", null); 
                        WildernessCallouts.Common.SetThermalVision(false);
                    }

#if DEBUG
                    Game.DisplayHelp(Camera.Direction.ToString());
#endif

                    Entity raycastedEntity = WildernessCallouts.Common.RaycastEntity2(Camera.Position, Camera.Direction, 2000.0f, Game.LocalPlayer.Character, Game.LocalPlayer.Character.CurrentVehicle);

                    if (_searchCounter != 0)
                    {
                        SizeF res = RAGENativeUI.UIMenu.GetScreenResolutionMantainRatio();
                        new Sprite("helicopterhud", "hud_line", new Point(((int)res.Width / 2) - ((int)res.Width / 8) /*- ((int)res.Width / 32)*/, ((int)res.Height / 2) + ((int)res.Height / 4)), new Size(5 + _searchCounter * 2, 30)).Draw();
                    }

                    if (raycastedEntity != null && raycastedEntity.Exists())
                    {
                        if (Controls.HeliCamScan.IsPressed() && (raycastedEntity.IsPed() || raycastedEntity.IsVehicle()))
                        {
                            _searchCounter++;
                            if (SearchLoopSound.HasFinished())
                                SearchLoopSound.PlayFrontend("COP_HELI_CAM_SCAN_PED_LOOP", null);
                            Vector2 v2 = World.ConvertWorldPositionToScreenPosition(raycastedEntity.Position);
                            SizeF res = RAGENativeUI.UIMenu.GetScreenResolutionMantainRatio();//fix: work with all res
                            new Sprite("helicopterhud", "hud_target", new Point((int)v2.X + (125 / (int)res.Width) /*- ((int)res.Width / 125)*/, (int)v2.Y + (125 / (int)res.Height) /*- ((int)res.Height / 125)*/), new Size(125, 125)).Draw();


                            if (_searchCounter > 240)
                            {
                                if (raycastedEntity.IsPed())
                                {
                                    //Game.DisplayNotification("Ped detected");
                                    if (SearchSuccessSound.HasFinished())
                                        SearchSuccessSound.PlayFrontend("COP_HELI_CAM_SCAN_PED_SUCCESS", null);
                                    _canOverrideDrawInfo = true;
                                    GameFiber.StartNew(delegate { DrawPedInfo((Ped)raycastedEntity, 13000); }, "DrawPedInfo Fiber");
                                }
                                else if (raycastedEntity.IsVehicle())
                                {
                                    //Game.DisplayNotification("Vehicle detected");
                                    if (SearchSuccessSound.HasFinished())
                                        SearchSuccessSound.PlayFrontend("COP_HELI_CAM_SCAN_PED_SUCCESS", null);
                                    _canOverrideDrawInfo = true;
                                    GameFiber.StartNew(delegate { DrawVehicleInfo((Vehicle)raycastedEntity, 13000); }, "DrawVehicleInfo Fiber");
                                }
                                if (!SearchLoopSound.HasFinished())
                                    SearchLoopSound.Stop();

                                _searchCounter = 0;
                            }
                        }

                        if (raycastedEntity.IsVehicle())
                        {
                            DrawVehicleSpeed((Vehicle)raycastedEntity);
                        }
                    }
                    else if (_searchCounter > 0)
                    {
                        _searchCounter--;
                    }


                    if (!SearchLoopSound.HasFinished())
                        SearchLoopSound.Stop();
                }
                else
                {
                    if (!BackgroundSound.HasFinished())
                        BackgroundSound.Stop();
                    if (!ZoomSound.HasFinished())
                        ZoomSound.Stop();
                    if (!TurnSound.HasFinished())
                        TurnSound.Stop();
                    if (!SearchLoopSound.HasFinished())
                        SearchLoopSound.Stop();
                    if (!SearchSuccessSound.HasFinished())
                        SearchLoopSound.Stop();
                }
            }
            else if (!_showHelpText)
            {
                _showHelpText = true;
            }
        }
        private bool _showHelpText = true;
        //private bool _isPointing = false;
        private bool _canOverrideDrawInfo = false;
        public void DrawVehicleInfo(Vehicle vehicle, ulong ticksToDraw)
        {
            ulong tickCount = Game.TickCount;
            SizeF res = RAGENativeUI.UIMenu.GetScreenResolutionMantainRatio();
            ResRectangle background = new ResRectangle(new Point((int)res.Width - 350, (int)res.Height - 600), new Size((int)res.Width, (int)res.Height - 571), Color.FromArgb(170, Color.Black));

            ResText plateText = new ResText("PLATE:  ~b~" + vehicle.LicensePlate.ToUpper(), new Point((int)res.Width - 320, (int)res.Height - 550), 0.3225f, Color.White, RAGENativeUI.Common.EFont.ChaletLondon, ResText.Alignment.Left);
            ResText ownerText = new ResText("OWNER:  ~b~" + Functions.GetVehicleOwnerName(vehicle), new Point((int)res.Width - 320, (int)res.Height - 500), 0.3225f, Color.White, RAGENativeUI.Common.EFont.ChaletLondon, ResText.Alignment.Left);
            ResText modelText = new ResText("MODEL:  ~b~" + vehicle.Model.Name.ToUpper(), new Point((int)res.Width - 320, (int)res.Height - 450), 0.3225f, Color.White, RAGENativeUI.Common.EFont.ChaletLondon, ResText.Alignment.Left);
            ResText primaryColorText = new ResText("PRIMARY COLOR:  ~b~" + vehicle.GetPrimaryColor().ToFriendlyName().ToUpper(), new Point((int)res.Width - 320, (int)res.Height - 400), 0.3225f, Color.White, RAGENativeUI.Common.EFont.ChaletLondon, ResText.Alignment.Left);
            ResText secondaryColorText = new ResText("SECONDARY COLOR:  ~b~" + vehicle.GetSecondaryColor().ToFriendlyName().ToUpper(), new Point((int)res.Width - 320, (int)res.Height - 350), 0.3225f, Color.White, RAGENativeUI.Common.EFont.ChaletLondon, ResText.Alignment.Left);

            _canOverrideDrawInfo = false;
            while (true)
            {
                if ((Game.TickCount - tickCount) > ticksToDraw) break;
                if (Camera == null || !Camera.Exists()) break;
                if (_canOverrideDrawInfo) break;
                background.Draw();
                plateText.Draw();
                ownerText.Draw();
                modelText.Draw();
                primaryColorText.Draw();
                secondaryColorText.Draw();
                GameFiber.Yield();
            }
        }

        public void DrawPedInfo(Ped ped, ulong ticksToDraw)
        {
            ulong tickCount = Game.TickCount;
            SizeF res = RAGENativeUI.UIMenu.GetScreenResolutionMantainRatio();
            ResRectangle background = new ResRectangle(new Point((int)res.Width - 350, (int)res.Height - 600), new Size((int)res.Width, (int)res.Height - 571), Color.FromArgb(170, Color.Black));

            Persona persona = Functions.GetPersonaForPed(ped);
            ResText nameText = new ResText("NAME:  ~b~" + persona.FullName.ToUpper(), new Point((int)res.Width - 320, (int)res.Height - 550), 0.3225f, Color.White, RAGENativeUI.Common.EFont.ChaletLondon, ResText.Alignment.Left);
            ResText genderText = new ResText("GENDER:  ~b~" + persona.Gender, new Point((int)res.Width - 320, (int)res.Height - 500), 0.3225f, Color.White, RAGENativeUI.Common.EFont.ChaletLondon, ResText.Alignment.Left);
            ResText birthdayText = new ResText("BIRTHDAY:  ~b~" + persona.BirthDay.ToShortDateString(), new Point((int)res.Width - 320, (int)res.Height - 450), 0.3225f, Color.White, RAGENativeUI.Common.EFont.ChaletLondon, ResText.Alignment.Left);
            ResText citationsText = new ResText("CITATIONS:  ~b~" + persona.Citations, new Point((int)res.Width - 320, (int)res.Height - 400), 0.3225f, Color.White, RAGENativeUI.Common.EFont.ChaletLondon, ResText.Alignment.Left);
            ResText wantedText = new ResText("EXTRA INFO:  ~b~" + (persona.Wanted ? "~r~WANTED" : "NONE"), new Point((int)res.Width - 320, (int)res.Height - 350), 0.3225f, Color.White, RAGENativeUI.Common.EFont.ChaletLondon, ResText.Alignment.Left);

            _canOverrideDrawInfo = false;
            while (true)
            {
                if((Game.TickCount - tickCount) > ticksToDraw) break;
                if (Camera == null || !Camera.Exists()) break;
                if (_canOverrideDrawInfo) break;
                background.Draw();
                nameText.Draw();
                genderText.Draw();
                birthdayText.Draw();
                citationsText.Draw();
                wantedText.Draw();
                GameFiber.Yield();
            }
        }


        public void DrawVehicleSpeed(Vehicle veh)
        {
            if (veh == null || !veh.Exists())
                return;

            float mpsSpeed = veh.Speed;
            float kphSpeed = MathHelper.ConvertMetersPerSecondToKilometersPerHour(mpsSpeed);
            float mphSpeed = MathHelper.ConvertMetersPerSecondToMilesPerHour(mpsSpeed);

            SizeF res = RAGENativeUI.UIMenu.GetScreenResolutionMantainRatio();

            new ResRectangle(new Point(0, (int)res.Height - 275), new Size((int)275, (int)184), Color.FromArgb(170, Color.Black)).Draw();
            new ResText("SPEED RADAR", new Point(80, (int)res.Height - 250), 0.375f, Color.White, RAGENativeUI.Common.EFont.ChaletLondon, ResText.Alignment.Centered).Draw();
            new ResText("  KM/H:  ~b~" + kphSpeed, new Point(20, (int)res.Height - 200), 0.3225f, Color.White, RAGENativeUI.Common.EFont.ChaletLondon, ResText.Alignment.Left).Draw();
            new ResText("  MI/H:  ~b~" + mphSpeed, new Point(20, (int)res.Height - 150), 0.3225f, Color.White, RAGENativeUI.Common.EFont.ChaletLondon, ResText.Alignment.Left).Draw();
        }

        public void CleanUp()
        {
            _canAbortManagerFiber = true;
            if (!BackgroundSound.HasFinished())
                BackgroundSound.Stop();
            if (!ZoomSound.HasFinished())
                ZoomSound.Stop();
            if (!TurnSound.HasFinished())
                TurnSound.Stop();
            if (!SearchLoopSound.HasFinished())
                SearchLoopSound.Stop();
            if (!SearchSuccessSound.HasFinished())
                SearchLoopSound.Stop();
            NativeFunction.CallByName<uint>("SET_NOISEOVERIDE", false);
            NativeFunction.CallByName<uint>("SET_NOISINESSOVERIDE", 0.0f);
            if (Camera.Exists())
            {
                Camera.Delete();
                Camera = null;
            }
            Scaleform = null;
            if (WildernessCallouts.Common.IsNightVisionActive())
                WildernessCallouts.Common.SetNightVision(false);
            if (WildernessCallouts.Common.IsThermalVisionActive())
                WildernessCallouts.Common.SetThermalVision(false);
            ManagerFiber.Abort();
        } 

        public static Vector3 GetCameraPositionOffsetForModel(Model model)
        {
            if (model == new Model("valkyrie") || model == new Model("valkyrie2"))//fixed
                return new Vector3(0.0f, 3.615f, -1.15f);
            else if (model == new Model("polmav"))//fixed
                return new Vector3(0.0f, 2.75f, -1.25f);
            else if (model == new Model("maverick"))//fixed
                return new Vector3(0.0f, 3.5f, -0.9225f);
            else if (model == new Model("savage"))//fixed
                return new Vector3(0.0f, 5.475f, -0.84115f);
            else if (model == new Model("buzzard") || model == new Model("buzzard2")) //fixed
                return new Vector3(0.0f, 1.958f, -0.75f);
            else if (model == new Model("cargobob") || model == new Model("cargobob3") || model == new Model("cargobob4"))//fixed
                return new Vector3(-0.58225f, 7.15f, -0.95f);
            else if(model ==  new Model("cargobob2"))//fixed
                return new Vector3(0.0f, 6.9625f, -1.0f);
            else if (model == new Model("frogger") || model == new Model("frogger2"))//fixed
                return new Vector3(0.0f, 3.25f, -0.5975f);
            else if (model == new Model("annihilator"))//fixed
                return new Vector3(-0.5715f, 4.0f, -0.686875f);
            else if (model == new Model("skylift"))//fixed
                return new Vector3(0.0f, 4.8385f, -2.275f);
            else if (model == new Model("swift") || model == new Model("swift2"))//fixed
                return new Vector3(0.0f, 4.765f, -0.6f);
            else if (model == new Model("supervolito") || model == new Model("supervolito2"))//fixed
                return new Vector3(0.0f, 3.145f, -0.9675f);
            else
                return new Vector3(0.0f, 2.75f, -1.25f);
        }
    }
}
