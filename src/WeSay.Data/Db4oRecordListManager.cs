using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Collections;

namespace WeSay.Data
{
	public class Db4oRecordListManager : AbstractRecordListManager
	{
		public Db4oRecordListManager()
		: base()
		{
		}

		protected override IRecordList<T> CreateMasterRecordList<T>()
		{
			return new InMemoryRecordList<T>();
		}

		protected override IRecordList<T> CreateFilteredRecordList<T>(Predicate<T> filter)
		{
			return new FilteredDb4oRecordList<T>(Get<T>(), filter);
		}

		class FilteredDb4oRecordList<T> : Db4oRecordList<T> where T : class, new()
		{
			IRecordList<T> _masterRecordList;
			bool _isSourceMasterRecord;
			Predicate<T> IsRelevant;

			public FilteredDb4oRecordList(IRecordList<T> sourceRecords, Predicate<T> filter)
			: base((Db4oRecordList<T>)sourceRecords)
			{
				_masterRecordList = sourceRecords;
				_masterRecordList.ListChanged += new ListChangedEventHandler(OnMasterRecordListListChanged);
				_masterRecordList.DeletingRecord += new EventHandler<RecordListEventArgs<T>>(OnMasterRecordListDeletingRecord);
				IsRelevant = filter;
				this.ApplyFilter(filter);
			}

			void OnMasterRecordListDeletingRecord(object sender, RecordListEventArgs<T> e)
			{
				_isSourceMasterRecord = true;
				HandleItemDeletedFromMaster(e.Item);
				_isSourceMasterRecord = false;
			}

			void OnMasterRecordListListChanged(object sender, ListChangedEventArgs e)
			{
				IRecordList<T> masterRecordList = (IRecordList<T>)sender;
				VerifyNotDisposed();
				_isSourceMasterRecord = true;
				switch (e.ListChangedType)
				{
					case ListChangedType.ItemAdded:
						HandleItemAddedToMaster(masterRecordList[e.NewIndex]);
						break;
					case ListChangedType.ItemChanged:
						HandleItemChangedInMaster(masterRecordList[e.NewIndex]);
						break;
					case ListChangedType.ItemDeleted:
						break;
					case ListChangedType.Reset:
						HandleItemsClearedFromMaster(masterRecordList);
						break;
				}
				_isSourceMasterRecord = false;
			}

			protected override bool ShouldAddRecord(T item)
			{
				bool shouldAdd = base.ShouldAddRecord(item);
				if (shouldAdd)
				{
					shouldAdd = !Contains(item);
				}
				return shouldAdd;
			}
			protected override bool ShouldDeleteRecord(T item)
			{
				if (!_isSourceMasterRecord && _masterRecordList != null)
				{
					_masterRecordList.Remove(item);
				}
				return base.ShouldDeleteRecord(item);
			}

			private void HandleItemAddedToMaster(T item)
			{
				AddIfRelevantElseRemove(item);
			}

			private void HandleItemChangedInMaster(T item)
			{
				AddIfRelevantElseRemove(item);
			}

			private void AddIfRelevantElseRemove(T item)
			{
				if (this.IsRelevant(item))
				{
					if (!Contains(item))
					{
						this.Add(item);
					}
				}
				else if (this.Contains(item))
				{
					this.Remove(item);
				}
			}

			private void HandleItemDeletedFromMaster(T item)
			{
				if (this.Contains(item))
				{
					this.Remove(item);
				}
			}

			private void HandleItemsClearedFromMaster(IRecordList<T> masterRecordList)
			{
				// The reset event can be raised when items are cleared and also when sorting or filtering occurs
				if (masterRecordList.Count == 0)
				{
					this.Clear();
				}
			}

			protected override void OnItemAdded(int newIndex)
			{
				base.OnItemAdded(newIndex);
				if (!_isSourceMasterRecord && _masterRecordList != null)
				{
					_masterRecordList.Add(this[newIndex]);
				}
			}


			#region IDisposable Members

			protected override void Dispose(bool disposing)
			{
				if (!this.IsDisposed)
				{
					if (disposing)
					{
						// dispose-only, i.e. non-finalizable logic
						_masterRecordList.ListChanged -= OnMasterRecordListListChanged;
						_masterRecordList = null;
					}
				}
				base.Dispose(disposing);
			}
			#endregion
		}

	}
}
