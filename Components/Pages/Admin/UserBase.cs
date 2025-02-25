using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;
using Ali25_V10.Data;
using Ali25_V10.Data.Modelos;
using Ali25_V10.Data.Sistema;
using System.Threading;
using Microsoft.AspNetCore.Identity;

namespace Ali25_V10.Components.Pages.Admin
{
    public class UserBase : ComponentBase, IDisposable
    {
        [CascadingParameter] protected ApplicationUser CurrentUser { get; set; } = default!;
        [Parameter] public string TipoOrg { get; set; } = "Vacio";
        [Parameter] public string LaOrgId { get; set; } = "Vacio";
        [Parameter] public bool ListaEdit { get; set; } = false;
        [Inject] protected IRepo<ApplicationUser> RepoUser { get; set; } = default!;
        [Inject] protected IRepoBitacora RepoBitacora { get; set; } = default!;
        [Inject] protected IRepo<W100_Org> RepoOrg { get; set; } = default!;
        [Inject] protected UserManager<ApplicationUser> UserManager { get; set; } = default!;

        protected RadzenDataGrid<ApplicationUser> gridUser = default!;
        
        protected int count;
        protected IEnumerable<ApplicationUser>? users;
        protected Dictionary<int, string> niveles = new();
        protected bool bypassCache { get; set; } = false;
        protected bool showAddDialog { get; set; } = false;

        
        protected List<ApplicationUser> usersToInsert = new();
        protected List<ApplicationUser> usersToUpdate = new();

        protected List<string> tiposOrgs = Constantes.TiposOrgs
            .Split(',')
            .Select(x => x.Trim())
            .Prepend("Todas")
            .ToList();

        protected DataGridEditMode editMode = DataGridEditMode.Single;
        protected bool isLoading = false;
        protected bool isRefreshing = false;
        protected bool isAdding = false;
        protected bool isEditing = false;

        
        protected List<W100_Org> orgsPrivadas { get; set; } = new();
        protected List<W100_Org> orgsFiltradas { get; set; } = new();
        
        protected string selectedTipo { get; set; } = "Todas";

        protected readonly CancellationTokenSource _ctsOperations = new(TimeSpan.FromSeconds(30));
        protected readonly CancellationTokenSource _ctsBitacora = new(TimeSpan.FromSeconds(5));
        protected readonly CancellationTokenSource _ctsLogs = new(TimeSpan.FromSeconds(5));

        protected string? errorMessage;
        protected List<string> MsnError { get; set; } = new List<string>();

        protected string ErroresCompleto => string.Join(" | ", MsnError);

        public void Dispose()
        {
            _ctsOperations.Dispose();
            _ctsBitacora.Dispose();
            _ctsLogs.Dispose();
        }

        protected override async Task OnInitializedAsync()
        {
            try
            {
                LaOrgId = CurrentUser.OrgId;
                
                var nivelesArray = Constantes.Niveles
                    .Split(',')
                    .Select(n => n.Trim())
                    .Where(n => !string.IsNullOrWhiteSpace(n) )
                    .ToArray();
   
                for (int i = 0; i < nivelesArray.Length; i++)
                {
                    niveles.Add(i + 1, nivelesArray[i]);
                }

                // Cargar organizaciones según nivel de usuario
                var result = await RepoOrg.Get(
                    orgId: CurrentUser.OrgId,
                    elUser: CurrentUser
                );
                
                if (result.Exito)
                {
                    orgsPrivadas = result.DataVarios?.ToList() ?? new();
                    orgsFiltradas = orgsPrivadas; // Inicializar con todas las orgs
                    if (orgsPrivadas.Any())
                    {
                        LaOrgId = orgsPrivadas.First().OrgId;
                        await LoadData(); // Cargará usuarios de la primera org
                    }
                }

                await RepoBitacora.AddBitacora(
                    userId: CurrentUser.Id,
                    desc: $"Accediendo a lista de usuarios de la organización",
                    orgId: CurrentUser.OrgId,
                    cancellationToken: _ctsBitacora.Token
                );
                
            }
            catch (Exception ex)
            {
                await LogError(ex, "OnInitializedAsync");
            }
        }

        protected async Task HandleUserCreated()
        {
            showAddDialog = false;
            await LoadData();
        }

        protected async Task LoadData()
        {
            if (isLoading) return;
            
            try
            {
                isLoading = true;
                
                if (CurrentUser != null)
                {
                    var result = await RepoUser.Get(
                        orgId: LaOrgId,
                        elUser: CurrentUser,
                        cancellationToken: _ctsOperations.Token
                    );

                    if (result.Exito)
                    {
                        users = result.DataVarios;
                        count = users?.Count() ?? 0;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                await RepoBitacora.AddLog(
                    userId: CurrentUser?.Id ?? "Sistema",
                    orgId: CurrentUser?.OrgId ?? "Sistema",
                    desc: "Operación de carga de usuarios cancelada por timeout",
                    tipoLog: "Warning",
                    origen: "UserBase.LoadData",
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

        protected async Task FiltroOrgByTipo()
        {
            try
            {
                isLoading = true;
                
                
                orgsFiltradas = selectedTipo == "Todas" 
                    ? orgsPrivadas 
                    : orgsPrivadas.Where(x => x.Tipo == selectedTipo).ToList();

                
            }
            catch (Exception ex)
            {
                await LogError(ex, "FiltroOrgByTipo");
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
        }

        protected async Task OnOrgSelected(object? value)
        {
            if (value != null)
            {
                LaOrgId = value.ToString() ?? CurrentUser.OrgId;
                await LoadData();
            }
        }

        protected async Task OnFiltroSelected(object? value)
        {
            selectedTipo = value?.ToString() ?? "Todas";
            await FiltroOrgByTipo();
        }

        protected async Task SaveRow(ApplicationUser user)
        {
            try
            {
                if (gridUser != null)
                {
                    await gridUser.UpdateRow(user);
                    usersToUpdate.Add(user);
                    var result = await RepoUser.Update(user, CurrentUser.OrgId, elUser: CurrentUser, cancellationToken: _ctsOperations.Token);
                    if (!result.Exito)
                    {
                        MsnError.Add(result.Texto);
                        errorMessage = ErroresCompleto;
                        await LogError(new Exception(errorMessage), "SaveRow");
                    }
                    await LoadData();
                }
            }
            catch (Exception ex)
            {
                MsnError.Add(ex.Message);
                errorMessage = ErroresCompleto;
                await LogError(ex, "SaveRow");
            }
        }

        protected async Task LogError(Exception ex, string origen)
        {
            await RepoBitacora.AddLog(
                userId: CurrentUser.Id,
                desc: $"Error en {origen}: {ex.Message}",
                tipoLog: "Error",
                origen: $"UserBase.{origen}",
                orgId: CurrentUser.OrgId,
                cancellationToken: _ctsLogs.Token
            );
        }
    }
} 