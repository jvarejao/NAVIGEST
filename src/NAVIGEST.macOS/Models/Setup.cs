namespace NAVIGEST.macOS.Models
{
    public class Setup
    {
        public string CodEmp { get; set; } = string.Empty;
        public string? Empresa { get; set; }
        public string? CaminhoServidor { get; set; }
        public string? CaminhoServidor2 { get; set; }
        
        // Credenciais Servidor (Opcional)
        public string? ServerUser { get; set; }
        public string? ServerPassword { get; set; }
        
        // Pastas Servidor 1
        public string? SERV1PASTA1 { get; set; }
        public string? SERV1PASTA2 { get; set; }
        public string? SERV1PASTA3 { get; set; }
        public string? SERV1PASTA4 { get; set; }
        public string? SERV1PASTA5 { get; set; }
        public string? SERV1PASTA6 { get; set; }
        public string? SERV1PASTA7 { get; set; }
        public string? SERV1PASTA8 { get; set; }

        // Pastas Servidor 2
        public string? SERV2PASTA1 { get; set; }
        public string? SERV2PASTA2 { get; set; }

        public List<string> GetSubfolders()
        {
            var list = new List<string>();
            if (!string.IsNullOrWhiteSpace(SERV1PASTA1)) list.Add(SERV1PASTA1);
            if (!string.IsNullOrWhiteSpace(SERV1PASTA2)) list.Add(SERV1PASTA2);
            if (!string.IsNullOrWhiteSpace(SERV1PASTA3)) list.Add(SERV1PASTA3);
            if (!string.IsNullOrWhiteSpace(SERV1PASTA4)) list.Add(SERV1PASTA4);
            if (!string.IsNullOrWhiteSpace(SERV1PASTA5)) list.Add(SERV1PASTA5);
            if (!string.IsNullOrWhiteSpace(SERV1PASTA6)) list.Add(SERV1PASTA6);
            if (!string.IsNullOrWhiteSpace(SERV1PASTA7)) list.Add(SERV1PASTA7);
            if (!string.IsNullOrWhiteSpace(SERV1PASTA8)) list.Add(SERV1PASTA8);
            // Adicionar SERV2 se necessário, mas o user falou em subpastas da pasta mãe
            return list;
        }
    }
}
