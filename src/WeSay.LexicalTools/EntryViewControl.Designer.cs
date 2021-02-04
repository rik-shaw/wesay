using System;
using System.Windows.Forms;
using WeSay.UI;

namespace WeSay.LexicalTools
{
	partial class EntryViewControl
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
			if (disposing && !IsDisposed)
			{
				if (_cleanupTimer != null)
				{
					_cleanupTimer.Stop();
					_cleanupTimer.Dispose();
					_cleanupTimer = null;
				}
				if (_record != null)
				{
					_record.PropertyChanged -= OnRecordPropertyChanged;
					_record.EmptyObjectsRemoved -= OnEmptyObjectsRemoved;
				}
				_detailListControl.ChangeOfWhichItemIsInFocus -= OnChangeOfWhichItemIsInFocus;
				_detailListControl.KeyDown -= _detailListControl_KeyDown;
				_detailListControl.SizeChanged -= OnScrollableContainerOrDetailListSizeChanged;
				_detailListControl.Dispose();
			}
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
			_isDisposed = true;
		}



		#region Component Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this._scrollableContainer = new ScrollableContainer();
			this._splitter = new WeSay.UI.CollapsibleSplitter();
			this._entryHeaderView = new WeSay.LexicalTools.EntryHeaderView();
			this.SuspendLayout();
			//
			// _scrollableContainer
			//
			this._scrollableContainer.AutoSize = true;
			this._scrollableContainer.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this._scrollableContainer.Dock = System.Windows.Forms.DockStyle.Fill;
			this._scrollableContainer.Location = new System.Drawing.Point(0, 137);
			this._scrollableContainer.Name = "_scrollableContainer";
			this._scrollableContainer.Size = new System.Drawing.Size(474, 233);
			this._scrollableContainer.TabIndex = 1;
			//
			// _splitter
			//
			this._splitter.BackColorEnd = System.Drawing.Color.Empty;
			this._splitter.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this._splitter.BorderStyle3D = System.Windows.Forms.Border3DStyle.Adjust;
			this._splitter.ControlToHide = this._entryHeaderView;
			this._splitter.Cursor = System.Windows.Forms.Cursors.HSplit;
			this._splitter.Dock = System.Windows.Forms.DockStyle.Top;
			this._splitter.GripLength = 15;
			this._splitter.GripperLocation = WeSay.UI.GripperLocations.RightOrBottom;
			this._splitter.GripperStyle = WeSay.UI.GripperStyles.DoubleDots;
			this._splitter.Location = new System.Drawing.Point(0, 129);
			this._splitter.Name = "_splitter";
			this._splitter.Size = new System.Drawing.Size(474, 8);
			this._splitter.TabIndex = 1;
			this._splitter.TabStop = false;
			//
			// _entryHeaderView
			//
			this._entryHeaderView.BackColor = System.Drawing.SystemColors.Control;
			this._entryHeaderView.Dock = System.Windows.Forms.DockStyle.Top;
			this._entryHeaderView.Location = new System.Drawing.Point(0, 0);
			this._entryHeaderView.Name = "_entryHeaderView";
			this._entryHeaderView.Size = new System.Drawing.Size(474, 129);
			this._entryHeaderView.TabIndex = 0;
			//
			// EntryViewControl
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.Controls.Add(this._scrollableContainer);
			this.Controls.Add(this._splitter);
			this.Controls.Add(this._entryHeaderView);
			this.Name = "EntryViewControl";
			this.Size = new System.Drawing.Size(474, 370);
			this.BackColorChanged += new System.EventHandler(this.OnBackColorChanged);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private CollapsibleSplitter _splitter;
		private ScrollableContainer _scrollableContainer;
		private EntryHeaderView _entryHeaderView;

	}
}
