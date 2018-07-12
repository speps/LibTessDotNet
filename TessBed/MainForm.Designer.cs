﻿namespace TessBed
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.statusMain = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStrip = new System.Windows.Forms.ToolStrip();
            this.toolStripLabelAsset = new System.Windows.Forms.ToolStripLabel();
            this.toolStripAssets = new System.Windows.Forms.ToolStripComboBox();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripLabelWinding = new System.Windows.Forms.ToolStripLabel();
            this.toolStripWinding = new System.Windows.Forms.ToolStripComboBox();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripLabelPolySize = new System.Windows.Forms.ToolStripLabel();
            this.toolStripPolySize = new System.Windows.Forms.ToolStripTextBox();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripButtonShowInput = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonShowWinding = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonNoEmpty = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonBench = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonFile = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonFolder = new System.Windows.Forms.ToolStripButton();
            this.panel = new System.Windows.Forms.Panel();
            this.statusStrip.SuspendLayout();
            this.toolStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // statusStrip
            // 
            this.statusStrip.ImageScalingSize = new System.Drawing.Size(32, 32);
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.statusMain});
            this.statusStrip.Location = new System.Drawing.Point(0, 1059);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Padding = new System.Windows.Forms.Padding(2, 0, 28, 0);
            this.statusStrip.Size = new System.Drawing.Size(1743, 22);
            this.statusStrip.TabIndex = 1;
            this.statusStrip.Text = "statusStrip1";
            // 
            // statusMain
            // 
            this.statusMain.Name = "statusMain";
            this.statusMain.Size = new System.Drawing.Size(0, 17);
            // 
            // toolStrip
            // 
            this.toolStrip.ImageScalingSize = new System.Drawing.Size(32, 32);
            this.toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripLabelAsset,
            this.toolStripAssets,
            this.toolStripSeparator1,
            this.toolStripLabelWinding,
            this.toolStripWinding,
            this.toolStripSeparator2,
            this.toolStripLabelPolySize,
            this.toolStripPolySize,
            this.toolStripSeparator3,
            this.toolStripButtonShowInput,
            this.toolStripButtonShowWinding,
            this.toolStripButtonNoEmpty,
            this.toolStripButtonBench,
            this.toolStripButtonFile,
            this.toolStripButtonFolder});
            this.toolStrip.Location = new System.Drawing.Point(0, 0);
            this.toolStrip.Name = "toolStrip";
            this.toolStrip.Padding = new System.Windows.Forms.Padding(0, 0, 2, 0);
            this.toolStrip.Size = new System.Drawing.Size(1743, 40);
            this.toolStrip.TabIndex = 0;
            // 
            // toolStripLabelAsset
            // 
            this.toolStripLabelAsset.Name = "toolStripLabelAsset";
            this.toolStripLabelAsset.Size = new System.Drawing.Size(71, 37);
            this.toolStripLabelAsset.Text = "Asset";
            // 
            // toolStripAssets
            // 
            this.toolStripAssets.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.toolStripAssets.Name = "toolStripAssets";
            this.toolStripAssets.Size = new System.Drawing.Size(238, 40);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 40);
            // 
            // toolStripLabelWinding
            // 
            this.toolStripLabelWinding.Name = "toolStripLabelWinding";
            this.toolStripLabelWinding.Size = new System.Drawing.Size(105, 37);
            this.toolStripLabelWinding.Text = "Winding";
            // 
            // toolStripWinding
            // 
            this.toolStripWinding.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.toolStripWinding.Name = "toolStripWinding";
            this.toolStripWinding.Size = new System.Drawing.Size(238, 40);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(6, 40);
            // 
            // toolStripLabelPolySize
            // 
            this.toolStripLabelPolySize.Name = "toolStripLabelPolySize";
            this.toolStripLabelPolySize.Size = new System.Drawing.Size(97, 37);
            this.toolStripLabelPolySize.Text = "Vertices";
            // 
            // toolStripPolySize
            // 
            this.toolStripPolySize.Name = "toolStripPolySize";
            this.toolStripPolySize.Size = new System.Drawing.Size(76, 40);
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(6, 40);
            // 
            // toolStripButtonShowInput
            // 
            this.toolStripButtonShowInput.CheckOnClick = true;
            this.toolStripButtonShowInput.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripButtonShowInput.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonShowInput.Image")));
            this.toolStripButtonShowInput.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonShowInput.Name = "toolStripButtonShowInput";
            this.toolStripButtonShowInput.Size = new System.Drawing.Size(140, 37);
            this.toolStripButtonShowInput.Text = "Show input";
            this.toolStripButtonShowInput.ToolTipText = "Show input polygon";
            // 
            // toolStripButtonShowWinding
            // 
            this.toolStripButtonShowWinding.CheckOnClick = true;
            this.toolStripButtonShowWinding.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripButtonShowWinding.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonShowWinding.Image")));
            this.toolStripButtonShowWinding.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonShowWinding.Name = "toolStripButtonShowWinding";
            this.toolStripButtonShowWinding.Size = new System.Drawing.Size(169, 37);
            this.toolStripButtonShowWinding.Text = "Show winding";
            // 
            // toolStripButtonNoEmpty
            // 
            this.toolStripButtonNoEmpty.CheckOnClick = true;
            this.toolStripButtonNoEmpty.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripButtonNoEmpty.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonNoEmpty.Name = "toolStripButtonNoEmpty";
            this.toolStripButtonNoEmpty.Size = new System.Drawing.Size(126, 37);
            this.toolStripButtonNoEmpty.Text = "No empty";
            this.toolStripButtonNoEmpty.ToolTipText = "Remove empty polygons";
            // 
            // toolStripButtonBench
            // 
            this.toolStripButtonBench.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripButtonBench.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonBench.Image")));
            this.toolStripButtonBench.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonBench.Name = "toolStripButtonBench";
            this.toolStripButtonBench.Size = new System.Drawing.Size(148, 37);
            this.toolStripButtonBench.Text = "Benchmarks";
            // 
            // toolStripButtonFile
            // 
            this.toolStripButtonFile.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripButtonFile.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonFile.Name = "toolStripButtonFile";
            this.toolStripButtonFile.Size = new System.Drawing.Size(56, 37);
            this.toolStripButtonFile.Text = "File";
            this.toolStripButtonFile.ToolTipText = "Open a .dat file";
            // 
            // toolStripButtonFolder
            // 
            this.toolStripButtonFolder.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripButtonFolder.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonFolder.Name = "toolStripButtonFolder";
            this.toolStripButtonFolder.Size = new System.Drawing.Size(86, 37);
            this.toolStripButtonFolder.Text = "Folder";
            // 
            // panel
            // 
            this.panel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel.Location = new System.Drawing.Point(0, 40);
            this.panel.Margin = new System.Windows.Forms.Padding(6);
            this.panel.Name = "panel";
            this.panel.Size = new System.Drawing.Size(1743, 1019);
            this.panel.TabIndex = 2;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.ClientSize = new System.Drawing.Size(1743, 1081);
            this.Controls.Add(this.panel);
            this.Controls.Add(this.toolStrip);
            this.Controls.Add(this.statusStrip);
            this.Margin = new System.Windows.Forms.Padding(6);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "LibTessDotNet - TessBed";
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.toolStrip.ResumeLayout(false);
            this.toolStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel statusMain;
        private System.Windows.Forms.ToolStrip toolStrip;
        private System.Windows.Forms.ToolStripComboBox toolStripAssets;
        private System.Windows.Forms.Panel panel;
        private System.Windows.Forms.ToolStripLabel toolStripLabelAsset;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripLabel toolStripLabelWinding;
        private System.Windows.Forms.ToolStripComboBox toolStripWinding;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripLabel toolStripLabelPolySize;
        private System.Windows.Forms.ToolStripTextBox toolStripPolySize;
        private System.Windows.Forms.ToolStripButton toolStripButtonShowInput;
        private System.Windows.Forms.ToolStripButton toolStripButtonShowWinding;
        private System.Windows.Forms.ToolStripButton toolStripButtonNoEmpty;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private System.Windows.Forms.ToolStripButton toolStripButtonBench;
        private System.Windows.Forms.ToolStripButton toolStripButtonFile;
        private System.Windows.Forms.ToolStripButton toolStripButtonFolder;
    }
}

