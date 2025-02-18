namespace Ali25_V10.Data.Sistema;
public class Constantes
    {
        public const string ElDominio = "https://pegia.mx";
        //public const string ElDominio = "localhost:7234";
        // Mail 01
        // Este usuario envia los mails
        public const string DeNombreMail01Soporte = "Soporte WebMaster";
        public const string DeMail01Soporte = "soporte@zuverworks.com";
        public const string ServerMail01 = "mail.omnis.com";
        public const int PortMail01 = 587;
        public const string UserNameMail01 = "soporte@zuverworks.com";
        public const string PasswordMail01 = "wB.2468022";
        

        // Mail 02
        // Ese es el usuario del sistema pero no envia mails
        public const string DeNombreMail02 = "WebMaster";
        public const string DeMail02 = "webmaster@zuverworks.com";
        public const string ServerMail02 = "mail.omnis.com";
        public const int PortMail02 = 587;
        public const string UserNameMail02 = "webmaster@zuverworks.com";
        public const string PasswordMail02 = "2468022Ih.";

        // Registro Inicial Publico en GENERAL Organizacion

        public const string PgRfc = "PGE010101AAA";
        public const string PgRazonSocial = "Publico en General";
        public const int PgEstado = 3;  // En caso de no quere que se utilice poner 2
        public const bool PgStatus = true;
        public const string PgMail = "peg@pegia.mx";

        // Registro Usuario Publico en GENERAL

        public const string DeNombreMailPublico = "Publico";
        public const string DeMailPublico = "publico@pegia.mx";
        public const int EstadoPublico = 3;
        public const int NivelPublico = 1;
        public const string UserNameMailPublico = "publico@pegia.mx";
        public const string PasswordMailPublico = "PublicoLibre1.";

        // Registro de Administrador del Sistema
        public const string RfcAdmin = "ZME130621FFA";
        public const string RazonSocialAdmin = "Zuverworks de Mexico";
        public const int EstadoAdmin = 3;
        public const bool StatusAdmin = true;
        public const int NivelAdmin = 7;
        public const string AdminMail = "webmaster@zuverworks.com";
        public const string AdminPassword = "24680212Ih.";

        // Configuracion
        public const string Arranque = "2.468022";

        public const string OrgTipo = "Administracion,Gobierno,Proveedores";
        public const string Niveles = 
        "Publico, Proveedor, Cliete_user,Cliente_admin, Admin_Captura, Admin_Admin, IHC";

        public const bool EsNecesarioConfirmarMail = false;
        public const string ConfirmarMailTxt = "https://lacamara.mx/Account/ConfirmEmail/Id=";

        public const string Calendario_Periodos = "Calendario Periodos"; // Dia, Semana, Mes
        public const string Calendario_Visible = "Calendario Visible"; // Global, Organizacion
        public const string Calendario_Tipos = "Calendario Tipos"; // Auditoria, 
        public const string Calendario_Meses_Titulos = "Ene,Feb,Mar,Abr,May,Jun,Jul,Ago,Sep,Oct,Nov,Dic,Enero,Febrero,Marzo,Abril,Mayo,Junio,Julio,Agosto,Septiembre,Octubre,Noviembre,Diciembre";
            
        public const string Files_Folder = "Archivos";
        public const string PlanCtasXLS = "PlanCtas.xlsx";
        public const string ObjetosdelGastoXLS = "ObjetoGasto.csv";
        public const string RubroIngresoXLS = "RubroIngreso.xlsx";
        public const string FolderBorrarAyer = "Ayer";

        public const string Sistema_Usuario = "Sistema_User";
        public const string Sistema_Org = "Sistema_Org";
        
        
        public const string NombreEdo = "0,No hay estado,1,Aguascalientes,2,Baja California,3,Baja California Sur,4,Campeche,5,Chiapas,6,Chihuahua,7,Ciudad de México,8,Coahuila,9,Colima,10,Durango,11,Estado de México,12,Guanajuato,13,Guerrero,14,Hidalgo,15,Jalisco,16,Michoacán,17,Morelos,18,Nayarit,19,Nuevo León,20,Oaxaca,21,Puebla,22,Querétaro,23,Quintana Roo,24,San Luis Potosí,25,Sinaloa,26,Sonora,27,Tabasco,28,Tamaulipas,29,Tlaxcala,30,Veracruz,31,Yucatán,32,Zacatecas";
        public const string DetalleTipos = "Requisitos,Funciones,Horario,Tipo de contrato,Otros";
        
        
        public const string LosEjercicios = "2024,2025";

       

/*
        public const string Bancos_MovTipos = "Cheque, Deposito, Transferencia, Comisión";
        public const string Bancos_Documentos = "Poliza, Factura, Informe";
        public const string Bitacoras_Oficios = "Oficios"; // Oficios, Orden de compra
        public const string Bitacoras_Ayudas ="Ayuda Social";
        public const string Bitacoras_Recepcion="Recepcion";
        public const string Bitacoras_Gasolina="Gasolina";
        public const string Bitacoras_Permisos = "Administrador, Invidiual, Todos"; 

        public const string Conciliacion = "Conciliacion";
        public const string Contabilidad_Tipos_ctas = "Balance, Resultado, Puente, Orden,Presupuesto";
        

*/
    }