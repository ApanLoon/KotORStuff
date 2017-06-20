
using ResourceBundleLib;
using NDesk.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;
using static ResourceBundleLib.KeyTable;

namespace bif
{
    class bif
    {
        static string exectutableName;
 
        static void Main(string[] args)
        {
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            exectutableName = Path.GetFileName (codeBase);

            string keyTableFilePath = "";
            //keyTableFilePath = "C:/Program Files/LucasArts/SWKotOR/chitin.key";
            //keyTableFilePath = "D:/Program Files (x86)/Steam/SteamApps/common/Knights of the Old Republic II/chitin.key";

            List<string> bifPaths = new List<string>();
            List<string> resourcePaths = new List<string>();

            bool showHelp = false;
            bool list = false;
            bool extract = false;

            string outputPath = "";

            var optionSet = new OptionSet()
            {
                {"k|keys=",     "The key table file. Also specifies the root for where to look for BIF files.", v => keyTableFilePath = v },
                {"b|bif=",      "Specify one or more of these to select which BIF files to operate on. Relative to the location of the key table.", v => bifPaths.Add(v) },
                {"r|resource=", "Specify one or more of these to select which resource files to operate on.", v => resourcePaths.Add(v) },
                {"l|list",      "List information from the key table.\n\nWith -b and -r:\nLists the specified resources if they are in any of the specified BIFs. (Do NOT specify extensions)\n\nWith only -b:\nLists all resources in the specified BIFs\n\nWith only -r:\nLists the specified resources. (do NOT specify extensions)\n\nWith none:\nLists all BIFs in the key table\nNote that here, -r arguments must NOT contain the path to the BIF.", v => list = v != null },
                {"x|extract",   "Extract resources.\nThis ignores any -b arguments, instead the -r arguments MUST contain the path to the BIF file. For example \"-r data/Textures.bif/lma_oceanbmp.tga\". The extension is optional.\nIf the ResRef is left out, the entire BIF will be extracted. For example \"-r data/textures.bif/\" - note that the trailing \"/\" must be there.", v => extract = v != null },
                {"o|output=",   "Output folder for extracting resources.", v => outputPath = v },
                {"h|help",      "Show this message and exit", v => showHelp = v != null }
            };

            List<string> extra;

            try
            {
                extra = optionSet.Parse(args);
            }
            catch (OptionException e)
            {
                Console.Write("{0}: ", exectutableName);
                Console.WriteLine(e.Message);
                Console.WriteLine("Try `{0} --help' for more information.", exectutableName);
                PauseOnExit();
                return;
            }

            // Check mandatory arguments:
            if (!showHelp && keyTableFilePath == "")
            {
                Console.WriteLine("No key table set.");
                showHelp = true;
            }

            if (extract && outputPath == "")
            {
                Console.WriteLine("Extracting requires an output path.");
                showHelp = true;
            }


            if (showHelp)
            {
                ShowHelp (optionSet);
                PauseOnExit();
                return;
            }

            if (extra.Count > 0)
            {
                Console.WriteLine("Ignoring extra arguments: {0}", string.Join(" ", extra.ToArray()));
            }




            KeyTable keyTable = new KeyTable(keyTableFilePath);


            if (list)
            {
                if (bifPaths.Count == 0 && resourcePaths.Count == 0)
                {
                    // List the BIF files in the key table:
                    foreach (BIFFileInfo fileInfo in keyTable.Files)
                    {
                        Console.WriteLine(fileInfo.ToString());
                    }
                }
                else if (bifPaths.Count != 0 && resourcePaths.Count == 0)
                {
                    // List all resources in specified BIFs:
                    foreach (KeyTable.KeyInfo keyInfo in keyTable.Keys.Where(k => bifPaths.Contains(k.FileInfo.Path)))
                    {
                        Console.WriteLine(keyInfo.ToString());
                    }
                }
                else if (bifPaths.Count == 0 && resourcePaths.Count != 0)
                {
                    // List specified resources: (TODO: Can't handle resourcePaths with extensions)
                    foreach (KeyTable.KeyInfo keyInfo in keyTable.Keys.Where(k => resourcePaths.Contains(k.ResRef)))
                    {
                        Console.WriteLine(keyInfo.ToString());
                    }
                }
                else if (bifPaths.Count != 0 && resourcePaths.Count != 0)
                {
                    // List specified resources if they are in any of the specified BIFs: (TODO: Can't handle resourcePaths with extensions)
                    foreach (KeyTable.KeyInfo keyInfo in keyTable.Keys.Where(k => bifPaths.Contains(k.FileInfo.Path) && resourcePaths.Contains(k.ResRef)))
                    {
                        Console.WriteLine(keyInfo.ToString());
                    }
                }
            }

            if (extract)
            {
                // Extract the specified resource files:
                string lastBIFPath = "";
                BIFFileInfo fileInfo = null;
                BIF bif = null;
                foreach (string resourcePath in resourcePaths.OrderBy (s => GetDirectoryName (s)).ToList())
                {
                    string bifPath = GetDirectoryName(resourcePath);
                    if (bifPath != lastBIFPath)
                    {
                        fileInfo = (from file in keyTable.Files where file.Path == bifPath select file).FirstOrDefault();
                        if (fileInfo == null)
                        {
                            Console.WriteLine("{0} doesn't exist in the key table. Can't extract {1}.", bifPath, resourcePath);
                            continue;
                        }
                        Console.WriteLine("Opening {0}...", bifPath);
                        bif = new BIF(bifPath, keyTable);
                        lastBIFPath = bifPath;
                    }

                    string resRef = Path.GetFileName(resourcePath);
                    if (resRef != "")
                    {
                        // Extract single resource:
                        KeyInfo keyInfo = keyTable.GetKeyInfo(bifPath, resRef);
                        if (keyInfo == null)
                        {
                            Console.WriteLine("{0} does not contain resource {1}", bifPath, resRef);
                            continue;
                        }
                        ExtractResource(keyInfo, bif, outputPath);
                    }
                    else
                    {
                        // Extract all resoures in the current BIF:
                        foreach (KeyTable.KeyInfo keyInfo in keyTable.Keys.Where(k => bifPath == k.FileInfo.Path))
                        {
                            ExtractResource(keyInfo, bif, outputPath);
                        }
                    }
                }
            }

            PauseOnExit();
        }


        static void ExtractResource (KeyInfo keyInfo, BIF bif, string outputPath)
        {
            if (keyInfo == null || bif == null)
            {
                return;
            }

            outputPath += "/" + keyInfo.FullPath;
            Console.WriteLine("Extracting {0} to {1}...", keyInfo.FullPath, outputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            byte[] buf = null;
            if (keyInfo.FixedIndex != 0)
            {
                buf = bif.GetFixedResource(keyInfo.FixedIndex);
            }
            else
            {
                buf = bif.GetVariableResource(keyInfo.VariableIndex);
            }
            using (BinaryWriter writer = new BinaryWriter (File.Open (outputPath, FileMode.Create)))
            {
                writer.Write(buf);
            }
        }

        static string GetDirectoryName (string path)
        {
            return Path.GetDirectoryName(path).Replace('\\', '/');
        }

        static void ShowHelp (OptionSet optionSet)
        {
            Console.WriteLine("Usage: {0} [OPTIONS]+", exectutableName);
            Console.WriteLine("Operate on files in the Bioware KEY/BIF format.");
            Console.WriteLine();
            Console.WriteLine("Options:");
            optionSet.WriteOptionDescriptions(Console.Out);
        }


        static void PauseOnExit()
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                Console.WriteLine("Enter to exit.");
                Console.ReadLine();
            }
        }
    }
}
