using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Ali25_V10.Data;
using Ali25_V10.Data.Modelos;
using Ali25_V10.Data.Sistema;
namespace Ali25_V10.Data.Modelos;
public class W100_Org
    {
        [Key]
        [StringLength(65)]
        public string OrgId { get; set; } = MyFunc.MyGuid("Org");
        [Required(ErrorMessage = "RFC es obligatorio")]
        [StringLength(15, MinimumLength = 12, ErrorMessage = "RFC debe tener 12 o 13 caracteres")]
        
        public string Rfc { get; set; } = "";
        [Required(ErrorMessage = "Nombre Comercial es obligatorio")]
        [StringLength(25, ErrorMessage = "Nombre Comercial no puede exceder 25 caracteres")]
        public string Comercial { get; set; } = "";
        [Required(ErrorMessage = "Razón Social es obligatoria")]
        [StringLength(75, ErrorMessage = "Razón Social no puede exceder 75 caracteres")]
        public string? RazonSocial { get; set; } = "";
        public bool Moral { get; set; } = true;
        [StringLength(10)]
        public string? NumCliente { get; set; } = "";
        [StringLength(15)]
        public string Tipo { get; set; } = "Cliente";
        public int Estado { get; set; } = 5;
        public bool Status { get; set; } = true;
        public string ComercialRfc => Comercial + " " + Rfc;
        public W100_Org() { }
        public W100_Org(string rfc, string comercial, string razonSocial, string tipo,
            int estado, bool status)
        {   
            Rfc = rfc; 
            Comercial = comercial; 
            RazonSocial = razonSocial; 
            Moral = rfc.Length == 12;
            Tipo = tipo;
            Estado = estado; 
            Status = status;
        }
        public virtual ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
        public void UserAdd(ApplicationUser user)
        {
            Users.Add(user);
            user.Org = this;
        }
        public virtual ICollection<Z900_Bitacora> Bitacoras { get; set; } = new List<Z900_Bitacora>();
        public void BitacoraAdd(Z900_Bitacora bita)
        {
            Bitacoras.Add(bita);
            bita.Org = this;
        }
    
    }