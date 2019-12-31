using System;

namespace YetAnotherLosslessCutter.FFProbe
{
#pragma warning disable CS8618 // Non-nullable field is uninitialized.

    public sealed class MediaInfo
    {

        public StreamInfo[] Streams { get; set; }


        public TimeSpan Duration { get; set; }




        public class StreamInfo
        {


            public string Index { get; set; }


            public string CodecName { get; set; }


            public string CodecLongName { get; set; }

            public string CodecType { get; set; }


            public string PixelFormat { get; set; }


            public int Width { get; set; }


            public int Height { get; set; }


            public int SampleRate { get; set; }


            public string ChannelLayout { get; set; }


            public long BitRate { get; set; }


            public float FrameRate { get; set; }

        }
    }
#pragma warning restore CS8618 // Non-nullable field is uninitialized.
}
