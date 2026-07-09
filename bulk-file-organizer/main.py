#!/usr/bin/env python3
"""
Bulk File Organizer - Modern Fluent Design Edition

A lightweight desktop application that organizes and renames files in bulk.
Built with PySide6 and qfluentwidgets (Fluent Design).
Compatible with Python 3.9+ on Windows, Linux, and macOS.

Entry point for the application.
"""

import sys
import os

# Add project root to path
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from gui import main


if __name__ == "__main__":
    main()