using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;
using Ali25_V10.Data;
using Ali25_V10.Data.Modelos;
using Ali25_V10.Data.Sistema;

namespace Ali25_V10.Components.Pages.Clientes;

public class FoliosBase : ComponentBase, IDisposable
{
    [CascadingParameter] protected ApplicationUser CurrentUser { get; set; } = default!;
    [Inject] protected IRepo<W220_Folios> RepoFolios { get; set; } = default!;
    [Inject] protected IRepo<W210_Clientes> RepoClientes { get; set; } = default!;
    [Inject] protected IRepo<W290_Formatos> RepoFormatos { get; set; } = default!;
    [Inject] protected IRepo<W100_Org> RepoOrg { get; set; } = default!;
    [Inject] protected IRepoBitacora RepoBitacora { get; set; } = default!;
    [Inject] protected NavigationManager NavigationManager { get; set; } = default!;

    protected RadzenDataGrid<W220_Folios> gridFolios = default!;
    protected IEnumerable<W220_Folios>? folios;
    protected IEnumerable<W210_Clientes>? clientes;
    protected IEnumerable<W100_Org>? organizaciones;
    protected IEnumerable<W290_Formatos>? formatos;
    protected List<W100_Org> organizacionesClientes = new();
    protected List<W220_Folios> foliosToInsert = new();
    protected List<W220_Folios> foliosToUpdate = new();
    protected int count;
    protected DataGridEditMode editMode = DataGridEditMode.Single;

    protected bool isLoading;
    protected bool isRefreshing;
    protected bool isEditing;
    protected bool bypassCache;
    protected string? errorMessage;

    protected readonly CancellationTokenSource _ctsOperations = new(TimeSpan.FromSeconds(30));
    protected readonly CancellationTokenSource _ctsBitacora = new(TimeSpan.FromSeconds(5));
    protected readonly CancellationTokenSource _ctsLogs = new(TimeSpan.FromSeconds(5));

    protected override async Task OnInitializedAsync()
    {
        try
        {
            await LoadData();
            await RepoBitacora.AddBitacora(
                userId: CurrentUser.Id,
                desc: "Accediendo a lista de folios",
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
            
            var foliosResult = await RepoFolios.Get(
                orgId: CurrentUser.OrgId,
                elUser: CurrentUser,
                byPassCache: bypassCache,
                cancellationToken: _ctsOperations.Token
            );

            if (foliosResult.Exito)
            {
                folios = foliosResult.DataVarios;
                count = folios?.Count() ?? 0;
            }

            var clientesResult = await RepoClientes.Get(
                orgId: CurrentUser.OrgId,
                elUser: CurrentUser,
                byPassCache: bypassCache,
                cancellationToken: _ctsOperations.Token
            );

            if (clientesResult.Exito)
            {
                clientes = clientesResult.DataVarios;
            }

            var orgsResult = await RepoOrg.Get(
                orgId: CurrentUser.OrgId,
                elUser: CurrentUser,
                byPassCache: bypassCache,
                cancellationToken: _ctsOperations.Token
            );

            if (orgsResult.Exito)
            {
                organizaciones = orgsResult.DataVarios;
                
                // Filtrar organizaciones que son clientes
                if (clientes != null && organizaciones != null)
                {
                    organizacionesClientes = organizaciones
                        .Where(org => clientes.Any(c => c.ClienteOrgId == org.OrgId))
                        .ToList();
                }
            }

            var formatosResult = await RepoFormatos.Get(
                orgId: CurrentUser.OrgId,
                elUser: CurrentUser,
                byPassCache: bypassCache,
                cancellationToken: _ctsOperations.Token
            );

            if (formatosResult.Exito)
            {
                formatos = formatosResult.DataVarios;
            }
        }
        catch (OperationCanceledException)
        {
            await RepoBitacora.AddLog(
                userId: CurrentUser?.Id ?? "Sistema",
                orgId: CurrentUser?.OrgId ?? "Sistema",
                desc: "Operación de carga de folios cancelada por timeout",
                tipoLog: "Warning",
                origen: "FoliosBase.LoadData",
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
        LoadData();
    }

    protected async Task LogError(Exception ex, string origen)
    {
        errorMessage = ex.Message;
        await RepoBitacora.AddLog(
            userId: CurrentUser.Id,
            desc: $"Error en {origen}: {ex.Message}",
            tipoLog: "Error",
            origen: $"FoliosBase.{origen}",
            orgId: CurrentUser.OrgId,
            cancellationToken: _ctsLogs.Token
        );
    }

    public void Dispose()
    {
        _ctsOperations.Dispose();
        _ctsBitacora.Dispose();
        _ctsLogs.Dispose();
    }
} 