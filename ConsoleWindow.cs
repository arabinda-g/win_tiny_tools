using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace TinyTools
{
    public partial class ConsoleWindow : Form
    {
        private TextBox outputTextBox;
        private TextBox inputTextBox;
        private Button sendButton;
        private Button clearButton;

        public ConsoleWindow()
        {
            InitializeComponent();
            ShowWelcomeMessage();
        }

        private void InitializeComponent()
        {
            // Form properties
            this.Text = "TinyTools Console";
            this.Size = new Size(600, 400);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimizeBox = true;
            this.MaximizeBox = true;
            this.ShowInTaskbar = true;

            // Output text box (read-only)
            outputTextBox = new TextBox
            {
                Location = new Point(10, 10),
                Size = new Size(560, 300),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 9F),
                BackColor = Color.Black,
                ForeColor = Color.LightGreen
            };
            this.Controls.Add(outputTextBox);

            // Input text box
            inputTextBox = new TextBox
            {
                Location = new Point(10, 320),
                Size = new Size(400, 23),
                Font = new Font("Consolas", 9F)
            };
            inputTextBox.KeyDown += InputTextBox_KeyDown;
            this.Controls.Add(inputTextBox);

            // Send button
            sendButton = new Button
            {
                Text = "Send",
                Location = new Point(420, 320),
                Size = new Size(70, 23)
            };
            sendButton.Click += SendButton_Click;
            this.Controls.Add(sendButton);

            // Clear button
            clearButton = new Button
            {
                Text = "Clear",
                Location = new Point(500, 320),
                Size = new Size(70, 23)
            };
            clearButton.Click += ClearButton_Click;
            this.Controls.Add(clearButton);

            // Set focus to input
            this.Load += (s, e) => inputTextBox.Focus();
        }

        private void ShowWelcomeMessage()
        {
            AppendOutput("TinyTools Console");
            AppendOutput("=================");
            AppendOutput("Type 'help' for available commands");
            AppendOutput("");
            ShowConsoleTools();
            AppendOutput("tiny-tools> ");
        }

        private void InputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                ProcessCommand();
            }
        }

        private void SendButton_Click(object sender, EventArgs e)
        {
            ProcessCommand();
        }

        private void ClearButton_Click(object sender, EventArgs e)
        {
            outputTextBox.Clear();
            ShowWelcomeMessage();
        }

        private void ProcessCommand()
        {
            string input = inputTextBox.Text.Trim();
            if (string.IsNullOrEmpty(input)) return;

            // Echo the command
            AppendOutput(input);
            inputTextBox.Clear();

            var parts = input.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var command = parts[0].ToLower();

            switch (command)
            {
                case "exit":
                case "close":
                    this.Close();
                    break;

                case "help":
                    ShowConsoleHelp();
                    break;

                case "list":
                    ShowConsoleTools();
                    break;

                case "enable":
                case "disable":
                case "toggle":
                    if (parts.Length > 1 && int.TryParse(parts[1], out int id))
                    {
                        HandleToolCommand(command, id);
                    }
                    else
                    {
                        AppendOutput($"Please specify a tool ID. Example: {command} 1");
                    }
                    break;

                case "settings":
                    ShowSettings();
                    break;

                case "clear":
                    outputTextBox.Clear();
                    ShowWelcomeMessage();
                    return; // Don't show prompt again

                default:
                    AppendOutput("Unknown command. Type 'help' for available commands.");
                    break;
            }

            AppendOutput("tiny-tools> ");
        }

        private void AppendOutput(string text)
        {
            if (outputTextBox.InvokeRequired)
            {
                outputTextBox.Invoke(new Action(() => AppendOutput(text)));
                return;
            }

            outputTextBox.AppendText(text + Environment.NewLine);
            outputTextBox.SelectionStart = outputTextBox.Text.Length;
            outputTextBox.ScrollToCaret();
        }

        private void ShowConsoleHelp()
        {
            AppendOutput("");
            AppendOutput("Available Commands:");
            AppendOutput("  list          - Show all tools and their status");
            AppendOutput("  enable <id>   - Enable tool by ID (e.g., enable 1)");
            AppendOutput("  disable <id>  - Disable tool by ID");
            AppendOutput("  toggle <id>   - Toggle tool by ID");
            AppendOutput("  settings      - Show current settings");
            AppendOutput("  clear         - Clear console output");
            AppendOutput("  help          - Show this help");
            AppendOutput("  exit/close    - Close console window");
            AppendOutput("");
        }

        private void ShowConsoleTools()
        {
            AppendOutput("");
            AppendOutput("Available Tools:");
            AppendOutput("ID | Status   | Tool Name        | Description");
            AppendOutput("---|----------|------------------|-------------------------");

            var tools = ToolManager.Instance.GetTools();
            for (int i = 0; i < tools.Count; i++)
            {
                var tool = tools[i];
                var status = tool.Enabled ? "Enabled " : "Disabled";
                AppendOutput($"{i + 1,2} | {status,-8} | {tool.Name,-16} | {tool.Description}");
            }
            AppendOutput("");
        }

        private void HandleToolCommand(string command, int id)
        {
            var tools = ToolManager.Instance.GetTools();
            if (id < 1 || id > tools.Count)
            {
                AppendOutput("Invalid tool ID. Use 'list' to see available tools.");
                return;
            }

            var tool = tools[id - 1];
            
            switch (command)
            {
                case "enable":
                    if (!tool.Enabled)
                    {
                        ToolManager.Instance.ToggleTool(id - 1);
                        AppendOutput($"✓ Enabled: {tool.Name}");
                    }
                    else
                    {
                        AppendOutput("Tool is already enabled.");
                    }
                    break;

                case "disable":
                    if (tool.Enabled)
                    {
                        ToolManager.Instance.ToggleTool(id - 1);
                        AppendOutput($"✗ Disabled: {tool.Name}");
                    }
                    else
                    {
                        AppendOutput("Tool is already disabled.");
                    }
                    break;

                case "toggle":
                    ToolManager.Instance.ToggleTool(id - 1);
                    var newStatus = tool.Enabled ? "Enabled" : "Disabled";
                    var symbol = tool.Enabled ? "✓" : "✗";
                    AppendOutput($"{symbol} {newStatus}: {tool.Name}");
                    break;
            }
        }

        private void ShowSettings()
        {
            AppendOutput("");
            AppendOutput("Current Settings:");
            var settings = SettingsManager.Instance.GetAllSettings();
            foreach (var pair in settings)
            {
                AppendOutput($"  {pair.Key} = {pair.Value}");
            }
            AppendOutput("");
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Just close the console window, don't affect the main application
            base.OnFormClosing(e);
        }
    }
}
