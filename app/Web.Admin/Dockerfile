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
    pnpm install --frozen-lockfile --filter @qingfa/admin...

COPY app/Web.Admin Web.Admin
RUN pnpm --filter @qingfa/admin build

FROM nginx:alpine
# Enforce: base '/admin/' rewrites URLs but does not nest the output directory, so the
# files must be mounted under html/admin for nginx.conf to resolve '/admin/...'.
COPY --from=build /src/Web.Admin/dist /usr/share/nginx/html/admin
COPY app/Web.Admin/nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
