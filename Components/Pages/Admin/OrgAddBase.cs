using Ali25_V10.Data;
using Ali25_V10.Data.Modelos;
using Ali25_V10.Data.Sistema;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Radzen;

namespace Ali25_V10.Components.Pages.Admin;

public class OrgAddBase : ComponentBase, IDisposable
{
    [CascadingParameter] protected ApplicationUser CurrentUser { get; set; } = default!;
    [Inject] protected IRepo<W100_Org> RepoOrg { get; set; } = default!;
    [Inject] protected UserManager<ApplicationUser> UserManager { get; set; } = default!;
    [Inject] protected IRepoBitacora RepoBitacora { get; set; } = default!;
    [Parameter] public string TipoToAdd { get; set; } = "Cliente";
    [Parameter] public EventCallback OnOrgCreated { get; set; }

    // Standard properties
    protected bool isLoading;
    protected bool isSaving;
    protected string? errorMessage;
    protected CancellationTokenSource cts = new(TimeSpan.FromSeconds(30));
    protected CancellationTokenSource quickCts = new(TimeSpan.FromSeconds(5));

    // Component specific properties
    protected W100_Org newOrg = new();
    protected ApplicationUser newAdmin = new();
    protected string adminEmail = "";
    protected string adminPassword = "";
    protected string adminConfirmPassword = "";
    protected Dictionary<int, string> niveles = new();
    protected int nivelSeleccionado;
    protected List<string> tiposOrg = new();
    protected string? rfcError;
    protected bool showPassword;
    protected bool showConfirmPassword;

    public void Dispose()
    {
        try { cts?.Cancel(); } catch { }
        try { cts?.Dispose(); } catch { }
        try { quickCts?.Cancel(); } catch { }
        try { quickCts?.Dispose(); } catch { }
        GC.SuppressFinalize(this);
    }

    protected override async Task OnInitializedAsync()
    {
        await LoadData();
        await base.OnInitializedAsync();
    }

    protected async Task LoadData()
    {
        try
        {
            isLoading = true;
            errorMessage = null;

            // Configurar tipos disponibles según nivel del usuario
            tiposOrg = CurrentUser.Nivel == 7 
                ? new List<string> { "Admin", "Cliente", "Proveedor" }
                : new List<string> { "Cliente", "Proveedor" };

            // Inicializar la organización con valores por defecto
            newOrg.Tipo = TipoToAdd;
            newOrg.Estado = 5;
            newOrg.Status = true;

            // Configurar niveles disponibles
            var nivelesArray = Constantes.Niveles
                .Split(',')
                .Select(n => n.Trim())
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .ToArray();

            // Crear diccionario temporal
            var tempNiveles = new Dictionary<int, string>();
            for (int i = 0; i < nivelesArray.Length; i++)
            {
                tempNiveles.Add(i + 1, nivelesArray[i]);
            }

            // Filtrar según TipoToAdd
            niveles = TipoToAdd switch
            {
                "Publico" => tempNiveles.Where(n => n.Key == 1).ToDictionary(x => x.Key, x => x.Value),
                "Proveedor" => tempNiveles.Where(n => n.Key == 2).ToDictionary(x => x.Key, x => x.Value),
                "Cliente" => tempNiveles.Where(n => n.Key == 3 || n.Key == 4).ToDictionary(x => x.Key, x => x.Value),
                _ => tempNiveles
            };
        }
        catch (Exception ex)
        {
            await LogError(ex, "Error al cargar datos iniciales");
        }
        finally
        {
            isLoading = false;
        }
    }

    protected async Task ValidateRfc(ChangeEventArgs e)
    {
        try
        {
            rfcError = null;
            var rfc = e.Value?.ToString()?.ToUpper() ?? "";

            if (string.IsNullOrEmpty(rfc))
            {
                rfcError = "RFC es requerido";
                return;
            }

            if (rfc.Length != 12 && rfc.Length != 13)
            {
                rfcError = "RFC debe tener 12 o 13 caracteres";
                return;
            }

            // Verificar RFC duplicado usando quickCts
            var allOrgs = await RepoOrg.GetAll(
                elUser: CurrentUser, 
                byPassCache: false, 
                cancellationToken: quickCts.Token);
            if (allOrgs.Exito && allOrgs.DataVarios != null)
            {
                var existingRfcs = new HashSet<string>(
                    allOrgs.DataVarios.Select(o => o.Rfc.ToUpper()),
                    StringComparer.OrdinalIgnoreCase
                );
                
                if (existingRfcs.Contains(rfc))
                {
                    rfcError = $"El RFC {rfc} ya existe en otra organización";
                }
            }
        }
        catch (Exception ex)
        {
            await LogError(ex, "Error al validar RFC");
        }
    }

    protected bool ValidateForm()
    {
        errorMessage = null;

        // Validar RFC
        if (string.IsNullOrEmpty(newOrg.Rfc) || (newOrg.Rfc.Length != 12 && newOrg.Rfc.Length != 13))
        {
            errorMessage = "RFC debe tener 12 o 13 caracteres";
            return false;
        }

        // Validar Comercial
        if (string.IsNullOrEmpty(newOrg.Comercial) || newOrg.Comercial.Length > 25)
        {
            errorMessage = "Nombre Comercial es obligatorio y no puede exceder 25 caracteres";
            return false;
        }

        // Validar Razón Social
        if (string.IsNullOrEmpty(newOrg.RazonSocial) || newOrg.RazonSocial.Length > 75)
        {
            errorMessage = "Razón Social es obligatoria y no puede exceder 75 caracteres";
            return false;
        }

        // Validar Nombre del administrador
        if (string.IsNullOrEmpty(newAdmin.Nombre) || newAdmin.Nombre.Length > 65)
        {
            errorMessage = "Nombre del administrador es obligatorio y no puede exceder 65 caracteres";
            return false;
        }

        // Validar Apellido Paterno
        if (string.IsNullOrEmpty(newAdmin.Paterno) || newAdmin.Paterno.Length > 65)
        {
            errorMessage = "Apellido Paterno del administrador es obligatorio y no puede exceder 65 caracteres";
            return false;
        }

        // Validar Apellido Materno (opcional pero con límite)
        if (newAdmin.Materno?.Length > 65)
        {
            errorMessage = "Apellido Materno del administrador no puede exceder 65 caracteres";
            return false;
        }

        // Validar Email del administrador
        if (string.IsNullOrEmpty(adminEmail) || adminEmail.Length > 256 || !adminEmail.Contains("@"))
        {
            errorMessage = "Email del administrador no es válido o excede 256 caracteres";
            return false;
        }

        // Validar Password
        if (string.IsNullOrEmpty(adminPassword) || adminPassword.Length < 6)
        {
            errorMessage = "La contraseña debe tener al menos 6 caracteres";
            return false;
        }

        if (adminPassword != adminConfirmPassword)
        {
            errorMessage = "Las contraseñas no coinciden";
            return false;
        }

        return true;
    }

    protected async Task SaveOrg()
    {
        try
        {
            if (!ValidateForm())
                return;

            isSaving = true;
            errorMessage = null;

            // 1. Crear la organización
            newOrg.Moral = newOrg.Rfc.Length == 12;
            var orgResult = await RepoOrg.Insert(
                entity: newOrg,
                orgId: CurrentUser.OrgId,
                elUser: CurrentUser,
                cancellationToken: cts.Token
            );
            
            if (!orgResult.Exito)
            {
                await LogError(new Exception(orgResult.Texto), "Error al crear organización");
                return;
            }

            // 2. Crear el usuario administrador
            newAdmin.UserName = adminEmail;
            newAdmin.Email = adminEmail;
            newAdmin.OrgId = newOrg.OrgId;
            newAdmin.Nivel = TipoToAdd == "Admin" ? 5 : nivelSeleccionado;
            newAdmin.Estado = 3;
            newAdmin.EsActivo = true;
            
            var userResult = await UserManager.CreateAsync(newAdmin, adminPassword);

            if (!userResult.Succeeded)
            {
                var errorMsg = string.Join(", ", userResult.Errors.Select(e => e.Description));
                await LogError(new Exception(errorMsg), "Error al crear usuario administrador");
                return;
            }

            // 3. Actualizar estado de la organización
            newOrg.Estado = 1;
            var updateResult = await RepoOrg.Update(
                entityToUpdate: newOrg,
                orgId: CurrentUser.OrgId,
                elUser: CurrentUser,
                cancellationToken: cts.Token
            );
            if (!updateResult.Exito)
            {
                await LogError(new Exception(updateResult.Texto), "Error al actualizar estado de la organización");
                return;
            }

            // Registrar en bitácora la creación exitosa
            await RepoBitacora.AddBitacora(
                userId: newAdmin.Id,
                desc: $"Se creó nueva organización - RFC: {newOrg.Rfc}, " +
                      $"Comercial: {newOrg.Comercial}, " +
                      $"Razón Social: {newOrg.RazonSocial}, " +
                      $"Tipo: {newOrg.Tipo}, " +
                      $"Administrador: {newAdmin.Nombre} {newAdmin.Paterno} {newAdmin.Materno}, " +
                      $"Email: {newAdmin.Email}",
                orgId: newOrg.OrgId
            );

            await OnOrgCreated.InvokeAsync();
            ResetForm();
        }
        catch (Exception ex)
        {
            await LogError(ex, "Error en proceso de creación de organización");
        }
        finally
        {
            isSaving = false;
        }
    }

    protected async Task LogError(Exception ex, string message)
    {
        errorMessage = $"{message}: {ex.Message}";
        await RepoBitacora.AddLog(
            userId: CurrentUser?.Id ?? "Sistema",
            orgId: CurrentUser?.OrgId ?? "Sistema",
            desc: $"{message}: {ex.Message}\nStackTrace: {ex.StackTrace}",
            tipoLog: "Error",
            origen: "OrgAddBase"
        );
    }

    protected void ResetForm()
    {
        newOrg = new();
        newOrg.Tipo = TipoToAdd;
        newOrg.Estado = 5;
        newOrg.Status = true;

        newAdmin = new();
        adminEmail = "";
        adminPassword = "";
        adminConfirmPassword = "";
        errorMessage = null;
        rfcError = null;
        showPassword = false;
        showConfirmPassword = false;
    }
} 