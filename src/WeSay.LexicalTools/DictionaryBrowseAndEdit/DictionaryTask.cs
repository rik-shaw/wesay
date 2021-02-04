using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using SIL.DictionaryServices.Model;
using SIL.i18n;
using SIL.Reporting;
using WeSay.Foundation;
using WeSay.LexicalModel;
using WeSay.Project;
using WeSay.UI;
using WeSay.LexicalTools.Properties;

/* todo
 *
 * handle:
 *     <filter class="WeSay.LexicalTools.MissingItemFilter" assembly="WeSay.LexicalTools">
		<viewTemplate ref="Default View Template" />
		<field>POS</field>
	  </filter>
 *
 * Deal with parameters that are just strings... do we adjust the paramters to match the
 * config file names?  Stoop to positionalParameters again? What would be nice is attributes on the parameters that we match.
*/

namespace WeSay.LexicalTools.DictionaryBrowseAndEdit
{
	public class DictionaryTask: TaskBase, ITaskForExternalNavigateToEntry
	{
		private DictionaryControl _dictionaryControl;
		//private readonly ViewTemplate _viewTemplate;
		//private readonly ILogger _logger;
		private TaskMemory _taskMemory;
		//private string _pendingNavigationUrl;
		DictionaryControl.Factory _dictionaryControlFactory;

		public const string LastUrlKey = "lastUrl";

		public DictionaryTask(DictionaryControl.Factory dictionaryControlFactory,
								DictionaryBrowseAndEditConfiguration config,
								LexEntryRepository lexEntryRepository,
								TaskMemoryRepository taskMemoryRepository)
			: base(config, lexEntryRepository, taskMemoryRepository)
		{
			_dictionaryControlFactory = dictionaryControlFactory;
//            if (viewTemplate == null)
//            {
//                throw new ArgumentNullException("viewTemplate");
//            }
//            _viewTemplate = viewTemplate;
//            _logger = logger;
			_taskMemory = taskMemoryRepository.FindOrCreateSettingsByTaskId(config.TaskName);

		}

		public override void Activate()
		{
			try
			{
				base.Activate();
			   // _dictionaryControl = new DictionaryControl(LexEntryRepository, ViewTemplate, _taskMemory.CreateNewSection("view"), _logger);
				var temp = _taskMemory.CreateNewSection("view");
				_dictionaryControl = _dictionaryControlFactory(temp);

				 _dictionaryControl.SelectedIndexChanged += new EventHandler(OnSelectedEntryOfDictionaryControlChanged);
//   Debug.Assert(_userSettings.Get("one", "0") == "1");

				var url = _taskMemory.Get(LastUrlKey, null);
				if (_taskMemory != null && url != null)
				{
					try
					{
						  _dictionaryControl.GoToUrl(url);
					}
					catch (Exception error)
					{
						//there's no scenario where it is worth crashing or even notifying
						Logger.WriteEvent("Error: " + error.Message);
#if DEBUG
						ErrorReport.NotifyUserOfProblem(error,"Could not find the entry at '{0}'\r\n{1}", url,error.Message);
#endif
					}
				}
				else
				{
					_dictionaryControl.GotoFirstEntry();
				}
			}
			catch (ConfigurationException)
			{
				IsActive = false;
				throw;
			}

#if DEBUG
			//Thread.Sleep(5000);
#endif
		}

		void OnSelectedEntryOfDictionaryControlChanged(object sender, EventArgs e)
		{
			LexEntry entry = _dictionaryControl.CurrentEntry;
			if(entry !=null)
			{
				_taskMemory.Set(LastUrlKey, _dictionaryControl.CurrentUrl);
			}
		}

		public override void Deactivate()
		{
			base.Deactivate();
			_dictionaryControl.Dispose();
			_dictionaryControl = null;
		}

		public string CurrentUrl
		{
			get
			{
				if (IsActive)
				{
					return _dictionaryControl.CurrentUrl;
				}
				return "NOTACTIVE";// string.Empty;
			}
		}

		public override void GoToUrl(string url)
		{
			try
			{
				if (IsActive) //activation may be delayed via a timer, if so, just file this away until we are ready for it
				{
					_dictionaryControl.GoToUrl(url);
				}
				else
				{
					if (_taskMemory != null)
					{
						_taskMemory.Set(LastUrlKey, url);
					}
				}
			}
			catch (Exception error)
			{
				SIL.Reporting.ErrorReport.NotifyUserOfProblem("Could not navigate to {0}. {1}", url, error.Message);
			}
		}


		/// <summary>
		/// The entry detail control associated with this task
		/// </summary>
		/// <remarks>Non null only when task is activated</remarks>
		public override Control Control
		{
			get { return _dictionaryControl; }
		}

		public override string Description
		{
			get
			{
				return

						StringCatalog.GetFormatted("~See all {0} {1} words.",
										  "The description of the 'Dictionary' task.  In place of the {0} will be the number of words in the dictionary.  In place of the {1} will be the name of the project.",
							ComputeCount(true),
							BasilProject.Project.Name);
			}
		}
//
//        public ViewTemplate ViewTemplate
//        {
//            get { return _viewTemplate; }
//        }

		protected override int ComputeCount(bool returnResultEvenIfExpensive)
		{
			return LexEntryRepository.CountAllItems();
		}

		protected override int ComputeReferenceCount()
		{
			return CountNotRelevant;
		}

		public override void FocusDesiredControl()
		{
			// This is the place to implement how the task selects its desired child control
			return;
		}

		public override ButtonStyle DashboardButtonStyle
		{
			get { return ButtonStyle.IconFixedWidth; }
		}

		public override Image DashboardButtonImage
		{
			get { return Resources.blueDictionary; }
		}
	}
}