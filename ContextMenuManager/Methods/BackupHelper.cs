using BluePointLilac.Methods;
using ContextMenuManager.Controls;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static ContextMenuManager.Controls.ShellList;
using System.Xml;
using System.Drawing;
using static ContextMenuManager.Methods.BackupList;
using System.Xml.Serialization;
using static ContextMenuManager.Controls.ShellNewList;
using BluePointLilac.Controls;

namespace ContextMenuManager.Methods
{
    /*******************************外部枚举变量************************************/

    // 右键菜单场景（新增备份类别处1）
    public enum Scenes
    {
        // 主页——第一板块
        File, Folder, Directory, Background, Desktop, Drive, AllObjects, Computer, RecycleBin, Library,
        // 主页——第二板块
        New, SendTo, OpenWith,
        // 主页——第三板块
        WinX,
        // 文件类型——第一板块
        LnkFile, UwpLnk, ExeFile,
        // 文件类型——第二板块
        /* 无 */
        // 文件类型——第三板块
        UnknownType,
        // 文件类型——第四板块
        /* 无 */
        // 其他规则
        /* 无 */
        // 不予备份的项目
        MenuAnalysis,
        CustomExtension, PerceivedType, DirectoryType,
        CommandStore, DragDrop, CustomRegPath, CustomExtensionPerceivedType,
    };

    // 备份项目类型（新增备份类别处3）
    public enum BackupItemType
    {
        ShellItem, ShellExItem, UwpModelItem, VisibleRegRuleItem, ShellNewItem, SendToItem,
        OpenWithItem, WinXItem
    }

    // 备份选项
    public enum BackupMode
    {
        All,    // 备份全部菜单项目
        OnlyVisible, // 仅备份启用的菜单项目
        OnlyInvisible   // 仅备份禁用的菜单项目
    };

    // 恢复模式
    public enum RestoreMode
    {
        NotHandleNotOnList,     // 启用备份列表上可见的菜单项，禁用备份列表上不可见的菜单项，不处理不位于备份列表上的菜单项
        DisableNotOnList,       // 启用备份列表上可见的菜单项，禁用备份列表上不可见以及不位于备份列表上的菜单项
        EnableNotOnList,        // 启用备份列表上可见的菜单项以及不位于备份列表上的菜单项，禁用备份列表上不可见
    };

    sealed class BackupHelper
    {
        /*******************************外部变量、函数************************************/

        // 目前备份版本号
        public const int BackupVersion = 1;

        // 右键菜单备份场景，包含全部场景（确保顺序与右键菜单场景Scenes相同）（新增备份类别处2）
        public static string[] BackupScenesText = new string[] {
            // 主页——第一板块
            AppString.SideBar.File, AppString.SideBar.Folder, AppString.SideBar.Directory, AppString.SideBar.Background,
            AppString.SideBar.Desktop, AppString.SideBar.Drive, AppString.SideBar.AllObjects, AppString.SideBar.Computer,
            AppString.SideBar.RecycleBin, AppString.SideBar.Library,
            // 主页——第二板块
            AppString.SideBar.New, AppString.SideBar.SendTo, AppString.SideBar.OpenWith,
            // 主页——第三板块
            AppString.SideBar.WinX,
            // 文件类型——第一板块
            AppString.SideBar.LnkFile, AppString.SideBar.UwpLnk, AppString.SideBar.ExeFile,
            // 文件类型——第二板块
            /* 无 */
            // 文件类型——第三板块
            AppString.SideBar.UnknownType,
        };

        // 右键菜单恢复场景，包含元数据中的场景
        public static string[] RestoreScenesText;

        public int backupCount = 0; // 备份项目总数量
        public int changeCount = 0; // 备份恢复改变项目数量
        public string createTime;   // 本次备份文件创建时间
        public string filePath; // 本次备份文件目录

        // 获取目前备份恢复场景文字
        public void GetBackupRestoreScenesText(List<Scenes> scenes)
        {
            List<string> scenesTextList = new List<string>();
            foreach(Scenes scene in scenes)
            {
                scenesTextList.Add(BackupScenesText[(int)scene]);
            }
            RestoreScenesText = scenesTextList.ToArray();
        }

        // 备份指定场景内容
        public void BackupItems(List<string> sceneTexts, BackupMode backupMode)
        {
            ClearBackupList();
            GetBackupRestoreScenes(sceneTexts);
            this.backupMode = backupMode;
            DateTime dateTime = DateTime.Now;
            string date = DateTime.Today.ToString("yyyy-MM-dd");
            string time = dateTime.ToString("HH-mm-ss");
            createTime = $@"{date} {time}";
            filePath = $@"{AppConfig.MenuBackupDir}\{createTime}.xml";
            // 构建备份元数据
            metaData.CreateTime = dateTime;
            metaData.Device = AppConfig.ComputerHostName;
            metaData.BackupScenes = currentScenes;
            metaData.Version = BackupVersion;
            // 加载备份文件到缓冲区
            BackupRestoreItems(true);
            // 保存缓冲区的备份文件
            SaveBackupList(filePath);
            backupCount = GetBackupListCount();
            ClearBackupList();
        }

        // 恢复指定场景内容
        public void RestoreItems(string filePath, List<string> sceneTexts, RestoreMode restoreMode)
        {
            ClearBackupList();
            GetBackupRestoreScenes(sceneTexts);
            this.restoreMode = restoreMode;
            changeCount = 0;
            // 加载备份文件到缓冲区
            LoadBackupList(filePath);
            // 还原缓冲区的备份文件
            BackupRestoreItems(false);
            ClearBackupList();
        }

        /*******************************内部变量、函数************************************/

        private Scenes currentScene;    // 目前处理场景
        private BackupMode backupMode;  // 目前备份模式
        private RestoreMode restoreMode;    // 目前恢复模式
        private readonly List<Scenes> currentScenes = new List<Scenes>();   // 目前备份恢复场景
        
        // 获取目前备份恢复场景
        private void GetBackupRestoreScenes(List<string> sceneTexts)
        {
            currentScenes.Clear();
            for (int i = 0; i < BackupScenesText.Length; i++)
            {
                string text = BackupScenesText[i];
                if (sceneTexts.Contains(text))
                {
                    // 顺序一一对应，直接转换
                    currentScenes.Add((Scenes)i);
                }
            }
        }

        // 按照目前处理场景逐个备份或恢复
        private void BackupRestoreItems(bool backup)
        {
            foreach(Scenes scene in currentScenes)
            {
                currentScene = scene;
                // 加载某个Scene的恢复列表
                if (!backup)
                {
                    LoadTempRestoreList(currentScene);
                }
                GetBackupItems(backup);
            }
        }

        // 开始进行备份或恢复
        private void GetBackupItems(bool backup)
        {
            switch (currentScene)
            {
                case Scenes.New:
                    // 新建
                    GetShellNewListBackupItems(backup); break;
                case Scenes.SendTo:
                    // 发送到
                    GetSendToListItems(backup); break;
                case Scenes.OpenWith:
                    // 打开方式
                    GetOpenWithListItems(backup); break;
                case Scenes.WinX:
                    // Win+X
                    GetWinXListItems(backup); break;
                default:
                    // 位于ShellList.cs内的备份项目
                    GetShellListItems(backup); break;
            }
        }

        /*******************************单个Item处理************************************/

        private void BackupRestoreItem(MyListItem item, string keyName, BackupItemType backupItemType, bool ifItemInMenu, Scenes currentScene, bool backup)
        {
            if (backup)
            {
                // 加入备份列表
                switch (backupMode)
                {
                    case BackupMode.All:
                    default:
                        AddItem(keyName, backupItemType, ifItemInMenu, currentScene);
                        break;
                    case BackupMode.OnlyVisible:
                        if (ifItemInMenu) AddItem(keyName, backupItemType, ifItemInMenu, currentScene);
                        break;
                    case BackupMode.OnlyInvisible:
                        if (!ifItemInMenu) AddItem(keyName, backupItemType, ifItemInMenu, currentScene);
                        break;
                }
            }
            else
            {
                // 恢复备份列表（新增备份类别处4）
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
                        case BackupItemType.WinXItem:
                            ((WinXItem)item).ItemVisible = !ifItemInMenu; break;
                    }
                }
            }
            // 释放资源
            item.Dispose();
        }

        private bool CheckItemNeedChange(string keyName, BackupItemType itemType, bool itemVisible)
        {
            foreach (BackupItem item in sceneRestoreList)
            {
                // 成功匹配到后的处理方式
                if (item.KeyName == keyName && item.ItemType == itemType)
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
            if ((restoreMode == RestoreMode.DisableNotOnList && itemVisible) || 
                (restoreMode == RestoreMode.EnableNotOnList && !itemVisible))
            {
                changeCount++;
                return true;
            }
            return false;
        }

        /*******************************ShellList.cs************************************/

        // （新增备份类别处5）
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
                case Scenes.LnkFile:
                    scenePath = GetOpenModePath(".lnk"); break;
                case Scenes.UwpLnk:
                    //Win8之前没有Uwp
                    if (WinOsVersion.Current < WinOsVersion.Win8) return;
                    scenePath = MENUPATH_UWPLNK; break;
                case Scenes.ExeFile:
                    scenePath = GetSysAssExtPath(".exe"); break;
                case Scenes.UnknownType:
                    scenePath = MENUPATH_UNKNOWN; break;
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
                case Scenes.ExeFile:
                    GetBackupItems(GetOpenModePath(".exe"), backup);
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

        /*******************************ShellNewList.cs************************************/

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

        /*******************************SendToList.cs************************************/

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

        /*******************************OpenWithList.cs************************************/

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

        /*******************************WinXList.cs************************************/

        private void GetWinXListItems(bool backup)
        {
#if DEBUG
            if (AppConfig.EnableLog)
            {
                using (StreamWriter sw = new StreamWriter(AppConfig.DebugLogPath, true))
                {
                    sw.WriteLine("BackupWinXItems");
                    sw.WriteLine("\tGetWinXItems");
                }
            }
            int i = 0;
#endif
            if (WinOsVersion.Current >= WinOsVersion.Win8)
            {
                string[] dirPaths = Directory.GetDirectories(WinXList.WinXPath);
                Array.Reverse(dirPaths);
                bool sorted = false;
                foreach (string dirPath in dirPaths)
                {
                    WinXGroupItem groupItem = new WinXGroupItem(dirPath);
                    string[] lnkPaths;
                    if (AppConfig.WinXSortable)
                    {
                        lnkPaths = WinXList.GetSortedPaths(dirPath, out bool flag);
                        if (flag) sorted = true;
                    }
                    else
                    {
                        lnkPaths = Directory.GetFiles(dirPath, "*.lnk");
                        Array.Reverse(lnkPaths);
                    }
                    foreach (string path in lnkPaths)
                    {
                        WinXItem item = new WinXItem(path, groupItem);
                        string filePath = item.FilePath;
                        string fileName = item.FileName;
                        string itemName = item.Text;
                        bool ifItemInMenu = item.ItemVisible;
                        BackupRestoreItem(item, fileName, BackupItemType.WinXItem, ifItemInMenu, currentScene, backup);
#if DEBUG
                        i++;
                        if (AppConfig.EnableLog)
                        {
                            using (StreamWriter sw = new StreamWriter(AppConfig.DebugLogPath, true))
                            {
                                sw.WriteLine("\t\t" + $@"{i}. {fileName} {itemName} {ifItemInMenu} {filePath}");
                            }
                        }
#endif
                    }
                    groupItem.Dispose();
                }
                if (sorted)
                {
                    ExplorerRestarter.Show();
                    AppMessageBox.Show(AppString.Message.WinXSorted);
                }
            }
        }
    }

    public sealed class BackupList
    {
        // 元数据缓存区
        public static MetaData metaData = new MetaData();  

        // 备份列表/恢复列表缓存区
        private static List<BackupItem> backupRestoreList = new List<BackupItem>();

        // 单场景恢复列表暂存区
        public static List<BackupItem> sceneRestoreList = new List<BackupItem>();

        // 创建一个XmlSerializer实例并设置根节点
        private static readonly XmlSerializer backupDataSerializer = new XmlSerializer(typeof(BackupData), 
            new XmlRootAttribute("ContextMenuManager"));
        // 自定义命名空间
        private static readonly XmlSerializerNamespaces namespaces = new XmlSerializerNamespaces();

        // 创建一个XmlSerializer实例并设置根节点
        private static readonly XmlSerializer metaDataSerializer = new XmlSerializer(typeof(MetaData),
            new XmlRootAttribute("MetaData"));

        static BackupList()
        {
            // 禁用默认命名空间
            namespaces.Add(string.Empty, string.Empty);
        }

        public static void AddItem(string keyName, BackupItemType backupItemType, bool itemVisible, Scenes scene)
        {
            backupRestoreList.Add(new BackupItem
            {
                KeyName = keyName,
                ItemType = backupItemType,
                ItemVisible = itemVisible,
                BackupScene = scene,
            });
        }

        public static int GetBackupListCount()
        {
            return backupRestoreList.Count;
        }

        public static void ClearBackupList()
        {
            backupRestoreList.Clear();
        }

        public static void SaveBackupList(string filePath)
        {
            // 创建一个父对象，并将BackupList和MetaData对象包装到其中
            BackupData myData = new BackupData()
            {
                MetaData = metaData,
                BackupList = backupRestoreList,
            };

            // 序列化root对象并保存到XML文档
            using (FileStream stream = new FileStream(filePath, FileMode.Create))
            {
                backupDataSerializer.Serialize(stream, myData, namespaces);
            }
        }

        public static void LoadBackupList(string filePath)
        {
            // 反序列化XML文件并获取根对象
            BackupData myData;
            using (FileStream stream = new FileStream(filePath, FileMode.Open))
            {
                myData = (BackupData)backupDataSerializer.Deserialize(stream);
            }

            // 获取MetaData对象
            metaData = myData.MetaData;

            // 清理backupRestoreList变量
            backupRestoreList.Clear();
            backupRestoreList = null;

            // 获取BackupList对象
            backupRestoreList = myData.BackupList;
        }

        public static void LoadTempRestoreList(Scenes scene)
        {
            sceneRestoreList.Clear();
            // 根据backupScene加载列表
            foreach (BackupItem item in backupRestoreList)
            {
                if (item.BackupScene == scene)
                {
                    sceneRestoreList.Add(item);
                }
            }
        }

        public static void LoadBackupDataMetaData(string filePath)
        {
            // 反序列化root对象并保存到XML文档
            using (FileStream stream = new FileStream(filePath, FileMode.Open))
            {
                // 读取 <MetaData> 节点
                using (XmlReader reader = XmlReader.Create(stream))
                {
                    // 寻找第一个<MetaData>节点
                    reader.ReadToFollowing("MetaData");

                    // 清理metaData变量
                    metaData = null;

                    // 反序列化<MetaData>节点为MetaData对象
                    metaData = (MetaData)metaDataSerializer.Deserialize(reader);
                }
            }
        }
    }

    // 定义一个类来表示备份数据
    [Serializable, XmlType("BackupData")]
    public sealed class BackupData
    {
        [XmlElement("MetaData")]
        public MetaData MetaData { get; set; }

        [XmlElement("BackupList")]
        public List<BackupItem> BackupList { get; set; }
    }

    // 定义一个类来表示备份项目
    [Serializable, XmlType("BackupItem")]
    public sealed class BackupItem
    {
        [XmlElement("KeyName")]
        public string KeyName { get; set; } // 查询索引名字

        [XmlElement("ItemType")]
        public BackupItemType ItemType { get; set; } // 备份项目类型

        [XmlElement("ItemVisible")]
        public bool ItemVisible { get; set; } // 是否位于右键菜单中

        [XmlElement("Scene")]
        public Scenes BackupScene { get; set; } // 右键菜单位置
    }

    // 定义一个类来表示备份项目的元数据
    [Serializable, XmlType("MetaData")]
    public sealed class MetaData
    {
        [XmlElement("Version")]
        public int Version { get; set; } // 备份版本

        [XmlElement("BackupScenes")]
        public List<Scenes> BackupScenes { get; set; } // 备份场景

        [XmlElement("CreateTime")]
        public DateTime CreateTime { get; set; } // 备份时间

        [XmlElement("Device")]
        public string Device { get; set; } // 备份设备
    }
}
