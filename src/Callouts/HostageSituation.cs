namespace WildernessCallouts.Callouts
{
    using Rage;
    using Rage.Native;
    using LSPD_First_Response.Mod.API;
    using LSPD_First_Response.Mod.Callouts;
    using System.Drawing;
    using WildernessCallouts.Types;

    [CalloutInfo("HostageSituation", CalloutProbability.Medium)]
    internal class HostageSituation : CalloutBase
    {
        const string GrabHostageAnimDict = "misssagrab_inoffice";
        const string SuspectGrabAnimName = "hostage_loop";
        const string HostageGrabbedAnimName = "hostage_loop_mrk";

        Ped suspect = null;
        Ped hostage = null;

        Blip suspectBlip = null;
        Blip hostageBlip = null;

        Vector3 spawnPoint;

        LHandle pursuit;
        bool isPursuitInitiated = false;

        static WeaponAsset[] pistols = { "WEAPON_PISTOL50", "WEAPON_PISTOL", "WEAPON_COMBATPISTOL", "WEAPON_APPISTOL", "WEAPON_VINTAGEPISTOL" };

        bool arePedsPlayingAnim = true;

        EHostageSituationState state;

        public override bool OnBeforeCalloutDisplayed()
        {
            spawnPoint = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.AroundPosition(215.0f));
            while (spawnPoint.DistanceTo(Game.LocalPlayer.Character.Position) < 30.0f)
            {
                spawnPoint = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.AroundPosition(210.0f)).GetSafeCoordinatesForPed();
                GameFiber.Yield();
            }
            if (spawnPoint == Vector3.Zero) spawnPoint = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.AroundPosition(195.0f));

            suspect = new Ped(spawnPoint);
            if (!suspect.Exists()) return false;
            hostage = new Ped(spawnPoint + suspect.ForwardVector * 0.9f);
            if (!hostage.Exists()) return false; 

            suspect.BlockPermanentEvents = true;
            hostage.BlockPermanentEvents = true;

            hostage.Heading = suspect.Heading = MathHelper.GetRandomSingle(1f, 359f);

            RelationshipGroup hostageRelation = new RelationshipGroup("HOSTAGE");
            hostage.RelationshipGroup = hostageRelation;
            Game.SetRelationshipBetweenRelationshipGroups(hostageRelation, "COP", Relationship.Companion);
            Game.SetRelationshipBetweenRelationshipGroups(hostageRelation, "PLAYER", Relationship.Companion);
            Game.SetRelationshipBetweenRelationshipGroups("COP", hostageRelation, Relationship.Companion);

            RelationshipGroup suspectRelation = new RelationshipGroup("SUSPECT");
            suspect.RelationshipGroup = suspectRelation;
            Game.SetRelationshipBetweenRelationshipGroups(suspectRelation, "COP", Relationship.Dislike);
            Game.SetRelationshipBetweenRelationshipGroups(suspectRelation, "PLAYER", Relationship.Dislike);
            Game.SetRelationshipBetweenRelationshipGroups("COP", suspectRelation, Relationship.Dislike);

            this.ShowCalloutAreaBlipBeforeAccepting(spawnPoint, 17.5f);
            this.AddMinimumDistanceCheck(15.0f, suspect.Position);

            // Set up our callout message and location
            this.CalloutMessage = "Hostage situation";
            this.CalloutPosition = spawnPoint;

            //Play the police scanner audio for this callout (available as of the 0.2a API)
            Functions.PlayScannerAudioUsingPosition("CITIZENS_REPORT CRIME_ASSAULT_CIVILIAN IN_OR_ON_POSITION UNITS_RESPOND_CODE_03", spawnPoint);   

            return base.OnBeforeCalloutDisplayed();
        }

        public override bool OnCalloutAccepted()
        {
            state = EHostageSituationState.EnRoute;
            suspect.Tasks.PlayAnimation(GrabHostageAnimDict, SuspectGrabAnimName, 6.0f, AnimationFlags.Loop);
            hostage.Tasks.PlayAnimation(GrabHostageAnimDict, HostageGrabbedAnimName, 6.0f, AnimationFlags.Loop);
            arePedsPlayingAnim = true;
            suspect.Inventory.GiveNewWeapon(pistols.GetRandomElement(true), 999, true);
            hostage.Position = suspect.GetOffsetPosition(new Vector3(0f, 0.14445f, 0f));

            hostageBlip = new Blip(hostage);
            hostageBlip.Color = Color.LightBlue;
            hostageBlip.Scale = 0.75f;

            suspectBlip = new Blip(suspect);

            return base.OnCalloutAccepted();
        }

        public override void OnCalloutNotAccepted()
        {
            if (suspect.Exists()) suspect.Delete();
            if (hostage.Exists()) hostage.Delete();
            if (suspectBlip.Exists()) suspectBlip.Delete();
            if (hostageBlip.Exists()) hostageBlip.Delete();
            if (isPursuitInitiated) Functions.ForceEndPursuit(pursuit);
            base.OnCalloutNotAccepted();
        }

        public override void Process()
        {
            if (state == EHostageSituationState.EnRoute && Vector3.Distance(Game.LocalPlayer.Character.Position, suspect.Position) < 16.5f)
            {
                state = EHostageSituationState.OnScene;
                Scenario();
            }

            if (((!suspect.IsPlayingAnimation(GrabHostageAnimDict, SuspectGrabAnimName) || !hostage.IsPlayingAnimation(GrabHostageAnimDict, HostageGrabbedAnimName)) || (suspect.IsRagdoll || hostage.IsRagdoll)) && arePedsPlayingAnim)
            {
                hostage.Tasks.Clear();
                suspect.Tasks.Clear();
                hostage.ReactAndFlee(suspect);
                GameFiber.Sleep(1000);
                if (Globals.Random.Next(4) <= 2) suspect.Tasks.FightAgainst(Game.LocalPlayer.Character);
                else suspect.ReactAndFlee(Game.LocalPlayer.Character);
                arePedsPlayingAnim = false;
            }

            if (!suspect.Exists() || suspect.IsDead || Functions.IsPedArrested(suspect)) this.End();

            base.Process();
        }

        public override void End()
        {
            state = EHostageSituationState.End;

            if (suspect.Exists()) suspect.Dismiss();
            if (hostage.Exists()) hostage.Dismiss();
            if (suspectBlip.Exists()) suspectBlip.Delete();
            if (hostageBlip.Exists()) hostageBlip.Delete();
            if (isPursuitInitiated) Functions.ForceEndPursuit(pursuit);
            base.End();
        }

        protected override void CleanUp()
        {
            // TODO: implement CleanUp()
        }


        public void Scenario()
        {
            int situation = Globals.Random.Next(0, 3);
            state = EHostageSituationState.Scenario;

            Logger.LogTrivial(this.GetType().Name, "Scenario: " + situation);

            hostage.PlayAmbientSpeech(Globals.Random.Next(2) == 1 ? Globals.Random.Next(2) == 1 ? Globals.Random.Next(2) == 1 ? Speech.GENERIC_SHOCKED_HIGH : Speech.GENERIC_SHOCKED_MED : Speech.GENERIC_FRIGHTENED_HIGH : Speech.GENERIC_FRIGHTENED_MED);
            suspect.PlayAmbientSpeech(Globals.Random.Next(2) == 1 ? Globals.Random.Next(2) == 1 ? Speech.GENERIC_FUCK_YOU : Speech.GENERIC_INSULT_HIGH : Speech.GENERIC_INSULT_MED);
            if (situation == 0)
            {
                GameFiber.StartNew(delegate
                {
                    if (suspect.IsAlive) Game.DisplaySubtitle("~r~Suspect: ~s~Get out of here, this isn't your problem!", 7000);
                    GameFiber.Sleep(Globals.Random.Next(4250, 17500));
                    if (suspect.Exists() && suspect.IsAlive && !Functions.IsPedArrested(suspect) && !Functions.IsPedGettingArrested(suspect) && arePedsPlayingAnim)
                    {
                        NativeFunction.Natives.SET_PED_SHOOTS_AT_COORD(suspect, 0.0f, 0.0f, 0.0f, 0);
                        WildernessCallouts.Common.StartParticleFxNonLoopedOnEntity("scr_solomon3", "scr_trev4_747_blood_impact", hostage, new Vector3(0.0f, 0.0f, 0.6f), new Rotator(90.0f, 0.0f, 0.0f), 0.235f);

                        hostage.Kill();
                        hostage.Tasks.Clear();
                        suspect.Tasks.Clear();
                        if (Globals.Random.Next(4) <= 2) suspect.Tasks.FightAgainst(Game.LocalPlayer.Character);
                        else suspect.ReactAndFlee(Game.LocalPlayer.Character);

                        //suspect.BlockPermanentEvents = false;
                        //hostage.BlockPermanentEvents = false;

                        pursuit = Functions.CreatePursuit();
                        Functions.AddPedToPursuit(pursuit, suspect);
                        isPursuitInitiated = true;
                    }
                });
            }
            else if (situation == 1)
            {
                GameFiber.StartNew(delegate
                {
                    if (suspect.Exists() && suspect.IsAlive && !Functions.IsPedArrested(suspect) && !Functions.IsPedGettingArrested(suspect) && arePedsPlayingAnim)
                    {
                        GameFiber.Sleep(450);
                        Game.DisplaySubtitle("~r~Suspect: ~s~Fuck!", 7000);

                        GameFiber.Sleep(795);
                        //suspect.Tasks.PlayAnimation("reaction@shove", "shove_var_a", 8.0f, AnimationFlags.None);
                        suspect.Tasks.Clear();
                        GameFiber.Sleep(100);
                        //NativeFunction.CallByName<uint>("TASK_PLAY_ANIM", suspect, "reaction@shove", "shove_var_a", 8f, -4f, 2000, 48, 0f, false, false, false);
                        suspect.Tasks.PlayAnimation("reaction@shove", "shove_var_a", -1, 2.1225f, 1.0f, 0.0f, AnimationFlags.None);
                        GameFiber.Sleep(750);
                        GameFiber.StartNew(delegate
                        {
                            hostage.Tasks.PlayAnimation("dead@fall", "dead_land_up", -1, 2.28f, 1.0f, 0.0f, AnimationFlags.None).WaitForCompletion();
                            NativeFunction.Natives.SET_PED_TO_RAGDOLL(hostage, 3500, 5000, 0, true, true, false);
                            GameFiber.Sleep(4000);
                            hostage.ReactAndFlee(suspect);
                        });

                        suspect.Tasks.Clear();
                        if (Globals.Random.Next(4) <= 2) suspect.Tasks.FightAgainst(Game.LocalPlayer.Character);
                        else suspect.ReactAndFlee(Game.LocalPlayer.Character);


                        pursuit = Functions.CreatePursuit();
                        Functions.AddPedToPursuit(pursuit, suspect);
                        isPursuitInitiated = true;
                    }
                });
            }
            else if (situation == 2)
            {
                GameFiber.StartNew(delegate
                {
                    if (suspect.IsAlive) Game.DisplaySubtitle("~r~Suspect: ~s~Stay away or I will shoot!", 7000);

                    while (Vector3.Distance(Game.LocalPlayer.Character.Position, suspect.Position) > 8.0f && state != EHostageSituationState.End)
                        GameFiber.Yield();

                    if (state != EHostageSituationState.End)
                    {
                        if (suspect.Exists() && suspect.IsAlive && !Functions.IsPedArrested(suspect) && !Functions.IsPedGettingArrested(suspect) && arePedsPlayingAnim)
                        {
                            NativeFunction.Natives.SET_PED_SHOOTS_AT_COORD(suspect, 0.0f, 0.0f, 0.0f, 0);
                            WildernessCallouts.Common.StartParticleFxNonLoopedOnEntity("scr_solomon3", "scr_trev4_747_blood_impact", hostage, new Vector3(0.0f, 0.0f, 0.6f), new Rotator(90.0f, 0.0f, 0.0f), 0.235f);

                            hostage.Kill();
                            hostage.Tasks.Clear();
                            suspect.Tasks.Clear();
                            suspect.ReactAndFlee(Game.LocalPlayer.Character);

                            //suspect.BlockPermanentEvents = false;
                            //hostage.BlockPermanentEvents = false;

                            pursuit = Functions.CreatePursuit();
                            Functions.AddPedToPursuit(pursuit, suspect);
                            isPursuitInitiated = true;
                        }
                    }
                });
            }
        }


        public enum EHostageSituationState
        {
            EnRoute,
            OnScene,
            Scenario,
            End,
        }
    }
}
