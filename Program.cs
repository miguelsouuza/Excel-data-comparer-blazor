using DataComparer.Components;
using DataComparer.Interface;
using DataComparer.Models;
using DataComparer.Services;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Mvc;
using MudBlazor.Services;
using OfficeOpenXml;

var builder = WebApplication.CreateBuilder(args);
ExcelPackage.License.SetNonCommercialOrganization("BlazorApp");

// Add services to the container.
builder.Services.AddMudServices();
builder.Services.AddServerSideBlazor()
    .AddCircuitOptions(options => { options.DetailedErrors = true; });
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddScoped<IAppState, AppState>();
builder.Services.AddScoped<ICompareService, CompareService>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<IAutoMappingService, AutoMappingService>();
builder.Services.AddScoped<IHeaderCompareService, HeaderCompareService>();
builder.Services.AddScoped<IDataEnrichmentService, DataEnrichmentService>();
builder.Services.AddScoped<IDataQualityService, DataQualityService>();
builder.Services.AddScoped<IValidationService, ValidationService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

// ======================================================
// DOWNLOAD CSV
// ======================================================
app.MapGet("/download/baseb/csv", ([FromServices] IAppState state, [FromServices] IFileService svc) =>
    {
        var bytes = svc.GerarCsvBytes(state.BaseB, state.DelimitadorCsv);

        if (bytes == null || bytes.Length == 0)
            return Results.NotFound();

        var nome = state.NomeArquivoExportacao;

        if (string.IsNullOrWhiteSpace(nome))
        {
            nome = Path.GetFileNameWithoutExtension(state.NomeArquivoA);
        }

        return Results.File(
            bytes,
            "text/csv; charset=utf-8",
            $"{nome}.csv"
        );
    });


// ======================================================
// DOWNLOAD JSON
// ======================================================
app.MapGet("/download/baseb/json", ([FromServices] IAppState state) =>
    {
        if (state.BaseB == null || !state.BaseB.Any())
            return Results.NotFound();

        var json = System.Text.Json.JsonSerializer.Serialize(state.BaseB);
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        var nome = state.NomeArquivoExportacao;

        if (string.IsNullOrWhiteSpace(nome))
        {
            nome = Path.GetFileNameWithoutExtension(state.NomeArquivoA);
        }

        return Results.File(
            bytes,
            "application/json; charset=utf-8",
            $"{nome}.json"
        );
    });

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
