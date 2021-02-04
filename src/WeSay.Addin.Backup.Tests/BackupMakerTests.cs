using System;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using NUnit.Framework;
using SIL.TestUtilities;
using WeSay.Project;
using WeSay.TestUtilities;

namespace Addin.Backup.Tests
{
	[TestFixture]
	public class BackupMakerTests
	{
		public BackupMaker _backupMaker;
		private string _destinationZip;
		private string _sourceProjectPath;
		private string[] _filesToBackup;

		[SetUp]
		public void Setup()
		{
			WeSayProjectTestHelper.InitializeForTests();
			_destinationZip = Path.Combine(Path.GetTempPath(), Path.GetTempFileName() + ".zip");
			_sourceProjectPath = BasilProject.GetPretendProjectDirectory();
			_backupMaker = new BackupMaker();
			_filesToBackup = WeSayWordsProject.GetFilesBelongingToProject(_sourceProjectPath);
		}

		[TearDown]
		public void TearDown()
		{
			if (File.Exists(_destinationZip))
			{
				File.Delete(_destinationZip);
			}
			WeSayProjectTestHelper.CleanupForTests();
		}

		[Test]
		public void ThrowIfCannotCreateDestination()
		{

			Assert.Throws<ApplicationException>(() =>
			BackupMaker.BackupToExternal(BasilProject.GetPretendProjectDirectory(),
										 "QQ:\\" + Path.GetRandomFileName(),
										 _filesToBackup));
		}

		[Test]
		public void ThrowIfSourceDoesntExist()
		{
			Assert.Throws<ApplicationException>(() => BackupMaker.BackupToExternal(Path.GetRandomFileName(), _destinationZip, _filesToBackup));
		}

		[Test]
		[Ignore("Not implemented yet")]
		public void WhatIfNotEnoughRoom() {}

		[Test]
		public void OverwritesExistingZip()
		{
			string dummyPath = MakeDummyExistingZip();
			try
			{
				BackupMaker.BackupToExternal(_sourceProjectPath, _destinationZip, _filesToBackup);
				AssertHasReasonableContents();
				AssertDoesNotContainFile(dummyPath);
			}
			finally
			{
				File.Delete(dummyPath);
			}
		}

		private void AssertDoesNotContainFile(string partialPath)
		{
			ZipFile f = null;
			try
			{
				f = new ZipFile(_destinationZip);
				Assert.AreEqual(-1, f.FindEntry(GetZipFileInternalPath(partialPath), true));
			}
			finally
			{
				if (f != null)
				{
					f.Close();
				}
			}
		}

		/// <summary>
		/// strip off c:\ and make all slashes forward slashes
		/// </summary>
		private static string GetZipFileInternalPath(string partialPath)
		{
			return partialPath.Substring(3).Replace("\\", "/");
		}

		private string MakeDummyExistingZip()
		{
			ZipFile z = ZipFile.Create(_destinationZip);
			z.BeginUpdate();
			string tempFile = Path.GetTempFileName();
			z.Add(tempFile);
			z.CommitUpdate();
			z.Close();
			File.Delete(tempFile);
			return tempFile;
		}

		[Test]
		public void SelectsCorrectFiles()
		{
			BackupMaker.BackupToExternal(_sourceProjectPath, _destinationZip, _filesToBackup);
			AssertHasReasonableContents();
		}

		[Test]
		public void BackupToExternal_FileToBackUpIsLocked_Throws()
		{
			using (var folderForBackup = new TemporaryFolder("Backup Test"))
			{
				string backUpFileName = Path.Combine(folderForBackup.Path, "Backup Test.zip");

				//Create and lock a lift file
				var fileToBackUp = new TempLiftFile("TempLiftFile.lift", folderForBackup, "", "0.13");

				//This is our lock
				var liftFileStreamForLocking = new FileStream(
					fileToBackUp.Path, FileMode.Open, FileAccess.Read, FileShare.None
				);

				Assert.Throws<System.IO.IOException>(
					() => BackupMaker.BackupToExternal(
						Path.GetDirectoryName(fileToBackUp.Path), backUpFileName, new[] {fileToBackUp.Path}
					)
				);
				liftFileStreamForLocking.Close();
			}
		}

		[Test]
		public void OkIfNoFilesChosen()
		{
			BackupMaker.BackupToExternal(_sourceProjectPath, _destinationZip, new string[] {});
		}

		private void AssertHasReasonableContents()
		{
			Assert.IsTrue(File.Exists(_destinationZip));
			ZipFile f = null;
			try
			{
				f = new ZipFile(_destinationZip);
				Assert.AreNotEqual(-1, f.FindEntry("PRETEND/PRETEND.lift", true));
			}
			finally
			{
				if (f != null)
				{
					f.Close();
				}
			}
		}
	}
}