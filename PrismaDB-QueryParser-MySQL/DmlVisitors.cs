using Antlr4.Runtime.Misc;
using PrismaDB.Commons;
using PrismaDB.QueryAST;
using PrismaDB.QueryAST.DML;
using System.Collections.Generic;

namespace PrismaDB.QueryParser.MySQL
{
    public partial class MySqlVisitor : AntlrMySqlParserBaseVisitor<object>
    {
        public override object VisitInsertStatement([NotNull] AntlrMySqlParser.InsertStatementContext context)
        {
            var res = new InsertQuery();
            res.Into = (TableRef)Visit(context.tableName());
            if (context.uidList() != null)
                foreach (var id in (List<Identifier>)Visit(context.uidList()))
                    res.Columns.Add(new ColumnRef(id));
            res.Values = (List<List<Expression>>)Visit(context.insertStatementValue());
            return res;
        }

        public override object VisitInsertStatementValue([NotNull] AntlrMySqlParser.InsertStatementValueContext context)
        {
            var res = new List<List<Expression>>();
            foreach (var exps in context.expressions())
                res.Add((List<Expression>)Visit(exps));
            return res;
        }

        public override object VisitSelectStatement([NotNull] AntlrMySqlParser.SelectStatementContext context)
        {
            var res = new SelectQuery();
            res.SelectExpressions = (List<Expression>)Visit(context.selectElements());
            if (context.fromClause() != null)
            {
                var from = (SelectQuery)Visit(context.fromClause());
                res.FromTables = from.FromTables;
                res.Joins = from.Joins;
                res.Where = from.Where;
                res.GroupBy = from.GroupBy;
            }
            if (context.orderByClause() != null)
                res.OrderBy = (OrderByClause)Visit(context.orderByClause());
            if (context.limitClause() != null)
                res.Limit = (uint?)Visit(context.limitClause());
            return res;
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

        public override object VisitSelectStarElement([NotNull] AntlrMySqlParser.SelectStarElementContext context)
        {
            return new AllColumns(((Identifier)Visit(context.uid())).id);
        }

        public override object VisitSelectColumnElement([NotNull] AntlrMySqlParser.SelectColumnElementContext context)
        {
            var res = (ColumnRef)Visit(context.fullColumnName());
            if (context.uid() != null)
                res.Alias = (Identifier)Visit(context.uid());
            return res;
        }

        public override object VisitSelectFunctionElement([NotNull] AntlrMySqlParser.SelectFunctionElementContext context)
        {
            var res = (ScalarFunction)Visit(context.functionCall());
            if (context.uid() != null)
                res.Alias = (Identifier)Visit(context.uid());
            else
                res.Alias = new Identifier(context.functionCall().GetText());
            return res;
        }

        public override object VisitSelectExpressionElement([NotNull] AntlrMySqlParser.SelectExpressionElementContext context)
        {
            var res = (Expression)Visit(context.expression());
            if (context.uid() != null)
                res.Alias = (Identifier)Visit(context.uid());
            else
                res.Alias = new Identifier(context.expression().GetText());
            return res;
        }

        public override object VisitFromClause([NotNull] AntlrMySqlParser.FromClauseContext context)
        {
            var res = new SelectQuery();
            res.FromTables = (List<TableRef>)Visit(context.tableSources());
            res.Joins = new List<JoinClause>();
            foreach (var joinPart in context.joinPart())
                res.Joins.Add((JoinClause)Visit(joinPart));
            if (context.expression() != null)
            {
                var expr = (Expression)Visit(context.expression());
                while (!CnfConverter.CheckCnf(expr)) expr = CnfConverter.ConvertToCnf(expr);
                res.Where.CNF = CnfConverter.BuildCnf(expr);
            }
            res.GroupBy.GroupColumns = new List<ColumnRef>();
            foreach (var groupByItem in context.groupByItem())
                res.GroupBy.GroupColumns.Add((ColumnRef)Visit(groupByItem));
            return res;
        }

        public override object VisitTableSources([NotNull] AntlrMySqlParser.TableSourcesContext context)
        {
            var res = new List<TableRef>();
            foreach (var tableSource in context.tableSourceItem())
                res.Add((TableRef)Visit(tableSource));
            return res;
        }

        public override object VisitTableSourceItem([NotNull] AntlrMySqlParser.TableSourceItemContext context)
        {
            var res = (TableRef)Visit(context.tableName());
            if (context.alias != null)
                res.Alias = (Identifier)Visit(context.uid());
            return res;
        }

        public override object VisitInnerJoin([NotNull] AntlrMySqlParser.InnerJoinContext context)
        {
            var res = new JoinClause();
            res.JoinType = JoinType.INNER;
            if (context.CROSS() != null)
                res.JoinType = JoinType.CROSS;
            res.JoinTable = (TableRef)Visit(context.tableSourceItem());
            if (context.ON() != null)
            {
                var exp = (BooleanEquals)Visit(context.expression());
                res.FirstColumn = (ColumnRef)exp.left;
                res.SecondColumn = (ColumnRef)exp.right;
            }
            return res;
        }

        public override object VisitOuterJoin([NotNull] AntlrMySqlParser.OuterJoinContext context)
        {
            var res = new JoinClause();
            if (context.LEFT() != null)
                res.JoinType = JoinType.LEFT_OUTER;
            else if (context.RIGHT() != null)
                res.JoinType = JoinType.RIGHT_OUTER;
            res.JoinTable = (TableRef)Visit(context.tableSourceItem());
            if (context.ON() != null)
            {
                var exp = (BooleanEquals)Visit(context.expression());
                res.FirstColumn = (ColumnRef)exp.left;
                res.SecondColumn = (ColumnRef)exp.right;
            }
            return res;
        }

        public override object VisitGroupByItem([NotNull] AntlrMySqlParser.GroupByItemContext context)
        {
            return Visit(context.expression());
        }

        public override object VisitOrderByClause([NotNull] AntlrMySqlParser.OrderByClauseContext context)
        {
            var res = new OrderByClause();
            foreach (var orderByExp in context.orderByExpression())
                res.OrderColumns.Add((Pair<ColumnRef, OrderDirection>)Visit(orderByExp));
            return res;
        }

        public override object VisitOrderByExpression([NotNull] AntlrMySqlParser.OrderByExpressionContext context)
        {
            var res = new Pair<ColumnRef, OrderDirection>();
            res.First = (ColumnRef)Visit(context.expression());
            res.Second = OrderDirection.ASC;
            if (context.DESC() != null)
                res.Second = OrderDirection.DESC;
            return res;
        }

        public override object VisitLimitClause([NotNull] AntlrMySqlParser.LimitClauseContext context)
        {
            return (uint?)((IntConstant)Visit(context.intLiteral())).intvalue;
        }
    }
}
