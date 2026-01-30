# Quick Start - Docker Hub Deployment

## Step 1: Build and Push to Docker Hub

Replace `<your-dockerhub-username>` with your actual Docker Hub username:

```powershell
# Windows PowerShell
.\build-and-push.ps1 -DockerHubUsername <your-dockerhub-username>
```

Or with a specific version:
```powershell
.\build-and-push.ps1 -DockerHubUsername <your-dockerhub-username> -Version 1.0.0
```

To build without pushing (for testing):
```powershell
.\build-and-push.ps1 -DockerHubUsername <your-dockerhub-username> -NoPush
```

## Step 2: Run Locally with Docker Compose

```bash
# Start all services
docker-compose up -d

# Check status
docker-compose ps

# View logs
docker-compose logs -f

# Stop services
docker-compose down
```

## Access the Application

- **Frontend**: http://localhost:3000
- **API Gateway**: http://localhost:5037/swagger
- **Prospect Service**: http://localhost:5110/swagger
- **Student Service**: http://localhost:5120/swagger
- **Instructor Service**: http://localhost:5130/swagger
- **Projection Service**: http://localhost:5140/swagger

## Using Your Docker Hub Images

After pushing to Docker Hub, update `docker-compose.yml` to use your images:

```yaml
services:
  api-gateway:
    image: <your-dockerhub-username>/events-api-gateway:latest
    # Comment out the 'build' section
    environment:
      # ... rest of config
```

Then pull and run:
```bash
docker-compose pull
docker-compose up -d
```

## Azure Container Apps (Future)

Once images are on Docker Hub, you can deploy to Azure Container Apps:

```bash
# Deploy API Gateway to ACA
az containerapp create \
  --name events-api-gateway \
  --resource-group events-rg \
  --environment events-env \
  --image <your-dockerhub-username>/events-api-gateway:latest \
  --target-port 8080 \
  --ingress external \
  --env-vars ASPNETCORE_ENVIRONMENT=Production
```

See [docs/docker-deployment.md](docs/docker-deployment.md) for complete documentation.
