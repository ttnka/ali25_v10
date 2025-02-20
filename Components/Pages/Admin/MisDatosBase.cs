using Ali25_V10.Data;
using Ali25_V10.Data.Modelos;  // Para W100_Org
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Ali25_V10.Data.Sistema;

namespace Ali25_V10.Components.Pages.Admin;

public class MisDatosBase : ComponentBase
{
    [CascadingParameter] protected ApplicationUser CurrentUser { get; set; } = default!;
    [Inject] protected UserManager<ApplicationUser> UserManager { get; set; } = default!;
    [Inject] protected IRepoBitacora RepoBitacora { get; set; } = default!;
    [Inject] protected IRepo<W100_Org> RepoOrg { get; set; } = default!;

    protected string currentPassword = "";
    protected string newPassword = "";
    protected string confirmPassword = "";
    protected bool isSaving;
    protected string? errorMessage;
    protected W100_Org? miOrg;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var result = await RepoOrg.Get(
                orgId: CurrentUser.OrgId,
                elUser: CurrentUser
            );
            
            if (result.Exito && result.DataVarios?.Any() == true)
            {
                miOrg = result.DataVarios.First();
            }
        }
        catch (Exception ex)
        {
            await RepoBitacora.AddLog(
                userId: CurrentUser.Id,
                orgId: CurrentUser.OrgId,
                desc: $"Error al cargar datos: {ex.Message}",
                tipoLog: "Error",
                origen: "MisDatosBase.OnInitializedAsync"
            );
            errorMessage = "Error al cargar datos de la organización";
        }
    }

    protected async Task SaveChanges()
    {
        try
        {
            isSaving = true;
            errorMessage = null;

            // Validar email
            if (string.IsNullOrEmpty(CurrentUser.Email) || !CurrentUser.Email.Contains("@"))
            {
                errorMessage = "Email no válido";
                return;
            }

            // Validar contraseña si se está cambiando
            if (!string.IsNullOrEmpty(newPassword))
            {
                if (newPassword.Length < 6)
                {
                    errorMessage = "La nueva contraseña debe tener al menos 6 caracteres";
                    return;
                }

                if (newPassword != confirmPassword)
                {
                    errorMessage = "Las contraseñas no coinciden";
                    return;
                }

                var passwordCheck = await UserManager.CheckPasswordAsync(CurrentUser, currentPassword);
                if (!passwordCheck)
                {
                    errorMessage = "Contraseña actual incorrecta";
                    return;
                }

                var passwordResult = await UserManager.ChangePasswordAsync(
                    CurrentUser, 
                    currentPassword, 
                    newPassword
                );

                if (!passwordResult.Succeeded)
                {
                    errorMessage = string.Join(", ", passwordResult.Errors.Select(e => e.Description));
                    return;
                }
            }

            var result = await UserManager.UpdateAsync(CurrentUser);
            if (!result.Succeeded)
            {
                errorMessage = string.Join(", ", result.Errors.Select(e => e.Description));
                return;
            }

            await RepoBitacora.AddBitacora(
                userId: CurrentUser.Id,
                desc: "Datos personales actualizados",
                orgId: CurrentUser.OrgId
            );

            // Limpiar campos de contraseña
            currentPassword = "";
            newPassword = "";
            confirmPassword = "";
        }
        catch (Exception ex)
        {
            await RepoBitacora.AddLog(
                userId: CurrentUser.Id,
                orgId: CurrentUser.OrgId,
                desc: $"Error al actualizar datos: {ex.Message}",
                tipoLog: "Error",
                origen: "MisDatosBase.SaveChanges"
            );
            errorMessage = "Error al guardar los cambios";
        }
        finally
        {
            isSaving = false;
        }
    }
} 