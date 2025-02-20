using Ali25_V10.Data;
using Ali25_V10.Data.Modelos;
using Ali25_V10.Data.Sistema;
using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;

namespace Ali25_V10.Components.Pages.Admin;

public class ClientesListBase : ComponentBase
{
    [CascadingParameter] protected ApplicationUser CurrentUser { get; set; } = default!;
    [Inject] protected IRepo<W210_Clientes> RepoClientes { get; set; } = default!;
    [Inject] protected IRepo<W100_Org> RepoOrg { get; set; } = default!;
    [Inject] protected IRepoBitacora RepoBitacora { get; set; } = default!;

    protected RadzenDataGrid<W210_Clientes> gridClientes = default!;
    protected IEnumerable<W210_Clientes>? clientes;
    protected List<W100_Org> orgsDisponibles = new();
    protected bool isLoading;
    protected string? errorMessage;
    protected int count;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            // Cargar organizaciones disponibles
            var orgsResult = await RepoOrg.Get(
                orgId: CurrentUser.OrgId,
                elUser: CurrentUser
            );

            if (orgsResult.Exito)
            {
                orgsDisponibles = orgsResult.DataVarios?.ToList() ?? new();
            }

            await LoadData();
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            await RepoBitacora.AddLog(
                userId: CurrentUser.Id,
                orgId: CurrentUser.OrgId,
                desc: $"Error al inicializar lista de clientes: {ex.Message}",
                tipoLog: "Error",
                origen: "ClientesListBase.OnInitializedAsync"
            );
        }
    }

    protected async Task LoadData()
    {
        try
        {
            isLoading = true;
            var result = await RepoClientes.Get(
                orgId: CurrentUser.OrgId,
                elUser: CurrentUser
            );

            if (result.Exito)
            {
                clientes = result.DataVarios;
                count = clientes?.Count() ?? 0;
            }
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            await RepoBitacora.AddLog(
                userId: CurrentUser.Id,
                orgId: CurrentUser.OrgId,
                desc: $"Error al cargar clientes: {ex.Message}",
                tipoLog: "Error",
                origen: "ClientesListBase.LoadData"
            );
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
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
            errorMessage = ex.Message;
            await RepoBitacora.AddLog(
                userId: CurrentUser.Id,
                orgId: CurrentUser.OrgId,
                desc: $"Error al agregar cliente: {ex.Message}",
                tipoLog: "Error",
                origen: "ClientesListBase.AddCliente"
            );
        }
    }
} 