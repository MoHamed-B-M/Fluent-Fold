# FluentFold Architecture Guide

## Overview

FluentFold is a Windows 11 file organization utility built with WinUI 3 and .NET 10. It helps users organize files into categorized folders, bulk-rename files, detect duplicates, analyze system storage, and undo organizational changes.

---

## Architecture

### Patterns

- **MVVM** -- Views bind to ViewModels via `x:Bind`; ViewModels use `CommunityToolkit.Mvvm` source generators (`[ObservableProperty]`, `[RelayCommand]`)
- **Dependency Injection** -- `Microsoft.Extensions.DependencyInjection` container built manually in `App.OnLaunched()`
- **Interface-based services** -- Every service has a corresponding `I{Name}Service` interface for testability and decoupling
- **Frame-based navigation** -- No MVVM navigation framework; `MainViewModel` holds a reference to a `Frame` and calls `Navigate()`

### DI Container Setup

Defined in `App.xaml.cs`:

- **Singletons** (shared state): `IWindowService`, `IAppSettingsService`, `IUndoService`, `IFirstLaunchService`
- **Transients** (new instance per injection): All operational services (`IFolderPickerService`, `IOrganizerService`, `IAnalyzerService`, `IRenamingService`, `IRulesEngine`) and all ViewModels
- **Logging**: `Microsoft.Extensions.Logging` with `ILogger<T>` injected into all services and ViewModels via primary constructors

### Solution Structure

```
FluentFold/
  App.xaml / .cs          Entry point, DI configuration, splash
  MainWindow.xaml / .cs   NavigationView shell, onboarding overlay
  MainPage.xaml / .cs     Default placeholder (unused in navigation)
  Models/                 Plain data objects + observable models
    AnalyzerItem.cs, CleanupFileItem.cs, DashboardCard.cs,
    DuplicateFileEntry.cs, ExtensionRule.cs, FileEntry.cs,
    FileMove.cs, OrganizeOperation.cs, RuleModel.cs
  ViewModels/
    MainViewModel.cs      Navigation logic, selected nav tag
    OrganizerViewModel.cs Folder scan, organize, rename, rules, undo
    AnalyzerViewModel.cs  System-wide scan, delete, dashboard
    HistoryViewModel.cs   Undo history list, restore
    SettingsViewModel.cs  App settings, theme, onboarding reset
  Views/
    OrganizerPage.xaml    Main file organizer (Standard + Pro modes)
    AnalyzerPage.xaml     System analysis with dashboard cards
    HistoryPage.xaml      Operation history with undo buttons
    SettingsPage.xaml     Settings form with all toggles/radios
    AboutPage.xaml        Version info, GitHub link
    OnboardingDialog.xaml 4-slide onboarding content dialog
    FirstLaunchSetupDialog.xaml  Standard vs Pro mode picker
    CleanupReviewDialog.xaml  Zero-byte/stale/temp file review
  Services/
    IWindowService / WindowService              Window handle for Win32 interop
    IAppSettingsService / AppSettingsService    LocalSettings persistence
    IFirstLaunchService / FirstLaunchService    Onboarding completion state
    IFolderPickerService / FolderPickerService  Folder picker with FutureAccessList
    IOrganizerService / OrganizerService        File scan, organize, undo, duplicates
    IAnalyzerService / AnalyzerService          Temp/cache/duplicate system scan
    IRenamingService / RenamingService          Pattern-based bulk rename
    IUndoService / UndoService                  Stack-based operation history
    IRulesEngine / RulesEngine                  Trigger-based category rules
  Helpers/
    ResourceHelper.cs     .resx resource loading via ResourceManager
    FormatHelper.cs       File size formatting (bytes -> KB/MB/GB)
  Resources/
    ErrorMessages.resx    Localized error strings
    LogMessages.resx      Localized log strings
  Assets/                 App icon, tile images, splash screen
  Properties/
    PublishProfiles/      win-x86, win-x64, win-arm64 MSIX profiles
```

---

## Navigation

`MainWindow` contains a `NavigationView` with five items: Organizer, History, Analyzer, Settings, About. On `ItemInvoked`, the page tag is sent to `MainViewModel.NavigateToPageCommand`, which maps tags to `System.Type` and calls `Frame.Navigate()`.

Landing page: `OrganizerPage` (navigated on first activation).

---

## Services Layer

### WindowService
Exposes `IntPtr WindowHandle` for Win32 interop (e.g., `FolderPickerService` uses it for `InitializeWithWindow`).

### AppSettingsService
Persists all app settings to `ApplicationData.Current.LocalSettings` as string values. Properties include notification toggle, organization mode, theme preference, sort mode, window position/size, app mode (Standard/Pro), recycle bin toggle, and teaching tips.

### FirstLaunchService
Tracks whether the user has completed onboarding via a `HasCompletedOnboarding` key in `LocalSettings`. Handles both `bool` and legacy `string` stored values.

### FolderPickerService
Shows the Windows folder picker via `FolderPicker` and persists the selected folder token in `StorageApplicationPermissions.FutureAccessList` so the folder remains accessible across sessions.

### OrganizerService
Core file scanning and organization logic:
- `ScanFolderAsync` -- enumerates files, assigns categories based on extension mapping
- `OrganizeFilesAsync` -- moves/copies files into category-based subfolders with customizable naming patterns (`{category}`, `{YYYY}`, `{MM}`, `{DD}`)
- `UndoOperationAsync` -- reverses a previous organize operation
- `FindDuplicatesAsync` -- detects duplicate files by size grouping then async byte-level comparison (4096-byte buffer)

Built-in categories: Images, Documents, Videos, Audio, Archives, Code, Other. Custom extension rules and trigger-based rules can override defaults.

### AnalyzerService
Scans system locations for cleanup candidates: `%TEMP%`, Windows temp, prefetch, browser caches, WER, and user profile temp folders. Detects duplicates via MD5 hashing. Reports results as categorized `AnalyzerItem` objects.

### RenamingService
Provides pattern-based bulk renaming with tokens `{name}`, `{n}` (sequential counter), `{ext}`. Supports configurable start number and zero-padding. Preview is a dry-run; `ApplyAsync` executes the renames.

### UndoService
Maintains a `Stack<OrganizeOperation>` for undo functionality. Supports push, pop, clear, and remove. Exposes a read-only history snapshot.

### RulesEngine
Evaluates trigger-based rules (Contains, StartsWith, EndsWith, Regex) against filenames to assign custom categories.

---

## ViewModels

### MainViewModel
Holds `SelectedNavTag` and `NavigateToPageCommand`. Receives a `Frame` reference via `Initialize()` and navigates to the target page type on command execution.

### OrganizerViewModel
The largest ViewModel (~800+ lines). Manages:
- Folder selection and scanning
- File organization (move/copy) with undo/redo
- Bulk rename with preview
- Duplicate detection
- Cleanup file detection (zero-byte, stale, temp, empty folders)
- Custom extension rules and trigger rules
- Sort modes (Type, Date Created, Date Accessed, Name, Size)
- Standard vs Pro mode UI toggling
- ListView/GridView view mode toggle
- Toast notifications via `AppNotificationManager`
- Progress reporting and cancellation

### AnalyzerViewModel
Manages system-wide scan state (idle/scanning/results). Categories results into Temp, Cache, and Duplicate groups with dashboard cards. Supports select-all, deselect-all, and bulk delete with progress reporting.

### HistoryViewModel
Loads operation history from `IUndoService`. Supports undoing individual operations (calls `OrganizerService.UndoOperationAsync`) and restoring all operations.

### SettingsViewModel
Loads/saves all settings via `IAppSettingsService`. Handles theme application (`Application.Current.RequestedTheme`). Exposes reset-to-defaults and reset-onboarding commands.

---

## Views / Pages

### OrganizerPage
Two layout modes controlled by `IsStandardMode` / `IsProMode`:
- **Standard**: Hero card with welcome message, select folder + organize buttons, category stat cards
- **Pro**: Full toolbar with folder picker, organize/undo/restore, statistics grid, custom rules editor, trigger rules editor, rename section, file list with sort/view toggles

Includes a modal overlay during operations (ProgressRing + cancel button) and an InfoBar for status messages.

### AnalyzerPage
Three states (idle, scanning, results):
- **Idle**: Large scan button with animated glow effect (CompositionApi DropShadow + ColorKeyFrameAnimation)
- **Scanning**: Progress card
- **Results**: Summary header + dashboard cards in a `UniformGridLayout`, each card expandable to show file checkboxes

### HistoryPage
Shows a list of past organize operations with timestamps, source folder paths, and file counts. Each entry has an Undo button. "Restore All Operations" button at the top.

### SettingsPage
Form with toggles, combo boxes, radio buttons, and action buttons. Organized into sections: Notifications, Organization, Teaching Tips, App Mode, Safety (Recycle Bin), Appearance (theme), Onboarding Reset.

### AboutPage
Version info from `Package.Current.Id.Version`, GitHub link, app description. No ViewModel.

### CleanupReviewDialog
ContentDialog listing cleanup candidates (zero-byte, stale, temp files) with checkboxes and delete size. Respects `UseRecycleBin` setting.

### OnboardingDialog
4-slide ContentDialog (Welcome, Smart Organization, Storage Analysis, Ready) with Back/Next navigation, dot indicators, and EntranceThemeTransition.

### FirstLaunchSetupDialog
ContentDialog for choosing Standard or Pro mode on first launch.

---

## Models

| Model | Type | Key Properties |
|-------|------|---------------|
| `FileEntry` | plain class | `Name`, `Path`, `Category`, `DateModified`, `DateCreated`, `DateAccessed`, `Size` |
| `OrganizeOperation` | plain class (init) | `Timestamp`, `SourceFolder`, `Moves`, `IsCopy` |
| `FileMove` | plain class (init) | `SourcePath`, `DestPath`, `FileName`, `Category` |
| `ExtensionRule` | `ObservableObject` | `Extension`, `CategoryName` |
| `RuleModel` | plain class | `TriggerType` (enum), `TriggerValue`, `TargetCategory` |
| `AnalyzerItem` | `ObservableObject` | `FilePath`, `FileName`, `Category`, `Size`, `DuplicateGroup`, `IsSelected` |
| `CleanupFileItem` | `ObservableObject` | `FilePath`, `FileName`, `Size`, `Reason`, `LastModified`, `IsSelected` |
| `DuplicateFileEntry` | plain class | `FilePath`, `FileName`, `Size`, `GroupId`, `IsSelected`, `GroupCount` |
| `DashboardCard` | `ObservableObject` | `Title`, `Icon`, `Count`, `SizeFormatted`, `Percent`, `Items`, `IsExpanded` |

---

## Onboarding / First Launch

1. `MainWindow.OnActivated` checks `IFirstLaunchService.IsFirstLaunch`
2. If true, a floating onboarding overlay card is shown with 4 slides
3. User can navigate slides (Back/Next), Skip, or close (X)
4. On dismiss, `MarkCompleted()` is called
5. In Settings, "Reset Onboarding" calls `Reset()` then re-shows the overlay

---

## Key Features

- **File Organization**: Scan folder, categorize files, move/copy into subfolders by type
- **Custom Rules**: Map extensions to categories; trigger-based rules with Contains/StartsWith/EndsWith/Regex
- **Bulk Rename**: Pattern-based with sequential numbering and preview
- **System Analysis**: Scan for temp files, cache files, large files, and duplicates
- **Cleanup Detection**: Identify zero-byte, stale, and temporary files
- **Undo/History**: Full operation history with per-operation and bulk undo
- **Duplicate Detection**: Byte-level comparison within a selected folder
- **Standard/Pro Modes**: Simplified hero UI vs full feature panel
- **Theme Support**: System/Light/Dark with Mica backdrop
- **Window Persistence**: Save/restore position and size across sessions
- **Notifications**: Optional toast notifications for completed operations
- **MSIX Packaging**: Side-loadable with `runFullTrust` capability

---

## Technical Stack

- **Runtime**: .NET 10, Windows 10 1809+ (min), Windows 11 24H2+ (target)
- **UI Framework**: WinUI 3, Windows App SDK 2.3.1
- **MVVM Toolkit**: CommunityToolkit.Mvvm 8.4.0 (source generators)
- **DI**: Microsoft.Extensions.DependencyInjection 9.0.4
- **Logging**: Microsoft.Extensions.Logging 9.0.4
- **Packaging**: MSIX with self-signed certificate
- **Publish**: ReadyToRun + Trimmed for Release
