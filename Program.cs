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

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
