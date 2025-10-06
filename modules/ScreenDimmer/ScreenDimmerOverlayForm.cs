using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace TinyTools.Modules.ScreenDimmer
{
    public partial class ScreenDimmerOverlayForm : Form
    {
        // P/Invoke declarations for layered window
        [DllImport("user32.dll")]
        private static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private const uint LWA_ALPHA = 0x00000002;
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_LAYERED = 0x80000;
        private const int WS_EX_TRANSPARENT = 0x20;
        private const int WS_EX_TOOLWINDOW = 0x80;
        private const int WS_EX_TOPMOST = 0x8;

        public ScreenDimmerOverlayForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            // Form properties for fullscreen overlay
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            this.TopMost = true;
            this.ShowInTaskbar = false;
            this.BackColor = Color.Black;
            
            // Get screen dimensions for all monitors
            Rectangle screenBounds = SystemInformation.VirtualScreen;
            this.Location = screenBounds.Location;
            this.Size = screenBounds.Size;

            // Make the form a layered window
            this.SetStyle(ControlStyles.SupportsTransparentBackColor, true);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            
            // Set extended window style to make it layered and transparent to mouse clicks
            int exStyle = GetWindowLong(this.Handle, GWL_EXSTYLE);
            exStyle |= WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW;
            SetWindowLong(this.Handle, GWL_EXSTYLE, exStyle);
        }

        public void SetOpacity(byte opacity)
        {
            if (this.Handle != IntPtr.Zero)
            {
                SetLayeredWindowAttributes(this.Handle, 0, opacity, LWA_ALPHA);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            // Fill the entire form with black
            using (SolidBrush brush = new SolidBrush(Color.Black))
            {
                e.Graphics.FillRectangle(brush, this.ClientRectangle);
            }
        }

        protected override bool ShowWithoutActivation => true;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_TOPMOST;
                return cp;
            }
        }

        // Prevent the form from being activated
        protected override void SetVisibleCore(bool value)
        {
            base.SetVisibleCore(value);
            if (value && this.WindowState == FormWindowState.Minimized)
            {
                this.WindowState = FormWindowState.Maximized;
            }
        }

        // Override WndProc to handle window messages
        protected override void WndProc(ref Message m)
        {
            const int WM_MOUSEACTIVATE = 0x0021;
            const int MA_NOACTIVATE = 0x0003;

            if (m.Msg == WM_MOUSEACTIVATE)
            {
                m.Result = (IntPtr)MA_NOACTIVATE;
                return;
            }

            base.WndProc(ref m);
        }
    }
}
