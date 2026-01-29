# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution file
COPY ["Events.sln", "./"]

# Copy project files
COPY ["ApiGateway/ApiGateway.csproj", "ApiGateway/"]
COPY ["Shared.Events/Shared.Events.csproj", "Shared.Events/"]

# Restore dependencies
RUN dotnet restore "ApiGateway/ApiGateway.csproj"

# Copy everything else
COPY ["ApiGateway/", "ApiGateway/"]
COPY ["Shared.Events/", "Shared.Events/"]

# Build
WORKDIR "/src/ApiGateway"
RUN dotnet build "ApiGateway.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "ApiGateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ApiGateway.dll"]
