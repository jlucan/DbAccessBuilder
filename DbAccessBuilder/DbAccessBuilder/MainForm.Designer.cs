namespace DbAccessBuilder
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
            this.label1 = new System.Windows.Forms.Label();
            this.txtXmlIn = new System.Windows.Forms.TextBox();
            this.btnXmlBrowse = new System.Windows.Forms.Button();
            this.btnOutputBrowse = new System.Windows.Forms.Button();
            this.txtOutputFile = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            this.txtSqlSvr = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.txtDatabaseName = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.txtProcStartsWith = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.btnDoPhase = new System.Windows.Forms.Button();
            this.panel1 = new System.Windows.Forms.Panel();
            this.rBtnPhase3 = new System.Windows.Forms.RadioButton();
            this.rBtnPhase2 = new System.Windows.Forms.RadioButton();
            this.rBtnPhase1 = new System.Windows.Forms.RadioButton();
            this.label6 = new System.Windows.Forms.Label();
            this.btnExit = new System.Windows.Forms.Button();
            this.warningMsgs = new System.Windows.Forms.TextBox();
            this.lblWarningMsgs = new System.Windows.Forms.Label();
            this.lblErrmsgs = new System.Windows.Forms.Label();
            this.errorMsgs = new System.Windows.Forms.TextBox();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(84, 164);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(93, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Input XML File:";
            // 
            // txtXmlIn
            // 
            this.txtXmlIn.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtXmlIn.Location = new System.Drawing.Point(207, 160);
            this.txtXmlIn.Name = "txtXmlIn";
            this.txtXmlIn.Size = new System.Drawing.Size(450, 20);
            this.txtXmlIn.TabIndex = 1;
            // 
            // btnXmlBrowse
            // 
            this.btnXmlBrowse.Location = new System.Drawing.Point(675, 159);
            this.btnXmlBrowse.Name = "btnXmlBrowse";
            this.btnXmlBrowse.Size = new System.Drawing.Size(90, 23);
            this.btnXmlBrowse.TabIndex = 2;
            this.btnXmlBrowse.Text = "Browse";
            this.btnXmlBrowse.UseVisualStyleBackColor = true;
            this.btnXmlBrowse.Click += new System.EventHandler(this.btnXmlBrowse_Click);
            // 
            // btnOutputBrowse
            // 
            this.btnOutputBrowse.Location = new System.Drawing.Point(674, 199);
            this.btnOutputBrowse.Name = "btnOutputBrowse";
            this.btnOutputBrowse.Size = new System.Drawing.Size(90, 23);
            this.btnOutputBrowse.TabIndex = 5;
            this.btnOutputBrowse.Text = "Browse";
            this.btnOutputBrowse.UseVisualStyleBackColor = true;
            this.btnOutputBrowse.Click += new System.EventHandler(this.btnOutputBrowse_Click);
            // 
            // txtOutputFile
            // 
            this.txtOutputFile.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtOutputFile.Location = new System.Drawing.Point(207, 200);
            this.txtOutputFile.Name = "txtOutputFile";
            this.txtOutputFile.Size = new System.Drawing.Size(450, 20);
            this.txtOutputFile.TabIndex = 4;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(84, 204);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(73, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Output File:";
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            // 
            // txtSqlSvr
            // 
            this.txtSqlSvr.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtSqlSvr.Location = new System.Drawing.Point(207, 240);
            this.txtSqlSvr.Name = "txtSqlSvr";
            this.txtSqlSvr.Size = new System.Drawing.Size(450, 20);
            this.txtSqlSvr.TabIndex = 7;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(84, 244);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(70, 13);
            this.label3.TabIndex = 6;
            this.label3.Text = "Sql Server:";
            // 
            // txtDatabaseName
            // 
            this.txtDatabaseName.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtDatabaseName.Location = new System.Drawing.Point(207, 280);
            this.txtDatabaseName.Name = "txtDatabaseName";
            this.txtDatabaseName.Size = new System.Drawing.Size(450, 20);
            this.txtDatabaseName.TabIndex = 9;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.Location = new System.Drawing.Point(84, 285);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(65, 13);
            this.label4.TabIndex = 8;
            this.label4.Text = "Database:";
            // 
            // txtProcStartsWith
            // 
            this.txtProcStartsWith.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtProcStartsWith.Location = new System.Drawing.Point(275, 320);
            this.txtProcStartsWith.Name = "txtProcStartsWith";
            this.txtProcStartsWith.Size = new System.Drawing.Size(382, 20);
            this.txtProcStartsWith.TabIndex = 11;
            this.txtProcStartsWith.Text = "Get";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.Location = new System.Drawing.Point(84, 324);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(179, 13);
            this.label5.TabIndex = 10;
            this.label5.Text = "Process Procs that Start With:";
            // 
            // btnDoPhase
            // 
            this.btnDoPhase.Location = new System.Drawing.Point(87, 375);
            this.btnDoPhase.Name = "btnDoPhase";
            this.btnDoPhase.Size = new System.Drawing.Size(570, 37);
            this.btnDoPhase.TabIndex = 12;
            this.btnDoPhase.Text = "Gen Column Defs";
            this.btnDoPhase.UseVisualStyleBackColor = true;
            this.btnDoPhase.Click += new System.EventHandler(this.btnDoPhase_Click);
            // 
            // panel1
            // 
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel1.Controls.Add(this.rBtnPhase3);
            this.panel1.Controls.Add(this.rBtnPhase2);
            this.panel1.Controls.Add(this.rBtnPhase1);
            this.panel1.Location = new System.Drawing.Point(87, 22);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(570, 111);
            this.panel1.TabIndex = 18;
            // 
            // rBtnPhase3
            // 
            this.rBtnPhase3.AutoSize = true;
            this.rBtnPhase3.Location = new System.Drawing.Point(28, 74);
            this.rBtnPhase3.Name = "rBtnPhase3";
            this.rBtnPhase3.Size = new System.Drawing.Size(265, 17);
            this.rBtnPhase3.TabIndex = 2;
            this.rBtnPhase3.TabStop = true;
            this.rBtnPhase3.Text = "Phase 3 - Generate Sql Db Interface Code";
            this.rBtnPhase3.UseVisualStyleBackColor = true;
            this.rBtnPhase3.CheckedChanged += new System.EventHandler(this.rBtnPhase3_CheckedChanged);
            // 
            // rBtnPhase2
            // 
            this.rBtnPhase2.AutoSize = true;
            this.rBtnPhase2.Location = new System.Drawing.Point(28, 46);
            this.rBtnPhase2.Name = "rBtnPhase2";
            this.rBtnPhase2.Size = new System.Drawing.Size(239, 17);
            this.rBtnPhase2.TabIndex = 1;
            this.rBtnPhase2.TabStop = true;
            this.rBtnPhase2.Text = "Phase 2 - Generate XML Column Defs";
            this.rBtnPhase2.UseVisualStyleBackColor = true;
            this.rBtnPhase2.CheckedChanged += new System.EventHandler(this.rBtnPhase2_CheckedChanged);
            // 
            // rBtnPhase1
            // 
            this.rBtnPhase1.AutoSize = true;
            this.rBtnPhase1.Location = new System.Drawing.Point(28, 18);
            this.rBtnPhase1.Name = "rBtnPhase1";
            this.rBtnPhase1.Size = new System.Drawing.Size(276, 17);
            this.rBtnPhase1.TabIndex = 0;
            this.rBtnPhase1.TabStop = true;
            this.rBtnPhase1.Text = "Phase 1 - Create XML Stored Procedure List";
            this.rBtnPhase1.UseVisualStyleBackColor = true;
            this.rBtnPhase1.CheckedChanged += new System.EventHandler(this.rBtnPhase1_CheckedChanged);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(103, 15);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(92, 13);
            this.label6.TabIndex = 19;
            this.label6.Text = "Chose a Phase";
            // 
            // btnExit
            // 
            this.btnExit.Location = new System.Drawing.Point(675, 382);
            this.btnExit.Name = "btnExit";
            this.btnExit.Size = new System.Drawing.Size(90, 23);
            this.btnExit.TabIndex = 20;
            this.btnExit.Text = "Exit";
            this.btnExit.UseVisualStyleBackColor = true;
            this.btnExit.Click += new System.EventHandler(this.btnExit_Click);
            // 
            // warningMsgs
            // 
            this.warningMsgs.BackColor = System.Drawing.SystemColors.Window;
            this.warningMsgs.Location = new System.Drawing.Point(87, 452);
            this.warningMsgs.Multiline = true;
            this.warningMsgs.Name = "warningMsgs";
            this.warningMsgs.ReadOnly = true;
            this.warningMsgs.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.warningMsgs.Size = new System.Drawing.Size(678, 101);
            this.warningMsgs.TabIndex = 21;
            // 
            // lblWarningMsgs
            // 
            this.lblWarningMsgs.AutoSize = true;
            this.lblWarningMsgs.Location = new System.Drawing.Point(91, 437);
            this.lblWarningMsgs.Name = "lblWarningMsgs";
            this.lblWarningMsgs.Size = new System.Drawing.Size(64, 13);
            this.lblWarningMsgs.TabIndex = 22;
            this.lblWarningMsgs.Text = "Warnings:";
            // 
            // lblErrmsgs
            // 
            this.lblErrmsgs.AutoSize = true;
            this.lblErrmsgs.Location = new System.Drawing.Point(94, 567);
            this.lblErrmsgs.Name = "lblErrmsgs";
            this.lblErrmsgs.Size = new System.Drawing.Size(44, 13);
            this.lblErrmsgs.TabIndex = 24;
            this.lblErrmsgs.Text = "Errors:";
            // 
            // errorMsgs
            // 
            this.errorMsgs.BackColor = System.Drawing.SystemColors.Window;
            this.errorMsgs.ForeColor = System.Drawing.Color.Red;
            this.errorMsgs.Location = new System.Drawing.Point(88, 582);
            this.errorMsgs.Multiline = true;
            this.errorMsgs.Name = "errorMsgs";
            this.errorMsgs.ReadOnly = true;
            this.errorMsgs.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.errorMsgs.Size = new System.Drawing.Size(678, 101);
            this.errorMsgs.TabIndex = 23;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(829, 703);
            this.Controls.Add(this.lblErrmsgs);
            this.Controls.Add(this.errorMsgs);
            this.Controls.Add(this.lblWarningMsgs);
            this.Controls.Add(this.warningMsgs);
            this.Controls.Add(this.btnExit);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.btnDoPhase);
            this.Controls.Add(this.txtProcStartsWith);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.txtDatabaseName);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.txtSqlSvr);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.btnOutputBrowse);
            this.Controls.Add(this.txtOutputFile);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.btnXmlBrowse);
            this.Controls.Add(this.txtXmlIn);
            this.Controls.Add(this.label1);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Name = "MainForm";
            this.Text = "Sql Database Interface Code Generator";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtXmlIn;
        private System.Windows.Forms.Button btnXmlBrowse;
        private System.Windows.Forms.Button btnOutputBrowse;
        private System.Windows.Forms.TextBox txtOutputFile;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.SaveFileDialog saveFileDialog1;
        private System.Windows.Forms.TextBox txtSqlSvr;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtDatabaseName;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox txtProcStartsWith;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Button btnDoPhase;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.RadioButton rBtnPhase3;
        private System.Windows.Forms.RadioButton rBtnPhase2;
        private System.Windows.Forms.RadioButton rBtnPhase1;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Button btnExit;
        private System.Windows.Forms.Label lblWarningMsgs;
        public System.Windows.Forms.TextBox warningMsgs;
        private System.Windows.Forms.Label lblErrmsgs;
        public System.Windows.Forms.TextBox errorMsgs;
    }
}

