using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using SIL.DictionaryServices.Model;
using SIL.Windows.Forms.Miscellaneous;
using SIL.Reporting;
using WeSay.LexicalModel;
using WeSay.LexicalTools.DictionaryBrowseAndEdit;
using WeSay.Project;
using WeSay.UI;
using WeSay.UI.Buttons;
using WeSay.UI.TextBoxes;

namespace WeSay.LexicalTools
{
	public partial class EntryViewControl: UserControl
	{
		//autofac generates a factory which comes up with all the other needed parameters from its container
		public delegate EntryViewControl Factory();

		private ViewTemplate _viewTemplate;
		private LexEntry _record;
		private LexEntryLayouter _layout;
		private Timer _cleanupTimer;
		private bool _isDisposed;
		private DetailList _detailListControl;

		private CurrentItemEventArgs _currentItemInFocus;
		private LexEntryRepository _lexEntryRepository;
		private bool _showNormallyHiddenFields;
		private bool _senseDeletionEnabled;
		private bool _showMinorMeaningLabel = false;
		private ConfirmDeleteFactory _confirmDeleteFactory;


		//designer and some tests
		public EntryViewControl()
		{
			InitializeComponent();
			this.SuspendLayout();
			InitializeDetailList();
			RefreshEntryDetail();
			this.ResumeLayout();
		}

		private void InitializeDetailList()
		{
			_scrollableContainer.SuspendLayout();
			_detailListControl = new DetailList();
			_detailListControl.SuspendLayout();
			_detailListControl.BackColor = BackColor;
			_detailListControl.Name = "LexEntryDetailList";
			_detailListControl.TabIndex = 1;
			_detailListControl.Size = new Size(_scrollableContainer.ClientRectangle.Width - 20, _detailListControl.Height);
			_detailListControl.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;
			_detailListControl.SizeChanged += OnScrollableContainerOrDetailListSizeChanged;
			_detailListControl.AutoSize = true;
			_detailListControl.ChangeOfWhichItemIsInFocus += OnChangeOfWhichItemIsInFocus;
			_detailListControl.KeyDown += _detailListControl_KeyDown;
			_detailListControl.MouseWheel += OnDetailListMouseWheel;
			_detailListControl.GeckoOption = WeSayWordsProject.GeckoOption;
			_scrollableContainer.AutoScroll = true;
			_scrollableContainer.Controls.Add(_detailListControl);
			_detailListControl.ResumeLayout(false);
			_scrollableContainer.ResumeLayout(false);
		}

		public EntryViewControl(EntryHeaderView.Factory entryHeaderViewFactory, ConfirmDeleteFactory confirmDeleteFactory)
		{
			_viewTemplate = null;
			InitializeComponent();
			this.SuspendLayout();
			_scrollableContainer.SizeChanged += OnScrollableContainerOrDetailListSizeChanged;
			_confirmDeleteFactory = confirmDeleteFactory;

			Controls.Remove(_entryHeaderView);
			_entryHeaderView.Dispose();
			_entryHeaderView = entryHeaderViewFactory();
			_entryHeaderView.Dock = DockStyle.Top;
			_entryHeaderView.BackColor = BackColor;
			Controls.Add(_entryHeaderView);
			Controls.SetChildIndex(_scrollableContainer, 0);
			Controls.SetChildIndex(_splitter, 1);
			Controls.SetChildIndex(_entryHeaderView, 2);

			_splitter.ControlToHide = _entryHeaderView;
			InitializeDetailList();
			RefreshEntryDetail();
			this.ResumeLayout();
		}

		private void OnScrollableContainerOrDetailListSizeChanged(object sender, EventArgs e)
		{
			if (_detailListControl != null && !_detailListControl.IsDisposed)
			{
				_detailListControl.Size = new Size(_scrollableContainer.ClientRectangle.Width - 20, _detailListControl.Height);
			}
		}

		protected override void OnHandleDestroyed(EventArgs e)
		{
			if (_cleanupTimer != null)
			{
				_cleanupTimer.Dispose();
			}
			base.OnHandleDestroyed(e);
		}

		public void SelectOnCorrectControl()
		{
			_detailListControl.Select();
		}

		private void _detailListControl_KeyDown(object sender, KeyEventArgs e)
		{
			OnKeyDown(e);
		}

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public ViewTemplate ViewTemplate
		{
			get
			{
				if (_viewTemplate == null)
				{
					throw new InvalidOperationException("ViewTemplate must be initialized");
				}
				return _viewTemplate;
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException();
				}
				_viewTemplate = value;
				Refresh();
			}
		}

		public string RtfContentsOfPreviewForTests//TODO: those tests shouldn't be testing this control
		{
			get { return _entryHeaderView.RtfForTests; }
		}

		public string TextContentsOfPreviewForTests//TODO: those tests shouldn't be testing this control
		{
			get { return _entryHeaderView.TextForTests; }
		}

		public DetailList ControlEntryDetail
		{
			get { return _detailListControl; }
		}

		public LexEntry DataSource
		{
			get { return _record; }
			set
			{
				SuspendLayout();
				Logger.WriteMinorEvent("In DataSource Set");
				_showNormallyHiddenFields = false;

				if (_record != value)
				{
					if (_record != null && !_record.IsBeingDeleted)
					{
						Logger.WriteMinorEvent(
								"Datasource set. Calling _record.CleanUpAfterEditting()");
						_record.PropertyChanged -= OnRecordPropertyChanged;
						_record.EmptyObjectsRemoved -= OnEmptyObjectsRemoved;

						_record.CleanUpAfterEditting();

//                        //from WS-1173 (jonathan_coombs@sil.org) Faulty Missing Baseform query?
//                        //what's wrong here is that since we've disabled the event handers, we don't
//                        //know if this CleanUp call makes any changes that need to be saved
//                        if(_record.IsDirty)
//                        {
//                            _lexEntryRepository.SaveItem(_record);
//                        }
					}
					_record = value;
					_currentItemInFocus = null;
					if (_record != null)
					{
						_record.PropertyChanged += OnRecordPropertyChanged;
						_record.EmptyObjectsRemoved += OnEmptyObjectsRemoved;
					}
					Logger.WriteMinorEvent("Datasource set calling RefreshLexicalEntryPreview()");
					RefreshLexicalEntryPreview();
					Logger.WriteMinorEvent("Datasource set calling RefreshEntryDetail()");
					ShowNormallyHiddenFields = false;
					RefreshEntryDetail();
				}
				ResumeLayout();
				Logger.WriteMinorEvent("Exit DataSource Set");
			}
		}



		/// <summary>
		/// Use for establishing relations been this entry and the rest
		/// </summary>
		public LexEntryRepository LexEntryRepository
		{
			set { _lexEntryRepository = value; }
		}

		public bool ShowNormallyHiddenFields
		{
			get { return _showNormallyHiddenFields; }
			set
			{
				_showNormallyHiddenFields = value;
				//no... this will lead to extra refreshing. RefreshEntryDetail();
			}
		}

		public bool SenseDeletionEnabled
		{
			get { return _senseDeletionEnabled; }
			set
			{
				if (_senseDeletionEnabled != value)
				{
					_senseDeletionEnabled = value;
					RefreshEntryDetail();
				}
			}
		}

		public bool ShowMinorMeaningLabel
		{
			get { return _showMinorMeaningLabel; }
			set
			{
				if (_showMinorMeaningLabel != value)
				{
					_showMinorMeaningLabel = value;
					RefreshEntryDetail();
				}
			}
		}

		public void SetMemory(IUserInterfaceMemory memory)
		{
			_splitter.SetMemory(memory.CreateNewSection("previewSplitter"));
		}

		public void ToggleShowNormallyHiddenFields()
		{
			ShowNormallyHiddenFields = !ShowNormallyHiddenFields;
			RefreshEntryDetail();
		}

		private void OnEmptyObjectsRemoved(object sender, EventArgs e)
		{
			VerifyNotDisposed();
			//find out where our current focus is and attempt to return to that place
			int? row = null;
			if (_detailListControl.ContainsFocus)
			{
				Control focussedControl = _detailListControl.FocussedImmediateChild;
				if (focussedControl != null)
				{
					row = _detailListControl.GetRow(focussedControl);
				}
			}
			Logger.WriteMinorEvent("OnEmptyObjectsRemoved: b4 RefreshEntryDetial");
			RefreshEntryDetail();
			Logger.WriteMinorEvent("OnEmptyObjectsRemoved: b4  Application.DoEvents()");
			Application.DoEvents();
			//TODO: We need to remove this.  It's a dangerous thing in a historically buggy spot
			Logger.WriteMinorEvent("OnEmptyObjectsRemoved: b4 MoveInsertionPoint");
			if (row != null)
			{
				row = Math.Min((int) row, _detailListControl.RowCount - 1);
				Debug.Assert(row > -1, "You've reproduced bug ws-511!");
				// bug WS-511, which we haven't yet been able to reproduce
				row = Math.Max((int) row, 0); //would get around bug WS-511
				_detailListControl.MoveInsertionPoint((int) row);
			}
			Logger.WriteMinorEvent("OnEmptyObjectsRemoved end");
		}

		private void OnRecordPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			VerifyNotDisposed();
			LexEntry entry = (LexEntry) sender;
			switch (e.PropertyName)
			{
					// these are changes to the list not a change that needs to clean up
					// and can actually have very detrimental effect if we do.
				case "exampleSentences":
				case "senses":
					break;
				default:
					//this is to fix WS-238. The scenario is this:
					//a paste operation starts by erasing the target... this fired off a cleanup
					//and then the new text came in so fast that we got various crashes.
					//With this, we just schedule a cleanup, as a ui event handler, for
					//a moment in the future.  The interval is chosen to allow even a quick
					//backspace followed by typing, without wiping out the sense/example.
					if (_cleanupTimer == null)
					{
						_cleanupTimer = new Timer();
						_cleanupTimer.Tick += OnCleanupTimer_Tick;
						_cleanupTimer.Interval = 500;
					}
					_cleanupTimer.Tag = entry;
					_cleanupTimer.Stop();//reset it
					_cleanupTimer.Start();
					break;
			}
			_lexEntryRepository.NotifyThatLexEntryHasBeenUpdated((LexEntry)sender);
		  // can't afford to do this every keystroke, with large files

		}



		private void OnCleanupTimer_Tick(object sender, EventArgs e)
		{
			_cleanupTimer.Stop();
			if (_isDisposed) ////saw this once get disposed while it was running
				return;

			Logger.WriteMinorEvent("OnCleanupTimer_Tick");
			LexEntry entry = (LexEntry) _cleanupTimer.Tag;
			entry.CleanUpEmptyObjects();

			RefreshLexicalEntryPreview();
		}

		protected void VerifyNotDisposed()
		{
			if (_isDisposed)
			{
#if DEBUG
				throw new ObjectDisposedException(GetType().FullName);
#else
				SIL.Reporting.ErrorReport.NotifyUserOfProblem("WeSay ran into a problem in the EntryViewControl (it was called after it was disposed.) If you can make this happen again, please contact the developers.");
#endif
			}
		}

		private void RefreshLexicalEntryPreview()
		{
			if (_isDisposed || _entryHeaderView.IsDisposed) ////saw this once get disposed while it was running
				return;

#if !DEBUG
			try
			{
#endif
			VerifyHasLexEntryRepository();
			_entryHeaderView.UpdateContents(_record,_currentItemInFocus,_lexEntryRepository);
#if !DEBUG
			}
			catch (Exception)
			{
				SIL.Reporting.ErrorReport.NotifyUserOfProblem("There was an error refreshing the entry preview. If you were quiting the program, this is a know issue (WS-554) that we are trying to track down.  If you can make this happen again, please contact the developers.");
			}
#endif
		}

		private void VerifyHasLexEntryRepository()
		{
			if (_lexEntryRepository == null)
			{
				throw new InvalidOperationException("LexEntryRepository has not been initialized");
			}
		}

		private void RefreshEntryDetail()
		{
			try
			{
				_scrollableContainer.SuspendLayout();
				// We can't suspend layout for _detailListControl here because
				// that causes occasional display layout failures.  Unfortunately, not
				// suspending layout allows a good deal of flicker on Linux/Mono.  But
				// making _detailListControl invisible gets rid of the flicker at the
				// cost of a noticeable pause before the display changes on Linux/Mono.
				// A possible hack for Linux/Mono would be to #ifdef __MonoCS__ do the
				// following:
				// 1) add _detailListControl.SuspendLayout()/ResumeLayout() here.
				// 2) at the end of this function, put in a timer that asynchonously
				//    invokes _detailListControl.PerformLayout() after a short pause.
				// There would still be a bit of flicker possibly, but not as much as
				// seen without the visibility hack, and probably less overall computing
				// and thus less overall delay.
				_detailListControl.Visible = false;
				if (_detailListControl.RowCount > 0)
				{
					_detailListControl.Controls.Clear();
					_detailListControl.RowStyles.Clear();
					_detailListControl.RowCount = 0;
				}
				if (_record != null)
				{
					VerifyHasLexEntryRepository();
					_layout = new LexEntryLayouter(
						_detailListControl,
						0,
						ViewTemplate,
						_lexEntryRepository,
						WeSayWordsProject.Project.ServiceLocator,
						_record,
						_senseDeletionEnabled,
						_confirmDeleteFactory,
						_showMinorMeaningLabel
					) {ShowNormallyHiddenFields = ShowNormallyHiddenFields};
					_layout.AddWidgets(_record, 0);
				}
				_detailListControl.Visible = true;
				_scrollableContainer.ResumeLayout();
			}
			catch (ConfigurationException e)
			{
				ErrorReport.NotifyUserOfProblem(e.Message);
			}
		}

		private void OnDetailListMouseWheel(object sender, MouseEventArgs e)
		{
			_scrollableContainer.ScrollAccordingToEventArgs(e);
		}

		private void OnChangeOfWhichItemIsInFocus(object sender, CurrentItemEventArgs e)
		{
			VerifyNotDisposed();
			_currentItemInFocus = e;
			RefreshLexicalEntryPreview();
		}


		private void OnBackColorChanged(object sender, EventArgs e)
		{
			if(_detailListControl !=null)
				_detailListControl.BackColor = BackColor;
			if(_entryHeaderView!=null)
				_entryHeaderView.BackColor = BackColor;
		}

		~EntryViewControl()
		{
			if (!_isDisposed)
			{
				throw new InvalidOperationException("Disposed not explicitly called on " +
													GetType().FullName + ".");
			}
		}

		// hack to get around the fact that SplitContainer takes over the
		// tab order and doesn't allow you to specify that the controls in the
		// right pane should get the highest tab order.
		// this means the RTF view looks bad. Still haven't figured out how to make
		// cursor go to right position.
		protected override void OnEnter(EventArgs e)
		{
			base.OnEnter(e);
			RefreshLexicalEntryPreview();
		}

		public void FocusFirstEditableField()
		{
			if (_detailListControl.RowCount > 0)
			{
				_detailListControl.MoveInsertionPoint(0);
			}
		}
	}
}
