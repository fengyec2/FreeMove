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
    public class RestoreRequestEventArgs : EventArgs
    {
        public string Symlink { get; set; }
        public string Target { get; set; }
        public bool Move { get; set; }
    }

    public partial class DirectoryBrowser : UserControl
    {
        public event EventHandler<string> SourceSelected;
        public event EventHandler<string> TargetSelected;
        public event EventHandler<RestoreRequestEventArgs> RestoreRequested;

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
            locateInTreeViewToolStripMenuItem.Text = Properties.Resources.ResourceManager.GetString("Menu_LocateInTreeView") ?? "Locate in TreeView";
            restoreSymlinkToolStripMenuItem.Text = Properties.Resources.ResourceManager.GetString("Menu_RestoreSymlink") ?? "Restore Symbolic Link";

            // TreeView Menu
            newToolStripMenuItemTV.Text = Properties.Resources.ResourceManager.GetString("Menu_New") ?? "New";
            newFolderToolStripMenuItemTV.Text = Properties.Resources.ResourceManager.GetString("Menu_Folder") ?? "Folder";
            newFileToolStripMenuItemTV.Text = Properties.Resources.ResourceManager.GetString("Menu_File") ?? "File";
            deleteToolStripMenuItemTV.Text = Properties.Resources.ResourceManager.GetString("Menu_Delete") ?? "Delete";
            renameToolStripMenuItemTV.Text = Properties.Resources.ResourceManager.GetString("Menu_Rename") ?? "Rename";
            refreshToolStripMenuItemTV.Text = Properties.Resources.ResourceManager.GetString("Menu_Refresh") ?? "Refresh";

            // ListView Menu
            newToolStripMenuItemLV.Text = Properties.Resources.ResourceManager.GetString("Menu_New") ?? "New";
            newFolderToolStripMenuItemLV.Text = Properties.Resources.ResourceManager.GetString("Menu_Folder") ?? "Folder";
            newFileToolStripMenuItemLV.Text = Properties.Resources.ResourceManager.GetString("Menu_File") ?? "File";
            deleteToolStripMenuItemLV.Text = Properties.Resources.ResourceManager.GetString("Menu_Delete") ?? "Delete";
            renameToolStripMenuItemLV.Text = Properties.Resources.ResourceManager.GetString("Menu_Rename") ?? "Rename";
            refreshToolStripMenuItemLV.Text = Properties.Resources.ResourceManager.GetString("Menu_Refresh") ?? "Refresh";
        }

        public void RefreshBrowser()
        {
            if (treeView_Dirs.SelectedNode != null)
            {
                RefreshListView((string)treeView_Dirs.SelectedNode.Tag);
            }
            else
            {
                LoadDrives();
            }
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

                // 列出文件
                try
                {
                    foreach (string file in Directory.GetFiles(path))
                    {
                        FileInfo fi = new FileInfo(file);
                        ListViewItem item = new ListViewItem(fi.Name);
                        item.SubItems.Add(Properties.Resources.ResourceManager.GetString("Type_File") ?? "File");
                        item.SubItems.Add("");
                        item.Tag = fi.FullName;
                        listView_Files.Items.Add(item);
                    }
                }
                catch (UnauthorizedAccessException) { /* 跳过无权限文件 */ }
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

        private void locateInTreeViewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView_Files.SelectedItems.Count > 0)
            {
                string path = (string)listView_Files.SelectedItems[0].Tag;
                if (!string.IsNullOrEmpty(path))
                {
                    LocatePathInTreeView(path);
                }
            }
        }

        private void restoreSymlinkToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView_Files.SelectedItems.Count > 0)
            {
                string path = (string)listView_Files.SelectedItems[0].Tag;
                if (!string.IsNullOrEmpty(path))
                {
                    if (IOHelper.IsReparsePoint(path))
                    {
                        string target = IOHelper.GetSymbolicLinkTarget(path);
                        if (!string.IsNullOrEmpty(target) && Directory.Exists(target))
                        {
                            string prompt = string.Format(Properties.Resources.ResourceManager.GetString("Restore_Prompt"), path, target);
                            string title = Properties.Resources.ResourceManager.GetString("Restore_Title");
                            
                            DialogResult result = MessageBox.Show(prompt, title, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                            if (result == DialogResult.Yes || result == DialogResult.No)
                            {
                                RestoreRequested?.Invoke(this, new RestoreRequestEventArgs
                                {
                                    Symlink = path,
                                    Target = target,
                                    Move = result == DialogResult.Yes
                                });
                            }
                        }
                        else
                        {
                            MessageBox.Show(Properties.Resources.ResourceManager.GetString("Restore_InvalidTarget"), 
                                Properties.Resources.ResourceManager.GetString("ErrorTitle"), 
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    else
                    {
                        MessageBox.Show(Properties.Resources.ResourceManager.GetString("Restore_NotSymlink"), 
                            Properties.Resources.ResourceManager.GetString("ErrorTitle"), 
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void LocatePathInTreeView(string targetPath)
        {
            targetPath = targetPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            
            // 找到对应的驱动器根节点
            TreeNode currentNode = null;
            foreach (TreeNode node in treeView_Dirs.Nodes)
            {
                string nodePath = ((string)node.Tag).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (targetPath.StartsWith(nodePath, StringComparison.OrdinalIgnoreCase))
                {
                    currentNode = node;
                    break;
                }
            }

            if (currentNode == null) return;

            // 逐层展开并查找
            string currentPath = ((string)currentNode.Tag).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string remainingPath = targetPath.Substring(currentPath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string[] parts = remainingPath.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string part in parts)
            {
                currentNode.Expand();
                TreeNode foundChild = null;
                foreach (TreeNode child in currentNode.Nodes)
                {
                    if (string.Equals(child.Text, part, StringComparison.OrdinalIgnoreCase))
                    {
                        foundChild = child;
                        break;
                    }
                }

                if (foundChild != null)
                {
                    currentNode = foundChild;
                }
                else
                {
                    // 没找到对应的子目录
                    break;
                }
            }

            // 选中并确保可见
            treeView_Dirs.SelectedNode = currentNode;
            currentNode.EnsureVisible();
            treeView_Dirs.Focus();
        }

        // Helper for Input Dialog
        private string ShowInputDialog(string text, string caption, string defaultValue = "")
        {
            Form prompt = new Form()
            {
                Width = 400,
                Height = 150,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = caption,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            };
            Label textLabel = new Label() { Left = 20, Top = 20, Text = text, AutoSize = true };
            TextBox textBox = new TextBox() { Left = 20, Top = 50, Width = 340, Text = defaultValue };
            Button confirmation = new Button() { Text = "OK", Left = 260, Width = 100, Top = 80, DialogResult = DialogResult.OK };
            
            prompt.Controls.Add(textBox);
            prompt.Controls.Add(confirmation);
            prompt.Controls.Add(textLabel);
            prompt.AcceptButton = confirmation;

            return prompt.ShowDialog() == DialogResult.OK ? textBox.Text : "";
        }

        // TreeView Context Menu Handlers
        private void newFolderToolStripMenuItemTV_Click(object sender, EventArgs e)
        {
            CreateNewItem(true);
        }

        private void newFileToolStripMenuItemTV_Click(object sender, EventArgs e)
        {
            CreateNewItem(false);
        }

        private void deleteToolStripMenuItemTV_Click(object sender, EventArgs e)
        {
            if (treeView_Dirs.SelectedNode != null && treeView_Dirs.SelectedNode.Parent != null) // Cannot delete Drive roots
            {
                DeleteItem((string)treeView_Dirs.SelectedNode.Tag, true, treeView_Dirs.SelectedNode);
            }
        }

        private void renameToolStripMenuItemTV_Click(object sender, EventArgs e)
        {
             if (treeView_Dirs.SelectedNode != null && treeView_Dirs.SelectedNode.Parent != null)
            {
                RenameItem((string)treeView_Dirs.SelectedNode.Tag, true, treeView_Dirs.SelectedNode);
            }
        }

        private void refreshToolStripMenuItemTV_Click(object sender, EventArgs e)
        {
            RefreshBrowser();
        }

        // ListView Context Menu Handlers
        private void newFolderToolStripMenuItemLV_Click(object sender, EventArgs e)
        {
            CreateNewItem(true);
        }

        private void newFileToolStripMenuItemLV_Click(object sender, EventArgs e)
        {
            CreateNewItem(false);
        }

        private void deleteToolStripMenuItemLV_Click(object sender, EventArgs e)
        {
            if (listView_Files.SelectedItems.Count > 0)
            {
                string path = (string)listView_Files.SelectedItems[0].Tag;
                bool isFolder = Directory.Exists(path);
                DeleteItem(path, isFolder);
            }
        }

        private void renameToolStripMenuItemLV_Click(object sender, EventArgs e)
        {
            if (listView_Files.SelectedItems.Count > 0)
            {
                string path = (string)listView_Files.SelectedItems[0].Tag;
                bool isFolder = Directory.Exists(path);
                RenameItem(path, isFolder);
            }
        }

        private void refreshToolStripMenuItemLV_Click(object sender, EventArgs e)
        {
            RefreshBrowser();
        }

        // Shared Logic
        private void CreateNewItem(bool isFolder)
        {
            if (treeView_Dirs.SelectedNode == null) return;

            string currentPath = (string)treeView_Dirs.SelectedNode.Tag;
            string typeName = isFolder ? (Properties.Resources.ResourceManager.GetString("Menu_Folder") ?? "Folder") : (Properties.Resources.ResourceManager.GetString("Menu_File") ?? "File");
            string title = isFolder ? (Properties.Resources.ResourceManager.GetString("NewFolder_Title") ?? "New Folder") : (Properties.Resources.ResourceManager.GetString("NewFile_Title") ?? "New File");
            string prompt = isFolder ? (Properties.Resources.ResourceManager.GetString("NewFolder_Prompt") ?? "Enter name for new Folder:") : (Properties.Resources.ResourceManager.GetString("NewFile_Prompt") ?? "Enter name for new File:");
            
            string name = ShowInputDialog(prompt, title);
            
            if (string.IsNullOrWhiteSpace(name)) return;

            string fullPath = Path.Combine(currentPath, name);
            try
            {
                if (isFolder)
                {
                    if (Directory.Exists(fullPath)) throw new Exception(Properties.Resources.ResourceManager.GetString("Error_FolderExists") ?? "Folder already exists.");
                    Directory.CreateDirectory(fullPath);
                    
                    // Update TreeView if expanded
                    if (treeView_Dirs.SelectedNode.IsExpanded)
                    {
                        TreeNode newNode = new TreeNode(name);
                        newNode.Tag = fullPath;
                        newNode.Nodes.Add(""); // Placeholder
                        treeView_Dirs.SelectedNode.Nodes.Add(newNode);
                    }
                }
                else
                {
                    if (File.Exists(fullPath)) throw new Exception(Properties.Resources.ResourceManager.GetString("Error_FileExists") ?? "File already exists.");
                    File.Create(fullPath).Close();
                }
                RefreshListView(currentPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Properties.Resources.ResourceManager.GetString("ErrorTitle") ?? "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DeleteItem(string path, bool isFolder, TreeNode nodeToRemove = null)
        {
            string name = Path.GetFileName(path);
            string prompt = string.Format(Properties.Resources.ResourceManager.GetString("ConfirmDelete_Message") ?? "Are you sure you want to delete '{0}'?", name);
            string title = Properties.Resources.ResourceManager.GetString("ConfirmDelete_Title") ?? "Confirm Delete";
            
            if (MessageBox.Show(prompt, title, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                try
                {
                    if (isFolder)
                    {
                        Directory.Delete(path, true);
                        if (nodeToRemove != null) 
                        {
                            nodeToRemove.Remove();
                        }
                        else
                        {
                            // If deleted from ListView, try to remove from TreeView if visible
                            // We can search for it in current node's children
                            if (treeView_Dirs.SelectedNode != null)
                            {
                                foreach(TreeNode child in treeView_Dirs.SelectedNode.Nodes)
                                {
                                    if ((string)child.Tag == path)
                                    {
                                        child.Remove();
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        File.Delete(path);
                    }
                    
                    if (treeView_Dirs.SelectedNode != null)
                        RefreshListView((string)treeView_Dirs.SelectedNode.Tag);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, Properties.Resources.ResourceManager.GetString("ErrorTitle") ?? "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void RenameItem(string oldPath, bool isFolder, TreeNode nodeToUpdate = null)
        {
            string oldName = Path.GetFileName(oldPath);
            string renameTitle = Properties.Resources.ResourceManager.GetString("Menu_Rename") ?? "Rename";
            string prompt = string.Format(Properties.Resources.ResourceManager.GetString("Rename_Prompt") ?? "Enter new name for '{0}':", oldName);
            
            string newName = ShowInputDialog(prompt, renameTitle, oldName);
            
            if (string.IsNullOrWhiteSpace(newName) || newName == oldName) return;

            string parentDir = Path.GetDirectoryName(oldPath);
            string newPath = Path.Combine(parentDir, newName);

            try
            {
                if (isFolder)
                {
                    Directory.Move(oldPath, newPath);
                    if (nodeToUpdate != null)
                    {
                        nodeToUpdate.Text = newName;
                        nodeToUpdate.Tag = newPath;
                    }
                    else
                    {
                        // Update TreeView node if visible
                        if (treeView_Dirs.SelectedNode != null)
                        {
                            foreach(TreeNode child in treeView_Dirs.SelectedNode.Nodes)
                            {
                                if ((string)child.Tag == oldPath)
                                {
                                    child.Text = newName;
                                    child.Tag = newPath;
                                    break;
                                }
                            }
                        }
                    }
                }
                else
                {
                    File.Move(oldPath, newPath);
                }

                if (treeView_Dirs.SelectedNode != null)
                    RefreshListView((string)treeView_Dirs.SelectedNode.Tag);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Properties.Resources.ResourceManager.GetString("ErrorTitle") ?? "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
