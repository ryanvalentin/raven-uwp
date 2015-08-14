using RavenClient.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RavenClient.Helpers
{
    public static class RavenExceptionHelper
    {
        private const string _stacktraceRegex = @"([\w]*)at (?<path>.*)\.(?<method>.*(.*))([\w]*)([in]*)(?<file>.*)([:line]*)(?<line>\d*)";

        public static IEnumerable<RavenJsonFrame> ToRavenFrames(this Exception ex)
        {
            do
            {
                if (!String.IsNullOrEmpty(ex.StackTrace))
                {
                    Regex r = new Regex(_stacktraceRegex);
                    MatchCollection matches = r.Matches(ex.StackTrace);
                    foreach (var match in matches)
                    {
                        var result = r.Match(match.ToString().Replace("\r", ""));
                        if (result.Success)
                        {
                            string exPath = result.Groups["path"].Value.ToString();
                            string method = result.Groups["method"].Value.ToString().Replace("\r", "");
                            yield return new RavenJsonFrame()
                            {
                                Filename = exPath,
                                Method = method
                            };
                        }
                    }
                }

                ex = ex.InnerException;
            }
            while (ex.InnerException != null);
        }
    }
}
