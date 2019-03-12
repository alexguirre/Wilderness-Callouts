namespace WildernessCallouts.Callouts
{
    using Rage;
    using LSPD_First_Response.Mod.Callouts;
    using WildernessCallouts.Types;

    internal abstract class CalloutBase : Callout
    {
        public bool HasBeenAccepted = false;
        public StaticFinalizer Finalizer { get; private set; }

        public override bool OnBeforeCalloutDisplayed()
        {
            Logger.LogTrivial(this.GetType().Name, "OnBeforeCalloutDisplayed()");
            Finalizer = new StaticFinalizer(CleanUp);

            return base.OnBeforeCalloutDisplayed();
        }

        public override bool OnCalloutAccepted()
        {
            Logger.LogTrivial(this.GetType().Name, "OnCalloutAccepted()");
            HasBeenAccepted = true;

            return base.OnCalloutAccepted();
        }

        public override void OnCalloutNotAccepted()
        {
            Logger.LogTrivial(this.GetType().Name, "OnCalloutNotAccepted()");
            if (!HasBeenAccepted)
            {
                WildernessCallouts.Common.PlayAIRespondingAudio();
            }

            if(Finalizer != null)
                Finalizer.Dispose();

            base.OnCalloutNotAccepted();
        }

        public override void Process()
        {
            //Logger.LogDebug(this.GetType().Name, "Process()");

            if (Controls.ForceCalloutEnd.IsJustPressed())
            {
                Logger.LogTrivial(this.GetType().Name, "End Forced");
                this.End();
            }

            base.Process();
        }

        public override void End()
        {
            Logger.LogTrivial(this.GetType().Name, "End()");

            if (HasBeenAccepted) WildernessCallouts.Common.EndMessage(this.CalloutMessage);

            if (Finalizer != null)
                Finalizer.Dispose();

            base.End();
        }

        /// <summary>
        /// Called in End() and OnCalloutNotAccepted()
        /// </summary>
        protected abstract void CleanUp();
    }
}
