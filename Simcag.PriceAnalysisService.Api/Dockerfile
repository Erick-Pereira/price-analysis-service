# ============================================================================
# Multi-stage — contexto de build: raiz do monorepo simcag
# ============================================================================
# Inclui: shared/, price-analysis-service/ (sem dependência de outros serviços).
# Para “repo só do serviço” + Simcag.Shared via GitHub Packages, ver padrão
# do Ingestion (ARG TOKEN + dotnet nuget add source).
# ============================================================================

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS builder

WORKDIR /src

ARG BUILD_CONFIGURATION=Release

# Opcional: feed privado (descomente se migrar shared para PackageReference Simcag.Shared)
# ARG NUGET_AUTH_TOKEN
# RUN dotnet nuget add source "https://nuget.pkg.github.com/OWNER/index.json" ...

# Camada 1: só .csproj (cache de restore)
COPY ["shared/shared.csproj", "shared/"]
COPY ["price-analysis-service/Simcag.PriceAnalysisService.Domain/Simcag.PriceAnalysisService.Domain.csproj", "price-analysis-service/Simcag.PriceAnalysisService.Domain/"]
COPY ["price-analysis-service/Simcag.PriceAnalysisService.Application/Simcag.PriceAnalysisService.Application.csproj", "price-analysis-service/Simcag.PriceAnalysisService.Application/"]
COPY ["price-analysis-service/Simcag.PriceAnalysisService.Infrastructure/Simcag.PriceAnalysisService.Infrastructure.csproj", "price-analysis-service/Simcag.PriceAnalysisService.Infrastructure/"]
COPY ["price-analysis-service/Simcag.PriceAnalysisService.Api/Simcag.PriceAnalysisService.Api.csproj", "price-analysis-service/Simcag.PriceAnalysisService.Api/"]

RUN dotnet restore "price-analysis-service/Simcag.PriceAnalysisService.Api/Simcag.PriceAnalysisService.Api.csproj"

# Camada 2: código-fonte
COPY shared/ shared/
COPY price-analysis-service/ price-analysis-service/

RUN dotnet publish "price-analysis-service/Simcag.PriceAnalysisService.Api/Simcag.PriceAnalysisService.Api.csproj" \
    --configuration "${BUILD_CONFIGURATION}" \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ============================================================================
# Runtime
# ============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

LABEL maintainer="SIMCAG Team <dev@simcag.com>"
LABEL description="Price Analysis Service"

WORKDIR /app

RUN groupadd -r appgroup && useradd -r -g appgroup appuser

COPY --from=builder /app/publish .

USER root
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/* \
    && chown -R appuser:appgroup /app

ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

USER appuser

HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
    CMD curl -fsS http://localhost:8080/health >/dev/null || exit 1

ENTRYPOINT ["dotnet", "Simcag.PriceAnalysisService.Api.dll"]
