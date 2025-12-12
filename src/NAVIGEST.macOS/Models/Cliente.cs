using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NAVIGEST.macOS.Models
{
    public class Cliente : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string? _clinome;
        public string? CLINOME 
        { 
            get => _clinome; 
            set { _clinome = value; OnPropertyChanged(); } 
        }

        private string? _clicodigo;
        public string? CLICODIGO 
        { 
            get => _clicodigo; 
            set { _clicodigo = value; OnPropertyChanged(); } 
        }

        private string? _telefone;
        public string? TELEFONE 
        { 
            get => _telefone; 
            set { _telefone = value; OnPropertyChanged(); } 
        }

        private string? _indicativo;
        public string? INDICATIVO 
        { 
            get => _indicativo; 
            set { _indicativo = value; OnPropertyChanged(); } 
        }

        private string? _email;
        public string? EMAIL 
        { 
            get => _email; 
            set { _email = value; OnPropertyChanged(); } 
        }

        private bool _externo;
        public bool EXTERNO 
        { 
            get => _externo; 
            set { _externo = value; OnPropertyChanged(); } 
        }

        private bool _anulado;
        public bool ANULADO 
        { 
            get => _anulado; 
            set { _anulado = value; OnPropertyChanged(); } 
        }

        private string? _vendedor;
        public string? VENDEDOR 
        { 
            get => _vendedor; 
            set { _vendedor = value; OnPropertyChanged(); } 
        }

        private string? _valorcredito;
        public string? VALORCREDITO 
        { 
            get => _valorcredito; 
            set { _valorcredito = value; OnPropertyChanged(); } 
        }

        private bool _pastasSincronizadas;
        public bool PastasSincronizadas 
        { 
            get => _pastasSincronizadas; 
            set { _pastasSincronizadas = value; OnPropertyChanged(); } 
        }

        private int _servicesCount;
        public int ServicesCount 
        { 
            get => _servicesCount; 
            set { _servicesCount = value; OnPropertyChanged(); } 
        }

        private decimal _vendasAnoAtual;
        public decimal VendasAnoAtual
        {
            get => _vendasAnoAtual;
            set { _vendasAnoAtual = value; OnPropertyChanged(); }
        }

        private decimal _vendasAnoAnterior;
        public decimal VendasAnoAnterior
        {
            get => _vendasAnoAnterior;
            set { _vendasAnoAnterior = value; OnPropertyChanged(); }
        }

        public bool CanSeeFinancials
        {
            get
            {
                var user = UserSession.Current.User;
                if (user.IsAdmin || user.IsFinancial) return true;

                if (string.Equals(user.Role, "VENDEDOR", StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrWhiteSpace(VENDEDOR) && 
                        string.Equals(VENDEDOR.Trim(), user.Name.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public Cliente Clone() => new()
        {
            CLINOME = CLINOME,
            CLICODIGO = CLICODIGO,
            TELEFONE = TELEFONE,
            INDICATIVO = INDICATIVO,
            EMAIL = EMAIL,
            EXTERNO = EXTERNO,
            ANULADO = ANULADO,
            VENDEDOR = VENDEDOR,
            VALORCREDITO = VALORCREDITO,
            PastasSincronizadas = PastasSincronizadas,
            ServicesCount = ServicesCount,
            VendasAnoAtual = VendasAnoAtual,
            VendasAnoAnterior = VendasAnoAnterior
        };
    }
}

