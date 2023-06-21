using BluePointLilac.Controls;
using BluePointLilac.Methods;
using ContextMenuManager.Controls.Interfaces;
using ContextMenuManager.Methods;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ContextMenuManager.Controls
{
    sealed class WinXItem : FoldSubItem, IChkVisibleItem, IBtnShowMenuItem, IBtnMoveUpDownItem, ITsiAdministratorItem,
        ITsiTextItem, ITsiWebSearchItem, ITsiFilePathItem, ITsiDeleteItem, ITsiShortcutCommandItem
    {
        public WinXItem(string filePath, FoldGroupItem group)
        {
            InitializeComponents();
            FoldGroupItem = group;
            FilePath = filePath;
            if (WinOsVersion.Current >= WinOsVersion.Win11)
            {
                keyPath = FilePath.Substring((ItemVisible ? WinXList.WinXPath : WinXList.BackupWinXPath).Length);
            } 
            Indent();
        }

        private string filePath;
        public string FilePath
        {
            get => filePath;
            set
            {
                filePath = value;
                ShellLink = new ShellLink(value);
                Text = ItemText;
                Image = ItemImage;
            }
        }

        private readonly string keyPath = null;
        private string BackupPath => $@"{(ItemVisible ? WinXList.BackupWinXPath : WinXList.WinXPath)}{keyPath}";
        private string DefaultFilePath => $@"{WinXList.DefaultWinXPath}{keyPath}";

        public string ItemText
        {
            get
            {
                string name = ShellLink.Description?.Trim();
                if(name.IsNullOrWhiteSpace()) name = DesktopIni.GetLocalizedFileNames(FilePath, true);
                if(name == string.Empty) name = Path.GetFileNameWithoutExtension(FilePath);
                return name;
            }
            set
            {
                ShellLink.Description = value;
                ShellLink.Save();
                DesktopIni.SetLocalizedFileNames(FilePath, value);
                Text = ResourceString.GetDirectString(value);
                ExplorerRestarter.Show();
            }
        }

        // Win11需要改变两处快捷方式，Win10仅需要隐藏一处快捷方式
        public bool ItemVisible
        {
            get
            {
                return (WinOsVersion.Current >= WinOsVersion.Win11) ? 
                    FilePath.Substring(0, WinXList.WinXPath.Length).Equals(WinXList.WinXPath, StringComparison.OrdinalIgnoreCase) : 
                    (File.GetAttributes(FilePath) & FileAttributes.Hidden) != FileAttributes.Hidden;
            }
            set
            {
                if (WinOsVersion.Current >= WinOsVersion.Win11)
                {
                    // 处理用户WinX菜单目录
                    string name = DesktopIni.GetLocalizedFileNames(FilePath);
                    if (!Directory.Exists(Path.GetDirectoryName(BackupPath)))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(BackupPath));
                    }
                    File.Move(FilePath, BackupPath);
                    // 处理用户WinX菜单目录下的desktop.ini文件（确保移动后名称在本地化下相同）
                    if (value)
                    {
                        DesktopIni.DeleteLocalizedFileNames(FilePath);
                    }
                    else
                    {
                        if (name != string.Empty) DesktopIni.SetLocalizedFileNames(BackupPath, name);
                    }
                    // 处理默认WinX菜单目录
                    if (value)
                    {
                        File.Copy(BackupPath, DefaultFilePath, true);
                    }
                    else
                    {
                        if (File.Exists(DefaultFilePath))
                        {
                            File.Delete(DefaultFilePath);
                        }
                    }
                    // 文件与备份文件目录交换
                    FilePath = BackupPath;
                }
                else
                {
                    FileAttributes attributes = File.GetAttributes(FilePath);
                    if (value)
                    {
                        attributes &= ~FileAttributes.Hidden;
                    }
                    else
                    {
                        attributes |= FileAttributes.Hidden;
                    }
                    File.SetAttributes(FilePath, attributes);
                }
                ExplorerRestarter.Show();
            }
        }

        public Icon ItemIcon
        {
            get
            {
                ShellLink.ICONLOCATION iconLocation = ShellLink.IconLocation;
                string iconPath = iconLocation.IconPath;
                int iconIndex = iconLocation.IconIndex;
                if(string.IsNullOrEmpty(iconPath)) iconPath = FilePath;
                Icon icon = ResourceIcon.GetIcon(iconPath, iconIndex);
                if(icon == null)
                {
                    string path = ItemFilePath;
                    if(File.Exists(path)) icon = ResourceIcon.GetExtensionIcon(path);
                    else if(Directory.Exists(path)) icon = ResourceIcon.GetFolderIcon(path);
                }
                return icon;
            }
        }

        public string ItemFilePath
        {
            get
            {
                string path = ShellLink.TargetPath;
                if(!File.Exists(path) && !Directory.Exists(path)) path = FilePath;
                return path;
            }
        }

        public ShellLink ShellLink { get; private set; }
        public string SearchText => $"{AppString.SideBar.WinX} {Text}";
        public string FileName => Path.GetFileName(FilePath);
        private Image ItemImage => ItemIcon?.ToBitmap() ?? AppImage.NotFound;

        public VisibleCheckBox ChkVisible { get; set; }
        public MenuButton BtnShowMenu { get; set; }
        public ChangeTextMenuItem TsiChangeText { get; set; }
        public WebSearchMenuItem TsiSearch { get; set; }
        public FilePropertiesMenuItem TsiFileProperties { get; set; }
        public FileLocationMenuItem TsiFileLocation { get; set; }
        public ShortcutCommandMenuItem TsiChangeCommand { get; set; }
        public RunAsAdministratorItem TsiAdministrator { get; set; }
        public DeleteMeMenuItem TsiDeleteMe { get; set; }
        public MoveButton BtnMoveUp { get; set; }
        public MoveButton BtnMoveDown { get; set; }

        readonly ToolStripMenuItem TsiDetails = new ToolStripMenuItem(AppString.Menu.Details);
        readonly ToolStripMenuItem TsiChangeGroup = new ToolStripMenuItem(AppString.Menu.ChangeGroup);

        private void InitializeComponents()
        {
            BtnShowMenu = new MenuButton(this);
            ChkVisible = new VisibleCheckBox(this);
            BtnMoveDown = new MoveButton(this, false);
            BtnMoveUp = new MoveButton(this, true);
            TsiChangeText = new ChangeTextMenuItem(this);
            TsiChangeCommand = new ShortcutCommandMenuItem(this);
            TsiAdministrator = new RunAsAdministratorItem(this);
            TsiSearch = new WebSearchMenuItem(this);
            TsiFileLocation = new FileLocationMenuItem(this);
            TsiFileProperties = new FilePropertiesMenuItem(this);
            TsiDeleteMe = new DeleteMeMenuItem(this);

            ContextMenuStrip.Items.AddRange(new ToolStripItem[] { TsiChangeText, new ToolStripSeparator(),
                TsiChangeGroup, new ToolStripSeparator(), TsiAdministrator, new ToolStripSeparator(),
                TsiDetails, new ToolStripSeparator(), TsiDeleteMe });

            TsiDetails.DropDownItems.AddRange(new ToolStripItem[] { TsiSearch,
                new ToolStripSeparator(), TsiChangeCommand, TsiFileProperties, TsiFileLocation });

            TsiChangeGroup.Click += (sender, e) => ChangeGroup();
            BtnMoveDown.MouseDown += (sender, e) => MoveItem(false);
            BtnMoveUp.MouseDown += (sender, e) => MoveItem(true);
            TsiChangeCommand.Click += (sender, e) =>
            {
                if(TsiChangeCommand.ChangeCommand(ShellLink))
                {
                    Image = ItemImage;
                    WinXHasher.HashLnk(FilePath);
                    ExplorerRestarter.Show();
                }
            };
        }

        // TODO:适配Win11
        private void ChangeGroup()
        {
            using(SelectDialog dlg = new SelectDialog())
            {
                dlg.Title = AppString.Dialog.SelectGroup;
                dlg.Items = WinXList.GetGroupNames();
                dlg.Selected = FoldGroupItem.Text;
                if(dlg.ShowDialog() != DialogResult.OK) return;
                if(dlg.Selected == FoldGroupItem.Text) return;
                string dirPath = $@"{WinXList.WinXPath}\{dlg.Selected}";
                int count = Directory.GetFiles(dirPath, "*.lnk").Length;
                string num = (count + 1).ToString().PadLeft(2, '0');
                string partName = FileName;
                int index = partName.IndexOf(" - ");
                if(index > 0) partName = partName.Substring(index + 3);
                string lnkPath = $@"{dirPath}\{num} - {partName}";
                lnkPath = ObjectPath.GetNewPathWithIndex(lnkPath, ObjectPath.PathType.File);
                string text = DesktopIni.GetLocalizedFileNames(FilePath);
                DesktopIni.DeleteLocalizedFileNames(FilePath);
                if(text != string.Empty) DesktopIni.SetLocalizedFileNames(lnkPath, text);
                File.Move(FilePath, lnkPath);
                FilePath = lnkPath;
                WinXList list = (WinXList)Parent;
                list.Controls.Remove(this);
                for(int i = 0; i < list.Controls.Count; i++)
                {
                    if(list.Controls[i] is WinXGroupItem groupItem && groupItem.Text == dlg.Selected)
                    {
                        list.Controls.Add(this);
                        list.SetItemIndex(this, i + 1);
                        Visible = !groupItem.IsFold;
                        FoldGroupItem = groupItem;
                        break;
                    }
                }
                ExplorerRestarter.Show();
            }
        }

        private void MoveItem(bool isUp)
        {
            WinXList list = (WinXList)Parent;
            int index = list.Controls.GetChildIndex(this);
            if(index == list.Controls.Count - 1) return;
            index += isUp ? -1 : 1;
            Control ctr = list.Controls[index];
            if(ctr is WinXGroupItem) return;
            WinXItem item = (WinXItem)ctr;
            string name1 = DesktopIni.GetLocalizedFileNames(FilePath);
            string name2 = DesktopIni.GetLocalizedFileNames(item.FilePath);
            DesktopIni.DeleteLocalizedFileNames(FilePath);
            DesktopIni.DeleteLocalizedFileNames(item.FilePath);
            string fileName1 = $@"{item.FileName.Substring(0, 2)}{FileName.Substring(2)}";
            string fileName2 = $@"{FileName.Substring(0, 2)}{item.FileName.Substring(2)}";
            string dirPath = Path.GetDirectoryName(FilePath);
            string path1 = $@"{dirPath}\{fileName1}";
            string path2 = $@"{dirPath}\{fileName2}";
            path1 = ObjectPath.GetNewPathWithIndex(path1, ObjectPath.PathType.File);
            path2 = ObjectPath.GetNewPathWithIndex(path2, ObjectPath.PathType.File);
            File.Move(FilePath, path1);
            File.Move(item.FilePath, path2);
            if(name1 != string.Empty) DesktopIni.SetLocalizedFileNames(path1, name1);
            if(name1 != string.Empty) DesktopIni.SetLocalizedFileNames(path2, name2);
            FilePath = path1;
            item.FilePath = path2;
            list.SetItemIndex(this, index);
            ExplorerRestarter.Show();
        }

        public void DeleteMe()
        {
            File.Delete(FilePath);
            DesktopIni.DeleteLocalizedFileNames(FilePath);
            if (File.Exists(DefaultFilePath))
            {
                File.Delete(DefaultFilePath);
                DesktopIni.DeleteLocalizedFileNames(DefaultFilePath);
            }
            ExplorerRestarter.Show();
            ShellLink.Dispose();
        }
    }
}