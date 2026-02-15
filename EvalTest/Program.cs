// See https://aka.ms/new-console-template for more information

using System.Reflection;
using LiteEval;
using NUnit.Framework;

public class MockValueProvider : IValueProvider {
    public bool TryGetValue(ReadOnlySpan<char> name, out double value) {
        if (name.SequenceEqual("x".AsSpan())) {
            value = 5;
            return true;
        }

        value = 0;
        return false;
    }
}


[TestFixture]
public class Tests {
    private IValueProvider _valueProvider = null;

    [SetUp]
    public void Setup() {
        _valueProvider = new MockValueProvider();
        ValueProviderContext.SetGlobalValueProvider(_valueProvider);
    }

    [Test]
    public void SimpleMath() {
        var expression = new Expression("(2*5)-(20/4)");
        Assert.AreEqual(5, expression.Result);
    }

    [Test]
    public void ExpressionCalcuation() {
        var expression1 = new Expression("2+2");
        var expression2 = new Expression("2*2");
        var expression3 = new Expression("2/2");
        var expression4 = new Expression("(2-2)");
        var expression5 = new Expression("2^2");

        var mergedExp = ((expression1 / expression2) * expression3 + (expression4 - expression5) * 2);
        
        Assert.AreEqual(-7, mergedExp.Result);
    }

    [Test]
    public void ToStringTest() {
        var expression = new Expression("(2*5)-(20/4)");
        Assert.AreEqual(expression.Result, new Expression(expression.ToString()).Result);
        
        var expression1 = new Expression("2+2");
        var expression2 = new Expression("2*2");
        var expression3 = new Expression("2/2");
        var expression4 = new Expression("(2-2)");
        var expression5 = new Expression("2^2");

        var mergedExp = ((expression1 / expression2) * expression3 + (expression4 - expression5) * 2);
        Assert.AreEqual(mergedExp.Result, new Expression(mergedExp.ToString()).Result);
        
        // 테스트 케이스 1: 복잡한 중첩된 괄호와 다양한 연산자
        var expr1 = new Expression("3 + 4 * 2 / (1 - 5) ^ 2 ^ 3");
        Assert.AreEqual(expr1.Result, new Expression(expr1.ToString()).Result);

        // 테스트 케이스 2: 단항 연산자와 함수 사용
        var expr2 = new Expression("-sin(30) + abs(-10)");
        Assert.AreEqual(expr2.Result, new Expression(expr2.ToString()).Result);

        using (new ValueProviderContext(
                   new ValueProvider()
                       .Add("x", 5)
                       .Add("y", 3)
               )) {
            var expr3 = new Expression("{x} * {y} + ({x} / {y}) - {x} ^ {y}");
            Assert.AreEqual(expr3.Result, new Expression(expr3.ToString()).Result);
        }

        // 테스트 케이스 4: 다양한 연산자 우선순위 및 결합법칙
        var expr4 = new Expression("2 ^ 3 ^ 2");
        Assert.AreEqual(expr4.Result, new Expression(expr4.ToString()).Result);

        // 테스트 케이스 5: 단항 연산자와 연산자 우선순위
        var expr5 = new Expression("-2 * 3 + 4");
        Assert.AreEqual(expr5.Result, new Expression(expr5.ToString()).Result);

        // 테스트 케이스 6: 함수와 연산자 결합
        var expr6 = new Expression("max(2, 3) + min(4, 5) * abs(-6)");
        Assert.AreEqual(expr6.Result, new Expression(expr6.ToString()).Result);

        // 테스트 케이스 7: 복잡한 중첩 함수
        var expr7 = new Expression("sqrt(16) + pow(2, 3) - log(100)");
        Assert.AreEqual(expr7.Result, new Expression(expr7.ToString()).Result);

        // 테스트 케이스 8: 다중 변수와 함수 결합
        using (new ValueProviderContext(
                   new ValueProvider()
                                        .Add("a", 10)
                                        .Add("b", 20))) {
            var expr8 = new Expression("{a} + {b} * sin({a} / {b})");
            Assert.AreEqual(expr8.Result, new Expression(expr8.ToString()).Result);
        }

        // 테스트 케이스 9: 복잡한 단항 연산자와 이항 연산자 조합
        var expr9 = new Expression("-(-2) + --3");
        Assert.AreEqual(expr9.Result, new Expression(expr9.ToString()).Result);

        // 테스트 케이스 10: 모든 요소를 포함한 복잡한 수식
        var expr10 = new Expression("-((3 + 5) * (7 - 2) / max(1, min(4, 2))) ^ 2");
        Assert.AreEqual(expr10.Result, new Expression(expr10.ToString()).Result);
    }

    [Test]
    public void UnaryMinus() {
        Assert.AreEqual(-5, new Expression("-5-5--5").Result);
    }

    [Test]
    public void TestVariable() {
        var exp = new Expression("2*{x}+3");
        Assert.AreEqual(13, exp.Result);
    }

    [Test]
    public void Test() {
        var exp = new Expression("2*(3+4)");
        Assert.AreEqual(14, exp.Result);
    }

    [Test]
    public void TestPow() {
        var exp = new Expression("2^3");
        Assert.AreEqual(8, exp.Result);
    }

    [Test]
    public void TestFunction() {
        var exp = new Expression("-sin(0+0)");
        Assert.AreEqual(0, exp.Result);
    }

    [Test]
    public void TestFunction2Args() {
        var exp = new Expression("max(1, 3)");
        Assert.AreEqual(3, exp.Result);
    }

    [Test]
    public void TestMultipleFunctions() {
        var exp = new Expression("max(2,3) + -min(5,6)");
        Assert.AreEqual(-2, exp.Result);
    }

    [Test]
    public void TestFunctionAndVariable() {
        var exp = new Expression("2*{x} + sin({y})");
        using var context = new ValueProviderContext(
            new ValueProvider()
                .Add("y", Math.PI / 2)
        );
        
        Assert.AreEqual(11, exp.Result);
    }

    [Test]
    public void TestComplex() {
        var exp = new Expression(
            "3*{x.value.test}^2 + 2*{y} + {z} + sin({w}) + cos({v}) + tan({u}) - log({t}) + sqrt({s}) + atan2({r}, {q}) - max({p}, {o}) + min({n}, {m}) + abs({l}) + round({k}) + floor({j}) + ceiling({i}) - truncate({h}) + sign({g})");

        using var valueContext = new ValueProviderContext(
            new ValueProvider()
                .Add("x.value.test", 5)
                .Add("y", 3.14)
                .Add("z", 2.71)
                .Add("w", 1.57)
                .Add("v", 0)
                .Add("u", 45)
                .Add("t", 10)
                .Add("s", 16)
                .Add("r", 2)
                .Add("q", 3)
                .Add("p", 5)
                .Add("o", 6)
                .Add("n", 7)
                .Add("m", 8)
                .Add("l", -9)
                .Add("k", 10.5)
                .Add("j", 11.4)
                .Add("i", 12.7)
                .Add("h", 13.9)
                .Add("g", -14)
        );

        Assert.IsNotNull(exp.Result);
    }

    [Test]
    public void TextContext() {
        using (new ValueProviderContext(new ValueProvider()
                                            .Add("x", 5))) {
            using (new ValueProviderContext(new ValueProvider()
                                                .Add("x", 7))) {
                using (new ValueProviderContext(new ValueProvider()
                                                    .Add("y", 8))) {
                    var exp = new Expression("{x}+{y}");
                    Assert.AreEqual(15.0, exp.Result);
                }
            }
        }
    }

    [Test]
    public void TestComplex2() {
        using var valueContext = new ValueProviderContext(
            new ValueProvider()
                .Add("level", 1)
        );
        var exp = new Expression("floor(Pow(10, 1 + 8.5 * (1 - Exp(-0.95 * {level} / 1000)) + 0.00035 * {level} + 2.5 * Log10({level} / 1000 + 1)))");
        
        Assert.AreEqual(exp.Result, 10.0);
    }

    [Test]
    public void TestSmallNumber() {
        var smallNumber = 0.00000056789;
        var exp = new Expression($"abs({smallNumber:R})");
        var expected = Math.Abs(smallNumber);
        Assert.AreEqual(expected, exp.Result);
    }

    [Test]
    public void TestBinaryRoundTrip() {
        // Simple arithmetic
        var expr1 = new Expression("2+3");
        Assert.AreEqual(expr1, Expression.FromBytes(expr1.ToBytes()));

        // Operators: add, subtract, multiply, divide, power
        var expr2 = new Expression("(2*5)-(20/4)^2");
        Assert.AreEqual(expr2, Expression.FromBytes(expr2.ToBytes()));

        // Variables
        var expr3 = new Expression("{x}*{y}");
        Assert.AreEqual(expr3, Expression.FromBytes(expr3.ToBytes()));

        // Functions (1-arg and 2-arg)
        var expr4 = new Expression("sin(max(1,2))");
        Assert.AreEqual(expr4, Expression.FromBytes(expr4.ToBytes()));

        // Complex: all token types
        var expr5 = new Expression("3*{x}^2 + sin({y}) - max({a}, {b})");
        Assert.AreEqual(expr5, Expression.FromBytes(expr5.ToBytes()));

        // Unary minus
        var expr6 = new Expression("-5-5--5");
        Assert.AreEqual(expr6, Expression.FromBytes(expr6.ToBytes()));

        // Nested functions and operators
        var expr7 = new Expression("-((3 + 5) * (7 - 2) / max(1, min(4, 2))) ^ 2");
        Assert.AreEqual(expr7, Expression.FromBytes(expr7.ToBytes()));

        // Constant expression
        var expr8 = new Expression(42.5);
        Assert.AreEqual(expr8, Expression.FromBytes(expr8.ToBytes()));

        // Empty/default expression
        var expr9 = default(Expression);
        var restored9 = Expression.FromBytes(expr9.ToBytes());
        Assert.AreEqual(0, restored9.Result);
    }

    [Test]
    public void TestBinaryRoundTripResults() {
        using var context = new ValueProviderContext(
            new ValueProvider()
                .Add("x", 5)
                .Add("y", 3)
                .Add("a", 10)
                .Add("b", 20)
        );

        var expressions = new[] {
            "2+3",
            "{x}*{y}",
            "sin(max(1,2))",
            "3*{x}^2 + sin({y}) - max({a}, {b})",
            "-5-5--5",
            "-((3 + 5) * (7 - 2) / max(1, min(4, 2))) ^ 2",
        };

        foreach (var exprStr in expressions) {
            var expr = new Expression(exprStr);
            var restored = Expression.FromBytes(expr.ToBytes());
            Assert.AreEqual(expr.Result, restored.Result, $"Round-trip failed for: {exprStr}");
        }
    }

    [Test]
    public void TestGetByteCount() {
        // Empty expression
        var empty = default(Expression);
        Assert.AreEqual(3, empty.GetByteCount());
        Assert.AreEqual(empty.ToBytes().Length, empty.GetByteCount());

        // Simple arithmetic
        var expr1 = new Expression("2+3");
        Assert.AreEqual(expr1.ToBytes().Length, expr1.GetByteCount());

        // Variables
        var expr2 = new Expression("{x}*{y}");
        Assert.AreEqual(expr2.ToBytes().Length, expr2.GetByteCount());

        // Functions
        var expr3 = new Expression("sin(max(1,2))");
        Assert.AreEqual(expr3.ToBytes().Length, expr3.GetByteCount());

        // Complex expression with all token types
        var expr4 = new Expression("3*{x}^2 + sin({y}) - max({a}, {b})");
        Assert.AreEqual(expr4.ToBytes().Length, expr4.GetByteCount());

        // Constant
        var expr5 = new Expression(42.5);
        Assert.AreEqual(expr5.ToBytes().Length, expr5.GetByteCount());
    }

    [Test]
    public void TestTryWriteBytes() {
        var expr = new Expression("3*{x}^2 + sin({y})");
        int size = expr.GetByteCount();

        // Successful write
        var buffer = new byte[size];
        Assert.IsTrue(expr.TryWriteBytes(buffer, out int written));
        Assert.AreEqual(size, written);

        // Round-trip via TryWriteBytes
        var restored = Expression.FromBytes(buffer.AsSpan(0, written));
        Assert.AreEqual(expr, restored);

        // Buffer too small → false
        var smallBuffer = new byte[size - 1];
        Assert.IsFalse(expr.TryWriteBytes(smallBuffer, out int notWritten));
        Assert.AreEqual(0, notWritten);

        // Empty expression
        var empty = default(Expression);
        var emptyBuf = new byte[3];
        Assert.IsTrue(empty.TryWriteBytes(emptyBuf, out int emptyWritten));
        Assert.AreEqual(3, emptyWritten);
        Assert.AreEqual(0, Expression.FromBytes(emptyBuf).Result);

        // Empty expression buffer too small
        var tinyBuf = new byte[2];
        Assert.IsFalse(empty.TryWriteBytes(tinyBuf, out _));
    }

    [Test]
    public void TestTryWriteBytesRoundTrip() {
        using var context = new ValueProviderContext(
            new ValueProvider()
                .Add("x", 5)
                .Add("y", 3)
                .Add("a", 10)
                .Add("b", 20)
        );

        var expressions = new[] {
            "2+3",
            "{x}*{y}",
            "sin(max(1,2))",
            "3*{x}^2 + sin({y}) - max({a}, {b})",
            "-5-5--5",
            "-((3 + 5) * (7 - 2) / max(1, min(4, 2))) ^ 2",
        };

        foreach (var exprStr in expressions) {
            var expr = new Expression(exprStr);
            int size = expr.GetByteCount();
            var buffer = new byte[size];
            Assert.IsTrue(expr.TryWriteBytes(buffer, out int written), $"TryWriteBytes failed for: {exprStr}");
            Assert.AreEqual(size, written, $"Written size mismatch for: {exprStr}");

            var restored = Expression.FromBytes(buffer.AsSpan(0, written));
            Assert.AreEqual(expr.Result, restored.Result, $"Round-trip result failed for: {exprStr}");
            Assert.AreEqual(expr, restored, $"Round-trip equality failed for: {exprStr}");
        }
    }

    [Test]
    public void TestFunctions() {
        var random = new Random((int)DateTime.Now.Ticks);
        
        // test abs
        for (int i = 0; i < 1000; ++i) {
            var value = random.NextDouble();
            var exp = new Expression($"abs({value})");
            Assert.AreEqual(Math.Abs(value), exp.Result);
        }
        
        // test acos
        for (int i = 0; i < 1000; ++i) {
            var value = random.NextDouble();
            var exp = new Expression($"acos({value})");
            Assert.AreEqual(Math.Acos(value), exp.Result);
        }
        
        // test asin
        for (int i = 0; i < 1000; ++i) {
            var value = random.NextDouble();
            var exp = new Expression($"asin({value})");
            Assert.AreEqual(Math.Asin(value), exp.Result);
        }
        
        // test atan
        for (int i = 0; i < 1000; ++i) {
            var value = random.NextDouble();
            var exp = new Expression($"atan({value})");
            Assert.AreEqual(Math.Atan(value), exp.Result);
        }
        
        // test ceil
        for (int i = 0; i < 1000; ++i) {
            var value = random.NextDouble();
            var exp = new Expression($"ceiling({value})");
            Assert.AreEqual(Math.Ceiling(value), exp.Result);
        }
        
        // test cos
        for (int i = 0; i < 1000; ++i) {
            var value = random.NextDouble();
            var exp = new Expression($"cos({value})");
            Assert.AreEqual(Math.Cos(value), exp.Result);
        }
        
        // test cosh
        for (int i = 0; i < 1000; ++i) {
            var value = random.NextDouble();
            var exp = new Expression($"cosh({value})");
            Assert.AreEqual(Math.Cosh(value), exp.Result);
        }
        
        // test exp
        for (int i = 0; i < 1000; ++i) {
            var value = random.NextDouble();
            var exp = new Expression($"exp({value})");
            Assert.AreEqual(Math.Exp(value), exp.Result);
        }
        
        // test floor
        for (int i = 0; i < 1000; ++i) {
            var value = random.NextDouble();
            var exp = new Expression($"floor({value})");
            Assert.AreEqual(Math.Floor(value), exp.Result);
        }
        
        // test log
        for (int i = 0; i < 1000; ++i) {
            var value = random.NextDouble();
            var exp = new Expression($"log({value})");
            Assert.AreEqual(Math.Log(value), exp.Result);
        }
        
        // test log10
        for (int i = 0; i < 1000; ++i) {
            var value = random.NextDouble();
            var exp = new Expression($"log10({value})");
            Assert.AreEqual(Math.Log10(value), exp.Result);
        }
        
        // test round
        for (int i = 0; i < 1000; ++i) {
            var value = random.NextDouble();
            var exp = new Expression($"round({value})");
            Assert.AreEqual(Math.Round(value), exp.Result);
        }
        
        // test sign
        for (int i = 0; i < 1000; ++i) {
            var value = random.NextDouble();
            var exp = new Expression($"sign({value})");
            Assert.AreEqual(Math.Sign(value), exp.Result);
        }
        
        // test sin
        for (int i = 0; i < 1000; ++i) {
            var value = random.NextDouble();
            var exp = new Expression($"sin({value})");
            Assert.AreEqual(Math.Sin(value), exp.Result);
        }
        
        // test sinh
        for (int i = 0; i < 1000; ++i) {
            var value = random.NextDouble();
            var exp = new Expression($"sinh({value})");
            Assert.AreEqual(Math.Sinh(value), exp.Result);
        }
        
        // test sqrt
        for (int i = 0; i < 1000; ++i) {
            var value = random.NextDouble();
            var exp = new Expression($"sqrt({value})");
            Assert.AreEqual(Math.Sqrt(value), exp.Result);
        }
        
        // test tan
        for (int i = 0; i < 1000; ++i) {
            var value = random.NextDouble();
            var exp = new Expression($"tan({value})");
            Assert.AreEqual(Math.Tan(value), exp.Result);
        }
        
        // test tanh
        for (int i = 0; i < 1000; ++i) {
            var value = random.NextDouble();
            var exp = new Expression($"tanh({value})");
            Assert.AreEqual(Math.Tanh(value), exp.Result);
        }
        
        // test truncate
        for (int i = 0; i < 1000; ++i) {
            var value = random.NextDouble();
            var exp = new Expression($"truncate({value})");
            Assert.AreEqual(Math.Truncate(value), exp.Result);
        }
        
        // test atan2
        for (int i = 0; i < 1000; ++i) {
            var value1 = random.NextDouble();
            var value2 = random.NextDouble();
            var exp = new Expression($"atan2({value1}, {value2})");
            Assert.AreEqual(Math.Atan2(value1, value2), exp.Result);
        }
        
        // test max
        for (int i = 0; i < 1000; ++i) {
            var value1 = random.NextDouble();
            var value2 = random.NextDouble();
            var exp = new Expression($"max({value1}, {value2})");
            Assert.AreEqual(Math.Max(value1, value2), exp.Result);
        }
        
        // test min
        for (int i = 0; i < 1000; ++i) {
            var value1 = random.NextDouble();
            var value2 = random.NextDouble();
            var exp = new Expression($"min({value1}, {value2})");
            Assert.AreEqual(Math.Min(value1, value2), exp.Result);
        }
        
        // test pow
        for (int i = 0; i < 1000; ++i) {
            var value1 = random.NextDouble();
            var value2 = random.NextDouble();
            var exp = new Expression($"pow({value1}, {value2})");
            Assert.AreEqual(Math.Pow(value1, value2), exp.Result);
        }
    }

    [Test]
    public void TestLongVariableName() {
        var longName = "this_is_a_very_long_variable_name_that_exceeds_thirty_two_characters";
        using var context = new ValueProviderContext(
            new ValueProvider().Add(longName, 42.0));

        var expr = new Expression($"{{{longName}}} + 1");
        Assert.AreEqual(43.0, expr.Result);
        Assert.AreEqual(1, expr.VariableCount);
        Assert.AreEqual(longName, expr.GetVariableName(0));

        // ToString round-trip
        var str = expr.ToString();
        Assert.IsTrue(str.Contains(longName));
        var expr2 = new Expression(str);
        Assert.AreEqual(expr.Result, expr2.Result);

        // Binary round-trip
        var bytes = expr.ToBytes();
        var restored = Expression.FromBytes(bytes);
        Assert.AreEqual(expr, restored);
        Assert.AreEqual(expr.Result, restored.Result);
    }

    [Test]
    public void TestVariableDeduplication() {
        var expr = new Expression("{x}+{x}+{y}+{x}");
        Assert.AreEqual(2, expr.VariableCount);
        Assert.AreEqual("x", expr.GetVariableName(0));
        Assert.AreEqual("y", expr.GetVariableName(1));
    }

    [Test]
    public void TestGetResultWithValues() {
        var expr = new Expression("{x}*{y}+{z}");
        // VariableNames order: x=0, y=1, z=2
        Assert.AreEqual(3, expr.VariableCount);

        // x=2, y=3, z=4 → 2*3+4 = 10
        Span<double> values = stackalloc double[] { 2.0, 3.0, 4.0 };
        Assert.AreEqual(10.0, expr.GetResult(values));

        // x=5, y=10, z=1 → 5*10+1 = 51
        Span<double> values2 = stackalloc double[] { 5.0, 10.0, 1.0 };
        Assert.AreEqual(51.0, expr.GetResult(values2));

        // No-variable expression
        var constExpr = new Expression("2+3");
        Assert.AreEqual(5.0, constExpr.GetResult(ReadOnlySpan<double>.Empty));
    }

    [Test]
    public void TestBinaryV2RoundTrip() {
        using var context = new ValueProviderContext(
            new ValueProvider()
                .Add("alpha", 10)
                .Add("beta", 20)
        );

        var expressions = new[] {
            "2+3",
            "{alpha}*{beta}",
            "{alpha}+{alpha}+{beta}",
            "sin(max(1,2))",
            "3*{alpha}^2 + sin({beta}) - max({alpha}, {beta})",
            "-5-5--5",
        };

        foreach (var exprStr in expressions) {
            var expr = new Expression(exprStr);
            var bytes = expr.ToBytes();

            // Verify version byte is 2
            Assert.AreEqual(2, bytes[0], $"Version byte should be 2 for: {exprStr}");

            // Verify GetByteCount matches actual
            Assert.AreEqual(bytes.Length, expr.GetByteCount(), $"GetByteCount mismatch for: {exprStr}");

            // Round-trip
            var restored = Expression.FromBytes(bytes);
            Assert.AreEqual(expr, restored, $"Equality failed for: {exprStr}");
            Assert.AreEqual(expr.Result, restored.Result, $"Result mismatch for: {exprStr}");
            Assert.AreEqual(expr.VariableCount, restored.VariableCount, $"VariableCount mismatch for: {exprStr}");
        }
    }

    [Test]
    public void TestV1BackwardCompat() {
        // Manually construct V1 bytes for "{x}+{y}"
        // V1 format: [version=1][tokenCount:2][tokens...]
        // Tokens in postfix: x y +
        // TokenType: Value=0, Operator=1, Variable=2, Function=3
        // Variable token V1: [type=2] [nameLen:1] [chars:nameLen*2]
        // Operator token V1: [type=1] [opType:1]
        var v1Bytes = new List<byte>();
        v1Bytes.Add(1); // version
        v1Bytes.AddRange(BitConverter.GetBytes((ushort)3)); // tokenCount = 3

        // Token 0: Variable "x"
        v1Bytes.Add(2); // TokenType.Variable
        v1Bytes.Add(1); // nameLen = 1
        v1Bytes.AddRange(BitConverter.GetBytes((ushort)'x'));

        // Token 1: Variable "y"
        v1Bytes.Add(2); // TokenType.Variable
        v1Bytes.Add(1); // nameLen = 1
        v1Bytes.AddRange(BitConverter.GetBytes((ushort)'y'));

        // Token 2: Operator Add
        v1Bytes.Add(1); // TokenType.Operator
        v1Bytes.Add(0); // OperatorType.Add = 0

        var expr = Expression.FromBytes(v1Bytes.ToArray());

        using var context = new ValueProviderContext(
            new ValueProvider().Add("x", 10).Add("y", 20));

        Assert.AreEqual(30.0, expr.Result);
        Assert.AreEqual(2, expr.VariableCount);
        Assert.AreEqual("x", expr.GetVariableName(0));
        Assert.AreEqual("y", expr.GetVariableName(1));
        Assert.AreEqual("{x} + {y}", expr.ToString());
    }

    [Test]
    public void TestCombineSharedVars() {
        var left  = new Expression("{x}+{y}");
        var right = new Expression("{y}+{z}");

        Assert.AreEqual(2, left.VariableCount);  // x, y
        Assert.AreEqual(2, right.VariableCount); // y, z

        var combined = left + right;
        Assert.AreEqual(3, combined.VariableCount); // x, y, z
        Assert.AreEqual("x", combined.GetVariableName(0));
        Assert.AreEqual("y", combined.GetVariableName(1));
        Assert.AreEqual("z", combined.GetVariableName(2));

        using var context = new ValueProviderContext(
            new ValueProvider().Add("x", 1).Add("y", 2).Add("z", 3));

        // (x+y) + (y+z) = (1+2) + (2+3) = 3+5 = 8
        Assert.AreEqual(8.0, combined.Result);

        // Binary round-trip
        var restored = Expression.FromBytes(combined.ToBytes());
        Assert.AreEqual(combined, restored);
        Assert.AreEqual(combined.Result, restored.Result);
    }
}