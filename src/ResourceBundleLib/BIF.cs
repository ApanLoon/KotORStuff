
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ResourceBundleLib
{
    public class BIF
    {
        public string Version;
        public List<FixedResourceEntry> FixedResources = new List<FixedResourceEntry>();
        public List<VariableResourceEntry> VariableResources = new List<VariableResourceEntry>();

        protected int fixedResourceDataOffset;
        protected byte[] fixedResourceData;

        protected int variableResourceDataOffset;
        protected byte[] variableResourceData;


        protected KeyTable keyTable;

        public byte[] GetFixedResource (int index)
        {
            if (index < 0 || index >= FixedResources.Count)
            {
                Console.WriteLine("BIF: Resource index out of range.");
                return null;
            }
            FixedResourceEntry entry = FixedResources[index];
            byte[] resourceData = new byte[entry.FileSize];
            Array.Copy(fixedResourceData, entry.Offset - fixedResourceDataOffset, resourceData, 0, entry.FileSize);
            return resourceData;
        }

        public byte[] GetVariableResource(int index)
        {
            if (index < 0 || index >= VariableResources.Count)
            {
                Console.WriteLine("BIF: Resource index out of range.");
                return null;
            }
            VariableResourceEntry entry = VariableResources[index];
            byte[] resourceData = new byte[entry.FileSize];
            Array.Copy(variableResourceData, entry.Offset - variableResourceDataOffset, resourceData, 0, entry.FileSize);
            return resourceData;
        }

        public BIF(string bifPath, KeyTable keyTable)
        {
            this.keyTable = keyTable;

            string filePath = keyTable.KeyTableFolder + "/" + bifPath;
            using (BinaryReader reader = new BinaryReader(File.OpenRead(filePath)))
            {
                // Read header:
                string fileType = Encoding.ASCII.GetString(reader.ReadBytes(4));
                if (fileType != "BIFF")
                {
                    throw new Exception("BIF: Expected \"BIFF\" file type, didn't get it.");
                }

                Version = Encoding.ASCII.GetString(reader.ReadBytes(4));
                if (Version != "V1  ")
                {
                    throw new Exception("BIF: Unknown version. (" + Version + ")");
                }

                UInt32 variableResourceCount = reader.ReadUInt32();
                UInt32 fixedResourceCount = reader.ReadUInt32();
                UInt32 variableResourceOffset = reader.ReadUInt32();

                // Read fixed resource table:
                UInt32 fixedResourceLength = 0;
                for (int i= 0; i < fixedResourceCount; i++)
                {
                    FixedResourceEntry entry = new FixedResourceEntry(reader);
                    FixedResources.Add(entry);
                    fixedResourceLength += entry.FileSize;
                }
                fixedResourceDataOffset = (int)(20 + fixedResourceCount * 20);

                // Read variable resource table:
                UInt32 variableResourceLength = 0;
                for (int i = 0; i < variableResourceCount; i++)
                {
                    VariableResourceEntry entry = new VariableResourceEntry(reader);
                    VariableResources.Add(entry);
                    variableResourceLength += entry.FileSize;
                }
                variableResourceDataOffset = fixedResourceDataOffset + (int)(variableResourceCount * 16);

                // Read fixed resource data:
                fixedResourceData = reader.ReadBytes((int)fixedResourceLength);

                // Read variable resource data:
                variableResourceData = reader.ReadBytes((int)variableResourceLength);
            }
        }

        public class FixedResourceEntry
        {
            public int BIFIndex;
            public int ResourceIndex;
            public UInt32 Offset;
            public UInt32 PartCount;
            public UInt32 FileSize;
            public ResourceType ResType;

            public FixedResourceEntry (BinaryReader reader)
            {
                UInt32 resID = reader.ReadUInt32();
                // ffffffff ffffxxxx xxvvvvvv vvvvvvvv
                // bif          fixed  variable
                BIFIndex = (int)(resID >> 20);
                //VariableIndex = (int)(resID & 0x3fff);
                ResourceIndex = (int)((resID >> 14) & 0x3f);

                Offset = reader.ReadUInt32();
                PartCount = reader.ReadUInt32();
                FileSize = reader.ReadUInt32();
                UInt32 resType = reader.ReadUInt32();

                if (ResourceType.ByResType.ContainsKey((UInt16)resType))
                {
                    ResType = ResourceType.ByResType[(UInt16)resType];
                }
                else
                {
                    //TODO: Perhaps look up the name in the KeyTable.
                    Console.WriteLine(string.Format("BIF: Unknown resource type ({0}=0x{0:x2})"));
                    ResType = ResourceType.ByResType[0xffff];
                }

            }
        }


        public class VariableResourceEntry
        {
            public int BIFIndex;
            public int ResourceIndex;
            public UInt32 Offset;
            public UInt32 FileSize;
            public ResourceType ResType;

            public VariableResourceEntry(BinaryReader reader)
            {
                UInt32 resID = reader.ReadUInt32();
                // ffffffff ffffxxxx xxvvvvvv vvvvvvvv
                // bif          fixed  variable
                BIFIndex = (int)(resID >> 20);
                ResourceIndex = (int)(resID & 0x3fff);
                //ResourceIndex = (int)((resID >> 14) & 0x3f);

                Offset = reader.ReadUInt32();
                FileSize = reader.ReadUInt32();
                UInt32 resType = reader.ReadUInt32();

                if (ResourceType.ByResType.ContainsKey((UInt16)resType))
                {
                    ResType = ResourceType.ByResType[(UInt16)resType];
                }
                else
                {
                    //TODO: Perhaps look up the name in the KeyTable.
                    Console.WriteLine(string.Format("BIF: Unknown resource type ({0}=0x{0:x2})"));
                    ResType = ResourceType.ByResType[0xffff];
                }
            }
        }
    }
}
