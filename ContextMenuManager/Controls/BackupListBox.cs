using BluePointLilac.Controls;
using ContextMenuManager.Methods;
using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using static ContextMenuManager.Methods.BackupList;

namespace ContextMenuManager.Controls
{
    sealed class BackupListBox : MyList, ITsiRestoreFile
    {
        private readonly BackupHelper helper = new BackupHelper();

        public void LoadItems()
        {
            string rootPath = AppConfig.MenuBackupRootDir;
            // 获取rootPath下的所有子目录
            string[] deviceDirs = Directory.GetDirectories(rootPath);
            foreach (string deviceDir in deviceDirs)
            {
                // 解析设备名称
                string deviceName = Path.GetFileName(deviceDir);
                // 获取当前设备目录下的所有XML文件
                string[] xmlFiles = Directory.GetFiles(deviceDir, "*.xml");
                // 遍历所有XML文件
                foreach (string xmlFile in xmlFiles)
                {
                    // 解析源文件名称
                    string sourceFileName = Path.GetFileName(xmlFile);
                    // 添加备份项目
                    string createTime = sourceFileName.Substring(0, sourceFileName.Length - 4);
                    AddItem(new RestoreItem(this, xmlFile, deviceName, createTime));
                }
            }
            SortItemByText();
            AddNewBackupItem();
        }

        private void AddNewBackupItem()
        {
            NewItem newItem = new NewItem("新建一个备份");
            InsertItem(newItem, 0);
            newItem.AddNewItem += () =>
            {
                BackupTarget backupTarget;
                using (SelectDialog dlg = new SelectDialog())
                {
                    dlg.Items = new[] { "基本", "详细" };
                    dlg.Title = "新建一个备份";
                    if (dlg.ShowDialog() != DialogResult.OK) return;
                    backupTarget = dlg.SelectedIndex == 0 ? BackupTarget.Basic : BackupTarget.AllHomePage;
                }
                Cursor = Cursors.WaitCursor;
                helper.BackupItems(backupTarget);
                AddItem(new RestoreItem(this, helper.filePath, AppConfig.ComputerHostName, helper.createTime));
                Cursor = Cursors.Default;
                int backupCount = helper.backupCount;
                AppMessageBox.Show("备份完成！共处理了" + backupCount.ToString() + "个菜单项目！");
            };
        }

        public void RestoreItems(string restoreFile, RestoreMode restoreMode)
        {
            Cursor = Cursors.WaitCursor;
            helper.RestoreItems(BackupTarget.Basic, restoreFile, restoreMode);
            Cursor = Cursors.Default;
            int changeCount = helper.changeCount;
            AppMessageBox.Show("恢复完成！共处理了" + changeCount.ToString() + "个菜单项目！");
        }
    }
}