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
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

namespace FreeMove
{
    public partial class Form1 : Form
    {
        bool safeMode = true;
        bool skipSecurityChecks = false;

        #region Initialization
        public Form1(bool skipSecurityChecks = false, string sourcePath = null, string destPath = null)
        {
            //Initialize UI elements
            InitializeComponent();

            this.skipSecurityChecks = skipSecurityChecks;

            // 初始化目录浏览器事件
            directoryBrowser1.SourceSelected += (s, path) => textBox_From.Text = path;
            directoryBrowser1.TargetSelected += (s, path) => textBox_To.Text = path;
            directoryBrowser1.RestoreRequested += async (s, args) => await RestoreLink(args.Symlink, args.Target, args.Move);

            if (!string.IsNullOrEmpty(sourcePath))
                textBox_From.Text = sourcePath;
            if (!string.IsNullOrEmpty(destPath))
                textBox_To.Text = destPath;
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            ApplyLanguage();
            SetToolTips();

            // 如果启用了跳过安全检查，显示警告对话框并禁用安全模式菜单
            if (skipSecurityChecks)
            {
                MessageBox.Show(
                    this,
                    Properties.Resources.ResourceManager.GetString("UnsafeMode_WarningMessage"),
                    Properties.Resources.ResourceManager.GetString("UnsafeMode_WarningTitle"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                
                // 禁用安全模式菜单项
                safeModeToolStripMenuItem.Enabled = false;
                safeModeToolStripMenuItem.Checked = false;
            }

            //Check whether the program is set to update on its start
            if (Settings.AutoUpdate)
            {
                //Update the menu item accordingly
                checkOnProgramStartToolStripMenuItem.Checked = true;
                //Start a background update task
                Updater updater = await Task<bool>.Run(() => Updater.SilentCheck());
                //If there is an update show the update dialog
                if (updater != null) updater.ShowDialog();
            }

            if (Settings.EnableContextMenu)
            {
                contextMenuToolStripMenuItem.Checked = true;
                RegisterContextMenu();
            }
            else
            {
                contextMenuToolStripMenuItem.Checked = false;
            }

            switch (Settings.PermCheck)
            {
                case Settings.PermissionCheckLevel.None:
                    noneToolStripMenuItem.Checked = true;
                    fastToolStripMenuItem.Checked = false;
                    fullToolStripMenuItem.Checked = false;
                    break;
                case Settings.PermissionCheckLevel.Fast:
                    noneToolStripMenuItem.Checked = false;
                    fastToolStripMenuItem.Checked = true;
                    fullToolStripMenuItem.Checked = false;
                    break;
                case Settings.PermissionCheckLevel.Full:
                    noneToolStripMenuItem.Checked = false;
                    fastToolStripMenuItem.Checked = false;
                    fullToolStripMenuItem.Checked = true;
                    break;
            }

            switch (Settings.CurrentWorkMode)
            {
                case Settings.WorkingMode.DirectoryOnly:
                    directoryOnlyToolStripMenuItem.Checked = true;
                    directoryAndFileToolStripMenuItem.Checked = false;
                    break;
                case Settings.WorkingMode.DirectoryAndFile:
                    directoryOnlyToolStripMenuItem.Checked = false;
                    directoryAndFileToolStripMenuItem.Checked = true;
                    break;
            }
        }

        #endregion

        /// <summary>
        /// 根据当前 UI 语言刷新界面上的文本（按钮/菜单/标签等）
        /// </summary>
        private void ApplyLanguage()
        {
            // 窗口标题
            Text = Properties.Resources.ResourceManager.GetString("Form_Title");

            // 标签和按钮
            label1.Text = Properties.Resources.ResourceManager.GetString("Label_From");
            label2.Text = Properties.Resources.ResourceManager.GetString("Label_To");
            button_BrowseFrom.Text = Properties.Resources.ResourceManager.GetString("Button_Browse");
            button_BrowseTo.Text = Properties.Resources.ResourceManager.GetString("Button_Browse");
            button_Move.Text = Properties.Resources.ResourceManager.GetString("Button_Move");
            button_Close.Text = Properties.Resources.ResourceManager.GetString("Button_Close");
            chkBox_originalHidden.Text = Properties.Resources.ResourceManager.GetString("Checkbox_OriginalHidden");
            chkBox_createDest.Text = Properties.Resources.ResourceManager.GetString("Checkbox_CreateDest");

            // 菜单
            settingsToolStripMenuItem.Text = Properties.Resources.ResourceManager.GetString("Menu_Settings");
            infoToolStripMenuItem.Text = Properties.Resources.ResourceManager.GetString("Menu_Info");
            lunguToolStripMenuItem.Text = Properties.Resources.ResourceManager.GetString("Menu_Language");

            workModeToolStripMenuItem.Text = Properties.Resources.ResourceManager.GetString("Menu_WorkingMode") ?? "Working Mode";
            directoryOnlyToolStripMenuItem.Text = Properties.Resources.ResourceManager.GetString("Menu_DirectoryOnly") ?? "Directory Only";
            directoryAndFileToolStripMenuItem.Text = Properties.Resources.ResourceManager.GetString("Menu_DirectoryAndFile") ?? "Directory and File";

            checkForUpdateToolStripMenuItem.Text = Properties.Resources.ResourceManager.GetString("Menu_CheckForUpdate");
            checkNowToolStripMenuItem.Text = Properties.Resources.ResourceManager.GetString("Menu_CheckNow");
            checkOnProgramStartToolStripMenuItem.Text = Properties.Resources.ResourceManager.GetString("Menu_CheckOnStart");

            PermissionCheckToolStripMenuItem.Text = Properties.Resources.ResourceManager.GetString("Menu_PermissionCheck");
            safeModeToolStripMenuItem.Text = Properties.Resources.ResourceManager.GetString("Menu_SafeMode");

            noneToolStripMenuItem.Text = Properties.Resources.ResourceManager.GetString("Menu_None");
            fastToolStripMenuItem.Text = Properties.Resources.ResourceManager.GetString("Menu_Fast");
            fullToolStripMenuItem.Text = Properties.Resources.ResourceManager.GetString("Menu_Full");

            noneToolStripMenuItem.ToolTipText = Properties.Resources.ResourceManager.GetString("Menu_NoneTooltip");
            fastToolStripMenuItem.ToolTipText = Properties.Resources.ResourceManager.GetString("Menu_FastTooltip");
            fullToolStripMenuItem.ToolTipText = Properties.Resources.ResourceManager.GetString("Menu_FullTooltip");

            contextMenuToolStripMenuItem.Text = Properties.Resources.ResourceManager.GetString("Menu_ContextMenu");

            reportAnIssueToolStripMenuItem.Text = Properties.Resources.ResourceManager.GetString("Menu_ReportIssue");
            gitHubToolStripMenuItem.Text = Properties.Resources.ResourceManager.GetString("Menu_GitHub");
            aboutToolStripMenuItem.Text = Properties.Resources.ResourceManager.GetString("Menu_About");
            helpToolStripMenuItem.Text = Properties.Resources.ResourceManager.GetString("HelpButtonText");

            zhToolStripMenuItem.Text = Properties.Resources.ResourceManager.GetString("Menu_Lang_English");
            chineseToolStripMenuItem.Text = Properties.Resources.ResourceManager.GetString("Menu_Lang_Chinese");

            // 语言菜单打勾状态
            string lang = Settings.Language;
            // lang 为空时：跟随系统，这里根据当前 Culture 判定
            string current = System.Globalization.CultureInfo.CurrentUICulture.Name;
            bool isChinese = (!string.IsNullOrEmpty(lang) ? lang : current).StartsWith("zh", StringComparison.OrdinalIgnoreCase);
            zhToolStripMenuItem.Checked = !isChinese;
            chineseToolStripMenuItem.Checked = isChinese;

            // 刷新目录浏览器语言
            directoryBrowser1.ApplyLanguage();
        }

        private async Task RestoreLink(string symlinkPath, string targetPath, bool move)
        {
            Enabled = false;
            try
            {
                // 先验证目标文件或文件夹是否存在
                if (!Directory.Exists(targetPath) && !File.Exists(targetPath))
                {
                    MessageBox.Show(Properties.Resources.ResourceManager.GetString("Restore_InvalidTarget"),
                        Properties.Resources.ResourceManager.GetString("ErrorTitle"),
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // 准备一个临时路径来存放符号链接（以便恢复）
                string tempSymlinkPath = symlinkPath + ".bak_" + DateTime.Now.Ticks;

                bool isFile = File.Exists(targetPath);
                try
                {
                    // 1. 将符号链接重命名（本质是移动）
                    if (isFile)
                        File.Move(symlinkPath, tempSymlinkPath);
                    else
                        Directory.Move(symlinkPath, tempSymlinkPath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(string.Format(System.Globalization.CultureInfo.CurrentUICulture, Properties.Resources.ResourceManager.GetString("Restore_PrepareFailed"), ex.Message),
                        Properties.Resources.ResourceManager.GetString("ErrorTitle"),
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                bool success = false;
                try
                {
                    if (move)
                    {
                        // 移动模式：直接使用现有的 BeginMove
                        await BeginMove(targetPath, symlinkPath);
                    }
                    else
                    {
                        // 复制模式
                        using (ProgressDialog progressDialog = new ProgressDialog(Properties.Resources.ResourceManager.GetString("MovingFilesTitle")))
                        {
                            IO.CopyOperation copyOp = new IO.CopyOperation(targetPath, symlinkPath);
                            copyOp.ProgressChanged += (sender, e) => progressDialog.UpdateProgress(e);
                            copyOp.End += (sender, e) => progressDialog.Invoke((Action)progressDialog.Close);

                            Task task = copyOp.Run();
                            progressDialog.ShowDialog(this);
                            await task;
                        }
                    }
                    success = true;
                }
                catch (Exception ex)
                {
                    // 如果失败，尝试恢复符号链接
                    try
                    {
                        if (isFile)
                        {
                            if (File.Exists(symlinkPath)) File.Delete(symlinkPath);
                            File.Move(tempSymlinkPath, symlinkPath);
                        }
                        else
                        {
                            if (Directory.Exists(symlinkPath)) Directory.Delete(symlinkPath, true);
                            Directory.Move(tempSymlinkPath, symlinkPath);
                        }
                    }
                    catch { /* 忽略恢复失败 */ }

                    MessageBox.Show(string.Format(System.Globalization.CultureInfo.CurrentUICulture, Properties.Resources.ResourceManager.GetString("Restore_Failed"), ex.Message),
                        Properties.Resources.ResourceManager.GetString("ErrorTitle"),
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                if (success)
                {
                    // 成功后删除备份的符号链接
                    try
                    {
                        if (isFile && File.Exists(tempSymlinkPath)) File.Delete(tempSymlinkPath);
                        else if (!isFile && Directory.Exists(tempSymlinkPath)) Directory.Delete(tempSymlinkPath);
                    }
                    catch { /* 忽略删除备份失败 */ }

                    MessageBox.Show(this, Properties.Resources.ResourceManager.GetString("DoneMessage"));
                }
            }
            finally
            {
                Enabled = true;
                // 刷新列表
                directoryBrowser1.RefreshBrowser();
            }
        }

        /// <summary>
        /// 执行移动前预检查，并在失败原因仅为只读属性时提供自动修复入口。
        /// </summary>
        private bool PreliminaryCheck(string source, string destination)
        {
            try
            {
                IOHelper.CheckDirectories(source, destination, safeMode, chkBox_createDest.Checked, skipAllChecks: skipSecurityChecks);
                return true;
            }
            catch (AggregateException ae) when (IOHelper.TryGetReadOnlyPrecheckFailures(ae, out List<IOHelper.ReadOnlyPrecheckException> readOnlyFailures))
            {
                // If every failure comes from read-only attributes, offer an in-place repair instead of failing immediately.
                DialogResult result = MessageBox.Show(
                    this,
                    string.Format(Properties.Resources.ResourceManager.GetString("ReadOnlyPrecheckConfirmMessage"), string.Join("\n", readOnlyFailures.ConvertAll(item => item.FilePath))),
                    Properties.Resources.ResourceManager.GetString("ReadOnlyPrecheckConfirmTitle"),
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button1);

                if (result != DialogResult.Yes)
                {
                    return false;
                }

                try
                {
                    IOHelper.ClearReadOnlyAttributes(source);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        this,
                        string.Format(Properties.Resources.ResourceManager.GetString("ReadOnlyClearFailedMessage"), ex.Message),
                        Properties.Resources.ResourceManager.GetString("ErrorTitle"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return false;
                }

                try
                {
                    // Re-run with Full so the move only continues after all files pass the stricter access check.
                    IOHelper.CheckDirectories(source, destination, safeMode, chkBox_createDest.Checked, Settings.PermissionCheckLevel.Full, skipAllChecks: skipSecurityChecks);
                    return true;
                }
                catch (Exception ex)
                {
                    ShowPreliminaryCheckError(ex);
                    return false;
                }
            }
            catch (Exception ex)
            {
                ShowPreliminaryCheckError(ex);
                return false;
            }
        }

        private void ShowPreliminaryCheckError(Exception ex)
        {
            if (ex is AggregateException aggregateException)
            {
                var msg = "";
                foreach (var innerException in aggregateException.InnerExceptions)
                {
                    msg += innerException.Message + "\n";
                }
                MessageBox.Show(msg, Properties.Resources.ResourceManager.GetString("ErrorTitle"));
                return;
            }

            MessageBox.Show(ex.Message, Properties.Resources.ResourceManager.GetString("ErrorTitle"));
        }

        private async void Begin()
        {
            Enabled = false;
            string source = textBox_From.Text.TrimEnd('\\');
            string destination = Path.Combine(textBox_To.Text.Length > 3 ? textBox_To.Text.TrimEnd('\\') : textBox_To.Text, Path.GetFileName(source));

            if (PreliminaryCheck(source, destination))
            {
                try
                {
                    await BeginMove(source, destination);
                    Symlink(destination, source);

                    if (chkBox_originalHidden.Checked)
                    {
                        DirectoryInfo olddir = new DirectoryInfo(source);
                        var attrib = File.GetAttributes(source);
                        olddir.Attributes = attrib | FileAttributes.Hidden;
                    }

                    // 刷新目录浏览器以显示最新状态（如符号链接）
                    directoryBrowser1.RefreshBrowser();

                    MessageBox.Show(this, Properties.Resources.ResourceManager.GetString("DoneMessage"));
                }
                catch (IO.MoveOperation.CopyFailedException ex)
                {
                    string question = string.Format(Properties.Resources.ResourceManager.GetString("UndoChangesDetailsQuestion"), ex.InnerException.Message);
                    switch (MessageBox.Show(this, question, ex.Message, MessageBoxButtons.YesNo, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1))
                    {
                        case DialogResult.Yes:
                            try
                            {
                                Directory.Delete(destination, true);
                            }
                            catch (Exception ie)
                            {
                                MessageBox.Show(this, ie.Message, Properties.Resources.ResourceManager.GetString("RemoveCopiedContentsFailed"));
                            }
                            break;
                        case DialogResult.No:
                            // MessageBox.Show(this, ie.Message, "Could not remove copied contents. Try removing manually");
                            break;
                    }
                }
                catch (IO.MoveOperation.DeleteFailedException ex)
                {
                    string question = string.Format(Properties.Resources.ResourceManager.GetString("UndoChangesDetailsQuestion"), ex.InnerException.Message);
                    switch (MessageBox.Show(this, question, ex.Message, MessageBoxButtons.YesNo, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1))
                    {
                        case DialogResult.Yes:
                            try
                            {
                                await BeginMove(destination, source);
                            }
                            catch (Exception ie)
                            {
                                MessageBox.Show(this, ie.Message, Properties.Resources.ResourceManager.GetString("MoveBackFailedMessage"));
                            }
                            break;
                        case DialogResult.No:
                            // MessageBox.Show(this, ie.Message, "Could not remove copied contents. Try removing manually");
                            break;
                    }
                }
                catch (IO.MoveOperation.MoveFailedException ex)
                {
                    string details = string.Format(Properties.Resources.ResourceManager.GetString("MoveFailedDetailsFormat"), ex.InnerException.Message);
                    MessageBox.Show(this, details, ex.Message, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (OperationCanceledException)
                {
                    string question = Properties.Resources.ResourceManager.GetString("UndoChangesSimpleQuestion");
                    string title = Properties.Resources.ResourceManager.GetString("CancelledTitle");
                    switch (MessageBox.Show(this, question, title, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1))
                    {
                        case DialogResult.Yes:
                            try
                            {
                                if (Directory.Exists(destination))
                                    Directory.Delete(destination, true);
                            }
                            catch (Exception ie)
                            {
                                MessageBox.Show(this, ie.Message, Properties.Resources.ResourceManager.GetString("RemoveCopiedContentsFailed"));
                            }
                            break;
                    }
                }
            }
            Enabled = true;
        }

        private async Task BeginMove(string source, string destination)
        {
            //Move files
            using (ProgressDialog progressDialog = new ProgressDialog(Properties.Resources.ResourceManager.GetString("MovingFilesTitle")))
            {
                IO.MoveOperation moveOp = IOHelper.MoveDir(source, destination, chkBox_createDest.Checked);

                moveOp.ProgressChanged += (sender, e) => progressDialog.UpdateProgress(e);
                moveOp.End += (sender, e) => progressDialog.Invoke((Action)progressDialog.Close);

                progressDialog.CancelRequested += (sender, e) =>
                {
                    string message = Properties.Resources.ResourceManager.GetString("CancelConfirmationMessage");
                    string title = Properties.Resources.ResourceManager.GetString("CancelConfirmationTitle");
                    if (DialogResult.Yes == MessageBox.Show(this, message, title, MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2))
                    {
                        moveOp.Cancel();
                        progressDialog.BeginInvoke(new Action(() => progressDialog.Cancellable = false));
                    }
                };

                Task task = moveOp.Run();

                progressDialog.ShowDialog(this);
                try
                {
                    await task;
                }
                finally
                {
                    progressDialog.Close();
                }
            }
        }

        private void Symlink(string destination, string link)
        {
            IOHelper.MakeLink(destination, link);
        }

        //Configure tooltips
        private void SetToolTips()
        {
            ToolTip Tip = new ToolTip()
            {
                ShowAlways = true,
                AutoPopDelay = 5000,
                InitialDelay = 600,
                ReshowDelay = 500
            };
            Tip.SetToolTip(this.textBox_From, Properties.Resources.ResourceManager.GetString("TooltipFrom"));
            Tip.SetToolTip(this.textBox_To, Properties.Resources.ResourceManager.GetString("TooltipTo"));
            Tip.SetToolTip(this.chkBox_originalHidden, Properties.Resources.ResourceManager.GetString("TooltipHidden"));
        }

        private void Reset()
        {
            textBox_From.Text = "";
            textBox_To.Text = "";
            textBox_From.Focus();
        }

        public static void Unauthorized(Exception ex)
        {
            string title = Properties.Resources.ResourceManager.GetString("ErrorDetailsTitle");
            MessageBox.Show(Properties.Resources.ErrorUnauthorizedMoveDetails + ex.Message, title);
        }

        #region Event Handlers

        private void Button_Move_Click(object sender, EventArgs e)
        {
            Begin();
        }

        //Show a directory picker for the source directory
        private void Button_BrowseFrom_Click(object sender, EventArgs e)
        {
            DialogResult result = folderBrowserDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                textBox_From.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        //Show a directory picker for the destination directory
        private void Button_BrowseTo_Click(object sender, EventArgs e)
        {
            DialogResult result = folderBrowserDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                textBox_To.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        //Start on enter key press
        private void TextBox_To_KeyUp(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                Begin();
            }
        }

        //Close the form
        private void Button_Close_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void OpenURL(string url)
        {
            var proc = new ProcessStartInfo(url)
            {
                UseShellExecute = true
            };
            Process.Start(proc);
        }

        //Open GitHub page
        private void GitHubToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenURL("https://github.com/fengyec2/FreeMove");
        }

        //Open the report an issue page on GitHub
        private void ReportAnIssueToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenURL("https://github.com/fengyec2/FreeMove/issues/new");
        }

        //Show an update dialog
        private void CheckNowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new Updater(false).ShowDialog();
        }

        //Set to check updates on program start
        private void CheckOnProgramStartToolStripMenuItem_Click(object sender, EventArgs e)
        {
            checkOnProgramStartToolStripMenuItem.Checked = !checkOnProgramStartToolStripMenuItem.Checked;
            Settings.AutoUpdate = checkOnProgramStartToolStripMenuItem.Checked;
        }
        #endregion

        private void AboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string msg = String.Format(Properties.Resources.AboutContent, System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).FileVersion);
            string title = Properties.Resources.ResourceManager.GetString("AboutTitle");
            MessageBox.Show(msg, title);
        }

        private void SafeModeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string title = Properties.Resources.ResourceManager.GetString("WarningTitle");
            if (MessageBox.Show(Properties.Resources.DisableSafeModeMessage, title, MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
            {
                safeMode = false;
                safeModeToolStripMenuItem.Checked = false;
                safeModeToolStripMenuItem.Enabled = false;
            }
        }

        private void NoneToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Settings.PermCheck = Settings.PermissionCheckLevel.None;
            noneToolStripMenuItem.Checked = true;
            fastToolStripMenuItem.Checked = false;
            fullToolStripMenuItem.Checked = false;
        }

        private void FastToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Settings.PermCheck = Settings.PermissionCheckLevel.Fast;
            noneToolStripMenuItem.Checked = false;
            fastToolStripMenuItem.Checked = true;
            fullToolStripMenuItem.Checked = false;
        }

        private void FullToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Settings.PermCheck = Settings.PermissionCheckLevel.Full;
            noneToolStripMenuItem.Checked = false;
            fastToolStripMenuItem.Checked = false;
            fullToolStripMenuItem.Checked = true;
        }

        private void zhToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // 切换为英文界面
            Settings.Language = "en";
            string msg = Properties.Resources.ResourceManager.GetString("LanguageChangedRestartMessage");
            string title = Properties.Resources.ResourceManager.GetString("LanguageChangedTitle");
            MessageBox.Show(msg, title);
            Application.Restart();
        }

        private void chineseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // 切换为简体中文界面
            Settings.Language = "zh-Hans";
            string msg = Properties.Resources.ResourceManager.GetString("LanguageChangedRestartMessage");
            string title = Properties.Resources.ResourceManager.GetString("LanguageChangedTitle");
            MessageBox.Show(msg, title);
            Application.Restart();
        }

        private void HelpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string help = Properties.Resources.ResourceManager.GetString("HelpContent");
            string title = Properties.Resources.ResourceManager.GetString("HelpButtonText");
            MessageBox.Show(this, help, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void contextMenuToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool isEnabled = !Settings.EnableContextMenu;
            Settings.EnableContextMenu = isEnabled;
            contextMenuToolStripMenuItem.Checked = isEnabled;

            if (isEnabled)
            {
                RegisterContextMenu();
            }
            else
            {
                UnregisterContextMenu();
            }
        }

        private void RegisterContextMenu()
        {
            try
            {
                string exePath = Application.ExecutablePath;
                string sourceLabel = Properties.Resources.ResourceManager.GetString("ContextMenu_SetSource") ?? "Set as Source";
                string destLabel = Properties.Resources.ResourceManager.GetString("ContextMenu_SetDest") ?? "Set as Destination";

                using (RegistryKey key = Registry.ClassesRoot.CreateSubKey(@"Directory\shell\FreeMove"))
                {
                    key.SetValue("MUIVerb", "FreeMove");
                    key.SetValue("Icon", $"\"{exePath}\"");
                    key.SetValue("SubCommands", "");

                    using (RegistryKey sourceKey = key.CreateSubKey(@"shell\SetSource"))
                    {
                        sourceKey.SetValue("MUIVerb", sourceLabel);
                        sourceKey.SetValue("Icon", $"\"{exePath}\"");
                        using (RegistryKey commandKey = sourceKey.CreateSubKey("command"))
                        {
                            commandKey.SetValue("", $"\"{exePath}\" --source \"%1\"");
                        }
                    }

                    using (RegistryKey destKey = key.CreateSubKey(@"shell\SetDestination"))
                    {
                        destKey.SetValue("MUIVerb", destLabel);
                        destKey.SetValue("Icon", $"\"{exePath}\"");
                        using (RegistryKey commandKey = destKey.CreateSubKey("command"))
                        {
                            commandKey.SetValue("", $"\"{exePath}\" --destination \"%1\"");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Properties.Resources.ResourceManager.GetString("ErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                Settings.EnableContextMenu = false;
                contextMenuToolStripMenuItem.Checked = false;
            }
        }

        private void UnregisterContextMenu()
        {
            try
            {
                Registry.ClassesRoot.DeleteSubKeyTree(@"Directory\shell\FreeMove", false);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Properties.Resources.ResourceManager.GetString("ErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void directoryOnlyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Settings.CurrentWorkMode = Settings.WorkingMode.DirectoryOnly;
            directoryOnlyToolStripMenuItem.Checked = true;
            directoryAndFileToolStripMenuItem.Checked = false;
            directoryBrowser1.RefreshBrowser();
        }

        private void directoryAndFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Settings.CurrentWorkMode = Settings.WorkingMode.DirectoryAndFile;
            directoryOnlyToolStripMenuItem.Checked = false;
            directoryAndFileToolStripMenuItem.Checked = true;
            directoryBrowser1.RefreshBrowser();
        }
    }
}
