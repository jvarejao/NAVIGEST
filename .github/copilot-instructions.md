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
