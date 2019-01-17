using PrismaDB.QueryParser.MySQL;
using System;
using System.Diagnostics;

namespace ParserBenchmark
{
    class Program
    {
        static void Main(string[] args)
        {
            var watch = Stopwatch.StartNew();
            MySqlParser.ParseToAst("select abc from def LIMIT 1;");
            watch.Stop();
            Console.WriteLine($"Run 1: {watch.ElapsedMilliseconds}");

            watch = Stopwatch.StartNew();
            MySqlParser.ParseToAst("select def from def LIMIT 1;");
            watch.Stop();
            Console.WriteLine($"Run 2: {watch.ElapsedMilliseconds}");


            watch = Stopwatch.StartNew();
            MySqlParser.ParseToAst("select tyrtyrtyrtyr from def LIMIT 1;");
            watch.Stop();
            Console.WriteLine($"Run 3: {watch.ElapsedMilliseconds}");

            watch = Stopwatch.StartNew();
            MySqlParser.ParseToAst("select 76543 from def LIMIT 1;");
            watch.Stop();
            Console.WriteLine($"Diff query type run 1: {watch.ElapsedMilliseconds}");

            watch = Stopwatch.StartNew();
            MySqlParser.ParseToAst("select 123 from def LIMIT 1;");
            watch.Stop();
            Console.WriteLine($"Diff query type run 2: {watch.ElapsedMilliseconds}");

            watch = Stopwatch.StartNew();
            MySqlParser.ParseToAst("select abc, 123, gsdfgkljsdfg from def LIMIT 1;");
            watch.Stop();
            Console.WriteLine($"Diff query type run 1: {watch.ElapsedMilliseconds}");

            watch = Stopwatch.StartNew();
            for (var i = 0; i < 1000000; i++)
            {
                MySqlParser.ParseToAst($"select {PrismaDB.Commons.Helper.GetRandomString(12)} from {PrismaDB.Commons.Helper.GetRandomString(12)} LIMIT 1;");
            }
            watch.Stop();
            Console.WriteLine($"100000 similar queries: {watch.ElapsedMilliseconds}");

        }
    }
}
