using BluePointLilac.Methods;
using ContextMenuManager.Methods;
using ContextMenuManager.Models;
using System;
using System.IO;
using System.Windows.Forms;

namespace ContextMenuManager
{
    //兼容.Net3.5和.Net4.0，兼容Vista - Win11
    static class Program
    {
        [STAThread]
        static void Main()
        {
#if DEBUG
            using (StreamWriter sw = new StreamWriter("D:\\log.txt", true))
            {
                sw.WriteLine("--------------------" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss ") + "--------------------");
            }
#endif
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            if(SingleInstance.IsRunning()) return;
            //BackupList.SaveBackupList("D:\\a.xml");
            AppString.LoadStrings();
            Updater.PeriodicUpdate();
            XmlDicHelper.ReloadDics();
            Application.Run(new MainForm());
        }
    }
}