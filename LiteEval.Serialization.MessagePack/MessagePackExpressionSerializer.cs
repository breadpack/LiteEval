using LiteEval;
using MessagePack;

namespace LiteEval.Serialization.MessagePack {
    public class MessagePackExpressionSerializer : IExpressionSerializer {
        private readonly MessagePackSerializerOptions _options;

        public MessagePackExpressionSerializer() {
            _options = MessagePackSerializerOptions.Standard.UseLiteEval();
        }

        public byte[] Serialize(Expression expression) {
            return MessagePackSerializer.Serialize(expression, _options);
        }

        public Expression Deserialize(byte[] data) {
            return MessagePackSerializer.Deserialize<Expression>(data, _options);
        }
    }
}
