// FreeMove -- Move directories without breaking shortcuts or installations
//    Copyright(C) 2020  Luca De Martini
//
//    This program is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FreeMove.Recommendations
{
    /// <summary>
    /// Lists the largest movable folders on the system drive. The person ticks the ones to move,
    /// and on OK the caller moves each one to <see cref="DestinationRoot"/> and links it back.
    /// </summary>
    public class RecommendationsForm : Form
    {
        readonly TextBox textBox_Dest;
        readonly Button button_BrowseDest;
        readonly NumericUpDown numeric_MinSize;
        readonly Button button_Scan;
        readonly ListView listView;
        readonly Label label_Status;
        readonly Button button_SelectAll;
        readonly Button button_SelectNone;
        readonly Button button_Move;
        readonly Button button_Cancel;

        CancellationTokenSource scanCts;

        /// <summary>The folder accepted recommendations are moved into, e.g. D:\FreeMove.</summary>
        public string DestinationRoot => textBox_Dest.Text.Trim().TrimEnd('\\');

        /// <summary>The recommendations the person ticked, largest first.</summary>
        public List<MoveRecommendation> SelectedRecommendations =>
            listView.CheckedItems.Cast<ListViewItem>().Select(i => (MoveRecommendation)i.Tag).ToList();

        static string T(string key, string fallback) =>
            Properties.Resources.ResourceManager.GetString(key) ?? fallback;

        public RecommendationsForm()
        {
            Text = T("Rec_Title", "Recommended moves");
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(720, 420);
            Size = new Size(860, 540);
            ShowIcon = false;
            ShowInTaskbar = false;
            Font = SystemFonts.MessageBoxFont;

            // --- Top: destination and minimum size ---
            var top = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 6,
                Padding = new Padding(8, 8, 8, 4)
            };
            top.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            textBox_Dest = new TextBox { Dock = DockStyle.Fill, Text = RecommendationScanner.SuggestDestinationRoot() ?? @"D:\FreeMove" };
            textBox_Dest.TextChanged += (s, e) => RefreshItems();
            button_BrowseDest = new Button { Text = T("Button_Browse", "Browse..."), AutoSize = true };
            button_BrowseDest.Click += BrowseDest_Click;
            numeric_MinSize = new NumericUpDown { Minimum = 50, Maximum = 1000000, Increment = 100, Value = 500, Width = 80 };
            button_Scan = new Button { Text = T("Rec_Scan", "Scan"), AutoSize = true };
            button_Scan.Click += async (s, e) => await ScanAsync();

            top.Controls.Add(new Label { Text = T("Rec_MoveTo", "Move to:"), AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
            top.Controls.Add(textBox_Dest, 1, 0);
            top.Controls.Add(button_BrowseDest, 2, 0);
            top.Controls.Add(new Label { Text = T("Rec_MinSize", "Minimum size (MB):"), AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(12, 3, 3, 3) }, 3, 0);
            top.Controls.Add(numeric_MinSize, 4, 0);
            top.Controls.Add(button_Scan, 5, 0);

            // --- Middle: the recommendation list ---
            listView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                CheckBoxes = true,
                FullRowSelect = true,
                GridLines = true,
                HideSelection = false
            };
            listView.Columns.Add(T("Rec_ColFolder", "Folder"), 330);
            listView.Columns.Add(T("Rec_ColSize", "Size"), 80, HorizontalAlignment.Right);
            listView.Columns.Add(T("Rec_ColType", "Type"), 90);
            listView.Columns.Add(T("Rec_ColDest", "Will be moved to"), 330);
            listView.ItemChecked += (s, e) => UpdateStatus();

            // --- Bottom: status and actions ---
            var bottom = new TableLayoutPanel
            {
                Dock = DockStyle.Bottom,
                AutoSize = true,
                ColumnCount = 5,
                Padding = new Padding(8, 4, 8, 8)
            };
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int i = 0; i < 4; i++) bottom.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            label_Status = new Label { AutoSize = true, Anchor = AnchorStyles.Left, MaximumSize = new Size(420, 0) };
            button_SelectAll = new Button { Text = T("Rec_SelectAll", "Select all"), AutoSize = true };
            button_SelectAll.Click += (s, e) => SetAllChecked(true);
            button_SelectNone = new Button { Text = T("Rec_SelectNone", "Select none"), AutoSize = true };
            button_SelectNone.Click += (s, e) => SetAllChecked(false);
            button_Move = new Button { Text = T("Rec_MoveSelected", "Move selected"), AutoSize = true, Enabled = false };
            button_Move.Click += Move_Click;
            button_Cancel = new Button { Text = T("Button_Close", "Close"), AutoSize = true, DialogResult = DialogResult.Cancel };

            bottom.Controls.Add(label_Status, 0, 0);
            bottom.Controls.Add(button_SelectAll, 1, 0);
            bottom.Controls.Add(button_SelectNone, 2, 0);
            bottom.Controls.Add(button_Move, 3, 0);
            bottom.Controls.Add(button_Cancel, 4, 0);

            Controls.Add(listView);
            Controls.Add(bottom);
            Controls.Add(top);

            CancelButton = button_Cancel;
            Shown += async (s, e) => await ScanAsync();
            FormClosing += (s, e) => scanCts?.Cancel();

            label_Status.Text = T("Rec_Hint", "Folders are ranked by size. Tick the ones to move; each is moved and linked back so programs keep working.");
        }

        async Task ScanAsync()
        {
            scanCts?.Cancel();
            scanCts = new CancellationTokenSource();
            CancellationToken token = scanCts.Token;

            listView.Items.Clear();
            SetBusy(true);
            var progress = new Progress<string>(path =>
            {
                if (!token.IsCancellationRequested)
                    label_Status.Text = string.Format(T("Rec_Scanning", "Scanning {0}"), path);
            });

            try
            {
                long minBytes = (long)numeric_MinSize.Value * 1024L * 1024L;
                List<MoveRecommendation> results = await RecommendationScanner.ScanAsync(minBytes, progress, token);
                if (token.IsCancellationRequested || IsDisposed) return;

                listView.BeginUpdate();
                foreach (MoveRecommendation rec in results)
                {
                    var item = new ListViewItem(rec.SourcePath) { Tag = rec };
                    item.SubItems.Add(RecommendationScanner.FormatSize(rec.SizeBytes));
                    item.SubItems.Add(KindLabel(rec.Kind));
                    item.SubItems.Add("");
                    listView.Items.Add(item);
                }
                listView.EndUpdate();
                RefreshItems();

                if (results.Count == 0)
                    label_Status.Text = T("Rec_NoneFound", "No movable folders above the minimum size were found.");
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                if (!IsDisposed) label_Status.Text = ex.Message;
            }
            finally
            {
                if (!IsDisposed) SetBusy(false);
            }
        }

        static string KindLabel(RecommendationKind kind)
        {
            switch (kind)
            {
                case RecommendationKind.Program: return T("Rec_KindProgram", "Program");
                case RecommendationKind.AppData: return T("Rec_KindAppData", "App data");
                case RecommendationKind.UserFolder: return T("Rec_KindUser", "User folder");
                default: return T("Rec_KindOther", "Other");
            }
        }

        void SetBusy(bool busy)
        {
            button_Scan.Enabled = !busy;
            numeric_MinSize.Enabled = !busy;
            button_SelectAll.Enabled = !busy;
            button_SelectNone.Enabled = !busy;
            UseWaitCursor = busy;
            if (busy) button_Move.Enabled = false;
            else UpdateStatus();
        }

        /// <summary>Recomputes each item's destination after the destination root changes.</summary>
        void RefreshItems()
        {
            string root = DestinationRoot;
            foreach (ListViewItem item in listView.Items)
            {
                var rec = (MoveRecommendation)item.Tag;
                item.SubItems[3].Text = IsDestinationValid(root, out _) ? rec.GetDestination(root) : "";
            }
            UpdateStatus();
        }

        void SetAllChecked(bool value)
        {
            listView.BeginUpdate();
            foreach (ListViewItem item in listView.Items) item.Checked = value;
            listView.EndUpdate();
            UpdateStatus();
        }

        void UpdateStatus()
        {
            if (button_Scan == null || !button_Scan.Enabled) return; // still scanning

            List<MoveRecommendation> selected = SelectedRecommendations;
            long total = selected.Sum(r => r.SizeBytes);

            if (!IsDestinationValid(DestinationRoot, out string destError))
            {
                label_Status.Text = destError;
                button_Move.Enabled = false;
                return;
            }

            long free = GetFreeSpace(DestinationRoot);
            label_Status.Text = string.Format(T("Rec_Status", "{0} selected, {1} total. Free on destination: {2}."),
                selected.Count, RecommendationScanner.FormatSize(total), free < 0 ? "?" : RecommendationScanner.FormatSize(free));

            bool fits = free < 0 || total < free;
            if (!fits)
                label_Status.Text += " " + T("Rec_NotEnoughSpace", "Not enough free space for this selection.");

            button_Move.Enabled = selected.Count > 0 && fits;
        }

        static bool IsDestinationValid(string root, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(root) || root.Length < 3 || root[1] != ':' || root[2] != '\\')
            {
                error = T("Rec_DestInvalid", "Enter a full destination path, e.g. D:\\FreeMove.");
                return false;
            }
            try
            {
                string drive = Path.GetPathRoot(Path.GetFullPath(root));
                if (string.Equals(drive, RecommendationScanner.SystemDrive, StringComparison.OrdinalIgnoreCase))
                {
                    error = T("Rec_DestSameDrive", "The destination must be on a different drive than Windows.");
                    return false;
                }
                var info = new DriveInfo(drive);
                if (!info.IsReady)
                {
                    error = string.Format(T("Rec_DestNotReady", "Drive {0} is not available."), drive);
                    return false;
                }
                if (!string.Equals(info.DriveFormat, "NTFS", StringComparison.OrdinalIgnoreCase))
                {
                    error = string.Format(T("Rec_DestNotNtfs", "Drive {0} is {1}; links need an NTFS drive."), drive, info.DriveFormat);
                    return false;
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
            return true;
        }

        static long GetFreeSpace(string root)
        {
            try { return new DriveInfo(Path.GetPathRoot(root)).AvailableFreeSpace; }
            catch { return -1; }
        }

        void BrowseDest_Click(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                if (Directory.Exists(DestinationRoot)) dialog.SelectedPath = DestinationRoot;
                if (dialog.ShowDialog(this) == DialogResult.OK)
                    textBox_Dest.Text = dialog.SelectedPath;
            }
        }

        void Move_Click(object sender, EventArgs e)
        {
            List<MoveRecommendation> selected = SelectedRecommendations;
            if (selected.Count == 0) return;

            string list = string.Join("\n", selected.Take(15).Select(r =>
                $"{r.SourcePath}  ({RecommendationScanner.FormatSize(r.SizeBytes)})"));
            if (selected.Count > 15) list += "\n...";

            string message = string.Format(
                T("Rec_ConfirmMessage", "Move {0} folder(s), {1} in total, to {2}?\n\n{3}\n\nClose the programs that use these folders before continuing. Each folder is moved and a link is left in its place."),
                selected.Count, RecommendationScanner.FormatSize(selected.Sum(r => r.SizeBytes)), DestinationRoot, list);

            if (MessageBox.Show(this, message, T("Rec_ConfirmTitle", "Confirm move"), MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
            {
                DialogResult = DialogResult.OK;
                Close();
            }
        }
    }
}
