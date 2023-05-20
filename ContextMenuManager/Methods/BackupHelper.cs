using BluePointLilac.Methods;
using ContextMenuManager.Controls;
using ContextMenuManager.Models;
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
using static ContextMenuManager.Models.BackupList;

namespace ContextMenuManager.Methods
{
    internal class BackupHelper
    {
        private Scenes currentScene;
        private List<BackupItem> currentRestoreItems = new List<BackupItem>();

        public void BackupItems(BackupMode mode)
        {
            string date = DateTime.Today.ToString("yyyy-MM-dd");
            string time = DateTime.Now.ToString("HH-mm-ss");
            string filePath = $@"{AppConfig.MenuBackupDir}\{date} {time}.xml";
            BackupRestoreItems(mode, true);
            if (!Directory.Exists($@"{AppConfig.MenuBackupDir}"))
            {
                Directory.CreateDirectory($@"{AppConfig.MenuBackupDir}");
            }
            SaveBackupList(filePath);
            ClearItems();
        }

        public void RestoreItems(BackupMode mode)
        {
            BackupRestoreItems(mode, false);
            ClearItems();
        }

        private void BackupRestoreItems(BackupMode mode, bool backup)
        {
            Scenes[] scenes = null;
            switch (mode)
            {
                case BackupMode.Basic:
                    scenes = new Scenes[] {
                        Scenes.File, Scenes.Folder, Scenes.Directory, Scenes.Background, Scenes.Desktop,
                        Scenes.Drive, Scenes.AllObjects, Scenes.Computer, Scenes.RecycleBin, Scenes.Library
                    }; break;
            }
            for (int i = 0; i < scenes.Length; i++)
            {
                currentScene = scenes[i];
                // 通过Scene筛选目前恢复项目
                if (!backup)
                {
                    currentRestoreItems.Clear();
                    foreach(BackupItem item in backupList)
                    {
                        if (item.BackupScene == currentScene)
                        {
                            currentRestoreItems.Add(item);
                        }
                    }
                }
                GetBackupItems(backup);
            }
        }

        private bool IfItemNeedChange(string keyName, BackupItemType itemType, bool itemVisible)
        {
            foreach(BackupItem item in currentRestoreItems)
            {
                if(item.KeyName == keyName && item.ItemType == itemType)
                {
                    if (item.ItemVisible != itemVisible)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private void GetBackupItems(bool backup)
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
            using (StreamWriter sw = new StreamWriter("D:\\log.txt", true))
            {
                sw.WriteLine("BackupItems: " + currentScene);
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
                    if (backup)
                    {
                        // 加入备份列表
                        AddItem(valueName, BackupItemType.VisibleRegRuleItem, ifItemInMenu, currentScene);
                    }
                    else
                    {
                        // 恢复备份列表
                        if (IfItemNeedChange(valueName, BackupItemType.VisibleRegRuleItem, ifItemInMenu))
                        {
                            item.ItemVisible = !ifItemInMenu;
                        }
                    }
#if DEBUG
                    i++;
                    using (StreamWriter sw = new StreamWriter("D:\\log.txt", true))
                    {
                        sw.WriteLine("\tBackupAddedItems");
                        sw.WriteLine("\t\t" + i.ToString() + ". " + valueName + " " + itemName + " " + ifItemInMenu + " " + regPath);
                    }
#endif
                    break;
                case Scenes.Computer:
                    item = new VisibleRegRuleItem(VisibleRegRuleItem.NetworkDrive);
                    regPath = item.RegPath;
                    valueName = item.ValueName;
                    itemName = item.Text;
                    ifItemInMenu = item.ItemVisible;
                    if (backup)
                    {
                        // 加入备份列表
                        AddItem(valueName, BackupItemType.VisibleRegRuleItem, ifItemInMenu, currentScene);
                    }
                    else
                    {
                        // 恢复备份列表
                        if (IfItemNeedChange(valueName, BackupItemType.VisibleRegRuleItem, ifItemInMenu))
                        {
                            item.ItemVisible = !ifItemInMenu;
                        }
                    }
                    
#if DEBUG
                    i++;
                    using (StreamWriter sw = new StreamWriter("D:\\log.txt", true))
                    {
                        sw.WriteLine("\tBackupAddedItems");
                        sw.WriteLine("\t\t" + i.ToString() + ". " + valueName + " " + itemName + " " + ifItemInMenu + " " + regPath);
                    }
#endif
                    break;
                case Scenes.RecycleBin:
                    item = new VisibleRegRuleItem(VisibleRegRuleItem.RecycleBinProperties);
                    regPath = item.RegPath;
                    valueName = item.ValueName;
                    itemName = item.Text;
                    ifItemInMenu = item.ItemVisible;
                    if (backup)
                    {
                        // 加入备份列表
                        AddItem(valueName, BackupItemType.VisibleRegRuleItem, ifItemInMenu, currentScene);
                    }
                    else
                    {
                        // 恢复备份列表
                        if (IfItemNeedChange(valueName, BackupItemType.VisibleRegRuleItem, ifItemInMenu))
                        {
                            item.ItemVisible = !ifItemInMenu;
                        }
                    }
#if DEBUG
                    i++;
                    using (StreamWriter sw = new StreamWriter("D:\\log.txt", true))
                    {
                        sw.WriteLine("\tBackupAddedItems");
                        sw.WriteLine("\t\t" + i.ToString() + ". " + valueName + " " + itemName + " " + ifItemInMenu + " " + regPath);
                    }
#endif
                    break;
                case Scenes.Library:
#if DEBUG
                    using (StreamWriter sw = new StreamWriter("D:\\log.txt", true))
                    {
                        sw.WriteLine("\tBackupAddedItems");
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
            using (StreamWriter sw = new StreamWriter("D:\\log.txt", true))
            {
                sw.WriteLine("\tGetBackupShellItems");
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
                    if (backup)
                    {
                        // 加入备份列表
                        AddItem(keyName, BackupItemType.ShellItem, ifItemInMenu, currentScene);
                    }
                    else
                    {
                        // 恢复备份列表
                        if (IfItemNeedChange(keyName, BackupItemType.ShellItem, ifItemInMenu))
                        {
                            item.ItemVisible = !ifItemInMenu;
                        }
                    }
#if DEBUG
                    i++;
                    using (StreamWriter sw = new StreamWriter("D:\\log.txt", true))
                    {
                        sw.WriteLine("\t\t" + i.ToString() + ". " + keyName + " " + itemName + " " + ifItemInMenu + " " + regPath);
                    }
#endif
                }
            }
        }

        private void GetBackupShellExItems(string shellExPath, bool backup)
        {
#if DEBUG
            using (StreamWriter sw = new StreamWriter("D:\\log.txt", true))
            {
                sw.WriteLine("\tGetBackupShellExItems");
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
                    // here!
                    groupItem = GetDragDropGroupItem(shellExPath);
#if DEBUG
                    using (StreamWriter sw = new StreamWriter("D:\\log.txt", true))
                    {
                        sw.WriteLine("\t\t" + shellExPath + "(FoldGroupItem)");
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
                        if (backup)
                        {
                            // 加入备份列表
                            AddItem(keyName, BackupItemType.ShellExItem, ifItemInMenu, currentScene);
                        }
                        else
                        {
                            // 恢复备份列表
                            if (IfItemNeedChange(keyName, BackupItemType.ShellExItem, ifItemInMenu))
                            {
                                item.ItemVisible = !ifItemInMenu;
                            }
                        }
                        
#if DEBUG
                        i++;
                        using (StreamWriter sw = new StreamWriter("D:\\log.txt", true))
                        {
                            sw.WriteLine("\t\t" + i.ToString() + ". " + keyName + " " + itemName + " " + ifItemInMenu + " " + regPath);
                        }
#endif
                        names.Add(keyName);
                    }
                }
            }
        }

        private void GetBackupUwpModeItem(bool backup)
        {
#if DEBUG
            using (StreamWriter sw = new StreamWriter("D:\\log.txt", true))
            {
                sw.WriteLine("\tGetBackupUwpModeItem");
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
                                string itemName = uwpItem.Text; // 右键菜单名称
                                bool ifItemInMenu = uwpItem.ItemVisible;
                                if (backup)
                                {
                                    // 加入备份列表
                                    AddItem(itemName, BackupItemType.UwpModelItem, ifItemInMenu, currentScene);
                                }
                                else
                                {
                                    // 恢复备份列表
                                    if (IfItemNeedChange(itemName, BackupItemType.UwpModelItem, ifItemInMenu))
                                    {
                                        uwpItem.ItemVisible = !ifItemInMenu;
                                    }
                                } 
#if DEBUG
                                i++;
                                using (StreamWriter sw = new StreamWriter("D:\\log.txt", true))
                                {
                                    sw.WriteLine("\t\t" + i.ToString() + ". " + uwpName + " " + itemName + " " + ifItemInMenu + " " + guid);
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
    }
}
