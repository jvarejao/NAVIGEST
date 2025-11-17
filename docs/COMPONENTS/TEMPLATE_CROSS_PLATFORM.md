# NAVIGEST - Cross-Platform Component Documentation Template

## 📋 Estratégia de Documentação

Este documento define o padrão para documentar componentes reutilizáveis across-platform (Android → iOS → macOS → Windows).

**Objetivo:** Criar referência única que facilite portar código entre plataformas com mínimas alterações.

---

## 1. Estrutura de Documentação de Componentes

Cada componente novo deve seguir este padrão:

```
docs/
├── COMPONENTS/                          # 🆕 Pasta central de componentes
│   ├── TEMPLATE_CROSS_PLATFORM.md      # Este ficheiro
│   ├── HORASCOLABORADOR_PAGE_SETUP.md  # Exemplo: HorasColaboradorPage
│   ├── [NOVO_COMPONENTE]_SETUP.md      # Para cada novo componente
│   └── README.md                        # Índice de componentes
│
├── PLATFORMS/                           # Notas específicas por plataforma
│   ├── ANDROID_SPECIFICS.md
│   ├── iOS_SPECIFICS.md
│   ├── macOS_SPECIFICS.md
│   ├── WINDOWS_SPECIFICS.md
│   └── CROSS_PLATFORM_GUIDE.md
│
└── [docs anteriores mantêm-se para referência histórica]
```

---

## 2. Template para Documentar Novo Componente

Use este template quando criar novo componente reutilizável:

### 2.1 Header Padrão

```markdown
# [NOME_COMPONENTE] - Cross-Platform Setup Guide

**Versão:** 1.0  
**Criado:** [data]  
**Modificado:** [data]  
**Plataformas:** Android ✅ | iOS ⏳ | macOS ⏳ | Windows ⏳  
**Status:** Estável em Android

## Resumo Executivo

[Descrição breve - máx 3 linhas - do que faz, para que serve, com que dados trabalha]

**Diagrama de Fluxo:**
```
[Opcional - visão rápida da arquitetura]
```
---

## 3. Seções Obrigatórias

### 3.1 Visão Geral (1-2 parágrafos)
- O que é o componente
- Para que serve
- Qual é o valor para o utilizador
- Inputs/outputs principais

### 3.2 Estrutura de Pastas (idêntica across-platform)

**Padrão (Android como exemplo):**
```
src/NAVIGEST.Android/
├── Models/
│   └── [ComponentName]Model.cs
├── ViewModels/  (ou PageModels)
│   └── [ComponentName]ViewModel.cs
├── Pages/  (ou Views)
│   ├── [ComponentName]Page.xaml
│   └── [ComponentName]Page.xaml.cs
├── Converters/
│   └── [ComponentName]Converter.cs
├── Services/
│   └── [ComponentName]Service.cs
└── Popups/  (se aplicável)
    └── [ComponentName]Popup.xaml(.cs)
```

**Nota para iOS/macOS:**
- Estrutura IDÊNTICA
- Apenas substituir `NAVIGEST.Android` por `NAVIGEST.iOS` / `NAVIGEST.macOS`
- Ficheiros são copias diretas, sem alterações no código C#

**Nota para Windows (Visual Studio):**
- Mesma estrutura acima
- Path: `src/NAVIGEST.Windows/`
- Usar namespaces: `NAVIGEST.Windows.*`
- ⚠️ Verificar espaçamento em WinUI (em caso de custom styling)

### 3.3 Modelos (Models)

**Template:**
```csharp
// Descrição: O que representa este modelo
// Tabelas BD: [Nome_Tabela] coluna [coluna1], [coluna2]

using CommunityToolkit.Mvvm.ComponentModel;

namespace NAVIGEST.Android.Models;

public partial class [ComponentName]Model : ObservableObject
{
    // Copy-paste idêntico para iOS/macOS/Windows - sem alterações
}
```

**Checklist:**
- [ ] Herda `ObservableObject` (para binding automático)
- [ ] Todas propriedades com `[ObservableProperty]`
- [ ] Propriedades computadas sem setter
- [ ] Comentários descrevendo mapeamento BD
- [ ] Sem lógica de negócio (apenas dados)

### 3.4 ViewModels

**Template:**
```csharp
// Descrição: Responsabilidades principais (filtros, carregamentos, etc)
// Métodos principais: [Metodo1], [Metodo2]
// Flag de inicialização: _isInitializing para prevenir ciclos

namespace NAVIGEST.Android.PageModels;

public partial class [ComponentName]ViewModel : ObservableObject
{
    // CRÍTICO: Este código é 100% idêntico em iOS/macOS/Windows
    // Apenas namespace muda: NAVIGEST.iOS, NAVIGEST.macOS, NAVIGEST.Windows
}
```

**Checklist:**
- [ ] Flag `_isInitializing` para evitar ciclos de recarregos
- [ ] Callbacks `OnPropertyChanged()` protegidos por flag
- [ ] RelayCommands para ações
- [ ] Tratamento de erros via `GlobalErro.TratarErro()`
- [ ] Sem UI specifics (nada de Launcher.OpenAsync, DisplayAlert, etc)
- [ ] Async/await para operações lentas

### 3.5 UI (XAML/Code-behind)

**XAML - Padrão:**
```xaml
<!-- Comentário: Descrever propósito desta secção -->
<!-- CRÍTICO: ResourceDictionary com todos os converters necessários -->

<ContentPage x:Class="NAVIGEST.Android.Pages.[ComponentName]Page"
             ...
             x:DataType="vm:[ComponentName]ViewModel">
    
    <ContentPage.Resources>
        <ResourceDictionary>
            <converters:[NomeConverter] x:Key="[chaveConverter]"/>
        </ResourceDictionary>
    </ContentPage.Resources>
    
    <!-- Content -->
</ContentPage>
```

**Code-behind - Padrão:**
```csharp
// Descrição: Code-behind mínimo - apenas inicialização e event handlers
// DI: Suporta tanto constructor com ViewModel como ServiceProvider

namespace NAVIGEST.Android.Pages;

public partial class [ComponentName]Page : ContentPage
{
    public [ComponentName]Page()
    {
        InitializeComponent();
        // Tentar resolver via DI, fallback para new()
    }
    
    public [ComponentName]Page([ComponentName]ViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
    
    // Apenas event handlers - lógica vai para ViewModel
}
```

**Checklist:**
- [ ] ResourceDictionary com todos converters/recursos
- [ ] Dois construtores: um com DI, outro com ViewModel
- [ ] UI minimalista (sem UI logic)
- [ ] Comentários nos layouts complexos
- [ ] Usar grid/flexlayout, evitar canvas
- [ ] Testing: Verificar que está centrado em todos os tamanhos

### 3.6 Converters

**Template:**
```csharp
// Descrição: Transforma [tipo_entrada] em [tipo_saida]
// Usado em: [Onde é usado no XAML]

using System.Globalization;
using Microsoft.Maui.Controls;

namespace NAVIGEST.Android.Converters;

public class [ConverterName] : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Copy-paste idêntico em todas plataformas
    }
    
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
```

**Checklist:**
- [ ] Código 100% idêntico em iOS/macOS/Windows
- [ ] Sem dependências de plataforma
- [ ] Resistente a null values

### 3.7 DatabaseService - Métodos Adicionais

**Template:**
```csharp
// Descrição: Que dados retorna e filtros aplicados
// Tabelas: [Tabela1], [Tabela2]
// Filtros: [Filtro1], [Filtro2]

public static async Task<List<[Model]>> Get[ComponentName]Async(
    [tipo] param1 = null,
    [tipo] param2 = null)
{
    var list = new List<[Model]>();
    try
    {
        using var conn = new MySqlConnection(GetConnectionString());
        await conn.OpenAsync();
        
        // SQL query
        using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue(...);
        
        using var rd = await cmd.ExecuteReaderAsync();
        while (await rd.ReadAsync())
        {
            list.Add(new [Model] { ... });
        }
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"Erro: {ex.Message}");
        throw;
    }
    return list;
}
```

**Checklist:**
- [ ] Métodos são IDÊNTICOS em todas plataformas (DatabaseService é shared)
- [ ] Null handling correto em colunas opcionais
- [ ] Debug.WriteLine para logging
- [ ] Sem UI specifics
- [ ] Retry logic se necessário (conexões instáveis)

---

## 4. Dependency Injection Setup

**MauiProgram.cs - Padrão para todas plataformas:**

```csharp
// Android
builder.Services.AddTransient<[ComponentName]ViewModel>();
builder.Services.AddTransient<[ComponentName]Page>();

// iOS (identico)
builder.Services.AddTransient<[ComponentName]ViewModel>();
builder.Services.AddTransient<[ComponentName]Page>();

// macOS (identico)
builder.Services.AddTransient<[ComponentName]ViewModel>();
builder.Services.AddTransient<[ComponentName]Page>();

// Windows (identico)
builder.Services.AddTransient<[ComponentName]ViewModel>();
builder.Services.AddTransient<[ComponentName]Page>();
```

**Registar Converter (opcional, se usar em XAML global):**
```csharp
builder.Resources.MergedDictionaries.Add(new ResourceDictionary
{
    { "StringNullOrEmptyToBoolConverter", new StringNullOrEmptyToBoolConverter() }
});
```

---

## 5. Navegação - MainYahPage Pattern

**Aplicável em todas plataformas (Android, iOS, macOS, Windows):**

```csharp
case "[route_name]":
{
    try
    {
        var services = this.Handler?.MauiContext?.Services;
        var page = services?.GetService<[ComponentName]Page>();

        if (page == null)
        {
            page = new [ComponentName]Page(new [ComponentName]ViewModel());
        }

        var pageContent = page.Content;
        
        if (pageContent is not null)
        {
            pageContent.BindingContext = page.BindingContext ?? pageContent.BindingContext;
            ShowContent(pageContent);
        }
    }
    catch (Exception ex) { TratarErro(ex); }
    break;
}
```

---

## 6. Tabelas de Banco de Dados

**Template SQL (idêntico para todas plataformas - mesmo BD):**

```sql
CREATE TABLE [TABLE_NAME] (
    ID INT AUTO_INCREMENT PRIMARY KEY,
    Column1 DATATYPE,
    Column2 DATATYPE,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Índices se necessário
CREATE INDEX idx_column1 ON [TABLE_NAME](Column1);
```

---

## 7. Checklist de Portação (Android → iOS/macOS/Windows)

Quando estiver pronto para portar um componente:

- [ ] **Models** - Copiar ficheiro, alterar namespace
- [ ] **ViewModel** - Copiar ficheiro, alterar namespace
- [ ] **Converters** - Copiar ficheiro, alterar namespace
- [ ] **XAML** - Copiar, adaptar UI se necessário (espaçamento, fonts)
- [ ] **Code-behind** - Copiar, alterar namespace
- [ ] **DatabaseService** - Adicionar métodos (já exist em Shared)
- [ ] **MauiProgram.cs** - Adicionar DI (identico ao Android)
- [ ] **MainYahPage.cs** - Adicionar case no switch (identico ao Android)
- [ ] **Testes** - Testar em device/simulator de cada plataforma

---

## 8. Ficheiros que NÃO devem ser alterados entre plataformas

Estes ficheiros são 100% idênticos e apenas namespace muda:

✅ **Models** - Copiar direto, alterar namespace  
✅ **ViewModels** - Copiar direto, alterar namespace  
✅ **Converters** - Copiar direto, alterar namespace  
✅ **DatabaseService methods** - Copiar direto, já existe em DatabaseService  
✅ **RelayCommands logic** - Copiar direto  
✅ **Binding logic** - Copiar direto  

⚠️ **XAML** - Pode precisar ajustes de UI (espaçamento, fonts, tamanho)  
⚠️ **Code-behind** - Normalmente copiar, exceto se houver platform-specifics  

---

## 9. Platform-Specific Considerations

### 9.1 Android
- Rotação de ecrã: Testar em portrait/landscape
- Teclado: Testar com teclado virtual
- Back button: Garantir que volta corretamente

### 9.2 iOS
- Safe areas: Verificar padding superior/inferior
- Gestos: Swipe, pull-to-refresh
- Font sizes: Podem precisar ajuste

### 9.3 macOS
- Resolução: Testar em resoluções diferentes
- Tamanho de fonte: Podem ser maiores
- Keyboard navigation: Tab, Enter, etc

### 9.4 Windows (Visual Studio)
- DPI scaling: Testar em 100%, 125%, 150%
- Keyboard: Alt+key combinations
- Window resize: Garantir que redimensiona bem

---

## 10. Exemplo Completo: Uso do Template

Quando documentar novo componente (ex: `ProductSelectorPage`), criar ficheiro:
```
docs/COMPONENTS/PRODUCTSELECTOR_PAGE_SETUP.md
```

Com secções:
1. ✅ Visão Geral
2. ✅ Estrutura de Pastas
3. ✅ Models (ProductSelector.cs)
4. ✅ ViewModel (ProductSelectorViewModel.cs)
5. ✅ XAML (ProductSelectorPage.xaml)
6. ✅ Code-behind (ProductSelectorPage.xaml.cs)
7. ✅ Converters (se houver)
8. ✅ DatabaseService methods (GetProductsAsync, etc)
9. ✅ DI Setup
10. ✅ Navegação
11. ✅ Tabelas BD
12. ✅ Checklist de Portação

---

## 11. Componentes Documentados

| Componente | Ficheiro | Android | iOS | macOS | Windows | Status |
|-----------|----------|---------|-----|-------|---------|--------|
| HorasColaboradorPage | `HORASCOLABORADOR_PAGE_SETUP.md` | ✅ | ⏳ | ⏳ | ⏳ | Estável |
| [Novo componente] | `[NOVO]_SETUP.md` | ⏳ | ⏳ | ⏳ | ⏳ | Planeado |

---

## 12. Revisões de Documentação

Quando actualizar componente, atualizar também:
- [ ] Versão no header (`Versão: 1.0 → 1.1`)
- [ ] Data modificada
- [ ] Changelog (se houver mudanças importantes)
- [ ] Checklist de portação (se métodos novos)

---

## 13. Referências Cruzadas

- 🔗 Guia iOS specifics: `docs/PLATFORMS/iOS_SPECIFICS.md`
- 🔗 Guia macOS specifics: `docs/PLATFORMS/macOS_SPECIFICS.md`
- 🔗 Guia Windows specifics: `docs/PLATFORMS/WINDOWS_SPECIFICS.md`
- 🔗 Cross-platform patterns: `docs/PLATFORMS/CROSS_PLATFORM_GUIDE.md`

