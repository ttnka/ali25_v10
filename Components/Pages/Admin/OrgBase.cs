using Ali25_V10.Data;
using Ali25_V10.Data.Modelos;
using Ali25_V10.Data.Sistema;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Radzen;
using Radzen.Blazor;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System;

namespace Ali25_V10.Components.Pages.Admin;

public class OrgBase : ComponentBase, IDisposable
{
    [CascadingParameter] protected ApplicationUser CurrentUser { get; set; } = default!;
    [Inject] protected IRepo<W100_Org> RepoOrg { get; set; } = default!;
    [Inject] protected IRepoBitacora RepoBitacora { get; set; } = default!;
    [Parameter] public string TipoOrgParameter { get; set; } = "Admin"; // Valor por defecto

    
    protected RadzenDataGrid<W100_Org>? grid;
    protected bool isLoading = false;
    protected bool isRefreshing = false;
    protected int count;
    protected IEnumerable<W100_Org>? orgs;
    protected bool bypassCache { get; set; } = false;
    protected bool showAddDialog { get; set; } = false;
    protected List<W100_Org> orgsToUpdate = new();
    protected DataGridEditMode editMode = DataGridEditMode.Single;
    protected List<string> tiposOrg = new();
    protected HashSet<string> existingRfcs = new();

    protected readonly CancellationTokenSource _ctsOperations = new(TimeSpan.FromSeconds(3000));
    private readonly CancellationTokenSource _ctsBitacora = new(TimeSpan.FromSeconds(500));

    public void Dispose()
    {
        _ctsOperations.Dispose();
        _ctsBitacora.Dispose();
    }

    protected override async Task OnInitializedAsync()
    {
        try
        {


            if (CurrentUser == null) return;
            
            // Configurar tipos disponibles según nivel del usuario
            tiposOrg = CurrentUser.Nivel == 7 
                ? new List<string> { "Admin", "Cliente", "Proveedor" }
                : new List<string> { "Cliente", "Proveedor" };

            // Cargar RFCs según el tipo
            var allOrgs = await RepoOrg.GetAll(CurrentUser, byPassCache: bypassCache);

            if (allOrgs.Exito && allOrgs.DataVarios != null)
            {
                existingRfcs = new HashSet<string>(
                    allOrgs.DataVarios.Select(o => o.Rfc.ToUpper()),
                    StringComparer.OrdinalIgnoreCase
                );
            }
            
            await LoadData();
            await RepoBitacora.AddBitacora(
                userId: CurrentUser?.Id ?? "Sistema",
                desc: $"Accediendo a gestión de organizaciones",
                orgId: CurrentUser?.OrgId ?? "Sistema",
                cancellationToken: _ctsBitacora.Token
            );
        }
        catch (Exception ex)
        {
            await RepoBitacora.AddLog(
                userId: CurrentUser?.Id ?? "Sistema",
                orgId: CurrentUser?.OrgId ?? "Sistema",
                desc: $"Error en inicialización: {ex.Message}",
                tipoLog: "Error",
                origen: "OrgBase.OnInitializedAsync",
                cancellationToken: _ctsBitacora.Token
            );
        }
    }

    protected async Task HandleOrgCreated()
    {
        showAddDialog = false;
        await LoadData();
    }

    protected async Task LoadData()
    {
        try
        {
            isLoading = true;
            var result = await RepoOrg.Get(
                orgId: CurrentUser.OrgId,
                elUser: CurrentUser,
                filtro: x => x.Tipo == TipoOrgParameter
            );
            
            if (result.Exito)
            {
                orgs = result.DataVarios;
                count = orgs?.Count() ?? 0;
            }
        }
        catch (Exception ex)
        {
            // TEMPORAL_TEST_INICIO - Logs diagnóstico caché
            await RepoBitacora.AddLog(
                userId: CurrentUser?.Id ?? "Sistema",
                orgId: CurrentUser?.OrgId ?? "Sistema",
                desc: $"Error al cargar datos: {ex.Message}",
                tipoLog: "Error",
                origen: "OrgBase.LoadData",
                cancellationToken: _ctsBitacora.Token
            );
            // TEMPORAL_TEST_FIN
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
            await RepoBitacora.AddLog(
                userId: CurrentUser?.Id ?? "Sistema",
                orgId: CurrentUser?.OrgId ?? "Sistema",
                desc: $"Error al refrescar organizaciones: {ex.Message}",
                tipoLog: "Error",
                origen: $"OrgBase.RefreshData",
                cancellationToken: _ctsBitacora.Token
            );
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
} 