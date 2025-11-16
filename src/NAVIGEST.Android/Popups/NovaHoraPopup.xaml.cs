using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls;
using NAVIGEST.Android.Models;
using NAVIGEST.Android.Services;

namespace NAVIGEST.Android.Popups;

public partial class NovaHoraPopup : Popup
{
    private HoraColaborador _hora;
    private List<Colaborador> _colaboradores;
    private bool _isEdit;

    public NovaHoraPopup(HoraColaborador hora, List<Colaborador> colaboradores)
    {
        InitializeComponent();
        
        _hora = hora;
        _colaboradores = colaboradores;
        _isEdit = hora.Id > 0;

        InicializarForm();
    }

    private void InicializarForm()
    {
        // Configurar título
        TituloLabel.Text = _isEdit ? "✏️ EDITAR REGISTO DE HORAS" : "➕ NOVO REGISTO DE HORAS";
        EliminarButton.IsVisible = _isEdit;

        // Carregar colaboradores
        ColaboradorPicker.ItemsSource = _colaboradores;
        ColaboradorPicker.ItemDisplayBinding = new Binding("DisplayText");

        // Se for edição, selecionar colaborador atual
        if (_isEdit && _hora.IdColaborador > 0)
        {
            var colab = _colaboradores.FirstOrDefault(c => c.ID == _hora.IdColaborador);
            ColaboradorPicker.SelectedItem = colab;
        }

        // Preencher campos
        DataPicker.Date = _hora.DataTrabalho;
        HorasTotalEntry.Text = (_hora.HorasTrab + _hora.HorasExtras).ToString("0.00");
        ObservacoesEditor.Text = _hora.Observacoes;

        // Calcular horas iniciais
        CalcularHoras();
    }

    private void OnHorasTotalChanged(object sender, TextChangedEventArgs e)
    {
        CalcularHoras();
    }

    private void CalcularHoras()
    {
        if (!float.TryParse(HorasTotalEntry.Text?.Replace(",", "."), out float totalHoras) || totalHoras < 0)
        {
            HorasTotalBorder.Stroke = Colors.Red;
            HorasNormaisLabel.Text = "0,00h";
            HorasExtraLabel.Text = "0,00h";
            return;
        }

        HorasTotalBorder.Stroke = Colors.Transparent;

        // Calcular (máximo 8h normais, resto é extra)
        var horasNormais = Math.Min(totalHoras, 8.0f);
        var horasExtra = Math.Max(totalHoras - 8.0f, 0.0f);

        HorasNormaisLabel.Text = $"{horasNormais:0.00}h";
        HorasExtraLabel.Text = $"{horasExtra:0.00}h";
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

            if (!float.TryParse(HorasTotalEntry.Text?.Replace(",", "."), out float totalHoras) || totalHoras <= 0)
            {
                MostrarErro("Insira um número de horas válido");
                return;
            }

            if (totalHoras > 24)
            {
                MostrarErro("Número de horas não pode exceder 24h");
                return;
            }

            SetBusy(true);
            EsconderErro();

            // Preencher objeto
            _hora.DataTrabalho = DataPicker.Date;
            _hora.IdColaborador = colab.ID;
            _hora.NomeColaborador = colab.Nome;
            _hora.Observacoes = ObservacoesEditor.Text?.Trim();

            // Calcular horas normais e extras
            _hora.CalcularHoras(totalHoras);

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
