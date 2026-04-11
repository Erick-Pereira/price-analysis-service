using Simcag.PriceAnalysisService.Application.Services;
using Simcag.PriceAnalysisService.Application.Interfaces;
using Simcag.PriceAnalysisService.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 🔥 AGORA COM INTERFACES
builder.Services.AddScoped<IPriceAnalysisService, PriceAnalysisService>();
builder.Services.AddScoped<IPriceStatisticsService, PriceStatisticsService>();
builder.Services.AddScoped<IPriceOutlierDetectionService, PriceOutlierDetectionService>();
builder.Services.AddScoped<IPriceRepository, InMemoryPriceRepository>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();