# NAVIGEST v1.0.24 - Nova Funcionalidade + Correções

## ✨ Novas Funcionalidades

### 📊 Página de Horas de Colaborador
- ✅ **Gestão completa de horas** - Criar, editar e eliminar registos
- ✅ **Cálculo automático** - Horas normais (max 8h) e horas extra calculadas automaticamente
- ✅ **Filtros avançados** - Por colaborador e intervalo de datas
- ✅ **Totais em tempo real** - Visualização de horas normais, extra e total geral
- ✅ **Interface moderna** - Design estilo iOS com popup modal para edição
- ✅ **Validação inteligente** - Bordas neon vermelhas para campos inválidos
- ✅ **Swipe actions** - Deslize para editar ou eliminar registos rapidamente
- ✅ **Pull-to-refresh** - Arraste para baixo para atualizar a lista

## 🐛 Correções de Bugs

### Popup de Seleção de Famílias
- ✅ **Corrigido erro XAML** - AppThemeBinding com sintaxe correta (`Light=#HEX`)
- ✅ **Popup agora funciona** - Implementado fluxo iOS-style (criação primeiro, depois lista)
- ✅ **Código automático** - Campo código é readonly e gerado automaticamente
- ✅ **Descrição em maiúsculas** - Conversão automática para uppercase
- ✅ **Botão "Nova Família" removido** - Interface simplificada na lista

## 🎨 Melhorias de Interface

### Horas de Colaborador
- Botões circulares com ícones Font Awesome 7
- Sombras suaves e bordas arredondadas
- Suporte para Dark/Light mode
- Loading states com overlay semitransparente
- Toast notifications para feedback

### Popup de Famílias
- Layout limpo e intuitivo
- Cálculo de horas visível em tempo real
- Botões de ação coloridos (Vermelho=Cancelar, Verde=Confirmar, Laranja=Eliminar)

---

## 📥 Download

**APK Android:** [com.navigatorcode.navigest-arm64-v8a-Signed.apk](https://github.com/jvarejao/NAVIGEST/releases/download/v1.0.24/com.navigatorcode.navigest-arm64-v8a-Signed.apk)

---

## 🔄 Atualização Automática

Esta versão inclui sistema de auto-update. A aplicação verificará automaticamente por novas versões ao iniciar.

---

## 📱 Plataformas Suportadas

- ✅ **Android** (arm64-v8a, armeabi-v7a, x86, x86_64)
- 🚧 iOS (em desenvolvimento)
- 🚧 macOS Catalyst (em desenvolvimento)

---

## 🗄️ Base de Dados

### Nova Tabela: HORASCOLABORADOR
```sql
ID INT AUTO_INCREMENT PRIMARY KEY,
CODCOLAB VARCHAR(10),
NOMECOLAB VARCHAR(100),
DATA DATE,
HORAINICIO TIME,
HORAFIM TIME,
HORASNORMAIS DECIMAL(5,2),
HORASEXTRA DECIMAL(5,2),
TAREFA VARCHAR(100),
OBS TEXT,
VALIDADO BIT,
UTILIZADOR VARCHAR(50)
```

### Tabela Utilizada: COLABORADORES
```sql
CODIGO VARCHAR(10),
NOME VARCHAR(100),
EMAIL VARCHAR(100),
TELEFONE VARCHAR(20),
ATIVO BIT
```

---

**Data de lançamento:** 16 de novembro de 2025
