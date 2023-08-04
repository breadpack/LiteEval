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

    [GlobalSetup]
    public void Setup() {
        _expression = new Expression("2 * 3 + (4 - 1) ^ 2");

         _expressionComplex = new Expression("3*x^2 + 2*y + z + sin(w) + cos(v) + tan(u) - log(t) + sqrt(s) + atan2(r, q) - max(p, o) + min(n, m) + abs(l) + round(k) + floor(j) + ceiling(i) - truncate(h) + sign(g)");
        _expressionComplex._expressionValue["x"] = 5;
        _expressionComplex._expressionValue["y"] = 3.14;
        _expressionComplex._expressionValue["z"] = 2.71;
        _expressionComplex._expressionValue["w"] = 1.57;
        _expressionComplex._expressionValue["v"] = 0;
        _expressionComplex._expressionValue["u"] = 45;
        _expressionComplex._expressionValue["t"] = 10;
        _expressionComplex._expressionValue["s"] = 16;
        _expressionComplex._expressionValue["r"] = 2;
        _expressionComplex._expressionValue["q"] = 3;
        _expressionComplex._expressionValue["p"] = 5;
        _expressionComplex._expressionValue["o"] = 6;
        _expressionComplex._expressionValue["n"] = 7;
        _expressionComplex._expressionValue["m"] = 8;
        _expressionComplex._expressionValue["l"] = -9;
        _expressionComplex._expressionValue["k"] = 10.5;
        _expressionComplex._expressionValue["j"] = 11.4;
        _expressionComplex._expressionValue["i"] = 12.7;
        _expressionComplex._expressionValue["h"] = 13.9;
        _expressionComplex._expressionValue["g"] = -14;
        
        _expressionCalc           = new ExpressionCalc("2 * 3 + (4 - 1) ^ 2");
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
}