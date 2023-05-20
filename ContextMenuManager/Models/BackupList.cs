using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Xml.Serialization;
using static ContextMenuManager.Controls.ShellList;
using static ContextMenuManager.Models.BackupList;

namespace ContextMenuManager.Models
{
    public static class BackupList
    {
        // 备份列表缓存区
        private static List<BackupItem> backupList = new List<BackupItem>();

        // 恢复列表暂存区
        public static List<BackupItem> tempRestoreList = new List<BackupItem>();

        // 创建一个XmlSerializer对象
        private static readonly XmlSerializer serializer = new XmlSerializer(typeof(List<BackupItem>));

        public enum BackupItemType
        {
            ShellItem, ShellExItem, UwpModelItem, VisibleRegRuleItem
        }

        public enum BackupTarget
        {
            Basic
        };

        public enum RestoreMode
        {
            EnableDiableOnList,     // 启用备份列表上可见的菜单项，禁用备份列表上不可见的菜单项，不处理不存在于备份列表上的菜单项
            JustEnableOnList,       // 仅启用备份列表上可见的菜单项，禁用备份列表上不可见以及不存在于备份列表上的菜单项
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
    public class BackupItem
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
