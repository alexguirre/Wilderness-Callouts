namespace WildernessCallouts.Callouts
{
    using Rage;
    using LSPD_First_Response.Mod.Callouts;
    using System.Drawing;
    using WildernessCallouts;
    using WildernessCallouts.Types;

    [CalloutInfo("AnimalAttack", CalloutProbability.Medium)]
    internal class AnimalAttack : CalloutBase
    {
        private Ped attackedPed;
        private Ped animal;

        private Blip attackedPedBlip;
        private Blip animalBlip;

        private Vector3 spawnPointOnStreet;

        private static string animalModel = "a_c_mtlion";

        private static string[] pedPhrases = { "Help!", "Anyone! Help!" };

        bool hasTalked = false;

        public override bool OnBeforeCalloutDisplayed()
        {
            float f = 200f;
            for (int i = 0; i < 10; i++)
            {
                GameFiber.Yield();

                spawnPointOnStreet = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.AroundPosition(f + 150f)).GetSafeCoordinatesForPed();

                if (spawnPointOnStreet != Vector3.Zero)
                {
                    break;
                }
            }
            if (spawnPointOnStreet == Vector3.Zero) spawnPointOnStreet = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.AroundPosition(500f));

            if (Vector3.Distance(Game.LocalPlayer.Character.Position, spawnPointOnStreet) < 25.0f) return false;

            EWorldArea spawnZone = WorldZone.GetArea(spawnPointOnStreet);
            if (spawnZone == EWorldArea.Los_Santos) return false;

            attackedPed = new Ped(spawnPointOnStreet);
            if (!attackedPed.Exists()) return false;

            Vector3 animalSpawnPos = spawnPointOnStreet.AroundPosition(25.0f);
            for (int i = 0; i < 10; i++)
            {
                if (Vector3.Distance(animalSpawnPos, attackedPed.Position) > 8.75f) break;
                animalSpawnPos = spawnPointOnStreet.AroundPosition(25.0f + i);
            }
            animal = new Ped(animalModel, animalSpawnPos.ToGround(), 0.0f);
            if (!animal.Exists()) return false;

            attackedPed.RelationshipGroup = new RelationshipGroup("PED");
            animal.RelationshipGroup = "COUGAR";

            this.ShowCalloutAreaBlipBeforeAccepting(spawnPointOnStreet, 35f);
            this.AddMinimumDistanceCheck(20.0f, attackedPed.Position);

            this.CalloutMessage = "Animal attack";
            this.CalloutPosition = spawnPointOnStreet;

            int audioRnd = Globals.Random.Next(0, 2);
            if (audioRnd == 0) LSPD_First_Response.Mod.API.Functions.PlayScannerAudioUsingPosition("CITIZENS_REPORT CRIME_VICIOUS_ANIMAL_ON_THE_LOOSE IN_OR_ON_POSITION UNITS_RESPOND_CODE_03", spawnPointOnStreet);
            if (audioRnd == 1) LSPD_First_Response.Mod.API.Functions.PlayScannerAudioUsingPosition("CITIZENS_REPORT CRIME_VICIOUS_ANIMAL_ON_THE_LOOSE IN_OR_ON_POSITION UNITS_RESPOND_CODE_99", spawnPointOnStreet);

            return base.OnBeforeCalloutDisplayed();
        }

        public override bool OnCalloutAccepted()
        {
            Game.SetRelationshipBetweenRelationshipGroups("COUGAR", "PED", Relationship.Hate);
            Game.SetRelationshipBetweenRelationshipGroups("COUGAR", "PLAYER", Relationship.Hate);
            Game.SetRelationshipBetweenRelationshipGroups("PED", "PLAYER", Relationship.Companion);

            attackedPed.MaxHealth = 200;
            
            attackedPedBlip = new Blip(attackedPed);
            attackedPedBlip.Color = Color.ForestGreen;

            animalBlip = new Blip(animal);
            animalBlip.Color = Color.DarkRed;

            attackedPed.ReactAndFlee(animal);
            animal.AttackPed(attackedPed);

            return base.OnCalloutAccepted();
        }

        public override void OnCalloutNotAccepted()
        {
            if (attackedPed.Exists()) attackedPed.Delete();
            if (animal.Exists()) animal.Delete();
            if (animalBlip.Exists()) animalBlip.Delete();
            if (attackedPedBlip.Exists()) attackedPedBlip.Delete();

            base.OnCalloutNotAccepted();
        }

        public override void Process()
        {
            if (attackedPed.IsDead)
            {
                Game.DisplayNotification("The person has died");
            }

            if (Vector3.Distance(Game.LocalPlayer.Character.Position, attackedPed.Position) < 30.0f || Vector3.Distance(Game.LocalPlayer.Character.Position, animal.Position) < 30.0f)
            {
                if (attackedPed.IsAlive && !hasTalked)
                {
                    attackedPed.PlayAmbientSpeech(Globals.Random.Next(2) == 1 ? Globals.Random.Next(2) == 1 ? Globals.Random.Next(2) == 1 ? Speech.GENERIC_SHOCKED_HIGH : Speech.GENERIC_SHOCKED_MED :  Speech.GENERIC_FRIGHTENED_HIGH : Speech.GENERIC_FRIGHTENED_MED);
                    Game.DisplaySubtitle("~b~Attacked person: ~w~" + pedPhrases[Globals.Random.Next(pedPhrases.Length)], 2500);
                }
                hasTalked = true;
            }

            if (animal.Exists())
            {
                if (animal.IsDead)
                {
                    if (attackedPed.IsAlive)
                    {
                        attackedPed.PlayAmbientSpeech(Speech.GENERIC_THANKS);
                        Game.DisplaySubtitle("~b~Attacked person: ~w~Thanks!", 2500);
                    }
                    this.End();

                }
            }
            else if (!animal.Exists())
                this.End();

            base.Process();
        }


        public override void End()
        {
            if (animalBlip.Exists()) animalBlip.Delete();
            if (animal.Exists()) animal.Dismiss();
            if (attackedPed.Exists()) attackedPed.Dismiss();
            if (attackedPedBlip.Exists()) attackedPedBlip.Delete();

            base.End();
        }

        protected override void CleanUp()
        {
            // TODO: implement CleanUp()
        }
    }
}
