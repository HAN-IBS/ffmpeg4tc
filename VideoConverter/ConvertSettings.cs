namespace VideoConverter
{
    using System;

    public class ConvertSettings : OutputSettings
    {
        public bool AppendSilentAudioStream;
        public float? Seek = null;
        public string CustomInputArgs;
    }
}

