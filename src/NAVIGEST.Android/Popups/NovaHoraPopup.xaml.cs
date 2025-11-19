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

        // Carregar colaboradores
        ColaboradorPicker.ItemsSource = _colaboradores;
        ColaboradorPicker.ItemDisplayBinding = new Binding("DisplayText");

        // Carregar clientes da BD
        try
        {
            var clientesDb = await DatabaseService.GetClientesAsync(null);
            _clientes = clientesDb.OrderBy(c => c.CLINOME).ToList();
            
            // Adicionar opção "Sem cliente" no início
            _clientes.Insert(0, new Cliente { CLICODIGO = "0", CLINOME = "Sem cliente" });
            
            ClientePicker.ItemsSource = _clientes;
            ClientePicker.ItemDisplayBinding = new Binding("CLINOME");
        }
        catch (Exception ex)
        {
            GlobalErro.TratarErro(ex, mostrarAlerta: false);
        }

        // Se for edição, selecionar colaborador atual
        if (_isEdit && _hora.IdColaborador > 0)
        {
            var colab = _colaboradores.FirstOrDefault(c => c.ID == _hora.IdColaborador);
            ColaboradorPicker.SelectedItem = colab;
            
            // Selecionar cliente também (se existir)
            if (!string.IsNullOrEmpty(_hora.IdCliente))
            {
                var idClienteTrim = _hora.IdCliente.Trim();
                var clienteSelecionado = _clientes.FirstOrDefault(c => c.CLICODIGO?.Trim() == idClienteTrim);
                
                if (clienteSelecionado != null)
                {
                    ClientePicker.SelectedItem = clienteSelecionado;
                }
                else
                {
                    // Cliente não existe mais na BD - selecionar "Sem cliente"
                    ClientePicker.SelectedIndex = 0;
                    await Shell.Current.DisplayAlert("⚠️ Aviso", 
                        $"O cliente '{idClienteTrim}' já não existe na base de dados.\n\n" +
                        $"Este registo tinha o cliente: {_hora.Cliente ?? "N/A"}", 
                        "OK");
                }
            }
            else
            {
                ClientePicker.SelectedIndex = 0;
            }
        }
        else
        {
            // Novo registo - sem cliente por padrão
            ClientePicker.SelectedIndex = 0;
        }

        // Preencher campos
        DataPicker.Date = _hora.DataTrabalho;
        HorasNormaisEntry.Text = _hora.HorasTrab.ToString("0.00");
        HorasExtrasEntry.Text = _hora.HorasExtras.ToString("0.00");
        ObservacoesEditor.Text = _hora.Observacoes;

        // Calcular total inicial
        CalcularTotal();
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
        TotalHorasLabel.Text = $"{total:0.00}h";
    }

    private async void OnGuardarClicked(object sender, EventArgs e)
    {
        try
        {
            // Validar
            if (ColaboradorPicker.SelectedItem is not Colaborador colab)
            {
                MostrarErro("Selecione um colaborador");
                return;
            }

            if (!float.TryParse(HorasNormaisEntry.Text?.Replace(",", "."), out float horasNormais) || horasNormais < 0)
            {
                MostrarErro("Insira horas normais válidas");
                return;
            }

            if (!float.TryParse(HorasExtrasEntry.Text?.Replace(",", "."), out float horasExtras) || horasExtras < 0)
            {
                MostrarErro("Insira horas extras válidas");
                return;
            }

            if (horasNormais + horasExtras > 24)
            {
                MostrarErro("Total de horas não pode exceder 24h");
                return;
            }

            SetBusy(true);
            EsconderErro();

            // Preencher objeto - Colaborador
            _hora.DataTrabalho = DataPicker.Date;
            _hora.IdColaborador = colab.ID;
            _hora.NomeColaborador = colab.Nome;
            
            // Preencher objeto - Cliente
            if (ClientePicker.SelectedItem is Cliente cliente && cliente.CLICODIGO != "0")
            {
                _hora.IdCliente = cliente.CLICODIGO;
                _hora.Cliente = cliente.CLINOME;
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
            var id = await DatabaseService.UpsertHoraColaboradorAsync(_hora);
            _hora.Id = id;

            // Fechar popup com sucesso
            Close(_hora);
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
}
