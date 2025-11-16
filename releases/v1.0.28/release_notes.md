# NAVIGEST v1.0.28 - Release Notes

## 📅 Data de Lançamento
16 de novembro de 2025

## 🐛 Fix Crash Menu Horas + Debug Logging

### Correções
- ✅ **Fix crash** ao clicar no menu "Horas"
- ✅ Adicionado **logging detalhado** para debug
- ✅ Melhorado tratamento de erros no ViewModel
- ✅ Adicionado fallback defensivo na criação da página

### Melhorias de Debug
**HorasColaboradorViewModel:**
- Log completo do construtor e inicialização
- Monitorização do carregamento de colaboradores
- Rastreamento de erros em cada etapa

**HorasColaboradorPage:**
- Log da resolução via DI
- Rastreamento da criação do ViewModel
- Detecção de falhas no InitializeComponent

**MainYahPage:**
- Log completo da navegação
- Rastreamento da resolução da página
- Monitorização do ShowContent

### Ferramentas de Debug
- ✅ Script `monitor-logs.sh` para monitorização em tempo real
- ✅ Filtros de log otimizados
- ✅ Instruções completas de debug

### Logs Disponíveis
```bash
# Monitorizar logs em tempo real
./scripts/monitor-logs.sh

# Ou comando direto
adb logcat | grep -E '\[HorasColaborador|\[MainYahPage\]|NAVIGEST'
```

### Funcionalidades Mantidas
- ✅ Sistema de Horas Colaborador completo
- ✅ Integração com HORASTRABALHADAS e COLABORADORESTRAB
- ✅ CRUD completo
- ✅ Filtros e totalizadores
- ✅ Auto-update

## 🔧 Detalhes Técnicos
- Models completamente adaptados
- DatabaseService otimizado
- ViewModel com tratamento robusto de erros
- Logging extensivo para diagnóstico

## 📱 Plataformas Suportadas
- ✅ Android (arm64-v8a)

## 🔄 Sistema de Auto-Update
- Sistema de atualização automática ativo
- Verificação ao iniciar aplicação
- Download facilitado de novas versões

---

**Versão Anterior:** v1.0.27  
**Versão Atual:** v1.0.28  
**Tipo:** Bug Fix + Debug Tools
