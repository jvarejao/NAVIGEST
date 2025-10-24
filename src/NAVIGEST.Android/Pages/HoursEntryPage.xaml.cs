using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using NAVIGEST.Android.ViewModels;

namespace NAVIGEST.Android.Pages
{
    public partial class HoursEntryPage : ContentPage
    {
        public HoursEntryPage()
            : this(Application.Current?.Handler?.MauiContext?.Services?.GetService<HoursEntryViewModel>()
                   ?? new HoursEntryViewModel())
        {
        }

        public HoursEntryPage(HoursEntryViewModel vm)
        {
            InitializeComponent();
            try
            {
                BindingContext = vm ?? throw new ArgumentNullException(nameof(vm));
                vm.RequestNotesEditorAsync = EditNotesAsync;
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        private async Task<string?> EditNotesAsync(TimeEntry entry)
        {
            var newText = await DisplayPromptAsync(
                "Observaes",
                "Editar observaes da linha:",
                accept: "Guardar",
                cancel: "Cancelar",
                placeholder: "Escreve aqui",
                initialValue: entry?.Notes,
                maxLength: 1000,
                keyboard: Keyboard.Text);

            return newText;
        }
    }
}
