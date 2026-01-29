# Docker Deployment Guide

This guide explains how to build, deploy, and run the Events project using Docker containers locally and prepare for Azure Container Apps (ACA) deployment.

## Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) installed and running
- [Docker Hub account](https://hub.docker.com/) (for pushing images)
- Git installed
- PowerShell (Windows) or Bash (Linux/Mac)

## Architecture

The Events project consists of 7 containerized services:

| Service | Purpose | Port |
|---------|---------|------|
| **api-gateway** | Main API Gateway, handles authentication and routing | 5037 |
| **prospect-service** | Manages prospect entities and events | 5110 |
| **student-service** | Manages student entities and events | 5120 |
| **instructor-service** | Manages instructor entities and events | 5130 |
| **event-relay** | Publishes events from Outbox to Event Grid | N/A |
| **projection-service** | Maintains read models from events | 5140 |
| **frontend** | React web application | 3000 |

Additional infrastructure services:
- **SQL Server** (port 1433) - Database for all services
- **Azurite** (ports 10000-10002) - Azure Storage emulator for local development

## Quick Start - Running Locally

### 1. Clone the Repository

```bash
git clone https://github.com/jb-apei/Events.git
cd Events
```

### 2. Start All Services with Docker Compose

```bash
docker-compose up -d
```

This command will:
- Build all Docker images from source
- Start SQL Server and Azurite containers
- Start all microservices
- Start the React frontend

### 3. Access the Application

- **Frontend UI**: http://localhost:3000
- **API Gateway Swagger**: http://localhost:5037/swagger
- **Prospect Service Swagger**: http://localhost:5110/swagger
- **Student Service Swagger**: http://localhost:5120/swagger
- **Instructor Service Swagger**: http://localhost:5130/swagger
- **Projection Service Swagger**: http://localhost:5140/swagger

### 4. Check Service Status

```bash
docker-compose ps
```

### 5. View Logs

```bash
# All services
docker-compose logs -f

# Specific service
docker-compose logs -f api-gateway
docker-compose logs -f prospect-service
```

### 6. Stop Services

```bash
# Stop all services
docker-compose down

# Stop and remove volumes (data will be lost)
docker-compose down -v
```

## Building and Pushing to Docker Hub

### Step 1: Login to Docker Hub

```bash
docker login
```

Enter your Docker Hub username and password.

### Step 2: Build and Push Images

**Windows (PowerShell):**
```powershell
.\build-and-push.ps1 -DockerHubUsername <your-username> -Version 1.0.0
```

**Linux/Mac (Bash):**
```bash
chmod +x build-and-push.sh
./build-and-push.sh -u <your-username> -v 1.0.0
```

**Options:**
- `-DockerHubUsername` / `-u`: Your Docker Hub username (required)
- `-Version` / `-v`: Image version tag (default: `latest`)
- `-NoPush` / `--no-push`: Build only, don't push to Docker Hub

**Example:**
```powershell
.\build-and-push.ps1 -DockerHubUsername jbouchard -Version 1.0.0
```

This will create and push images:
- `jbouchard/events-api-gateway:1.0.0`
- `jbouchard/events-prospect-service:1.0.0`
- `jbouchard/events-student-service:1.0.0`
- `jbouchard/events-instructor-service:1.0.0`
- `jbouchard/events-event-relay:1.0.0`
- `jbouchard/events-projection-service:1.0.0`
- `jbouchard/events-frontend:1.0.0`

### Step 3: Use Your Images in Docker Compose

Update `docker-compose.yml` to use your Docker Hub images instead of building locally:

```yaml
services:
  api-gateway:
    image: jbouchard/events-api-gateway:1.0.0
    # Remove 'build' section
    ...
```

## Building Individual Services

To build a specific service manually:

```bash
# API Gateway
docker build -t events-api-gateway:latest -f src/services/ApiGateway/Dockerfile src/services

# Prospect Service
docker build -t events-prospect-service:latest -f src/services/ProspectService/Dockerfile src/services

# Frontend
docker build -t events-frontend:latest -f src/frontend/Dockerfile src/frontend
```

## Environment Configuration

### Development (Local Docker)

The `docker-compose.yml` includes development configuration:
- SQL Server with developer edition
- Local connection strings
- Azurite for Azure Storage emulation
- Development JWT secret (change for production!)

### Production Considerations

For production deployment:

1. **Change passwords**: Update SQL Server password in docker-compose.yml
2. **Update JWT secret**: Use a strong secret key
3. **Use external databases**: Replace SQL Server container with Azure SQL
4. **Configure Azure Service Bus**: Replace Azurite with real Azure Service Bus
5. **Configure Azure Event Grid**: Use real Event Grid topics
6. **Enable HTTPS**: Add SSL certificates and configure HTTPS
7. **Set production URLs**: Update CORS and API base URLs

## Preparing for Azure Container Apps (ACA)

### 1. Push Images to Azure Container Registry (ACR)

Instead of Docker Hub, you can use ACR for Azure deployments:

```bash
# Login to ACR
az acr login --name <your-acr-name>

# Tag images for ACR
docker tag events-api-gateway:latest <your-acr-name>.azurecr.io/events-api-gateway:1.0.0

# Push to ACR
docker push <your-acr-name>.azurecr.io/events-api-gateway:1.0.0
```

### 2. Create ACA Environment

```bash
az containerapp env create \
  --name events-env \
  --resource-group events-rg \
  --location eastus
```

### 3. Deploy Services to ACA

```bash
az containerapp create \
  --name api-gateway \
  --resource-group events-rg \
  --environment events-env \
  --image <your-acr-name>.azurecr.io/events-api-gateway:1.0.0 \
  --target-port 8080 \
  --ingress external \
  --env-vars \
    ASPNETCORE_ENVIRONMENT=Production \
    ConnectionStrings__DefaultConnection="<azure-sql-connection-string>"
```

Repeat for each service.

## Troubleshooting

### Services Not Starting

Check logs:
```bash
docker-compose logs -f <service-name>
```

### SQL Server Connection Issues

Ensure SQL Server is healthy:
```bash
docker-compose ps sqlserver
```

Check connection string in service logs.

### Port Conflicts

If ports are already in use, update the port mappings in `docker-compose.yml`:
```yaml
ports:
  - "5037:8080"  # Change 5037 to another port
```

### Database Not Creating

SQL Server needs time to initialize. Check health status:
```bash
docker exec events-sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "YourStrong@Passw0rd" -Q "SELECT 1"
```

### Frontend Not Connecting to API

Verify `VITE_API_BASE_URL` environment variable in frontend container:
```bash
docker exec events-frontend env | grep VITE_API_BASE_URL
```

## Docker Commands Reference

```bash
# Build all images
docker-compose build

# Start services in background
docker-compose up -d

# Start services with live logs
docker-compose up

# Stop services
docker-compose down

# Restart specific service
docker-compose restart api-gateway

# View running containers
docker-compose ps

# View logs
docker-compose logs -f

# Execute command in container
docker exec -it events-api-gateway bash

# Remove all containers and volumes
docker-compose down -v

# Pull latest images
docker-compose pull

# Rebuild without cache
docker-compose build --no-cache
```

## Next Steps

1. **Local Development**: Run services with `docker-compose up -d`
2. **Build Images**: Use build scripts to create Docker Hub images
3. **Test Locally**: Verify all services work together
4. **Push to Registry**: Upload images to Docker Hub or ACR
5. **Deploy to ACA**: Create Azure Container Apps from images
6. **Configure Production**: Set up Azure SQL, Service Bus, Event Grid
7. **Enable Monitoring**: Add Application Insights and logging

## Support

For issues or questions:
- Check service logs: `docker-compose logs -f <service-name>`
- Review Dockerfiles in each service directory
- Verify environment variables in `docker-compose.yml`
- Consult main README.md for architecture details
