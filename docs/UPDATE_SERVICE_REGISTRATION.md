// ============================================================================
// INSTRUÇÕES: Adicionar isto ao MauiProgram.cs de CADA plataforma
// (NAVIGEST.Android/MauiProgram.cs, NAVIGEST.iOS/MauiProgram.cs, etc)
// 
// Local: Dentro de CreateMauiApp(), na secção "DI Services"
// ============================================================================

// Exemplo completo (snippet):

/*
// DI Services
builder.Services.AddSingleton<ProjectRepository>();
// ... outros serviços ...

// ✅ ADD THIS: Registar UpdateService para verificação de atualizações
builder.Services.AddHttpClient<NAVIGEST.Shared.Services.IUpdateService, NAVIGEST.Shared.Services.UpdateService>();

// ... resto dos serviços ...
return app;
*/

// ============================================================================
// RESUMO DO QUE ADICIONAR:
// ============================================================================

// 1. ANDROID (NAVIGEST.Android/MauiProgram.cs):
// Adiciona esta linha na secção de DI Services:

builder.Services.AddHttpClient<NAVIGEST.Shared.Services.IUpdateService, NAVIGEST.Shared.Services.UpdateService>();

// 2. iOS (NAVIGEST.iOS/MauiProgram.cs):
// Exactamente a mesma linha:

builder.Services.AddHttpClient<NAVIGEST.Shared.Services.IUpdateService, NAVIGEST.Shared.Services.UpdateService>();

// 3. macOS (NAVIGEST.macOS/MauiProgram.cs):
// Exactamente a mesma linha:

builder.Services.AddHttpClient<NAVIGEST.Shared.Services.IUpdateService, NAVIGEST.Shared.Services.UpdateService>();

// 4. Windows (se aplicável):
// Exactamente a mesma linha:

builder.Services.AddHttpClient<NAVIGEST.Shared.Services.IUpdateService, NAVIGEST.Shared.Services.UpdateService>();

// ============================================================================
// Por que AddHttpClient?
// ============================================================================
// - Registra HttpClient com pool automático (melhor performance)
// - Injecta HttpClient automaticamente no construtor de UpdateService
// - Permite reusar connections entre requisições
// - Padrão recomendado pela Microsoft para MAUI
