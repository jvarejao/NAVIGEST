#!/bin/bash
# Monitor logs do NAVIGEST em tempo real
# Uso: ./monitor-logs.sh

echo "🔍 Monitorizando logs do NAVIGEST..."
echo "📱 Clique no menu 'Horas' na app para ver os logs"
echo "❌ Pressione Ctrl+C para parar"
echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""

# Limpa logs anteriores
adb logcat -c

# Monitoriza logs filtrados
adb logcat | grep -E "\[HorasColaborador|\[MainYahPage\]|NAVIGEST|FATAL|AndroidRuntime"
