# Porting Guide: Icons, UI & Dark Mode (iOS/Android)

Este documento descreve as alterações realizadas na versão macOS (Novembro 2025) para suportar ícones dinâmicos, correções de Dark Mode e melhorias no calendário. Estas alterações devem ser replicadas nas versões iOS e Android.

## 1. Base de Dados (DatabaseService.cs)

A tabela `TIPOS_AUSENCIA` foi alterada para incluir uma coluna de ícone.

*   **Alteração**: Adicionar coluna `Icon VARCHAR(20)` à tabela `TIPOS_AUSENCIA`.
*   **Migração Automática**: O método `EnsureAbsenceTypesTableAsync` no `DatabaseService.cs` já inclui a lógica para:
    1.  Criar a tabela com a nova coluna.
    2.  Adicionar a coluna se a tabela já existir (`ALTER TABLE`).
    3.  Atualizar registos antigos com ícones padrão (Férias, Doença, etc.).
*   **Ação**: Copiar o método `EnsureAbsenceTypesTableAsync` e os métodos CRUD (`AddAbsenceTypeAsync`, `UpdateAbsenceTypeAsync`, etc.) para o serviço de base de dados das versões móveis.

## 2. Modelos (HoraColaborador.cs)

O modelo `HoraColaborador` foi enriquecido com propriedades computadas para facilitar o binding na UI.

*   **Novas Propriedades**:
    *   `Icon` (string): Mapeado da base de dados.
    *   `DisplayIcon` (string): Retorna o ícone da ausência ou um ícone padrão de "Trabalho" (`\uf1ad`).
    *   `DisplayText` (string): Retorna a descrição da ausência ou o nome do cliente.
*   **Ação**: Atualizar a classe `HoraColaborador` no projeto partilhado ou nos projetos específicos.

## 3. UI & Dark Mode

Foram feitas várias correções para garantir que a app funciona bem em modo escuro.

*   **Entradas de Texto (Entry)**:
    *   Em `DbConfigPage.xaml` e outros formulários, os campos `Entry` agora têm cores explícitas:
        ```xml
        BackgroundColor="{AppThemeBinding Light=White, Dark=#333333}"
        TextColor="{AppThemeBinding Light=Black, Dark=White}"
        ```
    *   Isto previne que o texto fique branco sobre fundo branco ou invisível.

*   **Fontes**:
    *   A app depende da fonte `FA7Solid` (FontAwesome 6 Free Solid).
    *   Certifique-se de que o ficheiro de fonte (`.otf` ou `.ttf`) está na pasta `Resources/Fonts` dos projetos iOS/Android e registado no `MauiProgram.cs`.

*   **Botões e Ícones em Listas (Estilo iOS)**:
    *   **Botões de Ação (Editar/Apagar)**:
        *   Editar: Azul (`#0A84FF` ou `#007AFF`). Fundo com opacidade 10% (`.WithAlpha(0.1f)`). Ícone: `\uf044` (pencil).
        *   Apagar: Vermelho (`#FF3B30` ou `#FF453A`). Fundo com opacidade 10% (`.WithAlpha(0.1f)`). Ícone: `\uf2ed` (trash).
    *   **Botões Principais (Adicionar/Guardar)**:
        *   Fundo: Azul Sistema (`#007AFF` Light / `#0A84FF` Dark).
        *   Texto: Branco.
        *   `CornerRadius`: 8 ou 12 para manter consistência com o estilo Apple.

## 4. Calendário (HoursEntryPage.xaml.cs)

A lógica de geração do calendário foi atualizada.

*   **Ícones Dinâmicos**:
    *   O método `ConstruirGridCalendario` deixou de usar a função hardcoded `GetAbsenceIcon`.
    *   Agora usa diretamente `primeiraAusencia.Icon` vindo da base de dados.
*   **Estilo**:
    *   Os ícones no calendário usam `FontFamily="FA7Solid"` e `TextColor="#FF2D55"` (Vermelho).
*   **Navegação**:
    *   Ao clicar na Tab "Calendário", a app agora força a atualização para o mês selecionado nos filtros (`_selectedYear`, `_selectedMonth`), garantindo sincronia.

## 5. Gestão de Tipos de Ausência (GerirTiposAusenciaPopup)

Foi criada uma nova interface para gerir os tipos de ausência.

*   **Ficheiros**: `Popups/GerirTiposAusenciaPopup.xaml` e `.xaml.cs`.
*   **Funcionalidades**:
    *   Listagem de tipos existentes.
    *   Adição/Edição com seletor visual de ícones (Grid).
    *   Remoção de tipos.
*   **Adaptação Mobile**:
    *   O tamanho do popup foi ajustado para `HeightRequest="650"`. Em telemóveis (ecrãs pequenos), pode ser necessário ajustar para usar `HeightRequest` relativo ou `VerticalOptions="Fill"`.
    *   O `TapGestureRecognizer` nos ícones é essencial para garantir a seleção em touch screens.

## Resumo de Ficheiros a Sincronizar

1.  `src/NAVIGEST.macOS/Services/DatabaseService.cs` (Lógica de migração e CRUD)
2.  `src/NAVIGEST.macOS/Models/HoraColaborador.cs` (Propriedades de exibição)
3.  `src/NAVIGEST.macOS/Pages/HoursEntryPage.xaml` & `.cs` (Lógica de lista e calendário)
4.  `src/NAVIGEST.macOS/Popups/GerirTiposAusenciaPopup.xaml` & `.cs` (Nova funcionalidade)
5.  `src/NAVIGEST.macOS/Popups/NovaHoraPopup.xaml` & `.cs` (Mostrar ícone selecionado ao criar registo)
