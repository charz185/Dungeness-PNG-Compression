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
        Dungeness.ProcCompressImg(@"C:\Users\CharlesZ\source\repos\StrandsPNGCompression\Dungeness-PNG-Compression\test.png","amog.bin",8);
        Dungeness.procDecompressImg("amog.bin","c.png");
        sw.Stop();
       Console.WriteLine(sw.ElapsedMilliseconds+"ms");
    }
    
}