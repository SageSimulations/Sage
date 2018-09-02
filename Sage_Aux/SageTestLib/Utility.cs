using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Highpoint.Sage.Materials.Chemistry;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SageTestLib
{
    static class Utility
    {
        public static string SAGE_ROOT { get; private set; }

        static Utility ()
        {
            const string targetDir = "Sage";
            string devRoot = typeof (Substance).Assembly.Location;
            devRoot = devRoot.Substring(0,
                devRoot.LastIndexOf(Path.DirectorySeparatorChar + targetDir + Path.DirectorySeparatorChar,
                    StringComparison.Ordinal) + targetDir.Length + 1);
            Assert.IsTrue(Directory.Exists(devRoot));
            SAGE_ROOT = devRoot;
        }
    }
}
