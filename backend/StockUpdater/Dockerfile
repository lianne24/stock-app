# Multi-stage build keeps the final image small and production-friendly.

# ---------- Build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj first to leverage Docker layer caching for restores.
COPY StockUpdater.csproj ./
RUN dotnet restore

# Copy the rest of the source and publish a Release build.
COPY . .
RUN dotnet publish -c Release -o /app/publish

# ---------- Runtime stage ----------
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app

# Copy compiled output from the build stage.
COPY --from=build /app/publish .

# Batch job: runs once, prints logs, exits with success/failure code.
ENTRYPOINT ["dotnet", "StockUpdater.dll"]
