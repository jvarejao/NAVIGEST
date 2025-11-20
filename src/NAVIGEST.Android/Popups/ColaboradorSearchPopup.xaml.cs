using System.Collections.ObjectModel;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls;
using NAVIGEST.Android.Models;

namespace NAVIGEST.Android.Popups;

public partial class ColaboradorSearchPopup : Popup
{
    private List<Colaborador> _todosColaboradores;
    private ObservableCollection<Colaborador> _filteredColaboradores;

    public ColaboradorSearchPopup(List<Colaborador> colaboradores)
    {
        InitializeComponent();
        
        _todosColaboradores = colaboradores?.OrderBy(c => c.Nome).ToList() ?? new();
        _filteredColaboradores = new ObservableCollection<Colaborador>(_todosColaboradores);
        
        colaboradoresCollection.ItemsSource = _filteredColaboradores;
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        var searchText = (e.NewTextValue ?? "").ToLower().Trim();
        
        var filtered = _todosColaboradores
            .Where(c => c.Nome.ToLower().Contains(searchText))
            .ToList();
        
        _filteredColaboradores.Clear();
        foreach (var item in filtered)
        {
            _filteredColaboradores.Add(item);
        }
    }

    private void OnColaboradorSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Colaborador colaborador)
        {
            Close(colaborador);
        }
    }
}
