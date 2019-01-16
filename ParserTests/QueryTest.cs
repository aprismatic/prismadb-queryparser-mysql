using System;
using PrismaDB.QueryAST;
using PrismaDB.QueryAST.DCL;
using PrismaDB.QueryAST.DDL;
using PrismaDB.QueryAST.DML;
using PrismaDB.QueryParser.MySQL;
using Xunit;

namespace ParserTests
{
    public class QueryTest
    {
        [Fact(DisplayName = "Test")]
        public void Test()
        {
            var q1 = MySqlParser.ParseToAst("select abc from def LIMIT 1;");
                 
            Assert.Single(q1);
            Assert.IsType<SelectQuery>(q1[0]);
            var selectQ = (SelectQuery)q1[0];
            Assert.NotNull(selectQ.Limit);
        }

        [Fact(DisplayName = "SpeedTest")]
        public void SpeedTest()
        {
            for (var i = 0; i < 10000; i++)
            {
                
                var q1 = MySqlParser.ParseToAst($"select {PrismaDB.Commons.Helper.GetRandomString(12)} from {PrismaDB.Commons.Helper.GetRandomString(12)};");

                Assert.Single(q1);

                Assert.IsType<SelectQuery>(q1[0]);
            }
        }
    }
}