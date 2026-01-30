# Build stage
FROM node:20-alpine AS build
WORKDIR /app

# Accept API Gateway URL as build argument
ARG VITE_API_URL=https://ca-events-api-gateway-dev.icyhill-68ffa719.westus2.azurecontainerapps.io/api
ENV VITE_API_URL=$VITE_API_URL

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

# Copy nginx configuration template
COPY nginx.conf /etc/nginx/templates/default.conf.template

# Set default API Gateway URL (can be overridden by environment variable)
ENV API_GATEWAY_URL=http://localhost:5037

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
