// FreeMove -- Move directories without breaking shortcuts or installations
//    Copyright(C) 2020  Luca De Martini
//
//    This program is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using FreeMove.Recommendations;

namespace FreeMove
{
    // Batch moving of the folders accepted in the "Recommended moves" window.
    public partial class Form1
    {
        static string R(string key, string fallback) =>
            Properties.Resources.ResourceManager.GetString(key) ?? fallback;

        private async void RecommendedMovesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            List<MoveRecommendation> accepted;
            string destinationRoot;

            using (var form = new RecommendationsForm())
            {
                if (form.ShowDialog(this) != DialogResult.OK) return;
                accepted = form.SelectedRecommendations;
                destinationRoot = form.DestinationRoot;
            }

            if (accepted.Count == 0) return;
            await MoveRecommendations(accepted, destinationRoot);
        }

        /// <summary>
        /// Moves each accepted folder into <paramref name="destinationRoot"/> (mirroring its path)
        /// and leaves a symbolic link at the original location. A failed item is rolled back and the
        /// batch continues; cancelling stops the batch. A summary is shown at the end.
        /// </summary>
        private async Task MoveRecommendations(List<MoveRecommendation> items, string destinationRoot)
        {
            var moved = new List<MoveRecommendation>();
            var failed = new List<string>();
            bool cancelled = false;

            Enabled = false;
            try
            {
                foreach (MoveRecommendation item in items)
                {
                    string source = item.SourcePath.TrimEnd('\\');
                    string destination = item.GetDestination(destinationRoot);

                    string error = await MoveAndLinkOne(source, destination);
                    if (error == null)
                    {
                        moved.Add(item);
                    }
                    else if (error == CancelledMarker)
                    {
                        cancelled = true;
                        break;
                    }
                    else
                    {
                        failed.Add($"{source}\n    {error}");
                    }
                }
            }
            finally
            {
                Enabled = true;
                directoryBrowser1.RefreshBrowser();
            }

            var summary = new StringBuilder();
            summary.AppendFormat(R("Rec_SummaryMoved", "Moved {0} folder(s), freeing about {1} on {2}."),
                moved.Count, RecommendationScanner.FormatSize(moved.Sum(m => m.SizeBytes)), RecommendationScanner.SystemDrive);
            if (failed.Count > 0)
            {
                summary.AppendLine().AppendLine();
                summary.AppendLine(R("Rec_SummaryFailed", "These were not moved and were left as they were:"));
                foreach (string f in failed) summary.AppendLine(f);
            }
            if (cancelled)
            {
                summary.AppendLine().AppendLine();
                summary.Append(R("Rec_SummaryCancelled", "The batch was cancelled; remaining folders were not touched."));
            }

            MessageBox.Show(this, summary.ToString(), R("Rec_SummaryTitle", "Recommended moves"),
                MessageBoxButtons.OK, failed.Count > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
        }

        const string CancelledMarker = "\u0000cancelled";

        /// <summary>
        /// Moves one folder and links it back. Returns null on success, <see cref="CancelledMarker"/>
        /// when the person cancelled, or an error message. On failure the original is restored.
        /// </summary>
        private async Task<string> MoveAndLinkOne(string source, string destination)
        {
            try
            {
                string parent = Path.GetDirectoryName(destination);
                if (!Directory.Exists(parent)) Directory.CreateDirectory(parent);

                // Same checks as a manual move (blacklist, safe mode, permissions, free space)
                IOHelper.CheckDirectories(source, destination, safeMode, createDestination: true, skipAllChecks: skipSecurityChecks);
            }
            catch (AggregateException ae)
            {
                return string.Join("; ", ae.InnerExceptions.Select(x => x.Message));
            }
            catch (Exception ex)
            {
                return ex.Message;
            }

            try
            {
                await BeginMove(source, destination, createDestination: true);
            }
            catch (IO.MoveOperation.CopyFailedException ex)
            {
                // Nothing was removed from the source yet: drop the partial copy
                TryDeleteDirectory(destination);
                return ex.Message + " " + ex.InnerException?.Message;
            }
            catch (IO.MoveOperation.DeleteFailedException ex)
            {
                // Copy finished but part of the source could not be deleted (usually a file in use).
                // Put everything back so the program is whole again on the original drive.
                try
                {
                    await BeginMove(destination, source, createDestination: true);
                }
                catch (Exception back)
                {
                    return ex.Message + " " + ex.InnerException?.Message + " " +
                           string.Format(R("Rec_MoveBackFailed", "Moving back also failed ({0}); the files are split between {1} and {2}."), back.Message, source, destination);
                }
                return ex.Message + " " + ex.InnerException?.Message;
            }
            catch (IO.MoveOperation.MoveFailedException ex)
            {
                return ex.Message + " " + ex.InnerException?.Message;
            }
            catch (OperationCanceledException)
            {
                // MoveOperation only cancels before it deletes anything, so the source is
                // still complete: drop the partial copy
                TryDeleteDirectory(destination);
                return CancelledMarker;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }

            // Leave a link in the original location
            if (!IOHelper.MakeLink(destination, source))
            {
                string linkError = new System.ComponentModel.Win32Exception().Message;
                try
                {
                    await BeginMove(destination, source, createDestination: true);
                }
                catch (Exception back)
                {
                    return string.Format(R("Rec_LinkFailedStranded", "Could not create the link ({0}) and could not move the folder back ({1}). Its files are now in {2}."), linkError, back.Message, destination);
                }
                return string.Format(R("Rec_LinkFailed", "Could not create the link ({0}); the folder was moved back."), linkError);
            }

            if (chkBox_originalHidden.Checked)
            {
                try
                {
                    var link = new DirectoryInfo(source);
                    link.Attributes |= FileAttributes.Hidden;
                }
                catch { }
            }

            return null;
        }

        static void TryDeleteDirectory(string path)
        {
            try
            {
                if (Directory.Exists(path)) Directory.Delete(path, true);
            }
            catch { }
        }
    }
}
