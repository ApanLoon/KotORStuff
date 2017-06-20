
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ResourceBundleLib
{
    public class ERF
    {
        protected static string[] FileTypes = { "ERF ", "MOD ", "SAV ", "HAK " };
        public enum LanguageID
        {
            English = 0,
            French = 1,
            German = 2,
            Italian = 3,
            Spanish = 4,
            Polish = 5,
            Korean = 128,
            ChineseTraditional = 129,
            ChineseSimplified = 130,
            Japanese = 131
        }
        public enum Gender
        {
            Male = 0,
            Female = 1
        }

        public string FileType;
        public string Version;
        public UInt32 BuildYear;
        public UInt32 BuildDay;

        public List<LocalizedString> Strings = new List<LocalizedString>();

        public List<ERFKey> Keys = new List<ERFKey>();

        protected UInt32 offsetToResourceList;
        protected List<ResourceEntry> entries = new List<ResourceEntry>();
        protected byte[] resourceData;

        /// <summary>
        /// Gets the ERFKey for the corresponding ResRef.
        /// </summary>
        /// <param name="resRef">May contain the extension.</param>
        /// <returns></returns>
        public ERFKey this[string resRef]
        {
            get
            {
                string resRefNoExtension = Path.GetFileNameWithoutExtension(resRef);
                string extension = Path.GetExtension(resRef);
                return (from key in Keys
                        where 
                            key.ResRef == resRef || (key.ResRef == resRefNoExtension && key.ResType.FileExtension == extension )
                        select key).FirstOrDefault();
            }
        }

        public byte[] GetResourceData (ERFKey erfKey)
        {
            return GetResourceData(Keys.IndexOf(erfKey));
        }

        public byte[] GetResourceData(int index)
        {
            if (index < 0 || index >= entries.Count)
            {
                Console.WriteLine("ERF: Resource index out of range.");
                return null;
            }
            int resourceDataOffset = (int)(offsetToResourceList + entries.Count * 8);
            ResourceEntry entry = entries[index];
            byte[] buf = new byte[entry.ResourceSize];
            Array.Copy(resourceData, entry.OffsetToResource - resourceDataOffset, buf, 0, entry.ResourceSize);
            return buf;
        }



        public ERF (string erfPath)
        {
            using (BinaryReader reader = new BinaryReader(File.OpenRead(erfPath)))
            {
                // Read header:
                string fileType = Encoding.ASCII.GetString(reader.ReadBytes(4));
                int fileTypeIndex = Array.IndexOf(FileTypes, fileType);
                if (fileTypeIndex == -1)
                {
                    throw new Exception(string.Format("ERF: Unknown file type. ({0})", fileType));
                }

                Version = Encoding.ASCII.GetString(reader.ReadBytes(4));
                if (Version != "V1.0")
                {
                    throw new Exception("ERF: Unknown version. (" + Version + ")");
                }

                UInt32 languageCount              = reader.ReadUInt32();
                UInt32 localizedStringSize        = reader.ReadUInt32();
                UInt32 entryCount                 = reader.ReadUInt32();
                UInt32 offsetToLocalizedString    = reader.ReadUInt32();
                UInt32 offsetToKeyList            = reader.ReadUInt32();
                offsetToResourceList              = reader.ReadUInt32();
                BuildYear                         = reader.ReadUInt32();
                BuildDay                          = reader.ReadUInt32();
                UInt32 descriptionStringReference = reader.ReadUInt32();
                reader.ReadBytes(116); // Reserved

                // Read string list:
                for (int i = 0; i < languageCount; i++)
                {
                    Strings.Add(new LocalizedString(reader));
                }

                // Read key list:
                for (int i = 0; i < entryCount; i++)
                {
                    Keys.Add(new ERFKey(reader, this));
                }

                // Read resource list:
                UInt32 resourceDataSize = 0;
                for (int i = 0; i < entryCount; i++)
                {
                    ResourceEntry entry = new ResourceEntry(reader);
                    entries.Add(entry);
                    resourceDataSize += entry.ResourceSize;
                }

                // Read resource data:
                resourceData = reader.ReadBytes((int)resourceDataSize);

            }
        }

        public class LocalizedString
        {
            LanguageID LanguageID;
            Gender Gender;
            string String;

            public LocalizedString(LanguageID languageID, Gender g, string s)
            {
                this.LanguageID = languageID;
                this.Gender = g;
                this.String = s;
            }
            public LocalizedString(BinaryReader reader)
            {
                UInt32 languageID = reader.ReadUInt32();

                this.LanguageID = (LanguageID)(languageID / 2);
                this.Gender = (Gender)(languageID % 2);

                UInt32 stringSize = reader.ReadUInt32();
                this.String = Encoding.ASCII.GetString(reader.ReadBytes((int)stringSize)).Trim('\0');
            }
            public override string ToString()
            {
                return string.Format("{0} {1} {2}", this.LanguageID, this.Gender, this.String);
            }
        }
        public class ERFKey
        {
            public string ResRef;
            public UInt32 ResID;
            public ResourceType ResType;

            protected ERF erf;

            public ERFKey (BinaryReader reader, ERF erf)
            {
                this.erf = erf;

                ResRef = Encoding.ASCII.GetString(reader.ReadBytes(16)).Trim('\0');
                ResID = reader.ReadUInt32();
                UInt16 resType = reader.ReadUInt16();
                if (ResourceType.ByResType.ContainsKey(resType))
                {
                    ResType = ResourceType.ByResType[resType];
                }
                else
                {
                    Console.WriteLine(string.Format("ERFKey: Unknown resource type ({0}=0x{0:x2}) for {1}.", resType, ResRef));
                    ResType = ResourceType.ByResType[0xffff];
                }
                reader.ReadUInt16(); //Unused
            }

            public override string ToString()
            {
                string name = this.ResRef + this.ResType.FileExtension;
                UInt32 size = erf.entries[(int)ResID].ResourceSize;
                return string.Format("{0,-20} {1,10}", name, size);
            }
        }

        protected class ResourceEntry
        {
            public UInt32 OffsetToResource;
            public UInt32 ResourceSize;

            public ResourceEntry(BinaryReader reader)
            {
                OffsetToResource = reader.ReadUInt32();
                ResourceSize = reader.ReadUInt32();
            }
        }

    }
}
