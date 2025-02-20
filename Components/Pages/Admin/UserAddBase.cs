using Microsoft.AspNetCore.Components;
using Ali25_V10.Data;
using Ali25_V10.Data.Modelos;
using Ali25_V10.Data.Sistema;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Ali25_V10.Components.Pages.Admin
{
    public class UserAddBase : ComponentBase
    {
        [CascadingParameter] protected ApplicationUser CurrentUser { get; set; } = default!;
        [Parameter] public string TipoOrg {get; set;} = "Vacio";
        [Inject] protected IRepo<ApplicationUser> RepoUser { get; set; } = default!;
        [Inject] protected UserManager<ApplicationUser> UserManager { get; set; } = default!;
        [Inject] protected IRepoBitacora RepoBitacora { get; set; } = default!;
        [Parameter] public EventCallback<ApplicationUser> OnUserCreated { get; set; }
        [Parameter] public List<W100_Org> Orgs { get; set; } = new();

        protected ApplicationUser newUser = new();
        
        protected string password = "";
        protected string confirmPassword = "";
        protected bool showPassword;
        protected bool showConfirmPassword;
        protected bool isSaving;
        protected string? errorMessage;
        protected Dictionary<int, string> niveles = new();

        protected override void OnInitialized()
        {
            // Configurar niveles desde Constantes
            var nivelesArray = Constantes.Niveles
                .Split(',')
                .Select(n => n.Trim())
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .ToArray();

            // Crear diccionario de niveles
            for (int i = 0; i < nivelesArray.Length; i++)
            {
                if (i + 1 <= CurrentUser.Nivel) // Solo mostrar niveles hasta el nivel del usuario actual
                {
                    if (TipoOrg == "Publico" && i+1 > 1) continue;
                    if (TipoOrg == "Proveedor" && i+1 > 3) continue;
                    if (i+1 >= CurrentUser.Nivel) continue; 
                    niveles.Add(i + 1, nivelesArray[i]);
                }
            }

            // Inicializar nuevo usuario
            
            newUser.Estado = 3;
            newUser.Status = true;
            newUser.EsActivo = true;
            newUser.FechaRegistro = DateTime.Now;
        }

        protected bool ValidateForm()
        {
            errorMessage = null;

            // Validar Nombre
            if (string.IsNullOrEmpty(newUser.Nombre) || newUser.Nombre.Length > 65)
            {
                errorMessage = "Nombre es obligatorio y no puede exceder 65 caracteres";
                return false;
            }

            // Validar Apellido Paterno
            if (string.IsNullOrEmpty(newUser.Paterno) || newUser.Paterno.Length > 65)
            {
                errorMessage = "Apellido Paterno es obligatorio y no puede exceder 65 caracteres";
                return false;
            }

            // Validar Apellido Materno (opcional pero con límite)
            if (newUser.Materno?.Length > 65)
            {
                errorMessage = "Apellido Materno no puede exceder 65 caracteres";
                return false;
            }

            // Validar Email
            if (string.IsNullOrEmpty(newUser.Email) || newUser.Email.Length > 256 || !newUser.Email.Contains("@"))
            {
                errorMessage = "Email no es válido o excede 256 caracteres";
                return false;
            }

            // Validar Password
            if (string.IsNullOrEmpty(password) || password.Length < 6)
            {
                errorMessage = "La contraseña debe tener al menos 6 caracteres";
                return false;
            }

            if (password != confirmPassword)
            {
                errorMessage = "Las contraseñas no coinciden";
                return false;
            }

            return true;
        }

        protected async Task SaveUser()
        {
            try
            {
                if (!ValidateForm())
                    return;

                isSaving = true;

                newUser.UserName = newUser.Email;
                password = Orgs.FirstOrDefault(x => x.OrgId == newUser.OrgId)!.Rfc.ToUpper() ?? "";
                var result = await UserManager.CreateAsync(newUser, password);

                if (!result.Succeeded)
                {
                    var errorMsg = string.Join(", ", result.Errors.Select(e => e.Description));
                    throw new Exception($"Error al crear usuario: {errorMsg}");
                }
                

                await RepoBitacora.AddBitacora(
                    userId: CurrentUser.Id,
                    desc: $"Se creó el usuario {newUser.Nombre} {newUser.Paterno}",
                    orgId: CurrentUser.OrgId
                );

                await OnUserCreated.InvokeAsync(newUser);
                ResetForm();
            }
            catch (Exception ex)
            {
                await RepoBitacora.AddLog(
                    userId: CurrentUser.Id,
                    orgId: CurrentUser.OrgId,
                    desc: $"Error al crear usuario: {ex.Message}",
                    tipoLog: "Error",
                    origen: "UserAddBase.SaveUser"
                );
                errorMessage = ex.Message;
            }
            finally
            {
                isSaving = false;
            }
        }

        protected void ResetForm()
        {
            newUser = new()
            {
                
                Estado = 5,
                Status = true,
                EsActivo = true,
                FechaRegistro = DateTime.Now
            };
            password = "";
            confirmPassword = "";
            errorMessage = null;
        }
    }
} 