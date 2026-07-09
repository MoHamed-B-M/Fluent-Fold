# Bulk File Organizer 📁

A modern, lightweight desktop application for organizing and renaming files in bulk. Built with **PySide6** and **qfluentwidgets** (Microsoft Fluent Design System for Qt), featuring a beautiful, responsive UI that works seamlessly on Windows, Linux, and macOS.

![Python](https://img.shields.io/badge/Python-3.9%2B-blue)
![PySide6](https://img.shields.io/badge/PySide6-6.9%2B-green)
![qfluentwidgets](https://img.shields.io/badge/qfluentwidgets-1.11%2B-orange)
![License](https://img.shields.io/badge/License-MIT-purple)
[![Build Status](https://github.com/<your-username>/bulk-file-organizer/actions/workflows/build.yml/badge.svg)](https://github.com/<your-username>/bulk-file-organizer/actions/workflows/build.yml)

---

## ✨ Features

### 📂 File Organization
- **Smart Categorization**: Automatically organizes files into folders by type:
  - **Images**: `.jpg`, `.jpeg`, `.png`, `.gif`, `.bmp`, `.svg`, `.ico`, `.webp`
  - **Documents**: `.pdf`, `.doc`, `.docx`, `.txt`, `.xls`, `.xlsx`, `.ppt`, `.pptx`, `.md`, `.csv`, `.json`, `.xml`, `.html`, `.css`, `.js`
  - **Videos**: `.mp4`, `.avi`, `.mkv`, `.mov`, `.wmv`, `.flv`, `.webm`
  - **Audio**: `.mp3`, `.wav`, `.flac`, `.aac`, `.ogg`, `.m4a`
  - **Archives**: `.zip`, `.rar`, `.7z`, `.tar`, `.gz`, `.bz2`, `.iso`
  - **Code**: `.py`, `.java`, `.cpp`, `.c`, `.h`, `.js`, `.ts`, `.go`, `.rs`, `.rb`, `.php`, `.sql`, `.sh`, `.bat`, `.ps1`
  - **Others**: Any unrecognized extension

### ✏️ Bulk Rename
- Custom naming patterns with sequential numbering (e.g., `photo_001.jpg`, `photo_002.jpg`)
- Configurable starting number and zero-padding
- Duplicate name handling with automatic suffixes
- Confirmation dialog before renaming

### ↩️ Undo Operations
- One-click undo for the last operation (organize or rename)
- Restores files to their original locations/names
- Visual confirmation of undo actions

### 📊 Folder Summary
- Real-time folder statistics:
  - Total files and folders count
  - File count per category
- Auto-refresh after operations

### 🎨 Modern Fluent Design UI
- **Fluent Design System** (Microsoft's design language)
- **Light/Dark/Auto theme** support with system detection
- Smooth animations and transitions
- Responsive layout (minimum 900×700)
- Status log with auto-scroll and timestamps
- Toast notifications for operations

### ⚡ Performance
- **Threaded operations** - UI never freezes during file operations
- Progress indicators for long-running tasks
- Optimized for low-end laptops
- Standard library only for core logic (no external deps in organizer)

---

## 📋 Requirements

- **Python 3.9+** (recommended 3.10+)
- **PySide6** ≥ 6.9
- **qfluentwidgets** (PyQt-Fluent-Widgets) ≥ 1.11

---

## 🚀 Installation

### 1. Clone the Repository
```bash
git clone <repository-url>
cd bulk-file-organizer
```

### 2. Create Virtual Environment (Recommended)
```bash
python -m venv venv
# Windows
venv\Scripts\activate
# Linux/macOS
source venv/bin/activate
```

### 3. Install Dependencies
```bash
pip install -r requirements.txt
```

> **Note**: `qfluentwidgets` is installed from GitHub since it's not on PyPI:
> ```bash
> pip install git+https://github.com/zhiyiYo/PyQt-Fluent-Widgets.git
> ```

---

## 🔨 Building from Source

Build a standalone Windows `.exe` (portable) or `.msi` installer using the automated workflows.

### Option 1: GitHub Actions (CI/CD)

Push a version tag to automatically build and release:

```bash
git tag v1.0.0
git push origin v1.0.0
```

The workflow in `.github/workflows/build.yml` will:
1. Build the `.exe` with PyInstaller
2. Create a portable ZIP archive
3. Build an MSI installer (via WiX Toolset)
4. Create a GitHub Release with all assets

You can also trigger a manual build from the **Actions** tab in your repository.

### Option 2: Local Windows Build

Run the PowerShell build script on Windows:

```powershell
.\build_local.ps1            # Build portable .exe only
.\build_local.ps1 -Clean     # Clean + build
.\build_local.ps1 -InstallMsi # Build .exe + MSI installer (requires WiX)
```

Or build manually with PyInstaller:

```bash
pip install pyinstaller
pyinstaller --onedir --windowed --name "FluentFold" `
  --exclude PyQt5 --hidden-import organizer `
  --add-data "organizer.py;." main.py
```

### Output
```
dist/
├── FluentFold/          # Portable folder (run FluentFold.exe)
├── FluentFold.zip       # Compressed portable archive
└── FluentFold-1.0.0.msi # Windows Installer (if MSI build enabled)
```

---

## 🎮 Usage

### Running the Application
```bash
python main.py
```

### Basic Workflow

1. **Select Folder**: Click "Browse Folder" to choose a directory
2. **Review Summary**: See file counts by category in the sidebar
3. **Organize Files**: Click "Organize Files" to sort into category folders
4. **Bulk Rename**: Click "Bulk Rename", enter pattern (e.g., `photo`), set start number
5. **Undo**: Click "Undo Last Operation" to revert the most recent action
6. **Refresh**: Click "Refresh Summary" to update statistics

### Keyboard Shortcuts
| Key | Action |
|-----|--------|
| `Ctrl+O` | Browse Folder |
| `Ctrl+R` | Refresh Summary |
| `Ctrl+Z` | Undo Last Operation |
| `Ctrl+Q` | Quit Application |

---

## 📁 Project Structure

```
bulk-file-organizer/
├── .github/
│   └── workflows/
│       └── build.yml          # GitHub Actions CI/CD (build .exe + .msi)
├── main.py                    # Application entry point
├── organizer.py               # Core file organization logic (unchanged from original)
├── gui.py                     # Fluent Design GUI with qfluentwidgets
├── FluentFold.spec    # PyInstaller spec file (for `pyinstaller FluentFold.spec`)
├── build_local.ps1           # Local Windows build script (PowerShell)
├── build_msi.ps1             # MSI installer builder (requires WiX Toolset)
├── requirements.txt           # Python dependencies
├── README.md                  # This file
├── LICENSE                    # MIT License
└── .gitignore                 # Git ignore rules
```

---

## 🔧 Configuration

### Customizing File Categories
Edit `organizer.py` and modify the `file_types` dictionary in the `FileOrganizer` class:

```python
self.file_types = {
    'Images': ['.jpg', '.jpeg', '.png', ...],
    'Documents': ['.pdf', '.doc', '.docx', ...],
    # Add your own categories:
    'Fonts': ['.ttf', '.otf', '.woff', '.woff2'],
    '3D Models': ['.obj', '.stl', '.fbx', '.blend'],
}
```

### Theme Settings
- **Auto**: Follows system theme (default)
- **Light**: Force light mode
- **Dark**: Force dark mode

Change via Settings page in the navigation sidebar.

---

## 🧪 Testing

Run the application and test with a sample folder:

```bash
# Create test files
mkdir test_folder
cd test_folder
touch test.txt image.jpg document.pdf video.mp4 audio.mp3 archive.zip script.py

# Run organizer
python ../main.py
```

Select `test_folder` and click "Organize Files" to see categorization in action.

---

## 🤝 Contributing

Contributions are welcome! Please follow these steps:

1. **Fork** the repository
2. **Create** a feature branch (`git checkout -b feature/amazing-feature`)
3. **Commit** your changes (`git commit -m 'Add amazing feature'`)
4. **Push** to the branch (`git push origin feature/amazing-feature`)
5. **Open** a Pull Request

### Code Style
- Follow PEP 8 for Python code
- Use type hints where appropriate
- Keep `organizer.py` logic unchanged (backward compatibility)
- Add comments for complex logic

### Reporting Issues
- Use GitHub Issues for bug reports
- Include Python version, OS, and steps to reproduce
- Attach relevant log output from the status area

---

## 📄 License

This project is licensed under the **MIT License** - see the [LICENSE](LICENSE) file for details.

---

## 🙏 Acknowledgments

- **[PySide6](https://wiki.qt.io/Qt_for_Python)** - Python bindings for Qt6
- **[qfluentwidgets](https://github.com/zhiyiYo/PyQt-Fluent-Widgets)** - Fluent Design widgets for Qt (by [zhiyiYo](https://github.com/zhiyiYo))
- **[Microsoft Fluent Design System](https://www.microsoft.com/design/fluent/)** - Design language inspiration
- **Python Standard Library** - Core file operations (`os`, `shutil`, `pathlib`)

---

## 💡 Tips for Low-End Systems

- The application uses background threads for all file operations
- Minimum window size: 900×700 (resizable)
- Disable animations in Settings if experiencing lag
- Works on Python 3.9+ with minimal dependencies

---

**Made with ❤️ for low-end laptops and productivity enthusiasts**