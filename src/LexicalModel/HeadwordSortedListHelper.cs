using System;
using System.Collections.Generic;
using Db4objects.Db4o;
using Palaso.Base32;
using WeSay.Data;
using WeSay.Foundation;
using WeSay.Language;

namespace WeSay.LexicalModel
{
    public class HeadwordSortedListHelper: ISortHelper<string, LexEntry>
    {
        Db4oDataSource _db4oData; // for data
        WritingSystem _writingSystem;
        private HomographCalculator _homographCalculator;

        public HeadwordSortedListHelper(Db4oRecordListManager recordListManager, 
                                  WritingSystem writingSystem)
        {
            _db4oData = (Db4oDataSource) recordListManager.DataSource.Data;
            _writingSystem = writingSystem;

            _homographCalculator = new HomographCalculator(recordListManager, writingSystem);
        }

        public HeadwordSortedListHelper(WritingSystem writingSystem)
        {
            if (writingSystem == null)
            {
                throw new ArgumentNullException("writingSystemId");
            }

            _writingSystem = writingSystem;
        }

        #region IDb4oSortHelper<string,LexEntry> Members

        public IComparer<string> KeyComparer
        {
            get
            {
                return StringComparer.Ordinal;//in strict order; these are already sortkeys, not normal strings
            }
        }

        /// <summary>
        /// This is used to build the list from scratch
        /// </summary>
        /// <returns></returns>
        public List<KeyValuePair<string, long>> GetKeyIdPairs()
        {
            if (_db4oData != null)
            {
                List<KeyValuePair<string, long>> pairs = new List<KeyValuePair<string, long>>();

                IObjectSet set = _db4oData.Data.Get(typeof (LexEntry));

                //enhance: This will be slow and take a lot of ram, as it will bring each entry
                //into ram.  It is theoretically possible to just query on the lexemeform
                //and citation form for each entry, and compute the headword.
                foreach (LexEntry entry in set)
                {
                    foreach (string key in GetKeys(entry))
                    {
                        pairs.Add(new KeyValuePair<string, long>(key, _db4oData.Data.Ext().GetID(entry)));
                    }
                }
                return pairs;
            }
            throw new InvalidOperationException();
        }

        /// <summary>
        /// Get the keys (there's only one) for a single entry
        /// </summary>
        /// <param name="entry"></param>
        /// <returns></returns>
        public IEnumerable<string> GetKeys(LexEntry entry)
        {
            List<string> keys = new List<string>();
            byte[] keydata = _writingSystem.GetSortKey(entry.GetHeadWord(_writingSystem.Id)).KeyData;
            string key = Base32Convert.ToBase32String(keydata, Base32FormattingOptions.None);
            int homographNumber = _homographCalculator.GetHomographNumber(entry);
            key += "_" + homographNumber.ToString("000000"); 
            
            keys.Add(key);
            return keys;
        }



        public string Name
        {
            get
            {
                return "Headwords sorted by " + _writingSystem.Id;
            }
        }

        public override int GetHashCode()
        {
            return _writingSystem.GetHashCode();
        }

        #endregion
    }
}