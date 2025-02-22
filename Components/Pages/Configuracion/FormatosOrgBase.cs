using Ali25_V10.Data;
using Ali25_V10.Data.Modelos;
using Ali25_V10.Data.Sistema;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace Ali25_V10.Components.Pages.Configuracion;

public class FormatosOrgBase : ComponentBase, IDisposable
{
    [CascadingParameter] protected ApplicationUser CurrentUser { get; set; } = default!;
    [Inject] protected IRepo<W291_FormatoGpo> RepoFormatoGpo { get; set; } = default!;
    [Inject] protected IRepo<W100_Org> RepoOrg { get; set; } = default!;
    [Inject] protected IRepoBitacora RepoBitacora { get; set; } = default!;
    //[Inject] protected DialogService DialogService { get; set; } = default!;

    [Parameter] public W290_Formatos? Formato { get; set; }
    [Parameter] public List<W100_Org>? OrganizacionesActuales { get; set; }

    protected List<W100_Org> organizacionesDisponibles = new();
    protected List<W100_Org> organizacionesAsignadas = new();
    protected string? selectedOrgId;
    protected string? errorMessage;
    
    private readonly CancellationTokenSource _ctsOperations = new(TimeSpan.FromSeconds(30));

    protected override async Task OnInitializedAsync()
    {
        try
        {
            if (Formato == null) return;

            organizacionesAsignadas = OrganizacionesActuales?.ToList() ?? new();
            await CargarOrganizacionesDisponibles();
        }
        catch (Exception ex)
        {
            await LogError(ex, "OnInitializedAsync");
        }
    }

    protected async Task CargarOrganizacionesDisponibles()
    {
        var result = await RepoOrg.Get(
            orgId: CurrentUser.OrgId,
            elUser: CurrentUser,
            cancellationToken: _ctsOperations.Token
        );

        if (result.Exito)
        {
            var asignadasIds = organizacionesAsignadas.Select(o => o.OrgId);
            organizacionesDisponibles = result.DataVarios
                .Where(o => !asignadasIds.Contains(o.OrgId))
                .ToList();
        }
    }

    protected async Task AgregarOrganizacion(string? orgId)
    {
        try
        {
            if (string.IsNullOrEmpty(orgId) || Formato == null) return;

            var formatoGpo = new W291_FormatoGpo(
                formatoId: Formato.FormatoId,
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

            var org = organizacionesDisponibles.First(o => o.OrgId == orgId);
            organizacionesAsignadas.Add(org);
            organizacionesDisponibles.Remove(org);
            selectedOrgId = null;

            await RepoBitacora.AddBitacora(
                userId: CurrentUser.Id,
                desc: $"Organización {org.Comercial} agregada al formato {Formato.FormatoNombre}",
                orgId: CurrentUser.OrgId,
                cancellationToken: _ctsOperations.Token
            );
        }
        catch (Exception ex)
        {
            await LogError(ex, "AgregarOrganizacion");
        }
    }

    protected async Task QuitarOrganizacion(W100_Org org)
    {
        try
        {
            if (Formato == null) return;

            var result = await RepoFormatoGpo.Get(
                orgId: CurrentUser.OrgId,
                elUser: CurrentUser,
                filtro: fg => fg.FormatoId == Formato.FormatoId && fg.OrgId == org.OrgId,
                cancellationToken: _ctsOperations.Token
            );

            if (!result.Exito || !result.DataVarios.Any()) return;

            var formatoGpo = result.DataVarios.First();
            formatoGpo.Status = false;

            await RepoFormatoGpo.Update(
                formatoGpo,
                CurrentUser.OrgId,
                CurrentUser,
                _ctsOperations.Token
            );

            organizacionesAsignadas.Remove(org);
            organizacionesDisponibles.Add(org);

            await RepoBitacora.AddBitacora(
                userId: CurrentUser.Id,
                desc: $"Organización {org.Comercial} removida del formato {Formato.FormatoNombre}",
                orgId: CurrentUser.OrgId,
                cancellationToken: _ctsOperations.Token
            );
        }
        catch (Exception ex)
        {
            await LogError(ex, "QuitarOrganizacion");
        }
    }

    protected async Task LogError(Exception ex, string origen)
    {
        errorMessage = ex.Message;
        await RepoBitacora.AddLog(
            desc: $"Error en {origen}: {ex.Message}",
            tipoLog: "Error",
            origen: $"FormatosOrgBase.{origen}",
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