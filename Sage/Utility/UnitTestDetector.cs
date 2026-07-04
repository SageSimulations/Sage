/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Linq;

namespace Highpoint.Sage.Utility
{
    public static class UnitTestDetector
    {
        static UnitTestDetector()
        {
            string[] testAssemblyNames =
            {
                "Microsoft.VisualStudio.TestPlatform.TestFramework",
                "nunit.framework",
                "xunit.core"
            };
            UnitTestDetector.IsInUnitTest = AppDomain.CurrentDomain.GetAssemblies()
                .Any(a => testAssemblyNames.Any(n => a.FullName.StartsWith(n, StringComparison.OrdinalIgnoreCase)));
        }

        public static bool IsInUnitTest { get; private set; }
    }


}
