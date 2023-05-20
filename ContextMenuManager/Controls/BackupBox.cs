using BluePointLilac.Controls;
using BluePointLilac.Methods;
using ContextMenuManager.Methods;
using ContextMenuManager.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ContextMenuManager.Controls
{
    sealed class BackupBox : Panel
    {
        private readonly BackupHelper helper = new BackupHelper();
        private List<RestoreFileItem> restoreFileList = new List<RestoreFileItem> { };

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);
        public static int MessageBox(string text, string caption, uint type = 0)
        {
            return MessageBox(new IntPtr(0), text, caption, type);
        }

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
            Backup.Click += (sender, e) => {
                Cursor = Cursors.WaitCursor; 
                helper.BackupItems(BackupList.BackupMode.Basic); 
                Cursor = Cursors.Default;
                MessageBox("备份完成！", "备份");
            };
            Restore.Click += (sender, e) => {
                UpdateRestoreFileList();
                using (RestoreListDialog dlg = new RestoreListDialog())
                {
                    dlg.BackupBox = this;
                    dlg.RestoreList = restoreFileList;
                    dlg.ShowDialog();
                };
            };
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

        public void RestoreItems(int restoreIndex)
        {
            if (restoreIndex == -1) return;
            BackupList.LoadBackupList(restoreFileList[restoreIndex].FilePath);
            helper.RestoreItems(BackupList.BackupMode.Basic);
            MessageBox("恢复完成！", "恢复");
        }

        private void UpdateRestoreFileList()
        {
            restoreFileList.Clear();
            string rootPath = AppConfig.MenuBackupRootDir;

            // 获取 rootPath 下的所有子目录
            string[] deviceDirs = Directory.GetDirectories(rootPath);

            foreach (string deviceDir in deviceDirs)
            {
                // 解析设备名称
                string deviceName = Path.GetFileName(deviceDir);

                // 获取当前设备目录下的所有 XML 文件
                string[] xmlFiles = Directory.GetFiles(deviceDir, "*.xml");

                foreach (string xmlFile in xmlFiles)
                {
                    // 解析源文件名称
                    string sourceFileName = Path.GetFileName(xmlFile);

                    // 打印设备名称、源文件名称和源文件路径
                    restoreFileList.Add(new RestoreFileItem
                    {
                        DeviceName = deviceName,
                        CreateTime = sourceFileName.Substring(0, sourceFileName.Length - 4),
                        FilePath = xmlFile,
                    });
                }
            }
        }

        class RestoreFileItem
        {
            public string DeviceName { get; set; }
            public string CreateTime { get; set; }
            public string FilePath { get; set; }
        }

        sealed class RestoreListDialog : CommonDialog
        {
            public BackupBox BackupBox { get; set; }
            public List<RestoreFileItem> RestoreList { get; set; }

            public override void Reset() { }

            protected override bool RunDialog(IntPtr hwndOwner)
            {
                using (RestoreListForm frm = new RestoreListForm())
                {
                    frm.BackupBox = BackupBox;
                    frm.RestoreListDialog = this;
                    frm.ShowRestoreList(RestoreList);
                    MainForm mainForm = (MainForm)FromHandle(hwndOwner);
                    frm.Left = mainForm.Left + (mainForm.Width + mainForm.SideBar.Width - frm.Width) / 2;
                    frm.Top = mainForm.Top + 150.DpiZoom();
                    frm.TopMost = AppConfig.TopMost;
                    frm.ShowDialog();
                }
                return true;
            }

            sealed class RestoreListForm : Form
            {
                public BackupBox BackupBox { get; set; }
                public RestoreListDialog RestoreListDialog { get; set; }

                private int GetSelectedRowIndex(DataGridView dgv)
                {
                    if (dgv.Rows.Count == 0)
                    {
                        return -1;
                    }
                    foreach (DataGridViewRow row in dgv.Rows)
                    {
                        if (row.Selected)
                        {
                            return row.Index;
                        }
                    }
                    return -1;
                }

                public RestoreListForm()
                {
                    Font = SystemFonts.DialogFont;
                    Text = "恢复列表";
                    SizeGripStyle = SizeGripStyle.Hide;
                    StartPosition = FormStartPosition.Manual;
                    Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                    MinimizeBox = MaximizeBox = ShowInTaskbar = false;
                    ClientSize = new Size(520, 350).DpiZoom();
                    MinimumSize = Size;
                    dgvRestore.ColumnHeadersDefaultCellStyle.Alignment
                        = dgvRestore.RowsDefaultCellStyle.Alignment
                        = DataGridViewContentAlignment.BottomCenter;
                    Controls.AddRange(new Control[] { dgvRestore, lblConfirm });
                    lblConfirm.MouseEnter += (sender, e) => lblConfirm.ForeColor = Color.FromArgb(0, 162, 255);
                    lblConfirm.MouseLeave += (sender, e) => lblConfirm.ForeColor = Color.DimGray;
                    lblConfirm.Click += (sender, e) => {
                        int restoreIndex = GetSelectedRowIndex(dgvRestore);
                        BackupBox.RestoreItems(restoreIndex);
                    };
                    this.AddEscapeButton();
                }

                readonly DataGridView dgvRestore = new DataGridView
                {
                    ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells,
                    SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                    BackgroundColor = SystemColors.Control,
                    BorderStyle = BorderStyle.None,
                    AllowUserToResizeRows = false,
                    AllowUserToAddRows = false,
                    RowHeadersVisible = false,
                    MultiSelect = false,
                    ReadOnly = true
                };

                readonly Label lblConfirm = new Label
                {
                    Text = "确定",
                    Cursor = Cursors.Hand,
                    AutoSize = true,
                };

                protected override void OnResize(EventArgs e)
                {
                    base.OnResize(e);
                    int margin = 20.DpiZoom();
                    dgvRestore.Location = new Point(margin, margin);
                    dgvRestore.Width = ClientSize.Width - 2 * margin;
                    dgvRestore.Height = ClientSize.Height - 3 * margin - lblConfirm.Height;
                    lblConfirm.Top = dgvRestore.Bottom + margin;
                    lblConfirm.Left = (ClientSize.Width - lblConfirm.Width) / 2;
                }

                public void ShowRestoreList(List<RestoreFileItem> restoreFileItems)
                {
                    string[] heads = new string[] { "源计算机", "创建于" };
                    int headLength = heads.Length;
                    dgvRestore.ColumnCount = headLength;
                    dgvRestore.Columns[headLength - 1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    for (int m = 0; m < headLength; m++)
                    {
                        dgvRestore.Columns[m].HeaderText = heads[m];
                    }
                    foreach(RestoreFileItem item in restoreFileItems)
                    {
                        string[] line = new string[headLength];
                        line[0] = item.DeviceName;
                        line[1] = item.CreateTime;
                        dgvRestore.Rows.Add(line);
                    }
                    dgvRestore.Sort(dgvRestore.Columns[0], ListSortDirection.Descending);
                }
            }
        }
    }
}