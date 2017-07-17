using System.Collections.Generic;

namespace TPCLib
{
    public class TXIValueDictionary
    {
        protected Dictionary<string, string> dictionary = new Dictionary<string, string>();
 
        public string this[string key]
        {
            get
            {
                if (dictionary.ContainsKey (key))
                {
                    return dictionary[key];
                }
                else
                {
                    return "";
                }
            }
            set
            {
                dictionary[key] = value;
            }
        }

        public void Add(string key, string value)
        {
            dictionary.Add(key, value);
        }

    }
}
