using Ali25_V10.Data;
using Ali25_V10.Data.Modelos;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Ali25_V10.Data.Sistema;
using Radzen;
using Radzen.Blazor;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ali25_V10.Components.Pages.Admin; 

public class ConfigBase : ComponentBase, IDisposable
{
    [Inject] protected AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
    [Inject] protected NavigationManager NavigationManager { get; set; } = default!;
    [Inject] protected UserManager<ApplicationUser> UserManager { get; set; } = default!;
    [Inject] protected IRepoBitacora RepoBitacora { get; set; } = default!;
    [CascadingParameter] protected ApplicationUser CurrentUser { get; set; } = default!;
    [Inject] protected IRepo<WConfig> RepoConfig { get; set; } = default!;

    protected RadzenDataGrid<WConfig> gridConfig = default!;
    protected IEnumerable<WConfig>? configs;
    protected bool isLoading;
    protected bool isRefreshing;
    protected bool isEditing;
    protected bool bypassCache;
    protected string? errorMessage;

    // Tokens para operaciones
    protected readonly CancellationTokenSource _ctsOperations = new(TimeSpan.FromSeconds(30));
    protected readonly CancellationTokenSource _ctsBitacora = new(TimeSpan.FromSeconds(5));

    protected override async Task OnInitializedAsync()
    {
        try
        {
            isLoading = true;
            await RepoBitacora.AddBitacora(
                userId: CurrentUser.Id,
                desc: "Accediendo a configuración",
                orgId: CurrentUser.OrgId,
                cancellationToken: _ctsBitacora.Token
            );
            await LoadData();
        }
        catch (Exception ex)
        {
            await LogError(ex, "OnInitializedAsync");
        }
        finally
        {
            isLoading = false;
        }
    }

    protected async Task LoadData()
    {
        try
        {
            var result = await RepoConfig.Get(
                CurrentUser.OrgId,
                CurrentUser,
                cancellationToken: _ctsOperations.Token
            );

            if (result.Exito)
            {
                configs = result.DataVarios;
            }
        }
        catch (Exception ex)
        {
            await LogError(ex, "LoadData");
        }
    }

    protected async Task LogError(Exception ex, string origen)
    {
        errorMessage = ex.Message;
        await RepoBitacora.AddLog(
            userId: CurrentUser.Id,
            desc: $"Error en {origen}: {ex.Message}",
            tipoLog: "Error",
            origen: $"ConfigBase.{origen}",
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