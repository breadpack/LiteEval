using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;

namespace LiteEval.Serialization.MessagePack {
    public static class LiteEvalMessagePackExtensions {
        public static MessagePackSerializerOptions UseLiteEval(this MessagePackSerializerOptions options) {
            var resolver = CompositeResolver.Create(
                new IMessagePackFormatter[] { new ExpressionFormatter() },
                new IFormatterResolver[] { options.Resolver });
            return options.WithResolver(resolver);
        }
    }
}
