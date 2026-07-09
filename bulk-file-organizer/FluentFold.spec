# -*- mode: python ; coding: utf-8 -*-
"""
PyInstaller spec file for Bulk File Organizer
Package: Bundles into a portable single-folder .exe distribution
"""

import os
import sys
from pathlib import Path

project_root = Path(__file__).parent.resolve()

block_cipher = None

a = Analysis(
    ['main.py'],
    pathex=[str(project_root)],
    binaries=[],
    datas=[
        ('organizer.py', '.'),
        ('pyqt5_shim.py', '.'),
    ],
    hiddenimports=[
        'organizer',
        'qfluentwidgets',
        'darkdetect',
        'PySide6',
        'PySide6.QtCore',
        'PySide6.QtGui',
        'PySide6.QtWidgets',
        'PySide6.QtNetwork',
        'shiboken6',
    ],
    hookspath=[],
    hooksconfig={},
    runtime_hooks=['pyqt5_shim.py'],
    excludes=[
        'PyQt5', 'PyQt5.QtCore', 'PyQt5.QtGui', 'PyQt5.QtWidgets',
        'PyQt5.sip', 'PyQt5.QtNetwork',
        'matplotlib', 'numpy', 'scipy', 'pandas', 'PIL', 'Pillow',
        'cv2', 'opencv', 'tensorflow', 'torch', 'sklearn',
        'IPython', 'jupyter', 'notebook', 'bokeh', 'plotly',
    ],
    win_no_prefer_redirects=False,
    win_private_assemblies=False,
    cipher=block_cipher,
    noarchive=False,
)

pyz = PYZ(a.pure, a.zipped_data, cipher=block_cipher)

exe = EXE(
    pyz,
    a.scripts,
    a.binaries,
    a.zipfiles,
    a.datas,
    [],
    name='FluentFold',
    debug=False,
    bootloader_ignore_signals=False,
    strip=False,
    upx=True,
    upx_exclude=[],
    runtime_tmpdir=None,
    console=False,
    disable_windowed_traceback=False,
    argv_emulation=False,
    target_arch=None,
    codesign_identity=None,
    entitlements_file=None,
    contents_directory='_internal',
    icon=None,
)

coll = COLLECT(
    exe,
    a.binaries,
    a.zipfiles,
    a.datas,
    [],
    strip=False,
    upx=True,
    upx_exclude=[],
    name='FluentFold',
)
