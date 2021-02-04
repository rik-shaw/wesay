using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SIL.Code;
using SIL.DictionaryServices.Model;
using SIL.Lift;
using SIL.Lift.Options;
using SIL.Text;

using Enumerable=SIL.Linq.Enumerable;

namespace WeSay.LexicalModel
{
	/// <summary>
	/// A query to identify entries which have some field which needs work, based on the emptiness
	/// of specified writing system alternatives, and the non-emptiness of other ones.
	/// </summary>
	public class MissingFieldQuery: IFieldQuery<LexEntry>
	{
		private readonly Field _field;
		private readonly string[] _writingSystemsWhichAreRequired;
		private readonly string[] _writingSystemsWhichWeWantToFillIn;

		public MissingFieldQuery(Field field, string[] writingSystemsWhichWeWantToFillIn, string[] writingSystemsWhichAreRequired)
		{
			Guard.AgainstNull(field, "field");
			_field = field;

			if (writingSystemsWhichWeWantToFillIn == null || writingSystemsWhichWeWantToFillIn.Length == 0)
			{
				//if none specified, include all of them
				_writingSystemsWhichWeWantToFillIn = field.WritingSystemIds.ToArray();
			}
			else
			{
				_writingSystemsWhichWeWantToFillIn = writingSystemsWhichWeWantToFillIn;
			}

			if (writingSystemsWhichAreRequired == null)
			{
				_writingSystemsWhichAreRequired = new string[0];
			}
			else
			{
				_writingSystemsWhichAreRequired = writingSystemsWhichAreRequired;
			}
		}

		#region IFilter<LexEntry> Members

		/// <summary>
		/// Filters are kept in a list; this is the string by which a filter is accessed.
		/// </summary>
		public string Key
		{
			get
			{
				string key = "Missing " + Field.FieldName;
				List<string> writingSystemIds = new List<string>(Field.WritingSystemIds);
				writingSystemIds.Sort(StringComparer.InvariantCulture);
				foreach (string writingSystemId in writingSystemIds)
				{
					key += " [" + writingSystemId + "]";
				}
				key += " Filter";
				return key;
			}
		}

		public Predicate<LexEntry> FilteringPredicate
		{
			get { return IsMissingItem; }
		}

		public string FieldName
		{
			get { return Field.FieldName; }
		}

		public Field Field
		{
			get { return _field; }
		}

		public string UniqueCacheId
		{
			get
			{
				return GetCacheWritingSystemTag(_writingSystemsWhichWeWantToFillIn) + "/" +
					   GetCacheWritingSystemTag(_writingSystemsWhichAreRequired);

			}
		}

		/// <summary>
		/// Given a list of writingSystems, combine them in a way that can be used to uniquely identify a cache of results
		/// </summary>
		/// <param name="ids"></param>
		/// <returns></returns>
		private static string GetCacheWritingSystemTag(string[] ids)
		{
			if (ids == null || ids.Length == 0)
			{
				return "--";
			}
			string wsTag = "";
			Enumerable.ForEach(ids, id => wsTag += id);
			return wsTag;
		}

		private bool IsMissingData(object content)
		{
			switch (Field.DataTypeName)
			{
				case "Option":
					return ((OptionRef) content).IsEmpty;
				case "OptionCollection":
					return ((OptionRefCollection) content).IsEmpty;
				case "MultiText":
					return HasAllRequiredButMissingAtLeastOneWeWantToFillIn((MultiText) content);

				case "RelationToOneEntry":
					LexRelationCollection collection = (LexRelationCollection) content;
					if (IsSkipped(collection.Parent, Field.FieldName))
					{
						return false;
					}
					foreach (LexRelation r in collection.Relations)
					{
						if (!string.IsNullOrEmpty(r.TargetId))
						{
							return false; // has one non-empty relation
						}
					}
					return true;
					//collection is empty or all its members don't really have targets
				default:
					Debug.Fail("unknown DataTypeName");
					return false;
			}
		}

		private bool IsMissingItem(LexEntry entry)
		{
			if (entry == null)
			{
				return false;
			}

			switch (Field.ClassName)
			{
				case "LexEntry":
					return IsMissingLexEntryField(entry);
				case "LexSense": // fall through
				case "LexExampleSentence":
					foreach (LexSense sense in entry.Senses)
					{
						if (Field.ClassName == "LexSense")
						{
							if (IsMissingLexSenseField(sense))
							{
								return true;
							}
						}
						else
						{
							foreach (LexExampleSentence example in sense.ExampleSentences)
							{
								if (Field.ClassName == "LexExampleSentence")
								{
									if (IsMissingLexExampleSentenceField(example))
									{
										return true;
									}
								}
							}
							if (sense.ExampleSentences.Count == 0 &&
								(Field.FieldName == Field.FieldNames.ExampleSentence.ToString()) &&
								_writingSystemsWhichAreRequired.Length==0)
							{
								//ghost field
								return true;
							}
						}
					}
					if (entry.Senses.Count == 0 &&
						Field.IsMeaningField &&
						((Field.FieldName == LexSense.WellKnownProperties.Definition) ||
							(Field.FieldName == LexSense.WellKnownProperties.Gloss)) &&
								_writingSystemsWhichAreRequired.Length == 0)
					{
						//ghost field
						return true;
					}

					break;
				default:
					Debug.Fail("unknown ClassName");
					break;
			}
			return false;
		}

		private static bool IsSkipped(PalasoDataObject parent, string fieldName)
		{
			return parent.GetHasFlag("flag-skip-" + fieldName);
		}

		private bool IsMissingLexExampleSentenceField(LexExampleSentence example)
		{
			if (!Field.IsBuiltInViaCode)
			{
				return IsMissingCustomField(example);
			}
			if (Field.FieldName == Field.FieldNames.ExampleSentence.ToString())
			{
				return HasAllRequiredButMissingAtLeastOneWeWantToFillIn(example.Sentence);
			}
			if (Field.FieldName == Field.FieldNames.ExampleTranslation.ToString())
			{
				return HasAllRequiredButMissingAtLeastOneWeWantToFillIn(example.Translation);
			}
			Debug.Fail("unknown FieldName");
			return false;
		}

		private bool IsMissingLexSenseField(PalasoDataObject sense)
		{
			if (!Field.IsBuiltInViaCode)
			{
				return IsMissingCustomField(sense);
			}
			//                if(this._field.FieldName == LexSense.WellKnownProperties.Gloss)
			//                {
			//                    return IsMissingWritingSystem(sense.Gloss);
			//                }
			//                else
			{
				Debug.Fail("unknown FieldName");
				return false;
			}
		}

		private bool IsMissingLexEntryField(LexEntry entry)
		{
			if (!Field.IsBuiltInViaCode)
			{
				return IsMissingCustomField(entry);
			}
			if (Field.FieldName == Field.FieldNames.EntryLexicalForm.ToString())
			{
				if (HasAllRequiredButMissingAtLeastOneWeWantToFillIn(entry.LexicalForm))
				{
					return true;
				}
			}
			else
			{
				Debug.Fail("unknown FieldName");
			}
			return false;
		}

		/// <summary>
		/// NB here is now a misleading name... what we really mean is "is ready to be filled in".
		/// </summary>
		private bool IsMissingCustomField(PalasoDataObject palasoData)
		{
			var content = palasoData.GetProperty<IPalasoDataObjectProperty>(Field.FieldName);
			if (content == null)
			{
				if (IsSkipped(palasoData, Field.FieldName))
					return false;

				//	WS-33978 situation: when some writing system is required don't flag it if the entire field is missing
				if (_writingSystemsWhichAreRequired.Length > 0)
					return false;
				return true;
			}
			if(Field.FieldName == "POS")
			{
				if(IsPosUnknown(palasoData))
				{
					return true;
				}
			}
			return IsMissingData(content);
		}

		private bool IsPosUnknown(PalasoDataObject sense)
		{
			return sense.Properties.Any(property => property.Key == "POS" && (((OptionRef) property.Value).Key == "unknown"));
		}


		private bool HasAllRequiredButMissingAtLeastOneWeWantToFillIn(MultiTextBase text)
		{
			foreach (var wsId in _writingSystemsWhichAreRequired)
			{
				if (!text.ContainsAlternative(wsId))
				{
					return false;
				}
			}

			foreach (var wsId in _writingSystemsWhichWeWantToFillIn)
			{
				if (!text.ContainsAlternative(wsId))
				{
					return true;
				}
			}
			return false;
		}


		#endregion
	}
}
