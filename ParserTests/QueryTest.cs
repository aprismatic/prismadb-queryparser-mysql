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
            var q1 = MySqlParser.ParseToAst("select `lala`.abc, 'asd', 0, -1, 23.0, -0.01 from def;");
                 
            Assert.Single(q1);
            Assert.IsType<SelectQuery>(q1[0]);
            var selectQ = (SelectQuery)q1[0];
            Assert.Null(selectQ.Limit);
            Assert.Equal("abc", ((ColumnRef)selectQ.SelectExpressions[0]).ColumnName.id);
            Assert.Equal("lala", ((ColumnRef)selectQ.SelectExpressions[0]).Table.Table.id);
            Assert.Equal("`lala`.abc", ((ColumnRef)selectQ.SelectExpressions[0]).Alias.id);
            Assert.Equal("asd", ((StringConstant)selectQ.SelectExpressions[1]).strvalue);
            Assert.Equal(0, ((IntConstant)selectQ.SelectExpressions[2]).intvalue);
            Assert.Equal(-1, ((IntConstant)selectQ.SelectExpressions[3]).intvalue);
            Assert.Equal(23.0m, ((FloatingPointConstant)selectQ.SelectExpressions[4]).floatvalue);
            Assert.Equal(-0.01m, ((FloatingPointConstant)selectQ.SelectExpressions[5]).floatvalue);
        }

        [Fact(DisplayName = "SpeedTest")]
        public void SpeedTest()
        {
            for (var i = 0; i < 10000; i++)
            {
                
                var q1 = MySqlParser.ParseToAst($"select {PrismaDB.Commons.Helper.GetRandomString(12)} from {PrismaDB.Commons.Helper.GetRandomString(12)} LIMIT 1;");

                Assert.Single(q1);

                Assert.IsType<SelectQuery>(q1[0]);
            }
        }
    }
}