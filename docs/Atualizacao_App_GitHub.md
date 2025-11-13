# Atualizacao App GitHub

> Guia interno para publicar novas versoes do NAVIGEST via GitHub Releases, preservar o token e preparar futuras builds para Android, Windows e macOS.

## 1. Visao Geral
- Os binarios publicados no GitHub sao usados pelo Update Checker (ficheiro `updates/version.json`).
- Mantem sempre o `ApplicationDisplayVersion` e o `ApplicationVersion` alinhados no projeto alvo.
- O token GitHub deve ter permissao `repo`. Nao partilhar publicamente.

## 2. Preparar Autenticacao
1. Gerar Personal Access Token (PAT):
   - Acede a <https://github.com/settings/tokens/new>.
   - Nome sugerido: `NAVIGEST-Release`.
   - Expiracao: 90 dias (ou superior).
   - Scopes: marca apenas **repo** (inclui `repo:status`, `repo_deployment`, etc.).
   - Guarda o valor do token (so aparece uma vez).
2. Guardar o token localmente (opcional mas recomendado):
   ```bash
   ./scripts/setup-github-token.sh
   # segue as instrucoes para abrir o browser e colar o token
   ```
   Isso cria `~/.config/navigest/github-token` e `github-token.env` com permissoes restritas.
3. Carregar o token para a sessao atual (sem armazenar permanentemente):
   ```bash
   export GITHUB_TOKEN="$(cat ~/.config/navigest/github-token)"
   ```
4. Verificar autenticacao do GitHub CLI:
   ```bash
   gh auth status
   ```
   - Se faltar `read:org`, executar `gh auth refresh -h github.com -s read:org`.

## 3. Atualizar Codigo e Versionamento
1. Ajustar versao no projeto correspondente (exemplo Android):
   ```xml
   <ApplicationDisplayVersion>1.0.7</ApplicationDisplayVersion>
   <ApplicationVersion>7</ApplicationVersion>
   ```
2. Garantir que os recursos (logos, XAML, etc.) estao atualizados e compilam.
3. `dotnet build` ou `dotnet publish` no alvo pretendido antes de criar release.

### Build Android Release
```bash
dotnet publish src/NAVIGEST.Android/NAVIGEST.Android.csproj -c Release -f net9.0-android
```
Arquivo esperado: `src/NAVIGEST.Android/bin/Release/net9.0-android/com.tuaempresa.navigest-arm64-v8a-Signed.apk`.

### Futuras Plataformas
- **Windows (MSIX)**: usar `dotnet publish -f net9.0-windows10.0.19041.0 -c Release` e empacotar via `msix packaging tool`.
- **macOS (.pkg ou .app)**: `dotnet publish -f net9.0-maccatalyst -c Release` e seguir guia de notarizacao quando aplicavel.
- Atualizar este documento com comandos exatos assim que pipelines estiverem definidos.

## 4. Atualizar Metadados de Update Checker
1. Editar `updates/version.json`:
   ```json
   {
       "version": "1.0.7",
     "minSupportedVersion": "1.0.0",
       "downloadUrl": "https://github.com/jvarejao/NAVIGEST/releases/download/v1.0.7/com.tuaempresa.navigest-arm64-v8a-Signed.apk",
     "notes": "Notas principais da versao."
   }
   ```
2. Confirmar que o link corresponde exatamente ao asset que sera carregado na release.

## 5. Commit e Push
```bash
git status
git add ...
git commit -m "feat(android): mensagem descritiva"
git push origin main
```
Se estiver a reutilizar uma tag existente (ex.: `v1.0.x`), atualizar:
```bash
git tag -d v1.0.x
git tag v1.0.x
git push --force origin v1.0.x
```

## 6. Criar Release no GitHub
### Script automatizado (Android)
```bash
export GITHUB_TOKEN="$(cat ~/.config/navigest/github-token)"
./scripts/create-release.sh 1.0.7
```
- Usa `dotnet publish` para gerar o APK Release.
- Faz `git push` da branch atual antes de criar a release.
- Cria a tag `v1.0.7` (se ainda nao existir) e publica a release com notas base.
- Personaliza com `--notes ficheiro.md`, `--skip-build` ou `--target outra-branch` conforme necessario.

### Script genérico
Os scripts na pasta `scripts/` (por exemplo `create-release.sh`, `setup-and-release.sh`) podem ser adaptados para novas versoes.

## 7. Verificacao Final
1. `gh release view v1.0.X` para confirmar titulo, notas e asset.
2. Abrir `https://github.com/jvarejao/NAVIGEST/releases/tag/v1.0.X` no browser.
3. Testar `curl -L` para garantir download do binario.
4. Instalar no dispositivo/VM alvo e confirmar que o Update Checker oferece a nova versao.

## 8. Consideracoes de Seguranca
- Revogar tokens expirados em <https://github.com/settings/tokens>.
- Nao commitar ficheiros dentro de `~/.config/navigest/`.
- Se o token vazar, revogar imediatamente e atualizar scripts com o novo valor.

## 9. Proximos Passos
- Documentar pipelines para Windows e macOS assim que os binarios estiverem a ser distribuidos.
- Automatizar geracao de release notes (ex.: `git cliff`) se necessario.
- Avaliar integracao com GitHub Actions para builds assinados e envio automatico.
