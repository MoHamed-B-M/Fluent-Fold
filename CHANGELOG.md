# Changelog

All notable changes to FluentFold are documented in this file.

## [1.1.0-beta] - 2026-08-08

### Added
- **What's New** section on the About page highlighting the latest features.
- **New app logo**: refreshed icon and branding across the store listing, Start menu, and splash screen.
- **Teaching tips** on the Organizer page for first-time users (folder selection and organizing), dismissible with light dismiss.
- **Elapsed time and estimated time remaining** shown during analyzer scans.
- **Select All** button in every analyzer category card (Temporary, Cache, Duplicate).
- **Empty states** for the Organizer hero flow and the Analyzer results (no junk found).
- **Accessibility pass**: automation names and IDs on all interactive controls; stat headers use the correct type ramp.

### Changed
- **Redesigned the Organizer screen**: new hero card, stat cards, and Pro-mode control panel using Fluent corner-radius and type tokens.
- **Analyzer scanning card**: replaced the progress bar with a cleaner loading indicator and enlarged the card.
- **App version bumped to 1.1.0-beta** (Package.appxmanifest, app.manifest, installer, About page).

### Fixed
- **Analyzer progress was fake**: the bar jumped to 85% and froze; progress is now reported genuinely across all four scan phases (temp, cache, large files, duplicates), including the hashing phase.
- **Analyzer results cards**: Temp/Cache/Duplicate results were bundled into one dashboard and could not be expanded independently — each category is now its own expandable card.
- **Folder picker** now uses a stable window handle retrieved through `IWindowService`, so the picker opens reliably.
- **XAML parse failure**: `{ThemeResource ControlCornerRadiusLarge}` does not exist in WinUI 3's theme resources (only `ControlCornerRadius` and `OverlayCornerRadius`); the token is now defined in `App.xaml`.
- **Theme tokens**: `ScrimFillBrush` defined for Light/Dark/HighContrast; corner-radius keys unified under app-defined tokens.

### Performance
- Analyzer scan runs on a background task; the UI no longer freezes while scanning.

---

## [1.0.0] - Initial release

- Organize files into categories (Images, Documents, Videos, Audio, Archives, Code, Other) with one-click undo.
- Custom extension rules, trigger rules, and folder naming patterns.
- Bulk rename with numbered patterns.
- Storage analyzer for temp, cache, and duplicate files.
- Settings with show-teaching-tips and launch-on-startup options.