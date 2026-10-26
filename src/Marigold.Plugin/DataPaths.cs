using System;
using System.IO;

namespace Marigold
{
    // All stores belong to the active game's user-data folder. No fallback or imports.
    internal sealed class DataPaths
    {
        public string Root { get; private set; }

        public static DataPaths Initialize(string userDataPath)
        {
            if (string.IsNullOrWhiteSpace(userDataPath) || !Path.IsPathRooted(userDataPath))
                throw new InvalidOperationException("The game's user data folder is not initialized.");

            string root = Path.Combine(Path.GetFullPath(userDataPath), "Storage", "Marigold");
            if (File.Exists(root)) throw new IOException("Marigold's data folder is occupied by a file: " + root);
            return new DataPaths { Root = root };
        }
    }
}
