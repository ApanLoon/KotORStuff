using NDesk.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

using TPCLib;
using System.Drawing.Imaging;
using System.Text;
using System.Drawing;

namespace tpc
{
    /* Test command lines:
List the data, including TXI:
-o none -t PO_PFHA3d.tpc

Create a new TPC with one imag, two sub images and add some TXI attributes:
-o tpc -s 2 --outpath PO_PFHA3d.tpc png/PO_PFHA3d.png -txi="channelscale 4 0.1 0.2 0.3 0.4" --txi="test1 ata" --txi="test2 ata" -txi="test3 hoglu" -txi="test4 hulu"
     */
    class tpc
    {
        static string exectutableName;

        static bool               outputToFile   = true;
        static string             outExtension   = "none";
        static string             outPath        = "";
        static int                imageIndex     = 0;
        static int                subImageIndex  = 0;
        static bool               dumpTXI        = false;
        static List<string>       txi            = new List<string>();
        static string             txiPath        = "";
        static bool               useCompression = false;
        static TPC.EncodingFormat format         = TPC.EncodingFormat.RGB;
        static bool               showHelp       = false;

        static List<string> inPaths;

        static Dictionary<string, ImageFormat> imageFormats = new Dictionary<string, ImageFormat>()
        {
            {"png", ImageFormat.Png },
            {"tif", ImageFormat.Tiff },
            {"raw", null },
            {"hex", null },
            {"tpc", null },
            {"none", null }
        };

        static void Main(string[] args)
        {
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            exectutableName = Path.GetFileName(codeBase);

            var optionSet = new OptionSet()
            {
                { "n|nofile",     "Don't output to a file, instead use stdout.",
                                  v => outputToFile = v == null },

                { "o|output=",   "The format of the output file(s). Possible options are: "
                                 + string.Join (", ", imageFormats.Keys),
                                 v => outExtension = v },

                { "i|image=",    "The index of the image to extract. ",
                                 v => imageIndex = int.Parse(v) },

                { "s|subimage=", "The index of the sub image to extract or the number of sub images to generate in new TPC. ",
                                 v => subImageIndex  = int.Parse(v) },

                { "t|txi:",       "Dump the texture info or add TXI attribute to new TPC. When adding an attribute, use the form --txi=\"key value...\"",
                                  v => 
                                  {
                                      dumpTXI = true; // TODO: This makes it impossible to explicitly disable TXI dump. "-t-" will still enable it.
                                      if (v != "")
                                      {
                                          txi.Add (v);
                                      }
                                  } },

                { "txipath=",   "Create this separate TXI file when reading a TPC or read this separate TXI file when creating a TPC.",
                                 v => txiPath = v },

                { "c|compress",  "Use DXT compression when creating new TPC. ",
                                 v => useCompression = v != null },

                { "f|format=",   "Pixel format for new TPC. Possible options are: "
                                 + string.Join (", ", Enum.GetNames(typeof(TPC.EncodingFormat))),
                                 v => format         = (TPC.EncodingFormat)Enum.Parse(typeof(TPC.EncodingFormat), v) },

                { "p|outpath=", "Path and file name of output. Default is the same as the input with a new extension.",
                                 v => outPath = v },

                { "h|help",      "Show this message and exit",
                                v => showHelp       = v != null }
            };

            try
            {
                inPaths = optionSet.Parse(args);
            }
            catch (OptionException e)
            {
                Console.Write("{0}: ", exectutableName);
                Console.WriteLine(e.Message);
                Console.WriteLine("Try `{0} --help' for more information.", exectutableName);
                PauseOnExit();
                return;
            }

            if (showHelp)
            {
                ShowHelp(optionSet);
                PauseOnExit();
                return;
            }

            if (outExtension == "tpc")
            {
                try
                {
                    CreateTPC();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(string.Format("Error: {0}\n{1}\n{2}", ex.Message, ex.InnerException, ex.StackTrace));
                }
            }
            else
            {
                foreach (string inPath in inPaths)
                {
                    try
                    {
                        ConvertTPC(inPath);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(string.Format("Error on file {0}: {1}\n{2}\n{3}", inPath, ex.Message, ex.InnerException, ex.StackTrace));
                    }
                }
            }

            PauseOnExit();
        }

        static void ConvertTPC(string inPath)
        {
            TPC tpc = new TPC(inPath);
            PrintInfo(inPath, tpc);
            WriteTXI(inPath, tpc);
            WriteImage(inPath, tpc);
        }

        private static void PrintInfo(string inPath, TPC tpc)
        {
            if (outputToFile) // Only output this if we are not using stdout for the data.
            {
                Console.WriteLine(string.Format("{0} {1,-9} {2, 8} {3, -4} {4,9} {5}",
                    flagsToString(tpc),
                    string.Format("{0,4} {1,4}", tpc.Width, tpc.Height),
                    string.Format("{0, 2} {1, 2} {2, 2}", tpc.ImageCount, tpc.SubImageCount, tpc.totalSubImageCount),
                    tpc.Format,
                    tpc.Unknown1,
                    inPath));

                //Console.WriteLine("{0} {1, 20} {2, 10} {3, -9} {4, 8} {5, -4} {6}",
                //    flagsToString(tpc),
                //    string.Format("{0:x}/{1:x}/{2:x}", tpc.ReadDataSize, tpc.BaseImageDataSize, tpc.streamSize),
                //    tpc.Unknown1,
                //    string.Format("{0,4} {1,4}", tpc.Width, tpc.Height),
                //    string.Format("{0, 2} {1, 2} {2, 2}", tpc.ImageCount, tpc.SubImageCount, tpc.totalSubImageCount),
                //    tpc.Encoding,
                //    inPath
                //    );

                //if (dumpTXI)
                //{
                //    foreach (string key in tpc.TXI)
                //    {
                //        Console.WriteLine(key + " " + tpc.TXI[key]);
                //    }
                //}
            }
        }

        private static void WriteTXI(string inPath, TPC tpc)
        {
            if (!dumpTXI)
            {
                return;
            }

            Stream outStream;

            if (outputToFile)
            {
                string path = (txiPath != "") ? txiPath : Path.GetFileNameWithoutExtension(inPath) + ".txi";
                outStream = new FileStream(path, FileMode.Create);
            }
            else
            {
                outStream = Console.OpenStandardOutput();
            }

            using (BinaryWriter writer = new BinaryWriter(outStream))
            {
                tpc.TXI.Save(writer);
            }
        }

        private static void WriteImage(string inPath, TPC tpc)
        {
            if (outExtension == "" || outExtension == "none" || !imageFormats.ContainsKey(outExtension))
            {
                return;
            }

            Stream outStream;
            Exception error = null;

            if (outputToFile)
            {
                string path = (outPath != "") ? outPath : Path.GetFileNameWithoutExtension(inPath) + "." + outExtension;
                outStream = new FileStream(path, FileMode.Create);
            }
            else
            {
                outStream = Console.OpenStandardOutput();
            }

            try
            {
                if (outExtension == "raw")
                {
                    TPC.Image image = tpc.Images[imageIndex];
                    TPC.SubImage subImage = image.SubImages[subImageIndex];
                    outStream.Write(subImage.Data, 0, (int)subImage.DataSize);
                }
                else if (outExtension == "hex")
                {
                    TPC.Image image = tpc.Images[imageIndex];
                    TPC.SubImage subImage = image.SubImages[subImageIndex];

                    byte[] tmp = Encoding.ASCII.GetBytes(HexDump(subImage.Data, (int)subImage.DataSize));
                    outStream.Write(tmp, 0, tmp.Length);

                }
                else
                {
                    tpc[imageIndex, subImageIndex].Save(outStream, imageFormats[outExtension]);
                }
            }
            catch (Exception ex)
            {
                error = ex;
            }

            outStream.Flush();
            outStream.Close();

            if (error != null)
            {
                throw error;
            }
        }

        static void CreateTPC()
        {
            if (outPath == "")
            {
                throw new Exception("outpath must be specified when creating TPCs.");
            }

            if (subImageIndex == 0)
            {
                subImageIndex = 1;
            }

            TPC tpc = new TPC(useCompression, format, subImageIndex);

            foreach (string s in txi)
            {
                int i = s.IndexOf(' ');
                if (i == -1)
                {
                    throw new Exception("Syntax error in TXI attribute. (" + s + ")");
                }
                string key = s.Substring(0, i);
                string value = s.Substring(i + 1);
                tpc.TXI[key] = value;
            }

            foreach (string inPath in inPaths)
            {
                Bitmap bitmap = new Bitmap(inPath);
                tpc.AddImage(bitmap);
            }

            Stream outStream;
            if (outputToFile)
            {
                outStream = new FileStream(outPath, FileMode.Create);
            }
            else
            {
                outStream = Console.OpenStandardOutput();
            }

            tpc.Save(outStream);

            outStream.Flush();
            outStream.Close();
        }

        static string HexDump (byte[] buf, int length)
        {
            string hexDump = "";
            int address = 0;
            string ascii;
            string bytes;
            while (address < length)
            {
                bytes = "";
                ascii = "";
                for (int i = 0; i < Math.Min(16, buf.Length - address); i++)
                {
                    bytes += String.Format("{1}{0:x2} ", buf[address + i], (i != 0)  && (i % 8 == 0) ? " " : "");
                    if (buf[address + i] >= 0x20 && buf[address + i] <= 0x7e)
                    {
                        ascii += Encoding.ASCII.GetChars(buf, address + i, 1)[0];
                    }
                    else
                    {
                        ascii += ".";
                    }
                }
                hexDump += String.Format("{0:x8}: {1,-49} {2}\n", address, bytes, ascii);
                address += 16;
            }
            return hexDump;
        }

        static string flagsToString (TPC tpc)
        {
            return   (tpc.isCompressed ? "z" : "-")
                   + (tpc.isCubeMap    ? "c" : "-");
        }

        static void ShowHelp(OptionSet optionSet)
        {
            Console.WriteLine("Usage: {0} [OPTIONS]+ [FILE]+", exectutableName);
            Console.WriteLine();
            Console.WriteLine("Operate on files in the Bioware TPC format.");
            Console.WriteLine();
            Console.WriteLine("Options:");
            optionSet.WriteOptionDescriptions(Console.Out);
            Console.WriteLine();
            Console.WriteLine("Unless the nofile option is given, {0} prints information about the file", exectutableName);
            Console.WriteLine("on stdout. This example explains what is shown:");
            Console.WriteLine();
            Console.WriteLine("zc   64   64  6  7 42 RGBA         1 CM_rakata.tpc");
            Console.WriteLine();
            Console.WriteLine("  Flags:                      z=DXT compression, c=Cube map");
            Console.WriteLine();
            Console.WriteLine("  Width Height:               Of the full-size version of the image(s).");
            Console.WriteLine("                              Each additional sub image, if any is reduced");
            Console.WriteLine("                              in size by half width and half height.");
            Console.WriteLine();
            Console.WriteLine("  Number of images:           The number of full-size images. Cube maps have");
            Console.WriteLine("                              six images.");
            Console.WriteLine();
            Console.WriteLine("  Sub images per image:       The number of sub images for each image.");
            Console.WriteLine("                              The first is the full-size image, subsequent");
            Console.WriteLine("                              sub images are mip maps.");
            Console.WriteLine();
            Console.WriteLine("  Total number of sub images: The header of some files indicate more sub");
            Console.WriteLine("                              images than there is data for in the file.");
            Console.WriteLine("                              This shows how many sub images there actually");
            Console.WriteLine("                              are.");
            Console.WriteLine();
            Console.WriteLine("  Pixel format:               Gray, RGB, RGBA, SwizzledBGRA");
            Console.WriteLine();
            Console.WriteLine("  Unknown1:                   Float value of unknown purpose.");
            Console.WriteLine();
            Console.WriteLine("  Full path to the file");
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
