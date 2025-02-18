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


        protected RadzenDataGrid<ApplicationUser> gridUser = default!;
        
        protected int count;
        protected IEnumerable<ApplicationUser>? users;
        protected Dictionary<int, string> niveles = new();
        protected bool bypassCache { get; set; } = false;
        protected bool showAddDialog { get; set; } = false;

        
        protected List<ApplicationUser> usersToInsert = new();
        protected List<ApplicationUser> usersToUpdate = new();

        protected DataGridEditMode editMode = DataGridEditMode.Single;
        protected bool isLoading = false;
        protected bool isRefreshing = false;
        protected bool isAdding = false;
        protected bool isEditing = false;

        private readonly CancellationTokenSource _ctsOperations = new(TimeSpan.FromSeconds(30));
        private readonly CancellationTokenSource _ctsBitacora = new(TimeSpan.FromSeconds(5));

        public void Dispose()
        {
            _ctsOperations.Dispose();
            _ctsBitacora.Dispose();
        }

        protected override async Task OnInitializedAsync()
        {
            try 
            {
                LaOrgId = CurrentUser.OrgId;
                
                var nivelesArray = Constantes.Niveles
                    .Split(',')
                    .Select(n => n.Trim())
                    .Where(n => !string.IsNullOrWhiteSpace(n)                    )
                    .ToArray();

        // Crear diccionario temporal
                
                for (int i = 0; i < nivelesArray.Length; i++)
                {
                    niveles.Add(i + 1, nivelesArray[i]);
                }

        
                await RepoBitacora.AddBitacora(
                    userId: CurrentUser.Id,
                    desc: $"Accediendo a lista de usuarios de la organización",
                    orgId: CurrentUser.OrgId,
                    cancellationToken: _ctsBitacora.Token
                );
                await RepoBitacora.AddBitacora(userId: "Segunda", "vacio", "segunda");
                
                await LoadData();
            }
            catch (Exception ex)
            {
                await RepoBitacora.AddLog(
                    userId: CurrentUser?.Id ?? "Sistema",
                    orgId: CurrentUser?.OrgId ?? "Sistema",
                    desc: $"Error al inicializar lista de usuarios: {ex.Message}",
                    tipoLog: "Error",
                    origen: $"UserBase.OnInitializedAsync",
                    cancellationToken: _ctsBitacora.Token
                );
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
                        filtro: x => x.OrgId == LaOrgId && x.Status == true,
                        elUser: CurrentUser,
                        byPassCache: bypassCache,
                        cancellationToken: _ctsOperations.Token
                    );

                    if (result.DataVarios != null && result.DataVarios.Any())
                    {
                        users = result.DataVarios.ToList();
                        count = result.DataVarios.Count;
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
                await RepoBitacora.AddLog(
                    userId: CurrentUser?.Id ?? "Sistema",
                    orgId: CurrentUser?.OrgId ?? "Sistema",
                    desc: $"Error al cargar usuarios: {ex.Message}",
                    tipoLog: "Error",
                    origen: "UserBase.LoadData",
                    cancellationToken: _ctsBitacora.Token
                );
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
                await RepoBitacora.AddLog(
                    userId: CurrentUser?.Id ?? "Sistema",
                    orgId: CurrentUser?.OrgId ?? "Sistema",
                    desc: $"Error al refrescar usuarios: {ex.Message}",
                    tipoLog: "Error",
                    origen: $"UserBase.RefreshData"
                );
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
        
        
    }
} 