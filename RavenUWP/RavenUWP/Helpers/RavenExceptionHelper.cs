using RavenUWP.Models;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

[assembly: InternalsVisibleTo("RavenUWP.Tests")]

namespace RavenUWP.Helpers
{
    internal static class RavenExceptionHelper
    {
        private const string _stacktraceRegex = @"([\w]*)at (?<path>.*)\.(?<method>.*(.*))([\w]*)([in]*)(?<file>.*)([:line]*)(?<line>\d*)";

        internal static IEnumerable<RavenFrame> ToRavenFrames(this Exception ex)
        {
            do
            {
                var frame = ParseStacktraceString(ex.StackTrace);
                if (frame == null)
                    yield break;
                else
                    yield return frame;

                ex = ex.InnerException;
            }
            while (ex != null);
        }

        internal static RavenFrame ParseStacktraceString(string stacktrace)
        {
            if (!String.IsNullOrEmpty(stacktrace))
            {
                Regex r = new Regex(_stacktraceRegex);
                MatchCollection matches = r.Matches(stacktrace);
                foreach (var match in matches)
                {
                    var result = r.Match(match.ToString().Replace("\r", ""));
                    if (result.Success)
                    {
                        return new RavenFrame()
                        {
                            Filename = result.Groups["path"].Value.ToString(),
                            Method = result.Groups["method"].Value.ToString()
                        };
                    }
                }
            }

            return null;
        }
    }
}
