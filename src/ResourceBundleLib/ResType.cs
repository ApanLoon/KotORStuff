using System;
using System.Collections.Generic;

namespace ResourceBundleLib
{
    public class ResourceType
    {
        public static Dictionary<UInt16, ResourceType> ByResType
        {
            get
            {
                if (byResType == null)
                {
                    Init();
                }
                return byResType;
            }
        }
        protected static Dictionary<UInt16, ResourceType> byResType;

        public static List<ResourceType> AllTypes;

        public static void Init()
        {
            byResType = new Dictionary<ushort, ResourceType>();
            AllTypes = new List<ResourceType>();

            AllTypes.Add(new ResourceType(0xFFFF, "",    "N/A",        "Invalid resource type"));
            AllTypes.Add(new ResourceType(1,      ".bmp", "binary",     "Windows BMP file"));
            AllTypes.Add(new ResourceType(3,      ".tga", "binary",     "TGA image format"));
            AllTypes.Add(new ResourceType(4,      ".wav", "binary",     "WAV sound file"));
            AllTypes.Add(new ResourceType(6,      ".plt", "binary",     "Bioware Packed Layered Texture, used for player character skins, allows for multiple color layers"));
            AllTypes.Add(new ResourceType(7,      ".ini", "text (ini)", "Windows INI file format"));
            AllTypes.Add(new ResourceType(10,     ".txt", "text",       "Text file"));
            AllTypes.Add(new ResourceType(2002,   ".mdl", "mdl",        "Aurora model"));
            AllTypes.Add(new ResourceType(2009,   ".nss", "text",       "NWScript Source"));
            AllTypes.Add(new ResourceType(2010,   ".ncs", "binary",     "NWScript Compiled Script"));
            AllTypes.Add(new ResourceType(2012,   ".are", "gff",        "BioWare Aurora Engine Area file. Contains information on what tiles are located in an area, as well as other static area properties that cannot change via scripting. For each .are file in a .mod, there must also be a corresponding .git and .gic file having the same ResRef."));
            AllTypes.Add(new ResourceType(2013,   ".set", "text (ini)", "BioWare Aurora Engine Tileset"));
            AllTypes.Add(new ResourceType(2014,   ".ifo", "gff",        "Module Info File. See the IFO Format document."));
            AllTypes.Add(new ResourceType(2015,   ".bic", "gff",        "Character/Creature"));
            AllTypes.Add(new ResourceType(2016,   ".wok", "mdl",        "Walkmesh"));
            AllTypes.Add(new ResourceType(2017,   ".2da", "text",       "2-D Array"));
            AllTypes.Add(new ResourceType(2022,   ".txi", "text",       "Extra Texture Info"));
            AllTypes.Add(new ResourceType(2023,   ".git", "gff",        "Game Instance File. Contains information for all object instances in an area, and all area properties that can change via scripting."));
            AllTypes.Add(new ResourceType(2025,   ".uti", "gff",        "Item Blueprint"));
            AllTypes.Add(new ResourceType(2027,   ".utc", "gff",        "Creature Blueprint"));
            AllTypes.Add(new ResourceType(2029,   ".dlg", "gff",        "Conversation File"));
            AllTypes.Add(new ResourceType(2030,   ".itp", "gff",        "Tile/Blueprint Palette File"));
            AllTypes.Add(new ResourceType(2032,   ".utt", "gff",        "Trigger Blueprint"));
            AllTypes.Add(new ResourceType(2033,   ".dds", "binary",     "Compressed texture file"));
            AllTypes.Add(new ResourceType(2035,   ".uts", "gff",        "Sound Blueprint"));
            AllTypes.Add(new ResourceType(2036,   ".ltr", "binary",     "Letter-combo probability info for name generation"));
            AllTypes.Add(new ResourceType(2037,   ".gff", "gff",        "Generic File Format. Used when undesirable to create a new file extension for a resource, but the resource is a GFF. (Examples of GFFs include itp, utc, uti, ifo, are, git)"));
            AllTypes.Add(new ResourceType(2038,   ".fac", "gff",        "Faction File"));
            AllTypes.Add(new ResourceType(2040,   ".ute", "gff",        "Encounter Blueprint"));
            AllTypes.Add(new ResourceType(2042,   ".utd", "gff",        "Door Blueprint"));
            AllTypes.Add(new ResourceType(2044,   ".utp", "gff",        "Placeable Object Blueprint"));
            AllTypes.Add(new ResourceType(2045,   ".dft", "text (ini)", "Default Values file. Used by area properties dialog"));
            AllTypes.Add(new ResourceType(2046,   ".gic", "gff",        "Game Instance Comments. Comments on instances are not used by the game, only the toolset, so they are stored in a gic instead of in the git with the other instance properties."));
            AllTypes.Add(new ResourceType(2047,   ".gui", "gff",        "Graphical User Interface layout used by game"));
            AllTypes.Add(new ResourceType(2051,   ".utm", "gff",        "Store/Merchant Blueprint"));
            AllTypes.Add(new ResourceType(2052,   ".dwk", "mdl",        "Door walkmesh"));
            AllTypes.Add(new ResourceType(2053,   ".pwk", "mdl",        "Placeable Object walkmesh"));
            AllTypes.Add(new ResourceType(2056,   ".jrl", "gff",        "Journal File"));
            AllTypes.Add(new ResourceType(2058,   ".utw", "gff",        "Waypoint Blueprint. See Waypoint GFF document."));
            AllTypes.Add(new ResourceType(2060,   ".ssf", "binary",     "Sound Set File. See Sound Set File Format document"));
            AllTypes.Add(new ResourceType(2064,   ".ndb", "binary",     "Script Debugger File"));
            AllTypes.Add(new ResourceType(2065,   ".ptm", "gff",        "Plot Manager file/Plot Instance"));
            AllTypes.Add(new ResourceType(2066,   ".ptt", "gff",        "Plot Wizard Blueprint"));

            // KotOR:
            AllTypes.Add(new ResourceType(2024,   ".bti", "binary",     "Item Blueprint"));     // templates.bif/nw_wblhvy001
            AllTypes.Add(new ResourceType(2026,   ".btc", "binary",     "Creature Blueprint")); // templates.bif/nw_drmark1 templates.bif/partymember templates.bif/sw_krayt
            AllTypes.Add(new ResourceType(3000,   ".lyt", "text",       "Room layout. How scene terrain connects."));
            AllTypes.Add(new ResourceType(3001,   ".vis", "text",       "?"));                  // lightmaps.bif
            AllTypes.Add(new ResourceType(3007,   ".tpc", "binary",     "Texture"));
            AllTypes.Add(new ResourceType(3008,   ".mdl", "binary",     "Aurora model"));


            foreach (ResourceType rt in AllTypes)
            {
                ByResType[rt.ResType] = rt;
            }
        }

        public UInt16 ResType;
        public string FileExtension;
        public string ContentType;
        public string Description;

        public ResourceType (UInt16 resType, string fileExtension, string contentType, string description)
        {
            ResType = resType;
            FileExtension = fileExtension;
            ContentType = contentType;
            Description = description;
        }
    }
}
