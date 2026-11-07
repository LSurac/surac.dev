FROM node:26-alpine AS build

WORKDIR /app

COPY package*.json ./
RUN npm ci --no-audit --no-fund

COPY angular.json tsconfig*.json ./
COPY public/ public/
COPY src/ src/
RUN npm run build

FROM nginx:1.31-alpine AS runtime

# nginx and the Angular SSR process share this container so the stack keeps to the two-container
# <app>-web / <app>-api pattern. The server bundle is self-contained, so only the node binary is
# needed, taken from the build stage to pin the Node major to the one the build and tests ran on.
RUN apk add --no-cache libstdc++ libatomic
COPY --from=build /usr/local/bin/node /usr/local/bin/node
RUN node --version

# Non-root nginx: the pid file and the cache directory must be writable by the nginx user.
RUN sed -i 's#^pid.*#pid /tmp/nginx.pid;#' /etc/nginx/nginx.conf \
  && mkdir -p /var/cache/nginx && chown -R nginx:nginx /var/cache/nginx

COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY security-headers.conf /etc/nginx/security-headers.conf
COPY docker-entrypoint.sh /usr/local/bin/docker-entrypoint.sh
RUN chmod +x /usr/local/bin/docker-entrypoint.sh

COPY --from=build /app/dist/surac-dev/ /srv/surac-dev/

EXPOSE 8080
USER nginx

ENTRYPOINT ["/usr/local/bin/docker-entrypoint.sh"]
