# 🚀 TinyTools v1.0.0 - Multi-Purpose Tool Management Application

**One executable, multiple modes of operation!** This C# application combines all tool management functionality into a single `TinyTools.exe` file.

## ✨ What's New in This Release

### 🛠️ Core Tools Included:
- **Notepad3 Hotkey Tool** - Press Ctrl+W to close Notepad3 windows
- **Screen Dimmer** - Advanced screen brightness control with multiple methods:
  - Hardware gamma ramp control (fast, system-wide)
  - Software overlay fallback for unsupported systems
  - Auto-detection with intelligent fallback
  - Dedicated system tray integration with brightness controls
  - Persistent settings and configuration window

### 🎮 Multiple Operation Modes:
- **GUI Mode** (default) - Full graphical interface with system tray integration
- **Console Mode** (`--console`) - Interactive command-line interface for scripting
- **Notepad3-Only Mode** (`--notepad3-only`) - Lightweight single-tool operation
- **Help Mode** (`--help`) - Comprehensive usage information

### 🔧 Key Features:
- **System Tray Integration** - Background operation with easy access
- **Settings Persistence** - All preferences automatically saved
- **Windows Startup Integration** - Auto-start with Windows option
- **Modular Architecture** - Easy to extend with new tools
- **Thread-Safe Operations** - Robust multi-threaded design
- **Modern C# Codebase** - Built on .NET 8.0 with Windows Forms

## 📦 What You Get
- Single executable file (`TinyTools.exe`) - no installation required
- Auto-generated configuration files for settings persistence
- Complete source code for customization and extension

## 🚀 Quick Start
1. Download `TinyTools.exe`
2. Run it (GUI mode starts by default)
3. Enable desired tools from the interface
4. Configure system tray and startup options as needed

## 💻 Usage Examples

### GUI Mode (Default)
```bash
TinyTools.exe
```
- Full graphical interface with visual tool management
- System tray integration with right-click menu
- Settings panel for all configuration options
- Live status updates for all tools

### Console Mode
```bash
TinyTools.exe --console
```
Interactive commands available:
- `list` - Show all tools and their status
- `enable <id>` - Enable tool by ID
- `disable <id>` - Disable tool by ID
- `toggle <id>` - Toggle tool by ID
- `settings` - Show current settings
- `help` - Show available commands
- `exit` - Exit the application

### Lightweight Mode
```bash
TinyTools.exe --notepad3-only
```
- Minimal resource usage - just the hotkey functionality
- Perfect for dedicated Notepad3 users

## 🔧 Technical Details

### System Requirements
- Windows 10/11
- .NET 8.0 Runtime (included in self-contained build)

### Architecture
- **Singleton Pattern** - Used for ToolManager, SettingsManager, and tool instances
- **P/Invoke** - Low-level Windows API calls for keyboard hooking and gamma control
- **WinForms** - Native Windows GUI framework
- **Thread-Safe** - Proper locking for multi-threaded operations

### Configuration Files
- `tool_manager_settings.ini` - Application settings (auto-created)
- `tool_manager_modules.ini` - Module states (auto-created)
- `screendimmer_settings.ini` - Screen dimmer specific settings (auto-created)

## 🎯 Perfect For
- Users who want lightweight, efficient tool management
- Both GUI and command-line flexibility
- System administrators needing scriptable tool control
- Developers looking for an extensible tool framework

---

**This release represents a complete, production-ready tool management system with a focus on simplicity, reliability, and extensibility.**

## 📝 Release Information
- **Version:** 1.0.0
- **Release Date:** October 6, 2025
- **Target Framework:** .NET 8.0-windows
- **Build Type:** Self-contained executable
