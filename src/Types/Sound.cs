namespace WildernessCallouts.Types
{
    using Rage;
    using Rage.Native;

    internal class Sound
    {
        public int Id { get; set; }

        public Sound(int id)
        {
            this.Id = id;
        }
        public Sound() : this(GetId()) { }

        public void Play(string soundName, string setName, bool p3 = false, int p4 = 0, bool p5 = true)
        {
            if (setName != null) NativeFunction.Natives.PLAY_SOUND(this.Id, soundName, setName, p3, p4, p5);
            else NativeFunction.Natives.PLAY_SOUND(this.Id, soundName, 0, p3, p4, p5);
        }

        public void PlayFrontend(string soundName, string setName, bool p3 = false)
        {
            if (setName != null) NativeFunction.Natives.PLAY_SOUND_FRONTEND(this.Id, soundName, setName, p3);
            else NativeFunction.Natives.PLAY_SOUND_FRONTEND(this.Id, soundName, 0, p3);
        }

        public void PlayFromEntity(string soundName, string setName, Entity entity, bool p4 = false, int p5 = 0)
        {
            if (setName != null) NativeFunction.Natives.PLAY_SOUND_FROM_ENTITY(this.Id, soundName, entity, setName, p4, p5);
            else NativeFunction.Natives.PLAY_SOUND_FROM_ENTITY(this.Id, soundName, entity, 0, p4, p5);
        }

        public void PlayFromPosition(string soundName, string setName, Vector3 position, bool p6 = false, int p7 = 0, bool p8 = false)
        {
            if (setName != null) NativeFunction.Natives.PLAY_SOUND_FROM_COORD(this.Id, soundName, position.X, position.Y, position.Z, setName, p6, p7, p8);
            else NativeFunction.Natives.PLAY_SOUND_FROM_COORD(this.Id, soundName, position.X, position.Y, position.Z, 0, p6, p7, p8);
        }

        public void SetVariable(string variableName, float value)
        {
            NativeFunction.Natives.SET_VARIABLE_ON_SOUND(this.Id, variableName, value);
        }

        public void Stop()
        {
            NativeFunction.Natives.STOP_SOUND( this.Id);
        }

        public bool HasFinished()
        {
            return NativeFunction.Natives.HAS_SOUND_FINISHED<bool>(this.Id);
        }

        public void ReleaseId()
        {
            NativeFunction.Natives.RELEASE_SOUND_ID(this.Id);
            this.Id = -1;
        }

        public static int GetId()
        {
            return NativeFunction.Natives.GET_SOUND_ID<int>();
        }

        public static bool RequestMissionAudioBank(string audioBankName, bool p1 = true)
        {
            return NativeFunction.Natives.REQUEST_MISSION_AUDIO_BANK<bool>(audioBankName, p1);
        }
        public static bool RequestAmbientAudioBank(string audioBankName, bool p1 = true)
        {
            return NativeFunction.Natives.REQUEST_AMBIENT_AUDIO_BANK<bool>( audioBankName, p1);
        }
        public static bool RequestScriptAudioBank(string audioBankName, bool p1 = true)
        {
            return NativeFunction.Natives.REQUEST_SCRIPT_AUDIO_BANK<bool>(audioBankName, p1);
        }
    }
}
