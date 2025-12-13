# REGRAS PARA O ASSISTENTE AI (GITHUB COPILOT)

Este ficheiro define as regras de interação e desenvolvimento para este projeto. O Assistente deve consultar estas regras para garantir o alinhamento com o utilizador.

## 1. IDIOMA E COMUNICAÇÃO
- **Idioma Obrigatório:** Todas as respostas, explicações e comentários no código devem ser em **Português (PT)**.
- **Tom:** Profissional, direto e focado na resolução do problema.

## 2. PRINCÍPIOS DE DESENVOLVIMENTO
- **Não Estragar o que Funciona:** Nunca alterar código existente que não esteja diretamente relacionado com a tarefa atual. Se uma alteração for necessária para a tarefa mas afetar outras áreas, **pedir confirmação primeiro**.
- **Alterações Cirúrgicas:** Ao editar ficheiros, focar apenas nas linhas necessárias. Evitar reformatações automáticas de ficheiros inteiros que dificultem a leitura de "diffs".
- **Estabilidade:** O projeto está numa fase avançada. A prioridade é a estabilidade e não a refatorização, a menos que explicitamente pedida.

## 3. FLUXO DE TRABALHO
- **Ler Antes de Agir:** Antes de propor uma solução complexa, verificar a estrutura existente para garantir consistência.
- **Diagnóstico:** Se algo não funcionar, investigar a causa raiz (logs, base de dados) antes de tentar "adivinhar" correções no código.

## 4. FICHEIROS PROTEGIDOS (Exemplos)
- Não alterar a estrutura base da Base de Dados sem autorização.
- Não alterar a lógica de autenticação (`UserSession`) sem autorização.
- Não alterar mais nada a nao ser no codigo que estamos a tratar no momento

## 5. FIO CONDUTOR
- Usar sempre os mesmos tipos de botoes, cores, cantos redondos ( ver pages já existentes se necessário)
- Usar sempre os entry´s iguais com mesmas cores altura, controno azul do macOS
Os popups tem de ter sempre a mesma largura e altura, com entry pesquisa e botao fechao com color azul macOS, (consultar outros popups de outras page se necessáio)
- Manter sempre a mesma largura nos entrys pesquisa usados nos topos das pages e a largura da largura das listas , ( consultar outras pages se necessário)
- **Consistência Visual:** Ao criar ou alterar interfaces (Pages, Popups), verificar sempre as propriedades de layout (Width, Height, Padding, Margin) de páginas semelhantes (ex: ClientsPage, ProductsPage) para garantir uniformidade.


## 6. IDIOMAS ( PT, EN, FR ES )
- Todo o codigo criado tem de ser editado para que tenha as os quatro ediomas, verificar onde esta guardado os idiomas e colocar aqui no rules para ser mais facil depois aceder para editar ou acrescentar.


## 7. PLATAFORMAS ( IOS, macOS, Android e Windows)
- Todo o codigo criado e editado tem de ter em atençao que vai ser preciso adaptar ou adicionar a todas as plataformas. 
- procurar nos DOC onde deve ser adicionado essa informação para quando for para tratar da adaptação dessas plataformas, ser mais facil

## 8. PREVILEGIOS ADMIN, FINANCEIRA, RESTANTES
- Ssempre que houver campos de valor ( moeda currency ) só os utilizadores ADMIN e FINANCEIRA podem ver. Os restantes utilizadores não podem ter acesso a esses dados, por isso nao podem aparecer. O utilizador VENDEDOR pode ver os valores mas só dos serviços dos clientes onde ele é o vendedor.

## 9. UI GUIDELINES (macOS)
Para garantir consistência visual e evitar bugs de "dupla borda" em inputs no macOS:

1.  **Recursos Globais:** Usar sempre os estilos definidos em `Resources/Styles/MacInputStyles.xaml`.
    *   `MacInputBorderStyle` para a borda envolvente.
    *   `MacEntryStyle` / `MacEditorStyle` para os campos (transparentes).
    *   `MacHeaderLabelStyle` para títulos de campos.

2.  **Inputs de Texto (Entry/Editor):**
    *   Devem estar dentro de uma `Border` com `Style="{StaticResource MacInputBorderStyle}"`.
    *   A `Border` deve ter um `DataTrigger` para mudar o `Stroke` para `MacFocusBlue` quando o input tem foco.
    *   O `Entry`/`Editor` deve ter o `Behavior` `<behaviors:MacInputBehavior />` para remover a borda nativa do sistema.

3.  **Popups:**
    *   Seguir a estrutura de `Border` com `StrokeShape="RoundRectangle 18"` e sombra padrão.
    *   Consultar `docs/UI_GUIDE_POPUPS.md` para exemplos de código (copy-paste).

---
*Este ficheiro deve ser atualizado pelo utilizador conforme necessário para ajustar o comportamento do assistente.*
