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
            MySqlQueryParser.ParseToAst("SELECT NULL;");
            watch.Stop();
            Console.WriteLine($"Run 1: {watch.ElapsedMilliseconds} ms");

            watch = Stopwatch.StartNew();
            MySqlQueryParser.ParseToAst("SELECT (a+b)*(a+b), ((a+b)*(a+b)), (((a+b)*(a+b))) FROM t WHERE (a < b) AND (t.b <= a) AND c IN ('abc', 'def') AND d NOT IN (123, 456) GROUP BY t.a, b ORDER BY a ASC, b DESC, c");
            watch.Stop();
            Console.WriteLine($"Run 2: {watch.ElapsedMilliseconds} ms");

            watch = Stopwatch.StartNew();
            MySqlQueryParser.ParseToAst("SELECT * FROM tbl1 WHERE col1 IS NOT NULL AND col2 IS NULL");
            watch.Stop();
            Console.WriteLine($"Run 3: {watch.ElapsedMilliseconds} ms");

            watch = Stopwatch.StartNew();
            MySqlQueryParser.ParseToAst("SELECT CONNECTION_ID()");
            watch.Stop();
            Console.WriteLine($"Run 4: {watch.ElapsedMilliseconds} ms");

            watch = Stopwatch.StartNew();
            MySqlQueryParser.ParseToAst("SELECT COUNT(tt.col1) AS Num, TEST('string',12)");
            watch.Stop();
            Console.WriteLine($"Run 5: {watch.ElapsedMilliseconds} ms");

            watch = Stopwatch.StartNew();
            MySqlQueryParser.ParseToAst("SELECT RandomFunc(), SuM(col1), CoUNt(col2), coUNT(*), avg (col3)");
            watch.Stop();
            Console.WriteLine($"Run 6: {watch.ElapsedMilliseconds} ms");

            watch = Stopwatch.StartNew();
            for (var i = 0; i < 100000; i++)
            {
                MySqlQueryParser.ParseToAst($"SELECT `{PrismaDB.Commons.Helper.GetRandomString(12)}`, '{PrismaDB.Commons.Helper.GetRandomString(12)}', {PrismaDB.Commons.Helper.GetRandomString(12)} FROM {PrismaDB.Commons.Helper.GetRandomString(12)} WHERE `{PrismaDB.Commons.Helper.GetRandomString(12)}` = \"{PrismaDB.Commons.Helper.GetRandomString(12)}\";");
            }
            watch.Stop();
            Console.WriteLine($"100000 runs: {watch.ElapsedMilliseconds} ms");

        }
    }
}
