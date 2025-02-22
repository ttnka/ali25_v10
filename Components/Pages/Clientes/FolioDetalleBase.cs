using Ali25_V10.Data;
using Ali25_V10.Data.Modelos;
using Ali25_V10.Data.Sistema;
using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;

namespace Ali25_V10.Components.Pages.Clientes;

public class FolioDetalleBase : ComponentBase, IDisposable
{
    [Parameter] public string FolioId { get; set; } = string.Empty;
    [CascadingParameter] protected ApplicationUser CurrentUser { get; set; } = default!;
    [Inject] protected IRepo<W220_Folios> RepoFolios { get; set; } = default!;
    [Inject] protected IRepo<W222_FolioDet> RepoFolioDet { get; set; } = default!;
    [Inject] protected IRepo<W292_FormatoDet> RepoFormatoDet { get; set; } = default!;
    [Inject] protected IRepoBitacora RepoBitacora { get; set; } = default!;
    [Inject] protected NavigationManager NavigationManager { get; set; } = default!;

    protected RadzenDataGrid<W222_FolioDet> gridFolioDet = default!;
    protected IEnumerable<W222_FolioDet>? folioDetalles;
    protected Dictionary<string, W292_FormatoDet> formatoDetalles = new();
    protected W220_Folios? folio;
    protected bool isLoading;
    protected string? errorMessage;
    protected int count;

    private readonly CancellationTokenSource _ctsOperations = new(TimeSpan.FromSeconds(30));

    protected override async Task OnInitializedAsync()
    {
        try
        {
            await LoadFolio();
            await LoadFormatoDetalles();
            await LoadData();
        }
        catch (Exception ex)
        {
            await LogError(ex, "OnInitializedAsync");
        }
    }

    protected async Task LoadFolio()
    {
        var result = await RepoFolios.Get(
            orgId: CurrentUser.OrgId,
            elUser: CurrentUser,
            filtro: f => f.FolioId == FolioId,
            cancellationToken: _ctsOperations.Token
        );

        if (!result.Exito || !result.DataVarios.Any())
        {
            NavigationManager.NavigateTo("/Clientes/Folios");
            return;
        }

        folio = result.DataVarios.First();
    }

    protected async Task LoadFormatoDetalles()
    {
        if (folio == null) return;

        var result = await RepoFormatoDet.Get(
            orgId: CurrentUser.OrgId,
            elUser: CurrentUser,
            filtro: f => f.FormatoId == folio.FormatoId && f.Status,
            cancellationToken: _ctsOperations.Token
        );

        if (result.Exito)
        {
            formatoDetalles = result.DataVarios.ToDictionary(d => d.Campo);
        }
    }

    protected async Task LoadData()
    {
        try
        {
            isLoading = true;
            var result = await RepoFolioDet.Get(
                orgId: CurrentUser.OrgId,
                elUser: CurrentUser,
                filtro: f => f.FolioId == FolioId && f.Status,
                cancellationToken: _ctsOperations.Token
            );

            if (result.Exito)
            {
                folioDetalles = result.DataVarios;
                count = folioDetalles?.Count() ?? 0;
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

    protected async Task OnUpdateRow(W222_FolioDet detalle)
    {
        try
        {
            // Validar el valor según el tipo de campo
            if (formatoDetalles.TryGetValue(detalle.Campo, out var formatoDet))
            {
                ValidarValor(detalle.Valor, formatoDet.Tipo);
            }

            var result = await RepoFolioDet.Update(
                detalle,
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
                desc: $"Campo actualizado: {detalle.Campo} = {detalle.Valor}",
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

    protected void ValidarValor(string valor, string tipo)
    {
        switch (tipo.ToLower())
        {
            case "decimal":
                if (!decimal.TryParse(valor, out _))
                {
                    throw new Exception("El valor debe ser un número decimal válido");
                }
                break;
            case "lista":
                // Aquí podrías validar que el valor esté en una lista permitida
                break;
            // Para "texto" no necesitamos validación especial
        }
    }

    protected string GetTipoCampo(string campo)
    {
        return formatoDetalles.TryGetValue(campo, out var detalle) ? detalle.Tipo : "texto";
    }

    protected void EditRow(W222_FolioDet detalle)
    {
        gridFolioDet.EditRow(detalle);
    }

    protected void SaveRow(W222_FolioDet detalle)
    {
        gridFolioDet.UpdateRow(detalle);
    }

    protected void CancelEdit(W222_FolioDet detalle)
    {
        gridFolioDet.CancelEditRow(detalle);
    }

    protected async Task LogError(Exception ex, string origen)
    {
        errorMessage = ex.Message;
        await RepoBitacora.AddLog(
            desc: $"Error en {origen}: {ex.Message}",
            tipoLog: "Error",
            origen: $"FolioDetalleBase.{origen}",
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