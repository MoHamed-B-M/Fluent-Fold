# FluentFold Wiki

> Internal documentation hub for the FluentFold project.

---

## Table of Contents

1. [Project Overview](#project-overview)
2. [Architecture](#architecture)
3. [Core Feature Modules](#core-feature-modules)
4. [Code Structure](#code-structure)
5. [Setup Instructions](#setup-instructions)
6. [Building & Packaging](#building--packaging)
7. [CI/CD Pipeline](#cicd-pipeline)
8. [Contributing Guide](#contributing-guide)

---

## Project Overview

FluentFold is a Windows 11 file organization utility built with **WinUI 3** and **.NET 10**. It helps users automatically categorize, organize, rename, and clean up files on their system.

### Key Features

- **Smart Organization** -- Scan any folder and automatically move/copy files into category-based subfolders (Images, Documents, Videos, Audio, Archives, Code, Other)
- **Bulk Rename** -- Pattern-based renaming with sequential numbering and dry-run preview
- **System Analysis** -- Scan temp folders, caches, and user profiles for reclaimable space
- **Duplicate Detection** -- Byte-level comparison finds duplicate files within a folder or across a system scan
- **Undo/History** -- Full operation history with per-operation and bulk undo
- **Custom Rules** -- Extension-to-category mappings and trigger-based rules (Contains, StartsWith, EndsWith, Regex)
- **Standard & Pro Modes** -- Simplified hero UI or full-featured control panel
- **Theme Support** -- System, Light, and Dark themes with Mica backdrop
- **Notifications** -- Optional Windows toast notifications

### Target Platform

- **OS**: Windows 10 1809+ (minimum), Windows 11 24H2+ (target)
- **Runtime**: .NET 10
- **UI Framework**: WinUI 3 + Windows App SDK 2.3.1

---

## Architecture

### Patterns

| Pattern | Usage |
|---------|-------|
| **MVVM** | Views bind to ViewModels via `x:Bind`; ViewModels use `CommunityToolkit.Mvvm` source generators |
| **Dependency Injection** | `Microsoft.Extensions.DependencyInjection` container built in `App.OnLaunched()` |
| **Interface Segregation** | Every service has a dedicated `I{Name}Service` interface |
| **Frame Navigation** | `MainViewModel` holds a `Frame` reference and calls `Navigate()` |
| **Stack-based Undo** | `Stack<OrganizeOperation>` for LIFO undo/redo |

### DI Container

Defined in `App.xaml.cs`:

```csharp
// Singletons (shared state)
services.AddSingleton<IWindowService, WindowService>();
services.AddSingleton<IAppSettingsService, AppSettingsService>();
services.AddSingleton<IUndoService, UndoService>();
services.AddSingleton<IFirstLaunchService, FirstLaunchService>();

// Transients (new instance per injection)
services.AddTransient<IFolderPickerService, FolderPickerService>();
services.AddTransient<IOrganizerService, OrganizerService>();
services.AddTransient<IAnalyzerService, AnalyzerService>();
services.AddTransient<IRenamingService, RenamingService>();
services.AddTransient<IRulesEngine, RulesEngine>();
services.AddTransient<MainViewModel>();
services.AddTransient<OrganizerViewModel>();
services.AddTransient<AnalyzerViewModel>();
services.AddTransient<SettingsViewModel>();
services.AddTransient<HistoryViewModel>();
```

### Navigation Flow

```
MainWindow (NavigationView)
  |
  |-- OrganizerPage  (default landing)
  |-- HistoryPage
  |-- AnalyzerPage
  |-- SettingsPage
  |-- AboutPage
```

Navigation starts in `MainWindow.OnActivated` with a 200ms defer to ensure the window is fully initialized. Page tags are mapped to `System.Type` via a dictionary in `MainViewModel.NavigateToPageCommand`.

### Lifecycle

1. `App.OnLaunched()` builds the DI container and creates `MainWindow`
2. `MainWindow` constructor receives `IServiceProvider`, resolves ViewModel and services
3. `OnActivated` fires once -- initializes navigation, shows onboarding if first launch
4. Window position/state is saved to `LocalSettings` on close and restored on next launch

---

## Core Feature Modules

### 1. File Organizer (`OrganizerService`)

Scans a folder, categorizes files by extension, and moves/copies them into subfolders.

**Built-in categories**: Images, Documents, Videos, Audio, Archives, Code, Other

**Folder naming patterns**: `{category}`, `{YYYY}`, `{MM}`, `{DD}`

**Custom rules** override the default extension-to-category mapping. **Trigger rules** (Contains, StartsWith, EndsWith, Regex) match on filename.

### 2. Bulk Rename (`RenamingService`)

Pattern-based renaming with tokens:
- `{name}` -- original filename (without extension)
- `{n}` -- sequential counter
- `{ext}` -- original extension

Configurable start number and zero-padding width. `Preview()` performs a dry-run; `ApplyAsync()` executes the renames.

### 3. System Analyzer (`AnalyzerService`)

Scans system locations for cleanup candidates:
- `%TEMP%`, `Windows\Temp`, `Windows\Prefetch`
- Browser caches (Internet Explorer, Edge)
- `INetCache`, `WER`
- User profile temp folders on all fixed drives
- Large files (>100MB)
- Duplicate files (MD5 hash-based)

Results are categorized into Temp, Cache, and Duplicate groups with a dashboard view.

### 4. Duplicate Detection (`OrganizerService.FindDuplicatesAsync`)

Within a selected folder, files are grouped by size, then compared byte-by-byte using 4096-byte async streams with `MemoryExtensions.SequenceEqual`.

### 5. Cleanup Detection

Automatically identifies cleanup candidates in organized folders:
- Zero-byte files
- Stale files (not modified in >1 year)
- Temporary files (`.tmp`, `.log`, `.cache`)
- Empty folders

Presented in `CleanupReviewDialog` with per-file checkboxes. Deletion respects the Recycle Bin setting.

### 6. Undo/History (`UndoService`)

Maintains a `Stack<OrganizeOperation>` that records every file move during organization. Supports:
- `Push(operation)` -- record an operation
- `Pop()` -- undo most recent
- `Clear()` -- reset all history
- `Remove(op)` -- remove a specific operation
- `History` -- read-only snapshot of all operations

### 7. Settings

Persisted to `ApplicationData.Current.LocalValues`:

| Setting | Type | Default |
|---------|------|---------|
| EnableNotifications | bool | true |
| DefaultOrganizationMode | string | "Move" |
| KeepFolderStructure | bool | false |
| ThemePreference | string | "System" |
| ShowTeachingTips | bool | true |
| AppMode | string | "Pro" |
| UseRecycleBin | bool | true |
| SortMode | string | "Type" |
| WindowWidth/Height | double | 1200/800 |

### 8. Standard vs Pro Mode

- **Standard**: Simplified hero card UI with welcome message, select folder button, organize button, and category stat cards
- **Pro**: Full control panel with custom rules, trigger rules, rename section, sort modes, view toggle, detailed statistics grid

Controlled by `IAppSettingsService.AppMode`. The two layouts use conditional `x:Load` in `OrganizerPage.xaml`.

---

## Code Structure

```
FluentFold/
  App.xaml / .cs                 Entry point, DI setup, splash
  MainWindow.xaml / .cs          NavigationView shell + onboarding overlay
  MainPage.xaml / .cs            Default placeholder (unused)

  Models/                        Data objects
    FileEntry.cs                 Scanned file entry
    FileMove.cs                  Individual file move record
    OrganizeOperation.cs         Snapshot of an organize operation
    ExtensionRule.cs             Extension-to-category mapping
    RuleModel.cs                 Trigger-based rule
    AnalyzerItem.cs              System scan result item
    CleanupFileItem.cs           Cleanup candidate item
    DuplicateFileEntry.cs        Duplicate file entry
    DashboardCard.cs             Analyzer dashboard card

  ViewModels/                    MVVM ViewModels
    MainViewModel.cs             Navigation state, page switching
    OrganizerViewModel.cs        Organize, rename, rules, duplicates, cleanup
    AnalyzerViewModel.cs         System scan, dashboard, delete
    HistoryViewModel.cs          Undo history list, restore
    SettingsViewModel.cs         Settings load/save, theme, onboarding reset

  Views/                         XAML Pages and Dialogs
    OrganizerPage.xaml / .cs     Main organizer (Standard + Pro layouts)
    AnalyzerPage.xaml / .cs      System analyzer (idle/scanning/results)
    HistoryPage.xaml / .cs       Operation history
    SettingsPage.xaml / .cs      Settings form
    AboutPage.xaml / .cs         App info (no ViewModel)
    OnboardingDialog.xaml / .cs  4-slide onboarding
    FirstLaunchSetupDialog.xaml / .cs  Standard/Pro mode picker
    CleanupReviewDialog.xaml / .cs     Cleanup file review

  Services/                      Interfaces + Implementations
    IWindowService / WindowService              Window handle (HWND)
    IAppSettingsService / AppSettingsService    Settings persistence
    IFirstLaunchService / FirstLaunchService    Onboarding state
    IFolderPickerService / FolderPickerService  Folder picker + FutureAccessList
    IOrganizerService / OrganizerService        Scan, organize, undo, duplicates
    IAnalyzerService / AnalyzerService          System temp/cache/duplicate scan
    IRenamingService / RenamingService          Bulk rename
    IUndoService / UndoService                  Operation history stack
    IRulesEngine / RulesEngine                  Trigger rule evaluation

  Helpers/
    ResourceHelper.cs            .resx resource loading
    FormatHelper.cs              File size formatting

  Resources/
    ErrorMessages.resx           Localized error strings
    LogMessages.resx             Localized log strings

  Assets/                        App icons, tile images, splash screen
  Properties/
    PublishProfiles/             MSIX publish profiles (x86, x64, arm64)
```

### Design Decisions

- **Service Locator in Views**: Pages resolve their ViewModel via `App.Services.GetRequiredService<T>()` in the constructor. This avoids constructor injection into pages (which WinUI 3 pages don't support natively in XAML).
- **Primary Constructors**: All services and ViewModels use primary constructors for `ILogger<T>` and dependency injection.
- **No MVVM Navigation Framework**: Frame-based navigation keeps the dependency count small and avoids framework lock-in.
- **Two-State and Three-State UI Patterns**: AnalyzerPage uses computed `Visibility` properties (Idle, Scanning, Results) to swap layouts. OrganizerPage uses `x:Load` to toggle Standard/Pro layouts.
- **Composition API**: Spring animations on buttons (scale transform) and glow animation on the Analyzer scan button (cycling DropShadow colors).

---

## Setup Instructions

### Prerequisites

- Windows 10 1809+ (Windows 11 recommended)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) with the following workloads:
  - `.NET desktop development`
  - `Universal Windows Platform development`
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (pre-release)
- [Windows App SDK 2.3.1](https://learn.microsoft.com/windows/apps/windows-app-sdk/)

### Clone & Build

```bash
git clone https://github.com/MoHamed-B-M/Fluent-Fold.git
cd Fluent-Fold

# Restore dependencies
dotnet restore

# Build Debug
dotnet build -c Debug

# Build Release MSIX (x64)
dotnet build -c Release `
  -p:Platform=x64 `
  -p:RuntimeIdentifierOverride=win-x64 `
  -p:GenerateAppxPackageOnBuild=true `
  -p:AppxPackageSigningEnabled=false `
  -p:AppxPackage=true
```

> Note: `AppxPackageSigningEnabled=false` is used during local development. For distribution, enable signing with a valid code-signing certificate.

### Install & Run

1. Enable Windows Developer Mode: **Settings > Privacy & security > For developers > Developer Mode**
2. Trust the self-signed certificate (run as Administrator):
   ```powershell
   Import-PfxCertificate -FilePath FluentFold_TemporaryKey.pfx `
     -Password (ConvertTo-SecureString -String "FluentFold2026" -Force -AsPlainText) `
     -CertStoreLocation Cert:\LocalMachine\TrustedPeople
   ```
3. Sign the MSIX:
   ```powershell
   signtool sign /fd SHA256 /a /f FluentFold_TemporaryKey.pfx /p FluentFold2026 bin/.../FluentFold.msix
   ```
4. Double-click `FluentFold.msix` or install via PowerShell:
   ```powershell
   Add-AppPackage -Path .\FluentFold.msix
   ```

---

## Building & Packaging

### Publish Profiles

Pre-configured profiles in `Properties/PublishProfiles/`:

| Profile | Platform | Runtime |
|---------|----------|---------|
| `win-x86.pubxml` | x86 | win-x86 |
| `win-x64.pubxml` | x64 | win-x64 |
| `win-arm64.pubxml` | ARM64 | win-arm64 |

### Publishing

```bash
dotnet publish -c Release -p:Platform=x64 /p:PublishProfile=Properties\PublishProfiles\win-x64.pubxml
```

### MSIX Packaging

The MSIX is created as part of the build when `GenerateAppxPackageOnBuild=true` and `AppxPackage=true`. The output is at:

```
bin\{Platform}\Release\{TargetFramework}\{RuntimeIdentifier}\{AppName}.msix
```

### Signing

For sideloading, the MSIX must be signed. Use `signtool.exe` from the Windows SDK:

```powershell
signtool sign /fd SHA256 /a /f cert.pfx /p password App.msix
```

For production distribution, obtain a code-signing certificate from one of these authorities:

| Provider | Example Product | Approx. Cost |
|----------|----------------|-------------|
| DigiCert | DigiCert Code Signing | $200-500/yr |
| Sectigo | Sectigo Code Signing | $150-300/yr |
| GlobalSign | GlobalSign Code Signing | $200-400/yr |
| Let's Encrypt | Code signing not available | N/A (SSL only) |

---

## CI/CD Pipeline

The repository includes a GitHub Actions workflow at `.github/workflows/build.yml` that:

1. Triggers on push/PR to `main`
2. Builds MSIX packages for x64, x86, and ARM64
3. Uploads the MSIX artifacts

For signing in CI, store the code-signing certificate as a **GitHub Actions Secret** (Base64-encoded PFX) and the password as another secret, then add a signing step before upload.

### Adding Signing to CI

```yaml
- name: Decode cert and sign
  shell: pwsh
  run: |
    $pfxPath = "$env:RUNNER_TEMP\cert.pfx"
    [IO.File]::WriteAllBytes($pfxPath, [Convert]::FromBase64String($env:BASE64_CERT))
    & signtool sign /fd SHA256 /a /f $pfxPath /p $env:CERT_PASSWORD `
      (Get-ChildItem -Recurse -Filter "*.msix" | Select-Object -First 1).FullName
  env:
    BASE64_CERT: ${{ secrets.BASE64_CERT }}
    CERT_PASSWORD: ${{ secrets.CERT_PASSWORD }}
```

---

## Contributing Guide

### Coding Standards

- **File-scoped namespaces** -- no block-scoped `namespace`
- **Primary constructors** for DI injection
- **`[ObservableProperty]`** for bindable properties (with `[NotifyPropertyChangedFor]`, `[NotifyCanExecuteChangedFor]` as needed)
- **`[RelayCommand]`** for commands (async where applicable)
- No XML comments on internal code; public API surface only
- No emojis in source code or documentation
- Follow existing naming: `I{Name}Service`, `{Name}Service`, `{Name}ViewModel`, `{Name}Page`

### Testing

- Run tests: `dotnet test`
- Ensure no build warnings before committing
- Verify with lint analysis:
  ```bash
  dotnet build -warnaserror
  ```

### Pull Request Process

1. Create a feature branch from `main`
2. Make changes following the coding standards above
3. Build and test locally
4. Push and open a PR against `main`
5. Ensure CI checks pass (build + lint)

---

## Appendix: NuGet Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| `CommunityToolkit.Mvvm` | 8.4.0 | MVVM source generators |
| `Microsoft.Extensions.DependencyInjection` | 9.0.4 | DI container |
| `Microsoft.Extensions.Logging` | 9.0.4 | Logging abstractions |
| `Microsoft.WindowsAppSDK` | 2.3.1 | Windows App SDK + WinUI 3 |

### MSIX Capabilities

```xml
<Capabilities>
  <rescap:Capability Name="runFullTrust" />
  <systemai:Capability Name="systemAIModels"/>
</Capabilities>
```

- `runFullTrust` -- required for full file system access
- `systemAIModels` -- reserved for future AI features

---

*Last updated: July 2026*
