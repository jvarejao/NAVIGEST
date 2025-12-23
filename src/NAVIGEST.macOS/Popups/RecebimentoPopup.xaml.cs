using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls;
using NAVIGEST.macOS.Models;
using NAVIGEST.macOS.PageModels;
using NAVIGEST.macOS.Services;

#if MACCATALYST
using UIKit;
#endif

namespace NAVIGEST.macOS.Popups;

public partial class RecebimentoPopup : Popup
{
    private readonly OrderInfoModel _order;
    private bool _suppressAmountChange;

    public ObservableCollection<BillInfoModel> Receipts { get; } = new();

    public RecebimentoPopup(OrderInfoModel order)
    {
        InitializeComponent();
        _order = order;
        BindingContext = this;
        Opened += OnPopupOpened;
    }

    private async void OnPopupOpened(object? sender, EventArgs e)
    {
        try
        {
            await LoadReceiptsAsync();
            PrefillFields();
        }
        catch (Exception ex)
        {
            NAVIGEST.macOS.GlobalErro.TratarErro(ex);
        }
    }

    private async Task LoadReceiptsAsync()
    {
        try
        {
            var list = await DatabaseService.GetBillInfoByOrderAsync(_order.OrderNo);
            Receipts.Clear();
            foreach (var r in list) Receipts.Add(r);
        }
        catch (Exception ex)
        {
            NAVIGEST.macOS.GlobalErro.TratarErro(ex);
        }
    }

    private void PrefillFields()
    {
        var currentYear = DateTime.Now.Year;

        if (!string.IsNullOrWhiteSpace(_order.Numfatura))
        {
            InvoiceEntry.Text = FormatDocumentNumber(_order.Numfatura, currentYear);
        }
        else
        {
            InvoiceEntry.Text = FormatDocumentNumber(InvoiceEntry.Text, currentYear);
        }

        if (!string.IsNullOrWhiteSpace(_order.Numrecibo))
        {
            ReceiptEntry.Text = FormatDocumentNumber(_order.Numrecibo, currentYear);
        }
        else
        {
            ReceiptEntry.Text = FormatDocumentNumber(ReceiptEntry.Text, currentYear);
        }

        var pendingAmount = GetPendingAmount();
        _suppressAmountChange = true;
        AmountEntry.Text = FormatAmountText(pendingAmount.ToString(CultureInfo.InvariantCulture));
        _suppressAmountChange = false;
    }

    private decimal GetPendingAmount()
    {
        var pending = _order.VALORPENDENTE ?? Math.Max(0m, (_order.TotalAmount ?? 0m) - (_order.VALORPAGO ?? 0m));
        return Math.Max(0m, pending);
    }

    private void OnAmountFocused(object sender, FocusEventArgs e)
    {
        try
        {
            if (sender is Entry entry)
            {
                var len = entry.Text?.Length ?? 0;
                Dispatcher.Dispatch(() =>
                {
                    try { entry.CursorPosition = 0; entry.SelectionLength = len; } catch { }
                });
            }
        }
        catch (Exception ex) { NAVIGEST.macOS.GlobalErro.TratarErro(ex, mostrarAlerta: false); }
    }

    private void OnAmountUnfocused(object sender, FocusEventArgs e)
    {
        try
        {
            if (sender is not Entry entry) return;
            var formatted = FormatAmountText(entry.Text);
            _suppressAmountChange = true;
            entry.Text = formatted;
        }
        catch (Exception ex) { NAVIGEST.macOS.GlobalErro.TratarErro(ex, mostrarAlerta: false); }
        finally { _suppressAmountChange = false; }
    }

    private void OnAmountChanged(object? sender, TextChangedEventArgs e)
    {
        if (_suppressAmountChange) return;
        if (sender is not Entry entry || !entry.IsFocused) return;

        try
        {
            var txt = e.NewTextValue ?? string.Empty;
            var filtered = new string(txt.Where((c, idx) => char.IsDigit(c) || (c == '-' && idx == 0) || c == ',' || c == '.').ToArray());

            int firstDec = filtered.IndexOfAny(new[] { ',', '.' });
            if (firstDec >= 0)
            {
                var tail = filtered[(firstDec + 1)..].Replace(".", string.Empty).Replace(",", string.Empty);
                filtered = filtered.Substring(0, firstDec + 1) + tail;
                filtered = filtered.Replace('.', ',');
            }

            if (filtered != txt)
            {
                _suppressAmountChange = true;
                var caret = filtered.Length;
                entry.Text = filtered;
                entry.CursorPosition = caret;
            }
        }
        catch (Exception ex) { NAVIGEST.macOS.GlobalErro.TratarErro(ex, mostrarAlerta: false); }
        finally { _suppressAmountChange = false; }
    }

    private decimal ParseAmount(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0m;
        var cleaned = text
            .Replace("€", string.Empty)
            .Replace(" ", string.Empty)
            .Replace("\u00A0", string.Empty)
            .Replace('.', ',');
        return decimal.TryParse(cleaned, NumberStyles.Any, new CultureInfo("pt-PT"), out var value) ? value : 0m;
    }

    private string FormatAmountText(string? text)
    {
        var amount = ParseAmount(text);
        return $"{amount.ToString("N2", new CultureInfo("pt-PT"))}€";
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        var currentYear = DateTime.Now.Year;
        var nextReceiptSeq = GetNextReceiptSequence();

        var receiptNumber = FormatReceiptNumber(null, currentYear, nextReceiptSeq); // auto número de recebimento
        var invoiceDoc = FormatDocumentNumber(InvoiceEntry.Text, currentYear);      // fatura manual
        var receiptDoc = FormatDocumentNumber(ReceiptEntry.Text, currentYear);      // recibo manual

        InvoiceEntry.Text = invoiceDoc;
        ReceiptEntry.Text = receiptDoc;

        var amount = ParseAmount(AmountEntry.Text);
        if (amount <= 0)
        {
            var page = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (page != null)
            {
                await page.DisplayAlert("Recebimento", "Introduza um valor válido.", "OK");
            }
            return;
        }

        try
        {
            var (paid, pending) = await DatabaseService.AddReceiptAsync(_order, receiptNumber, invoiceDoc, receiptDoc, amount);
            _order.VALORPAGO = paid;
            _order.VALORPENDENTE = pending;
            if (!string.IsNullOrWhiteSpace(invoiceDoc)) _order.Numfatura = invoiceDoc;
            if (!string.IsNullOrWhiteSpace(receiptDoc)) _order.Numrecibo = receiptDoc;

            await LoadReceiptsAsync();
            ServicePageModel.LastInstance?.LoadAsync(force: true);
            await AppShell.DisplayToastAsync("Recebimento registado", NAVIGEST.macOS.ToastTipo.Sucesso, 2000);
        }
        catch (Exception ex)
        {
            NAVIGEST.macOS.GlobalErro.TratarErro(ex);
            await AppShell.DisplayToastAsync("Erro ao registar recebimento", NAVIGEST.macOS.ToastTipo.Erro, 2500);
        }
    }

    private void OnCancelClicked(object sender, EventArgs e) => Close();

    private void OnCloseClicked(object sender, EventArgs e) => Close();

    private void OnInvoiceUnfocused(object sender, FocusEventArgs e)
    {
        try
        {
            InvoiceEntry.Text = FormatDocumentNumber(InvoiceEntry.Text);
        }
        catch (Exception ex) { NAVIGEST.macOS.GlobalErro.TratarErro(ex, mostrarAlerta: false); }
    }

    private void OnReceiptUnfocused(object sender, FocusEventArgs e)
    {
        try
        {
            ReceiptEntry.Text = FormatDocumentNumber(ReceiptEntry.Text);
        }
        catch (Exception ex) { NAVIGEST.macOS.GlobalErro.TratarErro(ex, mostrarAlerta: false); }
    }

    private string FormatDocumentNumber(string? raw, int? yearOverride = null)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;

        var currentYear = yearOverride ?? DateTime.Now.Year;
        var sanitized = raw.Replace(" ", string.Empty);
        var numberPart = sanitized.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? sanitized;
        var digits = new string(numberPart.Where(char.IsDigit).ToArray());
        if (string.IsNullOrWhiteSpace(digits)) return string.Empty;

        return $"{digits}/{currentYear}";
    }

    private string FormatReceiptNumber(string? raw, int? yearOverride = null, int? fallbackSequence = null)
    {
        var currentYear = yearOverride ?? DateTime.Now.Year;
        var sanitized = (raw ?? string.Empty).Replace(" ", string.Empty);
        var numberPart = sanitized.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? sanitized;
        numberPart = numberPart.Replace("RE", string.Empty, StringComparison.OrdinalIgnoreCase);
        var digits = new string(numberPart.Where(char.IsDigit).ToArray());

        int seq;
        if (!string.IsNullOrWhiteSpace(digits) && int.TryParse(digits, out var parsed))
        {
            seq = parsed;
        }
        else if (fallbackSequence.HasValue)
        {
            seq = fallbackSequence.Value;
        }
        else
        {
            return string.Empty;
        }

        var padded = seq.ToString("000");
        return $"RE{padded}/{currentYear}";
    }

    private int GetNextReceiptSequence()
    {
        var maxSeq = Receipts
            .Select(r => ParseReceiptSequence(r.InvoiceNo))
            .Where(n => n.HasValue)
            .Select(n => n!.Value)
            .DefaultIfEmpty(0)
            .Max();

        return maxSeq + 1;
    }

    private int? ParseReceiptSequence(string? receipt)
    {
        if (string.IsNullOrWhiteSpace(receipt)) return null;

        var sanitized = receipt.Replace(" ", string.Empty);
        var numberPart = sanitized.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? sanitized;
        numberPart = numberPart.Replace("RE", string.Empty, StringComparison.OrdinalIgnoreCase);
        var digits = new string(numberPart.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out var n) ? n : null;
    }

    private async void OnPrintClicked(object sender, EventArgs e)
    {
        if ((sender as Button)?.CommandParameter is not BillInfoModel bill) return;
#if MACCATALYST
        try
        {
            await PrintReceiptAsync(bill);
        }
        catch (Exception ex)
        {
            NAVIGEST.macOS.GlobalErro.TratarErro(ex);
            await AppShell.DisplayToastAsync("Erro ao imprimir recibo", NAVIGEST.macOS.ToastTipo.Erro, 2500);
        }
#else
        await AppShell.DisplayToastAsync("Impressão disponível apenas no macOS.", NAVIGEST.macOS.ToastTipo.Info, 2000);
#endif
    }

#if MACCATALYST
    private Task PrintReceiptAsync(BillInfoModel bill)
    {
        var html = BuildReceiptHtml(bill);
        var formatter = new UIMarkupTextPrintFormatter(html)
        {
            StartPage = 0
        };

        var printInfo = UIPrintInfo.PrintInfo;
        printInfo.OutputType = UIPrintInfoOutputType.General;
        printInfo.JobName = $"Recibo_{bill.InvoiceNo}";

        var controller = UIPrintInteractionController.SharedPrintController;
        controller.PrintFormatter = formatter;
        controller.PrintInfo = printInfo;

        controller.Present(true, null);
        return Task.CompletedTask;
    }

    private string BuildReceiptHtml(BillInfoModel bill)
    {
        var sb = new StringBuilder();
        sb.Append("<html><body style='font-family:-apple-system; font-size:14px;'>");
        sb.Append($"<h2>Recibo {bill.RECIBO ?? bill.InvoiceNo}</h2>");
        sb.Append($"<p><strong>Serviço:</strong> {bill.OrderNo}</p>");
        sb.Append($"<p><strong>Cliente:</strong> {bill.CustomerName}</p>");
        sb.Append($"<p><strong>Data:</strong> {bill.BillingDate:dd/MM/yyyy}</p>");
        sb.Append($"<p><strong>Valor Recebido:</strong> {bill.TotalPayment:N2} €</p>");
        sb.Append($"<p><strong>Pendente:</strong> {bill.PaymentDue:N2} €</p>");
        if (!string.IsNullOrWhiteSpace(bill.FATURA)) sb.Append($"<p><strong>Fatura:</strong> {bill.FATURA}</p>");
        if (!string.IsNullOrWhiteSpace(bill.RECIBO)) sb.Append($"<p><strong>Recibo:</strong> {bill.RECIBO}</p>");
        sb.Append("</body></html>");
        return sb.ToString();
    }
#endif
}
