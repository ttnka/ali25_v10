using Ali25_V10.Data;
using Ali25_V10.Data.Modelos;  // Para W100_Org
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Ali25_V10.Data.Sistema;
using Radzen;
using Radzen.Blazor;

namespace Ali25_V10.Components.Pages.Admin;

public class MisDatosBase : ComponentBase, IDisposable
{
    [CascadingParameter] protected ApplicationUser CurrentUser { get; set; } = default!;
    [Inject] protected UserManager<ApplicationUser> UserManager { get; set; } = default!;
    [Inject] protected IRepoBitacora RepoBitacora { get; set; } = default!;
    [Inject] protected IRepo<W100_Org> RepoOrg { get; set; } = default!;
    [Inject] protected IRepo<ApplicationUser> RepoUser { get; set; } = default!;

    protected string currentPassword = "";
    protected string newPassword = "";
    protected string confirmPassword = "";
    protected bool isLoading;
    protected bool isSaving;
    protected bool isRefreshing;
    protected bool bypassCache;
    protected string? errorMessage;
    protected W100_Org? miOrg;
    protected bool showCurrentPassword;
    protected bool showNewPassword;
    protected bool showConfirmPassword;

    protected readonly CancellationTokenSource _ctsOperations = new(TimeSpan.FromSeconds(30));
    protected readonly CancellationTokenSource _ctsBitacora = new(TimeSpan.FromSeconds(5));

    protected override async Task OnInitializedAsync()
    {
        try
        {
            await LoadData();
            await RepoBitacora.AddBitacora(
                userId: CurrentUser.Id,
                desc: "Accediendo a datos personales",
                orgId: CurrentUser.OrgId,
                cancellationToken: _ctsBitacora.Token
            );
        }
        catch (Exception ex)
        {
            await LogError(ex, "OnInitializedAsync");
        }
    }

    protected async Task LoadData()
    {
        if (isLoading) return;

        try
        {
            isLoading = true;
            isRefreshing = true;

            var result = await RepoOrg.Get(
                CurrentUser.OrgId,
                CurrentUser,
                filtro: o => o.OrgId == CurrentUser.OrgId,
                byPassCache: bypassCache,
                cancellationToken: _ctsOperations.Token
            );

            if (!result.Exito)
            {
                throw new Exception(result.Texto);
            }

            if (result.DataVarios?.Any() == true)
            {
                miOrg = result.DataVarios.First();
            }
        }
        catch (OperationCanceledException)
        {
            await RepoBitacora.AddLog(
                userId: CurrentUser?.Id ?? "Sistema",
                orgId: CurrentUser?.OrgId ?? "Sistema",
                desc: "Operación de carga de datos cancelada por timeout",
                tipoLog: "Warning",
                origen: "MisDatosBase.LoadData",
                cancellationToken: _ctsBitacora.Token
            );
        }
        catch (Exception ex)
        {
            await LogError(ex, "LoadData");
        }
        finally
        {
            isLoading = false;
            isRefreshing = false;
            StateHasChanged();
        }
    }

    protected async Task RefreshData()
    {
        if (isRefreshing) return;
        
        try 
        {   
            isRefreshing = true;
            await LoadData();
        }
        catch (Exception ex)
        {
            await LogError(ex, "RefreshData");
        }
        finally
        {
            isRefreshing = false;
            StateHasChanged();
        }
    }

    protected void ToggleBypassCache()
    {
        bypassCache = !bypassCache;
    }

    protected async Task SaveChanges()
    {
        try
        {
            isSaving = true;
            if (string.IsNullOrEmpty(CurrentUser.Nombre) || 
                string.IsNullOrEmpty(CurrentUser.Paterno) || 
                string.IsNullOrEmpty(CurrentUser.Email))
            {
                throw new Exception("Todos los campos marcados son obligatorios");
            }

            if (!CurrentUser.Email.Contains("@"))
            {
                throw new Exception("Email no válido");
            }

            if (!string.IsNullOrEmpty(newPassword))
            {
                if (newPassword.Length < 6)
                {
                    throw new Exception("La nueva contraseña debe tener al menos 6 caracteres");
                }

                if (newPassword != confirmPassword)
                {
                    throw new Exception("Las contraseñas no coinciden");
                }

                var passwordCheck = await UserManager.CheckPasswordAsync(CurrentUser, currentPassword);
                if (!passwordCheck)
                {
                    throw new Exception("Contraseña actual incorrecta");
                }

                var passwordResult = await UserManager.ChangePasswordAsync(
                    CurrentUser, 
                    currentPassword, 
                    newPassword
                );

                if (!passwordResult.Succeeded)
                {
                    throw new Exception(string.Join(", ", passwordResult.Errors.Select(e => e.Description)));
                }
            }

            var result = await RepoUser.UpdateMisDatos(
                CurrentUser,
                CurrentUser.OrgId,
                CurrentUser,
                _ctsOperations.Token
            );

            if (!result.Exito)
            {
                throw new Exception(result.Texto);
            }

            await RepoBitacora.AddBitacora(
                userId: CurrentUser.Id,
                desc: "Datos personales actualizados",
                orgId: CurrentUser.OrgId,
                cancellationToken: _ctsBitacora.Token
            );

            currentPassword = "";
            newPassword = "";
            confirmPassword = "";
            showCurrentPassword = false;
            showNewPassword = false;
            showConfirmPassword = false;
        }
        catch (Exception ex)
        {
            await LogError(ex, "SaveChanges");
        }
        finally
        {
            isSaving = false;
            StateHasChanged();
        }
    }

    protected async Task LogError(Exception ex, string origen)
    {
        errorMessage = ex.Message;
        await RepoBitacora.AddLog(
            userId: CurrentUser.Id,
            desc: $"Error en {origen}: {ex.Message}",
            tipoLog: "Error",
            origen: $"MisDatosBase.{origen}",
            orgId: CurrentUser.OrgId,
            cancellationToken: _ctsBitacora.Token
        );
    }

    public void Dispose()
    {
        _ctsOperations.Dispose();
        _ctsBitacora.Dispose();
    }
} 