using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;
using Ali25_V10.Data;
using Ali25_V10.Data.Modelos;
using Ali25_V10.Data.Sistema;

namespace Ali25_V10.Components.Pages.Clientes;

public class FolioConceptosBase : ComponentBase, IDisposable
{
    [CascadingParameter] public ApplicationUser CurrentUser { get; set; } = default!;
    [Parameter] public string FolioId { get; set; } = string.Empty;
    [Inject] protected IRepo<W221_Conceptos> RepoConceptos { get; set; } = default!;
    [Inject] protected IRepo<W281_Productos> RepoProductos { get; set; } = default!;
    [Inject] protected IRepo<W282_Precios> RepoPrecios { get; set; } = default!;
    [Inject] protected IRepo<W280_ListaPrecios> RepoListaPrecios { get; set; } = default!;
    [Inject] protected IRepo<W220_Folios> RepoFolios { get; set; } = default!;
    [Inject] protected IRepoBitacora RepoBitacora { get; set; } = default!;
    [Inject] protected NavigationManager NavigationManager { get; set; } = default!;

    protected RadzenDataGrid<W221_Conceptos> gridConceptos = default!;
    protected W220_Folios? folio;
    protected IEnumerable<W221_Conceptos>? conceptos;
    protected IEnumerable<W281_Productos>? productos;
    protected IEnumerable<W282_Precios>? precios;
    protected List<W221_Conceptos> conceptosToInsert = new();
    protected List<W221_Conceptos> conceptosToUpdate = new();
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
                desc: $"Accediendo a conceptos del folio {FolioId}",
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

            var folioResult = await RepoFolios.GetById(
                FolioId,
                CurrentUser.OrgId,
                CurrentUser,
                bypassCache,
                _ctsOperations.Token
            );

            if (folioResult.Exito)
            {
                folio = folioResult.DataVarios?.FirstOrDefault();
            }

            var conceptosResult = await RepoConceptos.Get(
                orgId: CurrentUser.OrgId,
                elUser: CurrentUser,
                filtro: c => c.FolioId == FolioId,
                byPassCache: bypassCache,
                cancellationToken: _ctsOperations.Token
            );

            if (conceptosResult.Exito)
            {
                conceptos = conceptosResult.DataVarios;
                count = conceptos?.Count() ?? 0;
            }

            var productosResult = await RepoProductos.Get(
                orgId: CurrentUser.OrgId,
                elUser: CurrentUser,
                byPassCache: bypassCache,
                cancellationToken: _ctsOperations.Token
            );

            if (productosResult.Exito)
            {
                productos = productosResult.DataVarios;
            }

            var preciosResult = await RepoPrecios.Get(
                orgId: CurrentUser.OrgId,
                elUser: CurrentUser,
                byPassCache: bypassCache,
                cancellationToken: _ctsOperations.Token
            );

            if (preciosResult.Exito)
            {
                precios = preciosResult.DataVarios;
            }
        }
        catch (OperationCanceledException)
        {
            await RepoBitacora.AddLog(
                userId: CurrentUser?.Id ?? "Sistema",
                orgId: CurrentUser?.OrgId ?? "Sistema",
                desc: "Operación de carga de conceptos cancelada por timeout",
                tipoLog: "Warning",
                origen: "FolioConceptosBase.LoadData",
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
            origen: $"FolioConceptosBase.{origen}",
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