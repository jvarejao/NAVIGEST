# Copilot Workspace Instructions – NAVIGEST

## Contexto
Este repositório tem uma solução .NET MAUI com código partilhado e **3 projetos de UI separados**:
- `NAVIGEST.Android`
- `NAVIGEST.iOS`
- `NAVIGEST.macOS` (MacCatalyst)

Muitas páginas existem em **todas as plataformas** com o mesmo nome (ex.: `ClientsPage`).

## Regras que tens de seguir

1. **Alterações simétricas**  
   Se alterares uma página ou code-behind numa plataforma (Android), faz a mesma alteração nas outras 2 (iOS e macOS) quando elas existirem.

2. **Não apagar XAML existente**  
   Não substituas o XAML existente de uma plataforma por MAUI genérico sem criares a versão equivalente nas outras plataformas.

3. **Centralizar lógica no Shared**  
   Sempre que vires o mesmo handler/evento duplicado em Android/iOS/macOS, cria uma classe base no projeto `NAVIGEST.Shared` (ex.: `NAVIGEST.Shared.Pages.ClientsPageBase`) e faz as 3 páginas herdarem dela.

4. **Namespaces fixos**
   - Android → `NAVIGEST.Android.*`
   - iOS → `NAVIGEST.iOS.*`
   - macOS → `NAVIGEST.macOS.*`
   - Shared → `NAVIGEST.Shared.*`
   Não misturar.

5. **Mostrar sempre os 3 ficheiros**
   Quando gerares código para uma página que existe nas 3 plataformas, mostra os 3 `.xaml.cs` atualizados (Android, iOS, macOS) + o ficheiro do Shared, se existir.

6. **Não tocar em serviços/DI sem motivo**
   Só mexe em `MauiProgram.cs` das plataformas se o pedido falar explicitamente de arranque/app.

## Objetivo
Evitar que o projeto fique com Android atualizado e iOS/macOS desatualizados.

## Guia rápido (o que deves saber primeiro)
- **Arquitetura:** solução .NET MAUI com múltiplos projetos UI (`NAVIGEST.Android`, `NAVIGEST.iOS`, `NAVIGEST.macOS`) e um projeto de código partilhado (`NAVIGEST.Shared`). A maior parte da lógica e páginas unificadas estão em `src/NAVIGEST.Shared`.
- **Padrão importante:** quando existirem versões para cada plataforma, mantenha alterações simétricas nas 3 plataformas ou leve a lógica para `NAVIGEST.Shared` (ex.: `Pages/ClientsPageUnified.xaml`).
- **DI & inicialização:** registos de serviços e configurações ficam em `MauiProgram.cs` (cada plataforma chama `MauiProgram.CreateMauiApp()`). Evita alterações neste ficheiro a menos que o pedido seja sobre startup/DI.

## Big picture — arquitetura e fluxos
- **Projects:**
  - `src/NAVIGEST.Shared/` — shared views, viewmodels, services, models (prioridade para centralizar lógica aqui).
  - `src/NAVIGEST.Android/`, `src/NAVIGEST.iOS/`, `src/NAVIGEST.macOS/` — platform-specific pages, behaviors, resources and minor overrides.
- **UI pattern:** XAML pages frequently duplicated; where possible there is a unified XAML in `NAVIGEST.Shared.Pages` (example: `ClientsPageUnified.xaml`) and platform-specific code-behind that may call `Handler?.MauiContext?.Services` to access DI services.
- **Service access pattern:** pages sometimes obtain services via `this.Handler?.MauiContext?.Services` instead of constructor injection — prefer adding services to `MauiProgram` and use DI where feasible, but don't refactor startup unless requested.
- **Third-party UI libs:** Syncfusion toolkit and Maui CommunityToolkit are used (see usages in `src/*/Pages/Controls` and `*.xaml` with `xmlns:sf` or `toolkit`). Be cautious when modifying controls that rely on Syncfusion features.

## Developer workflows (build / run / release)
- **Local build (all projects):** `dotnet build NAVIGEST.sln -c Debug`
- **Run MAUI app on macOS:** prefer Visual Studio for Mac for full platform testing; `dotnet build` + platform deploy tools also usable. iOS builds require a Mac host and Apple toolchain.
- **Android deploy:** use Visual Studio / Android Studio or emulator; some tasks in `.vscode/tasks.json` show device install commands (`ideviceinstaller`, `xcrun devicectl`) used for device automation.
- **Release scripts:** `create-release.sh`, `publish.sh`, and `scripts/create_release.py` exist — consult `RELEASE_INSTRUCTIONS.md` before changing version numbers (it references updating `MauiProgram.cs` or `App.xaml.cs`).

## Conventions & patterns to follow
- **Symmetry first:** if you change XAML or code-behind in one platform, update the other two when an equivalent file exists (rule 1). Show all three files in PRs when applicable.
- **Centralize duplicated handlers:** move duplicated handlers/events into `NAVIGEST.Shared` (e.g., create `Pages.ClientsPageBase` and derive platform pages).
- **Namespaces:** respect fixed namespaces (`NAVIGEST.Android.*`, `NAVIGEST.iOS.*`, `NAVIGEST.macOS.*`, `NAVIGEST.Shared.*`).
- **Platform-specific files:** platform-only views or behaviors belong in their platform project (e.g., `src/NAVIGEST.macOS/Behaviors/*`).

## Integration points & external dependencies
- **Syncfusion:** used across platform controls (look for `Syncfusion.Maui.Toolkit` in XAML). Ensure licenses and package references are consistent across projects.
- **Platform services:** native APIs (biometrics, storage, device) are accessed via Maui APIs and sometimes platform-specific code — search for `Microsoft.Maui.ApplicationModel` and `Microsoft.Maui.Storage` usages.
- **Device tooling:** iOS deployment may use `ideviceinstaller` and `xcrun devicectl` (see `.vscode/tasks.json`). Building iOS on CI requires a macOS build agent.

## Files & locations to reference in PRs or tasks
- `src/NAVIGEST.Shared/Pages/ClientsPageUnified.xaml` — example of unified page used across platforms.
- `src/*/MauiProgram.cs` or top-level `MauiProgram.cs` — DI registration and app startup.
- `src/*/Pages/*` — platform pages; when making UI changes, show Android/iOS/macOS variants.
- `.vscode/tasks.json` — contains device install tasks and helpful terminal commands.
- `docs/` and `RELEASE_INSTRUCTIONS.md` — operational notes and release/version steps.

## Small examples to follow
- When reading a page that accesses services via handler:
  - Example: `ProductsPage.xaml.cs` uses `this.Handler?.MauiContext?.Services` to resolve services — follow that pattern when adding quick fixes unless instructed to refactor to constructor DI.
- When adding a shared page: add XAML to `NAVIGEST.Shared.Pages`, update platform projects to reference the shared page class, and keep platform-specific tweaks in `*.macOS.cs` / `*.iOS.cs` / `*.Android.cs` files.

## What NOT to do
- Don't change startup/DI (`MauiProgram.cs`) unless issue/PR explicitly asks for it.
- Don't replace platform-specific XAML with a single MAUI file without creating equivalent platform files or moving behavior to `NAVIGEST.Shared`.

## When you need help or extra context
- Ask for the target platform and whether the change should be symmetric or centralized in `NAVIGEST.Shared`.
- If a build or device deploy step is needed, mention whether you have a macOS host available for iOS tasks.

---

**Status do Projecto (ponto de partida para próximas sessões):**
- ✅ Estrutura MAUI com 3 plataformas configurada
- ✅ Página unificada `ClientsPageUnified.xaml` como padrão
- ✅ Sincfusion integrado para UI avançada
- 🔄 Em desenvolvimento: sincronizar funcionalidades entre plataformas

