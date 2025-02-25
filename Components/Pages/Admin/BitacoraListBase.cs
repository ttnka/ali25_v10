using Ali25_V10.Data;
using Ali25_V10.Data.Modelos;
using Ali25_V10.Data.Sistema;
using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ali25_V10.Components.Pages.Admin;

public class BitacoraListBase : ComponentBase, IDisposable
{
    // Standard properties
    [CascadingParameter] protected ApplicationUser CurrentUser { get; set; } = default!;
    protected bool isLoading;
    protected bool isRefreshing;
    protected bool bypassCache;
    protected string? errorMessage;
    protected CancellationTokenSource cts = new(TimeSpan.FromSeconds(30));
    protected CancellationTokenSource quickCts = new(TimeSpan.FromSeconds(5));

    // Injected services
    [Inject] protected IRepoBitacora RepoBitacora { get; set; } = default!;
    [Inject] protected IRepo<W100_Org> RepoOrg { get; set; } = default!;
    [Inject] protected IRepo<ApplicationUser> RepoUser { get; set; } = default!;
    [Inject] protected IRepo<W210_Clientes> RepoClientes { get; set; } = default!;

    // Grid properties
    protected RadzenDataGrid<Z900_Bitacora> gridBitacora = default!;
    protected IEnumerable<Z900_Bitacora>? bitacoras;
    protected int count;
    protected int pageSize = 10;
    protected int pageNumber = 0;

    // Component specific properties
    protected List<W100_Org> orgsDisponibles = new();
    protected List<ApplicationUser> usuariosDisponibles = new();
    protected List<Z900_Bitacora> bitacorasToInsert = new();
    
    protected bool showFilters = true;
    protected string? selectedOrgId;
    protected string? selectedUserId;
    protected string? descripcionFilter;
    protected DateTime? fechaFilter;
    protected DateTime? fechaInicio;
    protected DateTime? fechaFin;

    public void Dispose()
    {
        try { cts?.Cancel(); } catch { }
        try { cts?.Dispose(); } catch { }
        try { quickCts?.Cancel(); } catch { }
        try { quickCts?.Dispose(); } catch { }
        GC.SuppressFinalize(this);
    }

    protected override async Task OnInitializedAsync()
    {
        await LoadData();
        await base.OnInitializedAsync();
    }

    protected async Task LoadData()
    {
        try
        {
            isLoading = true;
            errorMessage = null;

            // Cargar organizaciones según nivel
            var orgsResult = await RepoOrg.Get(
                orgId: CurrentUser.OrgId,
                elUser: CurrentUser,
                cancellationToken: cts.Token
            );

            if (orgsResult.Exito)
            {
                // Si es nivel 5 o 6, incluir orgs de clientes
                if (CurrentUser.Nivel >= 5)
                {
                    var clientes = await RepoClientes.Get(
                        orgId: CurrentUser.OrgId,
                        elUser: CurrentUser,
                        cancellationToken: quickCts.Token
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
                elUser: CurrentUser,
                cancellationToken: quickCts.Token
            );

            if (usersResult.Exito)
            {
                usuariosDisponibles = usersResult.DataVarios?.ToList() ?? new();
            }

            selectedOrgId = CurrentUser.OrgId;
            await RefreshData();
        }
        catch (Exception ex)
        {
            await LogError(ex, "Error al cargar datos iniciales");
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    protected async Task RefreshData()
    {
        try
        {
            isRefreshing = true;
            errorMessage = null;
            
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
                    Ascen = false
                },
                byPassCache: bypassCache,
                cancellationToken: cts.Token
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
            await LogError(ex, "Error al refrescar datos");
        }
        finally
        {
            isRefreshing = false;
            StateHasChanged();
        }
    }

    protected async Task LoadPage(LoadDataArgs args)
    {
        try
        {
            pageSize = args.Top ?? 10;
            pageNumber = args.Skip ?? 0;
            await RefreshData();
        }
        catch (Exception ex)
        {
            await LogError(ex, "Error al cargar página");
        }
    }

    protected async Task OnOrgSelected(string? orgId)
    {
        selectedOrgId = orgId;
        await RefreshData();
    }

    protected async Task OnUserSelected(string? userId)
    {
        selectedUserId = userId;
        await RefreshData();
    }

    protected async Task OnDescripcionChanged(string? desc)
    {
        descripcionFilter = desc;
        await RefreshData();
    }

    protected async Task OnFechaChanged(DateTime? fecha)
    {
        fechaFilter = fecha;
        await RefreshData();
    }
    

    protected void Reset(Z900_Bitacora bitacora)
    {
        bitacorasToInsert.Remove(bitacora);
        
    }

    protected void Reset()
    {
        bitacorasToInsert.Clear();
        
    }


    protected async Task ToggleBypassCache()
    {
        bypassCache = !bypassCache;
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

    protected async Task LogError(Exception ex, string message)
    {
        errorMessage = $"{message}: {ex.Message}";
        await RepoBitacora.AddLog(
            userId: CurrentUser?.Id ?? "Sistema",
            orgId: CurrentUser?.OrgId ?? "Sistema",
            desc: $"{message}: {ex.Message}\nStackTrace: {ex.StackTrace}",
            tipoLog: "Error",
            origen: "BitacoraListBase",
            cancellationToken: quickCts.Token
        );
    }
} 