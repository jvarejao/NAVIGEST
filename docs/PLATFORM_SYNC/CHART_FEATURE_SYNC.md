# Chart Feature Synchronization Plan

This document outlines the steps to synchronize the "Annual Chart" feature across Android and macOS platforms, matching the implementation recently completed for iOS.

## Feature Description
The feature involves clicking on the annual chart in the Dashboard, which opens a detailed popup (`ChartDetailPopup`).
- **Behavior**: The screen rotates to landscape (on mobile) to maximize chart visibility.
- **Popup**: Contains an annual chart and a monthly chart (drill-down).
- **UI**: Uses `BoolToColorConverter` for styling.

## iOS Implementation (Reference)
- **Converter**: `src/NAVIGEST.iOS/Converters/BoolToColorConverter.cs`
- **Popup XAML**: `src/NAVIGEST.iOS/Popups/ChartDetailPopup.xaml` (Uses converter, no hardcoded size)
- **Popup C#**: `src/NAVIGEST.iOS/Popups/ChartDetailPopup.xaml.cs` (Calculates size based on screen dimensions)
- **View**: `src/NAVIGEST.iOS/Views/DashboardView.xaml.cs` (Handles rotation to Landscape and back to Portrait)

## Android Synchronization
### Status
- `ChartDetailPopup` exists but uses hardcoded size (700x400).
- `DashboardView` has rotation logic.
- Missing `BoolToColorConverter`.

### Action Items
1.  **Create Converter**:
    - Copy `BoolToColorConverter.cs` from iOS to `src/NAVIGEST.Android/Converters/`.
2.  **Update Popup XAML** (`src/NAVIGEST.Android/Popups/ChartDetailPopup.xaml`):
    - Add `xmlns:converters` namespace.
    - Add Resources for `BoolToColorConverter`.
    - Remove `WidthRequest="700"` and `HeightRequest="400"` from the root Grid.
3.  **Update Popup C#** (`src/NAVIGEST.Android/Popups/ChartDetailPopup.xaml.cs`):
    - Add logic in constructor to calculate `Size` based on `DeviceDisplay.MainDisplayInfo` (similar to iOS).

## macOS Synchronization
### Status
- `ChartDetailPopup` does not exist.
- `BoolToColorConverter` does not exist.
- `DashboardView` likely missing the event handler.

### Action Items
1.  **Create Converter**:
    - Copy `BoolToColorConverter.cs` from iOS to `src/NAVIGEST.macOS/Converters/`.
2.  **Create Popup**:
    - Create `src/NAVIGEST.macOS/Popups/ChartDetailPopup.xaml` (Copy from iOS).
    - Create `src/NAVIGEST.macOS/Popups/ChartDetailPopup.xaml.cs` (Copy from iOS).
    - **Adaptation**: macOS windows are resizable. We might not need to force "rotation" (which doesn't exist). We might just set a large size for the popup or open a new Window.
    - *Decision*: For now, use the Popup with calculated size (large).
3.  **Update DashboardView** (`src/NAVIGEST.macOS/Views/DashboardView.xaml.cs`):
    - Subscribe to `RequestOpenChartDetail` event in `OnBindingContextChanged`.
    - Implement `Vm_RequestOpenChartDetail`.
    - **Note**: Do NOT implement rotation logic for macOS. Just show the popup.

## Shared Code (If applicable)
- Ensure `DashboardViewModel` and Models are consistent.
- **Android**: Located in `src/NAVIGEST.Android/PageModels/DashboardViewModel.cs`.
- **iOS**: Located in `src/NAVIGEST.iOS/ViewModels/DashboardViewModel.cs`.
- **macOS**: Check `src/NAVIGEST.macOS/PageModels/` or `ViewModels/`.
- If logic was changed in iOS ViewModel (e.g. event triggering), ensure it is replicated in Android/macOS ViewModels.

## Next Steps
- Execute Android Action Items.
- Execute macOS Action Items.
