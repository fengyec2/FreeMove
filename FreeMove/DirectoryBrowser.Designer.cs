namespace FreeMove
{
    partial class DirectoryBrowser
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.splitContainer_Main = new System.Windows.Forms.SplitContainer();
            this.treeView_Dirs = new System.Windows.Forms.TreeView();
            this.contextMenu_TreeView = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.setAsSourceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.setAsTargetToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.listView_Files = new System.Windows.Forms.ListView();
            this.contextMenu_ListView = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.setAsSourceListViewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.setAsTargetListViewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.panel_Buttons = new System.Windows.Forms.Panel();
            this.button_SetSource = new System.Windows.Forms.Button();
            this.button_SetTarget = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer_Main)).BeginInit();
            this.splitContainer_Main.Panel1.SuspendLayout();
            this.splitContainer_Main.Panel2.SuspendLayout();
            this.splitContainer_Main.SuspendLayout();
            this.contextMenu_TreeView.SuspendLayout();
            this.contextMenu_ListView.SuspendLayout();
            this.panel_Buttons.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer_Main
            // 
            this.splitContainer_Main.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer_Main.Location = new System.Drawing.Point(0, 0);
            this.splitContainer_Main.Name = "splitContainer_Main";
            // 
            // splitContainer_Main.Panel1
            // 
            this.splitContainer_Main.Panel1.Controls.Add(this.treeView_Dirs);
            // 
            // splitContainer_Main.Panel2
            // 
            this.splitContainer_Main.Panel2.Controls.Add(this.listView_Files);
            this.splitContainer_Main.Panel2.Controls.Add(this.panel_Buttons);
            this.splitContainer_Main.Size = new System.Drawing.Size(565, 260);
            this.splitContainer_Main.SplitterDistance = 180;
            this.splitContainer_Main.TabIndex = 0;
            // 
            // treeView_Dirs
            // 
            this.treeView_Dirs.ContextMenuStrip = this.contextMenu_TreeView;
            this.treeView_Dirs.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeView_Dirs.Location = new System.Drawing.Point(0, 0);
            this.treeView_Dirs.Name = "treeView_Dirs";
            this.treeView_Dirs.Size = new System.Drawing.Size(180, 260);
            this.treeView_Dirs.TabIndex = 0;
            this.treeView_Dirs.BeforeExpand += new System.Windows.Forms.TreeViewCancelEventHandler(this.treeView_Dirs_BeforeExpand);
            this.treeView_Dirs.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treeView_Dirs_AfterSelect);
            // 
            // contextMenu_TreeView
            // 
            this.contextMenu_TreeView.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.setAsSourceToolStripMenuItem,
            this.setAsTargetToolStripMenuItem});
            this.contextMenu_TreeView.Name = "contextMenu_TreeView";
            this.contextMenu_TreeView.Size = new System.Drawing.Size(147, 48);
            // 
            // setAsSourceToolStripMenuItem
            // 
            this.setAsSourceToolStripMenuItem.Name = "setAsSourceToolStripMenuItem";
            this.setAsSourceToolStripMenuItem.Size = new System.Drawing.Size(146, 22);
            this.setAsSourceToolStripMenuItem.Text = "Set as Source";
            this.setAsSourceToolStripMenuItem.Click += new System.EventHandler(this.setAsSourceToolStripMenuItem_Click);
            // 
            // setAsTargetToolStripMenuItem
            // 
            this.setAsTargetToolStripMenuItem.Name = "setAsTargetToolStripMenuItem";
            this.setAsTargetToolStripMenuItem.Size = new System.Drawing.Size(146, 22);
            this.setAsTargetToolStripMenuItem.Text = "Set as Target";
            this.setAsTargetToolStripMenuItem.Click += new System.EventHandler(this.setAsTargetToolStripMenuItem_Click);
            // 
            // listView_Files
            // 
            this.listView_Files.ContextMenuStrip = this.contextMenu_ListView;
            this.listView_Files.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listView_Files.FullRowSelect = true;
            this.listView_Files.HideSelection = false;
            this.listView_Files.Location = new System.Drawing.Point(0, 0);
            this.listView_Files.MultiSelect = false;
            this.listView_Files.Name = "listView_Files";
            this.listView_Files.Size = new System.Drawing.Size(381, 230);
            this.listView_Files.TabIndex = 0;
            this.listView_Files.UseCompatibleStateImageBehavior = false;
            this.listView_Files.View = System.Windows.Forms.View.Details;
            // 
            // contextMenu_ListView
            // 
            this.contextMenu_ListView.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.setAsSourceListViewToolStripMenuItem,
            this.setAsTargetListViewToolStripMenuItem});
            this.contextMenu_ListView.Name = "contextMenu_ListView";
            this.contextMenu_ListView.Size = new System.Drawing.Size(147, 48);
            // 
            // setAsSourceListViewToolStripMenuItem
            // 
            this.setAsSourceListViewToolStripMenuItem.Name = "setAsSourceListViewToolStripMenuItem";
            this.setAsSourceListViewToolStripMenuItem.Size = new System.Drawing.Size(146, 22);
            this.setAsSourceListViewToolStripMenuItem.Text = "Set as Source";
            this.setAsSourceListViewToolStripMenuItem.Click += new System.EventHandler(this.setAsSourceListViewToolStripMenuItem_Click);
            // 
            // setAsTargetListViewToolStripMenuItem
            // 
            this.setAsTargetListViewToolStripMenuItem.Name = "setAsTargetListViewToolStripMenuItem";
            this.setAsTargetListViewToolStripMenuItem.Size = new System.Drawing.Size(146, 22);
            this.setAsTargetListViewToolStripMenuItem.Text = "Set as Target";
            this.setAsTargetListViewToolStripMenuItem.Click += new System.EventHandler(this.setAsTargetListViewToolStripMenuItem_Click);
            // 
            // panel_Buttons
            // 
            this.panel_Buttons.Controls.Add(this.button_SetSource);
            this.panel_Buttons.Controls.Add(this.button_SetTarget);
            this.panel_Buttons.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel_Buttons.Location = new System.Drawing.Point(0, 230);
            this.panel_Buttons.Name = "panel_Buttons";
            this.panel_Buttons.Size = new System.Drawing.Size(381, 30);
            this.panel_Buttons.TabIndex = 1;
            // 
            // button_SetSource
            // 
            this.button_SetSource.Location = new System.Drawing.Point(3, 4);
            this.button_SetSource.Name = "button_SetSource";
            this.button_SetSource.Size = new System.Drawing.Size(120, 23);
            this.button_SetSource.TabIndex = 0;
            this.button_SetSource.Text = "← Set Source";
            this.button_SetSource.UseVisualStyleBackColor = true;
            this.button_SetSource.Click += new System.EventHandler(this.button_SetSource_Click);
            // 
            // button_SetTarget
            // 
            this.button_SetTarget.Location = new System.Drawing.Point(129, 4);
            this.button_SetTarget.Name = "button_SetTarget";
            this.button_SetTarget.Size = new System.Drawing.Size(120, 23);
            this.button_SetTarget.TabIndex = 1;
            this.button_SetTarget.Text = "→ Set Target";
            this.button_SetTarget.UseVisualStyleBackColor = true;
            this.button_SetTarget.Click += new System.EventHandler(this.button_SetTarget_Click);
            // 
            // DirectoryBrowser
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitContainer_Main);
            this.Name = "DirectoryBrowser";
            this.Size = new System.Drawing.Size(565, 260);
            this.splitContainer_Main.Panel1.ResumeLayout(false);
            this.splitContainer_Main.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer_Main)).EndInit();
            this.splitContainer_Main.ResumeLayout(false);
            this.contextMenu_TreeView.ResumeLayout(false);
            this.contextMenu_ListView.ResumeLayout(false);
            this.panel_Buttons.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer_Main;
        private System.Windows.Forms.TreeView treeView_Dirs;
        private System.Windows.Forms.ListView listView_Files;
        private System.Windows.Forms.Panel panel_Buttons;
        private System.Windows.Forms.Button button_SetSource;
        private System.Windows.Forms.Button button_SetTarget;
        private System.Windows.Forms.ContextMenuStrip contextMenu_TreeView;
        private System.Windows.Forms.ToolStripMenuItem setAsSourceToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem setAsTargetToolStripMenuItem;
        private System.Windows.Forms.ContextMenuStrip contextMenu_ListView;
        private System.Windows.Forms.ToolStripMenuItem setAsSourceListViewToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem setAsTargetListViewToolStripMenuItem;
    }
}
