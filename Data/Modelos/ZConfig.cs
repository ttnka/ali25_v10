using System;
using System.ComponentModel.DataAnnotations;
using Ali25_V10.Data.Sistema;

namespace Ali25_V10.Data.Modelos;
public class ZConfig
{
    [Key]
    [StringLength(65)]
    public string ConfigId { get; set; } = MyFunc.MyGuid("ZConfig");
    public bool TipoGrupo { get; set; } = false; // Grupo - Elemento
    [StringLength(25)]
    public string Grupo { get; set; } = string.Empty;
    public int NumeroId { get; set; } = 0;
    [StringLength(50)]
    public string TextoId { get; set; } = string.Empty;
    [StringLength(25)]
    public string Titulo { get; set; } = string.Empty;
    [StringLength(1000)]
    public string Descripcion { get; set; } = string.Empty;
    [StringLength(2)]
    public string Configuracion { get; set; } = string.Empty;
    public bool Global { get; set; } = false;
    [StringLength(65)]
    public string OrgId {get; set; } = string.Empty;
    public int Estado {get; set; } = 5;
    public bool Status {get; set; } = true;

    public ZConfig() {}

    public ZConfig(bool tipoGrupo, string grupo, int numeroId, string textoId, string titulo, 
        string desc, string configuracion, bool global, string orgId, int estado)
    {
            TipoGrupo = tipoGrupo;
            Grupo = grupo.Length > 25 ? grupo.Substring(0, 25) : grupo;
            NumeroId = numeroId;
            TextoId = textoId.Length > 65 ? textoId.Substring(0, 65) : textoId;
            Titulo = titulo.Length > 25 ? titulo.Substring(0, 25) : titulo;
            Descripcion = desc.Length > 1000 ? desc.Substring(0, 1000) : desc;
            Configuracion = configuracion.Length > 2 ? configuracion.Substring(0, 2) : configuracion;
            Global = global;
            OrgId = orgId;
            Estado = estado;
    }


}