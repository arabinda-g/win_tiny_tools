# Global Hotkey Test Instructions

## Testing the Ctrl+Shift+Mouse Wheel Global Hotkey

The global hotkey has been implemented and should work as follows:

### How to Test:
1. **Start TinyTools** - The application should be running (we can see it in tasklist)
2. **Enable ScreenDimmer** - Go to the TinyTools interface and start the ScreenDimmer module
3. **Open ScreenDimmer Settings** - Right-click the ScreenDimmer tray icon and select "Settings..."
4. **Verify Checkbox** - Make sure "Enable Ctrl+Shift+Mouse Wheel brightness control" is checked
5. **Test in Different Applications**:
   - Open Notepad, Word, Browser, or any other application
   - Hold Ctrl+Shift and scroll the mouse wheel up/down
   - The screen brightness should change by 1% per scroll notch

### Expected Behavior:
- **Scroll Up** (with Ctrl+Shift): Brightness increases by 1%
- **Scroll Down** (with Ctrl+Shift): Brightness decreases by 1%
- **Range**: Brightness is clamped between 10% and 100%
- **Global**: Works from any application, not just TinyTools
- **Toggle**: Can be enabled/disabled via the checkbox in settings

### Implementation Details:
- Uses low-level mouse hook to capture global mouse wheel events
- Checks for Ctrl+Shift modifier keys (Alt must not be pressed to avoid conflicts)
- Changes brightness by exactly 1% per wheel notch as requested
- Settings are saved to configuration file automatically
- Global hotkey is enabled by default but can be disabled

### Files Modified:
- `GlobalMouseHook.cs` - New file for global mouse hook implementation
- `ScreenDimmerConfig.cs` - Added GlobalHotkeyEnabled setting
- `ScreenDimmerManager.cs` - Integrated global hotkey functionality
- `ScreenDimmerSettingsForm.cs` - Added checkbox to enable/disable hotkey

The implementation is complete and ready for testing!
