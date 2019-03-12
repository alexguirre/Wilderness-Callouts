using System;

namespace WildernessCallouts.Callouts
{
    // System
    using System.Drawing;
    using System.Windows.Forms;

    // RPH
    using Rage;

    // LSPDFR
    using LSPD_First_Response.Mod.Callouts;

    // RNUI
    using RAGENativeUI;
    using RAGENativeUI.Elements;

    // Wilderness Callouts
    using WildernessCallouts.Menus;
    using WildernessCallouts.Types;

    [CalloutInfo("MurderInvestigation", CalloutProbability.Medium)]
    internal partial class MurderInvestigation : CalloutBase
    {
        //SET_ENTITY_RENDER_SCORCHED(Entity entity, BOOL toggle) render burn
        
        private bool _isPlayerOnScene = false;
        public bool IsPlayerOnScene
        {
            get
            {
                return _isPlayerOnScene;
            }
            private set
            {
                _isPlayerOnScene = value;
                timeBeforeDisplayHelp = Game.GameTime + intervalBeforeDisplayHelp;
            }
        }
        private const int intervalBeforeDisplayHelp = 90000;

        public string HelpText { get; private set; } = "<NOT IMPLEMENTED>";

        private uint timeBeforeDisplayHelp;
        private bool canDisplayHelp = false;

        public CalloutMenu Menu { get; private set; }
        public ReportHandler Report { get; private set; }

        private VariationBaseClass Varitation { get; set; }

        public override bool OnBeforeCalloutDisplayed()
        {
            bool baseSuccess = base.OnBeforeCalloutDisplayed();
            if (baseSuccess == false)
                return false;


            Varitation = VariationBaseClass.GetRandomVariation();

            bool success = Varitation.OnBeforeCalloutDisplayed(this);

            if (success == false)
                return false;

            return true;
        }

        
        public override bool OnCalloutAccepted()
        {
            Report = new ReportHandler();
            Menu = new CalloutMenu(this);

            bool baseSuccess = base.OnCalloutAccepted();
            if (baseSuccess == false)
                return false;

            bool success = Varitation.OnCalloutAccepted(this);

            if (success == false)
                return false;
            return true;
        }

        
        public override void Process()
        {
            Varitation.Process(this);

            if (IsPlayerOnScene)
            {
                if(Game.GameTime >= timeBeforeDisplayHelp)
                {
                    Game.DisplayHelp("Do you need help with the investigation? Press ~b~0~s~ to get some help.");
                    timeBeforeDisplayHelp = Game.GameTime + intervalBeforeDisplayHelp;
                    canDisplayHelp = true;
                }
                if(canDisplayHelp && Game.IsKeyDown(Keys.D0))
                {
                    Game.DisplayHelp(HelpText);
                }
            }

            if (Controls.ToggleCalloutsMenu.IsJustPressed())
                Menu.MainMenu.Visible = !Menu.MainMenu.Visible;

            Report.Draw();

            if (Game.IsKeyDownRightNow(Keys.D9))
            {
                Report.IsVisible = true;
            }
            else
            {
                Report.IsVisible = false;
            }

            base.Process();
        }


        protected override void CleanUp()
        {
            Varitation?.CleanUp(this);
            Menu?.CleanUp();
        }


        internal abstract class VariationBaseClass
        {
            public static VariationBaseClass GetRandomVariation()
            {
#if DEBUG
                int index = 2;
#else
                int index = Globals.Random.Next(1, 3);
#endif
                switch (index)
                {
                    default:
                    case 1:
                        return new Variation1();
                    case 2:
                        return new Variation2();
                }
            }

            public abstract bool OnBeforeCalloutDisplayed(MurderInvestigation ownerCallout);
            public abstract bool OnCalloutAccepted(MurderInvestigation ownerCallout);
            public abstract void Process(MurderInvestigation ownerCallout);
            public abstract void CleanUp(MurderInvestigation ownerCallout);
        }


        internal class ReportHandler
        {
            public ResRectangle Background { get; }
            public ResText Title { get; }
            public ResText Text { get; }

            public bool IsVisible { get; set; }

            public ReportHandler()
            {
                Background = new ResRectangle(new Point(350, 45), new Size(Game.Resolution.Width - 475, Game.Resolution.Height - 120), Color.FromArgb(200, 10, 10, 10));
                Title = new ResText("CAD: Murder Call Report", new Point(360, 50), 0.975f, Color.White, RAGENativeUI.Common.EFont.ChaletLondon, ResText.Alignment.Left);
                Text = new ResText("", new Point(355, 80), 0.4225f, Color.White, RAGENativeUI.Common.EFont.ChaletLondon, ResText.Alignment.Left);
                AddTextToReport("[" + DateTime.UtcNow.ToShortDateString() + "  " + DateTime.UtcNow.ToLongTimeString() + "]" + "~n~>Call received." + "~n~>Officer " + Settings.General.Name + " en route.");
            }

            public void Draw()
            {
                if (!IsVisible)
                    return;

                Background.Draw();
                Title.Draw();
                Text.Draw();
            }

            public void AddTextToReport(string text)
            {
                Text.Caption += "~n~" + text.Replace(Environment.NewLine, "~n~>") + "~n~";
            }
        }

        internal class CalloutMenu
        {
            public UIMenu MainMenu { get; }
            public UIMenu EvidencesSubMenu { get; }

            public CalloutMenu(MurderInvestigation owner)
            {
                MainMenu = new UIMenu("Wilderness Callouts", "Murder Investigation");
                MainMenu.AllowCameraMovement = true;
                MainMenu.MouseControlsEnabled = false;
                MenuCommon.Pool.Add(MainMenu);
                UIMenuCheckboxItem showReportCheckboxItem = new UIMenuCheckboxItem("Show CAD Report", false, "If checked displays the CAD report with information about this callout");
                showReportCheckboxItem.CheckboxEvent += (sender, itemChecked) => { owner.Report.IsVisible = itemChecked; };

                EvidencesSubMenu = new UIMenu("Wilderness Callouts", "Murder Investigation: Evidences");
                EvidencesSubMenu.AllowCameraMovement = true;
                EvidencesSubMenu.MouseControlsEnabled = false;
                MenuCommon.Pool.Add(EvidencesSubMenu);

                UIMenuItem evidencesItem = new UIMenuItem("Evidences", "Opens the evidences menu");

                MainMenu.AddItem(showReportCheckboxItem);
                MainMenu.AddItem(evidencesItem);
                MainMenu.RefreshIndex();

                MainMenu.BindMenuToItem(EvidencesSubMenu, evidencesItem);
            }

            public void CleanUp()
            {
                if (MainMenu != null)
                {
                    MenuCommon.Pool.Remove(MainMenu);
                }
                if (EvidencesSubMenu != null)
                {
                    MenuCommon.Pool.Remove(EvidencesSubMenu);
                }
            }

            public void AddEvidence(string label, string description, Action action)
            {
                UIMenuItem item = new UIMenuItem(label, description);
                item.Activated += (sender, selectedItem) => { GameFiber.StartNew(delegate { action.Invoke(); }); };
                EvidencesSubMenu.AddItem(item);
                EvidencesSubMenu.RefreshIndex();
            }
        }
    }
}
