# LiteEval

[한국어](README.ko.md)

A lightweight, fast expression evaluator for C#.

Expressions are tokenized once at construction time. Subsequent evaluations perform minimal allocation and computation, so reusing `Expression` objects is recommended.

Also available for Unity.

## Installation

### NuGet

```
dotnet add package dev.breadpack.LiteEval
```

Serialization support (optional):

```
dotnet add package dev.breadpack.LiteEval.Serialization.Newtonsoft
dotnet add package dev.breadpack.LiteEval.Serialization.MessagePack
```

### Unity Package Manager

Add the following Git URL via **Window > Package Manager > + > Add package from git URL**:

```
https://github.com/breadpack/LiteEval.git?path=UnityPackage
```

## Supported Environments

- Target: `netstandard2.1`
- .NET 5.0+, .NET Core 3.1+
- Unity 2022.3+

## Features

- **Arithmetic operators** — `+`, `-`, `*`, `/`, `^` (power)
- **Parentheses and unary minus** — `(2+3)*-4`
- **Scientific notation** — `1.5e3`, `2.5E-2`
- **Variables** — `{variableName}` syntax with scoped value providers
- **System.Math functions**
  - 1-arg: `abs`, `acos`, `asin`, `atan`, `ceiling`, `cos`, `cosh`, `exp`, `floor`, `log`, `log10`, `round`, `sign`, `sin`, `sinh`, `sqrt`, `tan`, `tanh`, `truncate`
  - 2-arg: `atan2`, `max`, `min`, `pow`
- **Expression combining** — operator overloads (`+`, `-`, `*`, `/`, `^`) to merge expressions
- **Binary serialization** — `ToBytes()` / `FromBytes()` for compact storage
- **JSON & MessagePack serialization** — via extension packages

## Usage

### Basic arithmetic

```csharp
var expr = new Expression("(2*5)-(20/4)");
Console.WriteLine(expr.Result); // 5
```

### Unary minus

```csharp
Console.WriteLine(new Expression("-5-5--5").Result); // -5
```

### Power operator

```csharp
Console.WriteLine(new Expression("2^3").Result); // 8
```

### Variables

Variables are enclosed in `{}`. Values are supplied through `ValueProviderContext`.

```csharp
var expr = new Expression("2*{x}+3");

using (new ValueProviderContext(
        new ValueProvider().Add("x", 5)))
{
    Console.WriteLine(expr.Result); // 13
}
```

### Functions

```csharp
Console.WriteLine(new Expression("max(2, 3)").Result); // 3
Console.WriteLine(new Expression("sqrt(16) + pow(2, 3)").Result); // 12
```

### Functions with variables

```csharp
var expr = new Expression("2*{x} + sin({y})");

using (new ValueProviderContext(
        new ValueProvider()
            .Add("x", 5)
            .Add("y", Math.PI / 2)))
{
    Console.WriteLine(expr.Result); // 11
}
```

### Nested value provider contexts

Inner contexts inherit variables from outer contexts. The innermost value takes precedence.

```csharp
using (new ValueProviderContext(new ValueProvider().Add("x", 5)))
{
    using (new ValueProviderContext(new ValueProvider().Add("x", 7)))
    {
        using (new ValueProviderContext(new ValueProvider().Add("y", 8)))
        {
            var expr = new Expression("{x}+{y}");
            Console.WriteLine(expr.Result); // 15  (x=7, y=8)
        }
    }
}
```

### Evaluating with direct values (no context)

Use `GetResult(ReadOnlySpan<double>)` to pass variable values directly by index, matching the order from `GetVariableName()`.

```csharp
var expr = new Expression("{x}*{y}+{z}");

Span<double> values = stackalloc double[] { 2.0, 3.0, 4.0 }; // x=2, y=3, z=4
Console.WriteLine(expr.GetResult(values)); // 10
```

### Expression combining

Expressions can be combined using operator overloads.

```csharp
var a = new Expression("2+2");
var b = new Expression("2*2");

var combined = a + b;
Console.WriteLine(combined.Result); // 8

// Combine with scalar
var scaled = a * 3;
Console.WriteLine(scaled.Result); // 12
```

### Binary serialization

```csharp
var expr = new Expression("3*{x}^2 + sin({y})");

byte[] bytes = expr.ToBytes();
var restored = Expression.FromBytes(bytes);
// expr and restored are equal
```

### Newtonsoft.Json serialization

```csharp
var settings = new JsonSerializerSettings().UseLiteEval();
string json = JsonConvert.SerializeObject(expr, settings);
var restored = JsonConvert.DeserializeObject<Expression>(json, settings);
```

### MessagePack serialization

```csharp
var options = MessagePackSerializerOptions.Standard.UseLiteEval();
byte[] bytes = MessagePackSerializer.Serialize(expr, options);
var restored = MessagePackSerializer.Deserialize<Expression>(bytes, options);
```

## Benchmark

Measured with [BenchmarkDotNet](https://benchmarkdotnet.org/) on Apple M1 / .NET 8.0.

Simple expression: `2 * 3 + (4 - 1) ^ 2`
Complex expression: `3*{x}^2 + 2*{y} + {z} + sin({w}) + ...` (17 variables, 10+ functions)

### Evaluation

| Method | Mean | Allocated |
|---|---:|---:|
| Simple evaluation (`Result`) | 36.92 ns | **0 B** |
| Complex evaluation (`Result`) | 529.41 ns | **0 B** |
| Legacy `ExpressionCalc.Eval()` | 942.53 ns | 2,872 B |

Once an `Expression` is constructed, evaluation allocates **zero bytes** on the managed heap — regardless of expression complexity.

### Construction

| Method | Mean | Allocated |
|---|---:|---:|
| `new Expression(simple)` | 342.98 ns | 224 B |
| `new Expression(complex)` | 2,620.39 ns | 8,608 B |
| Legacy `new ExpressionCalc(simple)` | 130.17 ns | 288 B |

Construction performs tokenization and compilation. Reuse `Expression` objects to avoid repeated allocation.

### Serialization

| Method | Mean | Allocated |
|---|---:|---:|
| `ToBytes()` (simple) | 47.57 ns | 88 B |
| `ToBytes()` (complex) | 330.67 ns | 264 B |
| `FromBytes()` (simple) | 24.37 ns | 112 B |
| `FromBytes()` (complex) | 324.26 ns | 1,192 B |
| `new Expression(string)` | 341.00 ns | 224 B |
| `ToString()` | 356.85 ns | 184 B |

Binary deserialization (`FromBytes`) is significantly faster than parsing from string, especially for complex expressions.

## License

See [LICENSE](LICENSE) for details.
