using System;
using System.Diagnostics;
using System.IO;
using System.Xml.Linq;

namespace Highpoint.Sage.SystemDynamics.Utility
{
    public static class Program<T1> where T1 : StateBase<T1>, new()
    {
        public static void Run(
            string[] args, 
            Integrator integrator = Integrator.Euler, 
            XElement parameters = null, 
            string outputFileName = null,
            string header = null, 
            Action<TextWriter, T1> toWrite = null,
            T1 seed = null)
        {
            outputFileName = outputFileName ?? Path.GetTempPath() + typeof(T1).FullName + Guid.NewGuid() + string.Format("{0}.csv", integrator);

            using (TextWriter tw = new StreamWriter(outputFileName))
            {
                if (toWrite == null)
                {
                    // Create Header Row
                    var tmp2 = new T1(); // <-- BOGUS: Figure out a way to get it from a static.
                    tw.Write("Index,");
                    string[] stockNames = tmp2.StockNames();
                    foreach (string stockName in stockNames)
                    {
                        tw.Write("{0},", stockName);
                    }
                    tw.WriteLine();

                    // Create Data content
                    if ( seed == null ) seed = new T1();
                    seed.Configure(parameters);
                    foreach (T1 s in Behavior<T1>.Run(seed, integrator))
                    {
                        tw.Write("{0:0.00},", s.TimeSliceNdx*s.TimeStep);
                        foreach (var getter in s.StockGetters)
                        {
                            tw.Write("{0:0.000},", getter(s));
                        }
                        tw.WriteLine();
                    }
                }
                else
                {
                    tw.WriteLine(header ?? "");
                    foreach (T1 s in Behavior<T1>.Run(new T1(), integrator))
                    {
                        toWrite(tw, s);
                    }
                }
            }
            Process.Start("excel.exe", outputFileName);
        }
    }
}
