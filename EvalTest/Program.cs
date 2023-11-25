// See https://aka.ms/new-console-template for more information

using System.Reflection;
using LiteEval;
using NUnit.Framework;

public class MockValueProvider : IValueProvider {
    public bool TryGetValue(ReadOnlyMemory<char> name, out double value) {
        if (name.Span.SequenceEqual("x".AsSpan())) {
            value = 5;
            return true;
        }

        value = 0;
        return false;
    }
}

[TestFixture]
public class Tests {
    private IValueProvider _valueProvider;
    [SetUp]
    public void Setup() {
        _valueProvider = new MockValueProvider();
        Expression.SetGlobalValueProvider(_valueProvider);
    }

    [Test]
    public void SimpleMath() {
        Assert.AreEqual(5, new Expression("2*5-20/4").Result);
    }

    [Test]
    public void UnaryMinus() {
        Assert.AreEqual(-5, new Expression("-5-5--5").Result);
    }

    [Test]
    public void TestVariable() {
        Expression exp = new Expression("2*{x}+3");
        Assert.AreEqual(13, exp.Result);
    }

    [Test]
    public void Test() {
        Expression exp = new Expression("2*(3+4)");
        Assert.AreEqual(14, exp.Result);
    }

    [Test]
    public void TestPow() {
        Expression exp = new Expression("2^3");
        Assert.AreEqual(8, exp.Result);
    }

    [Test]
    public void TestFunction() {
        Expression exp = new Expression("-sin(0+0)");
        Assert.AreEqual(0, exp.Result);
    }

    [Test]
    public void TestFunction2Args() {
        Expression exp = new Expression("max(1, 3)");
        Assert.AreEqual(3, exp.Result);
    }

    [Test]
    public void TestMultipleFunctions() {
        Expression exp = new Expression("max(2,3) + -min(5,6)");
        Assert.AreEqual(-2, exp.Result);
    }

    [Test]
    public void TestFunctionAndVariable() {
        Expression exp = new Expression("2*{x} + sin({y})");
        exp["y"] = Math.PI / 2;
        Assert.AreEqual(11, exp.Result);
    }

    [Test]
    public void TestComplex() {
        Expression exp = new Expression(
            "3*{x.value.test}^2 + 2*{y} + {z} + sin({w}) + cos({v}) + tan({u}) - log({t}) + sqrt({s}) + atan2({r}, {q}) - max({p}, {o}) + min({n}, {m}) + abs({l}) + round({k}) + floor({j}) + ceiling({i}) - truncate({h}) + sign({g})");
        exp["x.value.test"] = 5;
        exp["y"]            = 3.14;
        exp["z"]            = 2.71;
        exp["w"]            = 1.57;
        exp["v"]            = 0;
        exp["u"]            = 45;
        exp["t"]            = 10;
        exp["s"]            = 16;
        exp["r"]            = 2;
        exp["q"]            = 3;
        exp["p"]            = 5;
        exp["o"]            = 6;
        exp["n"]            = 7;
        exp["m"]            = 8;
        exp["l"]            = -9;
        exp["k"]            = 10.5;
        exp["j"]            = 11.4;
        exp["i"]            = 12.7;
        exp["h"]            = 13.9;
        exp["g"]            = -14;

        Assert.IsNotNull(exp.Result);
    }
}