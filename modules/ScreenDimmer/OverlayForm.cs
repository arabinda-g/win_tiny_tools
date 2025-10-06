using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ScreenDimmer
{
    public partial class OverlayForm : Form
    {
        // P/Invoke declarations for layered window
        [DllImport("user32.dll")]
        private static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

        private const uint LWA_ALPHA = 0x00000002;

        public OverlayForm()
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
            
            // Set extended window style to make it layered and transparent to mouse clicks
            int exStyle = GetWindowLong(this.Handle, GWL_EXSTYLE);
            exStyle |= WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW;
            SetWindowLong(this.Handle, GWL_EXSTYLE, exStyle);
        }

        public void SetOpacity(byte opacity)
        {
            SetLayeredWindowAttributes(this.Handle, 0, opacity, LWA_ALPHA);
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

        // P/Invoke for window styles
        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_LAYERED = 0x80000;
        private const int WS_EX_TRANSPARENT = 0x20;
        private const int WS_EX_TOOLWINDOW = 0x80;
        private const int WS_EX_TOPMOST = 0x8;
    }
}
