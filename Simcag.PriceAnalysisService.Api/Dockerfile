FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Simcag.PriceAnalysisService.Api/Simcag.PriceAnalysisService.Api.csproj", "Simcag.PriceAnalysisService.Api/"]
COPY ["Simcag.PriceAnalysisService.Application/Simcag.PriceAnalysisService.Application.csproj", "Simcag.PriceAnalysisService.Application/"]
COPY ["Simcag.PriceAnalysisService.Domain/Simcag.PriceAnalysisService.Domain.csproj", "Simcag.PriceAnalysisService.Domain/"]
COPY ["Simcag.PriceAnalysisService.Infrastructure/Simcag.PriceAnalysisService.Infrastructure.csproj", "Simcag.PriceAnalysisService.Infrastructure/"]

RUN dotnet restore "Simcag.PriceAnalysisService.Api/Simcag.PriceAnalysisService.Api.csproj"
COPY . .
WORKDIR "/src/Simcag.PriceAnalysisService.Api"
RUN dotnet build "Simcag.PriceAnalysisService.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Simcag.PriceAnalysisService.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Simcag.PriceAnalysisService.Api.dll"]