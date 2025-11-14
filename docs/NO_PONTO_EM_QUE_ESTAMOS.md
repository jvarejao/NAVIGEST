# 📍 No Ponto Em Que Estamos - NAVIGEST

## 🎯 Resumo do Projeto

**NAVIGEST** é uma aplicação móvel cross-platform (Android/iOS/macOS) desenvolvida em **MAUI/C#** para gestão de produtos com suporte a múltiplas plataformas.

- **Versão Atual:** v1.0.16
- **Plataformas:** Android (principal), iOS, macOS
- **Framework:** .NET MAUI
- **Linguagem:** C#/XAML
- **Distribuição:** GitHub Releases (Android), App Store (iOS - em desenvolvimento)

---

## 🏗️ Estrutura do Projeto

```
NAVIGEST/
├── src/
│   ├── NAVIGEST.Android/          # Projeto Android (principal)
│   │   ├── Pages/
│   │   │   └── ProductsPage.xaml  # Página de gestão de produtos
│   │   ├── NAVIGEST.Android.csproj
│   │   └── bin/Release/net9.0-android/  # APK gerado
│   ├── NAVIGEST.iOS/              # Projeto iOS
│   ├── NAVIGEST.macOS/            # Projeto macOS
│   ├── NAVIGEST.Shared/           # Código compartilhado
│   │   └── Services/
│   │       └── UpdateService.cs   # Sistema de atualizações
│   └── _OLD_UNUSED_FILES/
├── scripts/
│   └── create-release.sh           # Script de automação de releases
├── updates/
│   └── version.json                # Arquivo de controlo de versões
├── docs/
│   └── [documentação]
└── NAVIGEST.sln

NaviGest.Maui/                     # Projeto alternativo/experimental
```

---

## 🔧 Stack Técnico

| Componente | Tecnologia |
|-----------|-----------|
| **Runtime** | .NET 9.0 |
| **Framework** | MAUI (Multi-platform App UI) |
| **Linguagem** | C# 12 |
| **UI** | XAML |
| **Temas** | AppThemeBinding (Light/Dark) |
| **Ícones** | Font Awesome 7 Solid |
| **CI/CD** | GitHub Actions + Shell Scripts |

---

## 📱 ApplicationId Actual

- **Android:** `com.navigatorcode.navigest`
- **iOS:** `com.navigatorcode.navigest`
- **macOS:** `com.navigatorcode.navigest`

⚠️ **Importante:** Todas as releases partir de v1.0.16 usam este ApplicationId.

---

## ✅ Últimas Correções (v1.0.15 - v1.0.16)

### v1.0.16 (Actual)
✅ Atualizado `ApplicationId` de `com.tuaempresa.*` para `com.navigatorcode.navigest`
✅ Corrigido script de release para usar novo nome de APK
✅ URL de download agora correcto: `com.navigatorcode.navigest-arm64-v8a-Signed.apk`

### v1.0.15
✅ Corrigido `PlaceholderColor` em `ProductsPage.xaml` (formato inválido `Light:#C6C6C8`)
✅ Atualizado para `Light=#8E8E93` (cor legível em ambos temas)

### v1.0.14
✅ Removido `{StaticResource Black}` e `{StaticResource White}` do Picker
✅ Substituído por cores hexadecimais directas

### v1.0.13
✅ Corrigidas cores de `TextColor` dos Entry em tema escuro
✅ Campo `ColaboradorEntry` marcado como `IsReadOnly="True"`
✅ Cores definidas como `{AppThemeBinding Light:#000000, Dark:#FFFFFF}`

---

## 🛠️ Processos Críticos

### 1️⃣ Criar uma Release

```bash
cd /Users/joaovarejao/Dev/NAVIGEST

# Passo 1: Atualizar versão no .csproj
# Editar: src/NAVIGEST.Android/NAVIGEST.Android.csproj
# Mudar: <ApplicationDisplayVersion>1.0.X</ApplicationDisplayVersion>
#        <ApplicationVersion>X</ApplicationVersion>

# Passo 2: Atualizar version.json
# Editar: updates/version.json
# {
#   "version":"1.0.X",
#   "minSupportedVersion":"1.0.0",
#   "downloadUrl":"https://github.com/jvarejao/NAVIGEST/releases/download/v1.0.X/com.navigatorcode.navigest-arm64-v8a-Signed.apk",
#   "notes":"v1.0.X: Descrição das mudanças"
# }

# Passo 3: Fazer commit
git add -A
git commit -m "chore: bump version to 1.0.X - descrição"

# Passo 4: Criar release (automático)
./scripts/create-release.sh 1.0.X
```

**Nota:** O script automaticamente:
- Compila o APK em Release mode
- Faz push para o GitHub
- Cria a release com o APK

### 2️⃣ Versão.json - Controlo de Atualizações

O ficheiro `updates/version.json` controla as atualizações da app:

```json
{
  "version": "1.0.16",
  "minSupportedVersion": "1.0.0",
  "downloadUrl": "https://github.com/jvarejao/NAVIGEST/releases/download/v1.0.16/com.navigatorcode.navigest-arm64-v8a-Signed.apk",
  "notes": "v1.0.16: Atualiza ApplicationId para com.navigatorcode.navigest"
}
```

**Como funciona:**
- A app lê este ficheiro de: `https://raw.githubusercontent.com/jvarejao/NAVIGEST/main/updates/version.json`
- Se `version` > versão instalada, mostra notificação de atualização
- Se `minSupportedVersion` > versão instalada, força actualização
- `downloadUrl` é o link para download do APK

---

## 📂 Ficheiros Críticos para Edição

### ProductsPage.xaml
**Localização:** `src/NAVIGEST.Android/Pages/ProductsPage.xaml`

**O que faz:** Página principal de gestão de produtos

**Componentes principais:**
- `SearchBar` (linha 18) - Busca de produtos
- `CollectionView` (linha 28) - Lista de produtos com SwipeView
- `FormViewContainer` (linha 168) - Formulário de edição
- Campos: Código, Descrição, Família, Colaborador, Preço

**Padrão de cores (IMPORTANTE):**
```xaml
<!-- Tema claro e escuro -->
TextColor="{AppThemeBinding Light=#000000, Dark=#FFFFFF}"
PlaceholderColor="{AppThemeBinding Light=#8E8E93, Dark=#8E8E93}"
BackgroundColor="{AppThemeBinding Light=#FFFFFF, Dark=#1C1C1E}"
```

### UpdateService.cs
**Localização:** `src/NAVIGEST.Shared/Services/UpdateService.cs`

**O que faz:** Verifica atualizações consultando o `version.json` do GitHub

**URL:** `https://raw.githubusercontent.com/jvarejao/NAVIGEST/main/updates/version.json`

### create-release.sh
**Localização:** `scripts/create-release.sh`

**O que faz:** Script de automação para criar releases no GitHub

**Caminho do APK (CRÍTICO):**
```bash
APK_PATH="src/NAVIGEST.Android/bin/Release/net9.0-android/com.navigatorcode.navigest-arm64-v8a-Signed.apk"
```

---

## 🚨 Pontos de Atenção

### ⚠️ Comum: Erro de Formato XAML

**Problema:** `XamlParseException: Position X. Type converter failed: Cannot convert "Light:#XXXXX"`

**Causa:** Espaço extra entre `Light:` e `#` ou formato incorreto

**Solução CORRECTA:**
```xaml
<!-- ✅ CORRECTO -->
TextColor="{AppThemeBinding Light=#000000, Dark=#FFFFFF}"

<!-- ❌ ERRADO -->
TextColor="{AppThemeBinding Light: #000000, Dark: #FFFFFF}"
TextColor="{AppThemeBinding Light:#000000, Dark:#FFFFFF}"
```

### ⚠️ StaticResource não disponível

**Problema:** `StaticResource not found for key Black`

**Causa:** Recursos definidos em `Colors.xaml` não carregados

**Solução:** Usar cores em hex directo em vez de `{StaticResource Black}`

### ⚠️ 404 ao fazer download

**Causas possíveis:**
1. APK não foi gerado (erro de compilação)
2. Nome do APK no script não corresponde ao gerado
3. URL em `version.json` está errada
4. Cache do navegador/app

**Verificação:**
```bash
# Verificar APK gerado
ls -la src/NAVIGEST.Android/bin/Release/net9.0-android/ | grep apk

# Verificar URL no version.json
cat updates/version.json

# Verificar release no GitHub
gh release view v1.0.16 --json assets
```

---

## 🔄 Fluxo de Trabalho - Novo Update

1. **Desenvolver e testar** localmente
2. **Atualizar versão** em:
   - `src/NAVIGEST.Android/NAVIGEST.Android.csproj` (ApplicationDisplayVersion + ApplicationVersion)
3. **Actualizar** `updates/version.json` com:
   - Novo número de versão
   - URL correcta do APK
   - Notas de release
4. **Fazer commit:** `git add -A && git commit -m "..."`
5. **Criar release:** `./scripts/create-release.sh X.X.X`
6. **Verificar no GitHub:** A release e APK devem estar visíveis
7. **Confirmar version.json:** Estar sincronizado com a release

---

## 📊 Histórico de Releases

| Versão | Data | Mudança Principal |
|--------|------|------------------|
| v1.0.16 | 13 Nov | ✅ ApplicationId → com.navigatorcode.navigest |
| v1.0.15 | 13 Nov | ✅ Fix PlaceholderColor format |
| v1.0.14 | 13 Nov | ✅ Remove StaticResource |
| v1.0.13 | 13 Nov | ✅ Fix Entry TextColor (dark theme) |
| v1.0.12 | 12 Nov | 🔧 Previous fix |

---

## 🍎 iOS - Próximos Passos

Atualmente o iOS não está distribuído via GitHub (Apple Store necessária).

**Opções:**
1. **App Store:** Distribuição normal (requer aprovação 24-48h)
2. **TestFlight:** Testes internos (até 100 testers)
3. **Enterprise:** Distribuição privada (certificado pago)

Código iOS existe em `src/NAVIGEST.iOS/` mas não está pronto para produção.

---

## 🔗 Links Importantes

- **Repository:** https://github.com/jvarejao/NAVIGEST
- **Releases:** https://github.com/jvarejao/NAVIGEST/releases
- **Latest Release:** https://github.com/jvarejao/NAVIGEST/releases/tag/v1.0.16
- **Version.json:** https://raw.githubusercontent.com/jvarejao/NAVIGEST/main/updates/version.json

---

## 💡 Dicas para o Copilot

Quando pedir ajuda ao Copilot:

1. **Para bugfix:** Inclua o erro e a linha do ficheiro XAML
2. **Para feature:** Descreva o componente e padrão de cores/estilos
3. **Para release:** Use `./scripts/create-release.sh X.X.X`
4. **Para test:** Teste sempre no Android device/emulator antes de release

---

**Última actualização:** 14 de Novembro de 2025
**Versão do documento:** 1.0
