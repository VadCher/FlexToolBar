# FlexToolBar Architecture Specification

## Project Overview
FlexToolBar is a lightweight, high-performance hybrid between a classic ToolBar and a Ribbon control designed for modern desktop applications (Avalonia 12+, with future targets for WPF/MAUI). It operates purely under MVVM ideology.

## Target Frameworks
- Multi-targeting: `.NET 8.0`, `.NET 9.0`, `.NET 10.0`
- Language Version: `latest` (C# 13/14 features enabled)
- Main Platform: `Avalonia 12.1.x`

## Core Architectural Rules & Pipeline
1. **Separation of Concerns**: 
   - `FlexToolBar.Core`: Pure C# layer. Zero UI dependencies. Contains interfaces, state machine, and JSON DTOs.
   - `FlexToolBar.Avalonia`: UI implementation layer (`Avalonia 12`). Contains custom controls, themes, and templates.
2. **Context & Hierarchy**: Standard `DataContext` inheritance is preserved. Elements within groups bind directly to the inherited or explicitly overwritten `DataContext` (e.g., active document from a docking system).
3. **Internal Layout Freedom**: `FlexGroup` is a `ContentControl`. The inner layout (Grid, StackPanel, WrapPanel) is fully defined by the end-user in XAML.
4. **Resource Names**: To prevent Avalonia 12 XAMLIL compiler conflicts with C# class types, all standalone control templates must use the `.Styles.axaml` file extension suffix.

## Component Specifications & Defaults

### 1. ToolBar (Root Control)
- `double ToolBar.GroupSpacing` (Attached Property, Default: `6.0`): **The paramount layout controller of the entire library.** Registered with visual tree inheritance enabled (`inherits: true`). This single `double` value leaks down the entire tree hierarchy to drive the synchronized layout rhythm across arrows, tabs, and inner groups natively. Changing this single value scales the entire toolbar gaps proportionally.
- `bool IsSingleExpandGroup` (Default: `false`): If `true`, only one unpinned group can be expanded within the current tab at any given time. Coordinates state changes via the Logical Tree.
- `bool RestoreSelectedTab` (Default: `false`): Controls whether the toolbar restores the last active tab workspace on startup. If `false`, it always defaults to the first available XAML tab.
- `ObservableCollection<Tab> Tabs`
- `bool IsTabHeaderVisible` (Read-only): Automatically evaluated (`Tabs.Count > 1`). If `false`, the tab selection strip is completely hidden.
- `ICommand ResetLayoutCommand` (Read-only): Autonomous library command. Deletes the physical JSON file from disk, resets `SelectedIndex` to 0, and forces all controls to gracefully fall back to their compiled XAML defaults via internal caching.
- `string? AutoSaveId` (Default: `null`): Unique configuration identifier for automated layout persistence. Enables complete zero-code operation.
- `void RefreshLayout()`: Public API method. Forces the toolbar to instantly re-read and apply configurations from the physical JSON layout file.
- **Root Structural Framework (Nested Grid Infrastructure)**: The global layout orchestration is split into two logical layers inside the `ToolBar` template to cleanly isolate global tab definitions from active layout presentation spaces:
  ```xml
  <!-- Primary Structural Skeleton Frame -->
  <Grid RowDefinitions="Auto,*" VerticalAlignment="Stretch" HorizontalAlignment="Stretch">
      <!-- Row 0: Hosts PART_HeaderContainer with the native TabStrip -->
      <!-- Row 1: Hosts PART_ContentContainer wrapping the single-row navigation Grid -->
  </Grid>
  ```
  * **Row 0 (Tab Header Workspace Layer):** Hosts `PART_HeaderContainer` (`Border`). This contains the global `TabStrip` navigation manager (`PART_TabSelectionStrip`) operating natively on a single flat surface. It provides static, wobble-free tab header positioning that never jitters or slides during content scrolling operations.
  * **Row 1 (Active Ribbon Plate Layer):** Hosts `PART_ContentContainer` (`Border`). This border acts as the single styleable background and frame chassis for tool groups. Inside this container, a standalone single-row, 7-column layout engine coordinates touch navigation:
    ```xml
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="Auto"/> <!-- 0: Fixed Left Edge Application Padding -->
        <ColumnDefinition Width="Auto"/> <!-- 1: Left Arrow Button Context -->
        <ColumnDefinition Width="Auto"/> <!-- 2: Dynamic Inner Left Spacing Gap -->
        <ColumnDefinition Width="*"/>    <!-- 3: Global Main Content Scroll Viewport -->
        <ColumnDefinition Width="Auto"/> <!-- 4: Dynamic Inner Right Spacing Gap -->
        <ColumnDefinition Width="Auto"/> <!-- 5: Right Arrow Button Context -->
        <ColumnDefinition Width="Auto"/> <!-- 6: Fixed Right Edge Application Padding -->
    </Grid.ColumnDefinitions>
    ```
    * **Column 3 Viewport Bounds:** The central `ScrollViewer` (`PART_TabScrollViewer`) and active `ContentPresenter` are cleanly isolated inside Column 3. When navigation arrow columns (Column 1 or 5) collapse to zero width, adjacent inner spacing gap borders (Column 2 or 4) reactively collapse as well, ensuring that the remaining tool items smoothly scale and float back to touch the respective outer padding lines symmetrically.

### 2. Tab
- Represents a collection of `FlexGroup` elements. Inherits from `HeaderedItemsControl`.
- **Naked Content Carrier**: The `Tab` control is completely stripped of internal scroll containers, navigation arrow buttons, and outer layout framing metrics.
- **Items Panel Rule**: The internal items presenter template uses a horizontal `StackPanel` where the explicit spacing is completely reset to `0`. All visual distance and logical gaps between individual groups are now driven 100% strictly by the internal group layout separators.
  ```xml
  <StackPanel Orientation="Horizontal" Spacing="0"/>
  ```
- `ftb:Tab.TabId` (Attached Property): Unique string identifier for layout serialization.

### 3. FlexGroup (Smart Container)
- Acts as a `ContentControl` switching between two main visual states via pseudo-classes:
  - `:collapsed` (`IsExpanded == false`): Rendered as a single large action button containing `Icon` (top) and `Header` (bottom). Clicking toggles `IsExpanded = true`.
  - `:expanded` (`IsExpanded == true`): Hides the large button, presents user `Content`, and shows top-left pinning controls.
- `ftb:FlexGroup.GroupId` (Attached Property): Unique string identifier for layout serialization.
- **Properties & Defaults**:
  - `SeparatorTemplate` (Default: `null`): Allows custom themes to completely redefine the inner visual content and layout style of the group's left spacing separator. Defaults to an empty transparent `Border` "out-of-the-box", converting the gap into a fully themeable Ribbon boundary asset.
  - `IsExpanded` (Default: `true`, TwoWay binding mode).
  - `IsPinned` (Default: `false`): When `IsPinned == true`, the group becomes non-collapsible. The close action is suppressed, and `PART_CloseButton` is hidden via XAML pseudo-classes.
  - `PinVisible` (Default: `true`): Controls the visibility of the pin toggle button.
- **Headers Fallback Logic**:
  - `Header`: Display text for the collapsed button.
  - `ExpandedHeader`: Display text for the bottom of the expanded panel. If null, automatically falls back to `Header`. If explicit empty string (`""`), the text block collapses (`IsVisible="False"`), saving vertical space.

## Layout Lifecycle & Serialization (JSON DTO)
1. **Phase Separation Engine**: During the control's boot phase (`OnAttachedToVisualTree`), all original XAML markup definitions are cached inside internal fields (`_xamlDefaultIsSingleExpand`, `_xamlDefaultIsExpanded`, etc.).
2. **File Loading Sequence**: The configuration JSON file is applied strictly inside the **`OnLoaded`** method via `RefreshLayout()`. This guarantees that file values safely override layout states without damaging or wiping the initial compiled XAML cache.
3. **Crash Resilience (Debounced Auto-Save)**: Built-in background serialization driven by a native `DispatcherTimer` running at `DispatcherPriority.Background`. 
   - Activates automatically upon any runtime mutations of properties (`IsExpanded`, `IsPinned`, or active tab switches).
   - Uses a strict **Debounce pattern** via `Stop()/Start()` sequence to group rapid user interaction cycles and minimize SSD write wear.
   - Flushes state primitives to disk after a cooling interval defined by `AutoSaveInterval` (Default: 5 seconds). Setting this property to `TimeSpan.Zero` completely disables the background timer.
4. **State Payload**: Matches elements via stable string identifiers (`TabId` and `GroupId`). Serializes only primitive state values: `SelectedTabId`, `IsSingleExpandMode`, `GroupId`, `IsExpanded`, `IsPinned`.

## 4. Styling & Customization Guide (XAML)
Every control in `FlexToolBar` is a `TemplatedControl`, meaning its look and feel is completely decoupled from logic. The base template styles (`FlexToolBar.axaml`) contain only structural grid skeletal definitions and logical bindings.

### Theme Architecture Rules
1. **Zero Hardcoded Spacing**: Standalone visual properties like `Margin`, `Padding`, `FontSize`, and `MinHeight` must not be hardcoded inside base control templates. They must use template bindings (`Padding="{TemplateBinding Padding}"`) or be defined strictly inside dedicated theme files within the `Themes/` directory.
2. **Autonomous Collapsing**: Layout grids inside components must use `Auto` dimensions for control columns (`ColumnDefinitions="Auto,*"`). Combined with `IsVisible="False"`, this ensures that control panels collapse completely, resetting margins to absolute zero when buttons are hidden.
3. **App-Level Theming**: The core library file `FlexToolBar.axaml` includes only essential layout mechanics. The app developer explicitly opts into a specific visual appearance by attaching a layout theme file (e.g., `Themes/Compact.Styles.axaml`) inside their `App.axaml` stylesheet scope.
4. **Direct Template Targetry**: Custom theme files manipulate internal named template parts (e.g., `ToggleButton#PART_PinButton`) using direct template scope selectors (`/template/`), leaving the core C# layout classes decoupled from granular pixel properties.

### Visual States (Pseudo-classes)
- `:expanded` — Active when `IsExpanded == true`. Renders the full control layout.
- `:collapsed` — Active when `IsExpanded == false`. Renders the group as a single large action button.
- `:pinned` — Active when `IsPinned == true`. Modifications apply to the pinning indicator state and hide close buttons.

### Standard Template Parts (Targetable via XAML Name)
- `PART_CollapsedButton` (`Button`) — Root wrapper visible only in the `:collapsed` state.
- `PART_ExpandedContainer` (`Border`) — Outer border surrounding content only in the `:expanded` state.
- `PART_PinButton` (`ToggleButton`) — Pin/unpin interface element.
- `PART_CloseButton` (`Button`) — Collapse/close action element.
- `PART_BottomHeaderBlock` (`TextBlock`) — Renders `ExpandedHeader` at the bottom of the group.
- `PART_TabSelectionStrip` (`TabStrip`) — Global tab switching bar located at the root ToolBar level.
- `PART_TabScrollViewer` (`ScrollViewer`) — Global horizontal viewport framework carrying active presenters.
- `PART_ScrollLeftButton` (`Button`) — Touch-friendly left navigation snap action element.
- `PART_ScrollRightButton` (`Button`) — Touch-friendly right navigation snap action element.
- `PART_Separator` (`ContentControl`) — Layout spacing разделитель на уровне каждой группы FlexGroup.

---

## 5. Extensible Layout Assets & Theme Sovereignty

Every navigation asset and layout placeholder inside the library complies with the strict "Zero Hardcoded Spacing" policy. Granular structural boundaries are exposed directly to the XAML compilation engine via properties, ensuring complete theme override independence.

### Group Separator Pipeline:
* **First-Child Kill-Switch:** The leftmost separator control within a tab collection context automatically forces its width to zero and hides itself via a native `:nth-child(1)` compiler selector. This guarantees pristine vertical alignment with the static tab header rails out-of-the-box, while all subsequent groups cleanly present their custom templates.

### Navigation Assets Injection API:
* **ScrollLeftContent & ScrollRightContent** (Type: `object`, Default: `"◀"` / `"▶"`): Declared at the root `ToolBar` dependency property registry. Allows styling engines to seamlessly inject vector geometry (`Path`), custom SVG graphics, or stylized Unicode glyphs without touching the base source templates.
* **Elastic Default Button Bounds**: Core navigation buttons inside `ToolBar.Styles.axaml` completely erase explicit width boundaries (`Width="Auto"`). They utilize a default style container specifying `Padding="4,0,4,0"`, which forces the button framework border to dynamically stretch or shrink to safely hug the shape of any injected glyph context natively.
