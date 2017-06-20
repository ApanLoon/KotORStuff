using NDesk.Options;
using ResourceBundleLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using static ResourceBundleLib.ERF;

namespace erf
{
    class erf
    {
        static string exectutableName;

        static void Main(string[] args)
        {
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            exectutableName = Path.GetFileName(codeBase);

            string erfPath = "";
            List<string> resRefs = new List<string>();
            bool showHelp = false;
            bool list = false;
            bool extract = false;
            string outputPath = "";

            var optionSet = new OptionSet()
            {
                {"e|erf=",      "The ERF file to operate on.", v => erfPath = v },
                {"r|resource=", "The resource files to operate on.", v => resRefs.Add(v) },
                {"l|list",      "List. If BIFs are given, all resources of those BIFs are listed. Otherwise all BIFs of the key tables are listed.", v => list = v != null },
                {"x|extract",   "Extract. If BIFs are given, all resources of those BIFs are listed. Otherwise all BIFs of the key tables are listed.", v => extract = v != null },
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
            if (!showHelp && erfPath == "")
            {
                Console.WriteLine("No ERF set.");
                showHelp = true;
            }

            if (extract && outputPath == "")
            {
                Console.WriteLine("Extracting requires an output path.");
                showHelp = true;
            }


            if (showHelp)
            {
                ShowHelp(optionSet);
                PauseOnExit();
                return;
            }

            if (extra.Count > 0)
            {
                Console.WriteLine("Ignoring extra arguments: {0}", string.Join(" ", extra.ToArray()));
            }

            ERF erf = new ERF(erfPath);

            if (list)
            {
                foreach (ERF.LocalizedString s in erf.Strings)
                {
                    Console.WriteLine(s.ToString());
                }

                if (resRefs.Count != 0)
                {
                    // List the specified resources:
                    foreach (string resRef in resRefs)
                    {
                        ERFKey key = erf[resRef];
                        if (key == null)
                        {
                            Console.WriteLine("{0} not found.", resRef);
                            continue;
                        }
                        Console.WriteLine(key.ToString());
                    }
                }
                else
                {
                    // List all resources:
                    foreach (ERFKey erfKey in erf.Keys)
                    {
                        Console.WriteLine(erfKey.ToString());
                    }
                }
            }

            if (extract)
            {
                if (resRefs.Count != 0)
                {
                    // Extract the specified resources:
                    foreach (string resRef in resRefs )
                    {
                        ERFKey key = erf[resRef];
                        if (key == null)
                        {
                            Console.WriteLine("{0} doesn't exist in {1}. Can't extract.", resRef, erfPath);
                            continue;
                        }
                        ExtractResource(key, erf, outputPath);
                    }
                }
                else
                {
                    // Extract all resources in the ERF.
                    foreach (ERFKey key in erf.Keys)
                    {
                        ExtractResource(erf[key.ResRef], erf, outputPath);
                    }
                }
            }


            PauseOnExit();
        }

        static void ExtractResource(ERFKey key, ERF erf, string outputPath)
        {
            if (key == null || erf == null)
            {
                return;
            }
            outputPath += "/" + key.ResRef + key.ResType.FileExtension;
            Console.WriteLine("Extracting {0} to {1}...", key.ResRef + key.ResType.FileExtension, outputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            byte[] buf = erf.GetResourceData(key);
            using (BinaryWriter writer = new BinaryWriter(File.Open(outputPath, FileMode.Create)))
            {
                writer.Write(buf);
            }
        }


        static void ShowHelp(OptionSet optionSet)
        {
            Console.WriteLine("Usage: {0} [OPTIONS]+", exectutableName);
            Console.WriteLine("Operate on files in the Bioware ERF format.");
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
