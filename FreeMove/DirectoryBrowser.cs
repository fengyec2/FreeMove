using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Runtime.InteropServices;

namespace FreeMove
{
    public partial class DirectoryBrowser : UserControl
    {
        public event EventHandler<string> SourceSelected;
        public event EventHandler<string> TargetSelected;

        public DirectoryBrowser()
        {
            InitializeComponent();
            InitializeBrowser();
        }

        private void InitializeBrowser()
        {
            // 初始化列标题（后续会通过 ApplyLanguage 刷新）
            listView_Files.Columns.Add("Name", 200);
            listView_Files.Columns.Add("Type", 100);
            listView_Files.Columns.Add("Target Path", 250);

            // 加载驱动器
            LoadDrives();
        }

        private const int EM_SETCUEBANNER = 0x1501;
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);

        private void SetPlaceholder(TextBox textBox, string placeholder)
        {
#if NETCOREAPP
            textBox.PlaceholderText = placeholder;
#else
            SendMessage(textBox.Handle, EM_SETCUEBANNER, (IntPtr)1, placeholder);
#endif
        }

        public void ApplyLanguage()
        {
            button_SetSource.Text = Properties.Resources.ResourceManager.GetString("Button_SetSource") ?? "← Set Source";
            button_SetTarget.Text = Properties.Resources.ResourceManager.GetString("Button_SetTarget") ?? "→ Set Target";
            
            SetPlaceholder(textBox_Search, Properties.Resources.ResourceManager.GetString("Search_Placeholder") ?? "Search with Everything...");

            if (listView_Files.Columns.Count >= 3)
            {
                listView_Files.Columns[0].Text = Properties.Resources.ResourceManager.GetString("Column_Name") ?? "Name";
                listView_Files.Columns[1].Text = Properties.Resources.ResourceManager.GetString("Column_Type") ?? "Type";
                listView_Files.Columns[2].Text = Properties.Resources.ResourceManager.GetString("Column_TargetPath") ?? "Target Path";
            }

            setAsSourceToolStripMenuItem.Text = Properties.Resources.ResourceManager.GetString("Menu_SetSource") ?? "Set as Source";
            setAsTargetToolStripMenuItem.Text = Properties.Resources.ResourceManager.GetString("Menu_SetTarget") ?? "Set as Target";
            setAsSourceListViewToolStripMenuItem.Text = Properties.Resources.ResourceManager.GetString("Menu_SetSource") ?? "Set as Source";
            setAsTargetListViewToolStripMenuItem.Text = Properties.Resources.ResourceManager.GetString("Menu_SetTarget") ?? "Set as Target";
        }

        private void LoadDrives()
        {
            treeView_Dirs.Nodes.Clear();
            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                if (drive.IsReady)
                {
                    TreeNode node = new TreeNode(drive.Name);
                    node.Tag = drive.RootDirectory.FullName;
                    node.Nodes.Add(""); // 占位符，用于显示展开图标
                    treeView_Dirs.Nodes.Add(node);
                }
            }
        }

        private void treeView_Dirs_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            TreeNode node = e.Node;
            if (node.Nodes.Count == 1 && node.Nodes[0].Text == "")
            {
                node.Nodes.Clear();
                string path = (string)node.Tag;
                try
                {
                    foreach (string dir in Directory.GetDirectories(path))
                    {
                        DirectoryInfo di = new DirectoryInfo(dir);
                        TreeNode child = new TreeNode(di.Name);
                        child.Tag = di.FullName;
                        child.Nodes.Add(""); // 占位符
                        node.Nodes.Add(child);
                    }
                }
                catch (UnauthorizedAccessException) { /* 跳过无权限目录 */ }
                catch (Exception ex) { Console.WriteLine(ex.Message); }
            }
        }

        private void treeView_Dirs_AfterSelect(object sender, TreeViewEventArgs e)
        {
            // 清除搜索框，触发视图切换回普通模式
            if (!string.IsNullOrEmpty(textBox_Search.Text))
            {
                textBox_Search.Text = string.Empty;
            }
            RefreshListView((string)e.Node.Tag);
        }

        private void textBox_Search_TextChanged(object sender, EventArgs e)
        {
            string query = textBox_Search.Text.Trim();
            if (string.IsNullOrEmpty(query))
            {
                if (treeView_Dirs.SelectedNode != null)
                {
                    RefreshListView((string)treeView_Dirs.SelectedNode.Tag);
                }
                else
                {
                    listView_Files.Items.Clear();
                }
                return;
            }

            PerformSearch(query);
        }

        private void PerformSearch(string query)
        {
            if (!Everything.IsAvailable())
             {
                 string error = Everything.GetLastErrorMessage();
                 string message;
                 
                 if (error == "Everything is not running")
                 {
                     message = Properties.Resources.ResourceManager.GetString("Everything_NotRunning") ?? "Everything is not running.";
                 }
                 else if (error == "DLL not found")
                 {
                     message = Properties.Resources.ResourceManager.GetString("Everything_NotInstalled") ?? "Everything is not installed or the DLL is missing.";
                 }
                 else
                 {
                     message = error; // 显示其他具体错误
                 }
                 
                 listView_Files.Items.Clear();
                 ListViewItem item = new ListViewItem(message);
                item.ForeColor = Color.Red;
                listView_Files.Items.Add(item);
                return;
            }

            listView_Files.Items.Clear();
            // 限制搜索结果为文件夹
            foreach (string path in Everything.Search("folder:" + query))
            {
                try
                {
                    if (Directory.Exists(path))
                    {
                        DirectoryInfo di = new DirectoryInfo(path);
                        ListViewItem item = new ListViewItem(di.Name);
                        
                        bool isReparse = IOHelper.IsReparsePoint(di.FullName);
                        if (isReparse)
                        {
                            item.SubItems.Add(Properties.Resources.ResourceManager.GetString("Type_Symlink") ?? "Symbolic Link");
                            item.SubItems.Add(IOHelper.GetSymbolicLinkTarget(di.FullName) ?? "");
                            item.ForeColor = Color.Green;
                        }
                        else
                        {
                            item.SubItems.Add(Properties.Resources.ResourceManager.GetString("Type_Folder") ?? "Folder");
                            item.SubItems.Add(di.FullName); // 在搜索结果中，第三列显示完整路径
                        }
                        
                        item.Tag = di.FullName;
                        listView_Files.Items.Add(item);
                    }
                }
                catch { /* 忽略搜索结果中的错误项 */ }
            }
        }

        private void RefreshListView(string path)
        {
            listView_Files.Items.Clear();
            try
            {
                foreach (string dir in Directory.GetDirectories(path))
                {
                    try
                    {
                        DirectoryInfo di = new DirectoryInfo(dir);
                        ListViewItem item = new ListViewItem(di.Name);
                        
                        bool isReparse = IOHelper.IsReparsePoint(di.FullName);
                        if (isReparse)
                        {
                            item.SubItems.Add(Properties.Resources.ResourceManager.GetString("Type_Symlink") ?? "Symbolic Link");
                            item.SubItems.Add(IOHelper.GetSymbolicLinkTarget(di.FullName) ?? "");
                            item.ForeColor = Color.Green; // 符号链接显示为绿色
                        }
                        else
                        {
                            item.SubItems.Add(Properties.Resources.ResourceManager.GetString("Type_Folder") ?? "Folder");
                            item.SubItems.Add("");
                        }
                        
                        item.Tag = di.FullName;
                        listView_Files.Items.Add(item);
                    }
                    catch (UnauthorizedAccessException) { /* 跳过无权限子目录 */ }
                }
            }
            catch (UnauthorizedAccessException) { /* 跳过无权限根目录 */ }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
        }

        private void button_SetSource_Click(object sender, EventArgs e)
        {
            string path = GetSelectedPath();
            if (!string.IsNullOrEmpty(path)) SourceSelected?.Invoke(this, path);
        }

        private void button_SetTarget_Click(object sender, EventArgs e)
        {
            string path = GetSelectedPath();
            if (!string.IsNullOrEmpty(path)) TargetSelected?.Invoke(this, path);
        }

        private string GetSelectedPath()
        {
            if (listView_Files.SelectedItems.Count > 0)
            {
                return (string)listView_Files.SelectedItems[0].Tag;
            }
            if (treeView_Dirs.SelectedNode != null)
            {
                return (string)treeView_Dirs.SelectedNode.Tag;
            }
            return null;
        }

        private void setAsSourceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeView_Dirs.SelectedNode != null)
                SourceSelected?.Invoke(this, (string)treeView_Dirs.SelectedNode.Tag);
        }

        private void setAsTargetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeView_Dirs.SelectedNode != null)
                TargetSelected?.Invoke(this, (string)treeView_Dirs.SelectedNode.Tag);
        }

        private void setAsSourceListViewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView_Files.SelectedItems.Count > 0)
                SourceSelected?.Invoke(this, (string)listView_Files.SelectedItems[0].Tag);
        }

        private void setAsTargetListViewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView_Files.SelectedItems.Count > 0)
                TargetSelected?.Invoke(this, (string)listView_Files.SelectedItems[0].Tag);
        }
    }
}
