# 🚀 TinyTools

**One executable, multiple modes of operation!** 

This C# application combines all tool management functionality into a single `TinyTools.exe` file, providing a comprehensive solution for managing your tools.

## 🎯 What's Included

- ✅ **Notepad3 Hotkey Tool** (Ctrl+W to close Notepad3)
- ✅ **GUI Management Interface** 
- ✅ **Console/Command-line Interface**
- ✅ **System Tray Integration**
- ✅ **Settings Persistence**
- ✅ **Auto-startup with Windows**

## 🎮 Usage Modes

### 🖼️ GUI Mode (Default)
```bash
TinyTools.exe
```
- **Full graphical interface** with visual tool management
- **System tray integration** with right-click menu
- **Settings panel** for all configuration options
- **Live status updates** for all tools
- **Minimize to tray** and **start with Windows** options

### 💻 Console Mode
```bash
TinyTools.exe --console
# or
TinyTools.exe -c
```
**Interactive command-line interface** with these commands:
- `list` - Show all tools and their status
- `enable <id>` - Enable tool by ID (e.g., `enable 1`)
- `disable <id>` - Disable tool by ID
- `toggle <id>` - Toggle tool by ID
- `settings` - Show current settings
- `help` - Show available commands
- `exit` - Exit the application

### ⚡ Notepad3-Only Mode (Lightweight)
```bash
TinyTools.exe --notepad3-only
```
- **Minimal resource usage** - just the hotkey functionality
- **Console output** for monitoring
- **Perfect for dedicated Notepad3 users**

### ❓ Help
```bash
TinyTools.exe --help
# or
TinyTools.exe -h
```
Shows detailed usage information and available options.

## 🔧 Features in Detail

### GUI Mode Features
- **Tool List View**: See all available tools with status and descriptions
- **Easy Toggle**: Click to enable/disable any tool
- **System Tray Options**:
  - ☑️ Show system tray icon
  - ☑️ Minimize to system tray
  - ☑️ Start minimized
  - ☑️ Start with Windows
- **Console Access**: Open console mode from GUI
- **Settings Persistence**: All preferences saved automatically

### Console Mode Features
- **Fast Operations**: Quick tool management via commands
- **Scriptable**: Can be used in batch files and automation
- **Real-time Status**: Live updates when tools start/stop
- **Lightweight**: Minimal memory footprint

### System Integration
- **Registry Integration**: Automatic Windows startup configuration
- **Settings File**: `tool_manager_settings.ini` stores all preferences
- **System Tray**: Background operation with easy access

## 🛠️ Tool Management

### Current Tools
1. **Notepad3 Hotkey** - Press Ctrl+W while Notepad3 is active to close it

### Adding New Tools
To add new tools, modify the `InitializeTools()` method in `ToolManager.cs`:

```csharp
public void InitializeTools()
{
    // Existing Notepad3 tool
    var notepad3Tool = new ToolModule("Notepad3 Hotkey", "Close Notepad3 with Ctrl+W");
    notepad3Tool.SetStartFunction(() => Notepad3Hook.Instance.StartHook());
    notepad3Tool.SetStopFunction(() => Notepad3Hook.Instance.StopHook());
    tools.Add(notepad3Tool);
    
    // Add your new tool here
    var newTool = new ToolModule("My New Tool", "Description of what it does");
    newTool.SetStartFunction(() => StartMyNewTool());
    newTool.SetStopFunction(() => StopMyNewTool());
    tools.Add(newTool);
}
```

## 📁 Files

### Main Executable
- `TinyTools.exe` - **The only file you need!**

### Configuration
- `tool_manager_settings.ini` - Settings storage (auto-created)
- `tool_manager_modules.ini` - Module states (auto-created)

### Source & Build
- `*.cs` - C# source files
- `TinyTools.csproj` - Project file
- `build.bat` - Build script

## 🚀 Quick Start

1. **Build the application**: Run `build.bat` or use `dotnet build`
2. **Run GUI Mode**: Double-click `TinyTools.exe`
3. **Select Notepad3 Hotkey** from the tool list
4. **Click "Enable/Disable Tool"** to activate it
5. **Configure system tray options** as desired
6. **Click "Apply Settings"** to save preferences

## 🔄 Migration from C++ Version

This C# version provides the same functionality as the original C++ version:

✅ **Same Features**: All original functionality preserved  
✅ **Same Settings**: Compatible with existing `tool_manager_settings.ini`  
✅ **Same Interface**: Identical GUI and console modes  
✅ **Better Maintainability**: Modern C# codebase  

## 🎉 Benefits of C# Version

- **Cross-Platform Potential** - Can be adapted for other platforms
- **Modern Language Features** - Easier to maintain and extend
- **Better Error Handling** - Robust exception management
- **Memory Management** - Automatic garbage collection
- **Rich Framework** - Access to .NET ecosystem

## 🔧 Building

### Prerequisites
- .NET 8.0 SDK or later
- Windows (for Windows Forms and P/Invoke functionality)

### Build Commands
```bash
# Build debug version
dotnet build

# Build release version
dotnet build -c Release

# Publish single-file executable
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

### Using Build Script
```bash
.\build.bat
```

## 💡 Tips

- **System Tray**: Right-click the tray icon for quick access
- **Double-click Tray**: Restore the main window
- **Console from GUI**: Use "Open Console" button for command-line access
- **Startup**: Enable "Start with Windows" for automatic tool loading
- **Lightweight**: Use `--notepad3-only` for minimal resource usage

## 🔧 Technical Details

### Architecture
- **Singleton Pattern**: Used for ToolManager, SettingsManager, and Notepad3Hook
- **P/Invoke**: Low-level Windows API calls for keyboard hooking
- **WinForms**: Native Windows GUI framework
- **Thread-Safe**: Proper locking for multi-threaded operations

### Dependencies
- .NET 8.0 Windows Forms
- Windows API (user32.dll, kernel32.dll)
- Registry access for startup management

---

**🎯 Simple, powerful, all-in-one tool management in C#!** 🚀