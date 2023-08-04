FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["Benchmark/Benchmark.csproj", "Benchmark/"]
RUN dotnet restore "Benchmark/Benchmark.csproj"
COPY . .
WORKDIR "/src/Benchmark"
RUN dotnet build "Benchmark.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Benchmark.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Benchmark.dll"]
