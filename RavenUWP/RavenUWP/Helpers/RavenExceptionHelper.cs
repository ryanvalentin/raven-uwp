using RavenUWP.Models;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace RavenUWP.Helpers
{
    internal static class RavenExceptionHelper
    {
        private const string _stacktraceRegex = @"([\w]*)at (?<path>.*)\.(?<method>.*(.*))([\w]*)([in]*)(?<file>.*)([:line]*)(?<line>\d*)";

        internal static IEnumerable<RavenFrame> ToRavenFrames(this Exception ex)
        {
            do
            {
                if (String.IsNullOrEmpty(ex.StackTrace))
                    yield break;

                Regex r = new Regex(_stacktraceRegex);
                MatchCollection matches = r.Matches(ex.StackTrace);
                foreach (var match in matches)
                {
                    var result = r.Match(match.ToString().Replace("\r", ""));
                    if (result.Success)
                    {
                        yield return new RavenFrame()
                        {
                            Filename = result.Groups["path"].Value.ToString(),
                            Method = result.Groups["method"].Value.ToString()
                        };
                    }
                }

                ex = ex.InnerException;
            }
            while (ex != null);
        }
    }
}
