using Ali25_V10.Data;
using Ali25_V10.Data.Modelos;
using Ali25_V10.Data.Sistema;
using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;

namespace Ali25_V10.Components.Pages.Configuracion;

public class FormatosGpoListBase : ComponentBase, IDisposable
{
    [Parameter] public string FormatoId { get; set; } = string.Empty;
    [CascadingParameter] protected ApplicationUser CurrentUser { get; set; } = default!;
    [Inject] protected IRepo<W290_Formatos> RepoFormatos { get; set; } = default!;
    [Inject] protected IRepo<W291_FormatoGpo> RepoFormatoGpo { get; set; } = default!;
    [Inject] protected IRepo<W100_Org> RepoOrg { get; set; } = default!;
    [Inject] protected IRepoBitacora RepoBitacora { get; set; } = default!;
    [Inject] protected NavigationManager NavigationManager { get; set; } = default!;

    protected RadzenDataGrid<W291_FormatoGpo> gridFormatosGpo = default!;
    protected IEnumerable<W291_FormatoGpo>? formatosGpo;
    protected List<W100_Org> organizacionesDisponibles = new();
    protected Dictionary<string, W100_Org> orgCache = new();
    protected W290_Formatos? Formato;
    protected string? selectedOrgId;
    protected string? errorMessage;
    protected int count;
    protected bool isLoading;
    
    private readonly CancellationTokenSource _ctsOperations = new(TimeSpan.FromSeconds(30));

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

        Formato = result.DataVarios.First();
    }

    protected async Task LoadData()
    {
        try
        {
            isLoading = true;
            var result = await RepoFormatoGpo.Get(
                orgId: CurrentUser.OrgId,
                elUser: CurrentUser,
                filtro: f => f.FormatoId == FormatoId && f.Status,
                cancellationToken: _ctsOperations.Token
            );

            if (result.Exito)
            {
                formatosGpo = result.DataVarios;
                count = formatosGpo?.Count() ?? 0;
                await CargarOrganizaciones();
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

    protected async Task CargarOrganizaciones()
    {
        var result = await RepoOrg.Get(
            orgId: CurrentUser.OrgId,
            elUser: CurrentUser,
            cancellationToken: _ctsOperations.Token
        );

        if (result.Exito)
        {
            var asignadasIds = formatosGpo?.Select(f => f.OrgId) ?? new List<string>();
            organizacionesDisponibles = result.DataVarios
                .Where(o => !asignadasIds.Contains(o.OrgId))
                .ToList();

            // Actualizar caché de organizaciones
            foreach (var org in result.DataVarios)
            {
                orgCache[org.OrgId] = org;
            }
        }
    }

    protected string GetOrgNombre(string orgId)
    {
        return orgCache.ContainsKey(orgId) ? orgCache[orgId].Comercial : orgId;
    }

    protected async Task AgregarOrganizacion(string? orgId)
    {
        try
        {
            if (string.IsNullOrEmpty(orgId)) return;

            var formatoGpo = new W291_FormatoGpo(
                formatoId: FormatoId,
                orgId: orgId,
                estado: 5,
                status: true
            );

            var result = await RepoFormatoGpo.Insert(
                formatoGpo,
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
                desc: $"Organización agregada al formato: {GetOrgNombre(orgId)}",
                orgId: CurrentUser.OrgId,
                cancellationToken: _ctsOperations.Token
            );

            selectedOrgId = null;
            await LoadData();
        }
        catch (Exception ex)
        {
            await LogError(ex, "AgregarOrganizacion");
        }
    }

    protected BadgeStyle GetEstadoBadgeStyle(int estado) => estado switch
    {
        <= 0 => BadgeStyle.Danger,
        < 5 => BadgeStyle.Warning,
        5 => BadgeStyle.Success,
        _ => BadgeStyle.Info
    };

    protected string GetEstadoText(int estado) => estado switch
    {
        <= 0 => "Inactivo",
        < 5 => "Pendiente",
        5 => "Activo",
        _ => "Especial"
    };

    protected async Task LogError(Exception ex, string origen)
    {
        errorMessage = ex.Message;
        await RepoBitacora.AddLog(
            desc: $"Error en {origen}: {ex.Message}",
            tipoLog: "Error",
            origen: $"FormatosGpoListBase.{origen}",
            userId: CurrentUser.Id,
            orgId: CurrentUser.OrgId,
            cancellationToken: _ctsOperations.Token
        );
    }

    protected async Task DesactivarOrganizacion(W291_FormatoGpo formatoGpo)
    {
        try
        {
            formatoGpo.Status = false;
            var result = await RepoFormatoGpo.Update(
                formatoGpo,
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
                desc: $"Organización removida del formato: {GetOrgNombre(formatoGpo.OrgId)}",
                orgId: CurrentUser.OrgId,
                cancellationToken: _ctsOperations.Token
            );

            await LoadData();
        }
        catch (Exception ex)
        {
            await LogError(ex, "DesactivarOrganizacion");
        }
    }

    public void Dispose()
    {
        _ctsOperations.Dispose();
    }
} 