using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace YetAnotherLosslessCutter.MVVM
{
    class JsonImageSourceConverter : JsonConverter<ImageSource>
    {
        public override ImageSource Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string image = reader.GetString();

            byte[] byteBuffer = Convert.FromBase64String(image);
            using var stream = new MemoryStream(byteBuffer) { Position = 0 };
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.StreamSource = stream;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        public override void Write(Utf8JsonWriter writer, ImageSource value, JsonSerializerOptions options)
        {
            byte[] data;
            JpegBitmapEncoder encoder = new JpegBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create((BitmapSource)value));
            using (MemoryStream ms = new MemoryStream())
            {
                encoder.Save(ms);
                data = ms.ToArray();
            }
            writer.WriteBase64StringValue(data);
        }

    }
}
