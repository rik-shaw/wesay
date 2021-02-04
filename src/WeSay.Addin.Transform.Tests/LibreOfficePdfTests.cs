using System;
using System.IO;
using System.Diagnostics;
using SIL.Reporting;
using SIL.TestUtilities;
using SIL.WritingSystems;
using Addin.Transform.PdfDictionary;
using WeSay.Project;
using WeSay.Project.Tests;
using WeSay.AddinLib;

using NUnit.Framework;
using System.Threading;

namespace Addin.Transform.Tests
{
	[TestFixture]
	public class LibreOfficePdfTests
	{
		private class EnvironmentForTest : IDisposable
		{
			private readonly ProjectDirectorySetupForTesting _testProject;
			private readonly WeSayWordsProject _project;
			private readonly ProjectInfo _projectInfo;

			public EnvironmentForTest()
			{
				ErrorReport.IsOkToInteractWithUser = false;
				Sldr.Initialize(true);
				const string xmlOfEntries = @" <entry id='foo1'>
						<lexical-unit><form lang='qaa'><text>hello</text></form></lexical-unit>
					</entry>";
				_testProject = new ProjectDirectorySetupForTesting(xmlOfEntries);
				_project = _testProject.CreateLoadedProject();
				_projectInfo = _project.GetProjectInfoForAddin();

				string sourceTemplateDir = Path.Combine(_projectInfo.PathToApplicationRootDirectory, String.Format("..{0}..{0}templates", Path.DirectorySeparatorChar));
				TestUtilities.DeleteFolderThatMayBeInUse(OutputTemplateDir);
				CopyFolder(sourceTemplateDir, OutputTemplateDir);
			}

			public ProjectInfo ProjectInfo
			{
				get { return _projectInfo; }
			}

			private string OutputTemplateDir
			{
				get { return Path.Combine(_projectInfo.PathToApplicationRootDirectory, "templates"); }
			}

			public string OdtFile
			{
				get { return Path.Combine(_projectInfo.PathToExportDirectory, _projectInfo.Name + ".odt"); }
			}

			public string OdtContent
			{
				get { return Path.Combine(_projectInfo.PathToExportDirectory, "content.xml"); }
			}

			public string OdtStyles
			{
				get { return Path.Combine(_projectInfo.PathToExportDirectory, "styles.xml"); }
			}

			public string PdfFile
			{
				get { return Path.Combine(_projectInfo.PathToExportDirectory, _projectInfo.Name + ".pdf"); }
			}

			public void Dispose ()
			{
				_project.Dispose();
				_testProject.Dispose();
				Sldr.Cleanup();
				TestUtilities.DeleteFolderThatMayBeInUse(OutputTemplateDir);
			}

			private static void CopyFolder(string sourceFolder, string destFolder)
			{
				if (!Directory.Exists(destFolder))
					Directory.CreateDirectory(destFolder);
				string[] files = Directory.GetFiles(sourceFolder);
				foreach (string file in files)
				{
					string name = Path.GetFileName(file);
					string dest = Path.Combine(destFolder, name);
					File.Copy(file, dest);
				}
				string[] folders = Directory.GetDirectories(sourceFolder);
				foreach (string folder in folders)
				{
					string name = Path.GetFileName(folder);
					string dest = Path.Combine(destFolder, name);
					CopyFolder(folder, dest);
				}
			}
		}

		[Test]
		[Category("SkipOnTeamCity")]
		[Platform(Exclude="Win", Reason="Mono only test")]
		public void TestOpenDocumentPdf()
		{
			using (var e = new EnvironmentForTest())
			{
				var addin = new LibreOfficePdf();
				addin.LaunchAfterExport= false;

				addin.Launch(null,  e.ProjectInfo);
				if (!File.Exists(e.PdfFile))
				{
					e.Dispose();
					throw new ApplicationException("Did not create pdf " + e.PdfFile);
				}
				bool succeeded = (new FileInfo(e.PdfFile).Length > 0);
				if (!succeeded)
				{
					e.Dispose ();
					throw new ApplicationException("Pdf " + e.PdfFile + " was empty");
				}
			}
		}

		[Test]
		[Category("SkipOnTeamCity")]
		[Platform(Exclude="Win", Reason="Mono only test")]
		public void TestOpenDocumentPdfOpen()
		{
			using (var e = new EnvironmentForTest())
			{
				var addin = new LibreOfficePdf();

				addin.Launch(null,  e.ProjectInfo);
				Assert.IsTrue(File.Exists(e.PdfFile));
				bool succeeded = (new FileInfo(e.PdfFile).Length > 0);
				Assert.IsTrue(succeeded);
				Thread.Sleep(1000);
			}
		}

		[Test]
		public void LONotInstalledCheckDoesNotThrow()
		{
			using (var e = new EnvironmentForTest())
			{
				var addin = new LibreOfficePdf();
				Assert.DoesNotThrow(
					() => { if (addin.Available) {};  }
				);
			}
		}

		[Test]
		public void LONotInstalledThrowsConfigurationException()
		{
			using (var e = new EnvironmentForTest())
			{
				var addin = new LibreOfficePdf();
				if (addin.Available)
				{
					Assert.DoesNotThrow
						(
						() => { addin.Launch(null,  e.ProjectInfo); }
					);
				}
				else {

					Assert.Throws<ConfigurationException>(
						() => { addin.Launch(null,  e.ProjectInfo); }
					);
				}
			}
		}

	}
}
