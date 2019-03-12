//namespace WildernessCallouts.CalloutFunct
//{
    //using Rage;
    //using Rage.Native;
    //using System;
    //using System.Drawing;
    //using System.Collections.Generic;
    //using WildernessCallouts.Types;

    //public delegate void CorrectWireSelectedEventHandler(object sender);
    //public delegate void IncorrectWireSelectedEventHandler(object sender);

    //public class BombDisarm
    //{
    //    public event CorrectWireSelectedEventHandler OnCorrectWireSelected = delegate { };
    //    public event IncorrectWireSelectedEventHandler OnIncorrectWireSelected = delegate { };

    //    //public bool Active { get; set; }

    //    //public Texture BombTexture { get; set; }

    //    public static Texture CursorTexture { get; } = Game.CreateTextureFromFile(@"Plugins\LSPDFR\WildernessCallouts\WireCutterCursor.png");

    //    public static Texture RedWireTexture { get; } = Helper.GetTextureFromEmbeddedResource("Testing.Resources.RedWire.png");
    //    public static Texture GreenWireTexture { get; } = Helper.GetTextureFromEmbeddedResource("Testing.Resources.GreenWire.png");
    //    public static Texture BlueWireTexture { get; } = Helper.GetTextureFromEmbeddedResource("Testing.Resources.BlueWire.png");
    //    public static Texture WhiteWireTexture { get; } = Helper.GetTextureFromEmbeddedResource("Testing.Resources.WhiteWire.png");

    //    public Wire CorrectWire { get; set; }

    //    public List<Wire> CutWires { get; set; }

    //    public Attempt CurrentAttempt { get; set; }
    //    public Attempt MaxAttempts { get; set; }

    //    private int _redWireYPos;
    //    private int _greenWireYPos;
    //    private int _blueWireYPos;
    //    private int _whiteWireYPos;

    //    public BombDisarm()
    //    {
    //        //BombTexture = Game.CreateTextureFromFile(@"Plugins\LSPDFR\WildernessCallouts\Bomb.png");

    //        //RedWireTexture = GetTextureFromEmbeddedResource("Testing.Resources.RedWire.png")/*Game.CreateTextureFromFile(@"Plugins\LSPDFR\WildernessCallouts\RedWire.png")*/;
    //        //GreenWireTexture = GetTextureFromEmbeddedResource("Testing.Resources.GreenWire.png")/*Game.CreateTextureFromFile(@"Plugins\LSPDFR\WildernessCallouts\GreenWire.png")*/;
    //        //BlueWireTexture = GetTextureFromEmbeddedResource("Testing.Resources.BlueWire.png")/*Game.CreateTextureFromFile(@"Plugins\LSPDFR\WildernessCallouts\BlueWire.png")*/;
    //        //WhiteWireTexture = GetTextureFromEmbeddedResource("Testing.Resources.WhiteWire.png")/*Game.CreateTextureFromFile(@"Plugins\LSPDFR\WildernessCallouts\WhiteWire.png")*/;

    //        MaxAttempts = Attempt.Third;
    //        CurrentAttempt = Attempt.None;

    //        CutWires = new List<Wire>();

    //        CorrectWire = new Wire[] { Wire.Red, Wire.Green, Wire.Blue, Wire.White }.GetRandomElement();

    //        List<int> possibleYPos = new List<int> { 25, 125, 225, 325 };
    //        _redWireYPos = possibleYPos.GetRandomElement();
    //        possibleYPos.Remove(_redWireYPos);
    //        _greenWireYPos = possibleYPos.GetRandomElement();
    //        possibleYPos.Remove(_greenWireYPos);
    //        _blueWireYPos = possibleYPos.GetRandomElement();
    //        possibleYPos.Remove(_blueWireYPos);
    //        _whiteWireYPos = possibleYPos.GetRandomElement();
    //        possibleYPos.Remove(_whiteWireYPos);
    //    }


    //    public void Start()
    //    {
    //        Game.FrameRender += FrameRender;
    //        Game.RawFrameRender += RawFrameRender;

    //        Game.LocalPlayer.Character.Tasks.PlayAnimation("amb@medic@standing@tendtodead@idle_a", "idle_a", 2.0f, AnimationFlags.Loop);
    //    }

    //    public void Finish()
    //    {
    //        Game.FrameRender -= FrameRender;
    //        Game.RawFrameRender -= RawFrameRender;

    //        Game.LocalPlayer.Character.Tasks.PlayAnimation("amb@medic@standing@tendtodead@exit", "exit", 1.0f, AnimationFlags.None);
    //    }

    //    public void RawFrameRender(object sender, GraphicsEventArgs e)
    //    {
    //        //if (!Active) return;
    //        Rage.Graphics g = e.Graphics;

    //        //g.DrawTexture(RedWireTexture, new Vector2(25, 25), Vector2.Zero, new Vector2(RedWireTexture.Size.Width, RedWireTexture.Size.Height), 0f, 0f, 0f, 0f, 0f, Vector2.Zero);
    //        //g.DrawTexture(GreenWireTexture, new Vector2(25, 50), Vector2.Zero, new Vector2(GreenWireTexture.Size.Width, GreenWireTexture.Size.Height), 0f, 0f, 0f, 0f, 0f, Vector2.Zero);
    //        //g.DrawTexture(BlueWireTexture, new Vector2(25, 75), Vector2.Zero, new Vector2(BlueWireTexture.Size.Width, BlueWireTexture.Size.Height), 0f, 0f, 0f, 0f, 0f, Vector2.Zero);
    //        //g.DrawTexture(WhiteWireTexture, new Vector2(25, 100), Vector2.Zero, new Vector2(WhiteWireTexture.Size.Width, WhiteWireTexture.Size.Height), 0f, 0f, 0f, 0f, 0f, Vector2.Zero);

    //        g.DrawTexture(RedWireTexture, 25, _redWireYPos, 512, 77);
    //        g.DrawTexture(GreenWireTexture, 25, _greenWireYPos, 512, 77);
    //        g.DrawTexture(BlueWireTexture, 25, _blueWireYPos, 512, 77);
    //        g.DrawTexture(WhiteWireTexture, 25, _whiteWireYPos, 512, 77);

    //        //g.DrawCircle(GetMousePosition(), 5.0f, Color.FromArgb(120, Color.Orange));
    //    }
    //    public void FrameRender(object sender, GraphicsEventArgs e)
    //    {
    //        //if (!Active) return;

    //        Rage.Graphics g = e.Graphics;

    //        DisableAllGameControls();
    //        Helper.DisEnableGameControls(true, GameControl.CursorX, GameControl.CursorY, GameControl.CursorAccept);

    //        //NativeFunction.CallByHash<uint>(0xaae7ce1d63167423);  // _SHOW_CURSOR_THIS_FRAME

    //        Vector2 mousePos = GetMousePosition();

    //        g.DrawTexture(CursorTexture, mousePos.X, mousePos.Y, CursorTexture.Size.Width / 2, CursorTexture.Size.Height / 2);


    //        switch (GetCurrentSelectedWire())
    //        {
    //            case Wire.None:
    //                break;
    //            case Wire.Red:
    //                g.DrawRectangle(new RectangleF(new Point(25, _redWireYPos), new Size(512, 77)), Color.FromArgb(100, Color.Red));
    //                g.DrawText("CUT RED WIRE", "", 24.25f, new PointF(mousePos.X, mousePos.Y), Color.DarkRed);
    //                break;
    //            case Wire.Green:
    //                g.DrawRectangle(new RectangleF(new Point(25, _greenWireYPos), new Size(512, 77)), Color.FromArgb(100, Color.Green));
    //                g.DrawText("CUT GREEN WIRE", "", 24.25f, new PointF(mousePos.X, mousePos.Y), Color.DarkGreen);
    //                break;
    //            case Wire.Blue:
    //                g.DrawRectangle(new RectangleF(new Point(25, _blueWireYPos), new Size(512, 77)), Color.FromArgb(100, Color.Blue));
    //                g.DrawText("CUT BLUE WIRE", "", 24.25f, new PointF(mousePos.X, mousePos.Y), Color.DarkBlue);
    //                break;
    //            case Wire.White:
    //                g.DrawRectangle(new RectangleF(new Point(25, _whiteWireYPos), new Size(512, 77)), Color.FromArgb(100, Color.White));
    //                g.DrawText("CUT WHITE WIRE", "", 24.25f, new PointF(mousePos.X, mousePos.Y), Color.LightGray);
    //                break;
    //            default:
    //                break;
    //        }


    //        if (IsMouseInBounds(new Point(25, _redWireYPos), new Size(512, 77)))
    //        {
    //            Game.DisplaySubtitle("~r~In red wire", 1);
    //        }

    //        if (IsMouseInBounds(new Point(25, _greenWireYPos), new Size(512, 77)))
    //        {
    //            Game.DisplaySubtitle("~g~In green wire", 1);
    //        }

    //        if (IsMouseInBounds(new Point(25, _blueWireYPos), new Size(512, 77)))
    //        {
    //            Game.DisplaySubtitle("~b~In blue wire", 1);
    //        }

    //        if (IsMouseInBounds(new Point(25, _whiteWireYPos), new Size(512, 77)))
    //        {
    //            Game.DisplaySubtitle("~w~In white wire", 1);
    //        }

    //        if (RAGENativeUI.WildernessCallouts.Common.IsDisabledControlJustPressed(0, GameControl.CursorAccept))
    //        {
    //            Wire currentWire = GetCurrentSelectedWire();
    //            if (!CutWires.Contains(currentWire))
    //            {
    //                if (currentWire == CorrectWire)
    //                {
    //                    CurrentAttempt++;
    //                    CutWires.Add(currentWire);
    //                    GameFiber.StartNew(delegate
    //                    {
    //                        //PlaySound(Properties.Resources.WireCut); ADD RESOURCES
    //                        Finish();
    //                        if (OnCorrectWireSelected != null)
    //                            OnCorrectWireSelected(this);
    //                    });
    //                }
    //                else if (currentWire != Wire.None)
    //                {
    //                    CurrentAttempt++;
    //                    CutWires.Add(currentWire);
    //                    GameFiber.StartNew(delegate
    //                    {
    //                        //PlaySound(Properties.Resources.WireCut); ADD RESOURCES
    //                        if (CurrentAttempt == MaxAttempts)
    //                        {
    //                            Finish();
    //                            if (OnIncorrectWireSelected != null)
    //                                OnIncorrectWireSelected(this);
    //                        }
    //                    });
    //                }
    //            }
    //        }

    //        foreach (Wire wire in CutWires)
    //        {
    //            switch (wire)
    //            {
    //                case Wire.None:
    //                    break;
    //                case Wire.Red:
    //                    g.DrawRectangle(new RectangleF(new Point(25, _redWireYPos), new Size(512, 77)), Color.FromArgb(210, Color.Black));
    //                    break;
    //                case Wire.Green:
    //                    g.DrawRectangle(new RectangleF(new Point(25, _greenWireYPos), new Size(512, 77)), Color.FromArgb(210, Color.Black));
    //                    break;
    //                case Wire.Blue:
    //                    g.DrawRectangle(new RectangleF(new Point(25, _blueWireYPos), new Size(512, 77)), Color.FromArgb(210, Color.Black));
    //                    break;
    //                case Wire.White:
    //                    g.DrawRectangle(new RectangleF(new Point(25, _whiteWireYPos), new Size(512, 77)), Color.FromArgb(210, Color.Black));
    //                    break;
    //                default:
    //                    break;
    //            }
    //        }

    //        Game.DisplayHelp("CorrectWire: " + CorrectWire, 1000);
    //        Game.DisplaySubtitle("CurrentAttempt: " + CurrentAttempt, 10);

    //        if (RAGENativeUI.WildernessCallouts.Common.IsDisabledControlPressed(0, GameControl.CursorAccept))
    //        {
    //            if (initPoint == Vector2.Zero)
    //                initPoint = GetMousePosition();

    //            Vector2 currentMousePos = GetMousePosition();
    //            g.DrawRectangle(new RectangleF(initPoint.X, initPoint.Y, currentMousePos.X - initPoint.X, currentMousePos.Y - initPoint.Y), Color.FromArgb(100, Color.Red));
    //        }
    //        else if (initPoint != Vector2.Zero)
    //            initPoint = Vector2.Zero;
    //    }


    //    Vector2 initPoint = Vector2.Zero;

    //    public Wire GetCurrentSelectedWire()
    //    {
    //        if (IsMouseInBounds(new Point(25, _redWireYPos), new Size(512, 77)))
    //            return Wire.Red;
    //        else if (IsMouseInBounds(new Point(25, _greenWireYPos), new Size(512, 77)))
    //            return Wire.Green;
    //        else if (IsMouseInBounds(new Point(25, _blueWireYPos), new Size(512, 77)))
    //            return Wire.Blue;
    //        else if (IsMouseInBounds(new Point(25, _whiteWireYPos), new Size(512, 77)))
    //            return Wire.White;
    //        else
    //            return Wire.None;
    //    }

    //    public enum Wire
    //    {
    //        None,
    //        Red,
    //        Green,
    //        Blue,
    //        White,
    //    }

    //    public enum Attempt
    //    {
    //        None,
    //        First,
    //        Second,
    //        Third,
    //        Fourth,
    //    }


    //    private static bool IsMouseInBounds(Point topLeft, Size boxSize)
    //    {
    //        SizeF res = GetScreenResolutionMantainRatio();

    //        int mouseX = Convert.ToInt32(Math.Round(NativeFunction.CallByName<float>("GET_CONTROL_NORMAL", 0, (int)GameControl.CursorX) * res.Width));
    //        int mouseY = Convert.ToInt32(Math.Round(NativeFunction.CallByName<float>("GET_CONTROL_NORMAL", 0, (int)GameControl.CursorY) * res.Height));

    //        return (mouseX >= topLeft.X && mouseX <= topLeft.X + boxSize.Width)
    //               && (mouseY > topLeft.Y && mouseY < topLeft.Y + boxSize.Height);
    //    }

    //    private static Vector2 GetMousePosition()
    //    {
    //        SizeF res = GetScreenResolutionMantainRatio();

    //        int mouseX = Convert.ToInt32(Math.Round(NativeFunction.CallByName<float>("GET_CONTROL_NORMAL", 0, (int)GameControl.CursorX) * res.Width));
    //        int mouseY = Convert.ToInt32(Math.Round(NativeFunction.CallByName<float>("GET_CONTROL_NORMAL", 0, (int)GameControl.CursorY) * res.Height));

    //        return new Vector2(mouseX, mouseY);
    //    }

    //    private static SizeF GetScreenResolutionMantainRatio()
    //    {
    //        int screenw = Game.Resolution.Width;
    //        int screenh = Game.Resolution.Height;
    //        const float height = 1080f;
    //        float ratio = (float)screenw / screenh;
    //        var width = height * ratio;

    //        return new SizeF(width, height);
    //    }

    //    //private static Texture GetTextureFromEmbeddedResource(string resourceName)
    //    //{
    //    //    System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
    //    //    System.IO.Stream stream = assembly.GetManifestResourceStream(resourceName);
    //    //    string tempPath = System.IO.Path.GetTempFileName();
    //    //    using (System.IO.FileStream fs = new System.IO.FileStream(tempPath, System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.Write))
    //    //    {
    //    //        fs.SetLength(0);
    //    //        byte[] buffer = new byte[stream.Length];
    //    //        stream.Read(buffer, 0, buffer.Length);
    //    //        fs.Write(buffer, 0, buffer.Length);
    //    //    }

    //    //    return Game.CreateTextureFromFile(tempPath);
    //    //}


    //    private static void DisableAllGameControls()
    //    {
    //        foreach (GameControl control in Enum.GetValues(typeof(GameControl)))
    //        {
    //            NativeFunction.CallByName<uint>("DISABLE_CONTROL_ACTION", 0, (int)control, 1);
    //        }
    //    }

    //    private static void EnableAllGameControls()
    //    {
    //        foreach (GameControl control in Enum.GetValues(typeof(GameControl)))
    //        {
    //            NativeFunction.CallByName<uint>("ENABLE_CONTROL_ACTION", 0, (int)control, 1);
    //        }
    //    }
    //}
//}
