using Ali25_V10.Data;
using Ali25_V10.Data.Modelos;
using Ali25_V10.Data.Sistema;
using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;
using System.Threading;

namespace Ali25_V10.Components.Pages.Admin;

public class ClientesListBase : ComponentBase, IDisposable
{
    [CascadingParameter] protected ApplicationUser CurrentUser { get; set; } = default!;
    [Inject] protected IRepo<W210_Clientes> RepoClientes { get; set; } = default!;
    [Inject] protected IRepo<W100_Org> RepoOrg { get; set; } = default!;
    [Inject] protected IRepoBitacora RepoBitacora { get; set; } = default!;

    protected RadzenDataGrid<W210_Clientes> gridClientes = default!;
    protected IEnumerable<W210_Clientes>? clientes;
    protected List<W100_Org> orgsDisponibles = new();
    protected bool isLoading;
    protected bool isRefreshing;
    protected bool isEditing;
    protected bool bypassCache;
    protected string? errorMessage;
    protected int count;

    protected List<W210_Clientes> clientesToInsert = new();
    protected List<W210_Clientes> clientesToUpdate = new();
    protected DataGridEditMode editMode = DataGridEditMode.Single;

    protected readonly CancellationTokenSource _ctsOperations = new(TimeSpan.FromSeconds(30));
    protected readonly CancellationTokenSource _ctsBitacora = new(TimeSpan.FromSeconds(5));
    protected readonly CancellationTokenSource _ctsLogs = new(TimeSpan.FromSeconds(5));

    protected override async Task OnInitializedAsync()
    {
        try
        {
            // Cargar organizaciones disponibles
            var orgsResult = await RepoOrg.Get(
                orgId: CurrentUser.OrgId,
                elUser: CurrentUser,
                byPassCache: bypassCache,
                cancellationToken: _ctsOperations.Token
            );

            if (orgsResult.Exito)
            {
                orgsDisponibles = orgsResult.DataVarios?.ToList() ?? new();
            }

            await LoadData();
            await RepoBitacora.AddBitacora(
                userId: CurrentUser.Id,
                desc: $"Accediendo a lista de clientes",
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
            
            var result = await RepoClientes.Get(
                orgId: CurrentUser.OrgId,
                elUser: CurrentUser,
                byPassCache: bypassCache,
                cancellationToken: _ctsOperations.Token
            );

            if (result.Exito)
            {
                clientes = result.DataVarios;
                count = clientes?.Count() ?? 0;
            }
        }
        catch (OperationCanceledException)
        {
            await RepoBitacora.AddLog(
                userId: CurrentUser?.Id ?? "Sistema",
                orgId: CurrentUser?.OrgId ?? "Sistema",
                desc: "Operación de carga de clientes cancelada por timeout",
                tipoLog: "Warning",
                origen: "ClientesListBase.LoadData",
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
        LoadData();
    }

    protected async Task AddCliente(W100_Org org)
    {
        try
        {
            var cliente = new W210_Clientes(CurrentUser.OrgId, org.OrgId);
            var result = await RepoClientes.Insert(cliente, CurrentUser.OrgId, CurrentUser);

            if (result.Exito)
            {
                await LoadData();
            }
            else
            {
                errorMessage = result.Texto;
            }
        }
        catch (Exception ex)
        {
            await LogError(ex, "AddCliente");
        }
    }

    protected async Task LogError(Exception ex, string origen)
    {
        errorMessage = ex.Message;
        await RepoBitacora.AddLog(
            userId: CurrentUser.Id,
            desc: $"Error en {origen}: {ex.Message}",
            tipoLog: "Error",
            origen: $"ClientesListBase.{origen}",
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