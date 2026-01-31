# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution file
COPY ["Events.sln", "./"]

# Copy ONLY project files (no source code) for dependency restore
COPY ["EventRelay/EventRelay.csproj", "EventRelay/"]
COPY ["Shared.Events/Shared.Events.csproj", "Shared.Events/"]
COPY ["Shared.Infrastructure/Shared.Infrastructure.csproj", "Shared.Infrastructure/"]
COPY ["Shared.Configuration/Shared.Configuration.csproj", "Shared.Configuration/"]

# Restore dependencies (this layer is cached unless .csproj changes)
RUN dotnet restore "EventRelay/EventRelay.csproj"

# NOW copy all source code (only invalidates cache if source changes, not if dependencies change)
COPY ["EventRelay/", "EventRelay/"]
COPY ["Shared.Events/", "Shared.Events/"]
COPY ["Shared.Infrastructure/", "Shared.Infrastructure/"]
COPY ["Shared.Configuration/", "Shared.Configuration/"]

# Build
WORKDIR "/src/EventRelay"
RUN dotnet build "EventRelay.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "EventRelay.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EventRelay.dll"]
