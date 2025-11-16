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
        ColaboradorPicker.ItemDisplayBinding = new Binding("Nome");

        // Se for edição, selecionar colaborador atual
        if (_isEdit && !string.IsNullOrEmpty(_hora.CodColab))
        {
            var colab = _colaboradores.FirstOrDefault(c => c.Codigo == _hora.CodColab);
            ColaboradorPicker.SelectedItem = colab;
        }

        // Preencher campos
        DataPicker.Date = _hora.Data;
        HoraInicioPicker.Time = _hora.HoraInicio;
        HoraFimPicker.Time = _hora.HoraFim;
        TarefaEntry.Text = _hora.Tarefa;
        ObservacoesEditor.Text = _hora.Obs;

        // Calcular horas iniciais
        CalcularHoras();
    }

    private void OnHoraChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "Time")
        {
            CalcularHoras();
        }
    }

    private void CalcularHoras()
    {
        var inicio = HoraInicioPicker.Time;
        var fim = HoraFimPicker.Time;

        // Validar
        if (fim <= inicio)
        {
            HoraFimBorder.Stroke = Colors.Red;
            HorasNormaisLabel.Text = "0,00h";
            HorasExtraLabel.Text = "0,00h";
            return;
        }

        HoraFimBorder.Stroke = Colors.Transparent;

        // Calcular
        var duracao = (fim - inicio).TotalHours;
        var horasNormais = Math.Min(duracao, 8.0);
        var horasExtra = Math.Max(duracao - 8.0, 0.0);

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

            if (HoraFimPicker.Time <= HoraInicioPicker.Time)
            {
                MostrarErro("Hora fim deve ser maior que hora início");
                return;
            }

            if (string.IsNullOrWhiteSpace(TarefaEntry.Text))
            {
                MostrarErro("Preencha a tarefa");
                return;
            }

            SetBusy(true);
            EsconderErro();

            // Atualizar objeto
            _hora.CodColab = colab.Codigo;
            _hora.NomeColab = colab.Nome;
            _hora.Data = DataPicker.Date;
            _hora.HoraInicio = HoraInicioPicker.Time;
            _hora.HoraFim = HoraFimPicker.Time;
            _hora.Tarefa = TarefaEntry.Text?.Trim() ?? string.Empty;
            _hora.Obs = ObservacoesEditor.Text?.Trim();
            _hora.Utilizador = Environment.UserName;

            // Calcular horas finais
            _hora.CalcularHoras();

            // Guardar
            var id = await DatabaseService.UpsertHoraColaboradorAsync(_hora);
            _hora.Id = id;

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
                "Eliminar este registo?",
                "Sim",
                "Não"
            );

            if (!confirmacao) return;

            SetBusy(true);

            await DatabaseService.DeleteHoraColaboradorAsync(_hora.Id);

            Close(null);
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
