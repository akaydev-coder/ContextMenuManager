using BluePointLilac.Methods;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace BluePointLilac.Controls
{
    public class MyMainForm : Form
    {
        public MyMainForm()
        {
            SuspendLayout();
            Text = Application.ProductName;
            ForeColor = Color.FromArgb(80, 80, 80);
            BackColor = Color.FromArgb(250, 250, 250);
            StartPosition = FormStartPosition.CenterScreen;
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            Controls.AddRange(new Control[] { MainBody, SideBar, StatusBar, ToolBar });
            SideBar.Resize += (sender, e) => OnResize(null);
            ClientSize = new Size(850, 610).DpiZoom();
            MinimumSize = Size;
            MainBody.Dock = DockStyle.Left;
            StatusBar.CanMoveForm();
            ToolBar.CanMoveForm();
            ResumeLayout();
        }

        public readonly MyToolBar ToolBar = new MyToolBar();
        public readonly MySideBar SideBar = new MySideBar();
        public readonly MyStatusBar StatusBar = new MyStatusBar();
        public readonly MyListBox MainBody = new MyListBox();

        /// <summary>窗体移动时是否临时挂起MainBody</summary>
        public bool SuspendMainBodyWhenMove { get; set; } = false;
        /// <summary>窗体调整大小时是否临时挂起MainBody</summary>
        public bool SuspendMainBodyWhenResize { get; set; } = true;

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            MainBody.Width = ClientSize.Width - SideBar.Width;
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_NCLBUTTONDBLCLK = 0x00A3;
            const int WM_SYSCOMMAND = 0x0112;
            const int SC_MAXIMIZE = 0xF030;
            const int SC_MINIMIZE = 0xF020;
            const int SC_RESTORE = 0xF120;
            const int SC_MOVE = 0xF012;
            const int SC_SIZE = 0xF000;
            const int HT_CAPTION = 0x2;
            bool suspend = false;//临时挂起MainBody
            switch(m.Msg)
            {
                case WM_SYSCOMMAND:
                    switch(m.WParam.ToInt32())
                    {
                        //解决控件过多移动窗体时延迟问题
                        case SC_MOVE:
                        //解决控件过多调整窗体大小时延迟问题
                        case SC_SIZE:
                            suspend = SuspendMainBodyWhenMove; break;
                        //解决控件过多最大化、最小化、还原重绘卡顿问题
                        case SC_RESTORE:
                        case SC_MINIMIZE:
                        case SC_MAXIMIZE:
                            suspend = SuspendMainBodyWhenResize; break;
                    }
                    break;
                case WM_NCLBUTTONDBLCLK:
                    switch(m.WParam.ToInt32())
                    {
                        //双击标题栏最大化和还原窗口
                        case HT_CAPTION:
                            suspend = SuspendMainBodyWhenResize; break;
                    }
                    break;
            }
            if(suspend)
            {
                SuspendLayout();
                MainBody.SuspendLayout();
                Controls.Remove(MainBody);
                base.WndProc(ref m);
                Controls.Add(MainBody);
                MainBody.BringToFront();
                MainBody.ResumeLayout();
                ResumeLayout();
            }
            else base.WndProc(ref m);
        }
    }
}