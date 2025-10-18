using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace TinyTools
{
    public partial class MainForm : Form
    {
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr ExtractIcon(IntPtr hInst, string lpszExeFileName, int nIconIndex);

        private ListView listView;
        private Button toggleButton;
        private Button settingsButton;
        private CheckBox showTrayIconCheckBox;
        private CheckBox minimizeToTrayCheckBox;
        private CheckBox startMinimizedCheckBox;
        private CheckBox startWithWindowsCheckBox;
        private Label logLevelLabel;
        private ComboBox logLevelComboBox;
        private Button viewLogButton;
        private Button clearLogButton;
        private Button applySettingsButton;
        private Button openConsoleButton;
        private NotifyIcon notifyIcon;
        private bool forceShow;
        private bool debugMode;

        public MainForm(bool forceShow = false, bool debugMode = false)
        {
            this.forceShow = forceShow;
            this.debugMode = debugMode;
            InitializeComponent();
            LoadSettings();
            InitializeLogging();
            UpdateListView();
            CreateTrayIcon();
            AutoStartEnabledModules();
            
            Logger.Instance.LogInfo("TinyTools application started");
            
            if (SettingsManager.Instance.GetSetting("start_minimized") && !forceShow)
            {
                WindowState = FormWindowState.Minimized;
                ShowInTaskbar = false;
                Hide();
                Logger.Instance.LogDebug("Application started minimized to tray");
            }
        }

        private void InitializeComponent()
        {
            Text = "TinyTools";
            Size = new Size(500, 520);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;

            // Set the window icon explicitly
            try
            {
                // Extract the icon from the executable itself
                var iconHandle = ExtractIcon(System.Diagnostics.Process.GetCurrentProcess().Handle, 
                                           System.Reflection.Assembly.GetExecutingAssembly().Location, 0);
                
                if (iconHandle != IntPtr.Zero)
                {
                    Icon = Icon.FromHandle(iconHandle);
                    if (debugMode)
                        Console.WriteLine("Loaded icon from executable");
                }
                else
                {
                    // Fallback: try to load from file if it exists
                    var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.ico");
                    if (File.Exists(iconPath))
                    {
                        Icon = new Icon(iconPath);
                        if (debugMode)
                            Console.WriteLine("Loaded icon from file");
                    }
                    else if (debugMode)
                    {
                        Console.WriteLine("No custom icon found, using default");
                    }
                }
            }
            catch (Exception ex)
            {
                if (debugMode)
                    Console.WriteLine($"Could not load icon: {ex.Message}");
            }

            // Create ListView
            listView = new ListView
            {
                Location = new Point(10, 10),
                Size = new Size(460, 200),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                MultiSelect = false
            };

            listView.Columns.Add("Tool/Module", 150);
            listView.Columns.Add("Status", 80);
            listView.Columns.Add("Description", 220);
            listView.SelectedIndexChanged += ListView_SelectedIndexChanged;
            listView.MouseClick += ListView_MouseClick;

            Controls.Add(listView);

            // Create buttons and checkboxes
            toggleButton = new Button
            {
                Text = "Enable/Disable Tool",
                Location = new Point(10, 220),
                Size = new Size(150, 30)
            };
            toggleButton.Click += ToggleButton_Click;
            Controls.Add(toggleButton);

            settingsButton = new Button
            {
                Text = "Settings...",
                Location = new Point(170, 220),
                Size = new Size(100, 30),
                Enabled = false
            };
            settingsButton.Click += SettingsButton_Click;
            Controls.Add(settingsButton);

            showTrayIconCheckBox = new CheckBox
            {
                Text = "Show system tray icon",
                Location = new Point(10, 270),
                Size = new Size(200, 25)
            };
            Controls.Add(showTrayIconCheckBox);

            minimizeToTrayCheckBox = new CheckBox
            {
                Text = "Minimize to system tray",
                Location = new Point(10, 300),
                Size = new Size(200, 25)
            };
            Controls.Add(minimizeToTrayCheckBox);

            startMinimizedCheckBox = new CheckBox
            {
                Text = "Start minimized",
                Location = new Point(10, 330),
                Size = new Size(200, 25)
            };
            Controls.Add(startMinimizedCheckBox);

            startWithWindowsCheckBox = new CheckBox
            {
                Text = "Start with Windows",
                Location = new Point(10, 360),
                Size = new Size(200, 25)
            };
            Controls.Add(startWithWindowsCheckBox);

            logLevelLabel = new Label
            {
                Text = "Debug Log Level:",
                Location = new Point(10, 390),
                Size = new Size(100, 25)
            };
            Controls.Add(logLevelLabel);

            logLevelComboBox = new ComboBox
            {
                Location = new Point(115, 390),
                Size = new Size(100, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            logLevelComboBox.Items.AddRange(new[] { "Off", "Error", "Warning", "Info", "Debug", "Trace" });
            logLevelComboBox.SelectedIndexChanged += LogLevelComboBox_SelectedIndexChanged;
            Controls.Add(logLevelComboBox);

            viewLogButton = new Button
            {
                Text = "View Log",
                Location = new Point(220, 390),
                Size = new Size(80, 25)
            };
            viewLogButton.Click += ViewLogButton_Click;
            Controls.Add(viewLogButton);

            clearLogButton = new Button
            {
                Text = "Clear Log",
                Location = new Point(305, 390),
                Size = new Size(80, 25)
            };
            clearLogButton.Click += ClearLogButton_Click;
            Controls.Add(clearLogButton);

            applySettingsButton = new Button
            {
                Text = "Apply Settings",
                Location = new Point(220, 430),
                Size = new Size(100, 30)
            };
            applySettingsButton.Click += ApplySettingsButton_Click;
            Controls.Add(applySettingsButton);

            openConsoleButton = new Button
            {
                Text = "Open Console",
                Location = new Point(330, 430),
                Size = new Size(100, 30)
            };
            openConsoleButton.Click += OpenConsoleButton_Click;
            Controls.Add(openConsoleButton);
        }

        private void LoadSettings()
        {
            showTrayIconCheckBox.Checked = SettingsManager.Instance.GetSetting("show_tray_icon");
            minimizeToTrayCheckBox.Checked = SettingsManager.Instance.GetSetting("minimize_to_tray");
            startMinimizedCheckBox.Checked = SettingsManager.Instance.GetSetting("start_minimized");
            startWithWindowsCheckBox.Checked = SettingsManager.Instance.GetSetting("start_with_windows");
            
            var logLevel = SettingsManager.Instance.GetStringSetting("log_level");
            if (string.IsNullOrEmpty(logLevel)) logLevel = "Off";
            logLevelComboBox.SelectedItem = logLevel;
        }

        private void InitializeLogging()
        {
            var logLevelString = SettingsManager.Instance.GetStringSetting("log_level");
            if (string.IsNullOrEmpty(logLevelString)) logLevelString = "Off";
            
            if (Enum.TryParse<LogLevel>(logLevelString, out var logLevel))
            {
                Logger.Instance.LogLevel = logLevel;
            }
        }

        private void UpdateListView()
        {
            listView.Items.Clear();
            var tools = ToolManager.Instance.GetTools();
            
            for (int i = 0; i < tools.Count; i++)
            {
                var tool = tools[i];
                var item = new ListViewItem(tool.Name);
                item.SubItems.Add(tool.Enabled ? "Enabled" : "Disabled");
                item.SubItems.Add(tool.Description);
                item.Tag = i;
                listView.Items.Add(item);
            }
        }

        private void ToggleButton_Click(object sender, EventArgs e)
        {
            Logger.Instance.LogDebug("Toggle button clicked");
            
            if (listView.SelectedItems.Count == 0)
            {
                Logger.Instance.LogWarning("Toggle button clicked but no tool selected");
                MessageBox.Show("Please select a tool first.", "No Selection", 
                              MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var selectedItem = listView.SelectedItems[0];
            if (selectedItem.Tag is int index)
            {
                try
                {
                    var tool = ToolManager.Instance.GetTool(index);
                    Logger.Instance.LogInfo($"Toggling tool: {tool?.Name} (index: {index})");
                    ToolManager.Instance.ToggleTool(index);
                    UpdateListView();
                    Logger.Instance.LogDebug($"Tool toggle completed successfully");
                }
                catch (Exception ex)
                {
                    Logger.Instance.LogError($"Error toggling tool at index {index}", ex);
                    MessageBox.Show($"Error toggling tool: {ex.Message}", "Error", 
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void SettingsButton_Click(object sender, EventArgs e)
        {
            Logger.Instance.LogDebug("Settings button clicked");
            
            if (listView.SelectedItems.Count == 0)
            {
                Logger.Instance.LogWarning("Settings button clicked but no tool selected");
                MessageBox.Show("Please select a tool first.", "No Selection", 
                              MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var selectedItem = listView.SelectedItems[0];
            if (selectedItem.Tag is int index)
            {
                try
                {
                    var tool = ToolManager.Instance.GetTool(index);
                    if (tool != null && tool.HasSettings)
                    {
                        Logger.Instance.LogInfo($"Opening settings for tool: {tool.Name}");
                        tool.ShowSettings();
                        Logger.Instance.LogDebug("Tool settings window opened successfully");
                    }
                    else
                    {
                        Logger.Instance.LogWarning($"Tool {tool?.Name} does not have settings or is null");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.LogError($"Error opening settings for tool at index {index}", ex);
                    MessageBox.Show($"Error opening settings: {ex.Message}", "Error", 
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ListView_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool hasSelection = listView.SelectedItems.Count > 0;
            toggleButton.Enabled = hasSelection;
            
            if (hasSelection)
            {
                var selectedItem = listView.SelectedItems[0];
                if (selectedItem.Tag is int index)
                {
                    var tool = ToolManager.Instance.GetTool(index);
                    settingsButton.Enabled = tool != null && tool.HasSettings;
                }
                else
                {
                    settingsButton.Enabled = false;
                }
            }
            else
            {
                settingsButton.Enabled = false;
            }
        }

        private void ListView_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                var hitTest = listView.HitTest(e.Location);
                if (hitTest.Item != null && hitTest.Item.Tag is int index)
                {
                    var tool = ToolManager.Instance.GetTool(index);
                    if (tool != null)
                    {
                        ShowModuleContextMenu(tool, index, e.Location);
                    }
                }
            }
        }

        private void ShowModuleContextMenu(ToolModule tool, int index, Point location)
        {
            var contextMenu = new ContextMenuStrip();
            
            // Enable/Disable item
            var toggleItem = new ToolStripMenuItem(tool.Enabled ? "Disable" : "Enable");
            toggleItem.Click += (s, e) => {
                ToolManager.Instance.ToggleTool(index);
                UpdateListView();
            };
            contextMenu.Items.Add(toggleItem);
            
            // Settings item (if available)
            if (tool.HasSettings)
            {
                var settingsItem = new ToolStripMenuItem("Settings...");
                settingsItem.Click += (s, e) => tool.ShowSettings();
                contextMenu.Items.Add(settingsItem);
            }
            
            // Separator
            contextMenu.Items.Add(new ToolStripSeparator());
            
            // Module info
            var infoItem = new ToolStripMenuItem($"Module: {tool.Name}");
            infoItem.Enabled = false;
            contextMenu.Items.Add(infoItem);
            
            var descItem = new ToolStripMenuItem($"Description: {tool.Description}");
            descItem.Enabled = false;
            contextMenu.Items.Add(descItem);
            
            var statusItem = new ToolStripMenuItem($"Status: {(tool.Enabled ? "Enabled" : "Disabled")} | {(tool.IsRunning ? "Running" : "Stopped")}");
            statusItem.Enabled = false;
            contextMenu.Items.Add(statusItem);
            
            // Show context menu
            contextMenu.Show(listView, location);
        }

        private void LogLevelComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (logLevelComboBox.SelectedItem != null)
            {
                var selectedLevel = logLevelComboBox.SelectedItem.ToString();
                if (Enum.TryParse<LogLevel>(selectedLevel, out var logLevel))
                {
                    Logger.Instance.LogLevel = logLevel;
                    SettingsManager.Instance.SetStringSetting("log_level", selectedLevel);
                    Logger.Instance.LogInfo($"Log level changed to: {selectedLevel}");
                }
            }
        }

        private void ViewLogButton_Click(object sender, EventArgs e)
        {
            try
            {
                var logFilePath = Logger.Instance.GetLogFilePath();
                if (File.Exists(logFilePath))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = logFilePath,
                        UseShellExecute = true
                    });
                    Logger.Instance.LogDebug("Log file opened for viewing");
                }
                else
                {
                    MessageBox.Show("No log file exists yet.", "TinyTools", 
                                  MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError("Failed to open log file", ex);
                MessageBox.Show($"Failed to open log file: {ex.Message}", "Error", 
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ClearLogButton_Click(object sender, EventArgs e)
        {
            try
            {
                var result = MessageBox.Show("Are you sure you want to clear the log file?", "Clear Log", 
                                           MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    Logger.Instance.ClearLog();
                    MessageBox.Show("Log file cleared successfully!", "TinyTools", 
                                  MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError("Failed to clear log file", ex);
                MessageBox.Show($"Failed to clear log file: {ex.Message}", "Error", 
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ApplySettingsButton_Click(object sender, EventArgs e)
        {
            Logger.Instance.LogDebug("Applying settings...");
            
            SettingsManager.Instance.SetSetting("show_tray_icon", showTrayIconCheckBox.Checked);
            SettingsManager.Instance.SetSetting("minimize_to_tray", minimizeToTrayCheckBox.Checked);
            SettingsManager.Instance.SetSetting("start_minimized", startMinimizedCheckBox.Checked);
            SettingsManager.Instance.SetSetting("start_with_windows", startWithWindowsCheckBox.Checked);

            SettingsManager.Instance.SaveSettings();
            SettingsManager.Instance.SetStartupRegistry(startWithWindowsCheckBox.Checked);

            if (showTrayIconCheckBox.Checked)
            {
                CreateTrayIcon();
            }
            else
            {
                RemoveTrayIcon();
            }

            Logger.Instance.LogInfo("Settings applied successfully");
            MessageBox.Show("Settings applied successfully!", "TinyTools", 
                          MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void OpenConsoleButton_Click(object sender, EventArgs e)
        {
            var consoleWindow = new ConsoleWindow();
            consoleWindow.Show();
        }

        private void CreateTrayIcon()
        {
            if (!SettingsManager.Instance.GetSetting("show_tray_icon") || notifyIcon != null)
                return;

            notifyIcon = new NotifyIcon
            {
                Text = "TinyTools",
                Visible = true
            };

            // Use the same icon as the main form (embedded in executable)
            try
            {
                if (Icon != null)
                {
                    notifyIcon.Icon = Icon;
                }
                else
                {
                    notifyIcon.Icon = SystemIcons.Application;
                }
            }
            catch
            {
                notifyIcon.Icon = SystemIcons.Application;
            }

            // Create context menu
            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Show TinyTools", null, (s, e) => ShowMainWindow());
            contextMenu.Items.Add("Show Console", null, (s, e) => OpenConsoleButton_Click(s, e));
            contextMenu.Items.Add("-");
            contextMenu.Items.Add("Exit", null, (s, e) => Application.Exit());

            notifyIcon.ContextMenuStrip = contextMenu;
            notifyIcon.DoubleClick += (s, e) => ShowMainWindow();
        }

        private void RemoveTrayIcon()
        {
            if (notifyIcon != null)
            {
                notifyIcon.Visible = false;
                notifyIcon.Dispose();
                notifyIcon = null;
            }
        }

        private void ShowMainWindow()
        {
            Show();
            WindowState = FormWindowState.Normal;
            ShowInTaskbar = true;
            BringToFront();
            Activate();
        }

        private void AutoStartEnabledModules()
        {
            ToolManager.Instance.AutoStartEnabledModules();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            
            if (WindowState == FormWindowState.Minimized && 
                SettingsManager.Instance.GetSetting("minimize_to_tray"))
            {
                Hide();
                ShowInTaskbar = false;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // If user clicked X button, minimize to tray instead of closing
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                MinimizeToTray();
                return;
            }
            
            // Only actually close if it's application shutdown or other reasons
            ToolManager.Instance.StopAllTools();
            RemoveTrayIcon();
            base.OnFormClosing(e);
        }

        private void MinimizeToTray()
        {
            this.Hide();
            this.ShowInTaskbar = false;
            
            // // Ensure tray icon is visible
            // if (notifyIcon != null)
            // {
            //     notifyIcon.Visible = true;
            //     notifyIcon.ShowBalloonTip(2000, "TinyTools", "Application minimized to system tray", ToolTipIcon.Info);
            // }
        }

    }
}
