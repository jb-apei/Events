#!/bin/bash
# Build and push Docker images to Docker Hub

set -e

# Parse arguments
DOCKERHUB_USERNAME=""
VERSION="latest"
NO_PUSH=false

while [[ $# -gt 0 ]]; do
    case $1 in
        -u|--username)
            DOCKERHUB_USERNAME="$2"
            shift 2
            ;;
        -v|--version)
            VERSION="$2"
            shift 2
            ;;
        --no-push)
            NO_PUSH=true
            shift
            ;;
        *)
            echo "Unknown option: $1"
            exit 1
            ;;
    esac
done

if [ -z "$DOCKERHUB_USERNAME" ]; then
    echo "Error: Docker Hub username is required"
    echo "Usage: ./build-and-push.sh -u <username> [-v <version>] [--no-push]"
    exit 1
fi

# Define services and their build contexts
declare -A services=(
    ["api-gateway"]="src/services:ApiGateway/Dockerfile"
    ["prospect-service"]="src/services:ProspectService/Dockerfile"
    ["student-service"]="src/services:StudentService/Dockerfile"
    ["instructor-service"]="src/services:InstructorService/Dockerfile"
    ["event-relay"]="src/services:EventRelay/Dockerfile"
    ["projection-service"]="src/services:ProjectionService/Dockerfile"
    ["frontend"]="src/frontend:Dockerfile"
)

echo "Building Docker images for Events project..."
echo "Docker Hub Username: $DOCKERHUB_USERNAME"
echo "Version: $VERSION"
echo ""

for service in "${!services[@]}"; do
    IFS=':' read -r context dockerfile <<< "${services[$service]}"
    
    image_name="$DOCKERHUB_USERNAME/events-$service"
    image_tag="${image_name}:${VERSION}"
    
    echo "Building $service..."
    echo "  Image: $image_tag"
    
    # Build the image
    docker build \
        -t "$image_tag" \
        -f "$context/$dockerfile" \
        "$context"
    
    # Also tag as latest
    docker tag "$image_tag" "${image_name}:latest"
    
    echo "  ✓ Built successfully"
    
    # Push to Docker Hub if not skipped
    if [ "$NO_PUSH" = false ]; then
        echo "  Pushing to Docker Hub..."
        
        docker push "$image_tag"
        docker push "${image_name}:latest"
        
        echo "  ✓ Pushed successfully"
    fi
    
    echo ""
done

echo "All images built successfully!"

if [ "$NO_PUSH" = false ]; then
    echo ""
    echo "Images pushed to Docker Hub:"
    for service in "${!services[@]}"; do
        echo "  - $DOCKERHUB_USERNAME/events-$service:$VERSION"
    done
fi

echo ""
echo "To run locally with Docker Compose:"
echo "  docker-compose up -d"
