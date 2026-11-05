using System;
using System.Globalization;
using System.IO;

namespace Marigold
{
    internal static class WorkshopAssetPath
    {
        public const string SpaceEngineersId = "244850";

        // A server can send its own absolute path. Use only the Workshop ID and asset suffix.
        public static bool TrySplit(string asset, string assetFolder, out string modId, out string relativePath)
        {
            modId = relativePath = null;
            if (string.IsNullOrEmpty(asset))
                return false;

            var parts = asset.Replace('\\', '/').Split('/');
            for (int i = parts.Length - 4; i >= 0; i--)
            {
                if (parts[i] != SpaceEngineersId
                    || !ulong.TryParse(parts[i + 1], NumberStyles.None, CultureInfo.InvariantCulture, out ulong id)
                    || id == 0 || !parts[i + 2].Equals(assetFolder, StringComparison.OrdinalIgnoreCase))
                    continue;

                for (int j = i + 2; j < parts.Length; j++)
                    if (string.IsNullOrWhiteSpace(parts[j]) || parts[j] == "." || parts[j] == ".."
                        || parts[j].IndexOf(':') >= 0 || parts[j].IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                        return false;

                modId = id.ToString(CultureInfo.InvariantCulture);
                relativePath = string.Join(Path.DirectorySeparatorChar.ToString(), parts, i + 2, parts.Length - i - 2);
                return true;
            }
            return false;
        }
    }
}
