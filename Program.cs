using static System.Formats.Asn1.AsnWriter;
using System.Drawing;
using System.Collections;
using System.Drawing.Printing;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Drawing.Imaging;
using static System.Net.Mime.MediaTypeNames;
using System.Diagnostics.Metrics;
using System.Collections.Immutable;
using System.Data.SqlTypes;
internal static class StrandsPNGCompression
{
    public static void Main(string[] args)
    {
        Stopwatch sw = Stopwatch.StartNew();
        sw.Start();
        Dungeness.ProcCompressImg("c.png","amog.bin",16);
        sw.Stop();
        //Dungeness.procDecompressImg("amog.bin","c.png");

        Console.WriteLine(sw.ElapsedMilliseconds+"ms");
    }
    
}