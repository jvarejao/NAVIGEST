using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Maui.Views;
using NAVIGEST.Android.Models;

namespace NAVIGEST.Android.Popups;

public partial class ClienteSearchPopup : Popup
{
    private List<Cliente> _todosClientes;
    private List<Cliente> _clientesFiltrados;

    public ClienteSearchPopup(List<Cliente> clientes)
    {
        InitializeComponent();
        _todosClientes = clientes;
        _clientesFiltrados = clientes;
        clientesCollection.ItemsSource = _clientesFiltrados;
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        string searchText = e.NewTextValue?.ToLower() ?? string.Empty;
        
        if (string.IsNullOrWhiteSpace(searchText))
        {
            _clientesFiltrados = _todosClientes;
        }
        else
        {
            _clientesFiltrados = _todosClientes
                .Where(c => c.CLINOME?.ToLower().Contains(searchText) == true ||
                           c.CLICODIGO?.ToLower().Contains(searchText) == true)
                .ToList();
        }
        
        clientesCollection.ItemsSource = _clientesFiltrados;
    }

    private void OnClienteSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Cliente clienteSelecionado)
        {
            Close(clienteSelecionado);
        }
    }
}
