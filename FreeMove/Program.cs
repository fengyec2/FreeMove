// FreeMove -- Move directories without breaking shortcuts or installations 
//    Copyright(C) 2020  Luca De Martini

//    This program is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.

//    This program is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
//    GNU General Public License for more details.

//    You should have received a copy of the GNU General Public License
//    along with this program.If not, see<http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Globalization;
using System.Threading;
using System.Security.Principal;

namespace FreeMove
{
    static class Program
    {
        /// <summary>
        /// Punto di ingresso principale dell'applicazione.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // 如非管理员权限，提示并退出
            try
            {
                using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
                {
                    WindowsPrincipal principal = new WindowsPrincipal(identity);
                    if (!principal.IsInRole(WindowsBuiltInRole.Administrator))
                    {
                        string msg = Properties.Resources.ResourceManager.GetString("AdminRequiredMessage");
                        string title = Properties.Resources.ResourceManager.GetString("AdminRequiredTitle");
                        MessageBox.Show(msg, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }
            }
            catch
            {
                // 如果检查失败，出于安全考虑也直接退出
                string msg = Properties.Resources.ResourceManager.GetString("AdminRequiredMessage");
                string title = Properties.Resources.ResourceManager.GetString("AdminRequiredTitle");
                MessageBox.Show(msg, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 设置全局语言：优先使用用户在设置中选择的语言，否则跟随系统
            try
            {
                string lang = Settings.Language;
                CultureInfo culture;
                if (!string.IsNullOrEmpty(lang))
                {
                    culture = new CultureInfo(lang);
                }
                else
                {
                    culture = CultureInfo.InstalledUICulture;
                }
                Thread.CurrentThread.CurrentCulture = culture;
                Thread.CurrentThread.CurrentUICulture = culture;
            }
            catch
            {
                // 如果设置中的语言代码无效，忽略并使用系统默认
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
