# TAB3 Calendar Synchronization Guide (iOS to Android & macOS)

This document outlines the steps required to synchronize the "Calendar" tab (TAB3) features from the iOS implementation to Android and macOS platforms.

## 1. Overview of Changes
The following features have been implemented in iOS and need to be ported:
- **Calendar Icons**: Logic to display specific icons/colors for absences (Férias, Baixa, Feriado, Outros) in the calendar view.
- **Absence Management**: A new "Gerir Ausências" button and popup to manage absence types.
- **Client Selection**: A new `SelectionListPopup` with search functionality to replace the native `DisplayActionSheet` (which has a 20-item limit on some platforms and no search).
- **NovaHoraPopup**: Updated to use the new selection popup and remove the 20-client limit.

## 2. Shared Files (New)
These files are new and should be copied/created in the respective platform projects (`src/NAVIGEST.Android/Popups/` and `src/NAVIGEST.macOS/Popups/`).

### 2.1. `SelectionListPopup.xaml` & `.xaml.cs`
- **Purpose**: A generic popup for selecting items from a list with a search bar.
- **Key Features**:
    - `SearchBar` with real-time filtering.
    - `CollectionView` to display items.
    - Returns the selected string or object.
- **Action**: Copy the files from `src/NAVIGEST.iOS/Popups/` to the target platform.

### 2.2. `GerirTiposAusenciaPopup.xaml` & `.xaml.cs`
- **Purpose**: Allows users to Add, Edit, and Delete absence types (e.g., "Férias", "Doença").
- **Key Features**:
    - Lists existing types from `DatabaseService`.
    - "Adicionar" button to create new types.
    - Swipe-to-delete (or context menu) for existing types.
- **Action**: Copy the files from `src/NAVIGEST.iOS/Popups/` to the target platform.

## 3. Android Implementation Steps

### 3.1. Update `HorasColaboradorPage.xaml.cs`
The current Android implementation only shows "Horas Normais" and "Horas Extras". It needs to be updated to handle Absences.

1.  **Locate `CriarConteudoTab3` method**.
2.  **Update the Calendar Loop**:
    - Inside the loop where cells are created, add logic to check for absences (`IdCentroCusto != null`).
    - If it is an absence, determine the type (Férias, Baixa, etc.) and display the corresponding icon/color instead of (or in addition to) hours.
    - **Reference Code**: See `src/NAVIGEST.iOS/Pages/HorasColaboradorPage.xaml.cs` (Search for `// --- 9. Ausências (Outros) ---`).

3.  **Add "Gerir Ausências" Button**:
    - Add a button near the "Novo Registo" button (or in the header) that opens `GerirTiposAusenciaPopup`.
    - **Command**: `OnGerirAusenciasClicked`.

### 3.2. Update `NovaHoraPopup.xaml.cs`
1.  **Remove Limit**: Ensure `DatabaseService.GetClientesAsync` is called without `.Take(20)`.
2.  **Replace Selection Logic**:
    - Change `OnSelecionarClienteClicked` to use `SelectionListPopup` instead of `DisplayActionSheet`.
    - **Code Snippet**:
      ```csharp
      var popup = new SelectionListPopup("Selecione Cliente", clientesDisplay);
      var result = await Shell.Current.ShowPopupAsync(popup);
      ```

### 3.3. Update `DatabaseService.cs`
- Ensure the following methods exist and are implemented (they should be in `Shared` or duplicated if not using a shared project structure):
    - `GetAbsenceTypesAsync`
    - `AddAbsenceTypeAsync`
    - `UpdateAbsenceTypeAsync`
    - `DeleteAbsenceTypeAsync`

## 4. macOS Implementation Steps

### 4.1. Full Port Required
The macOS project likely lags behind. You will need to:
1.  Copy the new Popups (`SelectionListPopup`, `GerirTiposAusenciaPopup`).
2.  Update `HorasColaboradorPage` to match the iOS/Android logic (Calendar drawing).
3.  Update `NovaHoraPopup` to use the new selection mechanism.

## 5. Verification Checklist
- [ ] **Calendar**: Shows icons for "Férias" (Sun/Umbrella), "Baixa" (Medical), etc.
- [ ] **Calendar**: Clicking a day with an absence opens the detail popup correctly.
- [ ] **Absences**: Can add a new absence type via "Gerir Ausências".
- [ ] **New Record**: Clicking "Cliente" opens a popup with a Search Bar.
- [ ] **New Record**: Search filters the client list correctly.
- [ ] **New Record**: Can select a client that was previously hidden (beyond the top 20).
