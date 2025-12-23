namespace NAVIGEST.macOS.Models
{
    public class BillInfoModel
    {
        public string InvoiceNo { get; set; } = string.Empty;
        public DateTime BillingDate { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public decimal SubTotal { get; set; }
        public decimal TaxPercentage { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal GrandTotal { get; set; }
        public decimal TotalPayment { get; set; }
        public decimal PaymentDue { get; set; }
        public string? CostumerNO { get; set; }
        public string OrderNo { get; set; } = string.Empty;
        public string? ESTADO { get; set; }
        public string? FATURA { get; set; }
        public string? RECIBO { get; set; }
        public float? PERCIVA { get; set; }
        public string? CENTROCUSTO { get; set; }
        public int ID { get; set; }
    }
}
