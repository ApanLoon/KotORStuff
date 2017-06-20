using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;

namespace ResourceBundleLib
{
    public class KeyTable
    {
        public string Version;
        public UInt32 BuildYear;
        public UInt32 BuildDay;
        public List<BIFFileInfo> Files = new List<BIFFileInfo>();
        public List<KeyInfo> Keys = new List<KeyInfo>();

        public string KeyTableFolder;

        /// <summary>
        /// Returns the KeyInfo for a resource in a specific BIF if it exists.
        /// </summary>
        /// <param name="bifPath">Path to the BIF file relative to the location of the key table.</param>
        /// <param name="resRef">Name of the resource. May include the resource extension.</param>
        /// <returns></returns>
        public KeyInfo GetKeyInfo (string bifPath, string resRef)
        {
            BIFFileInfo fileInfo = (from file in Files where file.Path == bifPath select file).FirstOrDefault();
            if (fileInfo == null)
            {
                return null;
            }

            string resRefNoExt = Path.GetFileNameWithoutExtension(resRef);
            string resRefExt = Path.GetExtension(resRef);
            KeyInfo keyInfo = (from key in Keys where (   (key.FileInfo == fileInfo)
                                                       && ((key.ResRef == resRef) || (   (key.ResRef == resRefNoExt)
                                                                                      && (key.ResType.FileExtension == resRefExt)))) select key).FirstOrDefault();
            return keyInfo;
        }


        public KeyTable (string path)
        {
            KeyTableFolder = Path.GetDirectoryName (path);

            using (BinaryReader reader = new BinaryReader(File.OpenRead(path)))
            {
                // Read header:
                string fileType = Encoding.ASCII.GetString(reader.ReadBytes(4));
                if (fileType != "KEY ")
                {
                    throw new Exception("KeyTable: Expected \"KEY \" file type, didn't get it.");
                }

                Version = Encoding.ASCII.GetString(reader.ReadBytes(4));
                if (Version != "V1  ")
                {
                    throw new Exception("KeyTable: Unknown version. (" + Version + ")");
                }

                UInt32 bifCount = reader.ReadUInt32();
                UInt32 keyCount = reader.ReadUInt32();
                UInt32 offsetToFileTable = reader.ReadUInt32();
                UInt32 offsetToKeyTable = reader.ReadUInt32();
                BuildYear = reader.ReadUInt32();
                BuildDay = reader.ReadUInt32();

                reader.ReadBytes(32); // Reserved

                // Read file table:
                List<FileEntry> fileEntries = new List<FileEntry>();
                for (int i = 0; i < bifCount; i++)
                {
                    fileEntries.Add (new FileEntry(reader));
                }

                // Read file names:
                int offsetToFileNameTable = (int)(offsetToFileTable + bifCount * 12); // Each File Entry is 12 bytes long
                byte[] allNames = reader.ReadBytes((int)(offsetToKeyTable - offsetToFileNameTable));
                foreach (FileEntry fileEntry in fileEntries)
                {
                    Files.Add(new BIFFileInfo (
                        Encoding.ASCII.GetString(allNames, (int)(fileEntry.FileNameOffset - offsetToFileNameTable), fileEntry.FileNameSize).Trim('\0').Replace('\\', '/'),
                        fileEntry.Drives,
                        fileEntry.FileSize));
                }

                // Read key table:
                for (int i = 0; i < keyCount; i++)
                {
                    Keys.Add(new KeyInfo(reader, Files));
                }

            }
        }

        public class BIFFileInfo
        {
            public string Path;
            public UInt16 Drive;
            public UInt32 Size;
            public BIFFileInfo (string path, UInt16 drive, UInt32 size)
            {
                Path = path;
                Drive = drive;
                Size = size;
            }

            public override string ToString()
            {
                return string.Format("{0,-30} {1,10}", Path, Size);
            }
        }

        public class KeyInfo
        {
            public string ResRef;
            public ResourceType ResType;
            public BIFFileInfo FileInfo;
            public int VariableIndex;
            public int FixedIndex;

            public string FullPath
            {
                get
                {
                    return string.Format("{0}/{1}{2}", FileInfo.Path, ResRef, ResType.FileExtension);
                }
            }
            public KeyInfo(BinaryReader reader, List<BIFFileInfo> files)
            {
                ResRef = Encoding.ASCII.GetString(reader.ReadBytes(16)).Trim('\0');
                UInt16 resType = reader.ReadUInt16();
                UInt32 resID = reader.ReadUInt32();

                int fileIndex = (int)(resID >> 20);
                // ffffffff ffffxxxx xxvvvvvv vvvvvvvv
                // file         fixed  variable
                VariableIndex = (int)(resID & 0x3fff);
                FixedIndex = (int)((resID >> 14) & 0x3f);

                FileInfo = files[fileIndex];

                if (ResourceType.ByResType.ContainsKey(resType))
                {
                    ResType = ResourceType.ByResType[resType];
                }
                else
                {
                    Console.WriteLine(string.Format("KeyInfo: Unknown resource type ({0}=0x{0:x2}) for {1} in {2}", resType, ResRef, FileInfo.Path));
                    ResType = ResourceType.ByResType[0xffff];
                }
            }

            public override string ToString()
            {
                string name = ResRef + ResType.FileExtension;
                string file = FileInfo.Path;
                return string.Format ("{0,-40} {1,4} {2,4}", FullPath, FixedIndex, VariableIndex);
            }
        }

        protected class FileEntry
        {
            public UInt32 FileSize;
            public UInt32 FileNameOffset;
            public UInt16 FileNameSize;

            /// <summary>
            /// A number that represents which drives the BIF file is
            /// located in. Currently each bit represents a drive letter.
            /// e.g., bit 0 = HD0, which is the directory where the
            /// application was installed.
            /// </summary>
            public UInt16 Drives;

            public FileEntry (BinaryReader reader)
            {
                FileSize = reader.ReadUInt32();
                FileNameOffset = reader.ReadUInt32();
                FileNameSize = reader.ReadUInt16();
                Drives = reader.ReadUInt16();
            }
        }
    }
}
