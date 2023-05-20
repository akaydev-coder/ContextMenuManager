using BluePointLilac.Controls;
using BluePointLilac.Methods;
using ContextMenuManager.Methods;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ContextMenuManager.Controls.Interfaces
{
    interface ITsiRestoreItem
    {
        DeleteMeMenuItem TsiDeleteMe { get; set; }
        void RestoreMe();
    }

    sealed class RestoreMeMenuItem : ToolStripMenuItem
    {
        public RestoreMeMenuItem(ITsiRestoreItem item) : base("恢复备份")
        {
            Click += (sender, e) =>
            {
                item.RestoreMe();
            };
        }
    }
}
