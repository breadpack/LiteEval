using System;
using System.Buffers;
using LiteEval;
using Newtonsoft.Json;

namespace LiteEval.Serialization.Newtonsoft {
    public class ExpressionJsonConverter : JsonConverter<Expression> {
        private const int StackAllocThreshold = 256;

        public override void WriteJson(JsonWriter writer, Expression value, JsonSerializer serializer) {
            int byteCount = value.GetByteCount();
            byte[] pooled = null;
            Span<byte> buffer = byteCount <= StackAllocThreshold
                ? stackalloc byte[byteCount]
                : (pooled = ArrayPool<byte>.Shared.Rent(byteCount));
            try {
                value.TryWriteBytes(buffer, out int written);
                writer.WriteValue(Convert.ToBase64String(buffer.Slice(0, written)));
            }
            finally {
                if (pooled != null) ArrayPool<byte>.Shared.Return(pooled);
            }
        }

        public override Expression ReadJson(JsonReader reader, Type objectType, Expression existingValue,
            bool hasExistingValue, JsonSerializer serializer) {
            if (reader.TokenType == JsonToken.Null)
                return default;
            var base64 = (string)reader.Value;
            return Expression.FromBytes(Convert.FromBase64String(base64));
        }
    }
}
