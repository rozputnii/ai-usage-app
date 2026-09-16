[CmdletBinding()]
param([string] $Output = (Join-Path $PSScriptRoot '../../src/windows/AiUsage.Windows/Themes/Tokens.xaml'))
# Generates Themes/Tokens.xaml from the AIU-010 design tokens (Quiet Editorial 1b, specification section 3).
# Framework lightweight-styling keys receive explicit per-theme colours so RequestedTheme switches resolve correctly.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# name = light, dark, high-contrast system colour key
$tokens = [ordered]@{
    AppBg = '#F6F4EF', '#1F1E1B', 'SystemColorWindowColor'
    Card = '#FDFCFA', '#282725', 'SystemColorWindowColor'
    Card2 = '#EEEBE4', '#31302C', 'SystemColorWindowColor'
    Stroke = '#1C1D1B18', '#1FF3F1EC', 'SystemColorWindowTextColor'
    Stroke2 = '#3D1D1B18', '#42F3F1EC', 'SystemColorWindowTextColor'
    Text = '#1D1B18', '#F3F1EC', 'SystemColorWindowTextColor'
    Text2 = '#6B665E', '#B8B2A7', 'SystemColorWindowTextColor'
    Text3 = '#9A948A', '#8A857B', 'SystemColorGrayTextColor'
    Accent = '#C8684A', '#E38A6C', 'SystemColorHotlightColor'
    AccentHover = '#B2583C', '#EEA38A', 'SystemColorHotlightColor'
    AccentText = '#FFFFFF', '#1F1E1B', 'SystemColorHighlightTextColor'
    AccentSoft = '#21C8684A', '#2EE38A6C', 'SystemColorHighlightColor'
    Primary = '#1D1B18', '#F3F1EC', 'SystemColorButtonTextColor'
    PrimaryText = '#FFFFFF', '#1F1E1B', 'SystemColorButtonFaceColor'
    Fill = '#1D1B18', '#F3F1EC', 'SystemColorWindowTextColor'
    OkFill = '#3E9A5A', '#6CCB8A', 'SystemColorWindowTextColor'
    WarnFill = '#E8811C', '#F0A35E', 'SystemColorHighlightColor'
    Warn = '#B25A00', '#F0A35E', 'SystemColorHighlightColor'
    WarnBg = '#F6E7C4', '#24E5B95A', 'SystemColorWindowColor'
    WarnStroke = '#DCC48A', '#73E5B95A', 'SystemColorHighlightColor'
    Crit = '#B3301E', '#FF9C8A', 'SystemColorHotlightColor'
    CritBg = '#F8E1DC', '#24FF9C8A', 'SystemColorWindowColor'
    CritStroke = '#E8B3A8', '#73FF9C8A', 'SystemColorHotlightColor'
    Ok = '#2F7A3E', '#7BCB8A', 'SystemColorWindowTextColor'
    Track = '#1A1D1B18', '#1FF3F1EC', 'SystemColorWindowColor'
    Hatch = '#4D1D1B18', '#52F3F1EC', 'SystemColorWindowTextColor'
    Skel = '#EAE7E0', '#2C2B28', 'SystemColorWindowColor'
    Skel2 = '#F4F2ED', '#3A3834', 'SystemColorGrayTextColor'
    Focus = '#1D1B18', '#F3F1EC', 'SystemColorHighlightColor'
    Overlay = '#521D1B18', '#8C000000', 'SystemColorWindowColor'
    Btn = '#FDFCFA', '#282725', 'SystemColorButtonFaceColor'
    BtnHover = '#F0EDE6', '#33322E', 'SystemColorHighlightColor'
    DestructiveText = '#FFFFFF', '#1F1E1B', 'SystemColorButtonFaceColor'
    Shadow = '#291D1B18', '#80000000', 'SystemColorWindowTextColor'
    Transparent = '#00FFFFFF', '#00000000', ''
}

# Simulated high contrast for the demo shell (spec design stand-ins); real contrast themes use the system colours above.
$simulated = [ordered]@{
    AppBg = '#000000'; Card = '#000000'; Card2 = '#000000'; Stroke = '#FFFFFF'; Stroke2 = '#FFFFFF'; Text = '#FFFFFF'; Text2 = '#FFFFFF'; Text3 = '#FFFFFF'
    Accent = '#FFFF00'; AccentHover = '#FFFF80'; AccentText = '#000000'; AccentSoft = '#2A2A00'; Primary = '#FFFFFF'; PrimaryText = '#000000'; Fill = '#FFFFFF'
    OkFill = '#3FF23F'; WarnFill = '#FFFF00'; Warn = '#FFFF00'; WarnBg = '#000000'; WarnStroke = '#FFFF00'; Crit = '#FF8080'; CritBg = '#000000'; CritStroke = '#FF8080'
    Ok = '#3FF23F'; Track = '#000000'; Hatch = '#FFFFFF'; Skel = '#000000'; Skel2 = '#444444'; Focus = '#FFFF00'; Overlay = '#D9000000'; Btn = '#000000'; BtnHover = '#333333'
    DestructiveText = '#000000'; Shadow = '#FFFFFF'; Transparent = '#00000000'
}

# Framework key = token it takes its colour from.
$aliases = [ordered]@{
    NavigationViewTopPaneBackground = 'Transparent'; NavigationViewItemBackgroundPointerOver = 'Transparent'; NavigationViewItemBackgroundPressed = 'Transparent'
    NavigationViewItemBackgroundSelected = 'Transparent'; NavigationViewItemBackgroundSelectedPointerOver = 'Transparent'; NavigationViewItemBackgroundSelectedPressed = 'Transparent'
    TopNavigationViewItemForeground = 'Text2'; TopNavigationViewItemForegroundPointerOver = 'Text'; TopNavigationViewItemForegroundPressed = 'Text'
    TopNavigationViewItemForegroundSelected = 'Text'; TopNavigationViewItemForegroundSelectedPointerOver = 'Text'; TopNavigationViewItemForegroundSelectedPressed = 'Text'
    NavigationViewSelectionIndicatorForeground = 'Accent'; NavigationViewButtonForeground = 'Text2'; NavigationViewButtonBackgroundPointerOver = 'Card2'
    NavigationViewDefaultPaneBackground = 'AppBg'; NavigationViewExpandedPaneBackground = 'AppBg'; NavigationViewContentBackground = 'Transparent'; NavigationViewContentGridBorderBrush = 'Transparent'
    ToggleSwitchFillOn = 'Accent'; ToggleSwitchFillOnPointerOver = 'AccentHover'; ToggleSwitchFillOnPressed = 'AccentHover'; ToggleSwitchKnobFillOn = 'AccentText'; ToggleSwitchKnobFillOnPointerOver = 'AccentText'; ToggleSwitchKnobFillOnPressed = 'AccentText'
    ToggleSwitchFillOff = 'Card2'; ToggleSwitchFillOffPointerOver = 'Card2'; ToggleSwitchStrokeOff = 'Stroke2'; ToggleSwitchStrokeOffPointerOver = 'Stroke2'; ToggleSwitchKnobFillOff = 'Text2'; ToggleSwitchKnobFillOffPointerOver = 'Text'
    ToggleSwitchContentForeground = 'Text'; ToggleSwitchHeaderForeground = 'Text'
    TextControlBackground = 'Card'; TextControlBackgroundPointerOver = 'Card'; TextControlBackgroundFocused = 'Card'; TextControlBorderBrush = 'Stroke2'; TextControlBorderBrushPointerOver = 'Text2'; TextControlBorderBrushFocused = 'Accent'
    TextControlForeground = 'Text'; TextControlForegroundPointerOver = 'Text'; TextControlForegroundFocused = 'Text'; TextControlPlaceholderForeground = 'Text3'; TextControlPlaceholderForegroundFocused = 'Text3'; TextControlPlaceholderForegroundPointerOver = 'Text3'
    TextControlSelectionHighlightColor = 'Accent'; TextControlHeaderForeground = 'Text2'
    ComboBoxBackground = 'Card'; ComboBoxBackgroundPointerOver = 'BtnHover'; ComboBoxBackgroundPressed = 'Card2'; ComboBoxBackgroundFocused = 'Card'; ComboBoxBorderBrush = 'Stroke2'; ComboBoxBorderBrushPointerOver = 'Text2'; ComboBoxBorderBrushFocused = 'Accent'
    ComboBoxForeground = 'Text'; ComboBoxForegroundFocused = 'Text'; ComboBoxPlaceHolderForeground = 'Text3'; ComboBoxDropDownGlyphForeground = 'Text2'; ComboBoxDropDownBackground = 'Card'; ComboBoxDropDownBorderBrush = 'Stroke2'; ComboBoxHeaderForeground = 'Text2'
    ComboBoxItemForeground = 'Text'; ComboBoxItemForegroundSelected = 'Text'; ComboBoxItemBackgroundSelected = 'AccentSoft'; ComboBoxItemBackgroundSelectedPointerOver = 'AccentSoft'; ComboBoxItemBackgroundPointerOver = 'Card2'; ComboBoxItemPillFillBrush = 'Accent'
    CheckBoxForegroundUnchecked = 'Text2'; CheckBoxForegroundChecked = 'Text2'; CheckBoxForegroundUncheckedPointerOver = 'Text'; CheckBoxForegroundCheckedPointerOver = 'Text'
    CheckBoxCheckBackgroundFillChecked = 'Accent'; CheckBoxCheckBackgroundFillCheckedPointerOver = 'AccentHover'; CheckBoxCheckBackgroundStrokeChecked = 'Accent'; CheckBoxCheckGlyphForegroundChecked = 'AccentText'; CheckBoxCheckGlyphForegroundCheckedPointerOver = 'AccentText'
    CheckBoxCheckBackgroundFillUnchecked = 'Card'; CheckBoxCheckBackgroundStrokeUnchecked = 'Stroke2'; CheckBoxCheckBackgroundStrokeUncheckedPointerOver = 'Text2'
    ContentDialogBackground = 'Card'; ContentDialogForeground = 'Text'; ContentDialogBorderBrush = 'Stroke2'; ContentDialogTopOverlay = 'Card'
    ProgressBarForeground = 'Accent'; ProgressBarBackground = 'Track'; ProgressRingForegroundThemeBrush = 'Accent'
    FlyoutPresenterBackground = 'Card'; FlyoutBorderThemeBrush = 'Stroke2'; MenuFlyoutPresenterBackground = 'Card'; MenuFlyoutPresenterBorderBrush = 'Stroke2'
    MenuFlyoutItemForeground = 'Text'; MenuFlyoutItemForegroundPointerOver = 'Text'; MenuFlyoutItemBackgroundPointerOver = 'Card2'; MenuFlyoutItemBackgroundPressed = 'Card2'; MenuFlyoutItemForegroundDisabled = 'Text3'
    ToolTipBackground = 'Card'; ToolTipForeground = 'Text'; ToolTipBorderBrush = 'Stroke2'
    SystemControlFocusVisualPrimaryBrush = 'Focus'; SystemControlFocusVisualSecondaryBrush = 'AppBg'
    ScrollViewerScrollBarSeparatorBackground = 'Transparent'
    RadioButtonsHeaderForeground = 'Text2'
}

function Escape([string] $value) { [Security.SecurityElement]::Escape($value) }

$builder = New-Object Text.StringBuilder
[void]$builder.AppendLine('<!-- Generated by tools/windows/New-ThemeTokens.ps1 from the AIU-010 design tokens. Edit the script, not this file. -->')
[void]$builder.AppendLine('<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">')
[void]$builder.AppendLine('  <ResourceDictionary.ThemeDictionaries>')
foreach ($theme in @('Light', 'Dark', 'HighContrast')) {
    [void]$builder.AppendLine("    <ResourceDictionary x:Key=`"$theme`">")
    foreach ($name in $tokens.Keys) {
        $values = $tokens[$name]
        if ($theme -eq 'HighContrast') {
            if ($values[2]) { [void]$builder.AppendLine("      <SolidColorBrush x:Key=`"${name}Brush`" Color=`"{ThemeResource $($values[2])}`" />") }
            else { [void]$builder.AppendLine("      <SolidColorBrush x:Key=`"${name}Brush`" Color=`"Transparent`" />") }
        } else {
            $color = if ($theme -eq 'Light') { $values[0] } else { $values[1] }
            [void]$builder.AppendLine("      <SolidColorBrush x:Key=`"${name}Brush`" Color=`"$color`" />")
        }
    }
    if ($theme -ne 'HighContrast') {
        foreach ($alias in $aliases.Keys) {
            $values = $tokens[$aliases[$alias]]
            $color = if ($theme -eq 'Light') { $values[0] } else { $values[1] }
            [void]$builder.AppendLine("      <SolidColorBrush x:Key=`"$alias`" Color=`"$color`" />")
        }
    }
    [void]$builder.AppendLine('    </ResourceDictionary>')
}
[void]$builder.AppendLine('  </ResourceDictionary.ThemeDictionaries>')
[void]$builder.AppendLine('</ResourceDictionary>')
[IO.File]::WriteAllText([IO.Path]::GetFullPath($Output), $builder.ToString(), (New-Object Text.UTF8Encoding($false)))

# The demo's simulated contrast theme is a plain dictionary swapped in by ThemeService (Themes/SimulatedHighContrast.xaml).
$simulatedPath = Join-Path (Split-Path ([IO.Path]::GetFullPath($Output))) 'SimulatedHighContrast.xaml'
$sim = New-Object Text.StringBuilder
[void]$sim.AppendLine('<!-- Generated by tools/windows/New-ThemeTokens.ps1. Demo-only stand-in for a Windows contrast theme (specification section 3 values). -->')
[void]$sim.AppendLine('<!-- Swapped in for Tokens.xaml by ThemeService while the demo simulates high contrast; both themes carry the contrast values. -->')
[void]$sim.AppendLine('<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">')
[void]$sim.AppendLine('  <ResourceDictionary.ThemeDictionaries>')
foreach ($theme in @('Light', 'Dark', 'HighContrast')) {
    [void]$sim.AppendLine("    <ResourceDictionary x:Key=`"$theme`">")
    foreach ($name in $simulated.Keys) { [void]$sim.AppendLine("      <SolidColorBrush x:Key=`"${name}Brush`" Color=`"$($simulated[$name])`" />") }
    foreach ($alias in $aliases.Keys) { [void]$sim.AppendLine("      <SolidColorBrush x:Key=`"$alias`" Color=`"$($simulated[$aliases[$alias]])`" />") }
    [void]$sim.AppendLine('    </ResourceDictionary>')
}
[void]$sim.AppendLine('  </ResourceDictionary.ThemeDictionaries>')
[void]$sim.AppendLine('</ResourceDictionary>')
[IO.File]::WriteAllText($simulatedPath, $sim.ToString(), (New-Object Text.UTF8Encoding($false)))
"Generated $($tokens.Count) tokens and $($aliases.Count) framework aliases."
