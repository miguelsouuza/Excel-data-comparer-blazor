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
app.MapGet("/download/baseb/xlsx", async (AppState state) =>
{
    if (state.StreamB == null)
        return Results.NotFound();

    state.StreamB.Position = 0;

    using var originalPackage = new ExcelPackage(state.StreamB);
    using var outputPackage = new ExcelPackage();

    foreach (var wsOriginal in originalPackage.Workbook.Worksheets)
    {
        var nomeAba = Helpers.Normalize(wsOriginal.Name);

        // 🔹 existe alinhamento salvo?
        if (state.MapeamentosPorAba.TryGetValue(nomeAba, out var dadosAba))
        {
            var wsOut = outputPackage.Workbook.Worksheets.Add(wsOriginal.Name);

            var headersA = dadosAba.HeadersA;
            var mapping = dadosAba.Mapping;
            var registros = dadosAba.Registros;

            // cabeçalhos
            for (int c = 0; c < headersA.Count; c++)
            {
                wsOut.Cells[1, c + 1].Value = headersA[c];
            }

            // linhas
            for (int r = 0; r < registros.Count; r++)
            {
                var row = registros[r];

                for (int c = 0; c < headersA.Count; c++)
                {
                    var colunaA = headersA[c];

                    var colunaOrigem = colunaA;

                    if (mapping.TryGetValue(colunaA, out var mapped)
                        && !string.IsNullOrWhiteSpace(mapped))
                    {
                        colunaOrigem = mapped;
                    }

                    row.Campos.TryGetValue(colunaOrigem, out var valor);

                    wsOut.Cells[r + 2, c + 1].Value = valor ?? "";
                }
            }

            wsOut.Cells.AutoFitColumns();
        }
        else
        {
            // 🔹 copia aba original sem alterar
            var wsCopy = outputPackage.Workbook.Worksheets.Add(wsOriginal.Name);

            if (wsOriginal.Dimension != null)
            {
                int rows = wsOriginal.Dimension.End.Row;
                int cols = wsOriginal.Dimension.End.Column;

                for (int r = 1; r <= rows; r++)
                {
                    for (int c = 1; c <= cols; c++)
                    {
                        wsCopy.Cells[r, c].Value =
                            wsOriginal.Cells[r, c].Value;
                    }
                }

                wsCopy.Cells.AutoFitColumns();
            }
        }
    }

    var bytes = outputPackage.GetAsByteArray();

    var name = state.NomeArquivoExportacao
               ?? state.NomeArquivoA
               ?? "baseb";

    var baseName = Path.GetFileNameWithoutExtension(name);

    return Results.File(
        bytes,
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        $"{baseName}.xlsx");
});

app.MapGet("/download/baseb/json", (AppState state, HttpRequest request) =>
{
    if (state.BaseB == null || !state.BaseB.Any())
        return Results.NotFound();

    var json = System.Text.Json.JsonSerializer.Serialize(state.BaseB);
    var bytes = System.Text.Encoding.UTF8.GetBytes(json);

    var queryName = request.Query["name"].ToString();
    var name = !string.IsNullOrWhiteSpace(queryName)
        ? queryName
        : state.NomeArquivoExportacao
          ?? state.NomeArquivoA
          ?? "baseb";

    var baseName = System.IO.Path.GetFileNameWithoutExtension(name);
    var fileName = baseName + ".json";
    return Results.File(bytes, "application/json; charset=utf-8", fileName);
});

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
