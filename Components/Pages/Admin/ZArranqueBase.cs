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
            // Bitácora: Inicio de validación
            var bitacora = new Z900_Bitacora(
                userId: Usuario,
                desc: "Iniciando proceso de ValidarArranque",
                orgId: Organizacion
            );
            await BitacoraDb.Bitacoras.AddAsync(bitacora);
            await BitacoraDb.SaveChangesAsync(_ctsOperations.Token);

            if (!Clave.Pass.Equals(Constantes.Arranque))
            {
                intentos++;
                // Bitácora: Intento fallido
                var bitacoraIntento = new Z900_Bitacora(
                    userId: Usuario,
                    desc: $"02 Intento fallido de validación #{intentos}",
                    orgId: Organizacion
                );
                await BitacoraDb.Bitacoras.AddAsync(bitacoraIntento);
                await BitacoraDb.SaveChangesAsync(_ctsOperations.Token);

                Error = true;
                ErrorMessage = $"Clave incorrecta. Intento {intentos} de 3";
                if (intentos >= 3)
                {
                    NM.NavigateTo("/");
                }
                return;
            }

            var rfcAdmin = Constantes.RfcAdmin;
            var rfcPublico = Constantes.PgRfc;

            // Actualizado: Nombre del DbSet y agregado CancellationToken
            var orgsExist = await AppDbContext.Organizaciones.AnyAsync(
                o => o.Rfc == rfcAdmin || o.Rfc == rfcPublico,
                _ctsOperations.Token
            );
            
            // Bitácora: Verificación de organizaciones
            var bitacoraResultadoOrgs = new Z900_Bitacora(
                userId: Usuario,
                desc: $"03 Verificación de organizaciones existentes: {orgsExist}",
                orgId: Organizacion
            );
            await BitacoraDb.Bitacoras.AddAsync(bitacoraResultadoOrgs);
            await BitacoraDb.SaveChangesAsync(_ctsOperations.Token);

            if (orgsExist)
            {
                Error = true;
                ErrorMessage = "El sistema ya está inicializado";
                return;
            }

            // Actualizado: Nombre del DbSet y agregado CancellationToken
            var configExist = await AppDbContext.Set<WConfig>().AnyAsync(_ctsOperations.Token);
            if (configExist)
            {
                Error = true;
                ErrorMessage = "Ya existen configuraciones en el sistema";
                return;
            }

            // Crear organización Zuver
            var orgZuver = new W100_Org
            {
                OrgId = Organizacion,
                Rfc = rfcAdmin,
                RazonSocial = "Zuver Works",
                Comercial = "Zuver",
                Estado = 5,
                Status = true,
                //Mail = "soporte@zuverworks.com"
            };

            // Bitácora: Creación Zuver
            var bitacoraZuver = new Z900_Bitacora(
                userId: Usuario,
                desc: "04 Creando organización Zuver",
                orgId: Organizacion
            );
            await BitacoraDb.Bitacoras.AddAsync(bitacoraZuver);
            await BitacoraDb.SaveChangesAsync(_ctsOperations.Token);

            // Actualizado: Nombre del DbSet y agregado CancellationToken
            await AppDbContext.Organizaciones.AddAsync(orgZuver, _ctsOperations.Token);
            await AppDbContext.SaveChangesAsync(_ctsOperations.Token);

            // Crear organización Público General
            var orgPublico = new W100_Org
            {
                OrgId = "PGE010101AAA",
                Rfc = rfcPublico,
                RazonSocial = "Público General",
                Comercial = "Público",
                Estado = 5,
                Status = true,
                //Mail = "publico@pegia.mx"
            };

            // Bitácora: Creación Público General
            var bitacoraPublico = new Z900_Bitacora(
                userId: Usuario,
                desc: "05 Creando organización Público General",
                orgId: Organizacion
            );
            await BitacoraDb.Bitacoras.AddAsync(bitacoraPublico);
            await BitacoraDb.SaveChangesAsync(_ctsOperations.Token);

            // Actualizado: Nombre del DbSet y agregado CancellationToken
            await AppDbContext.Organizaciones.AddAsync(orgPublico, _ctsOperations.Token);
            await AppDbContext.SaveChangesAsync(_ctsOperations.Token);

            Success = true;
        }
        catch (Exception ex)
        {
            // Bitácora: Error
            var bitacoraError = new Z900_Bitacora(
                userId: Usuario,
                desc: $"Error en ValidarArranque: {ex.Message}",
                orgId: Organizacion
            );
            await BitacoraDb.Bitacoras.AddAsync(bitacoraError);
            await BitacoraDb.SaveChangesAsync(_ctsOperations.Token);

            Error = true;
            ErrorMessage = ex.Message;
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
