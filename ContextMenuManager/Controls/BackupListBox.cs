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
            // 仅获取deviceDir下的.xml备份文件
            foreach (string deviceDir in deviceDirs)
            {
                // 获取当前设备目录下的所有XML文件
                string[] xmlFiles = Directory.GetFiles(deviceDir, "*.xml");
                // 遍历所有XML文件
                foreach (string xmlFile in xmlFiles)
                {
                    // 加载项目元数据
                    BackupList.LoadBackupDataMetaData(xmlFile);
                    // 新增备份项目
                    string deviceName = BackupList.metaData?.Device;
                    string createTime = BackupList.metaData?.CreateTime.ToString("G");
                    AddItem(new RestoreItem(this, xmlFile, deviceName ?? AppString.Other.Unknown, createTime ?? AppString.Other.Unknown));
                }
            }
            SortItemByText();
            AddNewBackupItem();
        }

        private void AddNewBackupItem()
        {
            NewItem newItem = new NewItem(AppString.Dialog.NewBackupItem);
            InsertItem(newItem, 0);
            newItem.AddNewItem += BackupItems;
        }

        private void BackupItems()
        {
            // 获取备份选项
            BackupMode backupMode;
            List<string> backupScenes;
            using (BackupDialog dlg = new BackupDialog())
            {
                dlg.Title = AppString.Dialog.NewBackupItem;
                dlg.DgvTitle = AppString.Dialog.BackupContent;
                dlg.DgvItems = BackupHelper.BackupScenesText;
                dlg.CmbTitle = AppString.Dialog.BackupMode;
                dlg.CmbItems = new[] { AppString.Dialog.BackupMode1, AppString.Dialog.BackupMode2, AppString.Dialog.BackupMode3 };
                if (dlg.ShowDialog() != DialogResult.OK) return;
                switch (dlg.CmbSelectedIndex)
                {
                    case 0:
                    default:
                        backupMode = BackupMode.All; break;
                    case 1:
                        backupMode = BackupMode.OnlyVisible; break;
                    case 2:
                        backupMode = BackupMode.OnlyInvisible; break;
                }
                backupScenes = dlg.DgvSelectedItems;
            }
            // 开始备份项目
            Cursor = Cursors.WaitCursor;
            helper.BackupItems(backupScenes, backupMode);
            Cursor = Cursors.Default;
            // 新增备份项目（项目已加载元数据）
            string deviceName = BackupList.metaData.Device;
            string createTime = BackupList.metaData.CreateTime.ToString("G");
            AddItem(new RestoreItem(this, helper.filePath, deviceName, createTime));
            // 弹窗提示结果
            int backupCount = helper.backupCount;
            AppMessageBox.Show(AppString.Message.BackupSucceeded.Replace("%s", backupCount.ToString()));
        }

        public void RestoreItems(string filePath)
        {
            // 获取恢复选项
            RestoreMode restoreMode;
            List<string> restoreScenes;
            BackupList.LoadBackupDataMetaData(filePath);
            // 备份版本提示
            if (BackupList.metaData.Version < BackupHelper.BackupVersion)
            {
                AppMessageBox.Show(AppString.Message.OldBackupVersion);
            }
            helper.GetBackupRestoreScenesText(BackupList.metaData.BackupScenes);
            using (BackupDialog dlg = new BackupDialog())
            {
                dlg.Title = AppString.Dialog.RestoreBackupItem;
                dlg.DgvTitle = AppString.Dialog.RestoreContent;
                dlg.DgvItems = BackupHelper.RestoreScenesText;
                dlg.CmbTitle = AppString.Dialog.RestoreMode;
                dlg.CmbItems = new[] { AppString.Dialog.RestoreMode1, AppString.Dialog.RestoreMode2, AppString.Dialog.RestoreMode3 };
                if (dlg.ShowDialog() != DialogResult.OK) return;
                switch (dlg.CmbSelectedIndex)
                {
                    case 0:
                    default:
                        restoreMode = RestoreMode.NotHandleNotOnList; break;
                    case 1:
                        restoreMode = RestoreMode.DisableNotOnList; break;
                    case 2:
                        restoreMode = RestoreMode.EnableNotOnList; break;
                }
                restoreScenes = dlg.DgvSelectedItems;
            }
            // 开始恢复项目
            Cursor = Cursors.WaitCursor;
            helper.RestoreItems(filePath, restoreScenes, restoreMode);
            Cursor = Cursors.Default;
            // 弹窗提示结果
            int restoreCount = helper.restoreCount;
            AppMessageBox.Show(AppString.Message.BackupSucceeded.Replace("%s", restoreCount.ToString()));
        }
    }
}