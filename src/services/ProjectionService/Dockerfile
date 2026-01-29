# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution file
COPY ["Events.sln", "./"]

# Copy project files
COPY ["ProjectionService/ProjectionService.csproj", "ProjectionService/"]
COPY ["Shared.Events/Shared.Events.csproj", "Shared.Events/"]

# Restore dependencies
RUN dotnet restore "ProjectionService/ProjectionService.csproj"

# Copy everything else
COPY ["ProjectionService/", "ProjectionService/"]
COPY ["Shared.Events/", "Shared.Events/"]

# Build
WORKDIR "/src/ProjectionService"
RUN dotnet build "ProjectionService.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "ProjectionService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ProjectionService.dll"]
