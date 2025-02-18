using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ali25_V10.Data.Sistema;
public class ApiRespAll<TEntity> where TEntity : class
    {
        public bool Exito { get; set; }
        public bool Varios { get; set; }
        //public bool FinArchivo { get; set; } = false;
        public List<string> MsnError { get; set; } = new List<string>();
        public TEntity DataUno { get; set; }
        public List<TEntity> DataVarios { get; set; }
        public string Texto { get; set; }
        
    }