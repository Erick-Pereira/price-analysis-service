# Build context: raiz do repositório
ARG DOTNET_SDK_VERSION=10.0
ARG DOTNET_RUNTIME_VERSION=10.0

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_SDK_VERSION} AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files (layer 1)
COPY ["shared/shared.csproj", "shared/"]
COPY ["price-analysis-service/Simcag.PriceAnalysisService.Api/Simcag.PriceAnalysisService.Api.csproj", "price-analysis-service/Simcag.PriceAnalysisService.Api/"]
COPY ["price-analysis-service/Simcag.PriceAnalysisService.Application/Simcag.PriceAnalysisService.Application.csproj", "price-analysis-service/Simcag.PriceAnalysisService.Application/"]
COPY ["price-analysis-service/Simcag.PriceAnalysisService.Domain/Simcag.PriceAnalysisService.Domain.csproj", "price-analysis-service/Simcag.PriceAnalysisService.Domain/"]
COPY ["price-analysis-service/Simcag.PriceAnalysisService.Infrastructure/Simcag.PriceAnalysisService.Infrastructure.csproj", "price-analysis-service/Simcag.PriceAnalysisService.Infrastructure/"]

# Restore (layer 2)
RUN dotnet restore "price-analysis-service/Simcag.PriceAnalysisService.Api/Simcag.PriceAnalysisService.Api.csproj"

# Copy source (layer 3)
COPY shared/ shared/
COPY price-analysis-service/ price-analysis-service/

# Publish (layer 4)
RUN dotnet publish "price-analysis-service/Simcag.PriceAnalysisService.Api/Simcag.PriceAnalysisService.Api.csproj" -c ${BUILD_CONFIGURATION} -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_RUNTIME_VERSION} AS final

LABEL maintainer="SIMCAG Team <dev@simcag.com>"
LABEL description="Price Analysis Service - Price analysis and comparison"
LABEL version="1.0.0"

WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .

HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

EXPOSE 8080

RUN useradd -m -u 1000 appuser && chown -R appuser:appuser /app
USER appuser

ENTRYPOINT ["dotnet", "Simcag.PriceAnalysisService.Api.dll"]
