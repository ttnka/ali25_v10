using Ali25_V10.Components;
using Ali25_V10.Components.Account;
using Ali25_V10.Data;
using Ali25_V10.Data.Modelos;
using Ali25_V10.Data.Sistema;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Radzen;
using Microsoft.Extensions.Caching.Memory;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
})
.AddIdentityCookies();

var bitacoraConnection = builder.Configuration.GetConnectionString("Bitacora_Connection");
var writeConnection = builder.Configuration.GetConnectionString("Write_Connection");
var readConnection = builder.Configuration.GetConnectionString("Read_Connection");

builder.Services.AddDbContext<BitacoraDbContext>(options =>
    options.UseMySql(bitacoraConnection, ServerVersion.AutoDetect(bitacoraConnection)));

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(writeConnection, ServerVersion.AutoDetect(writeConnection)));

builder.Services.AddDbContext<ReadDbContext>(options =>
    options.UseMySql(readConnection, ServerVersion.AutoDetect(readConnection)));

builder.Services.AddIdentityCore<ApplicationUser>(options => 
    options.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

// Agregar servicio de memoria caché
builder.Services.AddMemoryCache();
builder.Services.Configure<MemoryCacheOptions>(options =>
{
    options.SizeLimit = 1024; // 1MB
});

// Registrar servicios de repositorio
builder.Services.AddTransient<IRepo<W100_Org>, Repo<W100_Org, ApplicationDbContext>>();
builder.Services.AddTransient<IRepo<W180_Files>, Repo<W180_Files, ApplicationDbContext>>();

builder.Services.AddTransient<IRepo<ApplicationUser>, Repo<ApplicationUser, ApplicationDbContext>>();
builder.Services.AddTransient<IRepo<ZConfig>, Repo<ZConfig, BitacoraDbContext>>();
builder.Services.AddTransient<IRepo<W210_Clientes>, Repo<W210_Clientes, ApplicationDbContext>>();
// TEMPORAL_TEST_INICIO - Cambio de Transient a Scoped para diagnóstico
builder.Services.AddScoped<IRepoBitacora, RepoBitacora>();
// TEMPORAL_TEST_FIN

builder.Services.AddRadzenComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.Run();
