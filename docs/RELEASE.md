# Release Checklist

Este guia resume o processo atual de release para o NAVIGEST.

## Pré-requisitos
- dotnet SDK 9 instalado
- GitHub CLI (`gh`) autenticado (`gh auth status`)
- git clean (sem alterações pendentes)
- Acesso ao repositório `jvarejao/NAVIGEST`

## Atualizar versão (fonte única)
1) Editar `Directory.Build.props` e definir:
   - `Version`
   - `ApplicationDisplayVersion` (igual à Version)
   - `ApplicationVersion` (inteiro incremental, ex.: 31)
2) Sincronizar `updates/version.json` com o mesmo número em `version`.
3) Commitar as alterações de versão.

## Build e release
1) Garantir branch correto e push em dia.
2) Executar o script (lê versão de `Directory.Build.props`):
   ```bash
   ./scripts/create-release.sh
   ```
   Opções úteis: `--notes <ficheiro>`, `--skip-build`, `--target <branch>`.
3) O script publica o APK de Release (`net9.0-android`) e cria a release GitHub com a tag `v<versao>`.

## Criar release no GitHub manualmente (se necessário)
- Usar a mesma versão de `Directory.Build.props` para a tag e título.
- Anexar o APK gerado em `src/NAVIGEST.Android/bin/Release/net9.0-android/`.

## Release notes
- Mantidas em ficheiros individuais na raiz (`release_notes_v*.md`).
- Referenciar o ficheiro correspondente ao publicar a release.
