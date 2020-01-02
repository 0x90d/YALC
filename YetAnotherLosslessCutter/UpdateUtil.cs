using System;
using System.Threading.Tasks;
using Octokit;

namespace YetAnotherLosslessCutter
{
   static class UpdateUtil
    {
        public static async Task<bool?> IsNewVersionAvailable()
        {
            var client = new GitHubClient(new ProductHeaderValue("YALC"));

            var releases = await client.Repository.Release.GetAll("0x90d", "YALC");
            if (releases == null || releases.Count == 0) return null;
            var latest = releases[0];
            var currentVersion = new Version(YALCConstants.ASSEMBLY_VERSION);
            var latestVersion = new Version(latest.TagName);
            if (latestVersion.Major < currentVersion.Major) return false;
            if (latestVersion.Minor < currentVersion.Minor) return false;
            if (latestVersion.Build < currentVersion.Build) return false;
            return true;

        }
    }
}
