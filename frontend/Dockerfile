# Stage 1: Build the static site
FROM node:22-alpine AS build

WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci

COPY . .

# No app ID is baked in: the app reads it from GET /api/argus/config when it starts
RUN npm run build

# Stage 2: Serve it with nginx, which also proxies the API so the browser sees one origin
FROM nginx:1.29-alpine AS runtime

# Where nginx forwards /api and /.well-known. The default fits deployment-service.yml.
ENV API_UPSTREAM=http://dotnet-auth:8080

COPY nginx.conf.template /etc/nginx/templates/default.conf.template
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 8080
