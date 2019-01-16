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
            var inputStream = new AntlrInputStream(input);
            var sqlLexer = new AntlrMySqlLexer(new CaseChangingCharStream(inputStream, true));
            var tokens = new CommonTokenStream(sqlLexer);
            var sqlParser = new AntlrMySqlParser(tokens);

            var visitor = new MySqlVisitor();
            var res = visitor.Visit(sqlParser.root()) as List<Query>;
            return res;
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
            var ctq = new CreateTableQuery();
            return ctq;
        }

        public override object VisitSimpleSelect([NotNull] AntlrMySqlParser.SimpleSelectContext context)
        {
            var sq = Visit(context.querySpecification()) as SelectQuery;
            return sq;
        }

        public override object VisitQuerySpecification([NotNull] AntlrMySqlParser.QuerySpecificationContext context)
        {
            var sq = new SelectQuery();
            sq.Limit = Visit(context.limitClause()) as uint?;
            return sq;
        }

        public override object VisitLimitClause([NotNull] AntlrMySqlParser.LimitClauseContext context)
        {
            if (context.OFFSET() != null || context.decimalLiteral().Length > 1)
                throw new ApplicationException("LIMIT clause currently does not support OFFSET.");

            var res = Visit(context.decimalLiteral()[0]) as IntConstant;
            return (uint?)res.intvalue;
        }

        public override object VisitDecimalLiteral([NotNull] AntlrMySqlParser.DecimalLiteralContext context)
        {
            return new IntConstant(Int64.Parse(context.GetText()));
        }
    }
}
