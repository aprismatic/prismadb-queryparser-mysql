﻿using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using PrismaDB.QueryAST;
using System;
using System.Collections.Generic;

namespace PrismaDB.QueryParser.MySQL
{
    public static class MySqlParser
    {
        public static List<Query> ParseToAst(String input)
        {
            var inputStream = new AntlrInputStream(input);
            var sqlLexer = new AntlrMySqlLexer(new CaseChangingCharStream(inputStream, true));
            var tokens = new CommonTokenStream(sqlLexer);
            var sqlParser = new AntlrMySqlParser(tokens);

            var visitor = new MySqlVisitor();
            var res = (List<Query>)visitor.Visit(sqlParser.root());
            return res;
        }
    }

    public partial class MySqlVisitor : AntlrMySqlParserBaseVisitor<object>
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
                queries.Add((Query)Visit(stmt));
            }
            return queries;
        }
    }
}
