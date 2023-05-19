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

        // 创建一个XmlSerializer对象
        private static readonly XmlSerializer serializer = new XmlSerializer(typeof(List<BackupItem>));

        public enum BackupItemType
        {
            ShellItem, ShellExItem, UwpModelItem, VisibleRegRuleItem
        }

        public enum BackupMode
        {
            Basic
        };

        static BackupList() { }

        public static void AddItem(string itemName, BackupItemType backupItemType, bool itemVisible, Scenes scene)
        {
            backupList.Add(new BackupItem
            {
                ItemName = itemName,
                ItemType = backupItemType,
                ItemVisible = itemVisible,
                BackupScene = scene,
            });
        }

        public static void ClearItems()
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

        public static void ReadBackupList(string filePath)
        {
            // 反序列化到List<BackupItem>对象
            using (StreamReader sr = new StreamReader(filePath))
            {
                backupList = serializer.Deserialize(sr) as List<BackupItem>;
            }
        }

        public static void RestoreBackupList()
        {

        }
    }

    // 定义一个类来表示BackupItem
    [Serializable, XmlType("BackupItem")]
    public class BackupItem
    {
        [XmlElement("ItemName")]
        public string ItemName { get; set; }// 右键菜单名字

        [XmlElement("BackupItemType")]
        public BackupItemType ItemType { get; set; }// 备份项目类型

        [XmlElement("ItemVisible")]
        public bool ItemVisible { get; set; }// 是否位于右键菜单中

        [XmlElement("Scene")]
        public Scenes BackupScene { get; set; }// 右键菜单位置
    }

}
