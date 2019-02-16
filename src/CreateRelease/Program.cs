using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreateRelease
{
    /// <summary>
    ///   Darn shame I can't figure out how to write this app in powershell.  This seems to me
    ///   the easiest way to create a way to create a ZIP file that's cross-platform.  Someday
    ///   it'd be cool if it could use the GitHub API's to actually push the release as well.
    /// </summary>
    internal class Program
    {
        static int Main(string[] args)
        {
            if (args.Length != 1 || !Directory.Exists(args[0]))
            {
                Console.Error.WriteLine("Usage: CreateRelease <release-folder>");
                return 1;
            }

            string gameDataPath = Path.Combine(args[0], "GameData");
            if (!Directory.Exists(gameDataPath))
            {
                Console.Error.WriteLine($"Error: {args[0]} should contain a folder called 'gamedata'");
                return 1;
            }

            string pksZipFile = Path.Combine(args[0], "ProgressiveColonizationSystem.zip");

            try
            {
                File.Delete(pksZipFile);
                ZipFile.CreateFromDirectory(gameDataPath, pksZipFile, CompressionLevel.Optimal, includeBaseDirectory: true);
            }
            catch(IOException ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return 1;
            }

            return 0;
        }
    }
}
