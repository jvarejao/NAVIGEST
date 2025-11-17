# Próxima Tarefa: Portar HorasColaboradorPage para iOS

## 🎯 Objetivo

Portar o componente `HorasColaboradorPage` (já documentado em Android) para iOS, usando o novo framework de documentação.

---

## 📋 Checklist de Portação

Usar este checklist enquanto porta:

### Fase 1: Preparação (15 min)

- [ ] Clonar/pull repositório (git pull origin main)
- [ ] Ler `COMPONENTS/HORASCOLABORADOR_PAGE_SETUP.md` (secção 3-8)
- [ ] Ler `PLATFORMS/iOS_SPECIFICS.md` (especialmente safe area, gestos, permissões)
- [ ] Abrir projeto iOS em Visual Studio

### Fase 2: Copiar Ficheiros Base (20 min)

- [ ] Copiar `Models/HoraColaborador.cs` → `NAVIGEST.iOS/Models/`
- [ ] Copiar `Models/Colaborador.cs` → `NAVIGEST.iOS/Models/`
- [ ] Copiar `Converters/StringNullOrEmptyToBoolConverter.cs` → `NAVIGEST.iOS/Converters/`
- [ ] Copiar `ViewModels/HorasColaboradorViewModel.cs` → `NAVIGEST.iOS/PageModels/` (alterar namespace)
- [ ] Verificar que não têm erros de compilação

### Fase 3: Criar UI (iOS) (30 min)

- [ ] Copiar `HorasColaboradorPage.xaml` → `NAVIGEST.iOS/Pages/`
- [ ] Copiar `HorasColaboradorPage.xaml.cs` → `NAVIGEST.iOS/Pages/`
- [ ] **Adaptar XAML para iOS:**
  - [ ] Adicionar safe area padding (ver `iOS_SPECIFICS.md`)
  - [ ] Ajustar font sizes (iOS pode ter diferentes)
  - [ ] Testar swipe gesture (segue nativo em iOS)
  - [ ] Verificar layout em notch (se houver)

### Fase 4: Dependency Injection (10 min)

- [ ] Abrir `NAVIGEST.iOS/MauiProgram.cs`
- [ ] Adicionar (copiar de Android):
  ```csharp
  builder.Services.AddTransient<HorasColaboradorViewModel>();
  builder.Services.AddTransient<HorasColaboradorPage>();
  ```

### Fase 5: Navegação (10 min)

- [ ] Abrir `NAVIGEST.iOS/Pages/MainYahPage.xaml.cs`
- [ ] Adicionar case no switch (copiar de Android, alterar namespace)
- [ ] Testar que consegue navegar para página

### Fase 6: DatabaseService (5 min)

- [ ] Abrir `NAVIGEST.iOS/Services/DatabaseService.cs` (ou verificar se existe em Shared)
- [ ] Adicionar métodos (copiar de Android):
  - `GetHorasColaboradorAsync()`
  - `GetColaboradoresAsync()`
- [ ] Testes de conexão

### Fase 7: Compilação & Testes (30 min)

- [ ] Compilar sem erros
- [ ] Executar em simulator (iPhone 14 ou 15)
- [ ] Testar:
  - [ ] Página carrega sem crash
  - [ ] Collaborador picker funciona
  - [ ] Date picker funciona
  - [ ] CollectionView lista dados
  - [ ] Swipe para edit/delete funciona
  - [ ] Safe area respeitado (sem sobreposição)

### Fase 8: Documentação (15 min)

- [ ] Atualizar `COMPONENTS/HORASCOLABORADOR_PAGE_SETUP.md`
  - Mudar `iOS: ⏳` para `iOS: ✅`
  - Adicionar notas se houver adaptações específicas
- [ ] Atualizar `COMPONENTS/README.md` (status iOS)
- [ ] Commit: `git commit -m "feat(iOS): Port HorasColaboradorPage to iOS"`

---

## 📁 Ficheiros a Copiar

### De Android para iOS (C# - Sem Alterações)

Estes ficheiros são **100% idênticos**, apenas namespace muda:

```
NAVEGEST.Android/Models/HoraColaborador.cs
    ↓
NAVIGEST.iOS/Models/HoraColaborador.cs
(Alterar apenas: namespace NAVIGEST.Android.Models → NAVIGEST.iOS.Models)

NAVEGEST.Android/Models/Colaborador.cs
    ↓
NAVIGEST.iOS/Models/Colaborador.cs

NAVEGEST.Android/Converters/StringNullOrEmptyToBoolConverter.cs
    ↓
NAVIGEST.iOS/Converters/StringNullOrEmptyToBoolConverter.cs

NAVEGEST.Android/ViewModels/HorasColaboradorViewModel.cs
    ↓
NAVIGEST.iOS/PageModels/HorasColaboradorViewModel.cs
(Alterar namespace: NAVEGEST.Android.ViewModels → NAVIGEST.iOS.PageModels)
```

### De Android para iOS (XAML/C# - Com Adaptações)

Estes ficheiros precisam de ajustes para iOS:

```
NAVEGEST.Android/Pages/HorasColaboradorPage.xaml
    ↓
NAVIGEST.iOS/Pages/HorasColaboradorPage.xaml
(Adaptar: Safe area, font sizes, layout padding)

NAVEGEST.Android/Pages/HorasColaboradorPage.xaml.cs
    ↓
NAVIGEST.iOS/Pages/HorasColaboradorPage.xaml.cs
(Alterar apenas: namespace)
```

---

## 🔍 Adaptações Específicas para iOS

### Safe Area Padding

```xaml
<!-- ❌ ERRADO (sobreposição em notch) -->
<Grid Padding="0">
    <Label Text="Título" />
</Grid>

<!-- ✅ CORRETO (respeita safe area) -->
<Grid Padding="{OnPlatform iOS='0,20,0,0', Android='0'}">
    <Label Text="Título" />
</Grid>
```

### Font Sizes

iOS pode ter diferentes defaults. Se fonts ficarem muito grandes:

```xaml
<!-- ❌ Se ficarem muito grandes -->
<Label FontSize="16" />

<!-- ✅ Usar tamanho específico iOS -->
<Label FontSize="{OnPlatform iOS=14, Android=16}" />
```

### CollectionView em iOS

Testar se scroll funciona. Se não:

```xaml
<!-- Use ScrollView wrapper -->
<ScrollView>
    <VerticalStackLayout>
        <!-- Content -->
    </VerticalStackLayout>
</ScrollView>
```

---

## 🧪 Testes em Simulator

### Abrir Simulator

```bash
# Se usar Visual Studio for Mac
open /Applications/Xcode.app/Contents/Developer/Applications/Simulator.app

# Depois compilar para iOS simulator
dotnet build -f net8.0-ios -c Debug
```

### Testar Comportamentos Específicos iOS

- [ ] Safe area: Não deve sobrepor com notch
- [ ] Gestos: Pull-to-refresh funciona
- [ ] Keyboard: Campos não são cobertos ao digitar
- [ ] Back gesture: Swipe da esquerda para voltar
- [ ] Rotation: Testar portrait + landscape

---

## 🐛 Troubleshooting

### Erro: "Type not found: HorasColaboradorViewModel"

**Solução:** Verificar namespace em XAML

```xaml
<!-- XAML precisa do namespace correto -->
<ContentPage
    xmlns:vm="clr-namespace:NAVIGEST.iOS.PageModels">
    <ContentPage.BindingContext>
        <vm:HorasColaboradorViewModel />
    </ContentPage.BindingContext>
</ContentPage>
```

### Erro: "XamlParseException: StringNullOrEmptyToBoolConverter not found"

**Solução:** Verificar que converter está registado em ResourceDictionary

```xaml
<ContentPage.Resources>
    <ResourceDictionary>
        <converters:StringNullOrEmptyToBoolConverter x:Key="StringNullOrEmptyToBool"/>
    </ResourceDictionary>
</ContentPage.Resources>
```

### CollectionView não scrolls

**Solução:** Usar ScrollView

```xaml
<ScrollView>
    <CollectionView ItemsSource="{Binding Items}">
        <!-- Items -->
    </CollectionView>
</ScrollView>
```

---

## 📊 Tempo Estimado

| Fase | Tarefa | Tempo |
|------|--------|-------|
| 1 | Preparação | 15 min |
| 2 | Copiar ficheiros | 20 min |
| 3 | Criar UI | 30 min |
| 4 | DI | 10 min |
| 5 | Navegação | 10 min |
| 6 | DatabaseService | 5 min |
| 7 | Compilação & Testes | 30 min |
| 8 | Documentação | 15 min |
| **TOTAL** | | **2h 15m** |

---

## ✅ Checklist Final

Antes de fazer commit:

- [ ] Todos testes passam em iOS simulator
- [ ] Sem crashes ou warnings
- [ ] UI está bem (safe area, fonts, layout)
- [ ] Documentação atualizada
- [ ] Git status limpo
- [ ] Commit message é clear

---

## 🚀 Próximo Passo Após iOS

Depois de iOS estar ✅:
1. Portar para macOS (similar a iOS, mas sem safe area)
2. Portar para Windows (diferentes DPI scales)
3. Documentar ClientesPage (novo componente)

---

## 📞 Referências

- Template: `COMPONENTS/TEMPLATE_CROSS_PLATFORM.md`
- Exemplo: `COMPONENTS/HORASCOLABORADOR_PAGE_SETUP.md`
- iOS Guide: `PLATFORMS/iOS_SPECIFICS.md`
- Este documento: Instruções passo-a-passo

---

**Início recomendado:** Assim que estiver confortável com o novo framework

**Status:** 🟢 Pronto para começar

