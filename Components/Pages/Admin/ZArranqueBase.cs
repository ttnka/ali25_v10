using Microsoft.AspNetCore.Components;  // Para ComponentBase, NavigationManager, Inject
using Microsoft.AspNetCore.Identity;    // Para UserManager
using Microsoft.EntityFrameworkCore;    // Para EF Core
using Microsoft.AspNetCore.Components.Web;  // Para KeyboardEventArgs
using Ali25_V10.Data;                  // Para ApplicationUser, IRepo
using Ali25_V10.Data.Modelos;          // Para W100_Org
using Ali25_V10.Data.Sistema;          // Para Constantes, ApiRespAll, ZConfig
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.Components.Routing;

namespace Ali25_V10.Components.Pages.Admin;

public class ZArranqueBase : ComponentBase
{
    private const string TBita = "Arranque";
    protected string Organizacion = Constantes.Sistema_Org;
    protected string Usuario = Constantes.Sistema_Usuario;

    private ApplicationUser SystemUser => new ApplicationUser 
    { 
        Id = Usuario,
        UserName = Usuario,
        OrgId = Organizacion
    };

    [Inject] protected IRepo<W100_Org> OrgsRepo { get; set; } = default!;
    [Inject] protected IRepo<ApplicationUser> UserRepo { get; set; } = default!;
    [Inject] protected IRepo<ZConfig> ConfigRepo { get; set; } = default!;
    [Inject] protected UserManager<ApplicationUser> UserManager { get; set; } = default!;
    [Inject] protected NavigationManager NM { get; set; } = default!;
    [Inject] protected IRepoBitacora RepoBitacora { get; set; } = default!;

    protected bool Loading { get; set; }
    protected bool Error { get; set; }
    protected bool Success { get; set; }
    protected string ErrorMessage { get; set; } = string.Empty;
    protected LaClave Clave { get; set; } = new();
    protected int intentos = 0;
    protected bool isLoading;  // Verificar que use isLoading

    protected async Task Enter(KeyboardEventArgs e)
    {
        if (e.Code == "Enter" || e.Code == "NumpadEnter")
        {
            await ValidarArranque();
        }
    }

    protected async Task ValidarArranque()
    {
        try
        {
            await RepoBitacora.AddLog(
                userId: Usuario,
                orgId: Organizacion,
                desc: "01 Iniciando proceso de ValidarArranque",
                tipoLog: "Debug",
                origen: "ZArranqueBase.ValidarArranque"
            );

            Loading = true;
            Error = false;

            if (!CorrectPass(Clave.Pass))
            {
                intentos++;
                await RepoBitacora.AddLog(
                    userId: Usuario,
                    orgId: Organizacion,
                    desc: $"02 Intento fallido de validación #{intentos}",
                    tipoLog: "Warning",
                    origen: "ZArranqueBase.ValidarArranque"
                );

                if (intentos > 2)
                {
                    await RepoBitacora.AddLog(
                        userId: Usuario,
                        orgId: Organizacion,
                        desc: "03 Máximo de intentos alcanzado, redirigiendo a inicio",
                        tipoLog: "Warning",
                        origen: "ZArranqueBase.ValidarArranque"
                    );
                    NM.NavigateTo("/", true);
                    return;
                }
                Error = true;
                ErrorMessage = "Contraseña incorrecta";
                return;
            }

            await RepoBitacora.AddLog(
                userId: Usuario,
                orgId: Organizacion,
                desc: "04 Contraseña validada correctamente, procediendo con RunInicio",
                tipoLog: "Debug",
                origen: "ZArranqueBase.ValidarArranque"
            );

            await RunInicio();
        }
        catch (Exception ex)
        {
            await RepoBitacora.AddLog(
                userId: Usuario,
                orgId: Organizacion,
                desc: $"05 Error en ValidarArranque: {ex.Message}\nStackTrace: {ex.StackTrace}",
                tipoLog: "Error",
                origen: "ZArranqueBase.ValidarArranque"
            );
            Error = true;
            ErrorMessage = $"Error en la validación: {ex.Message}";
        }
        finally
        {
            Loading = false;
            await RepoBitacora.AddLog(
                userId: Usuario,
                orgId: Organizacion,
                desc: "06 Finalizando proceso de ValidarArranque",
                tipoLog: "Debug",
                origen: "ZArranqueBase.ValidarArranque"
            );
        }
    }

    protected bool CorrectPass(string? pass)
    {
        return pass == Constantes.Arranque;
    }

    protected async Task RunInicio()
    {
        try
        {
            await RepoBitacora.AddLog(
                userId: Usuario,
                orgId: Organizacion,
                desc: "07 Iniciando RunInicio",
                tipoLog: "Debug",
                origen: "ZArranqueBase.RunInicio"
            );

            // Validar si ya existe alguna organización por RFC
            var rfcAdmin = Constantes.RfcAdmin;
            var rfcPublico = Constantes.PgRfc;

            await RepoBitacora.AddLog(
                userId: Usuario,
                orgId: Organizacion,
                desc: $"08 Verificando organizaciones existentes con RFC: {rfcAdmin} o {rfcPublico}",
                tipoLog: "Debug",
                origen: "ZArranqueBase.RunInicio"
            );

            var orgsExist = await OrgsRepo.Get(Organizacion, elUser: SystemUser);
            
            await RepoBitacora.AddLog(
                userId: Usuario,
                orgId: Organizacion,
                desc: $"09 Resultado de búsqueda de organizaciones - Éxito: {orgsExist.Exito}, Cantidad: {orgsExist.DataVarios?.Count ?? 0}",
                tipoLog: "Debug",
                origen: "ZArranqueBase.RunInicio"
            );

            if (orgsExist.DataVarios?.Any(o => o.Rfc == rfcAdmin || o.Rfc == rfcPublico) == true)
            {
                Error = true;
                ErrorMessage = $"El sistema ya ha sido inicializado (RFC existente). Debug: Exito={orgsExist.Exito}, " +
                    $"Registros={orgsExist.DataVarios?.Count ?? 0}, Texto={orgsExist.Texto}, " +
                    $"Errores={string.Join(", ", orgsExist.MsnError)}";
                await RepoBitacora.AddLog(
                    userId: Usuario,
                    orgId: Organizacion,
                    desc: $"{TBita}, Intento de inicialización con RFC duplicado",
                    tipoLog: "Error",
                    origen: "ZArranqueBase.RunInicio"
                );
                return;
            }

            // Validar si ya existe algún usuario
            var usersExist = await UserManager.Users.AnyAsync();
            if (usersExist)
            {
                Error = true;
                ErrorMessage = "El sistema ya ha sido inicializado (Usuarios existentes)";
                return;
            }

            // Validar si ya existen configuraciones
            var configExist = await ConfigRepo.Get(Organizacion, elUser: SystemUser);
            if (configExist.Exito && configExist.DataVarios.Any())
            {
                Error = true;
                ErrorMessage = "El sistema ya ha sido inicializado (Configuraciones existentes)";
                return;
            }

            // 1. Crear Organización Admin
            var orgZuver = new W100_Org(
                rfc: Constantes.RfcAdmin,
                comercial: "Zuverworks",
                razonSocial: Constantes.RazonSocialAdmin,
                tipo: "Admin",
                estado: Constantes.EstadoAdmin,
                status: Constantes.StatusAdmin
            );

            var respZuver = await OrgsRepo.Insert(
                orgZuver, 
                orgZuver.OrgId,
                elUser: new ApplicationUser 
                { 
                    Id = Constantes.Sistema_Usuario,
                    OrgId = Constantes.Sistema_Org 
                }
            );

            if (respZuver.Exito)
            {
                await RepoBitacora.AddBitacora(
                    userId: Usuario,
                    desc: $"{TBita}, Se creó la organización administrativa {orgZuver.RazonSocial} con RFC {orgZuver.Rfc}",
                    orgId: Organizacion
                );

                // 2. Crear Usuario Admin
                var adminUser = new ApplicationUser
                {
                    UserName = Constantes.AdminMail,
                    Email = Constantes.AdminMail,
                    Nombre = "Administrador",
                    Paterno = "Sistema",
                    Materno = "",
                    FechaNacimiento = DateTime.Now,
                    EsActivo = true,
                    Estado = Constantes.EstadoAdmin,
                    OrgId = orgZuver.OrgId,
                    Nivel = Constantes.NivelAdmin,
                    Status = true
                    
                };

                var resultAdmin = await UserManager.CreateAsync(adminUser, Constantes.AdminPassword);
                if (!resultAdmin.Succeeded)
                {
                    throw new Exception($"Error al crear usuario admin: {string.Join(", ", resultAdmin.Errors.Select(e => e.Description))}");
                }

                await RepoBitacora.AddBitacora(
                    userId: Usuario,
                    desc: $"{TBita}, Se creó el usuario administrador {adminUser.Email}",
                    orgId: Organizacion
                );

                // 3. Crear Org Público
                var orgPublico = new W100_Org(
                    rfc: Constantes.PgRfc,
                    comercial: "Público",
                    razonSocial: Constantes.PgRazonSocial,
                    tipo: "Publico",
                    estado: Constantes.PgEstado,
                    status: Constantes.PgStatus
                );

                //var respPublico = await OrgsRepo.Insert(orgPublico, Organizacion);
                //if (!respPublico.Exito)
                //{
                //    throw new Exception("Error al crear organización pública");
                //}

                await RepoBitacora.AddBitacora(
                    userId: Usuario,
                    desc: $"{TBita}, Se creó la organización pública {orgPublico.RazonSocial} con RFC {orgPublico.Rfc}",
                    orgId: Organizacion
                );

                // 4. Crear Usuario Público
                var userPublico = new ApplicationUser
                {
                    UserName = Constantes.DeMailPublico,
                    Email = Constantes.DeMailPublico,
                    Nombre = "Usuario",
                    Paterno = "Público",
                    Materno = "",
                    FechaNacimiento = DateTime.Now,
                    EsActivo = true,
                    Estado = Constantes.EstadoPublico,
                    OrgId = orgPublico.OrgId,
                    Nivel = 1,
                    Status = true   
                };

                var resultPublico = await UserManager.CreateAsync(userPublico, Constantes.PasswordMailPublico);
                if (!resultPublico.Succeeded)
                {
                    throw new Exception($"Error al crear usuario público: {string.Join(", ", resultPublico.Errors.Select(e => e.Description))}");
                }

                await RepoBitacora.AddBitacora(
                    userId: Usuario,
                    desc: $"{TBita}, Se creó el usuario público {userPublico.Email}",
                    orgId: Organizacion
                );

                Success = true;

                // 5. Crear configuraciones del sistema
                var configResp = await LasVariables();
                if (!configResp.Exito)
                {
                    throw new Exception("Error al crear configuraciones del sistema");
                }

                NM.NavigateTo("/", true);
            }
            else
            {
                throw new Exception("Error al crear organización administrativa");
            }
        }
        catch (Exception ex)
        {
            Error = true;
            ErrorMessage = ex.Message;
            await RepoBitacora.AddLog(
                userId: Usuario,
                orgId: Organizacion,
                desc: $"{TBita}, Error en inicialización: {ex.Message}",
                tipoLog: "Error",
                origen: "ZArranqueBase.RunInicio"
            );
        }
    }

    protected async Task<ApiRespAll<ZConfig>> LasVariables()
    {
        ApiRespAll<ZConfig> respuesta = new() { Exito = false, Varios = true };
        try
        {
            var grupos = new List<ZConfig>();

            // Calendario Periodos
            var calPeriodos = new ZConfig(
                tipoGrupo: true,
                grupo: "Grupos",
                numeroId: 0,
                textoId: Constantes.Calendario_Periodos,
                titulo: Constantes.Calendario_Periodos,
                desc: "Variables de periodos del calendario",
                configuracion: "TT",
                global: true,
                orgId: Usuario,
                estado: 2
            );
            grupos.Add(calPeriodos);
            await PoblarElementos(grupos, Constantes.Calendario_Periodos, 
                "Dia,Semana,Quincena,Mes,Bimestre,Trimestre,Cuatrimestre,Semestre");

            // Calendario Visibilidad
            var calVisible = new ZConfig(
                tipoGrupo: true,
                grupo: "Grupos",
                numeroId: 0,
                textoId: Constantes.Calendario_Visible,
                titulo: Constantes.Calendario_Visible,
                desc: "Configuración de visibilidad",
                configuracion: "TT",
                global: true,
                orgId: Usuario,
                estado: 2
            );
            grupos.Add(calVisible);
            await PoblarElementos(grupos, Constantes.Calendario_Visible,
                "Privado,Departamento,Organizacion,Global");

            // Tipos de Calendario
            var calTipos = new ZConfig(
                tipoGrupo: true,
                grupo: "Grupos",
                numeroId: 0,
                textoId: Constantes.Calendario_Tipos,
                titulo: Constantes.Calendario_Tipos,
                desc: "Tipos de eventos del calendario",
                configuracion: "TT",
                global: true,
                orgId: Usuario,
                estado: 2
            );
            grupos.Add(calTipos);
            await PoblarElementos(grupos, Constantes.Calendario_Tipos,
                "Todas,Actividad,Auditoria,Cumpleaños,Festivo,Reporte,Otros");

            //var resp = await ConfigRepo.InsertPlus(grupos, Usuario);
            //if (resp.Exito)
            //{
            //    respuesta.Exito = true;
            //    respuesta.DataVarios = resp.DataVarios;
            //    await RepoBitacora.AddBitacora(
            //        userId: Usuario,
            //        orgId: Organizacion,
            //        desc: $"{TBita}, Se crearon las configuraciones del sistema"
            //    );
            //}
        }
        catch (Exception ex)
        {
            await RepoBitacora.AddLog(
                userId: Usuario,
                orgId: Organizacion,
                desc: $"{TBita}, Error al crear configuraciones: {ex.Message}",
                tipoLog: "Error",
                origen: "ZArranqueBase.LasVariables"
            );
        }
        return respuesta;
    }

    private async Task<ApiRespAll<ZConfig>> PoblarElementos(
        List<ZConfig> lista, string titulo, string variables)
    {
        var respuesta = new ApiRespAll<ZConfig> { Exito = false, Varios = true };
        var elementos = variables.Split(',');
        var numero = 0;

        foreach (var elemento in elementos)
        {
            var nuevoElemento = new ZConfig(
                tipoGrupo: false,
                grupo: titulo,
                numeroId: numero++,
                textoId: elemento,
                titulo: elemento,
                desc: "Elemento de configuración",
                configuracion: "TT",
                global: true,
                orgId: Usuario,
                estado: 2
            );
            lista.Add(nuevoElemento);
        }
        respuesta.Exito = true;
        return respuesta;
    }
    public class LaClave
{
    public string Pass { get; set; } = string.Empty;
}     
}
