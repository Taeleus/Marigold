using System;
using System.IO;
using System.Security.Cryptography;

namespace Marigold
{
    // Resolve once before opening any stores. Custom game data folders remain isolated.
    internal sealed class DataPaths
    {
        public string Root { get; private set; }
        public string Notice { get; private set; }

        public static DataPaths Initialize(string userDataPath, string roamingAppDataPath)
        {
            if (string.IsNullOrWhiteSpace(userDataPath) || !Path.IsPathRooted(userDataPath))
                throw new InvalidOperationException("The game's user data folder is not initialized.");
            if (string.IsNullOrWhiteSpace(roamingAppDataPath) || !Path.IsPathRooted(roamingAppDataPath))
                throw new InvalidOperationException("The previous Marigold data location could not be resolved.");

            string gameRoot = Path.GetFullPath(userDataPath);
            string roamingRoot = Path.GetFullPath(roamingAppDataPath);
            var paths = new DataPaths { Root = Path.Combine(gameRoot, "Storage", "Marigold") };
            if (Directory.Exists(paths.Root)) return paths;
            if (File.Exists(paths.Root)) throw new IOException("Marigold's data folder is occupied by a file: " + paths.Root);

            string previous = Path.Combine(roamingRoot, "Marigold");
            if (!Directory.Exists(previous)) return paths;
            string standard = Path.Combine(roamingRoot, "SpaceEngineers");
            if (!SamePath(gameRoot, standard))
            {
                paths.Notice = "Custom game data folder selected. Previous Marigold data was left at " + previous +
                    ". To import it, close the game and copy that folder to " + paths.Root + "; do not overwrite existing data.";
                return paths;
            }

            string storage = Path.GetDirectoryName(paths.Root);
            string staging = Path.Combine(storage, "Marigold.migrating-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(storage);
            try
            {
                CopyDirectory(previous, staging, 0);
                // Atomic publication, on the same filesystem. A concurrent destination is never merged or overwritten.
                Directory.Move(staging, paths.Root);
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is System.Security.SecurityException)
            {
                throw new IOException("Marigold data migration failed. Original data is preserved at " + previous +
                    ". The destination was not merged or overwritten. " + error.Message, error);
            }
            finally
            {
                // Only the fresh staging tree created by this attempt is cleaned up.
                if (Directory.Exists(staging)) Directory.Delete(staging, true);
            }
            paths.Notice = "Copied previous Marigold data to " + paths.Root + ". Originals remain at " + previous + ".";
            return paths;
        }

        private static bool SamePath(string a, string b)
        {
            return string.Equals(a.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                b.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), StringComparison.OrdinalIgnoreCase);
        }

        private static void CopyDirectory(string source, string target, int depth)
        {
            if (depth > 64) throw new IOException("Previous data has too many nested folders.");
            if ((File.GetAttributes(source) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Linked folders cannot be migrated automatically: " + source);
            Directory.CreateDirectory(target);
            foreach (string entry in Directory.GetFileSystemEntries(source))
            {
                var attributes = File.GetAttributes(entry);
                if ((attributes & FileAttributes.ReparsePoint) != 0)
                    throw new IOException("Linked files or folders cannot be migrated automatically: " + entry);
                string destination = Path.Combine(target, Path.GetFileName(entry));
                if ((attributes & FileAttributes.Directory) != 0) CopyDirectory(entry, destination, depth + 1);
                else CopyVerified(entry, destination);
            }
        }

        private static void CopyVerified(string source, string target)
        {
            // Hold the source against concurrent writes until the copied bytes are verified.
            using (var input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (var output = new FileStream(target, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    input.CopyTo(output);
                    output.Flush(true);
                }
                input.Position = 0;
                using (var algorithm = SHA256.Create())
                using (var copy = File.OpenRead(target))
                {
                    var expected = algorithm.ComputeHash(input);
                    var actual = algorithm.ComputeHash(copy);
                    for (int i = 0; i < expected.Length; i++)
                        if (expected[i] != actual[i]) throw new IOException("Copied data did not verify: " + source);
                }
            }
        }
    }
}
