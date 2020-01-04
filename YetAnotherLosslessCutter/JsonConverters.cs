using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Linq;

namespace YetAnotherLosslessCutter
{
    // https://github.com/dotnet/corefx/issues/38641
    sealed class TimeSpanConverter : JsonConverter<TimeSpan>
    {
        public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var value = doc.RootElement.EnumerateObject().Single(e => e.Name.Equals("Ticks"));
            return TimeSpan.FromTicks(value.Value.GetInt64());
        }

        public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }
}
