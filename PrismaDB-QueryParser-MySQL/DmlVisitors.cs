using Antlr4.Runtime.Misc;
using PrismaDB.QueryAST;
using PrismaDB.QueryAST.DML;
using System.Collections.Generic;

namespace PrismaDB.QueryParser.MySQL
{
    public partial class MySqlVisitor : AntlrMySqlParserBaseVisitor<object>
    {
        public override object VisitSimpleSelect([NotNull] AntlrMySqlParser.SimpleSelectContext context)
        {
            var sq = (SelectQuery)Visit(context.querySpecification());
            return sq;
        }

        public override object VisitQuerySpecification([NotNull] AntlrMySqlParser.QuerySpecificationContext context)
        {
            var sq = new SelectQuery();
            sq.SelectExpressions = (List<Expression>)Visit(context.selectElements());
            if (context.limitClause() != null)
                sq.Limit = (uint?)Visit(context.limitClause());
            return sq;
        }

        public override object VisitSelectElements([NotNull] AntlrMySqlParser.SelectElementsContext context)
        {
            var res = new List<Expression>();

            if (context.star != null)
                res.Add(new AllColumns());

            foreach (var element in context.selectElement())
                res.Add((Expression)Visit(element));

            return res;
        }

        public override object VisitLimitClause([NotNull] AntlrMySqlParser.LimitClauseContext context)
        {
            return ((IntConstant)Visit(context.intLiteral())).intvalue;
        }

        public override object VisitSelectStarElement([NotNull] AntlrMySqlParser.SelectStarElementContext context)
        {
            var ids = (List<Identifier>)Visit(context.fullId());
            if (ids.Count == 1)
                return new AllColumns(ids[0].id);
            return null;
        }

        public override object VisitSelectColumnElement([NotNull] AntlrMySqlParser.SelectColumnElementContext context)
        {
            var res = (ColumnRef)Visit(context.fullColumnName());
            if (context.AS() != null)
                res.Alias = (Identifier)Visit(context.uid());
            else
                res.Alias = new Identifier(context.fullColumnName().GetText());
            return res;
        }

        public override object VisitSelectFunctionElement([NotNull] AntlrMySqlParser.SelectFunctionElementContext context)
        {
            var res = (ScalarFunction)Visit(context.functionCall());
            if (context.AS() != null)
                res.Alias = (Identifier)Visit(context.uid());
            else
                res.Alias = new Identifier(context.functionCall().GetText());
            return res;
        }

        public override object VisitSelectExpressionElement([NotNull] AntlrMySqlParser.SelectExpressionElementContext context)
        {
            var res = (Expression)Visit(context.expression());
            if (context.AS() != null)
                res.Alias = (Identifier)Visit(context.uid());
            else
                res.Alias = new Identifier(context.expression().GetText());
            return res;
        }
    }
}
