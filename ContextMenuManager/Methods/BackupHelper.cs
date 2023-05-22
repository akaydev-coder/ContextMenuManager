using BluePointLilac.Methods;
using ContextMenuManager.Controls;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ContextMenuManager.Controls.ShellList;
using System.Xml;
using System.Drawing;
using static ContextMenuManager.Methods.BackupList;
using System.Xml.Serialization;
using static ContextMenuManager.Controls.ShellNewList;
using System.Windows.Markup.Localizer;
using BluePointLilac.Controls;
using System.Web.UI;

namespace ContextMenuManager.Methods
{
    sealed class BackupHelper
    {
        private Scenes currentScene;
        private RestoreMode restoreMode;
        public int backupCount = 0;
        public int changeCount = 0;
        public string createTime;
        public string filePath;

        public void BackupItems(BackupTarget mode)
        {
            ClearBackupList();
            string date = DateTime.Today.ToString("yyyy-MM-dd");
            string time = DateTime.Now.ToString("HH-mm-ss");
            createTime = $@"{date} {time}";
            filePath = $@"{AppConfig.MenuBackupDir}\{createTime}.xml";
            // 加载备份文件到缓冲区
            BackupRestoreItems(mode, true);
            // 保存缓冲区的备份文件
            SaveBackupList(filePath);
            backupCount = GetBackupListCount();
            ClearBackupList();
        }

        public void RestoreItems(BackupTarget mode, string filePath, RestoreMode restoreMode)
        {
            ClearBackupList();
            changeCount = 0;
            this.restoreMode = restoreMode;
            // 加载备份文件到缓冲区
            LoadBackupList(filePath);
            // 还原缓冲区的备份文件
            BackupRestoreItems(mode, false);
            ClearBackupList();
        }

        private void BackupRestoreItems(BackupTarget mode, bool backup)
        {
            Scenes[] scenes = new Scenes[] {
                Scenes.File, Scenes.Folder, Scenes.Directory, Scenes.Background, Scenes.Desktop,
                Scenes.Drive, Scenes.AllObjects, Scenes.Computer, Scenes.RecycleBin, Scenes.Library,
                Scenes.NewItem, Scenes.SendTo, Scenes.OpenWith
            };
            switch (mode)
            {
                case BackupTarget.Basic:
                    break;
                case BackupTarget.AllHomePage:
                    break;
            }
            for (int i = 0; i < scenes.Length; i++)
            {
                currentScene = scenes[i];
                // 加载某个Scene的恢复列表
                if (!backup)
                {
                    LoadTempRestoreList(currentScene);
                }
                GetBackupItems(backup);
            }
        }

        private bool CheckItemNeedChange(string keyName, BackupItemType itemType, bool itemVisible)
        {
            foreach(BackupItem item in tempRestoreList)
            {
                // 成功匹配到后的处理方式
                if(item.KeyName == keyName && item.ItemType == itemType)
                {
                    if (item.ItemVisible != itemVisible)
                    {
                        changeCount++;
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            switch(restoreMode)
            {
                case RestoreMode.DisableNotOnList:
                    if (itemVisible) changeCount++;
                    return itemVisible;
                case RestoreMode.NotHandleNotOnList:
                default:
                    return false;
            }
        }

        private void BackupRestoreItem(MyListItem item, string keyName, BackupItemType backupItemType, bool ifItemInMenu, Scenes currentScene, bool backup)
        {
            if (backup)
            {
                // 加入备份列表
                AddItem(keyName, backupItemType, ifItemInMenu, currentScene);
            }
            else
            {
                // 恢复备份列表
                if (CheckItemNeedChange(keyName, backupItemType, ifItemInMenu))
                {
                    switch (backupItemType)
                    {
                        case BackupItemType.ShellItem:
                            ((ShellItem)item).ItemVisible = !ifItemInMenu; break;
                        case BackupItemType.ShellExItem:
                            ((ShellExItem)item).ItemVisible = !ifItemInMenu; break;
                        case BackupItemType.UwpModelItem:
                            ((UwpModeItem)item).ItemVisible = !ifItemInMenu; break;
                        case BackupItemType.VisibleRegRuleItem:
                            ((VisibleRegRuleItem)item).ItemVisible = !ifItemInMenu; break;
                        case BackupItemType.ShellNewItem:
                            ((ShellNewItem)item).ItemVisible = !ifItemInMenu; break;
                        case BackupItemType.SendToItem:
                            ((SendToItem)item).ItemVisible = !ifItemInMenu; break;
                        case BackupItemType.OpenWithItem:
                            ((OpenWithItem)item).ItemVisible = !ifItemInMenu; break;
                    }
                }
            }
            // 释放资源
            item.Dispose();
        }

        private void GetBackupItems(bool backup)
        {
            switch (currentScene)
            {
                case Scenes.NewItem:
                    // 新建右键菜单
                    GetShellNewListBackupItems(backup); break;
                case Scenes.SendTo:
                    // 发送到右键菜单
                    GetSendToListItems(backup); break;
                case Scenes.OpenWith:
                    // 打开方式右键菜单
                    GetOpenWithListItems(backup); break;
                default:
                    // 位于ShellList.cs内的备份项目
                    GetShellListItems(backup); break;
            }
        }

        /*******************************ShellList.cs内************************************/

        private void GetShellListItems(bool backup)
        {
            string scenePath = null;
            switch (currentScene)
            {
                case Scenes.File:
                    scenePath = MENUPATH_FILE; break;
                case Scenes.Folder:
                    scenePath = MENUPATH_FOLDER; break;
                case Scenes.Directory:
                    scenePath = MENUPATH_DIRECTORY; break;
                case Scenes.Background:
                    scenePath = MENUPATH_BACKGROUND; break;
                case Scenes.Desktop:
                    //Vista系统没有这一项
                    if (WinOsVersion.Current == WinOsVersion.Vista) return;
                    scenePath = MENUPATH_DESKTOP; break;
                case Scenes.Drive:
                    scenePath = MENUPATH_DRIVE; break;
                case Scenes.AllObjects:
                    scenePath = MENUPATH_ALLOBJECTS; break;
                case Scenes.Computer:
                    scenePath = MENUPATH_COMPUTER; break;
                case Scenes.RecycleBin:
                    scenePath = MENUPATH_RECYCLEBIN; break;
                case Scenes.Library:
                    //Vista系统没有这一项
                    if (WinOsVersion.Current == WinOsVersion.Vista) return;
                    scenePath = MENUPATH_LIBRARY; break;
            }
#if DEBUG
            if (AppConfig.EnableLog)
            {
                using (StreamWriter sw = new StreamWriter(AppConfig.DebugLogPath, true))
                {
                    sw.WriteLine($@"BackupItems: {currentScene}");
                }
            }
            int i = 0;
#endif
            // 获取ShellItem与ShellExItem类的备份项目
            GetBackupItems(scenePath, backup);
            if (WinOsVersion.Current >= WinOsVersion.Win10)
            {
                // 获取UwpModeItem类的备份项目
                GetBackupUwpModeItem(backup);
            }
            switch (currentScene)
            {
                case Scenes.Background:
                    VisibleRegRuleItem item = new VisibleRegRuleItem(VisibleRegRuleItem.CustomFolder);
                    string regPath = item.RegPath;
                    string valueName = item.ValueName;
                    string itemName = item.Text;
                    bool ifItemInMenu = item.ItemVisible;
                    BackupRestoreItem(item, valueName, BackupItemType.VisibleRegRuleItem, ifItemInMenu, currentScene, backup);
#if DEBUG
                    i++;
                    if (AppConfig.EnableLog)
                    {
                        using (StreamWriter sw = new StreamWriter(AppConfig.DebugLogPath, true))
                        {
                            sw.WriteLine("\tBackupAddedItems");
                            sw.WriteLine("\t\t" + $@"{i}. {valueName} {itemName} {ifItemInMenu} {regPath}");
                        }
                    }
#endif
                    break;
                case Scenes.Computer:
                    item = new VisibleRegRuleItem(VisibleRegRuleItem.NetworkDrive);
                    regPath = item.RegPath;
                    valueName = item.ValueName;
                    itemName = item.Text;
                    ifItemInMenu = item.ItemVisible;
                    BackupRestoreItem(item, valueName, BackupItemType.VisibleRegRuleItem, ifItemInMenu, currentScene, backup);
#if DEBUG
                    i++;
                    if (AppConfig.EnableLog)
                    {
                        using (StreamWriter sw = new StreamWriter(AppConfig.DebugLogPath, true))
                        {
                            sw.WriteLine("\tBackupAddedItems");
                            sw.WriteLine("\t\t" + $@"{i}. {valueName} {itemName} {ifItemInMenu} {regPath}");
                        }
                    }
#endif
                    break;
                case Scenes.RecycleBin:
                    item = new VisibleRegRuleItem(VisibleRegRuleItem.RecycleBinProperties);
                    regPath = item.RegPath;
                    valueName = item.ValueName;
                    itemName = item.Text;
                    ifItemInMenu = item.ItemVisible;
                    BackupRestoreItem(item, valueName, BackupItemType.VisibleRegRuleItem, ifItemInMenu, currentScene, backup);
#if DEBUG
                    i++;
                    if (AppConfig.EnableLog)
                    {
                        using (StreamWriter sw = new StreamWriter(AppConfig.DebugLogPath, true))
                        {
                            sw.WriteLine("\tBackupAddedItems");
                            sw.WriteLine("\t\t" + $@"{i}. {valueName} {itemName} {ifItemInMenu} {regPath}");
                        }
                    }
#endif
                    break;
                case Scenes.Library:
#if DEBUG
                    if (AppConfig.EnableLog)
                    {
                        using (StreamWriter sw = new StreamWriter(AppConfig.DebugLogPath, true))
                        {
                            sw.WriteLine("\tBackupAddedItems");
                        }
                    }
#endif
                    string[] AddedScenePathes = new string[] { MENUPATH_LIBRARY_BACKGROUND, MENUPATH_LIBRARY_USER };
                    RegTrustedInstaller.TakeRegKeyOwnerShip(scenePath);
                    for (int j = 0; j < AddedScenePathes.Length; j++)
                    {
                        scenePath = AddedScenePathes[j];
                        GetBackupShellItems(GetShellPath(scenePath), backup);
                        GetBackupShellExItems(GetShellExPath(scenePath), backup);
                    }
                    break;
            }
        }

        private void GetBackupItems(string scenePath, bool backup)
        {
            if (scenePath == null) return;
            RegTrustedInstaller.TakeRegKeyOwnerShip(scenePath);
            GetBackupShellItems(GetShellPath(scenePath), backup);
            GetBackupShellExItems(GetShellExPath(scenePath), backup);
        }

        private void GetBackupShellItems(string shellPath, bool backup)
        {
#if DEBUG
            if (AppConfig.EnableLog)
            {
                using (StreamWriter sw = new StreamWriter(AppConfig.DebugLogPath, true))
                {
                    sw.WriteLine("\tGetBackupShellItems");
                }
            }
            int i = 0;
#endif
            using (RegistryKey shellKey = RegistryEx.GetRegistryKey(shellPath))
            {
                if (shellKey == null) return;
                RegTrustedInstaller.TakeRegTreeOwnerShip(shellKey.Name);
                foreach (string keyName in shellKey.GetSubKeyNames())
                {
                    string regPath = $@"{shellPath}\{keyName}";
                    ShellItem item = new ShellItem(regPath);
                    string itemName = item.ItemText;
                    bool ifItemInMenu = item.ItemVisible;
                    BackupRestoreItem(item, keyName, BackupItemType.ShellItem, ifItemInMenu, currentScene, backup);
#if DEBUG
                    i++;
                    if (AppConfig.EnableLog)
                    {
                        using (StreamWriter sw = new StreamWriter(AppConfig.DebugLogPath, true))
                        {
                            sw.WriteLine("\t\t" + $@"{i}. {keyName} {itemName} {ifItemInMenu} {regPath}");
                        }
                    }
#endif
                }
            }
        }

        private void GetBackupShellExItems(string shellExPath, bool backup)
        {
#if DEBUG
            if (AppConfig.EnableLog)
            {
                using (StreamWriter sw = new StreamWriter(AppConfig.DebugLogPath, true))
                {
                    sw.WriteLine("\tGetBackupShellExItems");
                }
            }
            int i = 0;
#endif
            List<string> names = new List<string>();
            using (RegistryKey shellExKey = RegistryEx.GetRegistryKey(shellExPath))
            {
                if (shellExKey == null) return;
                bool isDragDrop = currentScene == Scenes.DragDrop;
                RegTrustedInstaller.TakeRegTreeOwnerShip(shellExKey.Name);
                Dictionary<string, Guid> dic = ShellExItem.GetPathAndGuids(shellExPath, isDragDrop);
                FoldGroupItem groupItem = null;
                if (isDragDrop)
                {
                    groupItem = GetDragDropGroupItem(shellExPath);
#if DEBUG
                    if (AppConfig.EnableLog)
                    {
                        using (StreamWriter sw = new StreamWriter(AppConfig.DebugLogPath, true))
                        {
                            sw.WriteLine($@"\t\t!!!!!!{shellExPath}(FoldGroupItem)");
                        }
                    }
#endif
                }
                foreach (string path in dic.Keys)
                {
                    string keyName = RegistryEx.GetKeyName(path);
                    if (!names.Contains(keyName))
                    {
                        string regPath = path; // 随是否显示于右键菜单中而改变
                        Guid guid = dic[path];
                        ShellExItem item = new ShellExItem(guid, path);
                        string itemName = item.ItemText;
                        bool ifItemInMenu = item.ItemVisible;
                        if (groupItem != null)
                        {
                            item.FoldGroupItem = groupItem;
                            item.Indent();
                        }
                        BackupRestoreItem(item, keyName, BackupItemType.ShellExItem, ifItemInMenu, currentScene, backup);
                        names.Add(keyName);
#if DEBUG
                        i++;
                        if (AppConfig.EnableLog)
                        {
                            using (StreamWriter sw = new StreamWriter(AppConfig.DebugLogPath, true))
                            {
                                sw.WriteLine("\t\t" + $@"{i}. {keyName} {itemName} {ifItemInMenu} {regPath}");
                            }
                        }
#endif
                    }
                }
            }
        }

        private void GetBackupUwpModeItem(bool backup)
        {
#if DEBUG
            if (AppConfig.EnableLog)
            {
                using (StreamWriter sw = new StreamWriter(AppConfig.DebugLogPath, true))
                {
                    sw.WriteLine("\tGetBackupUwpModeItem");
                }
            }
            int i = 0;
#endif
            List<Guid> guidList = new List<Guid>();
            foreach (XmlDocument doc in XmlDicHelper.UwpModeItemsDic)
            {
                if (doc?.DocumentElement == null) continue;
                foreach (XmlNode sceneXN in doc.DocumentElement.ChildNodes)
                {
                    if (sceneXN.Name == currentScene.ToString())
                    {
                        foreach (XmlElement itemXE in sceneXN.ChildNodes)
                        {
                            if (GuidEx.TryParse(itemXE.GetAttribute("Guid"), out Guid guid))
                            {
                                if (guidList.Contains(guid)) continue;
                                if (GuidInfo.GetFilePath(guid) == null) continue;
                                guidList.Add(guid);
                                string uwpName = GuidInfo.GetUwpName(guid); // uwp程序的名称
                                UwpModeItem uwpItem = new UwpModeItem(uwpName, guid);
                                string keyName = uwpItem.Text; // 右键菜单索引
                                // TODO:修复名称显示错误的问题
                                string itemName = keyName;  // 右键菜单名称
                                bool ifItemInMenu = uwpItem.ItemVisible;
                                BackupRestoreItem(uwpItem, keyName, BackupItemType.UwpModelItem, ifItemInMenu, currentScene, backup);
#if DEBUG
                                i++;
                                if (AppConfig.EnableLog)
                                {
                                    using (StreamWriter sw = new StreamWriter(AppConfig.DebugLogPath, true))
                                    {
                                        sw.WriteLine("\t\t" + $@"{i}. {keyName}({uwpName}) {itemName} {ifItemInMenu} {guid}");
                                    }
                                }
#endif
                            }
                        }
                    }
                }
            }
        }

        private FoldGroupItem GetDragDropGroupItem(string shellExPath)
        {
            string text = null;
            Image image = null;
            string path = shellExPath.Substring(0, shellExPath.LastIndexOf('\\'));
            switch (path)
            {
                case MENUPATH_FOLDER:
                    text = AppString.SideBar.Folder;
                    image = AppImage.Folder;
                    break;
                case MENUPATH_DIRECTORY:
                    text = AppString.SideBar.Directory;
                    image = AppImage.Directory;
                    break;
                case MENUPATH_DRIVE:
                    text = AppString.SideBar.Drive;
                    image = AppImage.Drive;
                    break;
                case MENUPATH_ALLOBJECTS:
                    text = AppString.SideBar.AllObjects;
                    image = AppImage.AllObjects;
                    break;
            }
            return new FoldGroupItem(shellExPath, ObjectPath.PathType.Registry) { Text = text, Image = image };
        }

        /*******************************ShellNewList.cs内************************************/

        private void GetShellNewListBackupItems(bool backup)
        {
#if DEBUG
            if (AppConfig.EnableLog)
            {
                using (StreamWriter sw = new StreamWriter(AppConfig.DebugLogPath, true))
                {
                    sw.WriteLine($@"BackupShellNewItems:");
                }
            }
            int i = 0;
#endif
            if (ShellNewLockItem.IsLocked)
            {
#if DEBUG
                if (AppConfig.EnableLog)
                {
                    using (StreamWriter sw = new StreamWriter(AppConfig.DebugLogPath, true))
                    {
                        sw.WriteLine("\tBackupLockItems");
                    }
                }
#endif
                string[] extensions = (string[])Registry.GetValue(ShellNewPath, "Classes", null);
                GetShellNewBackupItems(extensions.ToList(), backup);
            }
            else
            {
#if DEBUG
                if (AppConfig.EnableLog)
                {
                    using (StreamWriter sw = new StreamWriter(AppConfig.DebugLogPath, true))
                    {
                        sw.WriteLine("\tBackupUnlockItems");
                    }
                }
#endif
                List<string> extensions = new List<string> { "Folder" };//文件夹
                using (RegistryKey root = Registry.ClassesRoot)
                {
                    extensions.AddRange(Array.FindAll(root.GetSubKeyNames(), keyName => keyName.StartsWith(".")));
                    if (WinOsVersion.Current < WinOsVersion.Win10) extensions.Add("Briefcase");//公文包(Win10没有)
                    GetShellNewBackupItems(extensions, backup);
                }
            }
        }

        private void GetShellNewBackupItems(List<string> extensions, bool backup)
        {
#if DEBUG
            int i = 0;
#endif
            foreach (string extension in ShellNewItem.UnableSortExtensions)
            {
                if (extensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
                {
                    extensions.Remove(extension);
                    extensions.Insert(0, extension);
                }
            }
            using (RegistryKey root = Registry.ClassesRoot)
            {
                foreach (string extension in extensions)
                {
                    using (RegistryKey extKey = root.OpenSubKey(extension))
                    {
                        string defalutOpenMode = extKey?.GetValue("")?.ToString();
                        if (string.IsNullOrEmpty(defalutOpenMode) || defalutOpenMode.Length > 255) continue;
                        using (RegistryKey openModeKey = root.OpenSubKey(defalutOpenMode))
                        {
                            if (openModeKey == null) continue;
                            string value1 = openModeKey.GetValue("FriendlyTypeName")?.ToString();
                            string value2 = openModeKey.GetValue("")?.ToString();
                            value1 = ResourceString.GetDirectString(value1);
                            if (value1.IsNullOrWhiteSpace() && value2.IsNullOrWhiteSpace()) continue;
                        }
                        using (RegistryKey tKey = extKey.OpenSubKey(defalutOpenMode))
                        {
                            foreach (string part in ShellNewItem.SnParts)
                            {
                                string snPart = part;
                                if (tKey != null) snPart = $@"{defalutOpenMode}\{snPart}";
                                using (RegistryKey snKey = extKey.OpenSubKey(snPart))
                                {
                                    if (ShellNewItem.EffectValueNames.Any(valueName => snKey?.GetValue(valueName) != null))
                                    {
                                        ShellNewItem item = new ShellNewItem(snKey.Name);
                                        string regPath = item.RegPath;
                                        string openMode = item.OpenMode;
                                        string itemName = item.Text;
                                        bool ifItemInMenu = item.ItemVisible;
                                        BackupRestoreItem(item, openMode, BackupItemType.ShellNewItem, ifItemInMenu, currentScene, backup);
#if DEBUG
                                        i++;
                                        if (AppConfig.EnableLog)
                                        {
                                            using (StreamWriter sw = new StreamWriter(AppConfig.DebugLogPath, true))
                                            {
                                                sw.WriteLine("\t\t" + $@"{i}. {openMode} {itemName} {ifItemInMenu} {regPath}");
                                            }
                                        }
#endif
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /*******************************SendToList.cs内************************************/

        private void GetSendToListItems(bool backup)
        {
#if DEBUG
            if (AppConfig.EnableLog)
            {
                using (StreamWriter sw = new StreamWriter(AppConfig.DebugLogPath, true))
                {
                    sw.WriteLine("BackupSendToItems");
                    sw.WriteLine("\tGetSendToItems");
                }
            }
            int i = 0;
#endif
            string filePath, itemFileName, itemName;
            bool ifItemInMenu;
            foreach (string path in Directory.GetFileSystemEntries(SendToList.SendToPath))
            {
                if (Path.GetFileName(path).ToLower() == "desktop.ini") continue;
                SendToItem sendToItem = new SendToItem(path);
                filePath = sendToItem.FilePath;
                itemFileName = sendToItem.ItemFileName;
                itemName = sendToItem.Text;
                ifItemInMenu = sendToItem.ItemVisible;
                BackupRestoreItem(sendToItem, itemFileName, BackupItemType.SendToItem, ifItemInMenu, currentScene, backup);
#if DEBUG
                i = 0;
                i++;
                if (AppConfig.EnableLog)
                {
                    using (StreamWriter sw = new StreamWriter(AppConfig.DebugLogPath, true))
                    {
                        sw.WriteLine("\t\t" + $@"{i}. {itemFileName} {itemName} {ifItemInMenu} {filePath}");
                    }
                }
#endif
            }
            VisibleRegRuleItem item = new VisibleRegRuleItem(VisibleRegRuleItem.SendToDrive);
            string regPath = item.RegPath;
            string valueName = item.ValueName;
            itemName = item.Text;
            ifItemInMenu = item.ItemVisible;
            BackupRestoreItem(item, valueName, BackupItemType.VisibleRegRuleItem, ifItemInMenu, currentScene, backup);
#if DEBUG
            i = 0;
            i++;
            if (AppConfig.EnableLog)
            {
                using (StreamWriter sw = new StreamWriter(AppConfig.DebugLogPath, true))
                {
                    sw.WriteLine("\tBackupAddedItems");
                    sw.WriteLine("\t\t" + $@"{i}. {valueName} {itemName} {ifItemInMenu} {regPath}");
                }
            }
#endif
            item = new VisibleRegRuleItem(VisibleRegRuleItem.DeferBuildSendTo);
            regPath = item.RegPath;
            valueName = item.ValueName;
            itemName = item.Text;
            ifItemInMenu = item.ItemVisible;
            BackupRestoreItem(item, valueName, BackupItemType.VisibleRegRuleItem, ifItemInMenu, currentScene, backup);
#if DEBUG
            i++;
            if (AppConfig.EnableLog)
            {
                using (StreamWriter sw = new StreamWriter(AppConfig.DebugLogPath, true))
                {
                    sw.WriteLine("\t\t" + $@"{i}. {valueName} {itemName} {ifItemInMenu} {regPath}");
                }
            }
#endif
        }

        /*******************************OpenWithList.cs内************************************/

        private void GetOpenWithListItems(bool backup)
        {
#if DEBUG
            if (AppConfig.EnableLog)
            {
                using (StreamWriter sw = new StreamWriter(AppConfig.DebugLogPath, true))
                {
                    sw.WriteLine("BackupOpenWithItems");
                    sw.WriteLine("\tGetOpenWithItems");
                }
            }
            int i = 0;
#endif
            using (RegistryKey root = Registry.ClassesRoot)
            using (RegistryKey appKey = root.OpenSubKey("Applications"))
            {
                foreach (string appName in appKey.GetSubKeyNames())
                {
                    if (!appName.Contains('.')) continue;
                    using (RegistryKey shellKey = appKey.OpenSubKey($@"{appName}\shell"))
                    {
                        if (shellKey == null) continue;

                        List<string> names = shellKey.GetSubKeyNames().ToList();
                        if (names.Contains("open", StringComparer.OrdinalIgnoreCase)) names.Insert(0, "open");

                        string keyName = names.Find(name =>
                        {
                            using (RegistryKey cmdKey = shellKey.OpenSubKey(name))
                                return cmdKey.GetValue("NeverDefault") == null;
                        });
                        if (keyName == null) continue;

                        using (RegistryKey commandKey = shellKey.OpenSubKey($@"{keyName}\command"))
                        {
                            string command = commandKey?.GetValue("")?.ToString();
                            if (ObjectPath.ExtractFilePath(command) != null)
                            {
                                OpenWithItem item = new OpenWithItem(commandKey.Name);
                                string regPath = item.RegPath;
                                string itemFileName = item.ItemFileName;
                                string itemName = item.Text;
                                bool ifItemInMenu = item.ItemVisible;
                                BackupRestoreItem(item, itemFileName, BackupItemType.OpenWithItem, ifItemInMenu, currentScene, backup);
#if DEBUG
                                i++;
                                if (AppConfig.EnableLog)
                                {
                                    using (StreamWriter sw = new StreamWriter(AppConfig.DebugLogPath, true))
                                    {
                                        sw.WriteLine("\tBackupAddedItems");
                                        sw.WriteLine("\t\t" + $@"{i}. {itemFileName} {itemName} {ifItemInMenu} {regPath}");
                                    }
                                }
#endif
                            }
                        }
                    }
                }
            }
            //Win8及以上版本系统才有在应用商店中查找应用
            if (WinOsVersion.Current >= WinOsVersion.Win8)
            {
                VisibleRegRuleItem storeItem = new VisibleRegRuleItem(VisibleRegRuleItem.UseStoreOpenWith);
                string regPath = storeItem.RegPath;
                string valueName = storeItem.ValueName;
                string itemName = storeItem.Text;
                bool ifItemInMenu = storeItem.ItemVisible;
                BackupRestoreItem(storeItem, valueName, BackupItemType.VisibleRegRuleItem, ifItemInMenu, currentScene, backup);
#if DEBUG
                i = 1;
                if (AppConfig.EnableLog)
                {
                    using (StreamWriter sw = new StreamWriter(AppConfig.DebugLogPath, true))
                    {
                        sw.WriteLine("\tBackupAddedItems");
                        sw.WriteLine("\t\t" + $@"{i}. {valueName} {itemName} {ifItemInMenu} {regPath}");
                    }
                }
#endif
            }
        }
    }

    public sealed class BackupList
    {
        // 备份列表缓存区
        private static List<BackupItem> backupList = new List<BackupItem>();

        // 恢复列表暂存区
        public static List<BackupItem> tempRestoreList = new List<BackupItem>();

        // 创建一个XmlSerializer对象
        private static readonly XmlSerializer serializer = new XmlSerializer(typeof(List<BackupItem>));

        public enum BackupItemType
        {
            ShellItem, ShellExItem, UwpModelItem, VisibleRegRuleItem, ShellNewItem, SendToItem,
            OpenWithItem
        }

        public enum BackupTarget
        {
            Basic, AllHomePage
        };

        public enum RestoreMode
        {
            NotHandleNotOnList,     // 启用备份列表上可见的菜单项，禁用备份列表上不可见的菜单项，不处理不存在于备份列表上的菜单项
            DisableNotOnList,       // 启用备份列表上可见的菜单项，禁用备份列表上不可见以及不存在于备份列表上的菜单项
        };

        static BackupList() { }

        public static void AddItem(string keyName, BackupItemType backupItemType, bool itemVisible, Scenes scene)
        {
            backupList.Add(new BackupItem
            {
                KeyName = keyName,
                ItemType = backupItemType,
                ItemVisible = itemVisible,
                BackupScene = scene,
            });
        }

        public static int GetBackupListCount()
        {
            return backupList.Count;
        }

        public static void ClearBackupList()
        {
            backupList.Clear();
        }

        public static void SaveBackupList(string filePath)
        {
            // 序列化到XML文档
            using (StreamWriter sw = new StreamWriter(filePath))
            {
                serializer.Serialize(sw, backupList);
            }
        }

        public static void LoadBackupList(string filePath)
        {
            // 反序列化到List<BackupItem>对象
            using (StreamReader sr = new StreamReader(filePath))
            {
                backupList = serializer.Deserialize(sr) as List<BackupItem>;
            }
        }

        public static void LoadTempRestoreList(Scenes scene)
        {
            tempRestoreList.Clear();
            // 根据backupScene加载列表
            foreach (BackupItem item in backupList)
            {
                if (item.BackupScene == scene)
                {
                    tempRestoreList.Add(item);
                }
            }
        }
    }

    // 定义一个类来表示BackupItem
    [Serializable, XmlType("BackupItem")]
    public sealed class BackupItem
    {
        [XmlElement("KeyName")]
        public string KeyName { get; set; }// 查询索引名字

        [XmlElement("BackupItemType")]
        public BackupItemType ItemType { get; set; }// 备份项目类型

        [XmlElement("ItemVisible")]
        public bool ItemVisible { get; set; }// 是否位于右键菜单中

        [XmlElement("Scene")]
        public Scenes BackupScene { get; set; }// 右键菜单位置
    }
}
