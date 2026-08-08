# FlexToolBar Architecture Specification

## 1. Project Overview & Architectural Rules

FlexToolBar is a high-performance, lightweight hybrid layout controller designed under strict MVVM ideology for modern desktop and touch-based application interfaces.

### Core Architecture Pipeline
- **Separation of Concerns**: 
  - `FlexToolBar.Core`: Pure cross-platform data structures and state carriers. Zero framework visual dependencies. Highly optimized for in-place `System.Text.Json` `.Populate` mapping.
  - `FlexToolBar.Avalonia`: Implementation layer containing custom controls, interactive templates, and stylesheets.
- **Unified State Modeling (O(1) Architecture)**: Tab collection structures are managed statically at the XAML composition layer. The core tracking layer ignores nested visual tree iterations, isolating all running group states inside a globally accessible flat identity registry mapping layout parameters atomically via a unique `GroupId` dictionary key.

## 2. Component Specifications & Defaults (XAML/UI)

### ToolBar (Root Control)
- **The Structural Placement Mandate (DockPanel Law):** To guarantee pixel-perfect responsive alignment across touch targets, the `ToolBar` control must always be hosted directly inside a native `DockPanel` container at the view layer.
  ```xml
  <DockPanel>
      <local:ToolBar DockPanel.Dock="{Binding PanelEdge}" />
  </DockPanel>
  ```
- `FlexToolBarViewModel ViewModel` (Internal Registry Core): The single source of truth driving the component configuration.
- `double ToolBar.GroupSpacing` (Attached Property, Default: `6.0`): Visually drives layout padding gaps. Registered with visual tree inheritance enabled (`inherits: true`).
- `global::Avalonia.Controls.Dock ToolBar.PanelEdge` (Styled Property, Default: `Dock.Top`): Toggles screen edge attachment context.
- `ICommand ResetLayoutCommand` (UI Internal Command): Executes direct hardware asset resets utilizing the framework's internal `ClearValue` mechanisms. Property values seamlessly cascade back into the core model registry via active TwoWay data bindings.

### Tab
- Represents a structural container of `FlexGroup` instances. Inherits from `HeaderedItemsControl`.
- **Naked Content Carrier**: Stripped of internal scroll managers and outer boundary metrics. Internal elements panel implements a horizontal `StackPanel` with an explicit spacing constraint locked to `0`.
- `string TabId` (Attached Property): Unique identifier pairing the static XAML tab layout with localized group state boundaries.

### FlexGroup (Smart Container)
- Acts as a `ContentControl` switching between two main visual states via pseudo-classes:
  - `:collapsed` (`IsExpanded == false`): Rendered as a single large action button containing `Icon` (top) and `Header` (bottom). Clicking toggles `IsExpanded = true`.
  - `:expanded` (`IsExpanded == true`): Hides the large button, presents user `Content`, and shows top-left pinning controls.
- `string GroupId` (Required Identity Property): Symmetrically pairs the visual control instance with its tracked core model inside the global dictionary registry.
- **Properties & Defaults**:
  - `SeparatorTemplate` (Default: `null`): Allows custom themes to completely redefine the inner visual content and layout style of the group's left spacing separator. Defaults to an empty transparent `Border` "out-of-the-box", converting the gap into a fully themeable Ribbon boundary asset.
  - `IsExpanded` (Default: `true`, TwoWay binding mode).
  - `IsPinned` (Default: `false`): When `IsPinned == true`, the group becomes non-collapsible. The close action is suppressed, and `PART_CloseButton` is hidden via XAML pseudo-classes.
  - `PinVisible` (Default: `true`): Controls the visibility of the pin toggle button.
- **Headers Fallback Logic**:
  - `Header`: Display text for the collapsed button.
  - `ExpandedHeader`: Display text for the bottom of the expanded panel. If null, automatically falls back to `Header`. If explicit empty string (`""`), the text block collapses (`IsVisible="False"`), saving vertical space.

## 3. Layout Lifecycle & Serialization Pipeline

1. **Phase Separation Engine**: During the control's boot phase (XAML parsing), visual UI components (`FlexGroup`) initialize their internal concrete `FlexGroupViewModel` instances locally, caching compile-time XAML literal values and stylesheet default metrics natively.
2. **Top-Down Registry Assembly**: The true orchestration handshake executes strictly inside the root **`OnLoaded`** method of the `ToolBar` control before the configuration file is read. The master `ToolBar` iterates through its static `Tabs` collection, discovers all child `FlexGroup` containers, injects the corresponding `TabId` markers, and registers their live `GroupViewModel` references directly into the global flat dictionary registry via unique `GroupId` keys.
3. **In-Place Population & IsEdited Reset**: Immediately following assembly, the layout configuration JSON file is processed via the native .NET `JsonObjectCreationHandling.Populate` pipeline. Property mutations stream directly onto the active bound core models. Upon successful completion of this layout generation sequence, `ViewModel.ResetIsEdited()` is explicitly invoked to flush false-positive tracking flags caused by file deserialization steps, establishing a clean operational baseline (`IsEdited == false`).
4. **First-Boot Forced Baseline**: If the physical JSON configuration file does not exist on disk (cold startup phase), the serialization engine instantly forces an explicit execution of `GetLayoutJson()`. The compiled XAML defaults are instantly written to disk as an absolute blueprint snapshot, followed by an immediate execution of `ResetIsEdited()`.
5. **Crash Resilience (Debounced Auto-Save)**: Built-in background serialization driven by a native `DispatcherTimer` running at `DispatcherPriority.Background`.
   - Activates reactively only when the core view model fires an explicit property notification for the global `IsEdited` state flag.
   - Uses a strict **Debounce pattern** via `Stop()/Start()` sequence to group rapid user interaction cycles and minimize SSD write wear.
   - Flushes state primitives to disk after a cooling interval defined by `AutoSaveInterval` (Default: 5 seconds). Setting this property to `TimeSpan.Zero` completely disables the background timer.

## 4. Core Execution Models (`FlexToolBar.Core`)

### ViewModelBase
Provides property notification triggers optimized for modern C# development semantics.
- `bool IsEdited` (Read-only): Automatically flipped to `true` whenever any underlying state mutation executes via the core `RaiseAndSetIfChanged` assignment engine. Fires explicit `OnPropertyChanged` notifications upon both activation and successful persistence resets to drive external responsive UI indicators (saving status icons, explicit save buttons availability) natively.

### FlexToolBarViewModel
The master cross-platform flat container coordinating live library metric snapshots. Completely stripped of abstract UI-interface wrappers and heavy nested tab-collection tracking events.
- `Dictionary<string, FlexGroupViewModel> Groups`: Flat high-performance state catalog pairing unique `GroupId` keys with live group instances for `O(1)` accessibility.
- `bool IsSingleExpandGroup` (Default: `false`): Property setter intercepts activation mutations to instantly execute a localized, flat boundary collapsing sequence across the group registry.

### FlexGroupViewModel
Tracked container carrying state variables for a single user group instance.
- `string TabId`: Localized runtime string boundary marker allowing isolated single-expand mutations. Excluded from disk serialization routines via explicit `[JsonIgnore]` constraints.
- `bool IsExpanded` (Default: `true`): Setter logic intercepts execution steps to perform localized group collapses restricted within matching `TabId` scopes without subscribing to verbose external property changed listeners.
- `bool IsPinned` (Default: `false`): Prevents automatic or layout-driven structural collapsing routines.

## 5. Styling, Themes & Extensible Assets

Every control in `FlexToolBar` is a `TemplatedControl`, meaning its look and feel is completely decoupled from logic. The base template styles (`FlexToolBar.axaml`) contain only structural grid skeletal definitions and logical bindings.

### Theme Architecture Rules
- **Zero Hardcoded Spacing**: Standalone visual properties like `Margin`, `Padding`, `FontSize`, and `MinHeight` must not be hardcoded inside base control templates. They must use template bindings (`Padding="{TemplateBinding Padding}"`) or be defined strictly inside dedicated theme files within the `Themes/` directory.
- **Autonomous Collapsing**: Layout grids inside components must use `Auto` dimensions for control columns (`ColumnDefinitions="Auto,*"`). Combined with `IsVisible="False"`, this ensures that control panels collapse completely, resetting margins to absolute zero when buttons are hidden.
- **App-Level Theming**: The core library file `FlexToolBar.axaml` includes only essential layout mechanics. The app developer explicitly opts into a specific visual appearance by attaching a layout theme file (e.g., `Themes/Compact.Styles.axaml`) inside their `App.axaml` stylesheet scope.
- **Direct Template Targetry**: Custom theme files manipulate internal named template parts (e.g., `ToggleButton#PART_PinButton`) using direct template scope selectors (`/template/`), leaving the core C# layout classes decoupled from granular pixel properties.

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
- `PART_Separator` (`ContentControl`) — Layout spacing separator configured per individual `FlexGroup`.

### Group Separator Pipeline
- **First-Child Kill-Switch:** The leftmost separator control within a tab collection context automatically forces its width to zero and hides itself via a native `:nth-child(1)` compiler selector. This guarantees pristine vertical alignment with the static tab header rails out-of-the-box, while all subsequent groups cleanly present their custom templates.

### Navigation Assets Injection API
- **ScrollLeftContent & ScrollRightContent** (Type: `object`, Default: `"◀"` / `"▶"`): Declared at the root `ToolBar` dependency property registry. Allows styling engines to seamlessly inject vector geometry (`Path`), custom SVG graphics, or stylized Unicode glyphs without touching the base source templates.
- **Elastic Default Button Bounds**: Core navigation buttons inside `ToolBar.Styles.axaml` completely erase explicit width boundaries (`Width="Auto"`). They utilize a default style container specifying `Padding="4,0,4,0"`, which forces the button framework border to dynamically stretch or shrink to safely hug the shape of any injected glyph context natively.
