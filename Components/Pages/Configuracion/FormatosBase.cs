using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;
using Ali25_V10.Data;
using Ali25_V10.Data.Modelos;
using Ali25_V10.Data.Sistema;

namespace Ali25_V10.Components.Pages.Configuracion;

public class FormatosBase : ComponentBase, IDisposable
{
    [CascadingParameter] protected ApplicationUser CurrentUser { get; set; } = default!;
    [Inject] protected IRepo<W290_Formatos> RepoFormatos { get; set; } = default!;
    [Inject] protected IRepo<W291_FormatoGpo> RepoFormatoGpo { get; set; } = default!;
    [Inject] protected IRepo<W100_Org> RepoOrg { get; set; } = default!;
    [Inject] protected IRepoBitacora RepoBitacora { get; set; } = default!;
    [Inject] protected NavigationManager NavigationManager { get; set; } = default!;

    protected RadzenDataGrid<W290_Formatos> gridFormatos = default!;
    protected int count;
    protected IEnumerable<W290_Formatos>? formatos;
    protected Dictionary<string, List<W100_Org>> formatoOrgs = new();
    protected List<W290_Formatos> formatosToInsert = new();
    protected List<W290_Formatos> formatosToUpdate = new();
    
    protected bool bypassCache { get; set; } = false;
    protected DataGridEditMode editMode = DataGridEditMode.Single;
    protected bool isLoading = false;
    protected bool isRefreshing = false;
    protected bool isEditing = false;

    protected readonly CancellationTokenSource _ctsOperations = new(TimeSpan.FromSeconds(30));
    protected readonly CancellationTokenSource _ctsBitacora = new(TimeSpan.FromSeconds(5));
    protected readonly CancellationTokenSource _ctsLogs = new(TimeSpan.FromSeconds(5));

    protected string? errorMessage;

    public void Dispose()
    {
        _ctsOperations.Dispose();
        _ctsBitacora.Dispose();
        _ctsLogs.Dispose();
    }

    protected override async Task OnInitializedAsync()
    {
        try
        {
            await LoadData();
            await RepoBitacora.AddBitacora(
                userId: CurrentUser.Id,
                desc: "Accediendo a lista de formatos",
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
            
            var result = await RepoFormatos.Get(
                orgId: CurrentUser.OrgId,
                elUser: CurrentUser,
                byPassCache: bypassCache,
                cancellationToken: _ctsOperations.Token
            );

            if (result.Exito)
            {
                formatos = result.DataVarios;
                count = formatos?.Count() ?? 0;
                await LoadOrganizaciones();
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
                desc: "Operación de carga de formatos cancelada por timeout",
                tipoLog: "Warning",
                origen: "FormatosBase.LoadData",
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

    protected async Task LoadOrganizaciones()
    {
        if (formatos == null) return;

        foreach (var formato in formatos)
        {
            var orgs = await GetOrganizacionesFormato(formato.FormatoId);
            formatoOrgs[formato.FormatoId] = orgs.ToList();
        }
    }

    protected async Task<IEnumerable<W100_Org>> GetOrganizacionesFormato(string formatoId)
    {
        var result = await RepoFormatoGpo.Get(
            orgId: CurrentUser.OrgId,
            elUser: CurrentUser,
            filtro: f => f.FormatoId == formatoId && f.Status,
            byPassCache: bypassCache,
            cancellationToken: _ctsOperations.Token
        );

        if (!result.Exito) return new List<W100_Org>();

        var orgIds = result.DataVarios.Select(fg => fg.OrgId);
        var orgsResult = await RepoOrg.Get(
            orgId: CurrentUser.OrgId,
            elUser: CurrentUser,
            filtro: o => orgIds.Contains(o.OrgId),
            byPassCache: bypassCache,
            cancellationToken: _ctsOperations.Token
        );

        return orgsResult.Exito ? orgsResult.DataVarios : new List<W100_Org>();
    }

    protected void ShowOrganizaciones(W290_Formatos formato)
    {
        NavigationManager.NavigateTo($"/Configuracion/FormatosGpo/{formato.FormatoId}");
    }

    protected async Task LogError(Exception ex, string origen)
    {
        errorMessage = ex.Message;
        await RepoBitacora.AddLog(
            userId: CurrentUser.Id,
            desc: $"Error en {origen}: {ex.Message}",
            tipoLog: "Error",
            origen: $"FormatosBase.{origen}",
            orgId: CurrentUser.OrgId,
            cancellationToken: _ctsLogs.Token
        );
    }
} 