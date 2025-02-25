using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;
using Ali25_V10.Data;
using Ali25_V10.Data.Modelos;
using Ali25_V10.Data.Sistema;

namespace Ali25_V10.Components.Pages.Configuracion;

public class PreciosBase : ComponentBase, IDisposable
{
    [CascadingParameter] protected ApplicationUser CurrentUser { get; set; } = default!;
    [Inject] protected IRepo<W282_Precios> RepoPrecios { get; set; } = default!;
    [Inject] protected IRepoBitacora RepoBitacora { get; set; } = default!;
    [Inject] protected IRepo<W280_ListaPrecios> RepoListaPrecios { get; set; } = default!;
    [Inject] protected IRepo<W281_Productos> RepoProductos { get; set; } = default!;

    protected RadzenDataGrid<W282_Precios> gridPrecios = default!;
    protected IEnumerable<W282_Precios>? precios;
    protected IEnumerable<W280_ListaPrecios>? listaPrecios;
    protected IEnumerable<W281_Productos>? productos;
    protected List<W282_Precios> preciosToInsert = new();
    protected List<W282_Precios> preciosToUpdate = new();
    
    // Standard properties
    protected bool isLoading;
    protected bool isRefreshing;
    protected bool bypassCache;
    protected bool isEditing;
    protected int count;
    protected string? errorMessage;
    protected DataGridEditMode editMode = DataGridEditMode.Single;
    protected readonly CancellationTokenSource _ctsOperations = new(TimeSpan.FromSeconds(30));
    protected readonly CancellationTokenSource _ctsBitacora = new(TimeSpan.FromSeconds(5));

    // Component specific properties
    protected bool ListaEdit => CurrentUser.Nivel >= 5;

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
            errorMessage = null;
            
            var taskPrecios = RepoPrecios.Get(
                orgId: CurrentUser.OrgId, 
                elUser: CurrentUser,
                byPassCache: bypassCache,
                cancellationToken: _ctsOperations.Token
            );
            var taskListas = RepoListaPrecios.Get(
                orgId: CurrentUser.OrgId, 
                elUser: CurrentUser,
                byPassCache: bypassCache,
                cancellationToken: _ctsOperations.Token
            );
            var taskProductos = RepoProductos.Get(
                orgId: CurrentUser.OrgId, 
                elUser: CurrentUser,
                byPassCache: bypassCache,
                cancellationToken: _ctsOperations.Token
            );

            await Task.WhenAll(taskPrecios, taskListas, taskProductos);

            if (!taskPrecios.Result.Exito) throw new Exception(taskPrecios.Result.Texto);
            if (!taskListas.Result.Exito) throw new Exception(taskListas.Result.Texto);
            if (!taskProductos.Result.Exito) throw new Exception(taskProductos.Result.Texto);

            precios = taskPrecios.Result.DataVarios;
            listaPrecios = taskListas.Result.DataVarios;
            productos = taskProductos.Result.DataVarios;
            count = precios?.Count() ?? 0;
        }
        catch (OperationCanceledException)
        {
            await RepoBitacora.AddLog(
                userId: CurrentUser?.Id ?? "Sistema",
                orgId: CurrentUser?.OrgId ?? "Sistema",
                desc: "Operación de carga de precios cancelada por timeout",
                tipoLog: "Warning",
                origen: "PreciosBase.LoadData",
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

    protected void Reset(W282_Precios precio)
    {
        preciosToInsert.Remove(precio);
        preciosToUpdate.Remove(precio);
    }

    protected void Reset()
    {
        preciosToInsert.Clear();
        preciosToUpdate.Clear();
    }

    protected async Task EditRow(W282_Precios precio)
    {
        isEditing = true;
        if (gridPrecios?.EditMode == DataGridEditMode.Single)
        {
            Reset();
        }
        preciosToUpdate.Add(precio);
        await gridPrecios.EditRow(precio);
    }

    protected async Task SaveRow(W282_Precios precio)
    {
        try
        {
            await gridPrecios.UpdateRow(precio);
            var result = await RepoPrecios.Update(
                precio, 
                CurrentUser.OrgId,
                elUser: CurrentUser,
                cancellationToken: _ctsOperations.Token
            );

            if (!result.Exito)
            {
                throw new Exception(result.Texto);
            }

            await RepoBitacora.AddBitacora(
                userId: CurrentUser.Id,
                desc: $"Se actualizó el precio {precio.PrecioId}",
                orgId: CurrentUser.OrgId,
                cancellationToken: _ctsBitacora.Token
            );

            await LoadData();
            isEditing = false;
        }
        catch (Exception ex)
        {
            await LogError(ex, "SaveRow");
            throw;
        }
    }

    protected void CancelEdit(W282_Precios precio)
    {
        isEditing = false;
        Reset(precio);
        gridPrecios.CancelEditRow(precio);
    }

    protected async Task LogError(Exception ex, string origen)
    {
        errorMessage = ex.Message;
        await RepoBitacora.AddLog(
            userId: CurrentUser.Id,
            orgId: CurrentUser.OrgId,
            desc: ex.Message,
            tipoLog: "Error",
            origen: $"PreciosBase.{origen}",
            cancellationToken: _ctsBitacora.Token
        );
    }

    public void Dispose()
    {
        _ctsOperations.Dispose();
        _ctsBitacora.Dispose();
    }
} 