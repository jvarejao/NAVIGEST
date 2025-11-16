# NAVIGEST v1.0.26 - Release Notes

## 📅 Data de Lançamento
16 de novembro de 2025

## 🆕 Novidades

### Integração da Página Horas Colaborador
- ✅ Página de gestão de Horas Colaborador totalmente integrada no menu principal
- ✅ Acessível através do menu "Horas" no sidebar (ícone ⏱️)
- ✅ Registada no sistema de navegação Shell
- ✅ Dependency Injection configurado para ViewModel e Page

## 🔧 Melhorias Técnicas
- Registada `HorasColaboradorPage` no `AppShell.xaml` com route "horas-colaborador"
- Registados `HorasColaboradorViewModel` e `HorasColaboradorPage` no `MauiProgram.cs`
- Menu "Horas" no `MainYahPage` agora chama `HorasColaboradorPage` via DI

## 📱 Plataformas Suportadas
- ✅ Android (arm64-v8a)

## 🔄 Sistema de Auto-Update
- Sistema de atualização automática ativo
- Verificação de novas versões ao iniciar aplicação
- Download e instalação facilitada de atualizações

## 📋 Funcionalidades Disponíveis
- Gestão completa de horas de colaborador (CRUD)
- Filtros por colaborador e intervalo de datas
- Cálculo automático de horas normais e extras
- Swipe actions para edição e eliminação
- Totalizadores em tempo real

---

**Versão Anterior:** v1.0.25  
**Versão Atual:** v1.0.26
