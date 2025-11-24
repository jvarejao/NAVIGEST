using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls;
using NAVIGEST.iOS.Models;
using NAVIGEST.iOS.Services;

namespace NAVIGEST.iOS.Popups;

public partial class NovaHoraPopup : Popup
{
    private HoraColaborador _hora;
    private List<Colaborador> _colaboradores;
    private List<Cliente> _clientes = new();
    private List<AbsenceType> _absenceTypes = new();
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
        TituloLabel.Text = _isEdit ? "✏️ EDITAR REGISTO DE HORAS" : "➕ NOVO REGISTO DE HORAS";
        EliminarButton.IsVisible = _isEdit;

        try
        {
            var clientesDb = await DatabaseService.GetClientesAsync(null);
            _clientes = clientesDb.OrderBy(c => c.CLINOME).ToList();
            _clientes.Insert(0, new Cliente { CLICODIGO = "0", CLINOME = "Sem cliente" });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao carregar clientes: {ex.Message}");
        }

        if (_isEdit && _hora.IdColaborador > 0)
        {
            var colab = _colaboradores.FirstOrDefault(c => c.ID == _hora.IdColaborador);
            if (colab != null)
            {
                _colaboradorSelecionado = colab;
                ColaboradorLabel.Text = colab.Nome;
            }
            
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
                    _clienteSelecionado = _clientes[0];
                    ClienteLabel.Text = "Sem cliente";
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
            _clienteSelecionado = _clientes[0];
            ClienteLabel.Text = "Sem cliente";
            ColaboradorLabel.Text = "Selecione colaborador";
        }

        DataPicker.Date = _hora.DataTrabalho;
        HorasEntry.Text = _hora.HorasTrab.ToString("0.00");
        HorasExtrasEntry.Text = _hora.HorasExtras.ToString("0.00");

        _absenceTypes = await DatabaseService.GetAbsenceTypesAsync();
        AbsenceTypePicker.ItemsSource = _absenceTypes;

        bool isAbsence = false;
        if (_isEdit && _hora.IdCentroCusto.HasValue)
        {
            var absence = _absenceTypes.FirstOrDefault(a => a.Id == _hora.IdCentroCusto.Value);
            if (absence != null)
            {
                isAbsence = true;
                AbsenceTypePicker.SelectedItem = absence;
            }
        }

        TipoRegistoPicker.SelectedIndex = isAbsence ? 1 : 0;
        UpdateVisibility(isAbsence);
    }

    private void OnTipoRegistoChanged(object sender, EventArgs e)
    {
        bool isAbsence = TipoRegistoPicker.SelectedIndex == 1;
        UpdateVisibility(isAbsence);
    }

    private void UpdateVisibility(bool isAbsence)
    {
        if (ClienteContainer != null) ClienteContainer.IsVisible = !isAbsence;
        if (AbsenceReasonContainer != null) AbsenceReasonContainer.IsVisible = isAbsence;
        if (HorasNormaisContainer != null) HorasNormaisContainer.IsVisible = !isAbsence;
        if (HorasExtrasContainer != null) HorasExtrasContainer.IsVisible = !isAbsence;
    }

    private async void OnGuardarClicked(object sender, EventArgs e)
    {
        try
        {
            if (_colaboradorSelecionado == null)
            {
                await Shell.Current.DisplayAlert("Erro", "Selecione um colaborador", "OK");
                return;
            }
            
            var colab = _colaboradorSelecionado;
            bool isAbsence = TipoRegistoPicker.SelectedIndex == 1;

            string horasNormaisText = string.IsNullOrWhiteSpace(HorasEntry.Text) ? "0" : HorasEntry.Text.Replace(",", ".");
            string horasExtrasText = string.IsNullOrWhiteSpace(HorasExtrasEntry.Text) ? "0" : HorasExtrasEntry.Text.Replace(",", ".");
            
            float horasNormais = 0;
            float horasExtras = 0;

            if (!isAbsence)
            {
                if (!float.TryParse(horasNormaisText, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out horasNormais) || horasNormais < 0)
                {
                    await Shell.Current.DisplayAlert("Erro", "Insira horas normais válidas (0-24)", "OK");
                    return;
                }

                if (!float.TryParse(horasExtrasText, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out horasExtras) || horasExtras < 0)
                {
                    await Shell.Current.DisplayAlert("Erro", "Insira horas extras válidas (0-24)", "OK");
                    return;
                }

                if (horasNormais + horasExtras > 24)
                {
                    await Shell.Current.DisplayAlert("Erro", "Total de horas não pode exceder 24h", "OK");
                    return;
                }
                
                if (horasNormais + horasExtras == 0)
                {
                    await Shell.Current.DisplayAlert("Erro", "Insira pelo menos 1 hora", "OK");
                    return;
                }
            }

            _hora.DataTrabalho = DataPicker.Date;
            _hora.IdColaborador = colab.ID;
            _hora.NomeColaborador = colab.Nome;
            
            if (isAbsence)
            {
                if (AbsenceTypePicker.SelectedItem is not AbsenceType absence)
                {
                    await Shell.Current.DisplayAlert("Erro", "Selecione o motivo da ausência", "OK");
                    return;
                }
                _hora.IdCentroCusto = absence.Id;
                _hora.DescCentroCusto = absence.Descricao;
                _hora.IdCliente = null;
                _hora.Cliente = absence.Descricao.ToUpper();
            }
            else
            {
                _hora.IdCentroCusto = null;
                _hora.DescCentroCusto = null;

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
            }
            
            _hora.HorasTrab = horasNormais;
            _hora.HorasExtras = horasExtras;

            int id = await DatabaseService.UpsertHoraColaboradorAsync(_hora);
            _hora.Id = id;

            Close(_hora);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Erro", $"Erro ao guardar: {ex.Message}", "OK");
        }
    }

    private async void OnEliminarClicked(object sender, EventArgs e)
    {
        try
        {
            bool confirmacao = await Shell.Current.DisplayAlert("Confirmar", "Eliminar este registo?", "Sim", "Não");
            if (!confirmacao) return;

            await DatabaseService.DeleteHoraColaboradorAsync(_hora.Id);
            _hora.Id = -1;
            Close(_hora);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Erro", $"Erro ao eliminar: {ex.Message}", "OK");
        }
    }

    private void OnCancelarClicked(object sender, EventArgs e)
    {
        Close(null);
    }

    private async void OnSelecionarColaboradorClicked(object sender, EventArgs e)
    {
        var nomes = _colaboradores.Select(c => c.Nome).ToArray();
        var action = await Shell.Current.DisplayActionSheet("Selecione Colaborador", "Cancelar", null, nomes);
        
        if (action != null && action != "Cancelar")
        {
            var selecionado = _colaboradores.FirstOrDefault(c => c.Nome == action);
            if (selecionado != null)
            {
                _colaboradorSelecionado = selecionado;
                ColaboradorLabel.Text = selecionado.Nome;
            }
        }
    }

    private async void OnSelecionarClienteClicked(object sender, EventArgs e)
    {
        var clientesDisplay = _clientes.Take(20).Select(c => c.CLINOME).ToArray();
        var action = await Shell.Current.DisplayActionSheet("Selecione Cliente (Top 20)", "Cancelar", null, clientesDisplay);
        
        if (action != null && action != "Cancelar")
        {
            var selecionado = _clientes.FirstOrDefault(c => c.CLINOME == action);
            if (selecionado != null)
            {
                _clienteSelecionado = selecionado;
                ClienteLabel.Text = selecionado.CLINOME;
            }
        }
    }
}
