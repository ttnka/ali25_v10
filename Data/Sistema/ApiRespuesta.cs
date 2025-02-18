using System.Collections.Generic;

namespace Ali25_V10.Data.Sistema;
public class ApiRespuesta<TEntity> where TEntity : class
    {
        public bool Exito { get; set; }
        public List<string> MsnError { get; set; } = new List<string>();
        public TEntity? Data { get; set; }
        public string? Texto { get; set; }

    }