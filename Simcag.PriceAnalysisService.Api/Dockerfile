# ============================================================================
# Multi-stage — contexto de build: raiz da pasta price-analysis-service (monorepo)
# ou raiz do repositório só do serviço (pastas Simcag.* no topo).
# Simcag.Shared vem do GitHub Packages (TOKEN + NUGET_GITHUB_OWNER no build).
# ============================================================================

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS builder

WORKDIR /src

ARG BUILD_CONFIGURATION=Release
ARG TOKEN
ARG NUGET_GITHUB_OWNER

RUN if [ -n "${TOKEN:-}" ] && [ -n "${NUGET_GITHUB_OWNER:-}" ]; then \
    dotnet nuget add source "https://nuget.pkg.github.com/${NUGET_GITHUB_OWNER}/index.json" \
      --name github \
      --username "${NUGET_GITHUB_OWNER}" \
      --password "${TOKEN}" \
      --store-password-in-clear-text; \
  fi

COPY ["Simcag.PriceAnalysisService.Api/Simcag.PriceAnalysisService.Api.csproj", "Simcag.PriceAnalysisService.Api/"]
COPY ["Simcag.PriceAnalysisService.Application/Simcag.PriceAnalysisService.Application.csproj", "Simcag.PriceAnalysisService.Application/"]
COPY ["Simcag.PriceAnalysisService.Domain/Simcag.PriceAnalysisService.Domain.csproj", "Simcag.PriceAnalysisService.Domain/"]
COPY ["Simcag.PriceAnalysisService.Infrastructure/Simcag.PriceAnalysisService.Infrastructure.csproj", "Simcag.PriceAnalysisService.Infrastructure/"]

RUN dotnet restore "Simcag.PriceAnalysisService.Api/Simcag.PriceAnalysisService.Api.csproj"

COPY ["Simcag.PriceAnalysisService.Api/", "Simcag.PriceAnalysisService.Api/"]
COPY ["Simcag.PriceAnalysisService.Application/", "Simcag.PriceAnalysisService.Application/"]
COPY ["Simcag.PriceAnalysisService.Domain/", "Simcag.PriceAnalysisService.Domain/"]
COPY ["Simcag.PriceAnalysisService.Infrastructure/", "Simcag.PriceAnalysisService.Infrastructure/"]

RUN dotnet publish "Simcag.PriceAnalysisService.Api/Simcag.PriceAnalysisService.Api.csproj" \
    --configuration "${BUILD_CONFIGURATION}" \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

WORKDIR /app

RUN groupadd -r appgroup && useradd -r -g appgroup appuser

COPY --from=builder /app/publish .

USER root
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/* \
    && chown -R appuser:appgroup /app

ENV ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

USER appuser

HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
    CMD curl -fsS http://localhost:8080/health >/dev/null || exit 1

ENTRYPOINT ["dotnet", "Simcag.PriceAnalysisService.Api.dll"]
