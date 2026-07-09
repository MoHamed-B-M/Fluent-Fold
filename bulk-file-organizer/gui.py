#!/usr/bin/env python3
"""
Fluent Fold - Fluent Design GUI
Built with PySide6 and qfluentwidgets (PyQt-Fluent-Widgets)
"""

import sys
import os
from pathlib import Path
from threading import Thread
from queue import Queue, Empty

from PySide6.QtWidgets import QApplication, QMessageBox, QFileDialog, QHBoxLayout, QVBoxLayout
from PySide6.QtCore import Qt, QTimer, Signal, QObject
from PySide6.QtGui import QIcon, QFont

from qfluentwidgets import (
    FluentWindow, NavigationItemPosition, FluentIcon as FIF,
    InfoBar, InfoBarPosition, TitleLabel, SubtitleLabel, BodyLabel,
    PrimaryPushButton, PushButton, LineEdit, SpinBox, TextEdit,
    ComboBox, CheckBox, ProgressBar, CardWidget, VBoxLayout,
    FlowLayout, ScrollArea, isDarkTheme, setTheme, Theme,
    setFont, SearchLineEdit, ToolButton, Flyout, FlyoutAnimationType,
    InfoBarIcon, TeachingTip, TeachingTipTailPosition
)

from organizer import FileOrganizer


class WorkerSignals(QObject):
    """Signals for worker thread communication"""
    progress = Signal(str)  # status message
    finished = Signal(dict)  # result dict
    error = Signal(str)  # error message


class WorkerThread(Thread):
    """Worker thread for long-running operations"""
    
    def __init__(self, func, *args, **kwargs):
        super().__init__()
        self.func = func
        self.args = args
        self.kwargs = kwargs
        self.signals = WorkerSignals()
        self.daemon = True
    
    def run(self):
        try:
            result = self.func(*self.args, **self.kwargs)
            self.signals.finished.emit(result)
        except Exception as e:
            self.signals.error.emit(str(e))


class SummaryCard(CardWidget):
    """Card widget to display folder summary statistics"""
    
    def __init__(self, title, count, icon, parent=None):
        super().__init__(parent)
        self.setFixedSize(180, 100)
        
        layout = QQVBoxLayout(self)
        layout.setSpacing(8)
        layout.setContentsMargins(16, 16, 16, 16)
        
        # Icon
        self.icon_label = BodyLabel()
        self.icon_label.setAlignment(Qt.AlignCenter)
        font = QFont()
        font.setPointSize(24)
        self.icon_label.setFont(font)
        self.icon_label.setText(icon)
        
        # Count
        self.count_label = TitleLabel(str(count))
        self.count_label.setAlignment(Qt.AlignCenter)
        
        # Title
        self.title_label = BodyLabel(title)
        self.title_label.setAlignment(Qt.AlignCenter)
        self.title_label.setWordWrap(True)
        
        layout.addWidget(self.icon_label)
        layout.addWidget(self.count_label)
        layout.addWidget(self.title_label)
    
    def update_count(self, count):
        self.count_label.setText(str(count))


class RenameDialog(Flyout):
    """Flyout dialog for bulk rename options"""
    
    def __init__(self, parent=None):
        super().__init__(parent)
        self.setFixedWidth(380)
        
        # Create content widget
        content = CardWidget()
        layout = QQVBoxLayout(content)
        layout.setSpacing(16)
        layout.setContentsMargins(24, 24, 24, 24)
        
        # Title
        title = SubtitleLabel("Bulk Rename Files")
        layout.addWidget(title)
        
        # Pattern input
        pattern_label = BodyLabel("Name Pattern:")
        self.pattern_edit = LineEdit()
        self.pattern_edit.setPlaceholderText("e.g., photo, document, file")
        self.pattern_edit.setClearButtonEnabled(True)
        layout.addWidget(pattern_label)
        layout.addWidget(self.pattern_edit)
        
        # Start number
        start_label = BodyLabel("Starting Number:")
        self.start_spin = SpinBox()
        self.start_spin.setRange(1, 999999)
        self.start_spin.setValue(1)
        layout.addWidget(start_label)
        layout.addWidget(self.start_spin)
        
        # Preview
        preview_label = BodyLabel("Preview: photo_001.jpg, photo_002.jpg, ...")
        preview_label.setStyleSheet("color: #888; font-size: 12px;")
        layout.addWidget(preview_label)
        
        # Buttons
        btn_layout = QHBoxLayout()
        self.cancel_btn = PushButton("Cancel")
        self.cancel_btn.clicked.connect(self.close)
        self.confirm_btn = PrimaryPushButton("Confirm Rename")
        self.confirm_btn.clicked.connect(self._on_confirm)
        btn_layout.addWidget(self.cancel_btn)
        btn_layout.addWidget(self.confirm_btn)
        layout.addLayout(btn_layout)
        
        self.setWidget(content)
        self.confirmed = False
        self.pattern = ""
        self.start_num = 1
    
    def _on_confirm(self):
        pattern = self.pattern_edit.text().strip()
        if not pattern:
            InfoBar.warning(
                title="Invalid Pattern",
                content="Please enter a name pattern",
                orient=Qt.Horizontal,
                isClosable=True,
                position=InfoBarPosition.TOP,
                duration=2000,
                parent=self.parent()
            )
            return
        
        self.pattern = pattern
        self.start_num = self.start_spin.value()
        self.confirmed = True
        self.close()


class FileOrganizerGUI(FluentWindow):
    """Main application window with Fluent Design"""
    
    def __init__(self):
        super().__init__()
        
        # Initialize organizer backend
        self.organizer = FileOrganizer()
        self.current_folder = None
        self.worker = None
        
        # Setup window
        self.setWindowTitle("Fluent Fold")
        self.resize(1000, 700)
        self.setMinimumSize(900, 600)
        
        # Apply theme
        setTheme(Theme.AUTO)
        
        # Create main interface
        self.home_interface = HomeInterface(self)
        self.addSubInterface(
            self.home_interface,
            FIF.HOME,
            "Home",
            FIF.HOME_FILL,
            NavigationItemPosition.TOP
        )
        
        # Settings interface
        self.settings_interface = SettingsInterface(self)
        self.addSubInterface(
            self.settings_interface,
            FIF.SETTING,
            "Settings",
            FIF.SETTING,
            NavigationItemPosition.BOTTOM
        )
        
        # Navigation setup
        self.navigationInterface.setExpandWidth(220)
        self.navigationInterface.setMinimumWidth(70)
        
        # Center window
        self.center_window()
        
        # Status bar message
        self.status_bar = self.statusBar()
        self.status_bar.showMessage("Ready - Select a folder to begin")
    
    def center_window(self):
        """Center window on screen"""
        screen = QApplication.primaryScreen().geometry()
        size = self.geometry()
        self.move(
            (screen.width() - size.width()) // 2,
            (screen.height() - size.height()) // 2
        )
    
    def show_info_bar(self, title, content, icon=InfoBarIcon.INFORMATION, duration=3000):
        """Show info bar notification"""
        InfoBar.new(
            icon=icon,
            title=title,
            content=content,
            orient=Qt.Horizontal,
            isClosable=True,
            position=InfoBarPosition.TOP,
            duration=duration,
            parent=self
        )
    
    def run_operation(self, func, *args, on_finished=None, on_error=None, **kwargs):
        """Run operation in background thread"""
        if self.worker and self.worker.is_alive():
            self.show_info_bar("Busy", "Please wait for current operation to complete", 
                             InfoBarIcon.WARNING)
            return
        
        self.worker = WorkerThread(func, *args, **kwargs)
        self.worker.signals.finished.connect(
            lambda result: self._on_operation_finished(result, on_finished)
        )
        self.worker.signals.error.connect(
            lambda err: self._on_operation_error(err, on_error)
        )
        self.worker.signals.progress.connect(
            lambda msg: self.status_bar.showMessage(msg)
        )
        self.worker.start()
    
    def _on_operation_finished(self, result, callback):
        if 'error' in result:
            self.show_info_bar("Error", result['error'], InfoBarIcon.ERROR)
            if callback:
                callback(False, result)
        else:
            if callback:
                callback(True, result)
    
    def _on_operation_error(self, error, callback):
        self.show_info_bar("Error", error, InfoBarIcon.ERROR)
        if callback:
            callback(False, {'error': error})


class HomeInterface(ScrollArea):
    """Main home interface with folder selection and operations"""
    
    def __init__(self, parent_window):
        super().__init__(parent_window)
        self.parent_window = parent_window
        self.setWidgetResizable(True)
        self.setHorizontalScrollBarPolicy(Qt.ScrollBarAlwaysOff)
        self.setObjectName("homeInterface")
        
        # Main container
        self.container = CardWidget()
        self.setWidget(self.container)
        
        self.main_layout = QQVBoxLayout(self.container)
        self.main_layout.setSpacing(20)
        self.main_layout.setContentsMargins(30, 30, 30, 30)
        
        self._setup_ui()
        self._connect_signals()
    
    def _setup_ui(self):
        # Header
        header_layout = QHBoxLayout()
        title = TitleLabel("Fluent Fold")
        subtitle = BodyLabel("Organize and rename files in bulk")
        subtitle.setStyleSheet("color: #888; margin-top: -8px;")
        
        header_vbox = QVBoxLayout()
        header_vbox.addWidget(title)
        header_vbox.addWidget(subtitle)
        header_layout.addLayout(header_vbox)
        header_layout.addStretch()
        
        self.main_layout.addLayout(header_layout)
        
        # Folder selection card
        folder_card = CardWidget()
        folder_layout = QVBoxLayout(folder_card)
        folder_layout.setSpacing(12)
        folder_layout.setContentsMargins(20, 20, 20, 20)
        
        folder_title = SubtitleLabel("📁 Folder Selection")
        folder_layout.addWidget(folder_title)
        
        folder_input_layout = QHBoxLayout()
        self.folder_edit = LineEdit()
        self.folder_edit.setPlaceholderText("Select a folder to organize...")
        self.folder_edit.setReadOnly(True)
        self.folder_edit.setClearButtonEnabled(True)
        
        self.browse_btn = PrimaryPushButton("Browse Folder")
        self.browse_btn.setIcon(FIF.FOLDER)
        self.browse_btn.setFixedWidth(140)
        
        self.refresh_btn = PushButton("Refresh")
        self.refresh_btn.setIcon(FIF.SYNC)
        self.refresh_btn.setFixedWidth(100)
        self.refresh_btn.setEnabled(False)
        
        folder_input_layout.addWidget(self.folder_edit)
        folder_input_layout.addWidget(self.browse_btn)
        folder_input_layout.addWidget(self.refresh_btn)
        folder_layout.addLayout(folder_input_layout)
        
        self.main_layout.addWidget(folder_card)
        
        # Summary cards
        summary_card = CardWidget()
        summary_layout = QVBoxLayout(summary_card)
        summary_layout.setSpacing(16)
        summary_layout.setContentsMargins(20, 20, 20, 20)
        
        summary_title = SubtitleLabel("📊 Folder Summary")
        summary_layout.addWidget(summary_title)
        
        self.summary_container = CardWidget()
        self.summary_flow = FlowLayout(self.summary_container)
        self.summary_flow.setSpacing(16)
        self.summary_flow.setContentsMargins(16, 16, 16, 16)
        
        # Placeholder summary cards
        self.summary_cards = {}
        categories = [
            ("Total Files", 0, "📄"),
            ("Total Folders", 0, "📂"),
            ("Images", 0, "🖼️"),
            ("Documents", 0, "📄"),
            ("Videos", 0, "🎬"),
            ("Audio", 0, "🎵"),
            ("Archives", 0, "📦"),
            ("Code", 0, "💻"),
            ("Others", 0, "📎"),
        ]
        
        for title, count, icon in categories:
            card = SummaryCard(title, count, icon)
            self.summary_cards[title] = card
            self.summary_flow.addWidget(card)
        
        summary_layout.addWidget(self.summary_container)
        self.main_layout.addWidget(summary_card)
        
        # Operations card
        ops_card = CardWidget()
        ops_layout = QVBoxLayout(ops_card)
        ops_layout.setSpacing(16)
        ops_layout.setContentsMargins(20, 20, 20, 20)
        
        ops_title = SubtitleLabel("⚙️ Operations")
        ops_layout.addWidget(ops_title)
        
        # Organize section
        organize_layout = QHBoxLayout()
        self.organize_btn = PrimaryPushButton("Organize by Type")
        self.organize_btn.setIcon(FIF.FILTER)
        self.organize_btn.setFixedHeight(44)
        self.organize_btn.setEnabled(False)
        
        self.undo_btn = PushButton("Undo Last Operation")
        self.undo_btn.setIcon(FIF.UNDO)
        self.undo_btn.setFixedHeight(44)
        self.undo_btn.setFixedWidth(180)
        self.undo_btn.setEnabled(False)
        
        organize_layout.addWidget(self.organize_btn)
        organize_layout.addWidget(self.undo_btn)
        organize_layout.addStretch()
        ops_layout.addLayout(organize_layout)
        
        # Rename section
        rename_layout = QHBoxLayout()
        self.rename_btn = PrimaryPushButton("Bulk Rename")
        self.rename_btn.setIcon(FIF.EDIT)
        self.rename_btn.setFixedHeight(44)
        self.rename_btn.setEnabled(False)
        
        self.rename_preview = BodyLabel("Pattern: file_001.ext, file_002.ext, ...")
        self.rename_preview.setStyleSheet("color: #888;")
        
        rename_layout.addWidget(self.rename_btn)
        rename_layout.addWidget(self.rename_preview)
        rename_layout.addStretch()
        ops_layout.addLayout(rename_layout)
        
        self.main_layout.addWidget(ops_card)
        
        # Log card
        log_card = CardWidget()
        log_layout = QVBoxLayout(log_card)
        log_layout.setSpacing(12)
        log_layout.setContentsMargins(20, 20, 20, 20)
        
        log_header = QHBoxLayout()
        log_title = SubtitleLabel("📋 Activity Log")
        self.clear_log_btn = ToolButton(FIF.DELETE)
        self.clear_log_btn.setToolTip("Clear Log")
        self.clear_log_btn.clicked.connect(self._clear_log)
        log_header.addWidget(log_title)
        log_header.addStretch()
        log_header.addWidget(self.clear_log_btn)
        log_layout.addLayout(log_header)
        
        self.log_text = TextEdit()
        self.log_text.setReadOnly(True)
        self.log_text.setMinimumHeight(150)
        self.log_text.setFont(QFont("Consolas", 10))
        log_layout.addWidget(self.log_text)
        
        # Progress bar
        self.progress_bar = ProgressBar()
        self.progress_bar.setVisible(False)
        log_layout.addWidget(self.progress_bar)
        
        self.main_layout.addWidget(log_card)
        self.main_layout.addStretch()
        
        # Footer
        footer = BodyLabel("Fluent Fold - Made with ❤️ using PySide6 & qfluentwidgets")
        footer.setAlignment(Qt.AlignCenter)
        footer.setStyleSheet("color: #888; padding: 10px;")
        self.main_layout.addWidget(footer)
    
    def _connect_signals(self):
        self.browse_btn.clicked.connect(self._browse_folder)
        self.refresh_btn.clicked.connect(self._refresh_summary)
        self.organize_btn.clicked.connect(self._organize_files)
        self.rename_btn.clicked.connect(self._bulk_rename)
        self.undo_btn.clicked.connect(self._undo_operation)
        self.clear_log_btn.clicked.connect(self._clear_log)
    
    def _browse_folder(self):
        folder = QFileDialog.getExistingDirectory(
            self,
            "Select Folder to Organize",
            self.folder_edit.text() or str(Path.home())
        )
        if folder:
            self.folder_edit.setText(folder)
            self.current_folder = folder
            self.refresh_btn.setEnabled(True)
            self.organize_btn.setEnabled(True)
            self.rename_btn.setEnabled(True)
            self.undo_btn.setEnabled(True)
            self._refresh_summary()
            self._log(f"Selected folder: {folder}")
    
    def _refresh_summary(self):
        if not hasattr(self, 'current_folder') or not self.current_folder:
            return
        
        self.parent_window.run_operation(
            self.parent_window.organizer.get_folder_summary,
            self.current_folder,
            on_finished=self._update_summary_display
        )
        self._log("Refreshing folder summary...")
    
    def _update_summary_display(self, success, result):
        if not success:
            return
        
        summary = result
        if 'error' in summary:
            self._log(f"Error: {summary['error']}")
            return
        
        # Update cards
        self.summary_cards["Total Files"].update_count(summary.get('total_files', 0))
        self.summary_cards["Total Folders"].update_count(summary.get('total_folders', 0))
        
        categories = summary.get('categories', {})
        for cat_name, count in categories.items():
            if cat_name in self.summary_cards:
                self.summary_cards[cat_name].update_count(count)
        
        self._log(f"Summary updated: {summary.get('total_files', 0)} files, {summary.get('total_folders', 0)} folders")
    
    def _organize_files(self):
        if not hasattr(self, 'current_folder') or not self.current_folder:
            self.parent_window.show_info_bar("Error", "Please select a folder first", InfoBarIcon.ERROR)
            return
        
        self.organize_btn.setEnabled(False)
        self.progress_bar.setVisible(True)
        self.progress_bar.setValue(0)
        
        def on_finished(success, result):
            self.organize_btn.setEnabled(True)
            self.progress_bar.setVisible(False)
            if success:
                organized = result
                moved = organized.get('total', 0)
                self._log(f"✅ Organized {moved} files into category folders")
                for item in organized.get('moved', []):
                    self._log(f"  📁 {item['from']} → {item['to']}")
                self._refresh_summary()
                self.parent_window.show_info_bar("Success", f"Organized {moved} files", InfoBarIcon.SUCCESS)
            else:
                self.parent_window.show_info_bar("Error", result.get('error', 'Unknown error'), InfoBarIcon.ERROR)
        
        self.parent_window.run_operation(
            self.parent_window.organizer.organize_by_type,
            self.current_folder,
            on_finished=on_finished
        )
        self._log("🔄 Organizing files by type...")
    
    def _bulk_rename(self):
        if not hasattr(self, 'current_folder') or not self.current_folder:
            self.parent_window.show_info_bar("Error", "Please select a folder first", InfoBarIcon.ERROR)
            return
        
        # Show rename dialog
        dialog = RenameDialog(self)
        dialog.exec()
        
        if not dialog.confirmed:
            return
        
        pattern = dialog.pattern
        start_num = dialog.start_num
        
        self.rename_btn.setEnabled(False)
        self.progress_bar.setVisible(True)
        self.progress_bar.setValue(0)
        
        def on_finished(success, result):
            self.rename_btn.setEnabled(True)
            self.progress_bar.setVisible(False)
            if success:
                renamed = result
                count = renamed.get('total', 0)
                self._log(f"✅ Renamed {count} files with pattern '{pattern}_###'")
                for item in renamed.get('renamed', []):
                    self._log(f"  ✏️ {item['from']} → {item['to']}")
                self._refresh_summary()
                self.parent_window.show_info_bar("Success", f"Renamed {count} files", InfoBarIcon.SUCCESS)
            else:
                self.parent_window.show_info_bar("Error", result.get('error', 'Unknown error'), InfoBarIcon.ERROR)
        
        self.parent_window.run_operation(
            self.parent_window.organizer.rename_files,
            self.current_folder,
            pattern,
            start_num,
            on_finished=on_finished
        )
        self._log(f"🔄 Renaming files with pattern: {pattern}_{str(start_num).zfill(3)}...")
    
    def _undo_operation(self):
        self.undo_btn.setEnabled(False)
        
        def on_finished(success, result):
            self.undo_btn.setEnabled(True)
            if success:
                self._log(f"↩️ {result.get('message', 'Undo successful')}")
                self._refresh_summary()
                self.parent_window.show_info_bar("Success", result.get('message', 'Undo successful'), InfoBarIcon.SUCCESS)
            else:
                self._log(f"⚠️ {result.get('message', 'Nothing to undo')}")
                self.parent_window.show_info_bar("Info", result.get('message', 'Nothing to undo'), InfoBarIcon.INFORMATION)
        
        self.parent_window.run_operation(
            self.parent_window.organizer.undo_last_operation,
            on_finished=on_finished
        )
        self._log("↩️ Undoing last operation...")
    
    def _log(self, message):
        from datetime import datetime
        timestamp = datetime.now().strftime("%H:%M:%S")
        self.log_text.append(f"[{timestamp}] {message}")
        # Auto-scroll to bottom
        scrollbar = self.log_text.verticalScrollBar()
        scrollbar.setValue(scrollbar.maximum())
    
    def _clear_log(self):
        self.log_text.clear()
        self._log("Log cleared")


class SettingsInterface(ScrollArea):
    """Settings interface"""
    
    def __init__(self, parent_window):
        super().__init__(parent_window)
        self.parent_window = parent_window
        self.setWidgetResizable(True)
        self.setHorizontalScrollBarPolicy(Qt.ScrollBarAlwaysOff)
        self.setObjectName("settingsInterface")
        
        self.container = CardWidget()
        self.setWidget(self.container)
        
        self.layout = QVBoxLayout(self.container)
        self.layout.setSpacing(20)
        self.layout.setContentsMargins(30, 30, 30, 30)
        
        self._setup_ui()
    
    def _setup_ui(self):
        title = TitleLabel("Settings")
        subtitle = BodyLabel("Customize application behavior and appearance")
        subtitle.setStyleSheet("color: #888; margin-top: -8px;")
        
        self.layout.addWidget(title)
        self.layout.addWidget(subtitle)
        
        # Theme setting
        theme_card = CardWidget()
        theme_layout = QVBoxLayout(theme_card)
        theme_layout.setSpacing(12)
        theme_layout.setContentsMargins(20, 20, 20, 20)
        
        theme_title = SubtitleLabel("🎨 Appearance")
        theme_layout.addWidget(theme_title)
        
        theme_option_layout = QHBoxLayout()
        theme_label = BodyLabel("Theme:")
        self.theme_combo = ComboBox()
        self.theme_combo.addItems(["Auto (System)", "Light", "Dark"])
        self.theme_combo.setCurrentIndex(0)
        self.theme_combo.currentIndexChanged.connect(self._on_theme_changed)
        
        theme_option_layout.addWidget(theme_label)
        theme_option_layout.addWidget(self.theme_combo)
        theme_option_layout.addStretch()
        theme_layout.addLayout(theme_option_layout)
        
        self.layout.addWidget(theme_card)
        
        # File categories card
        categories_card = CardWidget()
        categories_layout = QVBoxLayout(categories_card)
        categories_layout.setSpacing(12)
        categories_layout.setContentsMargins(20, 20, 20, 20)
        
        cat_title = SubtitleLabel("📁 File Categories")
        cat_desc = BodyLabel("Default file extension categories used for organization. Modify organizer.py to customize.")
        cat_desc.setWordWrap(True)
        cat_desc.setStyleSheet("color: #888;")
        categories_layout.addWidget(cat_title)
        categories_layout.addWidget(cat_desc)
        
        # Display categories
        from organizer import FileOrganizer
        org = FileOrganizer()
        for cat, exts in org.file_types.items():
            cat_info = BodyLabel(f"<b>{cat}:</b> {', '.join(exts)}")
            cat_info.setWordWrap(True)
            cat_info.setTextFormat(Qt.RichText)
            cat_info.setStyleSheet("font-family: Consolas, monospace; font-size: 12px;")
            categories_layout.addWidget(cat_info)
        
        self.layout.addWidget(categories_card)
        
        # About card
        about_card = CardWidget()
        about_layout = QVBoxLayout(about_card)
        about_layout.setSpacing(12)
        about_layout.setContentsMargins(20, 20, 20, 20)
        
        about_title = SubtitleLabel("ℹ️ About")
        about_layout.addWidget(about_title)
        
        about_text = BodyLabel(
            "<b>Fluent Fold</b> v1.0.0<br>"
            "A modern file organization tool built with PySide6 and qfluentwidgets.<br><br>"
            "Features:<br>"
            "• Organize files by type (Images, Documents, Videos, Audio, Archives, Code, Others)<br>"
            "• Bulk rename with custom patterns and sequential numbering<br>"
            "• Undo last operation (organize or rename)<br>"
            "• Real-time folder summary<br>"
            "• Fluent Design UI with dark/light theme support<br><br>"
            "Fluent Fold - Built with ❤️ using Python, PySide6, and qfluentwidgets"
        )
        about_text.setWordWrap(True)
        about_text.setTextFormat(Qt.RichText)
        about_text.setOpenExternalLinks(True)
        about_layout.addWidget(about_text)
        
        self.layout.addWidget(about_card)
        self.layout.addStretch()
    
    def _on_theme_changed(self, index):
        themes = [Theme.AUTO, Theme.LIGHT, Theme.DARK]
        setTheme(themes[index])
        self.parent_window.show_info_bar("Theme Changed", f"Switched to {self.theme_combo.currentText()}", InfoBarIcon.SUCCESS)


def main():
    """Application entry point"""
    # Enable high DPI scaling
    QApplication.setHighDpiScaleFactorRoundingPolicy(
        Qt.HighDpiScaleFactorRoundingPolicy.PassThrough
    )
    
    app = QApplication(sys.argv)
    app.setAttribute(Qt.AA_EnableHighDpiScaling)
    app.setAttribute(Qt.AA_UseHighDpiPixmaps)
    
    # Set application font
    font = QFont("Segoe UI", 9)
    app.setFont(font)
    
    # Create and show main window
    window = FileOrganizerGUI()
    window.show()
    
    sys.exit(app.exec())


if __name__ == "__main__":
    main()