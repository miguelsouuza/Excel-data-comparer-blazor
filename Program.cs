using DataComparer.Components;
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
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

// Endpoints para download da Base B atual em CSV ou XLSX
app.MapGet("/download/baseb/csv", (AppState state, FileService svc) =>
{
    var bytes = svc.GerarCsvBytes(state.BaseB);
    if (bytes == null || bytes.Length == 0)
        return Results.NotFound();

    var name = state.NomeArquivoB ?? "baseb";
    var baseName = System.IO.Path.GetFileNameWithoutExtension(name);
    var fileName = baseName + ".csv";
    return Results.File(bytes, "text/csv; charset=utf-8", fileName);
});

app.MapGet("/download/baseb/xlsx", async (AppState state, FileService svc) =>
{
    // se tivermos o stream original salvo, geramos um XLSX multi-aba preservando nomes
    if (state.StreamB != null)
    {
        var origBytes = state.StreamB.ToArray();
        var headersA = state.BaseA?.FirstOrDefault()?.Campos.Keys.ToList();
        var mapping = state.Mapeamento;

        // tentar obter nomes das abas da Base A (se o stream A estiver disponível)
        List<string>? desiredNames = null;
        if (state.StreamA != null)
        {
            try
            {
                using var msA = new MemoryStream();
                state.StreamA.Position = 0;
                await state.StreamA.CopyToAsync(msA);
                msA.Position = 0;
                using var pkgA = new ExcelPackage(msA);
                desiredNames = pkgA.Workbook.Worksheets.Select(ws => ws.Name).ToList();
            }
            catch
            {
                desiredNames = null;
            }
        }

        var bytes = await svc.GerarExcelBytesMultiSheetAsync(origBytes, headersA, mapping, desiredNames);
        if (bytes == null || bytes.Length == 0)
            return Results.NotFound();

        var name = state.NomeArquivoB ?? "baseb";
        var baseName = System.IO.Path.GetFileNameWithoutExtension(name);
        var fileName = baseName + ".xlsx";
        return Results.File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    // fallback: gerar a partir do BaseB único
    var single = svc.GerarExcelBytes(state.BaseB);
    if (single == null || single.Length == 0)
        return Results.NotFound();

    var fname = state.NomeArquivoB ?? "baseb";
    var fn = System.IO.Path.GetFileNameWithoutExtension(fname) + ".xlsx";
    return Results.File(single, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fn);
});

app.MapGet("/download/baseb/json", (AppState state) =>
{
    if (state.BaseB == null || !state.BaseB.Any())
        return Results.NotFound();

    var json = System.Text.Json.JsonSerializer.Serialize(state.BaseB);
    var bytes = System.Text.Encoding.UTF8.GetBytes(json);
    var name = state.NomeArquivoB ?? "baseb";
    var baseName = System.IO.Path.GetFileNameWithoutExtension(name);
    var fileName = baseName + ".json";
    return Results.File(bytes, "application/json; charset=utf-8", fileName);
});

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
