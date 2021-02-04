using System;
using System.IO;
using Chorus.UI.Review;
using NUnit.Framework;
using SIL.Reporting;
using SIL.TestUtilities;
using SIL.WritingSystems;
using WeSay.LexicalModel;
using WeSay.LexicalModel.Foundation;
using WeSay.LexicalTools.DictionaryBrowseAndEdit;
using WeSay.Project;
using WeSay.TestUtilities;

namespace WeSay.LexicalTools.Tests
{
	[TestFixture]
	public class DictionaryTaskTests: TaskBaseTests
	{
		private LexEntryRepository _lexEntryRepository;
		private ViewTemplate _viewTemplate;
		private string _filePath;
		private TemporaryFolder _tempFolder;
		private DictionaryControl.Factory _dictControlFactory;
		private TaskMemoryRepository _taskMemoryRepository;

		[OneTimeSetUp]
		public void FixtureSetup()
		{
			SIL.Windows.Forms.Keyboarding.KeyboardController.Initialize();
		}

		[OneTimeTearDown]
		public void FixtureTeardown()
		{
			SIL.Windows.Forms.Keyboarding.KeyboardController.Shutdown();
		}

		[SetUp]
		public void Setup()
		{
			_tempFolder = new TemporaryFolder("DictionaryTaskTests");
			_filePath = _tempFolder.GetPathForNewTempFile(true);

			WeSayProjectTestHelper.InitializeForTests();
			string[] vernacularWritingSystemIds = new string[]
													  {
															  WritingSystemsIdsForTests.VernacularIdForTest
													  };
			_viewTemplate = new ViewTemplate();
			_viewTemplate.Add(new Field(Field.FieldNames.EntryLexicalForm.ToString(),
										"LexEntry",
										vernacularWritingSystemIds));
			_viewTemplate.Add(new Field("Note",
										"LexEntry",
										new string[] {"en"},
										Field.MultiplicityType.ZeroOr1,
										"MultiText"));
			_lexEntryRepository = new LexEntryRepository(_filePath);

			EntryViewControl.Factory entryViewFactory = (()=>new EntryViewControl());
			_dictControlFactory = (memory=>new DictionaryControl(entryViewFactory, _lexEntryRepository,_viewTemplate, memory, new StringLogger()));

			_taskMemoryRepository = new TaskMemoryRepository();
			_task = new DictionaryTask(_dictControlFactory,  DictionaryBrowseAndEditConfiguration.CreateForTests("definition"),  _lexEntryRepository,
				_taskMemoryRepository);

//            _task = new DictionaryTask( DictionaryBrowseAndEditConfiguration.CreateForTests(),  _lexEntryRepository,
//                _viewTemplate, new TaskMemoryRepository(),   new StringLogger())};//, new UserSettingsForTask());
		}

		[TearDown]
		public void TearDown()
		{
			if (_lexEntryRepository != null)
			{
				_lexEntryRepository.Dispose();
			}
			File.Delete(_filePath);
			WeSayProjectTestHelper.CleanupForTests();
		}

		[Test]
		public void Create()
		{
			Assert.IsNotNull(_task);
		}



		[Test]
		public void CreateAndActivate_TaskMemoryIsEmpty_Ok()
		{
			var task = new DictionaryTask(_dictControlFactory,  DictionaryBrowseAndEditConfiguration.CreateForTests("definition"), _lexEntryRepository,
				new TaskMemoryRepository());
			task.Activate();
			task.Deactivate();
		}

		[Test, Ignore("Failing due to ui teardown issues, which aren't the subject of the test")]
		public void GoToUrl_EntryDoesntExist_OK()
		{
//            var task = new DictionaryTask(DictionaryBrowseAndEditConfiguration.CreateForTests(), _lexEntryRepository,
//                _viewTemplate, new TaskMemoryRepository(), new StringLogger());
			_task.Activate();
			((DictionaryTask)_task).GoToUrl("notThere");
			_task.Deactivate();
		}

		[Test]
		public void CreateAndActivate_LastUrlDoesntExistAnymore_DoesNotCrash()
		{
			DictionaryBrowseAndEditConfiguration config = DictionaryBrowseAndEditConfiguration.CreateForTests("definition");

			_taskMemoryRepository.FindOrCreateSettingsByTaskId(config.TaskName).Set(DictionaryTask.LastUrlKey, "longGone");

#if DEBUG
	  //the code doesn't show the errror box in release builds, but
			//the builder publishing configuration does run tests in release builds
			using (new SIL.Reporting.ErrorReport.NonFatalErrorReportExpected())
#endif
			{
				_task.Activate();
			}
			_task.Deactivate();
		}

       ///rik nunitforms test: [Test]
    //    //[NUnit.Framework.Category("UsesObsoleteExpectedExceptionAttribute"), ExpectedException(typeof (ArgumentNullException))]
    //    public void Create_NullRecordListManager_Throws()
    //    {
    //        new DictionaryTask(DictionaryBrowseAndEditConfiguration.CreateForTests(), null, _viewTemplate, new TaskMemoryRepository(),  new StringLogger());//, new UserSettingsForTask());
    //    }

    //    [Test]
    //    //[NUnit.Framework.Category("UsesObsoleteExpectedExceptionAttribute"), ExpectedException(typeof (ArgumentNullException))]
    //    public void Create_NullviewTemplate_Throws()
    //    {
    //        new DictionaryTask(DictionaryBrowseAndEditConfiguration.CreateForTests(), _lexEntryRepository, null, new TaskMemoryRepository(),  new StringLogger());//, new UserSettingsForTask());
    //    }
	}
}