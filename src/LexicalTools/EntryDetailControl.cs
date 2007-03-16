using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using WeSay.Data;
using WeSay.Language;
using WeSay.LexicalModel;
using WeSay.LexicalModel.Db4o_Specific;
using WeSay.LexicalTools.Properties;
using WeSay.Project;
using Debug=System.Diagnostics.Debug;

namespace WeSay.LexicalTools
{
	public partial class EntryDetailControl : UserControl
	{
		private IBindingList _records;
		private WritingSystem _listWritingSystem;
		private ContextMenu _cmWritingSystems;

		private Db4oRecordListManager _recordManager;

		private readonly ViewTemplate _viewTemplate;
		public event EventHandler SelectedIndexChanged = delegate {};

		public EntryDetailControl()
		{
			Debug.Assert(DesignMode);
			InitializeComponent();
		}

		public EntryDetailControl(IRecordListManager recordManager, ViewTemplate viewTemplate)
		{
			if (recordManager == null)
			{
				throw new ArgumentNullException("recordManager");
			}
			if (viewTemplate == null)
			{
				throw new ArgumentNullException("viewTemplate");
			}
			_viewTemplate = viewTemplate;
			_recordManager = (Db4oRecordListManager)recordManager;

			WritingSystem listWritingSystem = null;
			_cmWritingSystems = new ContextMenu();

			Field field = viewTemplate.GetField(Field.FieldNames.EntryLexicalForm.ToString());
			if (field != null)
			{
				if (field.WritingSystems.Count > 0)
				{
					listWritingSystem = field.WritingSystems[0];
					foreach (WritingSystem writingSystem in field.WritingSystems)
					{
						RegisterWritingSystemAndField(field, writingSystem);
					}
				}
				else
				{
					MessageBox.Show(String.Format("There are no writing systems enabled for the Field '{0}'", field.FieldName), "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);//review
				}
			}
			Field glossfield = viewTemplate.GetField(Field.FieldNames.SenseGloss.ToString());
			if (glossfield != null)
			{
				foreach (WritingSystem writingSystem in glossfield.WritingSystems)
				{
					RegisterWritingSystemAndField(glossfield, writingSystem);
				}
			}

			if (listWritingSystem == null)
			{
				listWritingSystem = BasilProject.Project.WritingSystems.UnknownVernacularWritingSystem;
			}

			InitializeComponent();

			Control_EntryDetailPanel.ViewTemplate = _viewTemplate;

			SetListWritingSystem(listWritingSystem);

			_findText.KeyDown += new KeyEventHandler(_findText_KeyDown);
			_recordsListBox.SelectedIndexChanged += new EventHandler(OnRecordSelectionChanged);
			_btnDeleteWord.Enabled = (CurrentRecord != null);
		}

		private void RegisterWritingSystemAndField(Field field, WritingSystem writingSystem) {
			MenuItem item =
					new MenuItem(writingSystem.Id + "\t" + StringCatalog.Get(field.DisplayName),
								 OnCmWritingSystemClicked);
			item.Checked = (this._listWritingSystem == writingSystem);
			item.RadioCheck = true;
			item.Tag = writingSystem;
			this._cmWritingSystems.MenuItems.Add(item);


			LexEntrySortHelper sortHelper = new LexEntrySortHelper(_recordManager.DataSource,
													   writingSystem.Id,
													   IsWritingSystemUsedInLexicalForm(writingSystem));
			_recordManager.GetSortedList(sortHelper);

		}

		private bool IsWritingSystemUsedInLexicalForm(WritingSystem writingSystem)
		{
			Field field = _viewTemplate.GetField(Field.FieldNames.EntryLexicalForm.ToString());
			if (field != null)
			{
				return field.WritingSystems.Contains(writingSystem);
			}
			return false;
		}

		public void SetListWritingSystem(WritingSystem writingSystem) {
			if(writingSystem == null)
			{
				throw new ArgumentNullException();
			}
			if(_listWritingSystem == writingSystem)
			{
				return;
			}
			_listWritingSystem = writingSystem;

			LexEntrySortHelper sortHelper = new LexEntrySortHelper(_recordManager.DataSource,
																   this._listWritingSystem.Id,
																   IsWritingSystemUsedInLexicalForm(_listWritingSystem));
			this._records = _recordManager.GetSortedList(sortHelper);

			this._recordsListBox.DataSource = this._records;

			Control_EntryDetailPanel.DataSource = CurrentRecord;

			this._recordsListBox.Font = this._listWritingSystem.Font;

			this._findText.ItemFilterer = ApproximateMatcher.FindClosestAndNextClosestAndPrefixedForms;
			this._findText.Items = (CachedSortedDb4oList<string, LexEntry>)this._records;
			int originalHeight = this._findText.Height;
			this._findText.Font = this._listWritingSystem.Font;
			this._findText.WritingSystem = this._listWritingSystem;

			this._findWritingSystemId.Text = this._listWritingSystem.Id;
			this._findText.Left = this._findWritingSystemId.Right;
			int width = this._findText.Width;
			this._findWritingSystemId.AutoSize = false;
			this._findWritingSystemId.Height = this._findText.Height;
			this._findWritingSystemId.Width = Math.Min(width, 25);

			int heightDifference = this._findText.Height - originalHeight;
			this._recordsListBox.Location = new Point(this._recordsListBox.Location.X,
												 this._recordsListBox.Location.Y + heightDifference);
			this._btnFind.Height = this._findText.Height;
			this._btnFind.Width = this._findText.Height;
			this._writingSystemChooser.Height = this._findText.Height;
			this._btnFind.Image = Resources.Find.GetThumbnailImage(this._btnFind.Width - 2,
																   this._btnFind.Height - 2,
																   ReturnFalse, IntPtr.Zero);

			this._btnFind.Left = this._writingSystemChooser.Left - this._btnFind.Width;
			this._findText.Width = this._btnFind.Left - this._findText.Left;
			this._findText.PopupWidth = this._recordsListBox.Width;
		}

		void OnCmWritingSystemClicked(object sender, EventArgs e)
		{
			MenuItem item = (MenuItem) sender;
			if (_listWritingSystem != item.Tag)
			{
				SetListWritingSystem((WritingSystem)item.Tag);
			}
		}

		void _writingSystemChooser_Click(object sender, EventArgs e)
		{
			this._cmWritingSystems.Show(_writingSystemChooser, new Point(0,_writingSystemChooser.Height), LeftRightAlignment.Right);
		}

		void _findWritingSystemId_MouseClick(object sender, MouseEventArgs e)
		{
			_findText.Focus();
		}

		// primarily for testing
		public LexPreviewWithEntryControl Control_EntryDetailPanel
		{
			get
			{
				return this._entryDetailPanel;
			}
		}

		void _findText_KeyDown(object sender, KeyEventArgs e)
		{
			if(e.Modifiers == Keys.None &&
			   e.KeyData == Keys.Enter)
			{
				e.Handled = true;
				e.SuppressKeyPress = true; // otherwise it beeps!
				Find(this._findText.Text);
			}
		}

		void _findText_AutoCompleteChoiceSelected(object sender, System.EventArgs e)
		{
			Find(this._findText.Text);
		}

		void _btnFind_Click(object sender, EventArgs e)
		{
			Find(this._findText.Text);
		}

		private void Find(string text) {
			int index = ((CachedSortedDb4oList<string, LexEntry>) _records).BinarySearch(text);
			if (index < 0)
			{
				index = ~index;
				if (index == _records.Count && index != 0)
				{
					index--;
				}
			}
			if(index >=0)
			{
				_recordsListBox.SelectedIndex = index;
			}
		}


		void OnRecordSelectionChanged(object sender, EventArgs e)
		{
			if (Control_EntryDetailPanel.DataSource == CurrentRecord)
			{
				//we were getting 3 calls to this for each click on a new word
				return;
			}
			_btnDeleteWord.Enabled = (CurrentRecord != null);
			if (Control_EntryDetailPanel.ContainsFocus)
			{
				// When we change the content of the displayed string,
				// Windows.Forms.ListBox removes the item (and sends
				// the new selection event) then adds it in to the right
				// place (and sends the new selection event again)
				// We don't want to know about this case
				// There is nothing which can originate a change in
				// record selection while the entry detail panel
				// contains the focus so this is safe for now.
				return;
			}
			Control_EntryDetailPanel.DataSource = CurrentRecord;
			SelectedIndexChanged.Invoke(this,null);
		}

		public LexEntry CurrentRecord
		{
			get
			{
				if (_records.Count == 0 || CurrentIndex == -1)
				{
					return null;
				}
				return ((CachedSortedDb4oList<string, LexEntry>)_records).GetValue(CurrentIndex);
			}
		}

		protected int CurrentIndex
		{
			get
			{
			   return _recordsListBox.SelectedIndex;
			}
		}

		private void _btnNewWord_LinkClicked(object sender, EventArgs e)
		{
			if (!this._btnNewWord.Focused)
			{
				// if we use a hot key, it may not have received the focus
				// but we assume it has the focus when we do our selection change event
				this._btnNewWord.Focus();
			}
			LexEntry entry = new LexEntry();
			bool NoPriorSelection = _recordsListBox.SelectedIndex == -1;
			_records.Add(entry);
			_recordsListBox.SelectedIndex = _records.IndexOf(entry);
			if(NoPriorSelection)
			{
				// Windows.Forms.Listbox does not consider it a change of Selection
				// index if the index was -1 and a record is added.
				// (No event is sent so we must do it ourselves)
				OnRecordSelectionChanged(this, null);
			}
			_entryDetailPanel.Focus();
		}

		private void _btnDeleteWord_LinkClicked(object sender, EventArgs e)
		{
			Debug.Assert(CurrentIndex >= 0);
			if(CurrentIndex == -1)
			{
				return;
			}
			if (!this._btnDeleteWord.Focused)
			{
				// if we use a hot key, it may not have received the focus
				// but we assume it has the focus when we do our selection change event
				this._btnDeleteWord.Focus();
			}
			_records.RemoveAt(CurrentIndex);
			//hack until we can get selection change events sorted out in BindingGridList
			OnRecordSelectionChanged(this, null);
			_recordsListBox.Refresh();
			_entryDetailPanel.Focus();
		}
	}
}
