using Ali25_V10.Data;
using Ali25_V10.Data.Modelos;
using Ali25_V10.Data.Sistema;
using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;

namespace Ali25_V10.Components.Pages.Clientes;

public class FoliosBase : ComponentBase, IDisposable
{
    [CascadingParameter] protected ApplicationUser CurrentUser { get; set; } = default!;
    [Inject] protected IRepo<W220_Folios> RepoFolios { get; set; } = default!;
    [Inject] protected IRepo<W290_Formatos> RepoFormatos { get; set; } = default!;
    [Inject] protected IRepo<W292_FormatoDet> RepoFormatoDet { get; set; } = default!;
    [Inject] protected IRepo<W222_FolioDet> RepoFolioDet { get; set; } = default!;
    [Inject] protected IRepo<W210_Clientes> RepoClientes { get; set; } = default!;
    [Inject] protected IRepoBitacora RepoBitacora { get; set; } = default!;
    [Inject] protected NavigationManager NavigationManager { get; set; } = default!;

    protected RadzenDataGrid<W220_Folios> gridFolios = default!;
    protected IEnumerable<W220_Folios>? folios;
    protected IEnumerable<W290_Formatos>? formatos;
    protected IEnumerable<W210_Clientes>? clientes;
    protected bool isLoading;
    protected string? errorMessage;
    protected int count;

    private readonly CancellationTokenSource _ctsOperations = new(TimeSpan.FromSeconds(30));

    protected override async Task OnInitializedAsync()
    {
        try
        {
            await LoadFormatos();
            await LoadClientes();
            await LoadData();
        }
        catch (Exception ex)
        {
            await LogError(ex, "OnInitializedAsync");
        }
    }

    protected async Task LoadFormatos()
    {
        var result = await RepoFormatos.Get(
            orgId: CurrentUser.OrgId,
            elUser: CurrentUser,
            filtro: f => f.Status,
            cancellationToken: _ctsOperations.Token
        );

        if (result.Exito)
        {
            formatos = result.DataVarios;
        }
    }

    protected async Task LoadClientes()
    {
        var result = await RepoClientes.Get(
            orgId: CurrentUser.OrgId,
            elUser: CurrentUser,
            filtro: c => c.Status,
            cancellationToken: _ctsOperations.Token
        );

        if (result.Exito)
        {
            clientes = result.DataVarios;
        }
    }

    protected async Task LoadData()
    {
        try
        {
            isLoading = true;
            var result = await RepoFolios.Get(
                orgId: CurrentUser.OrgId,
                elUser: CurrentUser,
                filtro: f => f.Status,
                cancellationToken: _ctsOperations.Token
            );

            if (result.Exito)
            {
                folios = result.DataVarios;
                count = folios?.Count() ?? 0;
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

    protected async Task<string> GenerarNuevoFolio()
    {
        var ultimoFolio = folios?
            .Select(f => f.Folio)
            .Where(f => f.StartsWith("F"))
            .Select(f => int.TryParse(f[1..], out int n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max() ?? 0;

        return $"F{(ultimoFolio + 1):D5}";
    }

    protected async Task OnCreateRow(W220_Folios folio)
    {
        try
        {
            folio.Folio = await GenerarNuevoFolio();
            folio.FechaCaptura = DateTime.Now;
            folio.OrgId = CurrentUser.OrgId;

            var result = await RepoFolios.Insert(
                folio,
                CurrentUser.OrgId,
                CurrentUser,
                _ctsOperations.Token
            );

            if (!result.Exito)
            {
                throw new Exception(result.Texto);
            }

            // Crear detalles basados en el formato seleccionado
            if (!string.IsNullOrEmpty(folio.FormatoId))
            {
                await CrearDetallesFolio(folio.FolioId, folio.FormatoId);
            }

            await RepoBitacora.AddBitacora(
                userId: CurrentUser.Id,
                desc: $"Folio creado: {folio.Folio}",
                orgId: CurrentUser.OrgId,
                cancellationToken: _ctsOperations.Token
            );

            await LoadData();
        }
        catch (Exception ex)
        {
            await LogError(ex, "OnCreateRow");
            throw;
        }
    }

    protected async Task OnUpdateRow(W220_Folios folio)
    {
        try
        {
            var result = await RepoFolios.Update(
                folio,
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
                desc: $"Folio actualizado: {folio.Folio}",
                orgId: CurrentUser.OrgId,
                cancellationToken: _ctsOperations.Token
            );
        }
        catch (Exception ex)
        {
            await LogError(ex, "OnUpdateRow");
            throw;
        }
    }

    protected async Task CrearDetallesFolio(string folioId, string formatoId)
    {
        var campos = await RepoFormatoDet.Get(
            orgId: CurrentUser.OrgId,
            elUser: CurrentUser,
            filtro: f => f.FormatoId == formatoId && f.Status,
            cancellationToken: _ctsOperations.Token
        );

        if (!campos.Exito) return;

        foreach (var campo in campos.DataVarios)
        {
            var detalle = new W222_FolioDet(
                folioId: folioId,
                campo: campo.Campo,
                valor: "",
                estado: 5,
                status: true
                
            );

            await RepoFolioDet.Insert(
                detalle,
                CurrentUser.OrgId,
                CurrentUser,
                _ctsOperations.Token
            );
        }
    }

    protected void InsertRow()
    {
        gridFolios.InsertRow(new W220_Folios(
            fechaFolio: DateTime.Now,
            fechaCaptura: DateTime.Now,
            clienteId: "",
            formatoId: "",
            orgId: CurrentUser.OrgId,
            estado: 5,
            status: true
        ));
    }

    protected void EditRow(W220_Folios folio)
    {
        gridFolios.EditRow(folio);
    }

    protected void SaveRow(W220_Folios folio)
    {
        gridFolios.UpdateRow(folio);
    }

    protected void CancelEdit(W220_Folios folio)
    {
        gridFolios.CancelEditRow(folio);
    }

    protected async Task LogError(Exception ex, string origen)
    {
        errorMessage = ex.Message;
        await RepoBitacora.AddLog(
            desc: $"Error en {origen}: {ex.Message}",
            tipoLog: "Error",
            origen: $"FoliosBase.{origen}",
            userId: CurrentUser.Id,
            orgId: CurrentUser.OrgId,
            cancellationToken: _ctsOperations.Token
        );
    }

    public void Dispose()
    {
        _ctsOperations.Dispose();
    }
} 