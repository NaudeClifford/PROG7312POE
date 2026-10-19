
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy the entire solution into the build container.
COPY . .

# Restore the API and all referenced projects.
RUN dotnet restore "SmartX.API/SmartX.API.csproj"

# Publish the API after restore has completed.
WORKDIR /src/SmartX.API
RUN dotnet publish "SmartX.API.csproj" \
    --configuration Release \
    --output /app/publish \
    --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "SmartX.API.dll"]
