using SIL.Data;
using SIL.DictionaryServices.Model;
using SIL.UiBindings;

namespace WeSay.LexicalModel
{
	public class PairStringLexEntryIdDisplayProvider: IDisplayStringAdaptor
	{
		public string GetDisplayLabel(object item)
		{
			RecordToken<LexEntry> kv = (RecordToken<LexEntry>) item;
			return (string) kv["Form"];
		}

		public string GetToolTip(object item)
		{
			RecordToken<LexEntry> recordToken = (RecordToken<LexEntry>) item;
			LexEntry entry = recordToken.RealObject;
			return entry.GetToolTipText();
		}

		#region IDisplayStringAdaptor Members

		public string GetToolTipTitle(object item)
		{
			return "";
		}

		#endregion
	}
}