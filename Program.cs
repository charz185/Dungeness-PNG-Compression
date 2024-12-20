using static System.Formats.Asn1.AsnWriter;
using System.Drawing;
using System.Collections;
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
        bool running = true;
        while (running)
        {
            Console.WriteLine("Path?");
            String path = Console.ReadLine();
            Console.WriteLine("Output Path?");
            String OutPath = Console.ReadLine();
            Console.WriteLine("batch Size?");
            int batchSize = Convert.ToInt32(Console.ReadLine());
            Console.WriteLine("(C/G)");
            String use = Console.ReadLine();
            Console.WriteLine("Search amount per tick (length)?");
            ulong length = Convert.ToUInt64(Console.ReadLine());
            Stopwatch sw = Stopwatch.StartNew();
            sw.Start();
            if (use.StartsWith("G"))
            {
                Console.WriteLine("GPU Count?: ");
                int gpuCount = Convert.ToInt16(Console.ReadLine());
                Dungeness.ProcCompressLargeImage(path, OutPath, false, 8, 8,gpuCount,batchSize, length);
            }
            else
            {
                Dungeness.ProcCompressImg(path, OutPath, true, batchSize);
            }
            sw.Stop();
            Dungeness.procDecompressImg(OutPath, "c.png");

            Console.WriteLine(sw.ElapsedMilliseconds + "ms");

            Console.WriteLine("Exit? (y/n)");
            String exit = Console.ReadLine();
            if (exit == "y")
            {
                running = false;
            }
        }
    }
}