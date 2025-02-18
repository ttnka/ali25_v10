using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Ali25_V10.Data.Sistema;

namespace Ali25_V10.Data.Sistema;
public class EmailCuerpo 
    {
        [Key]
        [StringLength(65)]
        public string EmailId { get; set; } = MyFunc.MyGuid("EmailCuerpo");
        public bool Global { get; set; } = false;
        [StringLength(65)]
        public string OrgId { get; set; } = "";

        [StringLength(25)]
        public string Grupo { get; set; } = "";
        [StringLength(50)]
        public string Nombre { get; set; } = "";
        [StringLength(250)]
        public string TituloEmail { get; set; } = "";
        public string CuerpoEmail { get; set; } = "";
        public int Estado {get; set; } = 3;
        public bool Status {get; set; } = true;

    }