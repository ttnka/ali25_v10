using Ali25_V10.Data;
using Ali25_V10.Data.Modelos;
using Ali25_V10.Data.Sistema;
using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;

namespace Ali25_V10.Components.Pages.Configuracion;

public class ConfigListBase : ComponentBase, IDisposable
{
    [CascadingParameter] protected ApplicationUser CurrentUser { get; set; } = default!;
    [Inject] protected IRepo<WConfig> RepoConfig { get; set; } = default!;
    [Inject] protected IRepoBitacora RepoBitacora { get; set; } = default!;

    protected RadzenDataGrid<WConfig> gridConfig = default!;
    protected IEnumerable<WConfig>? configs;
    protected List<WConfig> configsToInsert = new();
    protected List<WConfig> configsToUpdate = new();
    
    protected bool isLoading;
    protected bool isEditing;
    protected bool isRefreshing;
    protected string? errorMessage;
    protected bool bypassCache;
    protected bool ListaEdit => CurrentUser.Nivel >= 5;
    protected DataGridEditMode editMode = DataGridEditMode.Single;
    protected readonly CancellationTokenSource _ctsOperations = new(TimeSpan.FromSeconds(30));

    protected string? selectedGrupo;
    protected string? selectedTipo;

    protected override async Task OnInitializedAsync()
    {
        if (CurrentUser.Nivel < 5) return;
        
        try
        {
            await LoadData();
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            await LogError(ex, "OnInitializedAsync");
        }
    }

    protected async Task LoadData()
    {
        try
        {
            isLoading = true;
            var result = await RepoConfig.Get(
                orgId: CurrentUser.OrgId,
                elUser: CurrentUser,
                cancellationToken: _ctsOperations.Token
            );

            if (result.Exito)
            {
                configs = result.DataVarios;
                
                // Aplicar filtros
                if (!string.IsNullOrEmpty(selectedGrupo))
                {
                    configs = configs?.Where(c => c.Grupo.Contains(selectedGrupo, StringComparison.OrdinalIgnoreCase));
                }
                if (!string.IsNullOrEmpty(selectedTipo))
                {
                    configs = configs?.Where(c => c.Tipo.Contains(selectedTipo, StringComparison.OrdinalIgnoreCase));
                }
            }
        }
        catch (Exception ex)
        {
            await LogError(ex, "LoadData");
            throw;
        }
        finally
        {
            isLoading = false;
        }
    }

    protected async Task OnCreateRow(WConfig config)
    {
        try
        {
            var result = await RepoConfig.Insert(
                config,
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
                desc: $"Configuración creada: {config.Titulo}",
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

    protected async Task OnUpdateRow(WConfig config)
    {
        try
        {
            var result = await RepoConfig.Update(
                config,
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
                desc: $"Configuración actualizada: {config.Titulo}",
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

    protected async Task LogError(Exception ex, string origen)
    {
        await RepoBitacora.AddLog(
            userId: CurrentUser.Id,
            desc: $"Error en {origen}: {ex.Message}",
            tipoLog: "Error",
            origen: $"ConfigListBase.{origen}",
            orgId: CurrentUser.OrgId,
            cancellationToken: _ctsOperations.Token
        );
    }

    protected void Reset(WConfig config)
    {
        configsToInsert.Remove(config);
        configsToUpdate.Remove(config);
    }

    protected void Reset()
    {
        configsToInsert.Clear();
        configsToUpdate.Clear();
    }

    protected async Task EditRow(WConfig config)
    {
        if (gridConfig?.EditMode == DataGridEditMode.Single)
        {
            Reset();
        }
        configsToUpdate.Add(config);
        await gridConfig.EditRow(config);
    }

    protected async Task SaveRow(WConfig config)
    {
        await gridConfig.UpdateRow(config);
    }

    protected void CancelEdit(WConfig config)
    {
        Reset(config);
        gridConfig.CancelEditRow(config);
    }

    protected async Task DeleteRow(WConfig config)
    {
        try 
        {
            Reset(config);
            if (configs?.Contains(config) == true)
            {
                config.Estado = 0;
                var result = await RepoConfig.Update(
                    config,
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
                    desc: $"Configuración eliminada: {config.Titulo}",
                    orgId: CurrentUser.OrgId,
                    cancellationToken: _ctsOperations.Token
                );

                await gridConfig.Reload();
            }
            else
            {
                gridConfig.CancelEditRow(config);
                await gridConfig.Reload();
            }
        }
        catch (Exception ex)
        {
            await LogError(ex, "DeleteRow");
        }
    }

    protected async Task ToggleBypassCache()
    {
        bypassCache = !bypassCache;
        await LoadData();
    }

    protected async Task RefreshData()
    {
        try
        {
            isRefreshing = true;
            await LoadData();
        }
        catch (Exception ex)
        {
            await LogError(ex, "RefreshData");
        }
        finally
        {
            isRefreshing = false;
        }
    }

    protected async Task InsertRow()
    {
        if (gridConfig?.EditMode == DataGridEditMode.Single)
        {
            Reset();
        }

        var config = new WConfig 
        { 
            OrgId = CurrentUser.OrgId,
            FechaCreacion = DateTime.Now,
            Estado = 5
        };
        
        configsToInsert.Add(config);
        await gridConfig.InsertRow(config);
    }

    protected string GetEstadoText(int estado)
    {
        if (estado <= 0) return "Inactivo";
        if (estado < 5) return "Pendiente";
        if (estado == 5) return "Activo";
        return "Especial";                         // (> 5)
    }

    protected BadgeStyle GetEstadoBadgeStyle(int estado)
    {
        if (estado <= 0) return BadgeStyle.Danger;  // Inactivo/Eliminado
        if (estado < 5) return BadgeStyle.Warning;  // Pendiente/En proceso
        if (estado == 5) return BadgeStyle.Success; // Activo/Normal
        return BadgeStyle.Info;                     // Especial (> 5)
    }

    public void Dispose()
    {
        _ctsOperations.Dispose();
    }
} 