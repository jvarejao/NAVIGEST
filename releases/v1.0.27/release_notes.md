# NAVIGEST v1.0.27 - Release Notes

## 📅 Data de Lançamento
16 de novembro de 2025

## 🔄 Adaptação Completa do Sistema de Horas

### Integração com Tabelas Existentes
- ✅ Sistema de Horas Colaborador adaptado para usar `HORASTRABALHADAS`
- ✅ Gestão de colaboradores adaptada para usar `COLABORADORESTRAB`
- ✅ Compatibilidade total com estrutura de base de dados existente

### Alterações no Modelo de Dados

**HORASTRABALHADAS:**
- `DataTrabalho` - Data do trabalho realizado
- `IDColaborador` / `NomeColaborador` - Identificação do colaborador
- `IDCliente` / `Cliente` - Cliente associado (opcional)
- `IDCentroCusto` / `DescCentroCusto` - Centro de custo (opcional)
- `HorasTrab` - Horas normais (máximo 8h)
- `HorasExtras` - Horas extras (acima de 8h)
- `Observacoes` - Notas adicionais

**COLABORADORESTRAB:**
- `ID` - Identificador único
- `Nome` - Nome do colaborador
- `Funcao` - Função/cargo
- `ValorHora` - Valor hora do colaborador

### Interface Simplificada
- ✅ Campo único "Horas Totais" para inserção
- ✅ Cálculo automático de horas normais vs extras
- ✅ Formulário mais limpo e intuitivo
- ✅ Validação de valores (máximo 24h)

### Funcionalidades Mantidas
- ✅ CRUD completo (Create, Read, Update, Delete)
- ✅ Filtros por colaborador e período
- ✅ Swipe actions para editar/eliminar
- ✅ Totalizadores em tempo real
- ✅ UI dark/light mode
- ✅ Validações de entrada

## 🔧 Melhorias Técnicas
- Models completamente reescritos para nova estrutura
- DatabaseService adaptado com queries SQL corretas
- ViewModel atualizado com novos tipos de dados
- Popup NovaHoraPopup redesenhado (altura reduzida: 680→580px)
- Bindings XAML atualizados para novos campos

## 📱 Plataformas Suportadas
- ✅ Android (arm64-v8a)

## 🔄 Sistema de Auto-Update
- Sistema de atualização automática ativo
- Verificação ao iniciar aplicação
- Download facilitado de novas versões

---

**Versão Anterior:** v1.0.26  
**Versão Atual:** v1.0.27  
**Breaking Change:** Requer base de dados com tabelas HORASTRABALHADAS e COLABORADORESTRAB
