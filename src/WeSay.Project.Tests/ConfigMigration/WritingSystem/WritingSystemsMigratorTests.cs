﻿using System;
using System.IO;
using System.Xml;
using NUnit.Framework;
using SIL.IO;
using SIL.TestUtilities;
using SIL.WritingSystems;
///rik using SIL.WritingSystems.Tests;
using WeSay.Project.ConfigMigration.WritingSystem;
using WeSay.TestUtilities;

namespace WeSay.Project.Tests.ConfigMigration.WritingSystem
{
	[TestFixture]
	public class WritingSystemsMigratorTests
	{
		private class TestEnvironment : IDisposable
		{
			private readonly TemporaryFolder _folder;
			private readonly XmlNamespaceManager _namespaceManager;
			private readonly TempFile _configFile;
			private readonly TempFile _liftFile;

			public TestEnvironment()
			{
				_folder = new TemporaryFolder("WritingSystemsMigratorTests");
				Sldr.Initialize(true);
				_namespaceManager = new XmlNamespaceManager(new NameTable());
				_namespaceManager.AddNamespace("palaso", "urn://palaso.org/ldmlExtensions/v1");
				var pathToConfigFile = Path.Combine(_folder.Path, "test.WeSayConfig");
				_configFile = new TempFile(_configFileContent);
				_configFile.MoveTo(pathToConfigFile);
				var pathtoLiftFile = Path.Combine(_folder.Path, "test.lift");
				_liftFile = new TempFile(_liftFileContent);
				_liftFile.MoveTo(pathtoLiftFile);
			}

			//This config file was created by opening WeSay 0.9.69 Config tool and turning on every option that I could find that might insert a writingsystem into the config file. Then I removed any redundancies for brevity sake. This probably means that the config file here would not load in WeSay but it contains the relevant xml for writingsystems
			private readonly string _configFileContent =
			#region LongFileContent
 @"<?xml version='1.0' encoding='utf-8'?>
<configuration
	version='8'>
	<components>
		<viewTemplate>
			<fields>
				<field>
					<className>LexEntry</className>
					<dataType>MultiText</dataType>
					<displayName>Word</displayName>
					<enabled>True</enabled>
					<fieldName>EntryLexicalForm</fieldName>
					<multiParagraph>False</multiParagraph>
					<spellCheckingEnabled>False</spellCheckingEnabled>
					<multiplicity>ZeroOr1</multiplicity>
					<visibility>Visible</visibility>
					<writingSystems>
						<id>bogusws1</id>
						<id>bogusws2</id>
					</writingSystems>
				</field>
			</fields>
			<id>Default View Template</id>
		</viewTemplate>
	</components>
	<tasks>
		<task
			taskName='Dashboard'
			visible='true' />
		<task
			taskName='Dictionary'
			visible='true' />
		<task
			taskName='AddMissingInfo'
			visible='true'>
			<label>Meanings</label>
			<longLabel>Add Meanings</longLabel>
			<description>Add meanings (senses) to entries where they are missing.</description>
			<field>definition</field>
			<showFields>definition</showFields>
			<readOnly>semantic-domain-ddp4</readOnly>
			<writingSystemsToMatch>bogusws1, bogusws2</writingSystemsToMatch>
			<writingSystemsWhichAreRequired>bogusws1, bogusws2</writingSystemsWhichAreRequired>
		</task>
	</tasks>
</configuration>".Replace("\"", "'");
			#endregion

			private readonly string _liftFileContent =
			#region LongFileContent
 @"<?xml version='1.0' encoding='utf-8'?>
<lift
	version='0.13'
	producer='WeSay 1.0.0.0'>
	<entry
		id='chùuchìi mǔu rɔ̂ɔp_dd15cbc4-9085-4d66-af3d-8428f078a7da'
		dateCreated='2008-11-03T06:17:24Z'
		dateModified='2009-10-12T04:05:40Z'
		guid='dd15cbc4-9085-4d66-af3d-8428f078a7da'>
		<lexical-unit>
			<form
				lang='bogusws1'>
				<text>chùuchìi mǔu krɔ̂ɔp</text>
			</form>
			<form
				lang='bogusws2'>
				<text>ฉู่ฉี่หมูรอบ</text>
			</form>
		</lexical-unit>
	</entry>
</lift>".Replace("'", "\"");
#endregion

			public string ProjectPath
			{
				get { return _folder.Path; }
			}

			public void Dispose()
			{
				Sldr.Cleanup();
				_configFile.Dispose();
				_folder.Dispose();
			}

			private string WritingSystemsPath
			{
				get { return Path.Combine(ProjectPath, "WritingSystems"); }
			}

			private string WritingSystemsOldPrefsFilePath
			{
				get { return Path.Combine(ProjectPath, "WritingSystemPrefs.xml"); }
			}

			public XmlNamespaceManager NamespaceManager
			{
				get { return _namespaceManager; }
			}

			public string PathToConfigFile
			{
				get { return _configFile.Path; }
			}

			public string PathToLiftFile
			{
				get { return _liftFile.Path; }
			}

			public string WritingSystemFilePath(string tag)
			{
				return Path.Combine(WritingSystemsPath, String.Format("{0}.ldml", tag));
			}

			public void WriteToPrefsFile(string content)
			{
				File.WriteAllText(WritingSystemsOldPrefsFilePath, content);
			}
		}

		[Test]
		public void MigrateIfNeeded_ConfigFileContainsWritingSystemIdThatIsMigrated_WritingSystemIdIsChanged()
		{
			using (var e = new TestEnvironment())
			{
				e.WriteToPrefsFile(WritingSystemPrefsFileContent.TwoWritingSystems("bogusws1", "bogusws2"));
				var migrator = new WritingSystemsMigrator(e.ProjectPath);
				migrator.MigrateIfNecessary();
				AssertThatXmlIn.File(e.PathToConfigFile).HasAtLeastOneMatchForXpath(
						"/configuration/components/viewTemplate/fields/field/writingSystems/id[1][text()='qaa-x-bogusws1']"
					);
				AssertThatXmlIn.File(e.PathToConfigFile).HasAtLeastOneMatchForXpath(
						"/configuration/components/viewTemplate/fields/field/writingSystems/id[2][text()='qaa-x-bogusws2']"
					);
				AssertThatXmlIn.File(e.PathToConfigFile).HasAtLeastOneMatchForXpath(
						"/configuration/tasks/task/writingSystemsToMatch[text()='qaa-x-bogusws1, qaa-x-bogusws2']"
					);
				AssertThatXmlIn.File(e.PathToConfigFile).HasAtLeastOneMatchForXpath(
						"/configuration/tasks/task/writingSystemsWhichAreRequired[text()='qaa-x-bogusws1, qaa-x-bogusws2']"
					);
			}
		}

		// This test is bogus, it is testing the writing systems migration with the config file which is not related.
		// Maybe could have a test that checks that the wsprefs fails if it's too new, and also the ldml migrator fails
		// if too new (but this should be in palaso).
		//[Test]
		//public void MigrateIfNeeded_ConfigFileIsVersionOtherThanWhatWeKnowTheWritingSystemMigratorCanChange_Throws()
		//{
		//    using (var e = new TestEnvironment())
		//    {
		//        const int versionTooNew = ConfigFile.LatestVersion + 1;
		//        WriteStringToFileAtXpath(e.PathToConfigFile, "//configuration[@version]", versionTooNew.ToString());

		//        e.WriteToPrefsFile(WritingSystemPrefsFileContent.TwoWritingSystems("bogusws1", "bogusws2"));
		//        var migrator = new WritingSystemsMigrator(e.ProjectPath);
		//        Assert.Throws<ConfigurationFileTooNewException>(migrator.MigrateIfNecessary);
		//    }
		//}

		private static void WriteStringToFileAtXpath(string pathtoFile, string xPath, string valueToWrite)
		{
			var file = new XmlDocument();
			file.Load(pathtoFile);
			XmlNode versionNode = file.SelectSingleNode(xPath);
			versionNode.Attributes[0].Value = valueToWrite;
			file.Save(pathtoFile);
		}

		[Test]
		public void MigrateIfNeeded_LiftFileContainsWritingSystemIdThatIsMigrated_WritingSystemIdIsChanged()
		{
			using (var e = new TestEnvironment())
			{
				e.WriteToPrefsFile(WritingSystemPrefsFileContent.TwoWritingSystems("bogusws1", "bogusws2"));
				var migrator = new WritingSystemsMigrator(e.ProjectPath);
				migrator.MigrateIfNecessary();
				var doc = new XmlDocument();
				doc.Load(e.PathToLiftFile);
				XmlNodeList nodes = doc.SelectNodes("//@lang");

				Assert.AreEqual("qaa-x-bogusws1", nodes.Item(0).InnerText);
				Assert.AreEqual("qaa-x-bogusws2", nodes.Item(1).InnerText);

				//The below doesn't work correctly unfortunately
				//AssertThatXmlIn.File(e.PathToLiftFile).HasAtLeastOneMatchForXpath(
				//        "//[@lang[1]='x-bogusws1']"
				//    );
				//AssertThatXmlIn.File(e.PathToLiftFile).HasAtLeastOneMatchForXpath(
				//        "//[@lang[2]='x-bogusws2']"
				//    );
			}
		}

		[Test]
		public void MigrateIfNeeded_OptionListContainsWritingSystemIdThatIsMigrated_WritingSystemIdIsChanged()
		{
			using (var e = new TestEnvironment())
			{
				e.WriteToPrefsFile(WritingSystemPrefsFileContent.TwoWritingSystems("bogusws1", "bogusws2"));
				var optionListPath = Path.Combine(e.ProjectPath, "options.xml");
				File.WriteAllText(optionListPath, OptionListFileContent.GetOptionListWithWritingSystems("bogusws1", "bogusws2"));
				var migrator = new WritingSystemsMigrator(e.ProjectPath);
				migrator.MigrateIfNecessary();
				var doc = new XmlDocument();
				doc.Load(optionListPath);
				XmlNodeList nameNodes = doc.SelectNodes("//name/form/@lang");
				Assert.AreEqual("qaa-x-bogusws1", nameNodes.Item(0).InnerText);
				Assert.AreEqual("qaa-x-bogusws2", nameNodes.Item(1).InnerText);
				nameNodes = doc.SelectNodes("//abbreviation/form/@lang");
				Assert.AreEqual("qaa-x-bogusws1", nameNodes.Item(0).InnerText);
				Assert.AreEqual("qaa-x-bogusws2", nameNodes.Item(1).InnerText);
			}
		}

		[Test]
		//OptionLists can have any file name it seems. So we need to make sure that we don't
		//choke on load and don't change anything.
		public void MigrateIfNeeded_FileIsNotOptionList_LeftAlone()
		{
			using (var e = new TestEnvironment())
			{
				const string optionListContent = "Just some text in a file.";
				e.WriteToPrefsFile(WritingSystemPrefsFileContent.TwoWritingSystems("bogusws1", "bogusws2"));
				var optionListPath = Path.Combine(e.ProjectPath, "options.xml");
				File.WriteAllText(optionListPath, optionListContent);
				var migrator = new WritingSystemsMigrator(e.ProjectPath);
				migrator.MigrateIfNecessary();
				Assert.AreEqual(optionListContent, File.ReadAllText(optionListPath));
			}
		}

		[Test]
		public void MigrateIfNeeded_PrefsFileContainsIdThatIsMigrated_WritingSystemChangeLogIsUpdated()
		{
			using (var e = new TestEnvironment())
			{
				const string language = "en";
				e.WriteToPrefsFile(WritingSystemPrefsFileContent.SingleWritingSystem(language, language, "", "", "", 12, false, language, "", false, true));
				string writingSystemsPath = Path.Combine(e.ProjectPath, "WritingSystems");
				string idChangeLogFilePath = Path.Combine(writingSystemsPath, "idchangelog.xml");
				Directory.CreateDirectory(writingSystemsPath);
				var migrator = new WritingSystemsMigrator(e.ProjectPath);
				migrator.MigrateIfNecessary();
				AssertThatXmlIn.File(idChangeLogFilePath).HasAtLeastOneMatchForXpath("/WritingSystemChangeLog/Changes/Change/To[text()='en-Zxxx-x-audio']");
			}
		}

		[Test]
		public void MigrateIfNeeded_PrefsFileContainsIdThatIsNotMigrated_WritingSystemChangeLogDoesNotExist()
		{
			using (var e = new TestEnvironment())
			{
				e.WriteToPrefsFile(WritingSystemPrefsFileContent.SingleWritingSystemForLanguage("en"));
				string writingSystemsPath = Path.Combine(e.ProjectPath, "WritingSystems");
				string idChangeLogFilePath = Path.Combine(writingSystemsPath, "idchangelog.xml");
				Directory.CreateDirectory(writingSystemsPath);
				var migrator = new WritingSystemsMigrator(e.ProjectPath);
				migrator.MigrateIfNecessary();
				// The change log does not exist because no id needed migrating
				Assert.That(File.Exists(idChangeLogFilePath), Is.Not.True);
			}
		}


		[Test]
		public void MigrateIfNeeded_LdmlV0ContainsIdThatIsNotMigrated_WritingSystemChangeLogDoesNotExist()
		{
			using (var e = new TestEnvironment())
			{
				string writingSystemsPath = Path.Combine(e.ProjectPath, "WritingSystems");
				string ldmlFilePath = Path.Combine(writingSystemsPath, "en.ldml");
				string idChangeLogFilePath = Path.Combine(writingSystemsPath, "idchangelog.xml");
				Directory.CreateDirectory(writingSystemsPath);
				///rik File.WriteAllText(ldmlFilePath, LdmlContentForTests.Version0("en", "", "", ""));
				var migrator = new WritingSystemsMigrator(e.ProjectPath);
				migrator.MigrateIfNecessary();
				// The change log does not exist because no id needed migrating
				Assert.That(File.Exists(idChangeLogFilePath), Is.Not.True);
			}
		}


		[Test]
		public void MigrateIfNeeded_LdmlV0ContainsIdThatNeedsMigrating_WritingSystemChangeLogUpdated()
		{
			using (var e = new TestEnvironment())
			{
				string writingSystemsPath = Path.Combine(e.ProjectPath, "WritingSystems");
				string ldmlFilePath = Path.Combine(writingSystemsPath, "blah.ldml");
				string idChangeLogFilePath = Path.Combine(writingSystemsPath, "idchangelog.xml");
				Directory.CreateDirectory(writingSystemsPath);
				///rik File.WriteAllText(ldmlFilePath, LdmlContentForTests.Version0("blah","","",""));
				var migrator = new WritingSystemsMigrator(e.ProjectPath);
				migrator.MigrateIfNecessary();

				AssertThatXmlIn.File(idChangeLogFilePath).HasAtLeastOneMatchForXpath("/WritingSystemChangeLog/Changes/Change/To[text()='qaa-x-blah']");
			}
		}

		[Test]
		public void MigrateIfNeeded_WSPrefsAndLdmlV0ContainsIdsThatNeedMigrating_WritingSystemChangeLogUpdated()
		{
			using (var e = new TestEnvironment())
			{
				const string language = "zzas";
				e.WriteToPrefsFile(WritingSystemPrefsFileContent.SingleWritingSystem(language, language, "", "", "", 12, false, language, "", false, true));
				string writingSystemsPath = Path.Combine(e.ProjectPath, "WritingSystems");
				string ldmlFilePath = Path.Combine(writingSystemsPath, "en-bogus.ldml");
				string idChangeLogFilePath = Path.Combine(writingSystemsPath, "idchangelog.xml");
				Directory.CreateDirectory(writingSystemsPath);
				///rik File.WriteAllText(ldmlFilePath, LdmlContentForTests.Version0("en-bogus", "", "", ""));
				var migrator = new WritingSystemsMigrator(e.ProjectPath);
				migrator.MigrateIfNecessary();
				AssertThatXmlIn.File(idChangeLogFilePath).HasAtLeastOneMatchForXpath("/WritingSystemChangeLog/Changes/Change/To[text()='en-x-bogus']");
				AssertThatXmlIn.File(idChangeLogFilePath).HasAtLeastOneMatchForXpath("/WritingSystemChangeLog/Changes/Change/To[text()='qaa-Zxxx-x-zzas-audio']");
			}
		}

		[Test]
		public void MigrateIfNeeded_LiftFileIsVersionOtherThanWhatWeKnowTheWritingSystemMigratorCanChange_LeavesLiftAlone()
		{
			using (var e = new TestEnvironment())
			{
				WriteStringToFileAtXpath(e.PathToLiftFile, "/lift[@version]", "0.12");

				e.WriteToPrefsFile(WritingSystemPrefsFileContent.TwoWritingSystems("bogusws1", "bogusws2"));
				var migrator = new WritingSystemsMigrator(e.ProjectPath);
				migrator.MigrateIfNecessary();
				var doc = new XmlDocument();
				doc.Load(e.PathToLiftFile);
				XmlNodeList nodes = doc.SelectNodes("//@lang");

				Assert.AreEqual("bogusws1", nodes.Item(0).InnerText);
				Assert.AreEqual("bogusws2", nodes.Item(1).InnerText);
			}
		}

		[Test]
		public void MigrateIfNeeded_NoWritingSystemsExist_DoesNotThrow()
		{
			using (var e = new TestEnvironment())
			{
				var migrator = new WritingSystemsMigrator(e.ProjectPath);
				migrator.MigrateIfNecessary();
			}
		}

		[Test]
		public void MigrateIfNeeded_WritingSystemPrefsContainsAudioWritingSystem_IsMigratedCorrectly()
		{
			using (var e = new TestEnvironment())
			{
				const string language = "english";
				string writingSystemsPath = Path.Combine(e.ProjectPath, "WritingSystems");
				Directory.CreateDirectory(writingSystemsPath);
				e.WriteToPrefsFile(WritingSystemPrefsFileContent.SingleWritingSystem(language, language, "", "", "", 12, false, language, "", false, true));
				var migrator = new WritingSystemsMigrator(e.ProjectPath);
				migrator.MigrateIfNecessary();

				AssertThatXmlIn.File(e.WritingSystemFilePath("qaa-Zxxx-x-english-audio")).HasAtLeastOneMatchForXpath(
						"/ldml/identity/language[@type='qaa']"
					);
				AssertThatXmlIn.File(e.WritingSystemFilePath("qaa-Zxxx-x-english-audio")).HasAtLeastOneMatchForXpath(
						"/ldml/identity/script[@type='Zxxx']"
					);
				AssertThatXmlIn.File(e.WritingSystemFilePath("qaa-Zxxx-x-english-audio")).HasNoMatchForXpath(
						"/ldml/identity/territory"
					);
				AssertThatXmlIn.File(e.WritingSystemFilePath("qaa-Zxxx-x-english-audio")).HasAtLeastOneMatchForXpath(
						"/ldml/identity/variant[@type='x-english-audio']"
					);
			}
		}
	}
}
