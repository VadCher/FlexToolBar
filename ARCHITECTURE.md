# FlexToolBar Architecture Specification

## 1. Project Overview & Architectural Rules

FlexToolBar is a high-performance, lightweight hybrid layout controller designed under strict MVVM ideology for modern desktop and touch-based application interfaces.

### Core Architecture Pipeline
- **Separation of Concerns**: 
  - `FlexToolBar.Core`: Pure cross-platform data structures and state carriers. Zero framework visual dependencies. Fully decoupled from specific UI assemblies.
  - `FlexToolBar.Avalonia`: Implementation layer containing custom controls, interactive templates, and stylesheets.
- **Unified State Modeling (O(1) Architecture)**: Tab collection structures are managed statically at the XAML composition layer. The core tracking layer ignores nested visual tree iterations, isolating all running group states inside a globally accessible flat identity registry mapping layout parameters atomically via a unique `GroupId` dictionary key.

## 2. Component Specifications & Defaults (XAML/UI)

### ToolBar (Root Control)
- **The Structural Placement Mandate (DockPanel Law):** To guarantee pixel-perfect responsive alignment across touch targets, the `ToolBar` control must always be hosted directly inside a native `DockPanel` container at the view layer. Supports complete multi-window, multi-bar круговые CAD-конфигурации.
  ```xml
  <DockPanel>
      <local:ToolBar ToolBarId="CadTopToolbar" PanelEdge="Top" />
      <local:ToolBar ToolBarId="CadLeftToolbar" PanelEdge="Left" />
  </DockPanel>
  ```
- `FlexToolBarViewModel ViewModel` (Internal Registry Core): The single source of truth driving the component instance configuration. Loaded lazily via unique `ToolBarId`.
- `double ToolBar.GroupSpacing` (Attached Property, Default: `6.0`): Visually drives layout padding gaps. Registered with visual tree inheritance enabled (`inherits: true`).
- `global::Avalonia.Controls.Dock ToolBar.PanelEdge` (Styled Property, Default: `Dock.Top`): Toggles screen edge attachment context (Top, Bottom, Left, Right).
- `ICommand ResetLayoutCommand` (UI Internal Command): Bound directly to the core manager instance: `new MiniRelayCommand(() => FlexLayoutManager.DeleteLayout());`. Completely bypasses local view-state routines, driving an atomic global configuration wipe.
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
3. **Monolithic Reflection Serialization**: The configuration JSON file represents the entire serialized snapshot of the root manager object (`FlexLayoutManager`), storing both global application-wide primitives (theme name) and individual tab layout catalogs. The loading pipeline executing inside `LoadLayout` reconstructs a temporary configuration blueprint and initiates a **single root-level pass** of the recursive properties reflection engine (`CopyProperties`), safely mutating active live trees in memory without severing UI pointer bindings.
4. **The Enumerable KeyValuePair Alignment Rule**: To strictly protect and maintain live framework UI pointer references in memory, the reflection engine ignores generic collection overrides. It processes structural boundaries through a unified parallel `IEnumerable` iterator. If an encountered node implements the explicit generic layout contract signature of `KeyValuePair<TKey, TValue>`, the engine automatically extracts the internal `Value` property contexts and drops down recursively to perform an in-place mutation of primitive state flags, securing a Zero Garbage Collection runtime footprint.
5. **Proactive Invalidation Safeguard**: To shield the platform against infinite auto-save disk loop cycles caused by rapid deserialization field mutations or unexpected file-system blocks, an explicit invocation of `Instance.ResetIsEdited()` is strictly forced at the absolute entry boundary of the loading transaction, immediately followed by a secondary flush call upon successful data transmission.
6. **First-Boot Forced Baseline**: If the physical JSON configuration file does not exist on disk (cold startup phase), the serialization engine instantly forces an explicit layout snapshot execution to disk as an absolute blueprint, followed by an immediate execution of `ResetIsEdited()`.
7. **Crash Resilience (Debounced Auto-Save)**: Built-in background serialization driven by a native `DispatcherTimer` running at `DispatcherPriority.Background` inside the UI layer.
   - Activates reactively only when the central `FlexLayoutManager.Instance` fires an explicit property notification for the global `IsEdited` state flag.
   - Uses a strict **Debounce pattern** via `Stop()/Start()` sequence to group rapid user interaction cycles and minimize SSD write wear.
   - Flushes state primitives to disk after a cooling interval defined by `AutoSaveInterval` (Default: 5 seconds). Setting this property to `TimeSpan.Zero` completely disables the background timer.

## 4. Core Execution Models (`FlexToolBar.Core`)

### ViewModelBase
Provides property notification triggers optimized for modern C# development semantics.
- `bool IsEdited` (Read-only): Automatically flipped to `true` whenever any underlying state mutation executes via the core `RaiseAndSetIfChanged` assignment engine. Fires explicit `OnPropertyChanged` notifications upon both activation and successful persistence resets to drive external responsive UI indicators (saving status icons, explicit save buttons availability) natively.

### FlexLayoutManager (Central Singleton Core)
The absolute master single source of truth managing application-wide metrics and multi-bar layouts.
- **Hardware-Enforced Singleton Initializer**: Built entirely upon native .NET Type Initializer specifications to eliminate overhead from system tokens and wrapper memory leaks: `public static FlexLayoutManager Instance { get; } = new();`.
- `string ActiveThemeName` (Default: `"Default"`): App-wide global theme controller. Completely isolated from individual bar models. Operates as the root TwoWay target directly synchronized with active `ToolBar` controls.
- `private Dictionary<string, FlexToolBarViewModel> Models { get; set; }`: Flat high-performance state catalog pairing unique `ToolBarId` identifiers with individual runtime toolbars data contexts. Managed securely via explicit `[JsonInclude]` metadata attributes.
- **Atomic Dispatcher-Safe Destruction**: Exposes a public `DeleteLayout()` transaction routine which physically cleanses disk blueprints and sequence-fires a pure, platform-independent cross-assembly notification bridge: `public static event Action? LayoutResetRequested;`.

### FlexToolBarViewModel
The cross-platform flat container coordinating live library metric snapshots for an individual bar ID. Completely stripped of abstract UI-interface wrappers and heavy nested tab-collection tracking events.
- `Dictionary<string, FlexGroupViewModel> Groups`: Flat state catalog pairing unique `GroupId` keys with live group instances for `O(1)` accessibility.
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
<ResourceDictionary xmlns="https://github.com"
                    xmlns:x="http://microsoft.com">

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

### 6. The Hardened Active-Theme Cascade Contract
The global application theme `ActiveThemeName` is hosted directly at the `FlexLayoutManager.Instance` root layer [1.1]. 
- **The Direct Binding Rule (Variant C):** To completely bypass view-model routing and prevent visual memory leaks, individual `ToolBar` instances bind their local `ActiveThemeNameProperty` directly to the singleton manager using native `TwoWay` bindings [1.1].
- **Atomic Theme Execution:** Changes instantly filter through a static `AddClassHandler`, invoking the optimized `ApplyThemeDirect` routing routine [1.1]. The local `this.Styles` collection is transactionally wiped via `.Clear()`, falling back instantly onto the raw application baseline before appending a newly instantiated `StyleInclude` token mapped at `O(1)` from the theme registry URI [1.1].

### 7. The Hybrid Queue-Driven Invalidation Rule (Reset Engine)
To support infinite круговые multi-bar CAD макеты layout topologies without memory corruption, recursive execution traps, or lock фризы, resets follow a strict **Hybrid Event/Queue Handshake Pipeline** [1.1]:
1. **The Entry Intercept:** Triggering `ResetLayoutCommand` on ANY visible bar routes execution strictly in-place to the root core, firing `FlexLayoutManager.DeleteLayout()` to destroy configuration footprints on disk [1.1].
2. **The Platform-Independent Broadcast:** The core manager fires a standard .NET `LayoutResetRequested` multicast delegate sequential wave [1.1].
3. **The View-Thread Marshalling:** Individual live `ToolBar` instances intercept the broadcast signal inside a dedicated `OnLayoutResetRequested` event handler [1.1]. Instead of forcing instant inline mutations, they push tasks directly into the framework operation stream via `Dispatcher.UIThread.Post(ResetToDefaultLayout, DispatcherPriority.Background);`, closing the click packet transaction without micro-lags [1.1].
4. **The Adaptive Native Fallback:** When a task unqueues, the view executes `this.ClearValue()` against its key dependency properties [1.1]. The framework instantly reverts metrics back to compile-time XAML defaults specified by the layout developer [1.1]. The alive `TwoWay` data bindings automatically capture these default metrics and stream values back to synchronize the active registry state without cyclic collisions [1.1].
### 8. The State-Machine Handshake Protocol & JIT Model Allocation
To guarantee absolute memory isolation, prevent runtime lifecycle race conditions, and preserve compile-time XAML defaults during the initialization phase, the framework completely abandons procedural reflection mutations in favor of an explicit Just-In-Time (JIT) model factory pattern [1.1].

- **The Sterile Core Boundary Constraint:** The `Core` layer contains pure view-models that implement standard `INotifyPropertyChanged` contracts (`ViewModelBase`), remaining completely agnostic of any Avalonia UI framework binary dependencies [1.1].
- **The State Factory Lifecycle Loop (`GetGroup` Routing):** The layout engine centralizes state allocation within the toolbar view-model layer. The state-machine tracks memory allocation states using an explicit structural factory marker flag (`IsNew` Paradigm) embedded directly within the group model blueprint [1.1].
- **The Hot-Start Vector (JSON Footprint Exists):** If the requested group identifier is found within the deserialized internal master dictionary, the factory yields the cached model snapshot retrieved from disk [1.1]. The allocation marker remains suppressed (`IsNew = false`), locking the loaded data configuration [1.1].
- **The Cold-Start Vector (First Launch / Missing Key):** If the key is absent, the factory instantiates a fresh model instance with the structural marker explicitly set to true (`IsNew = true`) and registers it within the master collection, forcing the UI to dictate the initial baseline [1.1].

### 9. Asynchronous Lifecycle Dispatching (`ToolBar.OnLoaded` Workflow)
Avalonia UI elements construct layout topologies from the bottom up, meaning child group instances lack a valid visual tree parent or master data context scope during early XAML parsing phases [1.1]. To eliminate initialization gaps and race conditions caused by premature property mutation triggers, all handshake workflows route strictly through the centralized layout dispatching pipeline during the window loading phase [1.1].

- **The Coordinator Responsibility Engine:** The host toolbar control functions as the sole structural coordinator [1.1]. It sequences through layout tabs, extracts the contextual layout boundaries, maps the internal structural tab identifiers inside the model, and passes live references downward to child controllers [1.1].
- **The Conflict-Free Data Mutation Shield:** The child control receives the model reference and instantly evaluates the state factory marker to resolve UI property conflicts:
  1. **XAML Baseline Ingestion:** If the allocation marker is active (`IsNew = true`), the group model safely ingests initial layout definitions (e.g., expanded and pinned states) straight from the XAML literal values declared by the application developer [1.1].
  2. **JSON Overrides Enforcement:** If the marker is suppressed (`IsNew = false`), the XAML default initialization pass is completely bypassed, enforcing absolute priority of the values retrieved from the disk layout configuration [1.1].
- **The Isolated TwoWay Data-Stream Bridge:** To ensure that application developers retain full, unpolluted control over the public `DataContext` property inside the layout boundaries for business logic bindings, all core toolbar synchronization parameters bind explicitly using targeted layout engine channels [1.1]. The runtime loop links dependency properties directly into the isolated internal model endpoint using strict two-way synchronization modes, keeping the host element context unpolluted [1.1].
