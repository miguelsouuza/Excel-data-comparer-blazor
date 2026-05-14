using DataComparer.Components;
using DataComparer.Models;
using DataComparer.Services;
using Microsoft.Win32.SafeHandles;
using MudBlazor.Services;
using OfficeOpenXml;
using System.Xml.Linq;

var builder = WebApplication.CreateBuilder(args);
ExcelPackage.License.SetNonCommercialOrganization("BlazorApp");

// Add services to the container.
builder.Services.AddMudServices();
builder.Services.AddServerSideBlazor()
    .AddCircuitOptions(options => { options.DetailedErrors = true; });
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddScoped<AppState>();
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
app.MapGet("/download/baseb/csv", (HttpRequest request,AppState state, FileService svc) =>
{
    var bytes = svc.GerarCsvBytes(state.BaseB, state.DelimitadorCsv);
    if (bytes == null || bytes.Length == 0)
        return Results.NotFound();

    var queryName = request.Query["name"].ToString();    
    var name = !string.IsNullOrWhiteSpace(queryName)
        ? queryName
        : state.NomeArquivoB ?? "baseb";

    var fileName = Helpers.SafeFileName(name, ".csv");

    return Results.File(bytes, "text/csv; charset=utf-8", fileName);
});

app.MapGet("/download/baseb/xlsx", async (HttpRequest request, AppState state) =>
{
    // Garantir streamB posicionado no início
    state.StreamB.Position = 0;
    byte[] origBytes = state.StreamB.ToArray();
    List<SheetMapping> mappings = state.MapeamentosPorAba;
    // Abrir BaseA a partir de state.StreamA
    using var msA = new MemoryStream(origBytes);
    using var pkgA = new ExcelPackage(msA);
    // Para cada aba de BaseA, criar uma planilha no resultado final
    var workbook = new ExcelPackage();
    foreach (var wsA in pkgA.Workbook.Worksheets)
    {
        string key = Helpers.Normalize(wsA.Name);
        // Verificar se há dados para esta aba em BaseBPorAba
        if (state.BaseBPorAba.TryGetValue(key, out var sheetData))
        {
            // use sheetData.Registros, sheetData.HeadersA, sheetData.Mapping
            var dadosB = sheetData.Registros;
            var headersA = sheetData.HeadersA;
            var mapping = sheetData.Mapping;
            // Copiar wsA para worksheet de saída e aplicar mapping
            var wsOut = workbook.Workbook.Worksheets.Add(wsA.Name);
            // ... código para preencher cabeçalhos e dados ...
        }
        else
        {
            // Aba de BaseA sem alinhamento correspondente: pode pular ou tratar fallback
        }
    }
    var resultBytes = workbook.GetAsByteArray();
    return Results.File(resultBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        Helpers.SafeFileName(request.Query["name"],".xlsx"));
});


app.MapGet("/download/baseb/json", (HttpRequest request,AppState state) =>
{
    if (state.BaseB == null || !state.BaseB.Any())
        return Results.NotFound();

    var json = System.Text.Json.JsonSerializer.Serialize(state.BaseB);
    var bytes = System.Text.Encoding.UTF8.GetBytes(json);

    var queryName = request.Query["name"].ToString();
    var name = !string.IsNullOrWhiteSpace(queryName)
        ? queryName
        : state.NomeArquivoB ?? "baseb";

    var fileName = Helpers.SafeFileName(name, ".json");

    return Results.File(bytes, "application/json; charset=utf-8", fileName);
});

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
