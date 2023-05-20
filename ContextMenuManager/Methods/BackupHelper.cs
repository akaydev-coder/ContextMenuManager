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
using System.Windows.Forms;
using System.Drawing;

namespace ContextMenuManager.Methods
{
    internal class BackupHelper
    {
        private Scenes backupScene;

        public void BackupItems(BackupList.BackupMode mode)
        {
            string date = DateTime.Today.ToString("yyyy-MM-dd");
            string time = DateTime.Now.ToString("HH-mm-ss");
            string filePath = $@"{AppConfig.MenuBackupDir}\{date} {time}.xml";
            Scenes[] BackupScenes = null;
            switch (mode)
            {
                case BackupList.BackupMode.Basic:
                    BackupScenes = new Scenes[] {
                        Scenes.File, Scenes.Folder, Scenes.Directory, Scenes.Background, Scenes.Desktop,
                        Scenes.Drive, Scenes.AllObjects, Scenes.Computer, Scenes.RecycleBin, Scenes.Library
                    }; break;
            }
            for (int i = 0; i < BackupScenes.Length; i++)
            {
                backupScene = BackupScenes[i];
                GetBackupItems();
            }
            if (!Directory.Exists($@"{AppConfig.MenuBackupDir}"))
            {
                Directory.CreateDirectory($@"{AppConfig.MenuBackupDir}");
            }
            BackupList.SaveBackupList(filePath);
            BackupList.ClearItems();
        }

        public void RestoreItems(BackupList.BackupMode mode)
        {
            /*Scenes[] BackupScenes = null;
            switch (mode)
            {
                case BackupList.BackupMode.Basic:
                    BackupScenes = new Scenes[] {
                        Scenes.File, Scenes.Folder, Scenes.Directory, Scenes.Background, Scenes.Desktop,
                        Scenes.Drive, Scenes.AllObjects, Scenes.Computer, Scenes.RecycleBin, Scenes.Library
                    }; break;
            }
            for (int i = 0; i < BackupScenes.Length; i++)
            {
                backupScene = BackupScenes[i];
                GetBackupItems();
            }
            if (!Directory.Exists($@"{AppConfig.MenuBackupDir}"))
            {
                Directory.CreateDirectory($@"{AppConfig.MenuBackupDir}");
            }
            BackupList.SaveBackupList(filePath);
            BackupList.ClearItems();*/
        }

        private void GetBackupItems()
        {
            string scenePath = null;
            switch (backupScene)
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
                sw.WriteLine("BackupItems: " + backupScene);
            }
            int i = 0;
#endif
            GetBackupItems(scenePath);
            if (WinOsVersion.Current >= WinOsVersion.Win10)
            {
                GetBackupUwpModeItem();
            }
            switch (backupScene)
            {
                case Scenes.Background:
                    VisibleRegRuleItem item = new VisibleRegRuleItem(VisibleRegRuleItem.CustomFolder);
                    string regPath = item.RegPath;
                    string valueName = item.ValueName;
                    string itemName = item.Text;
                    bool ifItemInMenu = item.ItemVisible;
                    // 加入备份列表
                    BackupList.AddItem(itemName, BackupList.BackupItemType.VisibleRegRuleItem, ifItemInMenu, backupScene);
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
                    // 加入备份列表
                    BackupList.AddItem(itemName, BackupList.BackupItemType.VisibleRegRuleItem, ifItemInMenu, backupScene);
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
                    // 加入备份列表
                    BackupList.AddItem(itemName, BackupList.BackupItemType.VisibleRegRuleItem, ifItemInMenu, backupScene);
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
                        GetBackupShellItems(GetShellPath(scenePath));
                        GetBackupShellExItems(GetShellExPath(scenePath));
                    }
                    break;
            }
        }

        private void GetBackupItems(string scenePath)
        {
            if (scenePath == null) return;
            RegTrustedInstaller.TakeRegKeyOwnerShip(scenePath);
            GetBackupShellItems(GetShellPath(scenePath));
            GetBackupShellExItems(GetShellExPath(scenePath));
        }

        private void GetBackupShellItems(string shellPath)
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
                    // 加入备份列表
                    BackupList.AddItem(itemName, BackupList.BackupItemType.ShellItem, ifItemInMenu, backupScene);
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

        private void GetBackupShellExItems(string shellExPath)
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
                bool isDragDrop = backupScene == Scenes.DragDrop;
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
                        // 加入备份列表
                        BackupList.AddItem(itemName, BackupList.BackupItemType.ShellExItem, ifItemInMenu, backupScene);
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
                groupItem?.SetVisibleWithSubItemCount();
            }
        }

        private void GetBackupUwpModeItem()
        {
#if DEBUG
            using (StreamWriter sw = new StreamWriter("D:\\log.txt", true))
            {
                sw.WriteLine("\tGetBackupUwpModeItem");
            }
            int i = 0;
#endif
            List<Guid> guidList = new List<Guid>() { };
            foreach (XmlDocument doc in XmlDicHelper.UwpModeItemsDic)
            {
                if (doc?.DocumentElement == null) continue;
                foreach (XmlNode sceneXN in doc.DocumentElement.ChildNodes)
                {
                    if (sceneXN.Name == backupScene.ToString())
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
                                // 加入备份列表
                                BackupList.AddItem(itemName, BackupList.BackupItemType.UwpModelItem, ifItemInMenu, backupScene);
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
