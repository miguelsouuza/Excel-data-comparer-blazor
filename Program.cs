using DataComparer.Components;
using DataComparer.Models;
using DataComparer.Services;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using MudBlazor.Services;
using OfficeOpenXml;

var builder = WebApplication.CreateBuilder(args);
ExcelPackage.License.SetNonCommercialOrganization("BlazorApp");

// Add services to the container.
builder.Services.AddMudServices();
builder.Services.AddServerSideBlazor().AddCircuitOptions(options => { options.DetailedErrors = true; });
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddSingleton<AppState>();
builder.Services.AddScoped<CompareService>();
builder.Services.AddScoped<FileService>();
builder.Services.AddScoped<AutoMappingService>();
builder.Services.AddScoped<ICompareService, CompareService>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<IAutoMappingService, AutoMappingService>();
builder.Services.AddScoped<IHeaderCompareService, HeaderCompareService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
//app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

// Endpoints para download da Base B atual em CSV ou XLSX
app.MapGet("/download/baseb/csv", (AppState state, FileService svc) =>
{
    var bytes = svc.GerarCsvBytes(state.BaseB, state.DelimitadorCsv);
    if (bytes == null || bytes.Length == 0)
        return Results.NotFound();

    var name = state.NomeArquivoExportacao ?? state.NomeArquivoB ?? "baseb";
    var baseName = System.IO.Path.GetFileNameWithoutExtension(name);
    var fileName = baseName + ".csv";
    return Results.File(bytes, "text/csv; charset=utf-8", fileName);
});

app.MapGet("/download/baseb/xlsx", async (
    AppState state,
    IFileService svc) =>
{
    if (state.StreamB == null)
        return Results.NotFound();

    var origBytes = state.StreamB.ToArray();

    var bytes = await svc.GerarExcelBytesMultiSheetAsync(
        origBytes,
        state.MapeamentosPorAba
    );

    if (bytes == null || bytes.Length == 0)
        return Results.NotFound();

    var name = state.NomeArquivoExportacao
               ?? state.NomeArquivoA
               ?? "baseb";

    var baseName = Path.GetFileNameWithoutExtension(name);

    return Results.File(
        bytes,
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        $"{baseName}.xlsx");
});

app.MapGet("/download/baseb/json", (AppState state) =>
{
    if (state.BaseB == null || !state.BaseB.Any())
        return Results.NotFound();

    var json = System.Text.Json.JsonSerializer.Serialize(state.BaseB);
    var bytes = System.Text.Encoding.UTF8.GetBytes(json);
    var name = state.NomeArquivoExportacao ?? state.NomeArquivoB ?? "baseb";
    var baseName = System.IO.Path.GetFileNameWithoutExtension(name);
    var fileName = baseName + ".json";
    return Results.File(bytes, "application/json; charset=utf-8", fileName);
});

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
