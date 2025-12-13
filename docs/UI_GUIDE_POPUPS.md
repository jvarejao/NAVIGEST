# Guia de UI: Popups e Inputs (macOS Style)

Este guia define o padrão para criar formulários e popups na aplicação NAVIGEST.macOS, garantindo consistência visual com o estilo nativo do macOS e evitando problemas conhecidos (como bordas duplicadas).

## 1. Estrutura do Popup

Os popups devem seguir esta estrutura base para manter o fundo, sombra e arredondamento consistentes.

```xaml
<Border BackgroundColor="{AppThemeBinding Light=#FFFFFF, Dark=#1C1C1E}"
        StrokeThickness="0"
        Padding="24"
        StrokeShape="RoundRectangle 18"
        WidthRequest="500"
        HeightRequest="850" <!-- Ajustar conforme necessário -->
        VerticalOptions="Center"
        HorizontalOptions="Center">
    
    <Border.Shadow>
        <Shadow Brush="Black" Opacity="0.25" Radius="12" Offset="0,4"/>
    </Border.Shadow>

    <Grid RowDefinitions="Auto,*,Auto" RowSpacing="20">
        <!-- Header (Título e Botão Fechar/Eliminar) -->
        <Grid Grid.Row="0" ...> ... </Grid>

        <!-- Conteúdo (Formulário) -->
        <ScrollView Grid.Row="1"> ... </ScrollView>

        <!-- Footer (Botões de Ação) -->
        <Grid Grid.Row="2" ...> ... </Grid>
    </Grid>
</Border>
```

## 2. Inputs de Texto (Entry e Editor)

Para evitar o problema de "dupla borda" (borda nativa do macOS + borda do MAUI), utilizamos um padrão específico:
1.  Uma `Border` envolvente com o estilo `MacInputBorderStyle`.
2.  Um `Entry` ou `Editor` transparente dentro.
3.  Um `Behavior` (`MacInputBehavior`) para remover a borda nativa do sistema.
4.  Um `DataTrigger` na Border para gerir o foco (cor azul).

### Exemplo de Implementação

```xaml
<!-- Namespace necessário no topo do ficheiro -->
xmlns:behaviors="clr-namespace:NAVIGEST.macOS.Behaviors"

<!-- Campo de Texto -->
<VerticalStackLayout>
    <Label Text="Nome do Campo" Style="{StaticResource MacHeaderLabelStyle}"/>
    
    <Border Style="{StaticResource MacInputBorderStyle}">
        <!-- Trigger para Foco Azul -->
        <Border.Triggers>
            <DataTrigger TargetType="Border" 
                         Binding="{Binding Source={x:Reference NomeDoEntry}, Path=IsFocused}" 
                         Value="True">
                <Setter Property="Stroke" Value="{StaticResource MacFocusBlue}"/>
                <Setter Property="StrokeThickness" Value="2"/>
            </DataTrigger>
        </Border.Triggers>

        <Entry x:Name="NomeDoEntry"
               Style="{StaticResource MacEntryStyle}">
            <Entry.Behaviors>
                <behaviors:MacInputBehavior />
            </Entry.Behaviors>
        </Entry>
    </Border>
</VerticalStackLayout>
```

## 3. Pickers (Dropdowns)

Os Pickers (seleção de Cliente, Colaborador, etc.) são simulados visualmente para manter consistência.

```xaml
<VerticalStackLayout>
    <Label Text="Selecione Algo" Style="{StaticResource MacHeaderLabelStyle}"/>
    
    <Border Style="{StaticResource MacInputBorderStyle}">
        <Grid ColumnDefinitions="*,Auto">
            <Grid.GestureRecognizers>
                <TapGestureRecognizer Tapped="OnTapGesture"/>
            </Grid.GestureRecognizers>
            
            <Label Text="Valor Selecionado" 
                   Style="{StaticResource MacInputLabelStyle}"/>
            
            <Label Grid.Column="1" Text="▼" 
                   FontSize="12" 
                   VerticalOptions="Center" 
                   TextColor="{StaticResource MacPlaceholderLight}"/>
        </Grid>
    </Border>
</VerticalStackLayout>
```

## 4. Botões

Utilizar os estilos padrão definidos em `Buttons.xaml` ou seguir o padrão macOS:

*   **Ação Principal:** Azul (`#0A84FF`), Texto Branco.
*   **Cancelar/Secundário:** Cinza Claro (`#E5E5EA` Light / `#3A3A3C` Dark), Texto Preto/Branco.
*   **Destrutivo (Eliminar):** Vermelho (`#FF3B30`), Texto Branco.

```xaml
<Button Text="Guardar"
        BackgroundColor="{StaticResource MacFocusBlue}"
        TextColor="White"
        CornerRadius="8"
        HeightRequest="50"
        FontAttributes="Bold"/>
```

## Resumo de Recursos (Styles)

Estes estilos estão definidos em `Resources/Styles/MacInputStyles.xaml` e disponíveis globalmente:

*   `MacHeaderLabelStyle`: Títulos acima dos inputs.
*   `MacInputBorderStyle`: A borda arredondada cinzenta.
*   `MacInputLabelStyle`: Texto dentro de Pickers.
*   `MacEntryStyle`: Estilo base para Entry (transparente).
*   `MacEditorStyle`: Estilo base para Editor (transparente).
*   `MacFocusBlue`: Cor azul de foco (`#0A84FF`).
