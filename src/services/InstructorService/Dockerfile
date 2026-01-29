# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution file
COPY ["Events.sln", "./"]

# Copy project files
COPY ["InstructorService/InstructorService.csproj", "InstructorService/"]
COPY ["Shared.Events/Shared.Events.csproj", "Shared.Events/"]

# Restore dependencies
RUN dotnet restore "InstructorService/InstructorService.csproj"

# Copy everything else
COPY ["InstructorService/", "InstructorService/"]
COPY ["Shared.Events/", "Shared.Events/"]

# Build
WORKDIR "/src/InstructorService"
RUN dotnet build "InstructorService.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "InstructorService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "InstructorService.dll"]
