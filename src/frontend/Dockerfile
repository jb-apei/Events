# Build stage
FROM node:20-alpine AS build
WORKDIR /app

# Accept API Gateway URL as build argument
ARG VITE_API_URL=https://ca-events-api-gateway-dev.orangehill-95ada862.eastus2.azurecontainerapps.io/api
ENV VITE_API_URL=$VITE_API_URL

# Accept Build Version
ARG BUILD_VERSION=DEV
ENV VITE_APP_VERSION=$BUILD_VERSION

# Copy package files
COPY package*.json ./

# Install dependencies
RUN npm ci

# Copy source code
COPY . .

# Build the application with environment variable baked in
RUN npm run build

# Production stage
FROM nginx:alpine AS final
WORKDIR /usr/share/nginx/html

# Remove default nginx static assets
RUN rm -rf ./*

# Copy built assets from build stage
COPY --from=build /app/dist .

# Copy nginx configuration template to a custom location (to avoid default 20-envsubst-on-templates.sh processing)
COPY nginx.conf /etc/nginx/nginx.conf.template

# Set default API Gateway URL (can be overridden by environment variable)
ENV API_GATEWAY_URL=http://localhost:5037

EXPOSE 80

# Manual envsubst execution to strictly replace ONLY ${API_GATEWAY_URL} and preserve $uri, $host, etc.
CMD ["/bin/sh", "-c", "envsubst '${API_GATEWAY_URL}' < /etc/nginx/nginx.conf.template > /etc/nginx/conf.d/default.conf && nginx -g 'daemon off;'"]
