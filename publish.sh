#!/bin/bash

# Cores
GREEN='\033[0;32m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color
BOLD='\033[1m'

# Emoji
ROCKET="🚀"
PHONE="📱"
APPLE="🍎"
ANDROID="🤖"
CLEAN="🧹"
RESTORE="🔄"

# Diretório base
BASE_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
IOS_DIR="$BASE_DIR/src/NAVIGEST.iOS"
MACOS_DIR="$BASE_DIR/src/NAVIGEST.macOS"
ANDROID_DIR="$BASE_DIR/src/NAVIGEST.Android"

# iPhone ID
IPHONE_ID="00008110-001259663689401E"
BUNDLE_ID="com.tuaempresa.apploginmaui"

clear
echo -e "${BOLD}${CYAN}"
echo "╔═══════════════════════════════════════════════════════╗"
echo "║         NAVIGEST - Build & Deploy Manager            ║"
echo "╚═══════════════════════════════════════════════════════╝"
echo -e "${NC}"

show_menu() {
    echo -e "${BOLD}${GREEN}Escolhe uma opção:${NC}\n"
    echo -e "  ${PHONE} ${BLUE}1)${NC} Publicar iOS + Instalar no iPhone"
    echo -e "  ${APPLE} ${BLUE}2)${NC} Publicar macOS + Abrir App"
    echo -e "  ${ANDROID} ${BLUE}3)${NC} Publicar Android + Instalar"
    echo ""
    echo -e "  ${PHONE} ${BLUE}4)${NC} Build iOS (apenas compilar)"
    echo -e "  ${APPLE} ${BLUE}5)${NC} Build macOS (apenas compilar)"
    echo -e "  ${ANDROID} ${BLUE}6)${NC} Build Android (apenas compilar)"
    echo ""
    echo -e "  ${CLEAN} ${BLUE}7)${NC} Limpar iOS"
    echo -e "  ${CLEAN} ${BLUE}8)${NC} Limpar macOS"
    echo -e "  ${CLEAN} ${BLUE}9)${NC} Limpar Android"
    echo ""
    echo -e "  ${RESTORE} ${BLUE}10)${NC} Restore NuGet (todas as plataformas)"
    echo ""
    echo -e "  ${RED}0)${NC} Sair"
    echo ""
    echo -ne "${YELLOW}Opção: ${NC}"
}

publish_ios() {
    echo -e "\n${ROCKET} ${BOLD}${CYAN}Publicando iOS...${NC}\n"
    cd "$IOS_DIR" || exit
    
    dotnet publish -f net9.0-ios -c Debug || { echo -e "${RED}Erro no build!${NC}"; return 1; }
    
    echo -e "\n${PHONE} ${YELLOW}Desinstalando versão anterior...${NC}"
    ios-deploy --id "$IPHONE_ID" --uninstall_only --bundle_id "$BUNDLE_ID" 2>/dev/null
    
    echo -e "${PHONE} ${YELLOW}Instalando no iPhone...${NC}"
    ios-deploy --id "$IPHONE_ID" --bundle bin/Debug/net9.0-ios/ios-arm64/publish/NAVIGEST.iOS.ipa
    
    echo -e "\n${GREEN}✅ iOS instalado com sucesso!${NC}\n"
}

publish_macos() {
    echo -e "\n${ROCKET} ${BOLD}${CYAN}Publicando macOS...${NC}\n"
    cd "$MACOS_DIR" || exit
    
    dotnet build -f net9.0-maccatalyst -c Debug || { echo -e "${RED}Erro no build!${NC}"; return 1; }
    
    echo -e "${APPLE} ${YELLOW}Abrindo aplicação macOS...${NC}"
    open bin/Debug/net9.0-maccatalyst/maccatalyst-arm64/NAVIGEST.macOS.app
    
    echo -e "\n${GREEN}✅ macOS aberto com sucesso!${NC}\n"
}

publish_android() {
    echo -e "\n${ROCKET} ${BOLD}${CYAN}Publicando Android...${NC}\n"
    cd "$ANDROID_DIR" || exit
    
    dotnet publish -f net9.0-android -c Debug || { echo -e "${RED}Erro no build!${NC}"; return 1; }
    
    echo -e "${ANDROID} ${YELLOW}Instalando no Android via ADB...${NC}"
    adb install -r bin/Debug/net9.0-android/publish/com.tuaempresa.navigest-Signed.apk
    
    echo -e "\n${GREEN}✅ Android instalado com sucesso!${NC}\n"
}

build_ios() {
    echo -e "\n${PHONE} ${BOLD}${CYAN}Build iOS...${NC}\n"
    cd "$IOS_DIR" || exit
    dotnet build -f net9.0-ios -c Debug
    echo -e "\n${GREEN}✅ Build iOS concluído!${NC}\n"
}

build_macos() {
    echo -e "\n${APPLE} ${BOLD}${CYAN}Build macOS...${NC}\n"
    cd "$MACOS_DIR" || exit
    dotnet build -f net9.0-maccatalyst -c Debug
    echo -e "\n${GREEN}✅ Build macOS concluído!${NC}\n"
}

build_android() {
    echo -e "\n${ANDROID} ${BOLD}${CYAN}Build Android...${NC}\n"
    cd "$ANDROID_DIR" || exit
    dotnet build -f net9.0-android -c Debug
    echo -e "\n${GREEN}✅ Build Android concluído!${NC}\n"
}

clean_ios() {
    echo -e "\n${CLEAN} ${YELLOW}Limpando iOS...${NC}\n"
    cd "$IOS_DIR" || exit
    dotnet clean
    echo -e "${GREEN}✅ iOS limpo!${NC}\n"
}

clean_macos() {
    echo -e "\n${CLEAN} ${YELLOW}Limpando macOS...${NC}\n"
    cd "$MACOS_DIR" || exit
    dotnet clean
    echo -e "${GREEN}✅ macOS limpo!${NC}\n"
}

clean_android() {
    echo -e "\n${CLEAN} ${YELLOW}Limpando Android...${NC}\n"
    cd "$ANDROID_DIR" || exit
    dotnet clean
    echo -e "${GREEN}✅ Android limpo!${NC}\n"
}

restore_all() {
    echo -e "\n${RESTORE} ${BOLD}${CYAN}Restore NuGet em todas as plataformas...${NC}\n"
    
    echo -e "${PHONE} ${YELLOW}iOS...${NC}"
    cd "$IOS_DIR" && dotnet restore
    
    echo -e "${APPLE} ${YELLOW}macOS...${NC}"
    cd "$MACOS_DIR" && dotnet restore
    
    echo -e "${ANDROID} ${YELLOW}Android...${NC}"
    cd "$ANDROID_DIR" && dotnet restore
    
    echo -e "\n${GREEN}✅ Restore concluído em todas as plataformas!${NC}\n"
}

# Loop principal
while true; do
    show_menu
    read -r choice
    
    case $choice in
        1) publish_ios ;;
        2) publish_macos ;;
        3) publish_android ;;
        4) build_ios ;;
        5) build_macos ;;
        6) build_android ;;
        7) clean_ios ;;
        8) clean_macos ;;
        9) clean_android ;;
        10) restore_all ;;
        0) 
            echo -e "\n${GREEN}👋 Até breve!${NC}\n"
            exit 0
            ;;
        *)
            echo -e "\n${RED}❌ Opção inválida!${NC}\n"
            sleep 1
            clear
            echo -e "${BOLD}${CYAN}"
            echo "╔═══════════════════════════════════════════════════════╗"
            echo "║         NAVIGEST - Build & Deploy Manager            ║"
            echo "╚═══════════════════════════════════════════════════════╝"
            echo -e "${NC}"
            ;;
    esac
    
    if [ "$choice" != "0" ] && [ "$choice" -ge 1 ] && [ "$choice" -le 10 ]; then
        echo -ne "\n${YELLOW}Pressiona Enter para continuar...${NC}"
        read -r
        clear
        echo -e "${BOLD}${CYAN}"
        echo "╔═══════════════════════════════════════════════════════╗"
        echo "║         NAVIGEST - Build & Deploy Manager            ║"
        echo "╚═══════════════════════════════════════════════════════╝"
        echo -e "${NC}"
    fi
done
