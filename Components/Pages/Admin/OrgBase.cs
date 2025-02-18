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
    [Inject] private IRepoBitacora? _repoBitacora { get; set; }
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
        if (_repoBitacora == null)
        {
            throw new InvalidOperationException("RepoBitacora no fue inyectado correctamente");
        }

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
            
            await LoadDataByTipo();
        }
        catch (Exception ex)
        {
            await _repoBitacora.AddLog(
                userId: CurrentUser?.Id ?? "Sistema",
                orgId: CurrentUser?.OrgId ?? "Sistema",
                desc: $"Error al inicializar: {ex.Message}",
                tipoLog: "Error",
                origen: $"OrgBase.OnInitializedAsync",
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
        // TEMPORAL_TEST_INICIO - Diagnóstico de IRepoBitacora
        try 
        {
            Console.WriteLine($"Estado de _repoBitacora: {(_repoBitacora == null ? "NULL" : "Instanciado")}");
            if (_repoBitacora == null) 
            {
                Console.WriteLine("WARNING: _repoBitacora es null - No se pueden escribir logs");
                return;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al verificar _repoBitacora: {ex.Message}");
        }
        // TEMPORAL_TEST_FIN
        
        try
        {
            isLoading = true;

            if (CurrentUser?.OrgId != null)
            {
                string bitTxt = "";

                // TEMPORAL_TEST_INICIO - Prueba de comportamiento del caché
                ApiRespAll<W100_Org>? result = null;  
                            
                for(int i = 1; i <= 5; i++)
                {
                    await _repoBitacora.AddLog(
                        userId: CurrentUser?.Id ?? "Sistema",
                        desc: $"PRUEBA #{i} - Iniciando llamada a Get",
                        tipoLog: "Debug",
                        origen: "OrgBase.LoadDataByTipo.Test",
                        orgId: CurrentUser?.OrgId ?? "Sistema",
                        cancellationToken: _ctsBitacora.Token
                    );

                    result = await RepoOrg.Get(
                        orgId: CurrentUser?.OrgId ?? "Sistema",
                        filtro: x => x.Tipo == TipoOrgParameter,
                        elUser: CurrentUser?? new ApplicationUser(),
                        byPassCache: bypassCache,
                        cancellationToken: _ctsOperations.Token
                    );

                    // Esperar 1 segundo entre llamadas
                    await Task.Delay(1000);
                }
                // TEMPORAL_TEST_FIN

                if (result?.DataVarios != null && result.DataVarios.Any())
                {
                    orgs = result.DataVarios.ToList();
                    count = result.DataVarios.Count;
                    bitTxt = $"Se encontraron {count} organizaciones tipo {TipoOrgParameter}";
                }
                else
                {
                    orgs = new List<W100_Org>();
                    count = 0;
                    bitTxt = $"No se encontraron organizaciones tipo {TipoOrgParameter}";
                }

                await _repoBitacora.AddBitacora(
                    userId: CurrentUser?.Id ?? "Sistema",
                    desc: bitTxt,
                    orgId: CurrentUser?.OrgId ?? "Sistema",
                    cancellationToken: _ctsBitacora.Token
                );
            }
        }
        catch (OperationCanceledException)
        {
            await _repoBitacora.AddLog(
                userId: CurrentUser?.Id ?? "Sistema",
                orgId: CurrentUser?.OrgId ?? "Sistema",
                desc: "Operación de carga de organizaciones cancelada por timeout",
                tipoLog: "Warning",
                origen: "OrgBase.LoadDataByTipo",
                cancellationToken: _ctsBitacora.Token
            );
        }
        catch (Exception ex)
        {
            orgs = new List<W100_Org>();
            count = 0;
            
            await _repoBitacora.AddLog(
                userId: CurrentUser?.Id ?? "Sistema",
                orgId: CurrentUser?.OrgId ?? "Sistema",
                desc: $"Error al cargar organizaciones tipo {TipoOrgParameter}: {ex.Message}\nStackTrace: {ex.StackTrace}",
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
        if (_repoBitacora == null) return;
        
        try 
        {   
            isRefreshing = true;
            await LoadDataByTipo();
        }
        catch (Exception ex)
        {
            await _repoBitacora.AddLog(
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