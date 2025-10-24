// File: AppLoginMaui/Models/OrderInfoModel.cs
#nullable enable
namespace AppLoginMaui.Models
{
    public class OrderInfoModel
    {
        public string OrderNo { get; set; } = "";
        public DateTime? OrderDate { get; set; }
        public string? OrderStatus { get; set; }
        public string CustomerNo { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public decimal? SubTotal { get; set; }
        public decimal? TaxPercentage { get; set; }
        public decimal? TaxAmount { get; set; }
        public decimal? TotalAmount { get; set; }
        public DateTime? OrderDateEnt { get; set; }
        public decimal? Desconto { get; set; }
        public decimal? DescPercentage { get; set; }
        public string? Observacoes { get; set; }
        public string? Utilizador { get; set; }
        public string? ANO { get; set; }
        public bool ControlAbert { get; set; }
        public string? CONTROLVEND { get; set; }
        public string? DESCRICAOCC { get; set; }
        public decimal? VALORPENDENTE { get; set; }
        public string? Numfatura { get; set; }
        public string? Numrecibo { get; set; }
        public decimal? VALORPAGO { get; set; }
        public decimal? TotalAmountCusto { get; set; }
        public decimal? MargemLucro { get; set; }
        public decimal? MargemPercentual { get; set; }
        public string? Numserv { get; set; }
        public string? Numext1 { get; set; }
        public string? Numext2 { get; set; }
        public bool? Encomenda { get; set; }
        public string? Servencomenda { get; set; }
        public bool? EMAILENC { get; set; }
        public bool? EMAILPROD { get; set; }
        public bool? EMAILPRODEXT { get; set; }
        public bool? EMAILFINAL { get; set; }
        public bool? EMAILLIQUID { get; set; }
        public string? PEQDESCSERVICO { get; set; }
        public string? DESCPROD { get; set; }
        public string? TEMPO { get; set; }
    }
}



