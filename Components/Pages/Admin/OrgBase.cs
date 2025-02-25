using Ali25_V10.Data;
using Ali25_V10.Data.Modelos;
using Ali25_V10.Data.Sistema;
using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;
using System.Threading;

namespace Ali25_V10.Components.Pages.Admin;

public class OrgBase : ComponentBase, IDisposable
{
    [CascadingParameter] protected ApplicationUser CurrentUser { get; set; } = default!;
    [Inject] protected IRepo<W100_Org> RepoOrg { get; set; } = default!;
    [Inject] protected IRepoBitacora RepoBitacora { get; set; } = default!;

    protected RadzenDataGrid<W100_Org> gridOrg = default!;
    protected IEnumerable<W100_Org>? orgs;
    protected int count;
    protected bool isLoading;
    protected bool isRefreshing;
    protected bool isEditing;
    protected bool bypassCache;
    protected string? errorMessage;

    // Colecciones para edición
    protected List<W100_Org> orgsToInsert = new();
    protected List<W100_Org> orgsToUpdate = new();
    protected DataGridEditMode editMode = DataGridEditMode.Single;

    // Datos específicos de organizaciones
    protected List<string> tiposOrg = new();
    protected HashSet<string> existingRfcs = new();

    protected readonly CancellationTokenSource _ctsOperations = new(TimeSpan.FromSeconds(30));
    protected readonly CancellationTokenSource _ctsBitacora = new(TimeSpan.FromSeconds(5)); 

    protected override async Task OnInitializedAsync()
    {
        try
        {
            // Configurar tipos disponibles según nivel del usuario
            tiposOrg = CurrentUser.Nivel == 7 
                ? new List<string> { "Admin", "Cliente", "Proveedor" }
                : new List<string> { "Cliente", "Proveedor" };

            // Cargar RFCs existentes
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
                userId: CurrentUser.Id,
                desc: "Accediendo a gestión de organizaciones",
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
            isRefreshing = true;
            
            var result = await RepoOrg.Get(
                orgId: CurrentUser.OrgId,
                elUser: CurrentUser,
                byPassCache: bypassCache,
                cancellationToken: _ctsOperations.Token
            );

            if (result.Exito)
            {
                orgs = result.DataVarios;
                count = orgs?.Count() ?? 0;
            }
        }
        catch (OperationCanceledException)
        {
            await RepoBitacora.AddLog(
                userId: CurrentUser?.Id ?? "Sistema",
                orgId: CurrentUser?.OrgId ?? "Sistema",
                desc: "Operación de carga de organizaciones cancelada por timeout",
                tipoLog: "Warning",
                origen: "OrgBase.LoadData",
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
            isRefreshing = false;
            StateHasChanged();
        }
    }

    protected async Task RefreshData()
    {
        if (isRefreshing) return;
        
        try 
        {   
            isRefreshing = true;
            
        }
        catch (Exception ex)
        {
            await LogError(ex, "RefreshData");
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

    protected void Reset(W100_Org org)
    {
        orgsToInsert.Remove(org);
        orgsToUpdate.Remove(org);
    }

    protected void Reset()
    {
        orgsToInsert.Clear();
        orgsToUpdate.Clear();
    }

    protected async Task EditRow(W100_Org org)
    {
        if (gridOrg?.EditMode == DataGridEditMode.Single)
        {
            Reset();
        }
        orgsToUpdate.Add(org);
        await gridOrg.EditRow(org);
    }

    protected async Task SaveRow(W100_Org org)
    {
        try
        {
            await gridOrg.UpdateRow(org);
            var result = await RepoOrg.Update(
                org, 
                CurrentUser.OrgId,
                elUser: CurrentUser,
                cancellationToken: _ctsOperations.Token
            );

            if (!result.Exito)
            {
                throw new Exception(result.Texto);
            }

            await RepoBitacora.AddBitacora(
                userId: CurrentUser.Id,
                desc: $"Se actualizó la organización {org.Comercial}",
                orgId: CurrentUser.OrgId,
                cancellationToken: _ctsBitacora.Token
            );

            await LoadData();
        }
        catch (Exception ex)
        {
            await LogError(ex, "SaveRow");
            throw;
        }
    }

    protected void CancelEdit(W100_Org org)
    {
        Reset(org);
        gridOrg.CancelEditRow(org);
    }

    protected async Task DeleteRow(W100_Org org)
    {
        try 
        {
            Reset(org);
            if (orgs?.Contains(org) == true)
            {
                org.Status = false;
                var result = await RepoOrg.Update(org, CurrentUser.OrgId, elUser: CurrentUser, cancellationToken: _ctsOperations.Token);
                if (!result.Exito)
                {
                    throw new Exception(result.Texto);
                }
                await gridOrg.Reload();

                await RepoBitacora.AddBitacora(
                    userId: CurrentUser.Id,
                    desc: $"Se eliminó la organización {org.Comercial}",
                    orgId: CurrentUser.OrgId,
                    cancellationToken: _ctsBitacora.Token
                );
            }
            else
            {
                gridOrg.CancelEditRow(org);
                await gridOrg.Reload();
            }
        }
        catch (Exception ex)
        {
            await LogError(ex, "DeleteRow");
        }
    }

    protected async Task OnUpdateRow(W100_Org org)
    {
        try
        {
            Reset(org);
            
            // Verificar RFC duplicado
            var rfcToCheck = org.Rfc.ToUpper();
            var originalOrg = orgs?.FirstOrDefault(o => o.OrgId == org.OrgId);
            if (originalOrg != null && 
                rfcToCheck != originalOrg.Rfc.ToUpper() && 
                existingRfcs.Contains(rfcToCheck))
            {
                throw new Exception($"El RFC {org.Rfc} ya existe en otra organización");
            }

            var result = await RepoOrg.Update(
                org, 
                CurrentUser.OrgId,
                elUser: CurrentUser,
                cancellationToken: _ctsOperations.Token
            );

            if (!result.Exito)
            {
                throw new Exception(result.Texto);
            }

            // Actualizar RFCs
            if (originalOrg != null)
            {
                existingRfcs.Remove(originalOrg.Rfc.ToUpper());
            }
            existingRfcs.Add(rfcToCheck);

            await RepoBitacora.AddBitacora(
                userId: CurrentUser.Id,
                desc: $"Se actualizó la organización {org.Comercial}",
                orgId: CurrentUser.OrgId,
                cancellationToken: _ctsBitacora.Token
            );

            await LoadData();
        }
        catch (Exception ex)
        {
            await LogError(ex, "OnUpdateRow");
            if (gridOrg != null)
            {
                await gridOrg.Reload();
            }
            throw;
        }
    }

    protected async Task LogError(Exception ex, string origen)
    {
        errorMessage = ex.Message;
        await RepoBitacora.AddLog(
            userId: CurrentUser.Id,
            desc: $"Error en {origen}: {ex.Message}",
            tipoLog: "Error",
            origen: $"OrgBase.{origen}",
            orgId: CurrentUser.OrgId,
            cancellationToken: _ctsBitacora.Token
        );
    }

    public void Dispose()
    {
        _ctsOperations.Dispose();
        _ctsBitacora.Dispose();
    }
} 