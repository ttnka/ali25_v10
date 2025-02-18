namespace Ali25_V10.Data.Sistema;
public class MailCampos
    {
        public List<string> ParaNombre { get; set; } = new List<string>();
        public List<string> ParaEmail { get; set; } = new List<string>();
        public string Titulo { get; set; } = "";
        public string Cuerpo { get; set; } = "";

        public string SenderName { get; set; } = "";
        public string SenderEmail { get; set; } = "";
        public string Replayto { get; set; } = "";

        public string Server { get; set; } = "";
        public int Port { get; set; }
        public string UserName { get; set; } = "";
        public string Password { get; set; } = "";

        public string UserId { get; set; } = "";
        public string OrgId { get; set; } = "";
    }