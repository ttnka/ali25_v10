using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Routing;
using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Ali25_V10.Data;
using Ali25_V10.Data.Modelos;
using Ali25_V10.Data.Sistema;
using Radzen;
using Radzen.Blazor;

namespace Ali25_V10.Components.Pages.Admin
{
    public class IndexAdminBase : ComponentBase, IDisposable
    {
        [Inject] protected AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
        [Inject] protected NavigationManager Navigation { get; set; } = default!;
        [Inject] protected IRepo<ApplicationUser> RepoUser { get; set; } = default!;
        [Inject] protected IRepoBitacora RepoBitacora { get; set; } = default!;

        protected readonly CancellationTokenSource _ctsBitacora = new(TimeSpan.FromSeconds(5));
        protected ApplicationUser CurrentUser { get; set; } = default!;
        protected override async Task OnInitializedAsync()
        {
            try
            {
                var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
                var user = authState.User;
                if (!user.Identity.IsAuthenticated)
                {
                    Navigation.NavigateTo("/login");
                    return;
                }
                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId != null)
                {
                    var result = await RepoUser.GetById(userId, 
                                                        "Sistema", 
                                                        null,
                                                        byPassCache: true,
                                                        cancellationToken: _ctsBitacora.Token);
                    CurrentUser = result.DataUno;
                }
            }
            catch (Exception ex)
            {
                await RepoBitacora.AddLog(
                    desc: ex.Message,
                    tipoLog: "Error",
                    origen: "Index Administracion",
                    userId: CurrentUser.UserId ?? "Sistema",
                    orgId: CurrentUser.OrgId ?? "Sistema",
                    cancellationToken: _ctsBitacora.Token
                );
            }
        }
        public void Dispose()
        {
            _ctsBitacora.Dispose();
        }
    }
}