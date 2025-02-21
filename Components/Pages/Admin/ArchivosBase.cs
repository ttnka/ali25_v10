using Ali25_V10.Data;
using Ali25_V10.Data.Modelos;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Ali25_V10.Data.Sistema;
using Radzen;
using Radzen.Blazor;
using System.IO;
using Microsoft.AspNetCore.Components.Forms;

namespace Ali25_V10.Components.Pages.Admin;

public class ArchivosBase : ComponentBase, IDisposable
{
    [CascadingParameter] protected ApplicationUser CurrentUser { get; set; } = default!;
    [Inject] protected IRepo<W180_Files> RepoFiles { get; set; } = default!;
    [Inject] protected IRepoBitacora RepoBitacora { get; set; } = default!;
    [Inject] protected IConfiguration Configuration { get; set; } = default!;

    protected RadzenDataGrid<W180_Files> gridFiles = default!;
    protected IEnumerable<W180_Files>? files;
    protected bool isLoading;
    protected string? errorMessage;
    protected int count;
    protected string baseArchivosPath = "Componentes/Archivos";
    protected long maxFileSize = 10 * 1024 * 1024; // 10MB por defecto
    
    private readonly CancellationTokenSource _ctsOperations = new(TimeSpan.FromSeconds(30));

    protected override async Task OnInitializedAsync()
    {
        try
        {
            // Configurar ruta base y tamaño máximo desde appsettings
            baseArchivosPath = Configuration["Archivos:BasePath"] ?? baseArchivosPath;
            maxFileSize = Configuration.GetValue<long>("Archivos:MaxFileSize", maxFileSize);

            await LoadData();
        }
        catch (Exception ex)
        {
            await LogError(ex, "OnInitializedAsync");
        }
    }

    protected async Task LoadData()
    {
        try
        {
            isLoading = true;
            var result = await RepoFiles.Get(
                orgId: CurrentUser.OrgId,
                elUser: CurrentUser,
                cancellationToken: _ctsOperations.Token
            );

            if (result.Exito)
            {
                files = result.DataVarios;
                count = files?.Count() ?? 0;
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

    protected async Task OnFileUpload(InputFileChangeEventArgs e)
    {
        try
        {
            var file = e.File;
            if (file.Size > maxFileSize)
            {
                throw new Exception($"El archivo excede el tamaño máximo permitido ({maxFileSize / 1024 / 1024}MB)");
            }

            // Crear estructura de carpetas
            var year = DateTime.Now.Year.ToString();
            var month = $"{DateTime.Now.Month:00}_{DateTime.Now.ToString("MMMM")}";
            var orgFolder = CurrentUser.Org?.Rfc ?? CurrentUser.OrgId;
            
            var relativePath = Path.Combine(year, orgFolder, month);
            var fullPath = Path.Combine(baseArchivosPath, relativePath);
            
            Directory.CreateDirectory(fullPath);

            // Generar nombre único
            var fileName = $"{DateTime.Now:yyyyMMddHHmmss}_{file.Name}";
            var filePath = Path.Combine(fullPath, fileName);

            // Guardar archivo
            await using var stream = file.OpenReadStream(maxFileSize);
            await using var fileStream = File.Create(filePath);
            await stream.CopyToAsync(fileStream);

            // Registrar en base de datos
            var fileRecord = new W180_Files
            {
                Fecha = DateTime.Now,
                OrgId = CurrentUser.OrgId,
                Fuente = "Upload",
                FuenteId = CurrentUser.Id,
                Tipo = file.ContentType,
                Archivo = file.Name,
                Ruta = Path.Combine(relativePath, fileName),
                Estado = 5,
                Status = true
            };

            var result = await RepoFiles.Insert(fileRecord, CurrentUser.OrgId, CurrentUser, _ctsOperations.Token);
            if (!result.Exito)
            {
                throw new Exception(result.Texto);
            }

            await RepoBitacora.AddBitacora(
                userId: CurrentUser.Id,
                desc: $"Archivo subido: {file.Name}",
                orgId: CurrentUser.OrgId,
                cancellationToken: _ctsOperations.Token
            );

            await LoadData();
        }
        catch (Exception ex)
        {
            await LogError(ex, "OnFileUpload");
            throw;
        }
    }

    protected async Task DeleteFile(W180_Files file)
    {
        try
        {
            // Eliminar archivo físico
            var fullPath = Path.Combine(baseArchivosPath, file.Ruta);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }

            // Eliminar registro
            await RepoFiles.DeleteEntity(file, CurrentUser);
            await LoadData();

            await RepoBitacora.AddBitacora(
                userId: CurrentUser.Id,
                desc: $"Archivo eliminado: {file.Archivo}",
                orgId: CurrentUser.OrgId,
                cancellationToken: _ctsOperations.Token
            );
        }
        catch (Exception ex)
        {
            await LogError(ex, "DeleteFile");
            throw;
        }
    }

    protected async Task LogError(Exception ex, string origen)
    {
        errorMessage = ex.Message;
        await RepoBitacora.AddLog(
            desc: $"Error en {origen}: {ex.Message}",
            tipoLog: "Error",
            origen: $"ArchivosBase.{origen}",
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