using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Mono.Addins;
using SIL.i18n;
using SIL.Progress;
using WeSay.AddinLib;
using WeSay.LexicalModel;
using System.Linq;

namespace Addin.Transform
{
	[Extension]
	public class SfmTransformer: LiftTransformer, IWeSayAddinHasSettings
	{
		private SfmTransformSettings _settings;

		public SfmTransformer()
		{
			_settings = new SfmTransformSettings();
		}

		public override string LocalizedName
		{
			get { return StringCatalog.Get("~Export To SFM"); }
		}

		public override string LocalizedLabel
		{
			get { return LocalizedName; }
		}

		public override string LocalizedLongLabel
		{
			get { return LocalizedName; }
		}

		public override string Description
		{
			get { return StringCatalog.Get("~Saves the lexicon in a form of standard format."); }
		}

		public override Image ButtonImage
		{
			get { return Resources.SfmTransformerButtonImage; }
		}

		public override Image DashboardButtonImage
		{
			get { return Resources.SfmTransformerButtonImage; }
		}

		/// <summary>
		/// this runs in a worker thread
		/// </summary>
		private static void OnDoGrepWork(object sender, DoWorkEventArgs args)
		{
			var progressState = (ProgressState) args.Argument;
			var workerArguments = (TransformWorkerArguments) (progressState.Arguments);

			progressState.StatusLabel = "Converting to MDF (can take a very long time)...";
			//System.Threading.Thread.Sleep(100);//don't event see that message otherwise
			GrepFile(workerArguments.outputFilePath, args);
		}

		private static void GrepFile(string inputPath, DoWorkEventArgs args)
		{
			var progressState = (ProgressState) args.Argument;
			var workerArguments = (TransformWorkerArguments) (progressState.Arguments);
			var sfmSettings = (SfmTransformSettings) workerArguments.postTransformArgument;
			int entriesCount = workerArguments.inputDocument.SelectNodes("//entry").Count;

			string tempPath = inputPath + ".tmp";
			IEnumerable<SfmTransformSettings.ChangePair> pairs = sfmSettings.ChangePairs;

			if(!pairs.Any())
			{
				return;
			}

			using (StreamReader reader = File.OpenText(inputPath))
			{
				using (var writer = new StreamWriter(tempPath))
				{
					if (progressState.Cancel)
					{
						return;
					}
					//we don't have a way of knowing      progressState.NumberOfStepsCompleted = ;
					int count = 0;
					foreach (string r in BreakUpSfmIntoRecords(reader))
					{
						string record = r;
						foreach (SfmTransformSettings.ChangePair pair in pairs)
						{
							record = pair.DoChange(record);
						}
						writer.Write(record);
						progressState.NumberOfStepsCompleted++;
						count++;
						if(count % 20 ==0 )
							progressState.StatusLabel = "Converting to MDF: "+count + "/" + entriesCount;
					}
					writer.Close();
				}
				reader.Close();
			}

			//System.Threading.Thread.Sleep(500);//don't event see that message otherwise
			progressState.StatusLabel = "Cleaning up...";
			progressState.NumberOfStepsCompleted++;
			File.Delete(inputPath);
			File.Move(tempPath, inputPath); //, backupPath);
			progressState.NumberOfStepsCompleted = progressState.TotalNumberOfSteps;
			Thread.Sleep(500); //don't event see that message otherwise
		}

		static private IEnumerable<string> BreakUpSfmIntoRecords(StreamReader reader)
		{
			var record = new StringBuilder();

			string line = "";
			while (!reader.EndOfStream)
			{
				line = reader.ReadLine();
				if(line != Environment.NewLine)
				{
					if (line.StartsWith("\\dt "))
					{
						line = ConvertDateLineToToolboxFormat(line);
					}
					record.Append(line);
					record.Append(Environment.NewLine);
				}
				if(reader.EndOfStream || line.Length==0)
				{
					yield return record.ToString();
					record = new StringBuilder();
				}
			}
		}

		private static string ConvertDateLineToToolboxFormat(string line)
		{
			string[] parts = line.Split(' ');
			if (parts.Length > 1)
			{
				DateTime dt = DateTime.Parse(parts[1]);
				line = parts[0] + " " + dt.ToString(@"d\/MMM\/yyyy");
			}
			return line;
		}

		public override void Launch(Form parentForm, ProjectInfo projectInfo)
		{
			_settings.FillEmptySettingsWithGuesses(projectInfo);
			SetupPostTransformMethod(OnDoGrepWork, _settings, 10 /*has some cushion*/);
			//LexEntryRepository repo = projectInfo.ServiceProvider.GetService(typeof (LexEntryRepository)) as LexEntryRepository;
			string output = TransformLiftToText(projectInfo, "lift2sfm.xsl", "-sfm.txt");

			if (string.IsNullOrEmpty(output))
			{
				return; // get this when the user cancels
			}

			// check that sfm file has no problem references - see WS-356
			// a problem reference is the full guid for the word rather than just the word
			// this happens if the reference in Lift is written out as NFD
			// e.g. \lf confer = me^_d9c25d1f-d373-4995-9ffa-ae2cf650603c
			// (me^ is not really NFD but indicates that if you look at the text in less then it will be 2 characters not 1)
			bool has_problems = SIL.IO.FileHelper.Grep(output, @"\\lf\ confer\ =\ (.+)_([0-9a-f\-]+)");

			// if it has any then touch all cross references and rerun transform
			// touching the cross reference will save it in NFC in Lift
			if (has_problems)
			{
				WeSay.Project.WeSayWordsProject.Project.TouchAllIfCrossReferences();
				output = TransformLiftToText(projectInfo, "lift2sfm.xsl", "-sfm.txt");
			}

			if (string.IsNullOrEmpty(output))
			{
				return; // get this when the user cancels
			}
			//GrepFile(output, _settings);

			if (_launchAfterTransform)
			{
				Process.Start(output);
			}
		}

		#region IWeSayAddinHasSettings Members

		public bool DoShowSettingsDialog(Form parentForm, ProjectInfo projectInfo)
		{
			var dlg = new SFMChangesDialog(_settings, projectInfo);
			return dlg.ShowDialog(parentForm) == DialogResult.OK;
		}

		public object Settings
		{
			get { return _settings; }
			set { _settings = (SfmTransformSettings) value; }
		}

		public override string ID
		{
			get { return "Export To SFM"; }
			//CAN'T CHANGE THIS WITHOUT PROVIDING A MIGRATION FOR FOLKS!
		}

		#endregion
	}
}