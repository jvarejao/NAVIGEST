#!/bin/bash
set -e

BUNDLE_ID="com.tuaempresa.navigest"
PROFILE_NAME="VS: ${BUNDLE_ID} Development"
CERT_NAME="Apple Development: Created via API (HRXXM344JN)"
TEAM_ID="HRXXM344JN"

echo "🔨 Criando provisioning profile para ${BUNDLE_ID}..."

# Criar App ID se não existir
fastlane produce create \
  --app_identifier "${BUNDLE_ID}" \
  --app_name "NAVIGEST" \
  --team_id "${TEAM_ID}" \
  --skip_itc true 2>/dev/null || echo "App ID já existe"

# Criar provisioning profile
fastlane sigh \
  --app_identifier "${BUNDLE_ID}" \
  --team_id "${TEAM_ID}" \
  --development \
  --force \
  --output_path . \
  --filename "navigest.mobileprovision"

echo "✅ Provisioning profile criado!"
