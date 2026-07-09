# Fluent Fold

A **WinUI 3** desktop application for organizing files in a folder by category with ease.

Built with:
- **Windows App SDK** (WinUI 3)
- **.NET 8.0** / C#
- **CommunityToolkit.Mvvm** (MVVM pattern)
- **MSIX** packaging

## Features

- **Folder Picker** – Select any folder to analyze and organize.
- **Organize by Category** – Automatically moves files into subfolders: `Images`, `Documents`, `Videos`, `Audio`, `Archives`, `Code`, `Other`.
- **Rename with Pattern** – Apply a custom naming pattern with sequential numbering (e.g. `MyFile_001`, `MyFile_002`).
- **Undo Last Operation** — Revert the most recent organize operation.
- **Folder Summary** — See total file/folder counts and per-category breakdown.
- **Mica Backdrop** — Modern Windows 11 backdrop material.
- **Dark/Light Theme** — Toggle in Settings page; auto-follows system theme.
- **InfoBar Notifications** — In-app status messages for success, warning, and errors.
- **ProgressRing** — Visual feedback during long-running operations.

## How to Build

1. Open `FluentFold.sln` in **Visual Studio 2022**.
2. Ensure the **Windows App SDK** workload is installed.
3. Set `FluentFoldPackage` as the startup project.
4. Build and run (F5).

## Project Structure

```
FluentFold/
├── FluentFold.sln
├── FluentFold/
│   ├── FluentFold.csproj
│   ├── App.xaml / App.xaml.cs
│   ├── MainWindow.xaml.cs
│   ├── Views/
│   │   ├── MainPage.xaml / .cs
│   │   ├── OrganizerPage.xaml / .cs
│   │   └── SettingsPage.xaml / .cs
│   ├── ViewModels/
│   │   ├── OrganizerViewModel.cs
│   │   └── SettingsViewModel.cs
│   ├── Models/
│   │   └── FileEntry.cs
│   └── Services/
│       ├── FileCategorizer.cs
│       ├── OrganizerService.cs
│       └── UndoService.cs
└── FluentFoldPackage/
    ├── FluentFoldPackage.wapproj
    └── Package.appxmanifest
```

## Requirements

- Visual Studio 2022 (17.8+)
- Windows 10 1809+ (build 17763)
- Windows App SDK 1.6