FROM docker.io/library/node:24-alpine AS build
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN npm run build

FROM docker.io/library/nginx:alpine AS final

ENV NGINX_ENTRYPOINT_LOCAL_RESOLVERS=1 \
    NGINX_ENVSUBST_FILTER=^(NGINX_LOCAL_RESOLVERS|SCDC_COMMUNITY_UPSTREAM|SCDC_HEALTH_UPSTREAM|SCDC_SAMPLE_ENABLED)$ \
    SCDC_COMMUNITY_UPSTREAM=chat-service:8080 \
    SCDC_HEALTH_UPSTREAM=chat-service:8080/api/v1/health \
    SCDC_SAMPLE_ENABLED=false

COPY nginx.conf /etc/nginx/templates/default.conf.template
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80

HEALTHCHECK --interval=10s --timeout=3s --retries=5 \
  CMD wget --quiet --tries=1 --spider http://127.0.0.1/healthz || exit 1

CMD ["nginx", "-g", "daemon off;"]
