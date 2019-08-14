namespace Startup
{
    partial class Form1
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
            this.iwadGroupBox = new System.Windows.Forms.GroupBox();
            this.pwadGroupBox = new System.Windows.Forms.GroupBox();
            this.iwadTextBox = new System.Windows.Forms.TextBox();
            this.browseButton = new System.Windows.Forms.Button();
            this.pwadListBox = new System.Windows.Forms.ListBox();
            this.addButton = new System.Windows.Forms.Button();
            this.removeButton = new System.Windows.Forms.Button();
            this.launchButton = new System.Windows.Forms.Button();
            this.argsTextBox = new System.Windows.Forms.TextBox();
            this.argLabel = new System.Windows.Forms.Label();
            this.mapLabel = new System.Windows.Forms.Label();
            this.mapNumericUpDown = new System.Windows.Forms.NumericUpDown();
            this.iwadGroupBox.SuspendLayout();
            this.pwadGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.mapNumericUpDown)).BeginInit();
            this.SuspendLayout();
            // 
            // iwadGroupBox
            // 
            this.iwadGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.iwadGroupBox.Controls.Add(this.browseButton);
            this.iwadGroupBox.Controls.Add(this.iwadTextBox);
            this.iwadGroupBox.Location = new System.Drawing.Point(12, 12);
            this.iwadGroupBox.Name = "iwadGroupBox";
            this.iwadGroupBox.Size = new System.Drawing.Size(353, 52);
            this.iwadGroupBox.TabIndex = 0;
            this.iwadGroupBox.TabStop = false;
            this.iwadGroupBox.Text = "IWAD";
            // 
            // pwadGroupBox
            // 
            this.pwadGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pwadGroupBox.Controls.Add(this.removeButton);
            this.pwadGroupBox.Controls.Add(this.addButton);
            this.pwadGroupBox.Controls.Add(this.pwadListBox);
            this.pwadGroupBox.Location = new System.Drawing.Point(12, 70);
            this.pwadGroupBox.Name = "pwadGroupBox";
            this.pwadGroupBox.Size = new System.Drawing.Size(353, 293);
            this.pwadGroupBox.TabIndex = 1;
            this.pwadGroupBox.TabStop = false;
            this.pwadGroupBox.Text = "PWADs";
            // 
            // iwadTextBox
            // 
            this.iwadTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.iwadTextBox.Location = new System.Drawing.Point(6, 19);
            this.iwadTextBox.Name = "iwadTextBox";
            this.iwadTextBox.Size = new System.Drawing.Size(260, 20);
            this.iwadTextBox.TabIndex = 0;
            // 
            // browseButton
            // 
            this.browseButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.browseButton.Location = new System.Drawing.Point(272, 17);
            this.browseButton.Name = "browseButton";
            this.browseButton.Size = new System.Drawing.Size(75, 23);
            this.browseButton.TabIndex = 1;
            this.browseButton.Text = "Browse";
            this.browseButton.UseVisualStyleBackColor = true;
            this.browseButton.Click += new System.EventHandler(this.BrowseButton_Click);
            // 
            // pwadListBox
            // 
            this.pwadListBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pwadListBox.FormattingEnabled = true;
            this.pwadListBox.Location = new System.Drawing.Point(6, 19);
            this.pwadListBox.Name = "pwadListBox";
            this.pwadListBox.Size = new System.Drawing.Size(260, 264);
            this.pwadListBox.TabIndex = 0;
            // 
            // addButton
            // 
            this.addButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.addButton.Location = new System.Drawing.Point(272, 19);
            this.addButton.Name = "addButton";
            this.addButton.Size = new System.Drawing.Size(75, 23);
            this.addButton.TabIndex = 2;
            this.addButton.Text = "Add";
            this.addButton.UseVisualStyleBackColor = true;
            this.addButton.Click += new System.EventHandler(this.AddButton_Click);
            // 
            // removeButton
            // 
            this.removeButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.removeButton.Location = new System.Drawing.Point(272, 48);
            this.removeButton.Name = "removeButton";
            this.removeButton.Size = new System.Drawing.Size(75, 23);
            this.removeButton.TabIndex = 3;
            this.removeButton.Text = "Remove";
            this.removeButton.UseVisualStyleBackColor = true;
            this.removeButton.Click += new System.EventHandler(this.RemoveButton_Click);
            // 
            // launchButton
            // 
            this.launchButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.launchButton.Location = new System.Drawing.Point(188, 395);
            this.launchButton.Name = "launchButton";
            this.launchButton.Size = new System.Drawing.Size(177, 23);
            this.launchButton.TabIndex = 4;
            this.launchButton.Text = "Launch Helion";
            this.launchButton.UseVisualStyleBackColor = true;
            this.launchButton.Click += new System.EventHandler(this.LaunchButton_Click);
            // 
            // argsTextBox
            // 
            this.argsTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.argsTextBox.Location = new System.Drawing.Point(120, 369);
            this.argsTextBox.Name = "argsTextBox";
            this.argsTextBox.Size = new System.Drawing.Size(245, 20);
            this.argsTextBox.TabIndex = 5;
            // 
            // argLabel
            // 
            this.argLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.argLabel.AutoSize = true;
            this.argLabel.Location = new System.Drawing.Point(9, 372);
            this.argLabel.Name = "argLabel";
            this.argLabel.Size = new System.Drawing.Size(105, 13);
            this.argLabel.TabIndex = 6;
            this.argLabel.Text = "Additional arguments";
            // 
            // mapLabel
            // 
            this.mapLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.mapLabel.AutoSize = true;
            this.mapLabel.Location = new System.Drawing.Point(9, 400);
            this.mapLabel.Name = "mapLabel";
            this.mapLabel.Size = new System.Drawing.Size(28, 13);
            this.mapLabel.TabIndex = 7;
            this.mapLabel.Text = "Map";
            // 
            // mapNumericUpDown
            // 
            this.mapNumericUpDown.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.mapNumericUpDown.Location = new System.Drawing.Point(43, 398);
            this.mapNumericUpDown.Name = "mapNumericUpDown";
            this.mapNumericUpDown.Size = new System.Drawing.Size(44, 20);
            this.mapNumericUpDown.TabIndex = 8;
            this.mapNumericUpDown.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(377, 430);
            this.Controls.Add(this.mapNumericUpDown);
            this.Controls.Add(this.mapLabel);
            this.Controls.Add(this.argLabel);
            this.Controls.Add(this.argsTextBox);
            this.Controls.Add(this.launchButton);
            this.Controls.Add(this.pwadGroupBox);
            this.Controls.Add(this.iwadGroupBox);
            this.Name = "Form1";
            this.Text = "Form1";
            this.iwadGroupBox.ResumeLayout(false);
            this.iwadGroupBox.PerformLayout();
            this.pwadGroupBox.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.mapNumericUpDown)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox iwadGroupBox;
        private System.Windows.Forms.Button browseButton;
        private System.Windows.Forms.TextBox iwadTextBox;
        private System.Windows.Forms.GroupBox pwadGroupBox;
        private System.Windows.Forms.Button removeButton;
        private System.Windows.Forms.Button addButton;
        private System.Windows.Forms.ListBox pwadListBox;
        private System.Windows.Forms.Button launchButton;
        private System.Windows.Forms.TextBox argsTextBox;
        private System.Windows.Forms.Label argLabel;
        private System.Windows.Forms.Label mapLabel;
        private System.Windows.Forms.NumericUpDown mapNumericUpDown;
    }
}

