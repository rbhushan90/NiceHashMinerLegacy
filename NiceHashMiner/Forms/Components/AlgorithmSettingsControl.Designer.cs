﻿namespace NiceHashMiner.Forms.Components {
    partial class AlgorithmSettingsControl {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.groupBoxSelectedAlgorithmSettings = new System.Windows.Forms.GroupBox();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.field_PowerUsage = new NiceHashMiner.Forms.Components.Field();
            this.fieldBoxBenchmarkSpeed = new NiceHashMiner.Forms.Components.Field();
            this.secondaryFieldBoxBenchmarkSpeed = new NiceHashMiner.Forms.Components.Field();
            this.richTextBoxExtraLaunchParameters = new System.Windows.Forms.RichTextBox();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.groupBoxExtraLaunchParameters = new System.Windows.Forms.GroupBox();
            this.groupBoxSelectedAlgorithmSettings.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.groupBoxExtraLaunchParameters.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBoxSelectedAlgorithmSettings
            // 
            this.groupBoxSelectedAlgorithmSettings.Controls.Add(this.flowLayoutPanel1);
            this.groupBoxSelectedAlgorithmSettings.Location = new System.Drawing.Point(3, 3);
            this.groupBoxSelectedAlgorithmSettings.Name = "groupBoxSelectedAlgorithmSettings";
            this.groupBoxSelectedAlgorithmSettings.Size = new System.Drawing.Size(282, 217);
            this.groupBoxSelectedAlgorithmSettings.TabIndex = 11;
            this.groupBoxSelectedAlgorithmSettings.TabStop = false;
            this.groupBoxSelectedAlgorithmSettings.Text = "Selected Algorithm Settings:";
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Controls.Add(this.field_PowerUsage);
            this.flowLayoutPanel1.Controls.Add(this.fieldBoxBenchmarkSpeed);
            this.flowLayoutPanel1.Controls.Add(this.secondaryFieldBoxBenchmarkSpeed);
            this.flowLayoutPanel1.Controls.Add(this.groupBoxExtraLaunchParameters);
            this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanel1.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(3, 16);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(276, 198);
            this.flowLayoutPanel1.TabIndex = 12;
            // 
            // field_PowerUsage
            // 
            this.field_PowerUsage.AutoSize = true;
            this.field_PowerUsage.BackColor = System.Drawing.Color.Transparent;
            this.field_PowerUsage.EntryText = "";
            this.field_PowerUsage.LabelText = "Power Usage (W):";
            this.field_PowerUsage.Location = new System.Drawing.Point(3, 3);
            this.field_PowerUsage.Name = "field_PowerUsage";
            this.field_PowerUsage.Size = new System.Drawing.Size(278, 26);
            this.field_PowerUsage.TabIndex = 15;
            // 
            // fieldBoxBenchmarkSpeed
            // 
            this.fieldBoxBenchmarkSpeed.AutoSize = true;
            this.fieldBoxBenchmarkSpeed.BackColor = System.Drawing.Color.Transparent;
            this.fieldBoxBenchmarkSpeed.EntryText = "";
            this.fieldBoxBenchmarkSpeed.LabelText = "Benchmark Speed (H/s):";
            this.fieldBoxBenchmarkSpeed.Location = new System.Drawing.Point(3, 35);
            this.fieldBoxBenchmarkSpeed.Name = "fieldBoxBenchmarkSpeed";
            this.fieldBoxBenchmarkSpeed.Size = new System.Drawing.Size(278, 26);
            this.fieldBoxBenchmarkSpeed.TabIndex = 1;
            // 
            // secondaryFieldBoxBenchmarkSpeed
            // 
            this.secondaryFieldBoxBenchmarkSpeed.AutoSize = true;
            this.secondaryFieldBoxBenchmarkSpeed.BackColor = System.Drawing.Color.Transparent;
            this.secondaryFieldBoxBenchmarkSpeed.EntryText = "";
            this.secondaryFieldBoxBenchmarkSpeed.LabelText = "Secondary Speed (H/s):";
            this.secondaryFieldBoxBenchmarkSpeed.Location = new System.Drawing.Point(3, 67);
            this.secondaryFieldBoxBenchmarkSpeed.Name = "secondaryFieldBoxBenchmarkSpeed";
            this.secondaryFieldBoxBenchmarkSpeed.Size = new System.Drawing.Size(278, 26);
            this.secondaryFieldBoxBenchmarkSpeed.TabIndex = 16;
            // 
            // richTextBoxExtraLaunchParameters
            // 
            this.richTextBoxExtraLaunchParameters.Location = new System.Drawing.Point(4, 24);
            this.richTextBoxExtraLaunchParameters.Name = "richTextBoxExtraLaunchParameters";
            this.richTextBoxExtraLaunchParameters.Size = new System.Drawing.Size(256, 55);
            this.richTextBoxExtraLaunchParameters.TabIndex = 0;
            this.richTextBoxExtraLaunchParameters.Text = "";
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = global::NiceHashMiner.Properties.Resources.info_black_18;
            this.pictureBox1.Location = new System.Drawing.Point(255, 0);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(18, 18);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.pictureBox1.TabIndex = 1;
            this.pictureBox1.TabStop = false;
            // 
            // groupBoxExtraLaunchParameters
            // 
            this.groupBoxExtraLaunchParameters.Controls.Add(this.pictureBox1);
            this.groupBoxExtraLaunchParameters.Controls.Add(this.richTextBoxExtraLaunchParameters);
            this.groupBoxExtraLaunchParameters.Location = new System.Drawing.Point(3, 99);
            this.groupBoxExtraLaunchParameters.Name = "groupBoxExtraLaunchParameters";
            this.groupBoxExtraLaunchParameters.Size = new System.Drawing.Size(270, 84);
            this.groupBoxExtraLaunchParameters.TabIndex = 14;
            this.groupBoxExtraLaunchParameters.TabStop = false;
            this.groupBoxExtraLaunchParameters.Text = "Extra Launch Parameters:";
            // 
            // AlgorithmSettingsControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.groupBoxSelectedAlgorithmSettings);
            this.Name = "AlgorithmSettingsControl";
            this.Size = new System.Drawing.Size(285, 220);
            this.groupBoxSelectedAlgorithmSettings.ResumeLayout(false);
            this.flowLayoutPanel1.ResumeLayout(false);
            this.flowLayoutPanel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.groupBoxExtraLaunchParameters.ResumeLayout(false);
            this.groupBoxExtraLaunchParameters.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBoxSelectedAlgorithmSettings;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private Field fieldBoxBenchmarkSpeed;
        private Field secondaryFieldBoxBenchmarkSpeed;
        private Field field_PowerUsage;
        private System.Windows.Forms.GroupBox groupBoxExtraLaunchParameters;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.RichTextBox richTextBoxExtraLaunchParameters;
    }
}
