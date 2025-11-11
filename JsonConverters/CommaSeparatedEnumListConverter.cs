using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WorkingSpaces.JsonConverters
{
    public class CommaSeparatedEnumListConverter<T> : JsonConverter<List<T>> where T : struct, Enum
    {
        public override List<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var str = reader.GetString();
            var list = new List<T>();

            if (!string.IsNullOrEmpty(str))
            {
                foreach (var item in str.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (Enum.TryParse<T>(item.Trim(), ignoreCase: true, out var value))
                        list.Add(value);
                }
            }

            return list;
        }

        public override void Write(Utf8JsonWriter writer, List<T> value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(string.Join(", ", value));
        }
    }
}
