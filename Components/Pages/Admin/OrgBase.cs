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
    [Parameter] public string TipoOrgParameter { get; set; } = "Cliente"; // Valor por defecto

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

    private readonly CancellationTokenSource _ctsOperations = new(TimeSpan.FromSeconds(30));
    private readonly CancellationTokenSource _ctsBitacora = new(TimeSpan.FromSeconds(5));

    public void Dispose()
    {
        _ctsOperations.Dispose();
        _ctsBitacora.Dispose();
    }

    protected override async Task OnInitializedAsync()
    {
        try
        {
            // TEMPORAL_TEST_INICIO - Diagnóstico de conexión a base de datos
            // Propósito: Verificar si la escritura funciona con valores fijos como en ConfigBase
            await RepoBitacora.AddBitacora(
                userId: "Prueba",
                desc: "TEST - Acceso a organizaciones con valores fijos",
                orgId: "Prueba"
            );
            // TEMPORAL_TEST_FIN

            await RepoBitacora.AddBitacora(
                userId: CurrentUser?.Id ?? "Sistema",
                desc: $"Accediendo a gestión de organizaciones",
                orgId: CurrentUser?.OrgId ?? "Sistema",
                cancellationToken: _ctsBitacora.Token
            );

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
            
            await LoadDataByTipo();
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
        await LoadDataByTipo();
    }

    protected async Task LoadDataByTipo(LoadDataArgs? args = null)
    {
        if (isLoading) return;
        
        try
        {
            isLoading = true;

            await RepoBitacora.AddLog(
                userId: CurrentUser?.Id ?? "Sistema",
                desc: $"Iniciando carga de organizaciones tipo {TipoOrgParameter}",
                tipoLog: "Debug",
                origen: "OrgBase.LoadDataByTipo",
                orgId: CurrentUser?.OrgId ?? "Sistema",
                cancellationToken: _ctsBitacora.Token
            );

            if (CurrentUser?.OrgId != null)
            {
                var result = await RepoOrg.Get(
                    orgId: CurrentUser.OrgId,
                    filtro: x => x.Tipo == TipoOrgParameter,
                    elUser: CurrentUser,
                    byPassCache: bypassCache,
                    cancellationToken: _ctsOperations.Token
                );

                string resultDesc = result?.DataVarios != null 
                    ? $"Se encontraron {result.DataVarios.Count()} organizaciones"
                    : "No se encontraron organizaciones";
                
                await RepoBitacora.AddBitacora(
                    userId: CurrentUser.Id,
                    desc: $"Carga de {TipoOrgParameter}: {resultDesc}",
                    orgId: CurrentUser.OrgId,
                    cancellationToken: _ctsBitacora.Token
                );

                if (result?.DataVarios != null && result.DataVarios.Any())
                {
                    orgs = result.DataVarios.ToList();
                    count = result.DataVarios.Count;
                }
                else
                {
                    orgs = new List<W100_Org>();
                    count = 0;
                }
            }
        }
        catch (OperationCanceledException)
        {
            await RepoBitacora.AddLog(
                userId: CurrentUser?.Id ?? "Sistema",
                orgId: CurrentUser?.OrgId ?? "Sistema",
                desc: "Operación de carga cancelada por timeout",
                tipoLog: "Warning",
                origen: "OrgBase.LoadDataByTipo",
                cancellationToken: _ctsBitacora.Token
            );
        }
        catch (Exception ex)
        {
            orgs = new List<W100_Org>();
            count = 0;
            
            await RepoBitacora.AddLog(
                userId: CurrentUser?.Id ?? "Sistema",
                orgId: CurrentUser?.OrgId ?? "Sistema",
                desc: $"Error en carga de {TipoOrgParameter}:\n" +
                      $"Mensaje: {ex.Message}\n" +
                      $"Stack: {ex.StackTrace}\n" +
                      $"Source: {ex.Source}\n" +
                      $"BypassCache: {bypassCache}",
                tipoLog: "Error",
                origen: $"OrgBase.LoadDataByTipo",
                cancellationToken: _ctsBitacora.Token
            );
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
            await LoadDataByTipo();
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