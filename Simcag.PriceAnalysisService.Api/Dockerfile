# Build context: raiz do repositório
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["shared/shared.csproj", "shared/"]
COPY ["price-analysis-service/Simcag.PriceAnalysisService.Api/Simcag.PriceAnalysisService.Api.csproj", "price-analysis-service/Simcag.PriceAnalysisService.Api/"]
COPY ["price-analysis-service/Simcag.PriceAnalysisService.Application/Simcag.PriceAnalysisService.Application.csproj", "price-analysis-service/Simcag.PriceAnalysisService.Application/"]
COPY ["price-analysis-service/Simcag.PriceAnalysisService.Domain/Simcag.PriceAnalysisService.Domain.csproj", "price-analysis-service/Simcag.PriceAnalysisService.Domain/"]
COPY ["price-analysis-service/Simcag.PriceAnalysisService.Infrastructure/Simcag.PriceAnalysisService.Infrastructure.csproj", "price-analysis-service/Simcag.PriceAnalysisService.Infrastructure/"]
RUN dotnet restore "price-analysis-service/Simcag.PriceAnalysisService.Api/Simcag.PriceAnalysisService.Api.csproj"
COPY shared/ shared/
COPY price-analysis-service/ price-analysis-service/
RUN dotnet publish "price-analysis-service/Simcag.PriceAnalysisService.Api/Simcag.PriceAnalysisService.Api.csproj" -c ${BUILD_CONFIGURATION} -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Simcag.PriceAnalysisService.Api.dll"]
