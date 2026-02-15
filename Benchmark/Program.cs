// See https://aka.ms/new-console-template for more information

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using LiteEval;
using System;
using System.Collections.Generic;
using System.Linq;

BenchmarkRunner.Run<ExpressionBenchmark>();


[MemoryDiagnoser]
public class ExpressionBenchmark {
    private Expression     _expression;
    private ExpressionCalc _expressionCalc;
    private Expression     _expressionComplex;
    private byte[]         _serializedBytes;
    private byte[]         _serializedComplexBytes;
    private string         _serializedString;

    [GlobalSetup]
    public void Setup() {
        _expression             = new Expression("2 * 3 + (4 - 1) ^ 2");
        _expressionComplex      = new Expression("3*{x}^2 + 2*{y} + {z} + sin({w}) + cos({v}) + tan({u}) - log({t}) + sqrt({s}) + atan2({r}, {q}) - max({p}, {o}) + min({n}, {m}) + abs({l}) + round({k}) + floor({j}) + ceiling({i}) - truncate({h}) + sign({g})");
        ValueProviderContext.SetGlobalValueProvider(
            new ValueProvider()
                .Add("x", 5)
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
        _expressionCalc         = new ExpressionCalc("2 * 3 + (4 - 1) ^ 2");
        _serializedBytes        = _expression.ToBytes();
        _serializedComplexBytes = _expressionComplex.ToBytes();
        _serializedString       = _expression.ToString();
    }

    [Benchmark]
    public void NewExpression() {
        var _expression = new Expression("2 * 3 + (4 - 1) ^ 2");
    }

    [Benchmark]
    public void NewComplexExpression() {
        var _expressionComplex = new Expression("3*{x}^2 + 2*{y} + {z} + sin({w}) + cos({v}) + tan({u}) - log({t}) + sqrt({s}) + atan2({r}, {q}) - max({p}, {o}) + min({n}, {m}) + abs({l}) + round({k}) + floor({j}) + ceiling({i}) - truncate({h}) + sign({g})");
        using var context = new ValueProviderContext(
            new ValueProvider()
                .Add("x", 5)
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
    }

    [Benchmark]
    public void NewExpressionCalc() {
        var _expressionCalc = new ExpressionCalc("2 * 3 + (4 - 1) ^ 2");
    }

    [Benchmark]
    public void BenchmarkResult() {
        var result = _expression.Result;
    }

    [Benchmark]
    public void BenchmarkComplex() {
        var result = _expressionComplex.Result;
    }

    [Benchmark]
    public void BenchmarkEval() {
        var result = _expressionCalc.Eval();
    }

    [Benchmark]
    public byte[] BenchmarkToBytes() => _expression.ToBytes();

    [Benchmark]
    public byte[] BenchmarkToBytesComplex() => _expressionComplex.ToBytes();

    [Benchmark]
    public Expression BenchmarkFromBytes() => Expression.FromBytes(_serializedBytes);

    [Benchmark]
    public Expression BenchmarkFromBytesComplex() => Expression.FromBytes(_serializedComplexBytes);

    [Benchmark]
    public Expression BenchmarkFromString() => new Expression(_serializedString);

    [Benchmark]
    public string BenchmarkToString() => _expression.ToString();
}