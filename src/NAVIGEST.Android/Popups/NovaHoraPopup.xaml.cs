using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls;
using NAVIGEST.Android.Models;
using NAVIGEST.Android.Services;
using NAVIGEST.Shared.Resources.Strings;

namespace NAVIGEST.Android.Popups;

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
        // Configurar título
        TituloLabel.Text = _isEdit ? AppResources.Hours_EditTitle : AppResources.Hours_NewTitle;
        EliminarButton.IsVisible = _isEdit;

        // Carregar clientes da BD
        try
        {
            var clientesDb = await DatabaseService.GetClientesAsync(null);
            _clientes = clientesDb.OrderBy(c => c.CLINOME).ToList();
            
            // Adicionar opção "Sem cliente" no início
            _clientes.Insert(0, new Cliente { CLICODIGO = "0", CLINOME = AppResources.Hours_NoClient });
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
                    ClienteLabel.Text = AppResources.Hours_NoClient;
                    await Shell.Current.DisplayAlert(AppResources.ClientsPage_ClientNotFound, 
                        $"O cliente '{idClienteTrim}' já não existe na base de dados.\n\n" +
                        $"Este registo tinha o cliente: {_hora.Cliente ?? "N/A"}", 
                        AppResources.Common_OK);
                }
            }
            else
            {
                _clienteSelecionado = _clientes[0];
                ClienteLabel.Text = AppResources.Hours_NoClient;
            }
        }
        else
        {
            // Novo registo - sem cliente por padrão
            _clienteSelecionado = _clientes[0];
            ClienteLabel.Text = AppResources.Hours_NoClient;
            ColaboradorLabel.Text = AppResources.Hours_SelectCollaborator;
        }

                // Preencher campos
        DataPicker.Date = _hora.DataTrabalho;
        HorasNormaisEntry.Text = _hora.HorasTrab.ToString("0.00");
        HorasExtrasEntry.Text = _hora.HorasExtras.ToString("0.00");
        ObservacoesEditor.Text = _hora.Observacoes;

        // Carregar Tipos de Ausência
        _absenceTypes = await DatabaseService.GetAbsenceTypesAsync();
        AbsenceTypePicker.ItemsSource = _absenceTypes;

        // Verificar se é Ausência
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
        if (ClientContainer != null) ClientContainer.IsVisible = !isAbsence;
        if (AbsenceReasonContainer != null) AbsenceReasonContainer.IsVisible = isAbsence;
    }



    private void OnHorasChanged(object sender, TextChangedEventArgs e)
    {
        // Total label removido
    }

    private async void OnGuardarClicked(object sender, EventArgs e)
    {
        try
        {
            // Validar colaborador
            if (_colaboradorSelecionado == null)
            {
                MostrarErro(AppResources.Hours_ErrorSelectCollaborator);
                return;
            }
            
            var colab = _colaboradorSelecionado;

            bool isAbsence = TipoRegistoPicker.SelectedIndex == 1;

            // Validar campos de horas - aceitar vazio como 0
            string horasNormaisText = string.IsNullOrWhiteSpace(HorasNormaisEntry.Text) ? "0" : HorasNormaisEntry.Text.Replace(",", ".");
            string horasExtrasText = string.IsNullOrWhiteSpace(HorasExtrasEntry.Text) ? "0" : HorasExtrasEntry.Text.Replace(",", ".");
            
            float horasNormais = 0;
            float horasExtras = 0;

            if (!isAbsence)
            {
                if (!float.TryParse(horasNormaisText, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out horasNormais) || horasNormais < 0)
                {
                    MostrarErro(AppResources.Hours_ErrorInvalidNormalHours);
                    return;
                }

                if (!float.TryParse(horasExtrasText, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out horasExtras) || horasExtras < 0)
                {
                    MostrarErro(AppResources.Hours_ErrorInvalidExtraHours);
                    return;
                }

                if (horasNormais + horasExtras > 24)
                {
                    MostrarErro(AppResources.Hours_ErrorTotalHoursExceeded);
                    return;
                }
                
                if (horasNormais + horasExtras == 0)
                {
                    MostrarErro(AppResources.Hours_ErrorEnterHours);
                    return;
                }
            }
            else
            {
                // Se for ausência, forçamos 0 para não somar nas horas trabalhadas
                horasNormais = 0;
                horasExtras = 0;
            }

            SetBusy(true);
            EsconderErro();

            // Preencher objeto - Colaborador
            _hora.DataTrabalho = DataPicker.Date;
            _hora.IdColaborador = colab.ID;
            _hora.NomeColaborador = colab.Nome;
            if (isAbsence)
            {
                if (AbsenceTypePicker.SelectedItem is not AbsenceType absence)
                {
                    MostrarErro(AppResources.Hours_ErrorSelectReason);
                    SetBusy(false);
                    return;
                }
                _hora.IdCentroCusto = absence.Id;
                _hora.DescCentroCusto = absence.Descricao;
                _hora.IdCliente = null;
                _hora.Cliente = absence.Descricao.ToUpper(); // Mostrar motivo na lista
            }
            else
            {
                _hora.IdCentroCusto = null;
                _hora.DescCentroCusto = null;

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
            }
            
            _hora.Observacoes = ObservacoesEditor.Text?.Trim();

            // Guardar horas normais e extras diretamente
            _hora.HorasTrab = horasNormais;
            _hora.HorasExtras = horasExtras;

            // Gravar na BD
            int id;
            try
            {
                id = await DatabaseService.UpsertHoraColaboradorAsync(_hora);
            }
            catch (Exception exDb)
            {
                GlobalErro.TratarErro(exDb, mostrarAlerta: true);
                throw;
            }
            
            _hora.Id = id;

            // Fechar popup com sucesso
            Close(_hora);
        }
        catch (Exception ex)
        {
            GlobalErro.TratarErro(ex, mostrarAlerta: false);
            MostrarErro(AppResources.Hours_ErrorSaving);
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
                AppResources.ClientsPage_DeleteConfirmTitle,
                AppResources.ClientsPage_DeleteConfirm,
                AppResources.Common_Yes,
                AppResources.Common_No
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
            MostrarErro(AppResources.Common_DeleteError);
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
            }
        }
        catch (Exception ex)
        {
            GlobalErro.TratarErro(ex, mostrarAlerta: false);
        }
    }
}
