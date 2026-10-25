# syntax=docker/dockerfile:1
FROM node:24-alpine AS build
RUN corepack enable && corepack prepare pnpm@12.8.1 --activate
WORKDIR /src

# Initialize: both SPAs share one pnpm workspace, so the lock file and every
# workspace manifest are required before a filtered install can resolve the tree.
COPY app/package.json app/pnpm-lock.yaml app/pnpm-workspace.yaml ./
COPY app/Web.Storefront/package.json Web.Storefront/
COPY app/Web.Admin/package.json Web.Admin/
RUN --mount=type=cache,target=/root/.local/share/pnpm/store \
    pnpm install --frozen-lockfile --filter @qingfa/storefront...

COPY app/Web.Storefront Web.Storefront
RUN pnpm --filter @qingfa/storefront build

FROM nginx:alpine
COPY --from=build /src/Web.Storefront/dist /usr/share/nginx/html
COPY app/Web.Storefront/nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
