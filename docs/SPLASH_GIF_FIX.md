# Splash GIF (iOS) – Alterações chave

## Contexto
Durante o ciclo de novembro/2025 o splash animado (`startup.gif`) voltou a funcionar de forma consistente no iOS depois de pequenas correções aplicadas diretamente no projeto `NAVIGEST.iOS`. As alterações resolvem duas causas distintas que estavam a impedir o carregamento imediato do GIF.

## O que foi alterado
- **Z-order no XAML** (`src/NAVIGEST.iOS/Pages/SplashIntroPage.xaml`)
  - O `<WebView>` passou a ser declarado antes do `<Image>` de fallback. Num `Grid`, o último filho fica no topo, logo esta inversão garante que a imagem estática aparece primeiro (quando `IsVisible=True`) e deixa de tapar o WebView quando este anima e é feito `FadeTo(1)`. Adicionou-se também um `Label` discreto com a versão para validar builds/testes.

- **Resolução do caminho do asset** (`src/NAVIGEST.iOS/Pages/SplashIntroPage.xaml.cs`)
  - O código do splash passou a procurar o ficheiro em ambos os caminhos: `Resources/Images/startup.gif` (layout padrão do MAUI) e `startup.gif` (quando existe `LogicalName`). Isto remove a dependência de um único nome lógico e evita falhas quando o asset é empacotado com estrutura de pastas.

- **Asset no `.csproj`** (`src/NAVIGEST.iOS/NAVIGEST.iOS.csproj`)
  - O atributo `LogicalName="startup.gif"` foi removido do `<MauiAsset>`. Assim, o runtime expõe o ficheiro no caminho físico `Resources/Images/startup.gif`, alinhado com a nova lógica de detecção.

## Como validar
1. Executar `dotnet build src/NAVIGEST.iOS/NAVIGEST.iOS.csproj -t:Run -f net9.0-ios` ou usar o publish habitual para dispositivo.
2. Confirmar no dispositivo iOS que o splash abre com a imagem estática por milissegundos, seguida da animação GIF carregada pelo WebView.

## Referência rápida
- XAML: `src/NAVIGEST.iOS/Pages/SplashIntroPage.xaml`
- Code-behind: `src/NAVIGEST.iOS/Pages/SplashIntroPage.xaml.cs`
- Projeto iOS: `src/NAVIGEST.iOS/NAVIGEST.iOS.csproj`

> Manter este documento sempre que o splash for ajustado. Se o GIF voltar a falhar, verificar primeiro estas configurações antes de alterar código adicional.
