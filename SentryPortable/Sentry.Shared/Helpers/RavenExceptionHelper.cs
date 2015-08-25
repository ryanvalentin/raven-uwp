using Sentry.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

[assembly: InternalsVisibleTo("Sentry.UWP.Tests")]

namespace Sentry.Helpers
{
    internal static class RavenExceptionHelper
    {
        private const string _stacktraceRegex = @"([\w]*)at (?<path>.*)\.(?<method>.*(.*))([\w]*)([in]*)(?<file>.*)([:line]*)(?<line>\d*)";

        internal static IEnumerable<RavenException> EnumerateAllExceptions(this Exception ex)
        {
            do
            {
                List<RavenFrame> frames = ex.StackTrace?.ParseStacktraceString().ToList();
                yield return new RavenException()
                {
                    Stacktrace = new RavenStacktrace()
                    {
                        Frames = frames
                    },
                    Module = ex.Source,
                    Type = ex.GetBaseException().GetType().FullName,
                    Value = ex.Message
                };

                ex = ex.InnerException;
            }
            while (ex != null);
        }

        internal static IEnumerable<RavenFrame> ParseStacktraceString(this string stacktrace)
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
                        yield return new RavenFrame()
                        {
                            Filename = result.Groups["path"].Value.ToString(),
                            Method = result.Groups["method"].Value.ToString()
                        };
                    }
                }
            }

            yield break;
        }
    }
}
