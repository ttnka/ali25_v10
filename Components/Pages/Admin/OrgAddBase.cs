using Ali25_V10.Data;
using Ali25_V10.Data.Modelos;
using Ali25_V10.Data.Sistema;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Radzen;

namespace Ali25_V10.Components.Pages.Admin;

public class OrgAddBase : ComponentBase
{
    [CascadingParameter] protected ApplicationUser CurrentUser { get; set; } = default!;
    [Inject] protected IRepo<W100_Org> RepoOrg { get; set; } = default!;
    [Inject] protected UserManager<ApplicationUser> UserManager { get; set; } = default!;
    [Inject] protected IRepoBitacora RepoBitacora { get; set; } = default!;
    [Parameter] public string TipoToAdd { get; set; } = "Cliente";
    [Parameter] public EventCallback OnOrgCreated { get; set; }

    protected W100_Org newOrg = new();
    protected ApplicationUser newAdmin = new();
    protected string adminEmail = "";
    protected string adminPassword = "";
    protected string adminConfirmPassword = "";
    protected bool isSaving;
    protected string? errorMessage;
    protected Dictionary<int, string> niveles = new();
    protected int nivelSeleccionado;
    protected List<string> tiposOrg = new();

    protected override void OnInitialized()
    {
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

        base.OnInitialized();
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

            // Verificar RFC duplicado
            var allOrgs = await RepoOrg.GetAll(CurrentUser);
            if (allOrgs.Exito && allOrgs.DataVarios != null)
            {
                var existingRfcs = new HashSet<string>(
                    allOrgs.DataVarios.Select(o => o.Rfc.ToUpper()),
                    StringComparer.OrdinalIgnoreCase
                );
                
                if (existingRfcs.Contains(newOrg.Rfc.ToUpper()))
                {
                    errorMessage = $"El RFC {newOrg.Rfc} ya existe en otra organización";
                    return;
                }
            }

            isSaving = true;

            // 1. Crear la organización
            newOrg.Moral = newOrg.Rfc.Length == 12;
            var orgResult = await RepoOrg.Insert(newOrg, CurrentUser.OrgId, elUser: CurrentUser);
            
            if (!orgResult.Exito)
            {
                await RepoBitacora.AddLog(
                    userId: CurrentUser?.Id ?? "Sistema",
                    orgId: CurrentUser?.OrgId ?? "Sistema",
                    desc: $"Error al crear organización: {orgResult.Texto}",
                    tipoLog: "Error",
                    origen: "OrgAddBase.SaveOrg"
                );
                throw new Exception($"Error al crear organización: {orgResult.Texto}");
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
                await RepoBitacora.AddLog(
                    userId: CurrentUser?.Id ?? "Sistema",
                    orgId: CurrentUser?.OrgId ?? "Sistema",
                    desc: $"Error al crear usuario administrador: {errorMsg}",
                    tipoLog: "Error",
                    origen: "OrgAddBase.SaveOrg"
                );
                throw new Exception($"Error al crear usuario: {errorMsg}");
            }

            // 3. Actualizar estado de la organización
            newOrg.Estado = 1;
            var updateResult = await RepoOrg.Update(newOrg, CurrentUser.OrgId, elUser: CurrentUser);
            if (!updateResult.Exito)
            {
                throw new Exception($"Error al actualizar estado de la organización: {updateResult.Texto}");
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
        }
        catch (Exception ex)
        {
            await RepoBitacora.AddLog(
                userId: CurrentUser?.Id ?? "Sistema",
                orgId: CurrentUser?.OrgId ?? "Sistema",
                desc: $"Error en proceso de creación de organización: {ex.Message}\nStackTrace: {ex.StackTrace}",
                tipoLog: "Error",
                origen: "OrgAddBase.SaveOrg"
            );
            errorMessage = ex.Message;
        }
        finally
        {
            isSaving = false;
        }
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
    }
} 