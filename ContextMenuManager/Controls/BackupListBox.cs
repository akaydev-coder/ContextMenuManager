using BluePointLilac.Controls;
using ContextMenuManager.Methods;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

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
                BackupMode backupMode;
                List<string> backupScenes;
                using (BackupDialog dlg = new BackupDialog())
                {
                    dlg.Title = "新建一个备份";
                    dlg.DgvTitle = "备份内容：";
                    dlg.DgvItems = BackupHelper.BackupScenesText;
                    dlg.CmbTitle = "备份模式：";
                    dlg.CmbItems = new[] { "备份全部菜单项目", "仅备份已启用的菜单项目" };
                    if (dlg.ShowDialog() != DialogResult.OK) return;
                    backupMode = dlg.CmbSelectedIndex == 0 ? BackupMode.All : BackupMode.OnlyVisible;
                    backupScenes = dlg.DgvSelectedItems;
                }
                // 开始备份
                Cursor = Cursors.WaitCursor;
                helper.BackupItems(backupScenes, backupMode);
                AddItem(new RestoreItem(this, helper.filePath, AppConfig.ComputerHostName, helper.createTime));
                Cursor = Cursors.Default;
                int backupCount = helper.backupCount;
                AppMessageBox.Show("备份完成！共处理了" + backupCount.ToString() + "个菜单项目！");
            };
        }

        public void RestoreItems(string filePath)
        {
            RestoreMode restoreMode;
            List<string> restoreScenes;
            BackupList.LoadBackupDataMetaData(filePath);
            helper.GetBackupRestoreScenesText(BackupList.metaData.BackupScenes);
            using (BackupDialog dlg = new BackupDialog())
            {
                dlg.Title = "恢复一个备份";
                dlg.DgvTitle = "恢复内容：";
                dlg.DgvItems = BackupHelper.RestoreScenesText;
                dlg.CmbTitle = "恢复模式：";
                dlg.CmbItems = new[] { "不处理不存在于备份列表上的菜单项", "关闭不存在于备份列表上的菜单项" };
                if (dlg.ShowDialog() != DialogResult.OK) return;
                restoreMode = dlg.CmbSelectedIndex == 0 ? RestoreMode.NotHandleNotOnList : RestoreMode.DisableNotOnList;
                restoreScenes = dlg.DgvSelectedItems;
            }
            // 开始恢复
            Cursor = Cursors.WaitCursor;
            helper.RestoreItems(filePath, restoreScenes, restoreMode);
            Cursor = Cursors.Default;
            int changeCount = helper.changeCount;
            AppMessageBox.Show("恢复完成！共处理了" + changeCount.ToString() + "个菜单项目！");
        }
    }
}