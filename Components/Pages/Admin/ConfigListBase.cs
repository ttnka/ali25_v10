using Ali25_V10.Data;
using Ali25_V10.Data.Modelos;
using Ali25_V10.Data.Sistema;
using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;

namespace Ali25_V10.Components.Pages.Admin;

public class ConfigListBase : ComponentBase
{
    [CascadingParameter] protected ApplicationUser CurrentUser { get; set; } = default!;
    [Inject] protected IRepo<ZConfig> RepoConfig { get; set; } = default!;
    [Inject] protected IRepo<W100_Org> RepoOrg { get; set; } = default!;
    [Inject] protected IRepoBitacora RepoBitacora { get; set; } = default!;
    [Inject] protected DialogService DialogService { get; set; } = default!;

    protected RadzenDataGrid<ZConfig> gridConfig = default!;
    protected IEnumerable<ZConfig>? configs;
    protected List<W100_Org> orgsDisponibles = new();
    protected List<ZConfig> configsToInsert = new();
    protected List<ZConfig> configsToUpdate = new();
    protected bool isLoading;
    protected bool isEditing;
    protected bool isRefreshing;
    protected string? errorMessage;
    protected DataGridEditMode editMode = DataGridEditMode.Single;

    // Filtros
    protected string? selectedTipo;
    protected string? selectedOrgId;
    protected bool bypassCache;
    protected bool tipoGrupoFilter;
    protected bool showInactive = false;

    protected Dictionary<string, string> tiposConfig = new()
    {
        { "Grupo", "Grupo" },
        { "Elemento", "Elemento" }
    };

    protected override async Task OnInitializedAsync()
    {
        try
        {
            // Cargar organizaciones según nivel
            var orgsResult = await RepoOrg.Get(
                orgId: CurrentUser.OrgId,
                elUser: CurrentUser
            );

            if (orgsResult.Exito)
            {
                orgsDisponibles = orgsResult.DataVarios?.ToList() ?? new();
            }

            selectedOrgId = CurrentUser.OrgId;
            await LoadData();
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            await RepoBitacora.AddLog(
                userId: CurrentUser.Id,
                orgId: CurrentUser.OrgId,
                desc: $"Error al inicializar configuraciones: {ex.Message}",
                tipoLog: "Error",
                origen: "ConfigListBase.OnInitializedAsync"
            );
        }
    }

    protected async Task LoadData()
    {
        try
        {
            isLoading = true;
            
            var result = await RepoConfig.Get(
                orgId: selectedOrgId ?? CurrentUser.OrgId,
                elUser: CurrentUser,
                byPassCache: bypassCache
            );

            if (result.Exito)
            {
                configs = result.DataVarios;
                if (selectedTipo != null)
                {
                    configs = configs?.Where(c => c.TipoGrupo == tipoGrupoFilter);
                }
                if (!showInactive)
                {
                    configs = configs?.Where(c => c.Status);
                }
            }
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            await RepoBitacora.AddLog(
                userId: CurrentUser.Id,
                orgId: CurrentUser.OrgId,
                desc: $"Error al cargar configuraciones: {ex.Message}",
                tipoLog: "Error",
                origen: "ConfigListBase.LoadData"
            );
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    protected async Task OnFiltroSelected(string? value)
    {
        selectedTipo = value;
        tipoGrupoFilter = value == "Grupo";
        await LoadData();
    }

    protected async Task OnOrgSelected(string? value)
    {
        selectedOrgId = value;
        await LoadData();
    }

    protected async Task RefreshData()
    {
        isRefreshing = true;
        await LoadData();
        isRefreshing = false;
    }

    protected void Reset(ZConfig config)
    {
        configsToInsert.Remove(config);
        configsToUpdate.Remove(config);
    }

    protected void Reset()
    {
        configsToInsert.Clear();
        configsToUpdate.Clear();
    }

    protected async Task ToggleBypassCache()
    {
        bypassCache = !bypassCache;
        await LoadData();
    }

    protected async Task EditRow(ZConfig config)
    {
        if (gridConfig?.EditMode == DataGridEditMode.Single)
        {
            Reset();
        }
        isEditing = true;
        configsToUpdate.Add(config);
        await gridConfig.EditRow(config);
    }

    protected async Task SaveRow(ZConfig config)
    {
        try 
        {
            if (gridConfig.IsValid)
            {
                isEditing = false;
                await gridConfig.UpdateRow(config);
            }
            else
            {
                errorMessage = "Por favor corrija los errores antes de guardar.";
            }
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            await RepoBitacora.AddLog(
                userId: CurrentUser.Id,
                orgId: CurrentUser.OrgId,
                desc: $"Error al guardar configuración: {ex.Message}",
                tipoLog: "Error",
                origen: "ConfigListBase.SaveRow"
            );
        }
    }

    protected void CancelEdit(ZConfig config)
    {
        isEditing = false;
        Reset(config);
        gridConfig.CancelEditRow(config);
    }

    protected async Task DeleteRow(ZConfig config)
    {
        try 
        {
            Reset(config);

            if (configs?.Contains(config) == true)
            {
                config.Status = false;
                var result = await RepoConfig.Update(config, CurrentUser.OrgId, elUser: CurrentUser);
                if (!result.Exito)
                {
                    throw new Exception(result.Texto);
                }

                await RepoBitacora.AddBitacora(
                    userId: CurrentUser.Id,
                    desc: $"Se eliminó la configuración {config.Titulo}",
                    orgId: CurrentUser.OrgId
                );

                errorMessage = null;
                await LoadData();
            }
            else
            {
                gridConfig.CancelEditRow(config);
                await LoadData();
            }
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            await RepoBitacora.AddLog(
                userId: CurrentUser.Id,
                orgId: CurrentUser.OrgId,
                desc: $"Error al eliminar configuración: {ex.Message}",
                tipoLog: "Error",
                origen: "ConfigListBase.DeleteRow"
            );
            throw;
        }
    }

    protected async Task InsertRow()
    {
        try
        {
            if (gridConfig?.EditMode == DataGridEditMode.Single)
            {
                Reset();
            }

            var config = new ZConfig 
            { 
                OrgId = selectedOrgId ?? CurrentUser.OrgId,
                Estado = 5,
                Status = true,
                Grupo = string.Empty,
                Titulo = string.Empty,
                Descripcion = string.Empty,
                Configuracion = string.Empty,
                TextoId = string.Empty
            };
            
            isEditing = true;
            configsToInsert.Add(config);
            await gridConfig.InsertRow(config);
            errorMessage = null;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            await RepoBitacora.AddLog(
                userId: CurrentUser.Id,
                orgId: CurrentUser.OrgId,
                desc: $"Error al preparar nueva configuración: {ex.Message}",
                tipoLog: "Error",
                origen: "ConfigListBase.InsertRow"
            );
        }
    }

    protected async Task OnCreateRow(ZConfig config)
    {
        try
        {
            if (!gridConfig.IsValid)
            {
                errorMessage = "Por favor corrija los errores antes de guardar.";
                return;
            }

            var result = await RepoConfig.Insert(config, CurrentUser.OrgId, elUser: CurrentUser);
            if (!result.Exito)
            {
                throw new Exception(result.Texto);
            }

            await RepoBitacora.AddBitacora(
                userId: CurrentUser.Id,
                desc: $"Se creó la configuración {config.Titulo}",
                orgId: CurrentUser.OrgId
            );

            configsToInsert.Remove(config);
            isEditing = false;
            errorMessage = null;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            await RepoBitacora.AddLog(
                userId: CurrentUser.Id,
                orgId: CurrentUser.OrgId,
                desc: $"Error al crear configuración: {ex.Message}",
                tipoLog: "Error",
                origen: "ConfigListBase.OnCreateRow"
            );
            throw;
        }
    }

    protected async Task OnUpdateRow(ZConfig config)
    {
        try
        {
            Reset(config);
            
            var result = await RepoConfig.Update(config, CurrentUser.OrgId, elUser: CurrentUser);
            if (!result.Exito)
            {
                throw new Exception(result.Texto);
            }

            await RepoBitacora.AddBitacora(
                userId: CurrentUser.Id,
                desc: $"Se actualizó la configuración {config.Titulo}",
                orgId: CurrentUser.OrgId
            );

            errorMessage = null; // Limpiar mensaje de error si todo sale bien
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            await RepoBitacora.AddLog(
                userId: CurrentUser.Id,
                orgId: CurrentUser.OrgId,
                desc: $"Error al actualizar configuración: {ex.Message}",
                tipoLog: "Error",
                origen: "ConfigListBase.OnUpdateRow"
            );
            throw; // Re-lanzar para que el grid maneje el error
        }
    }

    protected BadgeStyle GetEstadoBadgeStyle(int estado)
    {
        if (estado <= 0) return BadgeStyle.Danger;  // Inactivo/Eliminado
        if (estado < 5) return BadgeStyle.Warning;  // Pendiente/En proceso
        if (estado == 5) return BadgeStyle.Success; // Activo/Normal
        return BadgeStyle.Info;                     // Especial (> 5)
    }

    protected string GetEstadoText(int estado)
    {
        if (estado <= 0) return "Inactivo";
        if (estado < 5) return "Pendiente";
        if (estado == 5) return "Activo";
        return "Especial";                         // (> 5)
    }

    protected async Task ToggleInactive()
    {
        showInactive = !showInactive;
        await LoadData();
    }

    protected async Task ShowHelp()
    {
        await DialogService.Alert(
            "Las configuraciones permiten definir parámetros del sistema. " +
            "Los grupos agrupan elementos relacionados. " +
            "El estado indica la situación actual del registro (5=Activo). " +
            "El status indica si está disponible para uso.",
            "Ayuda",
            new AlertOptions { OkButtonText = "Entendido" });
    }
} 