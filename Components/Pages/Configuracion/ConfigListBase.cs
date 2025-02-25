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
    [Inject] protected NavigationManager NavigationManager { get; set; } = default!;

    protected RadzenDataGrid<WConfig> gridConfig = default!;
    protected IEnumerable<WConfig>? configs;
    protected List<WConfig> configsToInsert = new();
    protected List<WConfig> configsToUpdate = new();
    
    // Standard properties
    protected bool isLoading;
    protected bool isRefreshing;
    protected bool isEditing;
    protected bool bypassCache;
    protected int count;
    protected string? errorMessage;
    protected DataGridEditMode editMode = DataGridEditMode.Single;
    protected bool ListaEdit => CurrentUser.Nivel >= 5;

    protected readonly CancellationTokenSource _ctsOperations = new(TimeSpan.FromSeconds(30));
    protected readonly CancellationTokenSource _ctsBitacora = new(TimeSpan.FromSeconds(5));

    protected string? selectedGrupo;
    protected string? selectedTipo;

    protected override async Task OnInitializedAsync()
    {
        if (CurrentUser.Nivel < 5) return;
        
        try
        {
            await LoadData();
            await RepoBitacora.AddBitacora(
                userId: CurrentUser.Id,
                desc: "Accediendo a lista de configuraciones",
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
            
            var result = await RepoConfig.Get(
                orgId: CurrentUser.OrgId,
                elUser: CurrentUser,
                byPassCache: bypassCache,
                cancellationToken: _ctsOperations.Token
            );

            if (result.Exito)
            {
                configs = result.DataVarios;
                count = configs?.Count() ?? 0;
                
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
                desc: "Operación de carga de configuraciones cancelada por timeout",
                tipoLog: "Warning",
                origen: "ConfigListBase.LoadData",
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

    protected async Task RefreshData()
    {
        if (isRefreshing) return;
        
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
            StateHasChanged();
        }
    }

    protected void ToggleBypassCache()
    {
        bypassCache = !bypassCache;
        LoadData();
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
        isEditing = true;
        if (gridConfig?.EditMode == DataGridEditMode.Single)
        {
            Reset();
        }
        configsToUpdate.Add(config);
        await gridConfig.EditRow(config);
    }

    protected async Task SaveRow(WConfig config)
    {
        try
        {
            if (!ValidateConfig(config))
            {
                return;
            }

            await gridConfig.UpdateRow(config);
            var result = await RepoConfig.Update(
                config,
                CurrentUser.OrgId,
                CurrentUser,
                cancellationToken: _ctsOperations.Token
            );

            if (!result.Exito)
            {
                throw new Exception(result.Texto);
            }

            await RepoBitacora.AddBitacora(
                userId: CurrentUser.Id,
                desc: $"Se actualizó la configuración {config.ConfigId}",
                orgId: CurrentUser.OrgId,
                cancellationToken: _ctsBitacora.Token
            );

            await LoadData();
            isEditing = false;
        }
        catch (Exception ex)
        {
            await LogError(ex, "SaveRow");
            throw;
        }
    }

    protected void CancelEdit(WConfig config)
    {
        isEditing = false;
        Reset(config);
        gridConfig.CancelEditRow(config);
    }

    protected bool ValidateConfig(WConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.Grupo))
        {
            errorMessage = "El grupo es requerido";
            return false;
        }

        if (string.IsNullOrWhiteSpace(config.Clave))
        {
            errorMessage = "La clave es requerida";
            return false;
        }

        if (string.IsNullOrWhiteSpace(config.Titulo))
        {
            errorMessage = "El título es requerido";
            return false;
        }

        if (string.IsNullOrWhiteSpace(config.Tipo))
        {
            errorMessage = "El tipo es requerido";
            return false;
        }

        return true;
    }

    protected async Task LogError(Exception ex, string origen)
    {
        errorMessage = ex.Message;
        await RepoBitacora.AddLog(
            userId: CurrentUser.Id,
            orgId: CurrentUser.OrgId,
            desc: ex.Message,
            tipoLog: "Error",
            origen: $"ConfigListBase.{origen}",
            cancellationToken: _ctsBitacora.Token
        );
    }

    public void Dispose()
    {
        _ctsOperations.Dispose();
        _ctsBitacora.Dispose();
    }
} 