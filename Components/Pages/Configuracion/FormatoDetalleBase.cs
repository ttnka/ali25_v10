using Ali25_V10.Data;
using Ali25_V10.Data.Modelos;
using Ali25_V10.Data.Sistema;
using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;

namespace Ali25_V10.Components.Pages.Configuracion;

public class FormatoDetalleBase : ComponentBase, IDisposable
{
    [Parameter] public string FormatoId { get; set; } = string.Empty;
    [CascadingParameter] protected ApplicationUser CurrentUser { get; set; } = default!;
    [Inject] protected IRepo<W290_Formatos> RepoFormatos { get; set; } = default!;
    [Inject] protected IRepo<W292_FormatoDet> RepoFormatoDet { get; set; } = default!;
    [Inject] protected IRepoBitacora RepoBitacora { get; set; } = default!;
    [Inject] protected NavigationManager NavigationManager { get; set; } = default!;

    protected RadzenDataGrid<W292_FormatoDet> gridFormatoDet = default!;
    protected IEnumerable<W292_FormatoDet>? formatoDetalles;
    protected W290_Formatos? formato;
    protected bool isLoading;
    protected string? errorMessage;
    protected int count;

    protected List<W292_FormatoDet> formatoDetallesToInsert = new();
    protected List<W292_FormatoDet> formatoDetallesToUpdate = new();

    protected readonly List<string> tiposCampo = new()
    {
        "texto",
        "decimal",
        "lista"
    };

    protected readonly CancellationTokenSource _ctsOperations = new(TimeSpan.FromSeconds(30));

    protected override async Task OnInitializedAsync()
    {
        try
        {
            await LoadFormato();
            await LoadData();
        }
        catch (Exception ex)
        {
            await LogError(ex, "OnInitializedAsync");
        }
    }

    protected async Task LoadFormato()
    {
        var result = await RepoFormatos.Get(
            orgId: CurrentUser.OrgId,
            elUser: CurrentUser,
            filtro: f => f.FormatoId == FormatoId,
            cancellationToken: _ctsOperations.Token
        );

        if (!result.Exito || !result.DataVarios.Any())
        {
            NavigationManager.NavigateTo("/Configuracion/Formatos");
            return;
        }

        formato = result.DataVarios.First();
    }

    protected async Task LoadData()
    {
        try
        {
            isLoading = true;
            var result = await RepoFormatoDet.Get(
                orgId: CurrentUser.OrgId,
                elUser: CurrentUser,
                filtro: f => f.FormatoId == FormatoId && f.Status,
                cancellationToken: _ctsOperations.Token
            );

            if (result.Exito)
            {
                formatoDetalles = result.DataVarios?.OrderBy(d => d.Orden);
                count = formatoDetalles?.Count() ?? 0;
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

    

    protected async Task LogError(Exception ex, string origen)
    {
        errorMessage = ex.Message;
        await RepoBitacora.AddLog(
            desc: $"Error en {origen}: {ex.Message}",
            tipoLog: "Error",
            origen: $"FormatoDetalleBase.{origen}",
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