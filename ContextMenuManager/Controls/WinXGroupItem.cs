using BluePointLilac.Methods;
using ContextMenuManager.Controls.Interfaces;
using ContextMenuManager.Methods;
using System;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace ContextMenuManager.Controls
{
    sealed class WinXGroupItem : FoldGroupItem, IChkVisibleItem, ITsiDeleteItem, ITsiTextItem
    {
        // TODO:适配Win11
        public WinXGroupItem(string groupPath) : base(groupPath, ObjectPath.PathType.Directory)
        {
            InitializeComponents();
            RefreshKeyPath();
        }

        private string keyPath = null;
        private void RefreshKeyPath()
        {
            if (WinOsVersion.Current >= WinOsVersion.Win11)
            {
                keyPath = GroupPath.Substring(WinXList.WinXPath.Length);
            }
        }

        private string BackupGroupPath => $@"{WinXList.BackupWinXPath}{keyPath}";
        private string DefaultGroupPath => $@"{WinXList.DefaultWinXPath}{keyPath}";

        public bool ItemVisible
        {
            get
            {
                return (WinOsVersion.Current >= WinOsVersion.Win11) ?
                    Directory.GetFiles(GroupPath, "*.lnk").Length != 0 :
                    (File.GetAttributes(GroupPath) & FileAttributes.Hidden) != FileAttributes.Hidden;
            }
            set
            {
                if (WinOsVersion.Current >= WinOsVersion.Win11)
                {
                    // 处理用户WinX菜单目录
                    if (value)
                    {

                    }   
                    // 处理默认WinX菜单目录
                    
                }
                else
                {
                    FileAttributes attributes = File.GetAttributes(GroupPath);
                    if (value) attributes &= ~FileAttributes.Hidden;
                    else attributes |= FileAttributes.Hidden;
                    File.SetAttributes(GroupPath, attributes);
                    if (Directory.GetFiles(GroupPath, "*.lnk").Length > 0) ExplorerRestarter.Show();
                }
            }
        }

        public string ItemText
        {
            get => Path.GetFileNameWithoutExtension(GroupPath);
            set
            {
                string newPath = $@"{WinXList.WinXPath}\{ObjectPath.RemoveIllegalChars(value)}";
                Directory.Move(GroupPath, newPath);
                GroupPath = newPath;
                ExplorerRestarter.Show();
            }
        }

        public VisibleCheckBox ChkVisible { get; set; }
        public DeleteMeMenuItem TsiDeleteMe { get; set; }
        public ChangeTextMenuItem TsiChangeText { get; set; }
        readonly ToolStripMenuItem TsiRestoreDefault = new ToolStripMenuItem(AppString.Menu.RestoreDefault);

        private string DefaultFolderPath => $@"{((WinOsVersion.Current >= WinOsVersion.Win11) ? WinXList.WinXDefaultPath : WinXList.DefaultWinXPath)}\{ItemText}";

        private void InitializeComponents()
        {
            ChkVisible = new VisibleCheckBox(this);
            SetCtrIndex(ChkVisible, 1);
            TsiDeleteMe = new DeleteMeMenuItem(this);
            TsiChangeText = new ChangeTextMenuItem(this);
            ContextMenuStrip.Items.AddRange(new ToolStripItem[] { new ToolStripSeparator(),
                TsiChangeText, TsiRestoreDefault, new ToolStripSeparator(), TsiDeleteMe });
            ContextMenuStrip.Opening += (sender, e) => TsiRestoreDefault.Enabled = Directory.Exists(DefaultFolderPath);
            TsiRestoreDefault.Click += (sender, e) => RestoreDefault();
        }

        private void RestoreDefault()
        {
            if(AppMessageBox.Show(AppString.Message.RestoreDefault, MessageBoxButtons.OKCancel) == DialogResult.OK)
            {
                RestoreDefaultFolder(true);
                if (WinOsVersion.Current >= WinOsVersion.Win11)
                {
                    // Win11需要将默认WinX菜单也恢复，同时删除备份WinX菜单
                    RestoreDefaultFolder(false);
                    if (Directory.Exists(BackupGroupPath))
                    {
                        DirectoryInfo defaultWinXDir = new DirectoryInfo(BackupGroupPath);
                        defaultWinXDir.Delete(true);
                    }
                }

                WinXList list = (WinXList)Parent;
                list.ClearItems();
                list.LoadItems();
                ExplorerRestarter.Show();
            }
        }
        private void RestoreDefaultFolder(bool isWinX)
        {
            string meGroupPath = isWinX ? GroupPath : DefaultGroupPath;

            File.SetAttributes(meGroupPath, FileAttributes.Normal);
            Directory.Delete(meGroupPath, true);
            Directory.CreateDirectory(meGroupPath);
            File.SetAttributes(meGroupPath, File.GetAttributes(DefaultFolderPath));

            foreach (string srcPath in Directory.GetFiles(DefaultFolderPath))
            {
                string dstPath = $@"{meGroupPath}\{Path.GetFileName(srcPath)}";
                File.Copy(srcPath, dstPath);
            }
        }

        // TODO:适配Win11()
        public void DeleteMe()
        {
            bool flag = Directory.GetFiles(GroupPath, "*.lnk").Length > 0;
            if(flag && AppMessageBox.Show(AppString.Message.DeleteGroup, MessageBoxButtons.OKCancel) != DialogResult.OK) return;
            File.SetAttributes(GroupPath, FileAttributes.Normal);
            Directory.Delete(GroupPath, true);
            if(flag)
            {
                WinXList list = (WinXList)Parent;
                list.ClearItems();
                list.LoadItems();
                ExplorerRestarter.Show();
            }
        }
    }
}