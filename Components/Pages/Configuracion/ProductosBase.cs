using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;
using Ali25_V10.Data;
using Ali25_V10.Data.Modelos;
using Ali25_V10.Data.Sistema;

namespace Ali25_V10.Components.Pages.Configuracion;

public class ProductosBase : ComponentBase, IDisposable
{
    [CascadingParameter] protected ApplicationUser CurrentUser { get; set; } = default!;
    [Inject] protected IRepo<W281_Productos> RepoProductos { get; set; } = default!;
    [Inject] protected IRepoBitacora RepoBitacora { get; set; } = default!;
    [Inject] protected NavigationManager NavigationManager { get; set; } = default!;

    protected RadzenDataGrid<W281_Productos> gridProductos = default!;
    protected IEnumerable<W281_Productos>? productos;
    protected List<W281_Productos> productosToInsert = new();
    protected List<W281_Productos> productosToUpdate = new();
    
    // Standard properties
    protected bool isLoading;
    protected bool isRefreshing;
    protected bool isEditing;
    protected bool bypassCache;
    protected int count;
    protected string? errorMessage;
    protected DataGridEditMode editMode = DataGridEditMode.Single;
    protected bool ListaEdit => CurrentUser.Nivel >= 5;

    private readonly CancellationTokenSource _ctsOperations = new(TimeSpan.FromSeconds(30));
    private readonly CancellationTokenSource _ctsBitacora = new(TimeSpan.FromSeconds(5));

    protected override async Task OnInitializedAsync()
    {
        try
        {
            await LoadData();
            await RepoBitacora.AddBitacora(
                userId: CurrentUser.Id,
                desc: "Accediendo a lista de productos",
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
            
            var result = await RepoProductos.Get(
                orgId: CurrentUser.OrgId,
                elUser: CurrentUser,
                byPassCache: bypassCache,
                cancellationToken: _ctsOperations.Token
            );

            if (result.Exito)
            {
                productos = result.DataVarios;
                count = productos?.Count() ?? 0;
            }
            else
            {
                errorMessage = result.Texto;
            }
        }
        catch (OperationCanceledException)
        {
            await RepoBitacora.AddLog(
                userId: CurrentUser?.Id ?? "Sistema",
                orgId: CurrentUser?.OrgId ?? "Sistema",
                desc: "Operación de carga de productos cancelada por timeout",
                tipoLog: "Warning",
                origen: "ProductosBase.LoadData",
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
    }

    protected void Reset(W281_Productos producto)
    {
        productosToInsert.Remove(producto);
        productosToUpdate.Remove(producto);
    }

    protected void Reset()
    {
        productosToInsert.Clear();
        productosToUpdate.Clear();
    }

    protected async Task EditRow(W281_Productos producto)
    {
        isEditing = true;
        if (gridProductos?.EditMode == DataGridEditMode.Single)
        {
            Reset();
        }
        productosToUpdate.Add(producto);
        await gridProductos.EditRow(producto);
    }

    protected async Task SaveRow(W281_Productos producto)
    {
        try
        {
            await gridProductos.UpdateRow(producto);
            var result = await RepoProductos.Update(
                producto,
                CurrentUser.OrgId,
                CurrentUser,
                cancellationToken: _ctsOperations.Token
            );

            if (!result.Exito)
            {
                throw new Exception(result.Texto);
            }

            await RepoBitacora.AddBitacora(
                userId: CurrentUser.Id,
                desc: $"Se actualizó el producto {producto.ProductoId}",
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

    protected void CancelEdit(W281_Productos producto)
    {
        isEditing = false;
        Reset(producto);
        gridProductos.CancelEditRow(producto);
    }

    protected async Task LogError(Exception ex, string origen)
    {
        errorMessage = ex.Message;
        await RepoBitacora.AddLog(
            userId: CurrentUser.Id,
            orgId: CurrentUser.OrgId,
            desc: ex.Message,
            tipoLog: "Error",
            origen: $"ProductosBase.{origen}",
            cancellationToken: _ctsBitacora.Token
        );
    }

    public void Dispose()
    {
        _ctsOperations.Dispose();
        _ctsBitacora.Dispose();
    }
} 