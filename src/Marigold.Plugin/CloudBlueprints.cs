using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sandbox.Engine.Networking;

namespace Marigold
{
    internal static class CloudBlueprints
    {
        public static string[] List()
        {
            var result = new List<string>();
            var files = MyGameService.GetCloudFiles("Blueprints/cloud");
            if (files != null)
                foreach (var file in files)
                {
                    if (file.Size <= 32 * 1024 * 1024 && file.Name.EndsWith("/bp.sbc", StringComparison.OrdinalIgnoreCase))
                        result.Add("cloud:" + file.Name);
                    if (result.Count >= 500)
                        break;
                }

            result.Sort(StringComparer.OrdinalIgnoreCase);
            return result.ToArray();
        }

        public static Task<BlueprintDocument> Load(string path)
        {
            if (!path.StartsWith("Blueprints/cloud/", StringComparison.OrdinalIgnoreCase)
                || !path.EndsWith("/bp.sbc", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Select a cloud blueprint from the list.");
            var completion = new TaskCompletionSource<byte[]>();
            if (!MyGameService.LoadFromCloudAsync(path, bytes => completion.TrySetResult(bytes)))
                throw new InvalidOperationException("Cloud download could not start. Check your game service connection.");
            return Parse(completion.Task);
        }

        private static async Task<BlueprintDocument> Parse(Task<byte[]> download)
        {
            if (await Task.WhenAny(download, Task.Delay(30000)).ConfigureAwait(false) != download)
                throw new TimeoutException("Cloud download timed out. Try again.");
            var bytes = await download.ConfigureAwait(false);
            return await Task.Run(() => BlueprintDocument.FromBytes(bytes)).ConfigureAwait(false);
        }
    }
}
