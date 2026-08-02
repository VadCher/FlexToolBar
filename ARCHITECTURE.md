# FlexToolBar Architecture Specification

## Project Overview
FlexToolBar is a lightweight, high-performance hybrid between a classic ToolBar and a Ribbon control designed for modern desktop applications (Avalonia 12+, with future targets for WPF/MAUI). It operates purely under MVVM ideology.

## Target Frameworks
- Multi-targeting: `.NET 8.0`, `.NET 9.0`, `.NET 10.0`
- Language Version: `latest` (C# 13/14 features enabled)
- Main Platform: `Avalonia 12.1.x`

## Core Architectural Rules & Pipeline
1. **Separation of Concerns**: 
   - `FlexToolBar.Core`: Pure C# layer (`net8.0;net9.0;net10.0`). Zero UI dependencies. Contains interfaces, state machine, and JSON DTOs.
   - `FlexToolBar.Avalonia`: UI implementation layer (`Avalonia 12`). Contains custom controls, themes, and templating.
2. **Context & Hierarchy**: Standard `DataContext` inheritance is preserved. Elements within groups bind directly to the inherited or explicitly overwritten `DataContext` (e.g., active document from a docking system).
3. **Internal Layout Freedom**: `FlexGroup` is a `ContentControl`. The inner layout (Grid, StackPanel, WrapPanel) is fully defined by the end-user in XAML.

## Component Specifications & Defaults

### 1. ToolBar (Root Control)
- `bool IsSingleExpandGroup` (Default: `false`): If `true`, only one unpinned group can be expanded within the current tab at any given time.
- `ObservableCollection<Tab> Tabs`
- `bool IsTabHeaderVisible` (Read-only): Automatically evaluated (`Tabs.Count > 1`). If `false`, the tab selection strip is completely hidden.
- `ICommand ResetLayoutCommand` (Read-only): MVVM command that resets all groups to their default compiled XAML states.

### 2. Tab
- Represents a collection of `FlexGroup` elements.
- Embedded `ScrollViewer` inside the Tab template handles horizontal overflow and responds to pointer mouse wheel scrolling (`PointerWheelChangedEvent`).

### 3. FlexGroup (Smart Container)
- Acts as a `ContentControl` switching between two main visual states via pseudo-classes:
  - `:collapsed` (`IsExpanded == false`): Rendered as a single large action button containing `Icon` (top) and `Header` (bottom). Clicking toggles `IsExpanded = true`.
  - `:expanded` (`IsExpanded == true`): Hides the large button, displays a left-aligned vertical management column (Pin/Close buttons) and presents user `Content`.
- **Properties & Defaults**:
  - `IsExpanded` (Default: `true`)
  - `IsPinned` (Default: `false`)
  - `PinVisible` (Default: `true`): Controls the visibility of the pin toggle button. If `PinVisible="False"` and `IsPinned="True"`, the group is statically locked and ignores `IsSingleExpandGroup` cycles.
- **Headers Fallback Logic**:
  - `Header`: Display text for the collapsed button.
  - `ExpandedHeader`: Display text for the bottom of the expanded panel. If null, automatically falls back to `Header`. If explicit empty string (`""`), the text block collapses (`IsVisible="False"`), saving vertical space.

## Layout Serialization (JSON DTO)
- No tracking of visual indices or drag-and-drop order. 
- Elements matched via stable string identifiers: `ftb:Tab.TabId` and `ftb:FlexGroup.GroupId`.
- Serializes only state primitives: `SelectedTabId`, `GroupId`, `IsExpanded`, `IsPinned`.
- Layout Manager includes an implicit `ResetToDefault()` mechanism by falling back to compiled XAML defaults or deleting the local JSON state file.
