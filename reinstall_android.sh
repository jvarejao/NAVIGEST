#!/bin/bash

# Script para desinstalar e reinstalar NAVIGEST no Android
# Uso: ./reinstall_android.sh

echo "📱 Desinstalando NAVIGEST..."
adb uninstall com.tuaempresa.navigest

echo "📦 Instalando nova versão..."
adb install src/NAVIGEST.Android/bin/Release/net9.0-android/com.tuaempresa.navigest-Signed.apk

echo "✅ Pronto! App reinstalada."