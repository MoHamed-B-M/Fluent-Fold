# Fluent Fold

A modern Windows 11 file organizer built with WinUI 3 and the Windows App SDK.

## Features

- **Organize by Type** — Sorts files into category folders (Images, Documents, Videos, Audio, Archives, Code, Others) based on file extension. Duplicates are handled with `_1`, `_2` suffixes.
- **Bulk Rename** — Renames all files in a folder using a custom pattern and sequential numbering (e.g., `photo_001.jpg`, `photo_002.jpg`). Duplicates get a `_copy` suffix.
- **Undo Last Operation** — One-click revert of the most recent organize or rename action. Empty category folders are automatically cleaned up on undo.
- **Folder Summary** — Real-time counts of total files, folders, and per-category breakdown with rich icon cards.
- **Dark / Light Theme** — Manual toggle in Settings with auto system theme detection.
- **Mica Backdrop** — Windows 11 Mica material applied to the main window.

## Prerequisites

- Windows 10 (build 17763+) or Windows 11
- [Visual Studio 2022](https://visualstudio.microsoft.com/) with the **Windows App SDK** and **.NET 8.0** workloads
  - Required workloads: `.NET desktop development` + `Universal Windows Platform development`
- Or install the [Windows App SDK](https://aka.ms/windowsappsdk) manually

## Build & Run

### From Visual Studio

1. Open `FluentFold.sln`
2. Set solution platform to `x64`
3. Press **F5** to build and run

### From Command Line

```bash
dotnet restore FluentFold\FluentFold.csproj
dotnet build FluentFold\FluentFold.csproj -c Release -f net8.0-windows10.0.19041.0
```

### MSIX Package

1. Right-click the `FluentFold` project in Visual Studio
2. Select **Publish** → **Create App Packages**
3. Choose **Sideloading** or **Windows Store**
4. Follow the wizard

## Project Structure

```
FluentFold/
├── Models/                  # Data types (FileCategory, FolderSummary, results)
├── Services/                # Core logic (FileOrganizerService, ThemeService)
├── ViewModels/              # MVVM view models (OrganizerViewModel, SettingsViewModel)
├── Views/                   # XAML pages (OrganizerPage, SettingsPage)
├── Themes/                  # Resource dictionaries (card styles, light/dark colors)
├── Assets/                  # App icons and tile images
├── Strings/                 # Localization resources
├── Properties/              # .NET Native reflection XML
├── App.xaml / App.xaml.cs   # Application entry point, DI, NavigationView
├── FluentFold.csproj        # Project file
└── Package.appxmanifest     # MSIX manifest
```

## Tech Stack

| Component | Library |
|-----------|---------|
| UI Framework | WinUI 3 (Windows App SDK 1.6) |
| Language | C# 12 / .NET 8.0 |
| MVVM Toolkit | CommunityToolkit.Mvvm 8.4 |
| Storage APIs | Windows.Storage (async) |
| Packaging | MSIX |
| Backdrop | MicaBackdrop |
