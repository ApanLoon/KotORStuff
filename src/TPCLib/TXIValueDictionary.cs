using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TPCLib
{

    // Spotted tags, names are case insensitive:
    //
    // cube <bool>                                     (0, 1)

    // isbumpmap <bool>
    // bumpmapscaling <value>                          (1, 2, 3, 5)

    // isdefusebumpmap <bool>
    // isspecularbumpmap <bool>

    // envmaptexture <string>                          (CM_dsk, CM_jedcom, ...)

    // blending <string>                               (additive, punchthrough)

    // bumpyshinytexture <string>                      (CM_QATEAST, ...)
    // bumpmaptexture <string>                         (LQA_dewbackBMP, ...)

    // proceduretype cycle
    // numx 2
    // numy 2
    // fps 16

    // proceduretype arturo
    // channelscale 4
    // 0.2
    // 0.2
    // 0.2
    // 0.2
    // channeltranslate 4
    // 0.5
    // 0.7
    // 0.6
    // 0.5
    // distort 2
    // arturowidth 32
    // arturoheight 32
    // distortionamplitude 4
    // speed 60
    // defaultheight 32
    // defaultwidth 32

    // downsamplemin <value>                           (1)
    // downsamplemax <value>                           (1)

    // mipmap 0

    // islightmap <bool>
    // compresstexture 0

    // clamp <value>                                   (3)

    // decal <bool>                                    (0/1)

    // wateralpha <float>                              (0.40)

    // Fonts:
    // numchars 255
    // fontheight 0.140000
    // baselineheight 0.110000
    // texturewidth 2.560000
    // spacingR 0.000000
    // spacingB 0.000000
    // upperleftcoords 255
    // 0.000000 1.000000 0
    // 0.062500 1.000000 0
    // 0.125000 1.000000 0
    // ... (255 times)
    // lowerrightcoords 255
    // 0.031250 0.945313 0
    // 0.093750 0.945313 0
    // 0.156250 0.945313 0
    // ... (255 times)


    public class TXIValueDictionary : IEnumerator, IEnumerable
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
                if (!dictionary.ContainsKey(key))
                {
                    sortedKeys.Add(key);
                }
                dictionary[key] = value;
            }
        }

        protected List<string> sortedKeys = new List<string>();

        #region Construction
        public TXIValueDictionary()
        {
        }

        public TXIValueDictionary (string[] strings)
        {
            string name;
            string value;
            int i;
            int lineIndex = 0;
            while (lineIndex < strings.Length)
            {
                i = strings[lineIndex].IndexOf(' ');
                if (i == -1)
                {
                    name = strings[lineIndex].ToLower();
                    value = "";
                }
                else
                {
                    name = strings[lineIndex].Substring(0, i).Trim().ToLower();
                    value = strings[lineIndex].Substring(i + 1).Trim();
                }

                switch (name)
                {
                    case "channelscale":
                    case "channeltranslate":
                        value += " " + strings[++lineIndex];
                        value += " " + strings[++lineIndex];
                        value += " " + strings[++lineIndex];
                        value += " " + strings[++lineIndex];
                        break;
                    case "upperleftcoords":
                    case "lowerrightcoords":
                        int n = int.Parse(value.Trim());
                        i = 0;
                        while (i < n)
                        {
                            value += " " + strings[++lineIndex].Replace(' ', ',');
                            i++;
                        }
                        break;
                }

                Add(name, value);
                lineIndex++;
            }
        }
        #endregion Construction

        public void Add(string key, string value)
        {
            if (dictionary.ContainsKey(key))
            {
                // TODO: What to do with duplicate keys? Now it overwrites the old one.
                Console.WriteLine("Duplicate key: " + key + " overwriting old value \"" + dictionary[key] + "\" with \"" + value + "\"");
            }
            else
            {
                sortedKeys.Add(key);
            }
            dictionary[key] = value;
        }

        public void Save(BinaryWriter writer)
        {
            foreach (string key in sortedKeys)
            {
                int i;
                string value = dictionary[key];
                switch (key)
                {
                    case "channelscale":
                    case "channeltranslate":
                        i = value.IndexOf(' ');
                        if (i == -1)
                        {
                            throw new System.Exception(string.Format("Incorrect syntax for TXI attribute {0}.", key));
                        }
                        value = value.Substring(0, i) + "\r\n" + value.Substring(i + 1).Replace(" ", "\r\n");
                        break;
                    case "upperleftcoords":
                    case "lowerrightcoords":
                        i = value.IndexOf(' ');
                        if (i == -1)
                        {
                            throw new System.Exception(string.Format("Incorrect syntax for TXI attribute {0}.", key));
                        }
                        // First replace space with new line, then replace comma with space.
                        value = value.Substring(0, i) + "\r\n" + value.Substring(i + 1).Replace(" ", "\r\n").Replace(',', ' ');
                        break;
                }
                writer.Write(Encoding.ASCII.GetBytes(key + " " + value + "\r\n"));
            }
        }


        #region IEnumerable
        public IEnumerator GetEnumerator()
        {
            return this;
        }
        #endregion IEnumerable

        #region IEnumerator
        protected int currentKey = -1;
        public object Current
        {
            get
            {
                return sortedKeys[currentKey];
            }
        }
        public bool MoveNext()
        {
            currentKey++;
            return currentKey < sortedKeys.Count;
        }
        public void Reset()
        {
            currentKey = -1;
        }
        #endregion IEnumerator
    }
}
