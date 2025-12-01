using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace NAVIGEST.macOS.Models
{
    public class DashboardMetrics
    {
        public double TotalHoras { get; set; }
        public double TotalHorasNormais { get; set; }
        public double TotalHorasExtras { get; set; }
        public double MediaHorasDia { get; set; }
        public int TotalDiasTrabalhados { get; set; }
        public int TotalAusencias { get; set; }
        public string TopCliente { get; set; } = string.Empty;
        public double SaldoHoras { get; set; }
    }

    public partial class MonthlyHoursData : ObservableObject
    {
        public string Mes { get; set; } = string.Empty; // "Jan", "Fev", etc.
        public int MesNumero { get; set; }
        public int Ano { get; set; }
        public double HorasNormais { get; set; }
        public double HorasExtras { get; set; }
        public double HorasIdeais { get; set; } // Dias úteis * 8

        [ObservableProperty]
        private bool _isSelected;
    }

    public class DailyHoursData
    {
        public int Dia { get; set; }
        public DateTime Data { get; set; }
        public double HorasNormais { get; set; }
        public double HorasExtras { get; set; }
        public double HorasIdeais { get; set; } // 8 se dia útil, 0 se fds/feriado
        public bool IsAbsent { get; set; }
        public string AbsenceType { get; set; } = string.Empty;

        // Helper for Charting Absences
        public double? AbsencePoint => IsAbsent ? HorasIdeais : null;
    }

    public class AbsenceSummary
    {
        public string Tipo { get; set; } = string.Empty;
        public int Dias { get; set; }
        public string Cor { get; set; } = "#8E8E93"; // Hex color
        public string Icon { get; set; } = "\uf073"; // Default calendar icon
    }

    public class ClientHoursSummary
    {
        public string Cliente { get; set; } = string.Empty;
        public double Horas { get; set; }
        public double Percentagem { get; set; }
    }
}
