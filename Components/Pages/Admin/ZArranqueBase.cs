using Microsoft.AspNetCore.Components;  // Para ComponentBase, NavigationManager, Inject
using Microsoft.AspNetCore.Identity;    // Para UserManager
using Microsoft.EntityFrameworkCore;    // Para EF Core
using Microsoft.AspNetCore.Components.Web;  // Para KeyboardEventArgs
using Ali25_V10.Data;                  // Para ApplicationUser, IRepo
using Ali25_V10.Data.Modelos;          // Para W100_Org
using Ali25_V10.Data.Sistema;          // Para Constantes, ApiRespAll, WConfig
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.Components.Routing;
using System.Threading;

namespace Ali25_V10.Components.Pages.Admin;

public class ZArranqueBase : ComponentBase, IDisposable
{
    private const string TBita = "Arranque";
    protected string Organizacion = Constantes.Sistema_Org;
    protected string Usuario = Constantes.Sistema_Usuario;
    private readonly CancellationTokenSource _ctsOperations = new(TimeSpan.FromSeconds(30));

    private ApplicationUser SystemUser => new() 
    { 
        Id = Usuario,
        UserName = Usuario,
        OrgId = Organizacion
    };

    [Inject] protected ApplicationDbContext AppDbContext { get; set; } = default!;
    [Inject] protected BitacoraDbContext BitacoraDb { get; set; } = default!;
    [Inject] protected UserManager<ApplicationUser> UserManager { get; set; } = default!;
    [Inject] protected NavigationManager NM { get; set; } = default!;

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
            if (!Clave.Pass.Equals(Constantes.Arranque))
            {
                intentos++;
                Error = true;
                ErrorMessage = $"Clave incorrecta. Intento {intentos} de 3";
                if (intentos >= 3)
                {
                    NM.NavigateTo("/");
                }
                return;
            }

            isLoading = true;

            // 1. Validar que no existan organizaciones
            var orgsExist = await AppDbContext.Organizaciones.AnyAsync(
                o => o.Rfc == Constantes.RfcAdmin || o.Rfc == Constantes.PgRfc,
                _ctsOperations.Token
            );
            
            if (orgsExist)
            {
                Error = true;
                ErrorMessage = "El sistema ya está inicializado";
                return;
            }

            // 2. Crear organizaciones
            var orgZuver = new W100_Org(
                rfc: Constantes.RfcAdmin,
                comercial: "Zuver",
                razonSocial: Constantes.RazonSocialAdmin,
                tipo: "Admin",
                estado: Constantes.EstadoAdmin,
                status: Constantes.StatusAdmin
            );

            var orgPublico = new W100_Org(
                rfc: Constantes.PgRfc,
                comercial: "Público General",
                razonSocial: Constantes.PgRazonSocial,
                tipo: "Publico",
                estado: Constantes.PgEstado,
                status: Constantes.PgStatus
            );

            await AppDbContext.Organizaciones.AddAsync(orgZuver, _ctsOperations.Token);
            await AppDbContext.Organizaciones.AddAsync(orgPublico, _ctsOperations.Token);
            await AppDbContext.SaveChangesAsync(_ctsOperations.Token);

            // 3. Crear usuarios administradores
            var adminUser = new ApplicationUser
            {
                Id = MyFunc.MyGuid("User"),
                UserName = Constantes.AdminMail,
                Email = Constantes.AdminMail,
                Nombre = "Administrador",
                Paterno = "Zuverworks",
                Materno = "Sistema",
                Nivel = 7,
                OrgId = orgZuver.OrgId
            };

            var publicUser = new ApplicationUser
            {
                Id = MyFunc.MyGuid("User"),
                UserName = Constantes.UserNameMailPublico,
                Email = Constantes.DeMailPublico,
                Nombre = "Usuario",
                Paterno = "Público",
                Nivel = 1,
                OrgId = orgPublico.OrgId
            };

            var resultAdmin = await UserManager.CreateAsync(adminUser, Constantes.PasswordMail01); 
            var resultPublic = await UserManager.CreateAsync(publicUser, Constantes.PasswordMail02);

            if (!resultAdmin.Succeeded || !resultPublic.Succeeded)
            {
                throw new Exception("Error al crear usuarios administradores");
            }

            // 4. Crear configuraciones base
            var configResult = await LasVariables();
            if (!configResult.Exito)
            {
                throw new Exception("Error al crear configuraciones base");
            }

            // Guardar las configuraciones en la base de datos
            await AppDbContext.Set<WConfig>().AddRangeAsync(configResult.DataVarios, _ctsOperations.Token);
            await AppDbContext.SaveChangesAsync(_ctsOperations.Token);

            // 5. Registrar en bitácora
            var bitacora = new Z900_Bitacora(
                userId: Usuario,
                desc: "Sistema inicializado correctamente",
                orgId: Organizacion
            );
            await BitacoraDb.Bitacoras.AddAsync(bitacora, _ctsOperations.Token);
            await BitacoraDb.SaveChangesAsync(_ctsOperations.Token);

            // 6. Registrar en log
            var log = new Z910_Log(
                userId: Usuario,
                orgId: Organizacion,
                desc: "Sistema inicializado correctamente",
                tipoLog: "Info",
                origen: "ZArranque.ValidarArranque"
            );
            await BitacoraDb.Set<Z910_Log>().AddAsync(log, _ctsOperations.Token);
            await BitacoraDb.SaveChangesAsync(_ctsOperations.Token);

            Success = true;
        }
        catch (Exception ex)
        {
            Error = true;
            ErrorMessage = ex.Message;
            
            // Registrar error en bitácora
            var bitacoraError = new Z900_Bitacora(
                userId: Usuario,
                desc: $"Error al inicializar sistema: {ex.Message}",
                orgId: Organizacion
            );
            await BitacoraDb.Bitacoras.AddAsync(bitacoraError, _ctsOperations.Token);

            // Registrar error en log
            var logError = new Z910_Log(
                userId: Usuario,
                orgId: Organizacion,
                desc: $"Error al inicializar sistema: {ex.Message}\nStackTrace: {ex.StackTrace}",
                tipoLog: "Error",
                origen: "ZArranque.ValidarArranque"
            );
            await BitacoraDb.Set<Z910_Log>().AddAsync(logError, _ctsOperations.Token);
            
            await BitacoraDb.SaveChangesAsync(_ctsOperations.Token);
        }
        finally
        {
            isLoading = false;
        }
    }

    protected async Task<ApiRespAll<WConfig>> LasVariables()
    {
        ApiRespAll<WConfig> respuesta = new() { Exito = false, Varios = true };
        try
        {
            var grupos = new List<WConfig>();

            // Calendario Periodos
            var calPeriodos = new WConfig(
                
                grupo: "Grupos",
                clave: Constantes.Calendario_Periodos,
                titulo: Constantes.Calendario_Periodos,
                descripcion: "Variables de periodos del calendario",
                tipo: "TT", 
                numero: 0,  
                texto: Constantes.Calendario_Periodos,
                fecha: DateTime.Now,
                esGrupo: true,
                global: true,
                orgId: Usuario,
                estado: 2
            );
            grupos.Add(calPeriodos);
            await PoblarElementos(grupos, Constantes.Calendario_Periodos, 
                "Dia,Semana,Quincena,Mes,Bimestre,Trimestre,Cuatrimestre,Semestre");

            // Calendario Visibilidad
            var calVisible = new WConfig(
                
                grupo: "Grupos",
                clave: Constantes.Calendario_Visible,
                titulo: Constantes.Calendario_Visible,
                descripcion: "Configuración de visibilidad",
                tipo: "TT",
                esGrupo: true,
                numero: 0,
                texto: Constantes.Calendario_Visible,
                fecha: DateTime.Now,
                global: true,
                orgId: Usuario,
                estado: 2
            );
            grupos.Add(calVisible);
            await PoblarElementos(grupos, Constantes.Calendario_Visible,
                "Privado,Departamento,Organizacion,Global");

            // Tipos de Calendario
            var calTipos = new WConfig(
                
                grupo: "Grupos",
                clave: Constantes.Calendario_Tipos,
                titulo: Constantes.Calendario_Tipos,
                descripcion: "Tipos de eventos del calendario",
                tipo: "TT",
                esGrupo: true,
                numero: 0,
                texto: Constantes.Calendario_Tipos,
                fecha: DateTime.Now,
                global: true,
                orgId: Usuario,
                estado: 2
            );
            grupos.Add(calTipos);
            await PoblarElementos(grupos, Constantes.Calendario_Tipos,
                "Todas,Actividad,Auditoria,Cumpleaños,Festivo,Reporte,Otros");

            respuesta.Exito = true;
            respuesta.DataVarios = grupos;
            return respuesta;
        }
        catch (Exception ex)
        {
            var bitacoraError = new Z900_Bitacora(
                userId: Usuario,
                desc: $"{TBita}, Error al crear configuraciones: {ex.Message}",
                orgId: Organizacion
            );
            await BitacoraDb.Bitacoras.AddAsync(bitacoraError);
            await BitacoraDb.SaveChangesAsync(_ctsOperations.Token);
        }
        return respuesta;
    }

    private async Task<ApiRespAll<WConfig>> PoblarElementos(
        List<WConfig> lista, string titulo, string variables)
    {
        var respuesta = new ApiRespAll<WConfig> { Exito = false, Varios = true };
        try
        {
           
            var elementos = variables.Split(',');
           //var numero = 0;

            foreach (var elemento in elementos)
            {
                var nuevoElemento = new WConfig(
                   
                    grupo: titulo,
                    clave: elemento,
                    titulo: elemento,
                    descripcion: "Elemento de configuración",
                    tipo: "TT",
                    esGrupo: false,
                    numero: 0,
                    texto: elemento,
                    fecha: DateTime.Now,
                    global: true,
                    orgId: Usuario,
                    estado: 2
                );
                lista.Add(nuevoElemento);
            }
            respuesta.Exito = true;
            return respuesta;
        }
        catch (Exception ex)
        {
            var bitacoraError = new Z900_Bitacora(
                userId: Usuario,
                desc: $"{TBita}, Error al poblar elementos: {ex.Message}",
                orgId: Organizacion
            );
            await BitacoraDb.Bitacoras.AddAsync(bitacoraError);
            await BitacoraDb.SaveChangesAsync(_ctsOperations.Token);
        }
        return respuesta;
    }

    public void Dispose()
    {
        _ctsOperations.Dispose();
    }

    public class LaClave
    {
        public string Pass { get; set; } = string.Empty;
    }
}
