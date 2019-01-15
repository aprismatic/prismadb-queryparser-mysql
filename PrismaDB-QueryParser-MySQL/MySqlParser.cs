using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using PrismaDB.QueryAST;
using PrismaDB.QueryAST.DDL;
using PrismaDB.QueryAST.DML;
using System;
using System.Collections.Generic;

namespace PrismaDB.QueryParser.MySQL
{
    public static class MySqlParser
    {
        public static List<Query> ParseToAst(String input)
        {
            try
            {
                var inputStream = new AntlrInputStream(input);
                var sqlLexer = new AntlrMySqlLexer(new CaseChangingCharStream(inputStream, true));
                var tokens = new CommonTokenStream(sqlLexer);
                var sqlParser = new AntlrMySqlParser(tokens);

                var visitor = new MySqlVisitor();
                var res = visitor.Visit(sqlParser.root()) as List<Query>;
                return res;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex);
                return null;
            }

        }
    }

    public class MySqlVisitor : AntlrMySqlParserBaseVisitor<object>
    {
        public override object VisitRoot([NotNull] AntlrMySqlParser.RootContext context)
        {
            return Visit(context.sqlStatements());
        }

        public override object VisitSqlStatements([NotNull] AntlrMySqlParser.SqlStatementsContext context)
        {
            var queries = new List<Query>();
            foreach (var stmt in context.sqlStatement())
            {
                queries.Add(Visit(stmt) as Query);
            }
            return queries;
        }

        public override object VisitCreateTable([NotNull] AntlrMySqlParser.CreateTableContext context)
        {
            return new CreateTableQuery();
        }

        public override object VisitSimpleSelect([NotNull] AntlrMySqlParser.SimpleSelectContext context)
        {
            return new SelectQuery();
        }
    }
}
