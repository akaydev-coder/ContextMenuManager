using BluePointLilac.Controls;
using ContextMenuManager.Methods;
using System;
using System.Collections.Generic;
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

        // 备份
        private List<RestoreFileItem> restoreFileList = new List<RestoreFileItem> { };

        public void LoadItems()
        {
            UpdateRestoreFileList();
            foreach (RestoreFileItem item in restoreFileList)
            {
                AddItem(new RestoreItem(this, item.FilePath, item.DeviceName, item.CreateTime));
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
                // 新增新加入的备份项目

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

        sealed class RestoreFileItem
        {
            public string DeviceName { get; set; }
            public string CreateTime { get; set; }
            public string FilePath { get; set; }
        }

        private bool UpdateRestoreFileList()
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

            // 如果存在备份返回true
            return restoreFileList.Count > 0;
        }
    }
}