# Porting Calendar Absence Icons to Android

This document describes the logic implemented in the iOS project (`HorasColaboradorPage.xaml.cs`) to display absence icons on the calendar. This logic needs to be ported to the Android project.

## 1. Helper Method: `GetAbsenceIcon`

Add this helper method to the class responsible for rendering the calendar (likely the Android equivalent of `HorasColaboradorPage` or a ViewModel/Adapter).

```csharp
private string GetAbsenceIcon(string? description)
{
    if (string.IsNullOrEmpty(description)) return "⚠️";
    var desc = description.ToLower();
    if (desc.Contains("férias") || desc.Contains("ferias")) return "🏖️";
    if (desc.Contains("doença") || desc.Contains("doenca") || desc.Contains("médico") || desc.Contains("medico") || desc.Contains("hospital")) return "🏥";
    if (desc.Contains("pai") || desc.Contains("mãe") || desc.Contains("parental") || desc.Contains("filho")) return "👶";
    if (desc.Contains("casamento") || desc.Contains("matrimonio")) return "💍";
    if (desc.Contains("luto") || desc.Contains("falecimento") || desc.Contains("funeral")) return "⚫";
    if (desc.Contains("formação") || desc.Contains("formacao") || desc.Contains("curso")) return "🎓";
    return "⚠️";
}
```

## 2. Calendar Rendering Logic

In the loop where the calendar days are generated (equivalent to `CriarGridCalendario` or `GetView` in an Adapter), add the following logic to check for absences and render the icon.

**Logic:**
1.  Filter the hours for the current day to find any with `IdCentroCusto` (which indicates an absence).
2.  If an absence exists, get the first one.
3.  Call `GetAbsenceIcon` with the absence description (`DescCentroCusto`).
4.  Add a `Label` (or Android `TextView`) to the day's container with the returned emoji.

**C# Example (from iOS implementation):**

```csharp
// Inside the loop for each day...
var horasDoDia = horasDict[data].ToList(); // Get hours for the specific date

// ... existing code for normal/extra hours ...

// Ausências Logic
var ausencias = horasDoDia.Where(h => h.IdCentroCusto.HasValue).ToList();
if (ausencias.Any())
{
    var primeiraAusencia = ausencias.First();
    var icon = GetAbsenceIcon(primeiraAusencia.DescCentroCusto);
    
    // Create Label/TextView for the icon
    var lblAusencia = new Label 
    { 
        Text = icon, 
        FontSize = 12, 
        HorizontalTextAlignment = TextAlignment.Center,
        InputTransparent = true
    };
    
    // Add to the day's stack/layout
    diaStack.Add(lblAusencia);
}
```

## 3. UI Considerations

*   **Placement:** The icon should be placed below the day number and any hour indicators (normal/extra), but this depends on the specific Android layout.
*   **Size:** `FontSize = 12` works well for the emoji on iOS. Adjust for Android density if needed.
*   **Interaction:** Ensure the icon does not block touch events on the day cell (`InputTransparent = true` in MAUI/Xamarin.Forms, or `clickable="false"` in Android XML).
