# ── Stage 1: Build ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore dependencies first (cached layer unless .csproj changes)
COPY HMS.API/HMS.API.csproj HMS.API/
RUN dotnet restore HMS.API/HMS.API.csproj

# Copy the rest of the source and publish
COPY HMS.API/ HMS.API/
RUN dotnet publish HMS.API/HMS.API.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

# ── Stage 2: Runtime ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "HMS.API.dll"]
