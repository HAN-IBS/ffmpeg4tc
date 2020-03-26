namespace VideoConverter
{
    using System;
    using System.IO;
    using System.Runtime.CompilerServices;

    internal class Media
    {
        public string Filename { get; set; }

        public string Format { get; set; }

        public Stream DataStream { get; set; }
    }
}

