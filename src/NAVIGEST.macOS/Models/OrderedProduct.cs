using System;

namespace NAVIGEST.macOS.Models
{
    public class OrderedProduct
    {
        public long Id { get; set; }
        public string OrderNo { get; set; } = string.Empty;
        public string? Numserv { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Cor { get; set; } = string.Empty;
        public string Tam { get; set; } = string.Empty;
        public decimal Quantidade { get; set; }
        public decimal Altura { get; set; }
        public decimal Largura { get; set; }
        public decimal M2 { get; set; }
        public decimal PrecoUnit { get; set; }
        public decimal SUBTOTAIS { get; set; }
        public decimal PrecoCusto { get; set; }
        public decimal SubtotalCusto { get; set; }
        public DateTime? DATACOMPRA { get; set; }
        public string? SubTotal { get; set; }
        public decimal? SUBTOTALNUM { get; set; }
    }
}
