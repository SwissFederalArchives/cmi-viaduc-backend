namespace CMI.Tools.JsonCombiner
{
    partial class MainWindow
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainWindow));
            this.TxtPathMaster = new System.Windows.Forms.TextBox();
            this.TxtPathSource = new System.Windows.Forms.TextBox();
            this.LableTitleMaster = new System.Windows.Forms.Label();
            this.LableSourceFileTitle = new System.Windows.Forms.Label();
            this.DropBoxLanguageSource = new System.Windows.Forms.ComboBox();
            this.LableDropBoxMenueTitle = new System.Windows.Forms.Label();
            this.OpenFileSystemMaster = new System.Windows.Forms.Button();
            this.OpenFileSystemSoruce = new System.Windows.Forms.Button();
            this.LableOutput = new System.Windows.Forms.Label();
            this.FileDialog = new System.Windows.Forms.OpenFileDialog();
            this.btnStart = new System.Windows.Forms.Button();
            this.outputBox = new System.Windows.Forms.RichTextBox();
            this.outputLink = new System.Windows.Forms.Label();
            this.fileSystemWatcher1 = new System.IO.FileSystemWatcher();
            this.saveFileDialog = new System.Windows.Forms.SaveFileDialog();
            this.BtnSaveAsFile = new System.Windows.Forms.Button();
            this.BtnCopyAndSelect = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.fileSystemWatcher1)).BeginInit();
            this.SuspendLayout();
            // 
            // TxtPathMaster
            // 
            resources.ApplyResources(this.TxtPathMaster, "TxtPathMaster");
            this.TxtPathMaster.Name = "TxtPathMaster";
            // 
            // TxtPathSource
            // 
            resources.ApplyResources(this.TxtPathSource, "TxtPathSource");
            this.TxtPathSource.Name = "TxtPathSource";
            // 
            // LableTitleMaster
            // 
            resources.ApplyResources(this.LableTitleMaster, "LableTitleMaster");
            this.LableTitleMaster.Name = "LableTitleMaster";
            // 
            // LableSourceFileTitle
            // 
            resources.ApplyResources(this.LableSourceFileTitle, "LableSourceFileTitle");
            this.LableSourceFileTitle.Name = "LableSourceFileTitle";
            // 
            // DropBoxLanguageSource
            // 
            resources.ApplyResources(this.DropBoxLanguageSource, "DropBoxLanguageSource");
            this.DropBoxLanguageSource.FormattingEnabled = true;
            this.DropBoxLanguageSource.Name = "DropBoxLanguageSource";
            // 
            // LableDropBoxMenueTitle
            // 
            resources.ApplyResources(this.LableDropBoxMenueTitle, "LableDropBoxMenueTitle");
            this.LableDropBoxMenueTitle.Name = "LableDropBoxMenueTitle";
            // 
            // OpenFileSystemMaster
            // 
            resources.ApplyResources(this.OpenFileSystemMaster, "OpenFileSystemMaster");
            this.OpenFileSystemMaster.Name = "OpenFileSystemMaster";
            this.OpenFileSystemMaster.UseVisualStyleBackColor = true;
            this.OpenFileSystemMaster.Click += new System.EventHandler(this.BtnOpenFileSystemMaster_Click);
            // 
            // OpenFileSystemSoruce
            // 
            resources.ApplyResources(this.OpenFileSystemSoruce, "OpenFileSystemSoruce");
            this.OpenFileSystemSoruce.Name = "OpenFileSystemSoruce";
            this.OpenFileSystemSoruce.UseVisualStyleBackColor = true;
            this.OpenFileSystemSoruce.Click += new System.EventHandler(this.BtnOpenFileSystemSoruce_Click);
            // 
            // LableOutput
            // 
            resources.ApplyResources(this.LableOutput, "LableOutput");
            this.LableOutput.Name = "LableOutput";
            // 
            // FileDialog
            // 
            this.FileDialog.FileName = "Open Master File Path Dialog";
            // 
            // btnStart
            // 
            resources.ApplyResources(this.btnStart, "btnStart");
            this.btnStart.Name = "btnStart";
            this.btnStart.UseVisualStyleBackColor = true;
            this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
            // 
            // outputBox
            // 
            this.outputBox.AcceptsTab = true;
            resources.ApplyResources(this.outputBox, "outputBox");
            this.outputBox.BackColor = System.Drawing.SystemColors.Window;
            this.outputBox.ForeColor = System.Drawing.SystemColors.WindowText;
            this.outputBox.Name = "outputBox";
            // 
            // outputLink
            // 
            resources.ApplyResources(this.outputLink, "outputLink");
            this.outputLink.Name = "outputLink";
            // 
            // fileSystemWatcher1
            // 
            this.fileSystemWatcher1.EnableRaisingEvents = true;
            this.fileSystemWatcher1.SynchronizingObject = this;
            // 
            // BtnSaveAsFile
            // 
            resources.ApplyResources(this.BtnSaveAsFile, "BtnSaveAsFile");
            this.BtnSaveAsFile.Name = "BtnSaveAsFile";
            this.BtnSaveAsFile.UseVisualStyleBackColor = true;
            this.BtnSaveAsFile.Click += new System.EventHandler(this.BtnSaveAsFile_Click);
            // 
            // BtnCopyAndSelect
            // 
            resources.ApplyResources(this.BtnCopyAndSelect, "BtnCopyAndSelect");
            this.BtnCopyAndSelect.Name = "BtnCopyAndSelect";
            this.BtnCopyAndSelect.UseVisualStyleBackColor = true;
            this.BtnCopyAndSelect.Click += new System.EventHandler(this.BtnCopyAndSelect_Click);
            // 
            // MainWindow
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.HighlightText;
            this.Controls.Add(this.BtnCopyAndSelect);
            this.Controls.Add(this.BtnSaveAsFile);
            this.Controls.Add(this.outputLink);
            this.Controls.Add(this.outputBox);
            this.Controls.Add(this.btnStart);
            this.Controls.Add(this.LableOutput);
            this.Controls.Add(this.OpenFileSystemSoruce);
            this.Controls.Add(this.OpenFileSystemMaster);
            this.Controls.Add(this.LableDropBoxMenueTitle);
            this.Controls.Add(this.DropBoxLanguageSource);
            this.Controls.Add(this.LableSourceFileTitle);
            this.Controls.Add(this.LableTitleMaster);
            this.Controls.Add(this.TxtPathSource);
            this.Controls.Add(this.TxtPathMaster);
            this.ForeColor = System.Drawing.SystemColors.WindowText;
            this.Name = "MainWindow";
            this.Load += new System.EventHandler(this.MainWindow_Load);
            ((System.ComponentModel.ISupportInitialize)(this.fileSystemWatcher1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox TxtPathMaster;
        private System.Windows.Forms.TextBox TxtPathSource;
        private System.Windows.Forms.Label LableTitleMaster;
        private System.Windows.Forms.Label LableSourceFileTitle;
        private System.Windows.Forms.ComboBox DropBoxLanguageSource;
        private System.Windows.Forms.Label LableDropBoxMenueTitle;
        private System.Windows.Forms.Button OpenFileSystemMaster;
        private System.Windows.Forms.Button OpenFileSystemSoruce;
        private System.Windows.Forms.Label LableOutput;
        private System.Windows.Forms.OpenFileDialog FileDialog;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.RichTextBox outputBox;
        private System.Windows.Forms.Label outputLink;
        private System.IO.FileSystemWatcher fileSystemWatcher1;
        private System.Windows.Forms.SaveFileDialog saveFileDialog;
        private System.Windows.Forms.Button BtnSaveAsFile;
        private System.Windows.Forms.Button BtnCopyAndSelect;
    }
}

