# Build from the repository root:
#   docker build -f docker/Dockerfile -t dotnetmcptemplate-slug .

# ---------- ui (optional): builds MCP App bundles when ui/package.json exists ----------
FROM node:22-alpine AS ui
WORKDIR /repo
COPY ui/ ui/
COPY src/DotnetMcpTemplate/ui_dist/ src/DotnetMcpTemplate/ui_dist/
RUN if [ -f ui/package.json ] && [ -f ui/pnpm-lock.yaml ]; then \
      corepack enable && cd ui && pnpm install --frozen-lockfile && pnpm run build; \
    else echo "No UI lockfile: using committed ui_dist bundles"; fi

# ---------- build ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/DotnetMcpTemplate/DotnetMcpTemplate.csproj src/DotnetMcpTemplate/
RUN dotnet restore src/DotnetMcpTemplate/DotnetMcpTemplate.csproj
COPY src/ src/
COPY --from=ui /repo/src/DotnetMcpTemplate/ui_dist/ src/DotnetMcpTemplate/ui_dist/
RUN dotnet publish src/DotnetMcpTemplate/DotnetMcpTemplate.csproj -c Release --no-restore -o /app /p:UseAppHost=false

# ---------- runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV APP_PORT=8080 DOTNET_RUNNING_IN_CONTAINER=true AUTH_STORE_PATH=/data/auth
COPY --from=build /app .
# Sign-ins (AUTH_STORE=file) live in /data: mount a volume there so they survive container re-creation.
RUN mkdir -p /data/auth && chown -R $APP_UID /data && chmod 700 /data/auth
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "DotnetMcpTemplate.dll"]
