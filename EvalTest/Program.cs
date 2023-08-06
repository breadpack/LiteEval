// See https://aka.ms/new-console-template for more information

using System.Reflection;
using LiteEval;
using NUnit.Framework;

[TestFixture]
public class Tests {
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
        Expression exp = new Expression("2*x+3");
        exp._expressionValue["x"] = 5;
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
        Expression exp = new Expression("2*x + sin(y)");
        exp._expressionValue["x"] = 5;
        exp._expressionValue["y"] = Math.PI / 2;
        Assert.AreEqual(11, exp.Result);
    }

    [Test]
    public void TestComplex() {
        Expression exp = new Expression("3*x^2 + 2*y + z + sin(w) + cos(v) + tan(u) - log(t) + sqrt(s) + atan2(r, q) - max(p, o) + min(n, m) + abs(l) + round(k) + floor(j) + ceiling(i) - truncate(h) + sign(g)");
        exp._expressionValue["x"] = 5;
        exp._expressionValue["y"] = 3.14;
        exp._expressionValue["z"] = 2.71;
        exp._expressionValue["w"] = 1.57;
        exp._expressionValue["v"] = 0;
        exp._expressionValue["u"] = 45;
        exp._expressionValue["t"] = 10;
        exp._expressionValue["s"] = 16;
        exp._expressionValue["r"] = 2;
        exp._expressionValue["q"] = 3;
        exp._expressionValue["p"] = 5;
        exp._expressionValue["o"] = 6;
        exp._expressionValue["n"] = 7;
        exp._expressionValue["m"] = 8;
        exp._expressionValue["l"] = -9;
        exp._expressionValue["k"] = 10.5;
        exp._expressionValue["j"] = 11.4;
        exp._expressionValue["i"] = 12.7;
        exp._expressionValue["h"] = 13.9;
        exp._expressionValue["g"] = -14;

        Assert.IsNotNull(exp.Result);
    }
}