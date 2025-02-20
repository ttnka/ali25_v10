using Ali25_V10.Data;
using Ali25_V10.Data.Modelos;
using Ali25_V10.Data.Sistema;
using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;

namespace Ali25_V10.Components.Pages.Admin;

public class BitacoraListBase : ComponentBase
{
    [CascadingParameter] protected ApplicationUser CurrentUser { get; set; } = default!;
    [Inject] protected IRepoBitacora RepoBitacora { get; set; } = default!;
    [Inject] protected IRepo<W100_Org> RepoOrg { get; set; } = default!;
    [Inject] protected IRepo<ApplicationUser> RepoUser { get; set; } = default!;
    [Inject] protected IRepo<W210_Clientes> RepoClientes { get; set; } = default!;

    protected RadzenDataGrid<Z900_Bitacora> gridBitacora = default!;
    protected IEnumerable<Z900_Bitacora>? bitacoras;
    protected List<W100_Org> orgsDisponibles = new();
    protected List<ApplicationUser> usuariosDisponibles = new();
    protected bool isLoading;
    protected string? errorMessage;
    protected int count;
    protected int pageSize = 10;
    protected int pageNumber = 0;

    // Filtros
    protected bool showFilters = true;
    protected string? selectedOrgId;
    protected string? selectedUserId;
    protected string? descripcionFilter;
    protected DateTime? fechaFilter;
    protected DateTime? fechaInicio;
    protected DateTime? fechaFin;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            // Cargar organizaciones según nivel
            var orgsResult = await RepoOrg.Get(
                orgId: CurrentUser.OrgId,
                elUser: CurrentUser
            );

            if (orgsResult.Exito)
            {
                // Si es nivel 5 o 6, incluir orgs de clientes
                if (CurrentUser.Nivel >= 5)
                {
                    var clientes = await RepoClientes.Get(
                        orgId: CurrentUser.OrgId,
                        elUser: CurrentUser
                    );

                    if (clientes.Exito && clientes.DataVarios != null)
                    {
                        var clienteOrgIds = clientes.DataVarios.Select(c => c.ClienteOrgId);
                        orgsDisponibles = orgsResult.DataVarios?
                            .Where(o => o.OrgId == CurrentUser.OrgId || clienteOrgIds.Contains(o.OrgId))
                            .ToList() ?? new();
                    }
                }
                else
                {
                    orgsDisponibles = orgsResult.DataVarios?
                        .Where(o => o.OrgId == CurrentUser.OrgId)
                        .ToList() ?? new();
                }
            }

            // Cargar usuarios
            var usersResult = await RepoUser.Get(
                orgId: CurrentUser.OrgId,
                elUser: CurrentUser
            );

            if (usersResult.Exito)
            {
                usuariosDisponibles = usersResult.DataVarios?.ToList() ?? new();
            }

            selectedOrgId = CurrentUser.OrgId;
            await LoadData();
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            await RepoBitacora.AddLog(
                userId: CurrentUser.Id,
                orgId: CurrentUser.OrgId,
                desc: $"Error al inicializar bitácora: {ex.Message}",
                tipoLog: "Error",
                origen: "BitacoraListBase.OnInitializedAsync"
            );
        }
    }

    protected async Task LoadData()
    {
        try
        {
            isLoading = true;
            
            var result = await RepoBitacora.GetBitacoraFiltrada(
                orgId: selectedOrgId ?? CurrentUser.OrgId,
                elUser: CurrentUser,
                filtro: new FiltroBitacora
                {
                    UsuarioId = selectedUserId,
                    Desc = descripcionFilter,
                    FechaInicio = fechaInicio ?? fechaFilter,
                    FechaFin = fechaFin,
                    Rango = fechaInicio.HasValue && fechaFin.HasValue,
                    Ascen = false // Para ordenar descendente por fecha
                }
            );

            if (result.Exito && result.DataVarios != null)
            {
                bitacoras = result.DataVarios
                    .Skip(pageNumber)
                    .Take(pageSize);
                count = result.DataVarios.Count();
            }
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            await RepoBitacora.AddLog(
                userId: CurrentUser.Id,
                orgId: CurrentUser.OrgId,
                desc: $"Error al cargar bitácora: {ex.Message}",
                tipoLog: "Error",
                origen: "BitacoraListBase.LoadData"
            );
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    protected string GetUserDisplay(string userId)
    {
        var user = usuariosDisponibles.FirstOrDefault(u => u.Id == userId);
        if (user != null)
        {
            var inicial = !string.IsNullOrEmpty(user.Paterno) 
                ? user.Paterno[0].ToString().ToUpper() 
                : "";
            return $"{user.Nombre} {inicial}.";
        }
        return userId;
    }

    protected string GetOrgDisplay(string orgId)
    {
        return orgsDisponibles.FirstOrDefault(o => o.OrgId == orgId)?.Comercial ?? orgId;
    }

    protected async Task LoadPage(LoadDataArgs args)
    {
        pageNumber = args.Skip ?? 0;
        pageSize = args.Top ?? 10;
        await LoadData();
    }

    protected async Task OnOrgSelected(string? value)
    {
        selectedOrgId = value;
        await LoadData();
    }

    protected async Task OnUserSelected(string? value)
    {
        selectedUserId = value;
        await LoadData();
    }

    protected async Task OnDescripcionChanged(string? value)
    {
        descripcionFilter = value;
        await LoadData();
    }

    protected async Task OnFechaChanged(DateTime? value)
    {
        fechaFilter = value;
        fechaInicio = null;
        fechaFin = null;
        await LoadData();
    }

    protected async Task OnRangoFechasChanged(DateRange range)
    {
        fechaInicio = range.Start;
        fechaFin = range.End;
        fechaFilter = null;
        await LoadData();
    }

    protected class DateRange
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
    }
} 