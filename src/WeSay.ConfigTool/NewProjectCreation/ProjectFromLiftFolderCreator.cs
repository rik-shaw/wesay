﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using Palaso.DictionaryServices.Model;
using Palaso.Reporting;
using WeSay.LexicalModel.Foundation;
using WeSay.Project;

namespace WeSay.ConfigTool.NewProjectCreation
{
	/// <summary>
	/// WeSay uses more files than raw lift; for example, it has a .WeSayConfig file to hold the configuration of tasks.
	/// This class is used when we open a lift folder which lacks these wesay-specific files (yes, if we consider images/audio
	/// ranges, etc., we see
	/// lift is a folder format, not just  file). A common case for being asked to open such a lift folder is when
	/// the project already has a FLEx database, and they want to do Send/Receive with WeSay.
	///
	/// The idea for use of this class is that before opening a project, this class should be given a chance to
	/// "prepare the way".  If everything looks ok, it will just do nothing.
	/// </summary>
	public class ProjectFromLiftFolderCreator
	{
		private readonly string _path;
		private readonly ViewTemplate _viewTemplate;
		private readonly WritingSystemCollection _writingSystems;

		public ProjectFromLiftFolderCreator(string path, ViewTemplate viewTemplate, WritingSystemCollection writingSystems)
		{
			_path = path;
			_viewTemplate = viewTemplate;
			_writingSystems = writingSystems;
		}

		public void PrepareLiftFolderForWeSay()
		{
			SetWritingSystemsForFields();
		}

		private void SetWritingSystemsForFields()
		{
			var doc = new XmlDocument();
			doc.Load(_path); //will throw if the file is ill-formed
			var missingWritingSystems = new StringBuilder();

			foreach (XmlNode node in doc.SelectNodes("//@lang"))
			{
				if (node.Value == "x-spec" && !_writingSystems.ContainsKey("x-spec"))
				{
					_writingSystems.AddSimple("x-spec");
				}
				if (!_writingSystems.ContainsKey(node.Value))
				{
					_writingSystems.AddSimple(node.Value);
					missingWritingSystems.AppendFormat("{0},", node.Value);
				}
			}

			if(missingWritingSystems.Length > 0)
			{
				var list = missingWritingSystems.ToString().Trim(new char[]{','});
				ErrorReport.NotifyUserOfProblem(
					"WeSay had a problem locating information on at least one writing system used in the LIFT export from FLEx.  One known cause of this is an old version of FLEx. In the folder containing the LIFT file, there should have been '___.ldml' files for the following writing systems: {0}.\r\nBecause these Writing System definitions were not found, WeSay will create blank writing systems for each of these, which you will need to set up with the right fonts, keyboards, etc.", list);
			}
			// replace all "v" fields with the first lexical-unit writing system
			//and all "en" with the first translation one...

			var vernacular = GetTopWritingSystem(doc, "//lexical-unit/form/@lang");
			if (vernacular != String.Empty)
			{
				_viewTemplate.ChangeWritingSystemId(WritingSystem.IdForUnknownVernacular, vernacular);
				_writingSystems.Remove(WritingSystem.IdForUnknownVernacular);
			}
			var analysis = GetTopWritingSystem(doc, "//sense/gloss/@lang");
			if (analysis == String.Empty)
			{
				analysis = GetTopWritingSystem(doc, "//sense/definition/@lang");
				//nb: we don't want to remove english, even if they don't use it
			}
			if (analysis != String.Empty)
			{
				_viewTemplate.ChangeWritingSystemId(WritingSystem.IdForUnknownAnalysis, analysis);
			}

			AddWritingSystemsForField(doc,  "//lexical-unit/form/@lang", LexEntry.WellKnownProperties.LexicalUnit);
			AddWritingSystemsForField(doc,  "//sense/gloss/@lang", LexSense.WellKnownProperties.Gloss);

			AddWritingSystemsForField(doc,  "//sense/definition/form/@lang", LexSense.WellKnownProperties.Definition);

			AddAllGlossWritingSystemsToDefinition();

			AddWritingSystemsForField(doc,  "//example/form/@lang", LexExampleSentence.WellKnownProperties.ExampleSentence);
			AddWritingSystemsForField(doc,  "//translation/form/@lang", LexExampleSentence.WellKnownProperties.Translation);

			//------------ hack
			var gloss = _viewTemplate.GetField(LexSense.WellKnownProperties.Gloss);
			var def = _viewTemplate.GetField(LexSense.WellKnownProperties.Definition);

			foreach (var id in def.WritingSystemIds)
			{
				if(!gloss.WritingSystemIds.Contains(id))
					gloss.WritingSystemIds.Add(id);
			}
			foreach (var id in gloss.WritingSystemIds)
			{
				if (!def.WritingSystemIds.Contains(id))
					def.WritingSystemIds.Add(id);
			}
		}

		private string GetTopWritingSystem(XmlDocument doc, string xpath)
		{
			var nodes = doc.SelectNodes(xpath);
			if (nodes != null && nodes.Count > 0)
			{
				return nodes[0].Value;
			}
			return String.Empty;
		}

		/// <summary>
		/// This is done because even if they don't use definitions, their glosses are going to be moved over the definition field.
		/// </summary>
		private  void AddAllGlossWritingSystemsToDefinition()
		{
			var defField = _viewTemplate.GetField(LexSense.WellKnownProperties.Definition).WritingSystemIds;
			foreach (var id in _viewTemplate.GetField(LexSense.WellKnownProperties.Gloss).WritingSystemIds)
			{
				if(!defField.Contains(id))
				{
					defField.Add(id);
				}
			}
		}

		private  void AddWritingSystemsForField(XmlDocument doc,string xpath, string fieldName)
		{
			var f = _viewTemplate.GetField(fieldName);

			//now add in what we find
			XmlNodeList nodes = doc.SelectNodes(xpath);
			if(nodes!=null && nodes.Count > 0)
			{
				//ok, so there is at least one match. Take out all of the default writing system from this
				//field before adding in the ones that were being used in FLEx.
				f.WritingSystemIds.Clear();
			}

			foreach (XmlNode node in nodes)
			{
				if (!f.WritingSystemIds.Contains(node.Value))
				{
					f.WritingSystemIds.Add(node.Value);
				}
			}
		}
	}
}
