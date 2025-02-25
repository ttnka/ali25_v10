using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;
using Ali25_V10.Data;
using Ali25_V10.Data.Modelos;
using Ali25_V10.Data.Sistema;

namespace Ali25_V10.Components.Pages.Clientes;

public class FolioDetalleBase : ComponentBase, IDisposable
{
    [CascadingParameter] protected ApplicationUser CurrentUser { get; set; } = default!;
    [Parameter] public string FolioId { get; set; } = string.Empty;
    [Inject] protected IRepo<W220_Folios> RepoFolios { get; set; } = default!;
    [Inject] protected IRepo<W222_FolioDet> RepoFolioDet { get; set; } = default!;
    [Inject] protected IRepoBitacora RepoBitacora { get; set; } = default!;
    [Inject] protected NavigationManager NavigationManager { get; set; } = default!;

    protected RadzenDataGrid<W222_FolioDet> gridFolioDet = default!;
    protected W220_Folios? folio;
    protected IEnumerable<W222_FolioDet>? detalles;
    protected List<W222_FolioDet> detallesToInsert = new();
    protected List<W222_FolioDet> detallesToUpdate = new();
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
                desc: $"Accediendo al detalle del folio {FolioId}",
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
                folio = folioResult.DataUno;
            }

            var detallesResult = await RepoFolioDet.Get(
                orgId: CurrentUser.OrgId,
                elUser: CurrentUser,
                filtro: d => d.FolioId == FolioId,
                byPassCache: bypassCache,
                cancellationToken: _ctsOperations.Token
            );

            if (detallesResult.Exito)
            {
                detalles = detallesResult.DataVarios;
                count = detalles?.Count() ?? 0;
            }
        }
        catch (OperationCanceledException)
        {
            await RepoBitacora.AddLog(
                userId: CurrentUser?.Id ?? "Sistema",
                orgId: CurrentUser?.OrgId ?? "Sistema",
                desc: "Operación de carga de detalles cancelada por timeout",
                tipoLog: "Warning",
                origen: "FolioDetalleBase.LoadData",
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
            origen: $"FolioDetalleBase.{origen}",
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