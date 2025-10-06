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
        private CheckBox showTrayIconCheckBox;
        private CheckBox minimizeToTrayCheckBox;
        private CheckBox startMinimizedCheckBox;
        private CheckBox startWithWindowsCheckBox;
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
            UpdateListView();
            CreateTrayIcon();
            AutoStartEnabledModules();
            
            if (SettingsManager.Instance.GetSetting("start_minimized") && !forceShow)
            {
                WindowState = FormWindowState.Minimized;
                ShowInTaskbar = false;
                Hide();
            }
        }

        private void InitializeComponent()
        {
            Text = "TinyTools";
            Size = new Size(500, 450);
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

            applySettingsButton = new Button
            {
                Text = "Apply Settings",
                Location = new Point(220, 360),
                Size = new Size(100, 30)
            };
            applySettingsButton.Click += ApplySettingsButton_Click;
            Controls.Add(applySettingsButton);

            openConsoleButton = new Button
            {
                Text = "Open Console",
                Location = new Point(330, 360),
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
            if (listView.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select a tool first.", "No Selection", 
                              MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var selectedItem = listView.SelectedItems[0];
            if (selectedItem.Tag is int index)
            {
                try
                {
                    ToolManager.Instance.ToggleTool(index);
                    UpdateListView();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error toggling tool: {ex.Message}", "Error", 
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ApplySettingsButton_Click(object sender, EventArgs e)
        {
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

            MessageBox.Show("Settings applied successfully!", "TinyTools", 
                          MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void OpenConsoleButton_Click(object sender, EventArgs e)
        {
            var consoleThread = new Thread(() => ConsoleInterface.RunConsoleMode())
            {
                IsBackground = true
            };
            consoleThread.Start();
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
            ToolManager.Instance.StopAllTools();
            RemoveTrayIcon();
            base.OnFormClosing(e);
        }
    }
}
