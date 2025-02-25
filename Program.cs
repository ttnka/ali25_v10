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
builder.Services.AddTransient<IRepo<ApplicationUser>, Repo<ApplicationUser, ApplicationDbContext>>();
builder.Services.AddTransient<IRepo<WConfig>, Repo<WConfig, BitacoraDbContext>>();
builder.Services.AddScoped<IRepoBitacora, RepoBitacora>();

builder.Services.AddTransient<IRepo<W100_Org>, Repo<W100_Org, ApplicationDbContext>>();
builder.Services.AddTransient<IRepo<W180_Files>, Repo<W180_Files, ApplicationDbContext>>();

builder.Services.AddTransient<IRepo<W210_Clientes>, Repo<W210_Clientes, ApplicationDbContext>>();
builder.Services.AddTransient<IRepo<W220_Folios>, Repo<W220_Folios, ApplicationDbContext>>();
builder.Services.AddTransient<IRepo<W221_Conceptos>, Repo<W221_Conceptos, ApplicationDbContext>>();
builder.Services.AddTransient<IRepo<W222_FolioDet>, Repo<W222_FolioDet, ApplicationDbContext>>();

builder.Services.AddTransient<IRepo<W280_ListaPrecios>, Repo<W280_ListaPrecios, ApplicationDbContext>>();
builder.Services.AddTransient<IRepo<W281_Productos>, Repo<W281_Productos, ApplicationDbContext>>();
builder.Services.AddTransient<IRepo<W282_Precios>, Repo<W282_Precios, ApplicationDbContext>>();

builder.Services.AddTransient<IRepo<W290_Formatos>, Repo<W290_Formatos, ApplicationDbContext>>();
builder.Services.AddTransient<IRepo<W291_FormatoGpo>, Repo<W291_FormatoGpo, ApplicationDbContext>>();
builder.Services.AddTransient<IRepo<W292_FormatoDet>, Repo<W292_FormatoDet, ApplicationDbContext>>();


// Radzen Services
builder.Services.AddRadzenComponents();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<TooltipService>();
builder.Services.AddScoped<ContextMenuService>();
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
