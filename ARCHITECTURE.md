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
- **The Seamless Tab Persistence Mandate (SelectedValue Law):** To guarantee that the active workspace context never shifts or drops to index zero during destructive runtime theme stylesheet hot-swapping transactions, the template-driven `TabStrip` MUST bind its active state declaratively using the native string identity pipeline:
  ```xml
  <TabStrip x:Name="PART_TabSelectionStrip"
            ItemsSource="{TemplateBinding Tabs}"
            SelectedValueBinding="{Binding (ftb:Tab.TabId)}"
            SelectedValue="{Binding ViewModel.SelectedTabId, RelativeSource={RelativeSource TemplatedParent}, Mode=TwoWay}" />
  ```

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
2. **Top-Down Registry Assembly**: The true orchestration handshake executes strictly inside the root `OnLoaded` method of the `ToolBar` control before the configuration file is read. The master `ToolBar` iterates through its static `Tabs` collection, discovers all child `FlexGroup` containers, injects the corresponding `TabId` markers, links the parent view model references, and registers their live `GroupViewModel` references directly into the global flat dictionary registry via unique `GroupId` keys.
3. **Symmetric Reflection Transmission**: The configuration JSON file is processed inside the `LoadLayout` method via standard `JsonSerializer.Deserialize` into a clean, short-lived isolated memory layout snapshot. Property mutations and state configurations stream back into the active, pre-assembled live object tree matrix utilizing an optimized, single-loop recursive properties reflection carrier (`CopyProperties`).
4. **The Enumerable KeyValuePair Alignment Rule**: To strictly protect and maintain live framework UI pointer references in memory, the reflection engine ignores generic collection overrides. It processes structural boundaries through a unified parallel `IEnumerable` iterator. If an encountered node implements the explicit generic layout contract signature of `KeyValuePair<TKey, TValue>`, the engine automatically extracts the internal `Value` property contexts and drops down recursively to perform an in-place mutation of primitive state flags, securing a Zero Garbage Collection runtime footprint.
5. **Proactive Invalidation Safeguard**: To shield the platform against infinite auto-save disk loop cycles caused by rapid deserialization field mutations or unexpected file-system blocks, an explicit invocation of `ViewModel.ResetIsEdited()` is strictly forced at the absolute entry boundary of the loading transaction, immediately followed by a secondary flush call upon successful data transmission.
6. **First-Boot Forced Baseline**: If the physical JSON configuration file does not exist on disk (cold startup phase), the serialization engine instantly forces an explicit layout snapshot execution to disk as an absolute blueprint, followed by an immediate execution of `ResetIsEdited()`.
7. **Crash Resilience (Debounced Auto-Save)**: Built-in background serialization driven by a native `DispatcherTimer` running at `DispatcherPriority.Background`.
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

Every control in `FlexToolBar` is a `TemplatedControl` configured via modern `ControlTheme` dictionaries, meaning its look and feel is completely decoupled from logic. The base themes contain only structural grid skeletal definitions and logical bindings, ensuring `DataContext` remains untouched for application developers [1.1].

### Theme Architecture Rules
- **Zero Hardcoded Spacing**: Standalone visual properties like `Margin`, `Padding`, `FontSize`, and `MinHeight` must not be hardcoded inside base control templates. They must use template bindings (`Padding="{TemplateBinding Padding}"`) or be defined strictly inside dedicated theme files within the `Themes/` directory [1.1].
- **Autonomous Collapsing**: Layout grids inside components must use `Auto` dimensions for control columns (`ColumnDefinitions="Auto,*"`). Combined with `IsVisible="False"`, this ensures that control panels collapse completely, resetting margins to absolute zero when buttons are hidden [1.1].
- **App-Level Theming**: The core library file `FlexToolBar.axaml` is a root `ResourceDictionary` linking concrete component `ControlTheme` assets. The app developer explicitly opts into a specific visual appearance by attaching a layout theme file inside their `App.axaml` stylesheet scope [1.1].
- **Direct Template Targetry**: Custom theme files manipulate internal named template parts using nested template scope selectors (`^ /template/`), leaving the core C# layout classes decoupled from granular pixel properties [1.1].

### Visual States (Pseudo-classes)
- `:expanded` — Active when `IsExpanded == true`. Renders the full control layout [1.1].
- `:collapsed` — Active when `IsExpanded == false`. Renders the group as a single large action button [1.1].
- `:pinned` — Active when `IsPinned == true`. Modifications apply to the pinning indicator state [1.1].
- `:hidden` — Active when the control's `IsVisible` property drops to `false`, forcing adjacent separators to collapse layout metrics completely [1.1].

### Standard Template Parts (Targetable via XAML Name)
- `PART_CollapsedButton` (`Button`) — Root wrapper visible only in the `:collapsed` state [1.1].
- `PART_ExpandedBorder` (`Border`) — Outer border surrounding content only in the `:expanded` state [1.1].
- `PART_PinButton` (`Button`) — Adaptive pin/unpin interface button element [1.1].
- `PART_CloseButton` (`Button`) — Collapse/close action element [1.1].
- `PART_BottomHeaderBlock` (`TextBlock`) — Renders `ExpandedHeader` at the bottom of the group [1.1].
- `PART_TabSelectionStrip` (`TabStrip`) — Global tab switching bar driven by type-safe `SelectedValue` data bindings [1.1].
- `PART_TabScrollViewer` (`ScrollViewer`) — Global horizontal viewport framework carrying active presenters [1.1].
- `PART_ScrollLeftButton` (`Button`) — Touch-friendly left navigation snap action element [1.1].
- `PART_ScrollRightButton` (`Button`) — Touch-friendly right navigation snap action element [1.1].
- `PART_Separator` (`ContentControl`) — Trailing layout spacing separator configured per individual `FlexGroup` [1.1].

### Group Separator Pipeline
- **Adaptive Trailing Spacing:** Individual `FlexGroup` containers manage layout gaps using a right-side trailing `PART_Separator` [1.1]. If a control triggers the `:hidden` pseudo-class via business logic, its custom separator width automatically collapses to absolute zero natively, allowing the adjacent group to seamlessly slide left and align perfectly with the static left padding boundary [1.1].

### Navigation Assets Injection API
- **ScrollLeftContent & ScrollRightContent** (Type: `object`, Default: `"◀"` / `"▶"`): Declared at the root `ToolBar` dependency property registry [1.1]. Allows styling engines to seamlessly inject vector geometry (`Path`), custom SVG graphics, or stylized Unicode glyphs without touching the base source templates [1.1].
- **Elastic Default Button Bounds**: Core navigation buttons inside `ToolBar.Styles.axaml` completely erase explicit width boundaries (`Width="Auto"`). They utilize a default style container specifying `Padding="4,0,4,0"`, which forces the button framework border to dynamically stretch or shrink to safely hug the shape of any injected glyph context natively [1.1].

## 6. Resource Dictionary & Hybrid Theme Swapping Architecture

To eliminate layout compilation conflicts and maximize rendering performance while keeping active tab navigation rock-solid, the architecture implements a **Hybrid Swapping Pipeline** combining static resource dictionaries with reactive styles cascading engines [1.1].

### 1. The Pure Monolithic Dictionary Mandate
The core library entry file `FlexToolBar.axaml` MUST be implemented strictly as a naked `ResourceDictionary` [1.1]. It operates as a consolidated cross-assembly resource gateway using clean `ResourceInclude` nodes to publish default themes into the global application scope:
```xml
<ResourceDictionary xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <ResourceDictionary.MergedDictionaries>
        <ResourceInclude Source="avares://FlexToolBar.Avalonia/FlexGroup.Styles.axaml" />
        <ResourceInclude Source="avares://FlexToolBar.Avalonia/Controls/ToolBarSettingsFlexGroup.Styles.axaml" />
        <ResourceInclude Source="avares://FlexToolBar.Avalonia/Controls/FlexButton.Styles.axaml" />
        <ResourceInclude Source="avares://FlexToolBar.Avalonia/Controls/FlexToggleButton.Styles.axaml" />
        <ResourceInclude Source="avares://FlexToolBar.Avalonia/Tab.Styles.axaml" />
        <ResourceInclude Source="avares://FlexToolBar.Avalonia/ToolBar.Styles.axaml" />

    </ResourceDictionary.MergedDictionaries>
</ResourceDictionary>
```

### 2. Explicit Inheritance Coupling (StaticResource Law)
Derived out-of-the-box system components (`ToolBarSettingsFlexGroup`) must inherit the exact structural templates and interactive visual states of the base container by declaring a type-safe `BasedOn` relationship using compile-time lookups:
```xml
<ControlTheme x:Key="{x:Type ftb:ToolBarSettingsFlexGroup}" 
              TargetType="ftb:ToolBarSettingsFlexGroup" 
              BasedOn="{StaticResource {x:Type ftb:FlexGroup}}" />
```

### 3. The Protected DataContext Encapsulation Principle
Every out-of-the-box system component must guarantee absolute immunity against external data context overrides [1.1]. The template layout grid forces a local data context redirection loop back into the master workspace core safely within the isolated `DataTemplate` boundary scope, keeping the host element context unpolluted [1.1]. All underlying property mutations execute via type-safe root tree bindings:
```xml
Value="{Binding ViewModel.GroupSpacing, RelativeSource={RelativeSource AncestorType=ftb:ToolBar}, Mode=TwoWay}"
```

### 4. Static Singleton Theme Registry (`ToolBarThemeManager`)
To completely bypass OS-specific binary layer scanning constraints under Linux/Ubuntu environments, theme asset discovery completely abandons implicit directory walking [1.1]. All valid assets map explicitly into an isolated, memory-resident static dictionary registry pairing unique string tokens with type-safe `Uri` resource links:
- Pre-installed core library themes (`Compact`, `Green`) seed the manager automatically during static constructor initialization routines [1.1].
- Application developers are provided a clean, explicit public API to register custom assets dynamically from the host assembly scope at any stage of the lifecycle:
  ```csharp
  ToolBarThemeManager.RegisterTheme("Light.Dark", "avares://Notepad/Themes/Light.Dark.ToolBar.Theme.axaml");
  ```

### 5. Cascading Overrides Sheets (`<Styles>`)
Dynamic modifier packages (e.g. `Compact.ToolBar.Theme.axaml`) MUST be implemented using clean, flat `<Styles>` collection containers instead of resource dictionaries [1.1]. This architecture completely eliminates `ControlTheme` cyclic inheritance deadlocks and allows micro-stylesheets to override operational look-and-feel properties using pure type selectors without declaring redundant global dictionary resource keys [1.1]:
```xml
<Styles xmlns="https://github.com" xmlns:ftb="clr-namespace:FlexToolBar.Avalonia">
    <Style Selector="ftb|FlexGroup">
        <Setter Property="Padding" Value="2,1,2,1"/>
    </Style>
</Styles>
```

### 6. The Atomic Style Swapping Transaction Contract
Runtime theme switching executes within the `ToolBar.cs` orchestration layer by manipulating the local reactive `this.Styles` collection. The process follows a strict transaction sequence:
- **Wipe Phase**: Executing `this.Styles.Clear()` instantly washes away the custom style sheet layer, forcing the Avalonia engine to fall back onto the global unified application resources template baseline with zero layout creation overhead [1.1].
- **Default Check**: If the target state is `"Default"`, the thread immediately returns, concluding the transaction with optimal processor cycles efficiency [1.1].
- **Inject Phase**: The requested theme name extracts its target `Uri` from the static memory registry at `O(1)` performance [1.1]. The path encapsulates inside a native `StyleInclude` token and injects into the active local styles collection, forcing an instantaneous, ripple-free visual tree layout recalculation while the active tab remains safely frozen in place [1.1].
