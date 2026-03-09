# ── Stage 1: build ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY CrewRed.sln ./
COPY CrewRed.Application/CrewRed.Application.csproj ./CrewRed.Application/
COPY CrewRed.Console/CrewRed.Console.csproj ./CrewRed.Console/
COPY CrewRed.Infrastructure/CrewRed.Infrastructure.csproj ./CrewRed.Infrastructure/
RUN dotnet restore

COPY . ./

RUN ls -la
RUN ls -la CrewRed.Console/

RUN dotnet publish CrewRed.Console/CrewRed.Console.csproj -c Release -o /out --no-restore

# ── Stage 2: runtime ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime
WORKDIR /app

COPY --from=build /out ./

VOLUME ["/data"]

ENTRYPOINT ["dotnet", "CrewRed.Console.dll"]
CMD ["/data/sample-cab-data.csv"]