using BluePointLilac.Methods;
using ContextMenuManager.Methods;
using ContextMenuManager.Models;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ContextMenuManager.Controls
{
    sealed class BackupBox : Panel
    {
        private readonly BackupHelper helper = new BackupHelper();

        public BackupBox()
        {
            SuspendLayout();
            AutoScroll = true;
            Dock = DockStyle.Fill;
            BackColor = Color.White;
            Font = SystemFonts.MenuFont;
            Font = new Font(Font.FontFamily, Font.Size + 1F);
            Controls.AddRange(new Control[] { Backup, Restore });
            VisibleChanged += (sender, e) => this.SetEnabled(Visible);
            Backup.Click += (sender, e) => { helper.BackupItems(BackupList.BackupMode.Basic); };
            Restore.Click += (sender, e) => { };
            ResumeLayout();
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
            Backup.Top = Restore.Top = (Height - Backup.Height) / 2;
            Backup.Left = (Width - margin) / 2 - Backup.Width;
            Restore.Left = Backup.Right + margin;
        }
    }
}