# Configuração de Provisioning Profile para NAVIGEST.iOS

## Problema Actual
O SDK do iOS não consegue encontrar um provisioning profile para o bundle ID `com.joaovarejao.navigest` com o Team ID `HRXXM344JN`.

## Solução 1: Criar Provisioning Profile no Apple Developer Portal (Recomendado)

1. Aceda a [Apple Developer Portal](https://developer.apple.com)
2. Faça login com sua Apple ID
3. Vá para **Certificates, Identifiers & Profiles**
4. Clique em **Identifiers** → **+**
5. Selecione **App IDs** (ou App)
6. Preencha:
   - **Name**: NAVIGEST
   - **Bundle ID**: `com.joaovarejao.navigest`
   - **Team ID**: HRXXM344JN
7. Clique em **Continue** e **Register**
8. Agora vá para **Profiles** → **+**
9. Selecione **iOS App Development**
10. Selecione o App ID que criou (`com.joaovarejao.navigest`)
11. Selecione o certificado: "Apple Development: João Varejão (STSUBGQ84X)"
12. Selecione o dispositivo (seu iPhone)
13. Dê um nome: `NAVIGEST Development`
14. Clique em **Generate** e **Download**
15. Abra o ficheiro `.mobileprovision` (será importado automaticamente para `~/Library/MobileDevice/Provisioning Profiles/`)

## Solução 2: Usar Xcode (Mais Fácil)

1. Abra Xcode
2. Vá para **Xcode** > **Preferences** > **Accounts**
3. Clique no seu Apple ID/Team
4. Clique em **Manage Certificates**
5. Clique em **+** e selecione **iOS Development**
6. Feche Preferences
7. Xcode irá criar automaticamente o provisioning profile

## Solução 3: Use um Bundle ID Existente (Temporário)

Se quer testar rapidamente, pode usar um bundle ID que já tem provisioning profile:

```xml
<ApplicationId>com.tuaempresa.apploginmaui</ApplicationId>
```

Depois pode voltar a `com.joaovarejao.navigest` quando tiver o provisioning profile criado.

## Verificar Provisioning Profiles Instalados

```bash
# Listar todos os provisioning profiles instalados
ls -la ~/Library/MobileDevice/Provisioning\ Profiles/

# Ver detalhes de um provisioning profile
security cms -D -i ~/Library/MobileDevice/Provisioning\ Profiles/<UUID>.mobileprovision 2>/dev/null | grep -o "<string>[^<]*</string>"
```

## Próximos Passos

1. Escolha a solução acima
2. Após criar o provisioning profile, tente novamente:
   ```bash
   cd /Users/joaovarejao/Dev/NAVIGEST/src/NAVIGEST.iOS
   rm -rf bin obj
   dotnet build -f net9.0-ios -c Debug
   ```

3. Se funcionar, pode fazer publish:
   ```bash
   dotnet publish -f net9.0-ios -c Debug
   ```
