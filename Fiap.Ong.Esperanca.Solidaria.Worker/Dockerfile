# Dockerfile multi-stage for .NET 10 Worker

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution file and all project files for restore
COPY ["Fiap.Ong.Esperanca.Solidaria.Worker.slnx", "./"]
COPY ["Fiap.Ong.Esperanca.Solidaria.Worker/Fiap.Ong.Esperanca.Solidaria.Worker.csproj", "Fiap.Ong.Esperanca.Solidaria.Worker/"]
COPY ["Fiap.Ong.Esperanca.Solidaria.Contracts/Fiap.Ong.Esperanca.Solidaria.Contracts.csproj", "Fiap.Ong.Esperanca.Solidaria.Contracts/"]

# Restore packages
RUN dotnet restore "Fiap.Ong.Esperanca.Solidaria.Worker.slnx"

# Copy all source code
COPY . .

# Publish the worker project
RUN dotnet publish "Fiap.Ong.Esperanca.Solidaria.Worker/Fiap.Ong.Esperanca.Solidaria.Worker.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV DOTNET_RUNNING_IN_CONTAINER=true

HEALTHCHECK --interval=30s --timeout=5s --start-period=5s --retries=3 \
  CMD sh -c 'test "$(cat /proc/1/comm)" = "dotnet" || exit 1'

ENTRYPOINT ["dotnet", "Fiap.Ong.Esperanca.Solidaria.Worker.dll"]
