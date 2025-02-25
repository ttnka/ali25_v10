using Ali25_V10.Data;
using Ali25_V10.Data.Modelos;
using Ali25_V10.Data.Sistema;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Radzen;
using Radzen.Blazor;
using System.Threading;

namespace Ali25_V10.Components.Pages.Admin;

public class ArchivosBase : ComponentBase, IDisposable
{
    [CascadingParameter] protected ApplicationUser CurrentUser { get; set; } = default!;
    [Inject] protected IRepo<W180_Files> RepoFiles { get; set; } = default!;
    [Inject] protected IRepoBitacora RepoBitacora { get; set; } = default!;
    [Inject] protected IConfiguration Configuration { get; set; } = default!;

    protected RadzenDataGrid<W180_Files> gridFiles = default!;
    protected IEnumerable<W180_Files>? archivos;
    protected int count;
    protected bool isLoading;
    protected bool isRefreshing;
    protected bool bypassCache;
    protected string? errorMessage;
    protected string baseArchivosPath = "Componentes/Archivos";
    protected long maxFileSize = 10 * 1024 * 1024; // 10MB por defecto

    protected List<W180_Files> filesToInsert = new();
    protected List<W180_Files> filesToUpdate = new();
    protected DataGridEditMode editMode = DataGridEditMode.Single;

    protected readonly CancellationTokenSource _ctsOperations = new(TimeSpan.FromSeconds(30));
    protected readonly CancellationTokenSource _ctsBitacora = new(TimeSpan.FromSeconds(5));

    protected override async Task OnInitializedAsync()
    {
        try
        {
            baseArchivosPath = Configuration["Archivos:BasePath"] ?? baseArchivosPath;
            maxFileSize = Configuration.GetValue<long>("Archivos:MaxFileSize", maxFileSize);

            await LoadData();
            await RepoBitacora.AddBitacora(
                userId: CurrentUser.Id,
                desc: "Accediendo a lista de archivos",
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
            
            var result = await RepoFiles.Get(
                orgId: CurrentUser.OrgId,
                elUser: CurrentUser,
                byPassCache: bypassCache,
                cancellationToken: _ctsOperations.Token
            );

            if (result.Exito)
            {
                archivos = result.DataVarios;
                count = archivos?.Count() ?? 0;
            }
        }
        catch (OperationCanceledException)
        {
            await RepoBitacora.AddLog(
                userId: CurrentUser?.Id ?? "Sistema",
                orgId: CurrentUser?.OrgId ?? "Sistema",
                desc: "Operación de carga de archivos cancelada por timeout",
                tipoLog: "Warning",
                origen: "ArchivosBase.LoadData",
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

    protected void Reset(W180_Files file)
    {
        filesToInsert.Remove(file);
        filesToUpdate.Remove(file);
    }

    protected void Reset()
    {
        filesToInsert.Clear();
        filesToUpdate.Clear();
    }

    protected async Task EditRow(W180_Files file)
    {
        if (gridFiles?.EditMode == DataGridEditMode.Single)
        {
            Reset();
        }
        filesToUpdate.Add(file);
        await gridFiles.EditRow(file);
    }

    protected async Task SaveRow(W180_Files file)
    {
        try
        {
            await gridFiles.UpdateRow(file);
            var result = await RepoFiles.Update(
                file, 
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
                desc: $"Se actualizó el archivo {file.Archivo}",
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

    protected void CancelEdit(W180_Files file)
    {
        Reset(file);
        gridFiles.CancelEditRow(file);
    }

    protected async Task DeleteRow(W180_Files file)
    {
        try 
        {
            Reset(file);
            if (archivos?.Contains(file) == true)
            {
                file.Status = false;
                var result = await RepoFiles.Update(file, CurrentUser.OrgId, elUser: CurrentUser, cancellationToken: _ctsOperations.Token);
                if (!result.Exito)
                {
                    throw new Exception(result.Texto);
                }
                await gridFiles.Reload();

                await RepoBitacora.AddBitacora(
                    userId: CurrentUser.Id,
                    desc: $"Se eliminó el archivo {file.Archivo}",
                    orgId: CurrentUser.OrgId,
                    cancellationToken: _ctsBitacora.Token
                );
            }
            else
            {
                gridFiles.CancelEditRow(file);
                await gridFiles.Reload();
            }
        }
        catch (Exception ex)
        {
            await LogError(ex, "DeleteRow");
        }
    }

    protected async Task OnFileUpload(InputFileChangeEventArgs e)
    {
        try
        {
            var file = e.File;
            if (file == null)
            {
                throw new Exception("No se seleccionó ningún archivo");
            }

            if (file.Size > maxFileSize)
            {
                throw new Exception($"El archivo excede el tamaño máximo permitido de {maxFileSize / (1024 * 1024)}MB");
            }

            var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx", ".xls", ".xlsx" };
            var extension = Path.GetExtension(file.Name).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                throw new Exception("Tipo de archivo no permitido");
            }

            var newFile = new W180_Files
            {
                Archivo = file.Name,
                Tipo = file.ContentType,
                Fuente = "Upload",
                Estado = 5,
                Status = true
            };

            var result = await RepoFiles.Insert(
                newFile,
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
                desc: $"Se subió el archivo {file.Name}",
                orgId: CurrentUser.OrgId,
                cancellationToken: _ctsBitacora.Token
            );

            await LoadData();
        }
        catch (Exception ex)
        {
            await LogError(ex, "OnFileUpload");
        }
    }

    protected async Task LogError(Exception ex, string origen)
    {
        errorMessage = ex.Message;
        await RepoBitacora.AddLog(
            userId: CurrentUser.Id,
            desc: $"Error en {origen}: {ex.Message}",
            tipoLog: "Error",
            origen: $"ArchivosBase.{origen}",
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