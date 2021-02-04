﻿namespace WeSay.LexicalTools.DictionaryBrowseAndEdit
{
	partial class ConfirmDelete
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
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ConfirmDelete));
			this.pictureBox1 = new System.Windows.Forms.PictureBox();
			this.localizationHelper1 = new SIL.Windows.Forms.i18n.LocalizationHelper(this.components);
			this.deleteBtn = new System.Windows.Forms.Button();
			this.cancelBtn = new System.Windows.Forms.Button();
			this.textBox1 = new System.Windows.Forms.TextBox();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.localizationHelper1)).BeginInit();
			this.SuspendLayout();
			//
			// pictureBox1
			//
			this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
			this.pictureBox1.Location = new System.Drawing.Point(26, 24);
			this.pictureBox1.Name = "pictureBox1";
			this.pictureBox1.Size = new System.Drawing.Size(52, 65);
			this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
			this.pictureBox1.TabIndex = 1;
			this.pictureBox1.TabStop = false;
			//
			// localizationHelper1
			//
			this.localizationHelper1.Parent = this;
			//
			// deleteBtn
			//
			this.deleteBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
			this.deleteBtn.Image = global::WeSay.LexicalTools.Properties.Resources.DeleteIcon;
			this.deleteBtn.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
			this.deleteBtn.Location = new System.Drawing.Point(26, 132);
			this.deleteBtn.Name = "deleteBtn";
			this.deleteBtn.Size = new System.Drawing.Size(120, 36);
			this.deleteBtn.TabIndex = 1;
			this.deleteBtn.Text = "&Delete";
			this.deleteBtn.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.deleteBtn.UseVisualStyleBackColor = true;
			this.deleteBtn.Click += new System.EventHandler(this.deleteBtn_Click);
			//
			// cancelBtn
			//
			this.cancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.cancelBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
			this.cancelBtn.Location = new System.Drawing.Point(219, 132);
			this.cancelBtn.Name = "cancelBtn";
			this.cancelBtn.Size = new System.Drawing.Size(120, 36);
			this.cancelBtn.TabIndex = 0;
			this.cancelBtn.Text = "&Cancel";
			this.cancelBtn.UseVisualStyleBackColor = true;
			this.cancelBtn.Click += new System.EventHandler(this.cancelBtn_Click);
			//
			// textBox1
			//
			this.textBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.textBox1.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.textBox1.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.textBox1.Location = new System.Drawing.Point(116, 24);
			this.textBox1.Multiline = true;
			this.textBox1.Name = "textBox1";
			this.textBox1.ReadOnly = true;
			this.textBox1.Size = new System.Drawing.Size(223, 87);
			this.textBox1.TabIndex = 4;
			this.textBox1.TabStop = false;
			this.textBox1.Text = "This will permanently remove this entry.";
			//
			// ConfirmDelete
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.BackColor = System.Drawing.SystemColors.Window;
			this.CancelButton = this.cancelBtn;
			this.ClientSize = new System.Drawing.Size(359, 177);
			this.ControlBox = false;
			this.Controls.Add(this.textBox1);
			this.Controls.Add(this.cancelBtn);
			this.Controls.Add(this.deleteBtn);
			this.Controls.Add(this.pictureBox1);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "ConfirmDelete";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Confirm Delete";
			this.BackColorChanged += new System.EventHandler(this.ConfirmDelete_BackColorChanged);
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.localizationHelper1)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.PictureBox pictureBox1;
		private SIL.Windows.Forms.i18n.LocalizationHelper localizationHelper1;
		private System.Windows.Forms.Button deleteBtn;
		private System.Windows.Forms.Button cancelBtn;
		private System.Windows.Forms.TextBox textBox1;
	}
}