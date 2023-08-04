FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["EvalTest/EvalTest.csproj", "EvalTest/"]
RUN dotnet restore "EvalTest/EvalTest.csproj"
COPY . .
WORKDIR "/src/EvalTest"
RUN dotnet build "EvalTest.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EvalTest.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EvalTest.dll"]
