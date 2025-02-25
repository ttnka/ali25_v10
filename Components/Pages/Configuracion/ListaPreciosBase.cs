using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;
using Ali25_V10.Data;
using Ali25_V10.Data.Modelos;
using Ali25_V10.Data.Sistema;

namespace Ali25_V10.Components.Pages.Configuracion;

public class ListaPreciosBase : ComponentBase, IDisposable
{
    [CascadingParameter] protected ApplicationUser CurrentUser { get; set; } = default!;
    [Inject] protected IRepo<W280_ListaPrecios> RepoListaPrecios { get; set; } = default!;
    [Inject] protected IRepoBitacora RepoBitacora { get; set; } = default!;
    [Inject] protected NavigationManager NavigationManager { get; set; } = default!;

    // Grid y datos
    protected RadzenDataGrid<W280_ListaPrecios> gridListaPrecios = default!;
    protected IEnumerable<W280_ListaPrecios>? listaPrecios;
    protected List<W280_ListaPrecios> listaPreciosToInsert = new();
    protected List<W280_ListaPrecios> listaPreciosToUpdate = new();
    protected int count;
    protected DataGridEditMode editMode = DataGridEditMode.Single;

    // Estado del componente
    protected bool isLoading;
    protected bool isRefreshing;
    protected bool isEditing;
    protected bool bypassCache;
    protected string? errorMessage;

    // Tokens de cancelación
    protected readonly CancellationTokenSource _ctsOperations = new(TimeSpan.FromSeconds(30));
    protected readonly CancellationTokenSource _ctsBitacora = new(TimeSpan.FromSeconds(5));

    protected override async Task OnInitializedAsync()
    {
        if (CurrentUser.Nivel < 5) return;
        
        try
        {
            await LoadData();
            await RepoBitacora.AddBitacora(
                userId: CurrentUser.Id,
                desc: "Accediendo a lista de precios",
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
            
            var result = await RepoListaPrecios.Get(
                orgId: CurrentUser.OrgId,
                elUser: CurrentUser,
                filtro: f => f.Status,
                byPassCache: bypassCache,
                cancellationToken: _ctsOperations.Token
            );

            if (result.Exito)
            {
                listaPrecios = result.DataVarios;
                count = listaPrecios?.Count() ?? 0;
            }
            else
            {
                errorMessage = result.Texto;
            }
        }
        catch (OperationCanceledException)
        {
            await RepoBitacora.AddLog(
                userId: CurrentUser.Id,
                desc: "Operación de carga de lista de precios cancelada por timeout",
                tipoLog: "Warning",
                origen: "ListaPreciosBase.LoadData",
                orgId: CurrentUser.OrgId,
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

    protected async Task LogError(Exception ex, string origen)
    {
        errorMessage = ex.Message;
        await RepoBitacora.AddLog(
            userId: CurrentUser.Id,
            desc: $"Error en {origen}: {ex.Message}",
            tipoLog: "Error",
            origen: $"ListaPreciosBase.{origen}",
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