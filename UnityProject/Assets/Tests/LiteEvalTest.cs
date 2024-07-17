using System;
using System.Collections;
using System.Collections.Generic;
using LiteEval;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

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

public class LiteEvalTest
{
    private IValueProvider _valueProvider = null;

    [SetUp]
    public void Setup() {
        _valueProvider = new MockValueProvider();
        ValueProviderContext.SetGlobalValueProvider(_valueProvider);
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
}
