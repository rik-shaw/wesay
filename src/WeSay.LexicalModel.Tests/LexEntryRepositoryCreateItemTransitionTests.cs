using System.Collections.Generic;
using NUnit.Framework;
using SIL.DictionaryServices.Model;
using SIL.TestUtilities;
using SIL.Tests.Data;

namespace WeSay.LexicalModel.Tests
{
	[TestFixture]
	public class LexEntryRepositoryCreateItemTransitionTests :
		IRepositoryCreateItemTransitionTests<LexEntry>
	{
		private TemporaryFolder _tempFolder;
		private string _persistedFilePath;

		public LexEntryRepositoryCreateItemTransitionTests()
		{
			_hasPersistOnCreate = false;
		}

		[SetUp]
		public override void SetUp()
		{
			_tempFolder = new TemporaryFolder("CreateItemTransitionTests");
			_persistedFilePath = _tempFolder.GetPathForNewTempFile(true);
			DataMapperUnderTest = new LexEntryRepository(_persistedFilePath);
		}

		[TearDown]
		public override void TearDown()
		{
			DataMapperUnderTest.Dispose();
			_tempFolder.Dispose();
		}

		[Test]
		public void SaveItem_LexEntryIsDirtyIsFalse()
		{
			SetState();
			DataMapperUnderTest.SaveItem(Item);
			Assert.IsFalse(Item.IsDirty);
		}

		[Test]
		public void SaveItems_LexEntryIsDirtyIsFalse()
		{
			SetState();
			var itemsToBeSaved = new List<LexEntry>();
			itemsToBeSaved.Add(Item);
			DataMapperUnderTest.SaveItems(itemsToBeSaved);
			Assert.IsFalse(Item.IsDirty);
		}

		[Test]
		public void Constructor_LexEntryIsDirtyIsTrue()
		{
			SetState();
			Assert.IsTrue(Item.IsDirty);
		}

		protected override void CreateNewRepositoryFromPersistedData()
		{
			DataMapperUnderTest.Dispose();
			DataMapperUnderTest = new LexEntryRepository(_persistedFilePath);
		}
	}
}