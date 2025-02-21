using Ali25_V10.Data;
using Ali25_V10.Data.Modelos;
using Ali25_V10.Data.Sistema;
using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;

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
    protected IEnumerable<W290_Formatos>? formatos;
    protected Dictionary<string, List<W100_Org>> formatoOrgs = new();
    protected bool isLoading;
    protected string? errorMessage;
    protected int count;
    
    private readonly CancellationTokenSource _ctsOperations = new(TimeSpan.FromSeconds(30));

    protected override async Task OnInitializedAsync()
    {
        try
        {
            await LoadData();
        }
        catch (Exception ex)
        {
            await LogError(ex, "OnInitializedAsync");
        }
    }

    protected async Task LoadData()
    {
        try
        {
            isLoading = true;
            var result = await RepoFormatos.Get(
                orgId: CurrentUser.OrgId,
                elUser: CurrentUser,
                filtro: f => f.Status,
                cancellationToken: _ctsOperations.Token
            );

            if (result.Exito)
            {
                formatos = result.DataVarios;
                count = formatos?.Count() ?? 0;
                await LoadOrganizaciones();
            }
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
            cancellationToken: _ctsOperations.Token
        );

        if (!result.Exito) return new List<W100_Org>();

        var orgIds = result.DataVarios.Select(fg => fg.OrgId);
        var orgsResult = await RepoOrg.Get(
            orgId: CurrentUser.OrgId,
            elUser: CurrentUser,
            filtro: o => orgIds.Contains(o.OrgId),
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
            desc: $"Error en {origen}: {ex.Message}",
            tipoLog: "Error",
            origen: $"FormatosBase.{origen}",
            userId: CurrentUser.Id,
            orgId: CurrentUser.OrgId,
            cancellationToken: _ctsOperations.Token
        );
    }

    protected async Task OnCreateRow()
    {
        try
        {
            var newFormato = new W290_Formatos(
                formatoNombre: "Nuevo Formato",
                descripcion: "",
                orgId: CurrentUser.OrgId,
                global: false,
                estado: 5,
                status: true
            );

            gridFormatos.InsertRow(newFormato);
        }
        catch (Exception ex)
        {
            await LogError(ex, "OnCreateRow");
        }
    }

    protected void SaveRow(W290_Formatos formato)
    {
        try
        {
            gridFormatos.UpdateRow(formato);
        }
        catch (Exception ex)
        {
            gridFormatos.CancelEditRow(formato);
            throw;
        }
    }

    protected void CancelEdit(W290_Formatos formato)
    {
        gridFormatos.CancelEditRow(formato);
    }

    protected async Task OnUpdateRow(W290_Formatos formato)
    {
        try 
        {
            var result = await RepoFormatos.Update(formato, CurrentUser.OrgId, CurrentUser, _ctsOperations.Token);
            if (!result.Exito)
            {
                throw new Exception(result.Texto);
            }

            await RepoBitacora.AddBitacora(
                userId: CurrentUser.Id,
                desc: $"Formato actualizado: {formato.FormatoNombre}",
                orgId: CurrentUser.OrgId,
                cancellationToken: _ctsOperations.Token
            );

            await LoadData();
        }
        catch (Exception ex)
        {
            await LogError(ex, "OnUpdateRow");
            throw;
        }
    }

    protected async Task OnInsertRow(W290_Formatos formato)
    {
        try
        {
            var result = await RepoFormatos.Insert(formato, CurrentUser.OrgId, CurrentUser, _ctsOperations.Token);
            if (!result.Exito)
            {
                throw new Exception(result.Texto);
            }

            await RepoBitacora.AddBitacora(
                userId: CurrentUser.Id,
                desc: $"Formato creado: {formato.FormatoNombre}",
                orgId: CurrentUser.OrgId,
                cancellationToken: _ctsOperations.Token
            );

            await LoadData();
        }
        catch (Exception ex)
        {
            await LogError(ex, "OnInsertRow");
            throw;
        }
    }

    protected async Task DesactivarFormato(W290_Formatos formato)
    {
        try
        {
            formato.Status = false;
            var result = await RepoFormatos.Update(
                formato,
                CurrentUser.OrgId,
                CurrentUser,
                _ctsOperations.Token
            );

            if (!result.Exito)
            {
                throw new Exception(result.Texto);
            }

            await RepoBitacora.AddBitacora(
                userId: CurrentUser.Id,
                desc: $"Formato desactivado: {formato.FormatoNombre}",
                orgId: CurrentUser.OrgId,
                cancellationToken: _ctsOperations.Token
            );

            await LoadData();
        }
        catch (Exception ex)
        {
            await LogError(ex, "DesactivarFormato");
        }
    }

    public void Dispose()
    {
        _ctsOperations.Dispose();
    }
} 