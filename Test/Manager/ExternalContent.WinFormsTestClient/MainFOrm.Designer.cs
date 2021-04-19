namespace CMI.Manager.ExternalContent.WinFormsTestClient
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
            this.lblArchiveRecordId = new System.Windows.Forms.Label();
            this.txtResult = new System.Windows.Forms.TextBox();
            this.cmdGetDigitizationData = new System.Windows.Forms.Button();
            this.txtArchiveRecordId = new System.Windows.Forms.ComboBox();
            this.cmdTestIsUsageCopy = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // lblArchiveRecordId
            // 
            this.lblArchiveRecordId.AutoSize = true;
            this.lblArchiveRecordId.Location = new System.Drawing.Point(9, 12);
            this.lblArchiveRecordId.Name = "lblArchiveRecordId";
            this.lblArchiveRecordId.Size = new System.Drawing.Size(122, 17);
            this.lblArchiveRecordId.TabIndex = 0;
            this.lblArchiveRecordId.Text = "Archive-Record-Id";
            // 
            // txtResult
            // 
            this.txtResult.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtResult.Location = new System.Drawing.Point(12, 37);
            this.txtResult.Multiline = true;
            this.txtResult.Name = "txtResult";
            this.txtResult.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtResult.Size = new System.Drawing.Size(597, 656);
            this.txtResult.TabIndex = 2;
            // 
            // cmdGetDigitizationData
            // 
            this.cmdGetDigitizationData.Location = new System.Drawing.Point(347, 9);
            this.cmdGetDigitizationData.Name = "cmdGetDigitizationData";
            this.cmdGetDigitizationData.Size = new System.Drawing.Size(75, 23);
            this.cmdGetDigitizationData.TabIndex = 3;
            this.cmdGetDigitizationData.Text = "Get Data";
            this.cmdGetDigitizationData.UseVisualStyleBackColor = true;
            this.cmdGetDigitizationData.Click += new System.EventHandler(this.cmdGetDigitizationData_Click);
            // 
            // txtArchiveRecordId
            // 
            this.txtArchiveRecordId.FormattingEnabled = true;
            this.txtArchiveRecordId.Items.AddRange(new object[] {
            "9878095 (Dossier)",
            "557199 (Dokument)",
            "1422597 (Sub-Dossier mit vielen Behältnissen)",
            "30677669 (Dokument mit vielen Behältnissen)"});
            this.txtArchiveRecordId.Location = new System.Drawing.Point(137, 9);
            this.txtArchiveRecordId.Name = "txtArchiveRecordId";
            this.txtArchiveRecordId.Size = new System.Drawing.Size(204, 24);
            this.txtArchiveRecordId.TabIndex = 4;
            // 
            // cmdTestIsUsageCopy
            // 
            this.cmdTestIsUsageCopy.Location = new System.Drawing.Point(521, 8);
            this.cmdTestIsUsageCopy.Name = "cmdTestIsUsageCopy";
            this.cmdTestIsUsageCopy.Size = new System.Drawing.Size(75, 23);
            this.cmdTestIsUsageCopy.TabIndex = 5;
            this.cmdTestIsUsageCopy.Text = "Test ";
            this.cmdTestIsUsageCopy.UseVisualStyleBackColor = true;
            this.cmdTestIsUsageCopy.Click += new System.EventHandler(this.cmdTestIsUsageCopy_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(621, 705);
            this.Controls.Add(this.cmdTestIsUsageCopy);
            this.Controls.Add(this.txtArchiveRecordId);
            this.Controls.Add(this.cmdGetDigitizationData);
            this.Controls.Add(this.txtResult);
            this.Controls.Add(this.lblArchiveRecordId);
            this.Name = "MainForm";
            this.Text = "External Content Manager Test Client";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblArchiveRecordId;
        private System.Windows.Forms.TextBox txtResult;
        private System.Windows.Forms.Button cmdGetDigitizationData;
        private System.Windows.Forms.ComboBox txtArchiveRecordId;
        private System.Windows.Forms.Button cmdTestIsUsageCopy;
    }
}

