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

    public void Clear() {
        throw new NotImplementedException();
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
}