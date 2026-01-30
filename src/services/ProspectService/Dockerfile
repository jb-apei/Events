# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution file
COPY ["src/services/Events.sln", "./"]

# Copy project files
COPY ["src/services/ProspectService/ProspectService.csproj", "ProspectService/"]
COPY ["src/services/Shared.Events/Shared.Events.csproj", "Shared.Events/"]
COPY ["src/services/Shared.Infrastructure/Shared.Infrastructure.csproj", "Shared.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "ProspectService/ProspectService.csproj"

# Copy everything else
COPY ["src/services/ProspectService/", "ProspectService/"]
COPY ["src/services/Shared.Events/", "Shared.Events/"]
COPY ["src/services/Shared.Infrastructure/", "Shared.Infrastructure/"]

# Build
WORKDIR "/src/ProspectService"
RUN dotnet build "ProspectService.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "ProspectService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ProspectService.dll"]
