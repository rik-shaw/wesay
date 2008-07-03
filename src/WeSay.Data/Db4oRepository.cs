using System;
using System.Collections.Generic;
using Db4objects.Db4o;
using Db4objects.Db4o.Ext;
using Db4objects.Db4o.Query;

namespace WeSay.Data
{
	public class Db4oRepository<T> : IRepository<T> where T:class, new()
	{
		private readonly Db4oDataSource _database;
		private const int ActivationDepth = 99;

		[CLSCompliant(false)]
		protected IExtObjectContainer InternalDatabase
		{
			get
			{
				return Database;
			}
		}

		internal IExtObjectContainer Database
		{
			get
			{
				return _database.Data.Ext();
			}
		}
		public Db4oRepository(string path) : this(new Db4oDataSource(path))
		{
		}

		public Db4oRepository(Db4oDataSource database)
		{
			_database = database;
		}

		public DateTime LastModified
		{
			get
			{
				DatabaseModified databaseModified = GetDatabaseModified();
				return databaseModified.LastModified;
			}
			private set
			{
				DatabaseModified databaseModified = GetDatabaseModified();
				databaseModified.LastModified = value;
				Database.Set(databaseModified);
			}
		}

		private DatabaseModified GetDatabaseModified() {
			IList<DatabaseModified> query = Database.Query<DatabaseModified>();
			DatabaseModified databaseModified;
			switch (query.Count)
			{
				case 0:
					databaseModified = new DatabaseModified();
					Database.Set(databaseModified);
					break;
				case 1:
					databaseModified = query[0];
					break;
				default:
					throw new InvalidOperationException();
			}
			return databaseModified;
		}

		bool IRepository<T>.CanQuery
		{
			get { return true; }
		}

		bool IRepository<T>.CanPersist
		{
			get { return true; }
		}

		public T CreateItem()
		{
			T item = new T();
			Database.Set(item, ActivationDepth);
			Commit();
			return item;
		}

		public int CountAllItems()
		{
			return GetAllItems().Length;
		}

		public RepositoryId GetId(T item)
		{
			if (item == null)
			{
				throw new ArgumentNullException("item");
			}
			VerifyItemExistsInRepository(item);

			long id = Database.GetID(item);
			return new Db4oRepositoryId(id);
		}


		public T GetItem(RepositoryId id)
		{
			if (id == null)
			{
				throw new ArgumentNullException("id");
			}
			if(!(id is Db4oRepositoryId))
			{
				throw new ArgumentOutOfRangeException("id", id, "Bad repository id. This repository id was not generated by this repository.");
			}
			T item = (T) Database.GetByID(((Db4oRepositoryId)id).Db4oId);
			EnsureItemActive(item);
			if(item == null)
			{
				throw new ArgumentOutOfRangeException("id", id, "Object with this id does not exist in repository");
			}
			return item;
		}

		private void EnsureItemActive(T item) {
			if(!Database.IsActive(item))
			{
				Database.Activate(item, ActivationDepth);
			}
		}

		public void DeleteItem(T item)
		{
			if (item == null)
			{
				throw new ArgumentNullException("item");
			}
			VerifyItemExistsInRepository(item);
			EnsureItemActive(item);

			Database.Delete(item);
			Commit();
		}

		public void DeleteItem(RepositoryId id)
		{
			if (id == null)
			{
				throw new ArgumentNullException("id");
			}
			DeleteItem(GetItem(id));
		}

		public RepositoryId[] GetAllItems()
		{
			IObjectSet objectSet = Database.Query(typeof(T));
			long[] db4oIds = objectSet.Ext().GetIDs();
			return WrapDb4oIdsInRepositoryIds(db4oIds);
		}

		private static RepositoryId[] WrapDb4oIdsInRepositoryIds(long[] db4oIds)
		{
			RepositoryId[] ids = new RepositoryId[db4oIds.Length];
			for (int i = 0; i != db4oIds.Length; ++i)
			{
				ids[i] = new Db4oRepositoryId(db4oIds[i]);
			}
			return ids;
		}

		//todo: refactor this so handled by synchronizer
		public RepositoryId[] GetItemsModifiedSince(DateTime last)
		{
			throw new NotImplementedException();
			// by moving back 1 milliseconds, we ensure that we
			// will get the correct records with just a > and not >=
			last = last.AddMilliseconds(-1);
			IQuery q = Database.Query();
			q.Constrain(typeof(T));
			q.Descend("_modificationTime").Constrain(last).Greater();
			IObjectSet objectSet = q.Execute();
			return WrapDb4oIdsInRepositoryIds(objectSet.Ext().GetIDs());
		}

		public void SaveItems(IEnumerable<T> items)
		{
			if (items == null)
			{
				throw new ArgumentNullException("items");
			}
			bool hasItems = false;
			foreach (T item in items)
			{
				SaveItemCore(item);
				hasItems = true;
			}
			if (hasItems)
			{
				Commit();
			}
		}

		// Not fast but correct
		public ResultSet<T> GetItemsMatching(Query query)
		{
			List<RecordToken<T>> results = new List<RecordToken<T>>();
			IList<T> allItems = Database.Query<T>();
			foreach (T t in allItems)
			{
				EnsureItemActive(t);
				bool hasResults = false;
				foreach (Dictionary<string, object> result in query.GetResults(t))
				{
					hasResults = true;
					results.Add(new RecordToken<T>(this, result, GetId(t)));
				}
				if(!hasResults)
				{
					results.Add(new RecordToken<T>(this,GetId(t)));
				}
			}
			return new ResultSet<T>(this, results);
		}

		public void SaveItem(T item)
		{
			SaveItemCore(item);
			Commit();
		}

		private void Commit() {
			LastModified = PreciseDateTime.UtcNow;
			Database.Commit();
		}

		public bool CanQuery()
		{
			throw new NotImplementedException();
		}

		public bool CanPersist()
		{
			throw new NotImplementedException();
		}

		private void SaveItemCore(T item)
		{
			if (item == null)
			{
				throw new ArgumentNullException("item");
			}
			VerifyItemExistsInRepository(item);
			EnsureItemActive(item);
			Database.Set(item, ActivationDepth);
		}

		private void VerifyItemExistsInRepository(T item)
		{
			if(Database.GetID(item) == 0)
			{
				throw new ArgumentOutOfRangeException("item", item, "Object does not exist in repository");
			}
		}

		#region IDisposable Members
#if DEBUG
		~Db4oRepository()
		{
			if (!this._disposed)
			{
				throw new ApplicationException("Disposed not explicitly called on Db4oRepository.");
			}
		}
#endif

		private bool _disposed = false;

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!this._disposed)
			{
				if (disposing)
				{
					// dispose-only, i.e. non-finalizable logic
					Database.Dispose();
				}

				// shared (dispose and finalizable) cleanup logic
				this._disposed = true;
			}
		}

		protected void VerifyNotDisposed()
		{
			if (this._disposed)
			{
				throw new ObjectDisposedException("Db4oRepository");
			}
		}
		#endregion
	}
}
