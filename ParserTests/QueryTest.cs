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
            var q1 = MySqlParser.ParseToAst("SELECT `abc` FROM `def`;");

            Assert.Single(q1);

            Assert.IsType<SelectQuery>(q1[0]);
        }
    }
}