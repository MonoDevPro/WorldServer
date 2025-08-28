using System.Text.Json;
using System.Text.Json.Serialization;
using Simulation.Core.Abstractions.Commons;

namespace Simulation.Core.Utilities.Serialization
{
    // MapId, CharId, etc. são representados como um número no JSON (ex: "MapId": 1)
    public class IntWrapperConverter<T> : JsonConverter<T> where T : struct
    {
        private readonly Func<int, T> _ctor;
        public IntWrapperConverter(Func<int, T> ctor) => _ctor = ctor;

        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var v))
                return _ctor(v);

            // se o JSON for { "Value": 1 } possível também:
            if (reader.TokenType == JsonTokenType.StartObject)
            {
                using var doc = JsonDocument.ParseValue(ref reader);
                if (doc.RootElement.TryGetProperty("Value", out var prop) && prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out var vv))
                    return _ctor(vv);
            }

            throw new JsonException($"Cannot convert JSON token to {typeof(T).Name}");
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            // tenta ler propriedade "Value" via reflection — fallback: escrever int diretamente
            var prop = value.GetType().GetProperty("Value");
            if (prop != null)
            {
                var num = (int)prop.GetValue(value)!;
                writer.WriteNumberValue(num);
                return;
            }

            throw new JsonException($"Cannot serialize {typeof(T).Name}");
        }
    }

    // GameCoord { X, Y } converter: aceita objeto { "X":1,"Y":2 } ou array [1,2]
    public class GameCoordConverter : JsonConverter<GameCoord>
    {
        public override GameCoord Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.StartArray)
            {
                reader.Read();
                int x = reader.GetInt32();
                reader.Read();
                int y = reader.GetInt32();
                reader.Read(); // EndArray
                return new GameCoord(x, y);
            }

            if (reader.TokenType == JsonTokenType.StartObject)
            {
                using var doc = JsonDocument.ParseValue(ref reader);
                var root = doc.RootElement;
                int x = root.GetProperty("X").GetInt32();
                int y = root.GetProperty("Y").GetInt32();
                return new GameCoord(x, y);
            }

            throw new JsonException("Invalid JSON for GameCoord");
        }

        public override void Write(Utf8JsonWriter writer, GameCoord value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("X", value.X);
            writer.WriteNumber("Y", value.Y);
            writer.WriteEndObject();
        }
    }

    // GameDirection similar to GameCoord but with floats or ints depending seu tipo
    public class GameDirectionConverter : JsonConverter<GameDirection>
    {
        public override GameDirection Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.StartArray)
            {
                reader.Read();
                int x = reader.GetInt32();
                reader.Read();
                int y = reader.GetInt32();
                reader.Read();
                return new GameDirection(x, y);
            }

            if (reader.TokenType == JsonTokenType.StartObject)
            {
                using var doc = JsonDocument.ParseValue(ref reader);
                var root = doc.RootElement;
                int x = root.GetProperty("X").GetInt32();
                int y = root.GetProperty("Y").GetInt32();
                return new GameDirection(x, y);
            }

            throw new JsonException("Invalid JSON for GameDirection");
        }

        public override void Write(Utf8JsonWriter writer, GameDirection value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("X", value.X);
            writer.WriteNumber("Y", value.Y);
            writer.WriteEndObject();
        }
    }

    public class GameSizeConverter : JsonConverter<GameSize>
    {
        public override GameSize Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.StartObject)
            {
                using var doc = JsonDocument.ParseValue(ref reader);
                var root = doc.RootElement;
                int w = root.GetProperty("Width").GetInt32();
                int h = root.GetProperty("Height").GetInt32();
                return new GameSize(w, h);
            }
            throw new JsonException("Invalid JSON for GameSize");
        }

        public override void Write(Utf8JsonWriter writer, GameSize value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("Width", value.Width);
            writer.WriteNumber("Height", value.Height);
            writer.WriteEndObject();
        }
    }
}
