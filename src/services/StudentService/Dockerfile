# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution file
COPY ["src/services/Events.sln", "./"]

# Copy project files
COPY ["src/services/StudentService/StudentService.csproj", "StudentService/"]
COPY ["src/services/Shared.Events/Shared.Events.csproj", "Shared.Events/"]

# Restore dependencies
RUN dotnet restore "StudentService/StudentService.csproj"

# Copy everything else
COPY ["src/services/StudentService/", "StudentService/"]
COPY ["src/services/Shared.Events/", "Shared.Events/"]

# Build
WORKDIR "/src/StudentService"
RUN dotnet build "StudentService.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "StudentService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "StudentService.dll"]
