# LiteEval

[English](README.md)

가볍고 빠른 C# 수식 평가 라이브러리입니다.

Expression 객체를 생성할 때 토큰화를 수행하고, 이후 평가 시에는 최소한의 메모리 할당과 연산만 일어납니다. 따라서 한번 생성한 Expression을 재활용하는 것이 좋습니다.

Unity에서도 사용할 수 있습니다.

## 설치

### NuGet

```
dotnet add package dev.breadpack.LiteEval
```

직렬화 지원 (선택):

```
dotnet add package dev.breadpack.LiteEval.Serialization.Newtonsoft
dotnet add package dev.breadpack.LiteEval.Serialization.MessagePack
```

### Unity Package Manager

**Window > Package Manager > + > Add package from git URL**에서 아래 Git URL을 추가합니다:

```
https://github.com/breadpack/LiteEval.git?path=UnityPackage
```

## 지원 환경

- 타겟: `netstandard2.1`
- .NET 5.0+, .NET Core 3.1+
- Unity 2022.3+

## 기능

- **사칙연산 및 거듭제곱** — `+`, `-`, `*`, `/`, `^`
- **괄호 및 단항 마이너스** — `(2+3)*-4`
- **과학 표기법** — `1.5e3`, `2.5E-2`
- **변수** — `{변수명}` 구문, 스코프 기반 값 제공
- **System.Math 함수**
  - 1인자: `abs`, `acos`, `asin`, `atan`, `ceiling`, `cos`, `cosh`, `exp`, `floor`, `log`, `log10`, `round`, `sign`, `sin`, `sinh`, `sqrt`, `tan`, `tanh`, `truncate`
  - 2인자: `atan2`, `max`, `min`, `pow`
- **Expression 결합** — 연산자 오버로드(`+`, `-`, `*`, `/`, `^`)로 수식 합성
- **바이너리 직렬화** — `ToBytes()` / `FromBytes()`
- **JSON 및 MessagePack 직렬화** — 확장 패키지 제공

## 사용법

### 기본 사칙연산

```csharp
var expr = new Expression("(2*5)-(20/4)");
Console.WriteLine(expr.Result); // 5
```

### 단항 마이너스

```csharp
Console.WriteLine(new Expression("-5-5--5").Result); // -5
```

### 거듭제곱

```csharp
Console.WriteLine(new Expression("2^3").Result); // 8
```

### 변수

변수는 `{}`로 감싸서 표기합니다. `ValueProviderContext`를 통해 값을 제공합니다.

```csharp
var expr = new Expression("2*{x}+3");

using (new ValueProviderContext(
        new ValueProvider().Add("x", 5)))
{
    Console.WriteLine(expr.Result); // 13
}
```

### 함수

```csharp
Console.WriteLine(new Expression("max(2, 3)").Result); // 3
Console.WriteLine(new Expression("sqrt(16) + pow(2, 3)").Result); // 12
```

### 함수와 변수 함께 사용

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

### 중첩 ValueProviderContext

내부 컨텍스트는 외부 컨텍스트의 변수를 상속합니다. 가장 안쪽 값이 우선됩니다.

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

### 직접 값을 전달하여 평가 (컨텍스트 없이)

`GetResult(ReadOnlySpan<double>)`를 사용하면 `GetVariableName()` 순서에 맞춰 변수 값을 직접 전달할 수 있습니다.

```csharp
var expr = new Expression("{x}*{y}+{z}");

Span<double> values = stackalloc double[] { 2.0, 3.0, 4.0 }; // x=2, y=3, z=4
Console.WriteLine(expr.GetResult(values)); // 10
```

### Expression 결합

연산자 오버로드를 사용하여 수식을 결합할 수 있습니다.

```csharp
var a = new Expression("2+2");
var b = new Expression("2*2");

var combined = a + b;
Console.WriteLine(combined.Result); // 8

// 스칼라 값과 결합
var scaled = a * 3;
Console.WriteLine(scaled.Result); // 12
```

### 바이너리 직렬화

```csharp
var expr = new Expression("3*{x}^2 + sin({y})");

byte[] bytes = expr.ToBytes();
var restored = Expression.FromBytes(bytes);
// expr과 restored는 동일
```

### Newtonsoft.Json 직렬화

```csharp
var settings = new JsonSerializerSettings().UseLiteEval();
string json = JsonConvert.SerializeObject(expr, settings);
var restored = JsonConvert.DeserializeObject<Expression>(json, settings);
```

### MessagePack 직렬화

```csharp
var options = MessagePackSerializerOptions.Standard.UseLiteEval();
byte[] bytes = MessagePackSerializer.Serialize(expr, options);
var restored = MessagePackSerializer.Deserialize<Expression>(bytes, options);
```

## 라이선스

[LICENSE](LICENSE) 파일을 참고하세요.
