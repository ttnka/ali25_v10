using Ali25_V10.Data;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Ali25_V10.Data.Sistema;
using Radzen;

namespace Ali25_V10.Components.Pages.Admin; 

public class ConfigBase : ComponentBase
{
    [Inject] protected AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
    [Inject] protected NavigationManager NavigationManager { get; set; } = default!;
    [Inject] protected UserManager<ApplicationUser> UserManager { get; set; } = default!;
    [Inject] protected IRepoBitacora RepoBitacora { get; set; } = default!;
    protected ApplicationUser? CurrentUser { get; set; }
    protected bool IsLoading { get; set; } = true;
    protected string? ErrorMessage { get; set; }

    protected override async Task OnInitializedAsync()
    {
        try
        {
            await RepoBitacora.AddBitacora(
                userId: "Prueba",
                desc: $"Accediendo a configuración INICIAL",
                orgId: "Prueba"
            );
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            if (!user.Identity?.IsAuthenticated ?? true)
            {
                NavigationManager.NavigateTo("/Account/Login");
                return;
            }

            // Obtener el usuario real de la base de datos
            if (user.Identity?.Name != null)
            {
                CurrentUser = await UserManager.FindByNameAsync(user.Identity.Name);
                
                if (CurrentUser == null || !CurrentUser.EsActivo)
                {
                    NavigationManager.NavigateTo("/Account/Login");
                    return;
                }
            }
            else
            {
                NavigationManager.NavigateTo("/Account/Login");
                return;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }
} 