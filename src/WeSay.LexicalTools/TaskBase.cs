using System;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using SIL.i18n;
using WeSay.Foundation;
using WeSay.LexicalModel;
using WeSay.Project;

namespace WeSay.LexicalTools
{

	public abstract class TaskBase: ITask
	{
		public const int CountNotRelevant = -1;
		public const int CountNotComputed = -2;

		private readonly LexEntryRepository _lexEntryRepository;
		private readonly string _label;
		private readonly string _longLabel;
		private readonly string _description;
		private readonly string _remainingCountText;
		private readonly string _referenceCountText;
		private readonly bool _isPinned;
		private int _remainingCount;
		private int _referenceCount;

		public TaskBase(ITaskConfiguration config,
						LexEntryRepository lexEntryRepository,
						TaskMemoryRepository taskMemoryRepository)
		{
			if (config.Label == null)
			{
				throw new ArgumentNullException("label");
			}
			if (config.LongLabel == null)
			{
				throw new ArgumentNullException("longLabel");
			}
			if (config.Description == null)
			{
				throw new ArgumentNullException("description");
			}
			if (config.RemainingCountText == null)
			{
				throw new ArgumentNullException("remainingCountText");
			}
			if (config.ReferenceCountText == null)
			{
				throw new ArgumentNullException("referenceCountText");
			}
			if (lexEntryRepository == null)
			{
				throw new ArgumentNullException("lexEntryRepository");
			}
			_lexEntryRepository = lexEntryRepository;
			_taskMemory = taskMemoryRepository.FindOrCreateSettingsByTaskId(config.TaskName);

			// convert any amount of whitespace to one space
			Regex rgx = new Regex("\\s+");
			_label = rgx.Replace (config.Label.Trim(), " ");
			_longLabel = rgx.Replace (config.LongLabel.Trim(), " ");
			_description = rgx.Replace (config.Description.Trim(), " ");
			_remainingCountText = rgx.Replace (config.RemainingCountText.Trim(), " ");
			_referenceCountText = rgx.Replace (config.ReferenceCountText.Trim(), " ");
			_isPinned = config.IsPinned;

//            _cachePath = WeSayWordsProject.Project.PathToCache;
//            _cacheFilePath = Path.Combine(_cachePath, MakeSafeName(Label + ".cache"));
//
//            ReadCacheFile();

			_remainingCount = _taskMemory.Get("RemainingCount", CountNotComputed);
			_referenceCount =  _taskMemory.Get("ReferenceCount", CountNotComputed);

		}



		public virtual string Description
		{
			get { return StringCatalog.Get(_description); }
		}

		private bool _isActive;
		private TaskMemory _taskMemory;

		public virtual void Activate()
		{
			if (IsActive)
			{
				return;
				//the following is reasonable, but after 4 hours, I (JH)
				//gave up on trying to get a cross-process unit test (JumpToEntryMakesAppSwitchToUIMode)
				//to not run afoul of it
				throw new InvalidOperationException(
						"Activate should not be called when object is active.");
			}
			IsActive = true;
		}

		public virtual bool Available
		{
			get { return true; }
		}


//        private static string MakeSafeName(string fileName)
//        {
//            foreach (char invalChar in Path.GetInvalidFileNameChars())
//            {
//                fileName = fileName.Replace(invalChar.ToString(), "");
//            }
//            return fileName;
//        }

//        private void WriteCacheFile()
//        {
//            try
//            {
//                if (!Directory.Exists(_cachePath))
//                {
//                    Directory.CreateDirectory(_cachePath);
//                }
//                using (StreamWriter sw = File.CreateText(_cacheFilePath))
//                {
//                    sw.Write(_remainingCount + ", " + _referenceCount);
//                }
//            }
//            catch
//            {
//                Console.WriteLine("Could not write cache file: " + _cacheFilePath);
//            }
//        }

//        private void ReadCacheFile()
//        {
//            _remainingCount = CountNotComputed;
//            _referenceCount = CountNotComputed;
//            try
//            {
//                if (File.Exists(_cacheFilePath))
//                {
//                    using (StreamReader sr = new StreamReader(_cacheFilePath))
//                    {
//                        string s;
//                        s = sr.ReadToEnd();
//                        string[] values = s.Split(',');
//                        if (values.Length > 1) //old style didn't have reference
//                        {
//                            bool gotIt = int.TryParse(values[1], out _referenceCount);
//                            Debug.Assert(gotIt);
//                        }
//                        if (values.Length > 0) //old style didn't have reference
//                        {
//                            bool gotIt = int.TryParse(values[0], out _remainingCount);
//                            Debug.Assert(gotIt);
//                        }
//                    }
//                }
//            }
//            catch
//            {
//                // Console.WriteLine("Could not read cache file: " + cacheFilePath);
//            }
//        }

		public virtual void Deactivate()
		{
			if (!IsActive)
			{
				throw new InvalidOperationException(
						"Deactivate should only be called once after Activate.");
			}
			IsActive = false;
		}

		#region ITask Members

		public virtual void GoToUrl(string url)
		{
			throw new NotImplementedException();
		}

		#endregion

		protected void VerifyTaskActivated()
		{
			if (!IsActive)
			{
				throw new InvalidOperationException("Task must be activated first");
			}
		}

		public string Label
		{
			get { return _label; }
		}

		/// <summary>
		/// The control associated with this task
		/// </summary>
		/// <remarks>Non null only when task is activated</remarks>
		public abstract Control Control { get; }

		public bool IsPinned
		{
			get { return _isPinned; }
		}

		/// <summary>
		/// Gives a sense of how much work is left to be done
		/// </summary>
		public int GetRemainingCount()
		{
			int count = ComputeCount(true);
			if (count != CountNotComputed)
			{
				_remainingCount = count;
				_taskMemory.Set("RemainingCount", _remainingCount); //WriteCacheFile();

			}
			return _remainingCount;
		}

		/// <summary>
		/// Should Return CountNotComputed if expensive to figure out
		/// and force==false
		/// </summary>
		/// <returns>An integer indicating how much work is left to do</returns>
		protected abstract int ComputeCount(bool returnResultEvenIfExpensive);

		public int ExactCount
		{
			get
			{
				_remainingCount = ComputeCount(true);
				_taskMemory.Set("RemainingCount", _remainingCount); //WriteCacheFile();
				return _remainingCount;
			}
		}

		/// <summary>
		/// Gives a sense of the overall size of the task
		/// </summary>
		public int GetReferenceCount()
		{
			int count = ComputeReferenceCount();
			if (count != CountNotComputed)
			{
				_referenceCount = count;
				_taskMemory.Set("ReferenceCount", _referenceCount); //WriteCacheFile();
			}

			return _referenceCount;
		}

		/// <summary>
		/// Should Return CountNotComputed if expensive to figure out
		/// </summary>
		/// <returns>
		/// An integer indicating how much work there is total
		/// </returns>
		protected abstract int ComputeReferenceCount();

		public string GetRemainingCountText()
		{
			string result = _remainingCountText.Trim();
			int remainingCount = GetRemainingCount();
			if (result == string.Empty)
			{
				result = StringCatalog.Get("~Remaining:",
										   "Generic desciption of how many items in a task are left to do.");
			}
			if (remainingCount >= 0)
			{
				result += " " + remainingCount;
			}
			else
			{
				result = string.Empty;
			}
			return result;
		}

		public string GetReferenceCountText()
		{
			string result = _referenceCountText.Trim();
			int referenceCount = GetReferenceCount();
			if (result == string.Empty)
			{
				result = StringCatalog.Get("~Total:",
										   "Generic description of how many total items there are in a task.");
			}
			if (referenceCount >= 0)
			{
				result += " " + referenceCount;
			}
			else
			{
				result = string.Empty;
			}
			return result;
		}

		public abstract void FocusDesiredControl();

		public bool AreCountsRelevant()
		{
			return GetReferenceCount() != CountNotRelevant &&
				   GetRemainingCount() != CountNotRelevant;
		}

		protected LexEntryRepository LexEntryRepository
		{
			get { return _lexEntryRepository; }
		}

		public bool IsActive
		{
			get { return _isActive; }
			protected set { _isActive = value; }
		}

		#region IThingOnDashboard Members

		public virtual DashboardGroup Group
		{
			get { return DashboardGroup.Describe; }
		}

		public string LocalizedLabel
		{
			get { return StringCatalog.Get(_label); }
		}

		public string LocalizedLongLabel
		{
			get { return StringCatalog.Get(_longLabel); }
		}

		public virtual ButtonStyle DashboardButtonStyle
		{
			get { return ButtonStyle.VariableAmount; }
		}

		public virtual Image DashboardButtonImage
		{
			get { return null; }
		}

		#endregion
	}
}