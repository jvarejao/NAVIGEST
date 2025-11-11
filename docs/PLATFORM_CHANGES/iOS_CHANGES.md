# iOS CHANGES LOG

**Propósito**: Rastrear todas as mudanças feitas em NAVIGEST.iOS para sincronização com Android/macOS.

---

## Status Atual

### SplashIntroPage.xaml.cs - Installed Version Manager (2025-11-11)

**Status**: ✅ Implementado (sincronizado de Android)

**Mudanças**:
- ✅ Adicionar constante: `private const string INSTALLED_VERSION_KEY = "InstalledAppVersion";`
- ✅ OnAppearing: Detecta se app foi atualizada (manifest version ≠ saved version)
- ✅ OnAppearing: Guarda nova versão em Preferences se atualizada
- ✅ GetInstalledVersion(): Lê versão guardada em Preferences
- ✅ SaveInstalledVersion(): Guarda versão em Preferences
- ✅ NAVIGEST.iOS.csproj: ApplicationDisplayVersion 1.0.0 → 1.0.2

**Propósito**: Evitar loop infinito de atualização. App agora reconhece v1.0.2 após reinstalar.

**Referência**: 
- Android: `PLATFORM_CHANGES/ANDROID_CHANGES.md`
- Código Android: `src/NAVIGEST.Android/Pages/SplashIntroPage.xaml.cs` (linhas 475-529)
- Commit Android: be34253

**Detalhes Técnicos**:
- iOS usa o mesmo padrão que Android (Preferences é cross-platform)
- Sem diferenças de API entre plataformas
- Funciona com System.Diagnostics.Debug.WriteLine para logs

---

### ClientsPage.xaml.cs - Delete Confirmation

**Status**: ✅ Implementado (pré-existente)

**Detalhes**: 
- ShowConfirmAsync helper já estava implementado
- OnDeleteSwipeInvoked já tinha confirmação
- OnDeleteFromFormTapped já tinha confirmação

---

**Última Atualização**: 2025-11-11
