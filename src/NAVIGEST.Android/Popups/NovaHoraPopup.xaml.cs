using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls;
using NAVIGEST.Android.Models;
using NAVIGEST.Android.Services;

namespace NAVIGEST.Android.Popups;

public partial class NovaHoraPopup : Popup
{
    private HoraColaborador _hora;
    private List<Colaborador> _colaboradores;
    private List<Cliente> _clientes = new();
    private bool _isEdit;
    private Cliente? _clienteSelecionado;
    private Colaborador? _colaboradorSelecionado;

    public NovaHoraPopup(HoraColaborador hora, List<Colaborador> colaboradores)
    {
        InitializeComponent();
        
        _hora = hora;
        _colaboradores = colaboradores;
        _isEdit = hora.Id > 0;

        _ = InicializarFormAsync();
    }

    private async Task InicializarFormAsync()
    {
        // Configurar título
        TituloLabel.Text = _isEdit ? "✏️ EDITAR REGISTO DE HORAS" : "➕ NOVO REGISTO DE HORAS";
        EliminarButton.IsVisible = _isEdit;

        // Carregar clientes da BD
        try
        {
            var clientesDb = await DatabaseService.GetClientesAsync(null);
            _clientes = clientesDb.OrderBy(c => c.CLINOME).ToList();
            
            // Adicionar opção "Sem cliente" no início
            _clientes.Insert(0, new Cliente { CLICODIGO = "0", CLINOME = "Sem cliente" });
        }
        catch (Exception ex)
        {
            GlobalErro.TratarErro(ex, mostrarAlerta: false);
        }

        // Se for edição, selecionar colaborador atual
        if (_isEdit && _hora.IdColaborador > 0)
        {
            var colab = _colaboradores.FirstOrDefault(c => c.ID == _hora.IdColaborador);
            if (colab != null)
            {
                _colaboradorSelecionado = colab;
                ColaboradorLabel.Text = colab.Nome;
            }
            
            // Selecionar cliente também (se existir)
            if (!string.IsNullOrEmpty(_hora.IdCliente))
            {
                var idClienteTrim = _hora.IdCliente.Trim();
                var clienteSelecionado = _clientes.FirstOrDefault(c => c.CLICODIGO?.Trim() == idClienteTrim);
                
                if (clienteSelecionado != null)
                {
                    _clienteSelecionado = clienteSelecionado;
                    ClienteLabel.Text = clienteSelecionado.CLINOME;
                }
                else
                {
                    // Cliente não existe mais na BD - selecionar "Sem cliente"
                    _clienteSelecionado = _clientes[0];
                    ClienteLabel.Text = "Sem cliente";
                    await Shell.Current.DisplayAlert("⚠️ Aviso", 
                        $"O cliente '{idClienteTrim}' já não existe na base de dados.\n\n" +
                        $"Este registo tinha o cliente: {_hora.Cliente ?? "N/A"}", 
                        "OK");
                }
            }
            else
            {
                _clienteSelecionado = _clientes[0];
                ClienteLabel.Text = "Sem cliente";
            }
        }
        else
        {
            // Novo registo - sem cliente por padrão
            _clienteSelecionado = _clientes[0];
            ClienteLabel.Text = "Sem cliente";
            ColaboradorLabel.Text = "Selecione colaborador";
        }

        // Preencher campos
        DataPicker.Date = _hora.DataTrabalho;
        HorasNormaisEntry.Text = _hora.HorasTrab.ToString("0.00");
        HorasExtrasEntry.Text = _hora.HorasExtras.ToString("0.00");
        ObservacoesEditor.Text = _hora.Observacoes;

        // Forçar cálculo do total após preencher os campos - usar MainThread para garantir UI atualizada
        MainThread.BeginInvokeOnMainThread(() =>
        {
            CalcularTotal();
        });
    }

    private void OnHorasChanged(object sender, TextChangedEventArgs e)
    {
        CalcularTotal();
    }

    private void CalcularTotal()
    {
        float.TryParse(HorasNormaisEntry.Text?.Replace(",", "."), out float horasNormais);
        float.TryParse(HorasExtrasEntry.Text?.Replace(",", "."), out float horasExtras);
        
        if (horasNormais < 0) horasNormais = 0;
        if (horasExtras < 0) horasExtras = 0;
        
        float total = horasNormais + horasExtras;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            TotalHorasLabel.Text = $"{total:0.00}h";
            TotalHorasLabel.TextColor = Colors.White;
            TotalHorasLabel.FontAttributes = FontAttributes.Bold;
        });
    }

    private async void OnGuardarClicked(object sender, EventArgs e)
    {
        try
        {
            await Shell.Current.DisplayAlert("DEBUG", "Botão GUARDAR clicado!", "OK");
            
            // Validar colaborador
            if (_colaboradorSelecionado == null)
            {
                await Shell.Current.DisplayAlert("DEBUG", "FALHOU: Colaborador não selecionado", "OK");
                MostrarErro("Selecione um colaborador");
                return;
            }
            
            var colab = _colaboradorSelecionado;
            await Shell.Current.DisplayAlert("DEBUG", $"Colaborador OK: {colab.Nome} (ID={colab.ID})", "OK");

            // Validar campos de horas - aceitar vazio como 0
            string horasNormaisText = string.IsNullOrWhiteSpace(HorasNormaisEntry.Text) ? "0" : HorasNormaisEntry.Text.Replace(",", ".");
            string horasExtrasText = string.IsNullOrWhiteSpace(HorasExtrasEntry.Text) ? "0" : HorasExtrasEntry.Text.Replace(",", ".");
            
            await Shell.Current.DisplayAlert("DEBUG", $"Horas: N={horasNormaisText}, E={horasExtrasText}", "OK");
            
            if (!float.TryParse(horasNormaisText, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float horasNormais) || horasNormais < 0)
            {
                await Shell.Current.DisplayAlert("DEBUG", "FALHOU: Parse horas normais", "OK");
                MostrarErro("Insira horas normais válidas (0-24)");
                return;
            }

            if (!float.TryParse(horasExtrasText, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float horasExtras) || horasExtras < 0)
            {
                await Shell.Current.DisplayAlert("DEBUG", "FALHOU: Parse horas extras", "OK");
                MostrarErro("Insira horas extras válidas (0-24)");
                return;
            }

            if (horasNormais + horasExtras > 24)
            {
                await Shell.Current.DisplayAlert("DEBUG", "FALHOU: Total > 24h", "OK");
                MostrarErro("Total de horas não pode exceder 24h");
                return;
            }
            
            if (horasNormais + horasExtras == 0)
            {
                await Shell.Current.DisplayAlert("DEBUG", "FALHOU: Total = 0", "OK");
                MostrarErro("Insira pelo menos 1 hora");
                return;
            }

            await Shell.Current.DisplayAlert("DEBUG", "Validações OK! Vai gravar...", "OK");
            SetBusy(true);
            EsconderErro();

            // Preencher objeto - Colaborador
            _hora.DataTrabalho = DataPicker.Date;
            _hora.IdColaborador = colab.ID;
            _hora.NomeColaborador = colab.Nome;
            
            // Preencher objeto - Cliente
            if (_clienteSelecionado != null && _clienteSelecionado.CLICODIGO != "0")
            {
                _hora.IdCliente = _clienteSelecionado.CLICODIGO;
                _hora.Cliente = _clienteSelecionado.CLINOME;
            }
            else
            {
                _hora.IdCliente = null;
                _hora.Cliente = null;
            }
            
            _hora.Observacoes = ObservacoesEditor.Text?.Trim();

            // Guardar horas normais e extras diretamente
            _hora.HorasTrab = horasNormais;
            _hora.HorasExtras = horasExtras;

            // Gravar na BD
            await Shell.Current.DisplayAlert("DEBUG", "Chamando UpsertHoraColaboradorAsync...", "OK");
            
            int id;
            try
            {
                id = await DatabaseService.UpsertHoraColaboradorAsync(_hora);
            }
            catch (Exception exDb)
            {
                await Shell.Current.DisplayAlert("ERRO NA BD", $"Exception: {exDb.Message}\n\n{exDb.InnerException?.Message}", "OK");
                GlobalErro.TratarErro(exDb, mostrarAlerta: true);
                throw;
            }
            
            _hora.Id = id;
            await Shell.Current.DisplayAlert("DEBUG", $"Gravado! ID={id}. Vai fechar popup...", "OK");

            // Fechar popup com sucesso
            await Shell.Current.DisplayAlert("DEBUG", $"Chamando Close() com objeto ID={_hora.Id}", "OK");
            Close(_hora);
            await Shell.Current.DisplayAlert("DEBUG", "DEPOIS de Close() - NÃO DEVIA APARECER!", "OK");
        }
        catch (Exception ex)
        {
            GlobalErro.TratarErro(ex, mostrarAlerta: false);
            MostrarErro("Erro ao guardar registo");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void OnEliminarClicked(object sender, EventArgs e)
    {
        try
        {
            bool confirmacao = await Shell.Current.DisplayAlert(
                "Confirmar",
                "Eliminar este registo de horas?",
                "Sim",
                "Não"
            );

            if (!confirmacao) return;

            SetBusy(true);

            await DatabaseService.DeleteHoraColaboradorAsync(_hora.Id);

            // Fechar popup indicando eliminação (Id negativo)
            _hora.Id = -1;
            Close(_hora);
        }
        catch (Exception ex)
        {
            GlobalErro.TratarErro(ex, mostrarAlerta: false);
            MostrarErro("Erro ao eliminar registo");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void OnCancelarClicked(object sender, EventArgs e)
    {
        Close(null);
    }

    private async void OnPesquisarClienteClicked(object sender, EventArgs e)
    {
        try
        {
            var popup = new ClienteSearchPopup(_clientes);
            var page = Application.Current?.Windows[0]?.Page;
            if (page != null)
            {
                var result = await page.ShowPopupAsync(popup);
                if (result is Cliente clienteSelecionado)
                {
                    _clienteSelecionado = clienteSelecionado;
                    ClienteLabel.Text = clienteSelecionado.CLINOME;
                    ClientePicker.SelectedItem = clienteSelecionado;
                }
            }
        }
        catch (Exception ex)
        {
            GlobalErro.TratarErro(ex, mostrarAlerta: true);
        }
    }

    private void SetBusy(bool busy)
    {
        BusyOverlay.IsVisible = busy;
        GuardarButton.IsEnabled = !busy;
        CancelarButton.IsEnabled = !busy;
        EliminarButton.IsEnabled = !busy;
    }

    private void MostrarErro(string mensagem)
    {
        ErrorLabel.Text = mensagem;
        ErrorLabel.IsVisible = true;
    }

    private void EsconderErro()
    {
        ErrorLabel.Text = string.Empty;
        ErrorLabel.IsVisible = false;
    }

    private async void OnSelecionarColaboradorClicked(object sender, EventArgs e)
    {
        try
        {
            var popup = new ColaboradorSearchPopup(_colaboradores);
            var result = await Shell.Current.ShowPopupAsync(popup);
            
            if (result is Colaborador colaboradorSelecionado)
            {
                _colaboradorSelecionado = colaboradorSelecionado;
                ColaboradorLabel.Text = colaboradorSelecionado.Nome;
                CalcularTotal();
            }
        }
        catch (Exception ex)
        {
            GlobalErro.TratarErro(ex, mostrarAlerta: false);
        }
    }
}
