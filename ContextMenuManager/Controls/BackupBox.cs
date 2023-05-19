using BluePointLilac.Controls;
using BluePointLilac.Methods;
using ContextMenuManager.Methods;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ContextMenuManager.Controls
{
    sealed class BackupBox : Panel
    {
        public BackupBox()
        {
            this.SuspendLayout();
            this.AutoScroll = true;
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.White;
            this.Font = SystemFonts.MenuFont;
            this.Font = new Font(this.Font.FontFamily, this.Font.Size + 1F);
            this.Controls.AddRange(new Control[] { Backup, Restore });
            this.VisibleChanged += (sender, e) => this.SetEnabled(this.Visible);
            Backup.Click += (sender, e) => { };
            Restore.Click += (sender, e) => { };
            this.ResumeLayout();
        }

        readonly Label Backup = new Label
        {
            Text = "备份",
            Cursor = Cursors.Hand,
            AutoSize = true
        };

        readonly Label Restore = new Label
        {
            Text = "恢复",
            Cursor = Cursors.Hand,
            AutoSize = true
        };

        protected override void OnResize(EventArgs e)
        {
            int margin = 40.DpiZoom();
            base.OnResize(e);
            Backup.Top = Restore.Top = (this.Height - Backup.Height) / 2;
            Backup.Left = (this.Width - margin) / 2 - Backup.Width;
            Restore.Left = Backup.Right + margin;
        }
    }
}