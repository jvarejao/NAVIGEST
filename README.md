# NAVIGEST

App de gestão (horas/serviços/produtos) em .NET MAUI 9.0 com alvos Android, iOS e macOS.

## Estado
- ✅ Base da app MAUI ativa (Android/macOS a compilar; páginas de clientes/produtos/serviços e popups em uso)
- 🔄 iOS: build/configuração de provisioning ainda em curso
- 🔄 Windows: suporte opcional (não é alvo principal)

## Como compilar/lançar
No raiz do repositório:
```bash
# macOS (Catalyst)
dotnet build src/NAVIGEST.macOS/NAVIGEST.macOS.csproj -f net9.0-maccatalyst

# Android (APK Release)
dotnet publish src/NAVIGEST.Android/NAVIGEST.Android.csproj -c Release -f net9.0-android

# iOS (apenas se tiver provisioning configurado)
dotnet build src/NAVIGEST.iOS/NAVIGEST.iOS.csproj -f net9.0-ios
```

## Versionamento
- Fonte única: `Directory.Build.props` (`Version`, `ApplicationDisplayVersion`, `ApplicationVersion`).
- `updates/version.json` deve refletir o mesmo número.

## Documentação
- Entrar em `docs/` (ver `docs/README.md` para índice geral).
- Processo de release resumido em `docs/RELEASE.md`.

## Estrutura (alto nível)
```
src/                  Código MAUI (Android/iOS/macOS + shared)
docs/                 Documentação (ativos e arquivo)
scripts/              Automação (releases, utilitários)
updates/version.json  Metadados de update in-app
```
