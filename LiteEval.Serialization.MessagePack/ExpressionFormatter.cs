using System;
using System.Buffers;
using LiteEval;
using MessagePack;
using MessagePack.Formatters;

namespace LiteEval.Serialization.MessagePack {
    public class ExpressionFormatter : IMessagePackFormatter<Expression> {
        private const int StackAllocThreshold = 256;

        public void Serialize(ref MessagePackWriter writer, Expression value, MessagePackSerializerOptions options) {
            int byteCount = value.GetByteCount();
            writer.WriteBinHeader(byteCount);

            var span = writer.GetSpan(byteCount);
            value.TryWriteBytes(span, out int written);
            writer.Advance(written);
        }

        public Expression Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) {
            var sequence = reader.ReadBytes();
            if (!sequence.HasValue)
                return default;

            var value = sequence.Value;
            if (value.IsSingleSegment)
                return Expression.FromBytes(value.FirstSpan);

            // Multi-segment fallback
            int length = checked((int)value.Length);
            byte[] pooled = ArrayPool<byte>.Shared.Rent(length);
            try {
                value.CopyTo(pooled);
                return Expression.FromBytes(pooled.AsSpan(0, length));
            }
            finally {
                ArrayPool<byte>.Shared.Return(pooled);
            }
        }
    }
}
