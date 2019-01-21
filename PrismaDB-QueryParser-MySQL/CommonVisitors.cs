using Antlr4.Runtime.Misc;
using PrismaDB.QueryAST;
using PrismaDB.QueryAST.DML;
using System;
using System.Collections.Generic;

namespace PrismaDB.QueryParser.MySQL
{
    public partial class MySqlVisitor : AntlrMySqlParserBaseVisitor<object>
    {
        public override object VisitUid([NotNull] AntlrMySqlParser.UidContext context)
        {
            if (context.simpleId() != null)
                return Visit(context.simpleId());
            if (context.REVERSE_QUOTE_ID() != null)
                return new Identifier(context.REVERSE_QUOTE_ID().GetText().Trim('`'));
            return null;
        }

        public override object VisitUidList([NotNull] AntlrMySqlParser.UidListContext context)
        {
            var res = new List<Identifier>();
            foreach (var uid in context.uid())
                res.Add((Identifier)Visit(uid));
            return res;
        }

        public override object VisitSimpleId([NotNull] AntlrMySqlParser.SimpleIdContext context)
        {
            if (context.ID() != null)
                return new Identifier(context.ID().GetText());
            return null;
        }

        public override object VisitDottedId([NotNull] AntlrMySqlParser.DottedIdContext context)
        {
            if (context.uid() != null)
                return Visit(context.uid());
            if (context.DOT_ID() != null)
                return new Identifier(context.DOT_ID().GetText().TrimStart('.'));
            return null;
        }

        public override object VisitConstants([NotNull] AntlrMySqlParser.ConstantsContext context)
        {
            var res = new List<Constant>();
            foreach (var constant in context.constant())
                res.Add((Constant)Visit(constant));
            return res;
        }

        public override object VisitConstant([NotNull] AntlrMySqlParser.ConstantContext context)
        {
            if (context.nullLiteral != null)
                return new NullConstant();
            else
                return base.VisitConstant(context);
        }

        public override object VisitIntLiteral([NotNull] AntlrMySqlParser.IntLiteralContext context)
        {
            return new IntConstant(Int64.Parse(context.INT_LITERAL().GetText()));
        }

        public override object VisitDecimalLiteral([NotNull] AntlrMySqlParser.DecimalLiteralContext context)
        {
            return new FloatingPointConstant(Decimal.Parse(context.DECIMAL_LITERAL().GetText()));
        }

        public override object VisitStringLiteral([NotNull] AntlrMySqlParser.StringLiteralContext context)
        {
            var str = context.STRING_LITERAL().GetText();
            if (str.StartsWith("\'"))
                return new StringConstant(str.Trim('\''));
            if (str.StartsWith("\""))
                return new StringConstant(str.Trim('\"'));
            return null;
        }

        public override object VisitHexadecimalLiteral([NotNull] AntlrMySqlParser.HexadecimalLiteralContext context)
        {
            throw new NotImplementedException();
        }

        public override object VisitNullNotnull([NotNull] AntlrMySqlParser.NullNotnullContext context)
        {
            if (context.NOT() != null)
                return false;
            return true;
        }

        public override object VisitTableName([NotNull] AntlrMySqlParser.TableNameContext context)
        {
            return new TableRef(((Identifier)Visit(context.uid())).id);
        }

        public override object VisitFullColumnName([NotNull] AntlrMySqlParser.FullColumnNameContext context)
        {
            if (context.dottedId() == null)
                return new ColumnRef((Identifier)Visit(context.uid()));
            else
                return new ColumnRef(((Identifier)Visit(context.uid())).id, (Identifier)Visit(context.dottedId()));
        }

        public override object VisitNotExpression([NotNull] AntlrMySqlParser.NotExpressionContext context)
        {
            var exp = Visit(context.expression());
            ((BooleanExpression)exp).NOT = !((BooleanExpression)exp).NOT;
            return exp;
        }

        public override object VisitLogicalExpression([NotNull] AntlrMySqlParser.LogicalExpressionContext context)
        {
            switch (context.logicalOperator().GetText())
            {
                case "AND":
                    return new AndClause((Expression)Visit(context.expression()[0]), (Expression)Visit(context.expression()[1]));
                case "OR":
                    return new OrClause((Expression)Visit(context.expression()[0]), (Expression)Visit(context.expression()[1]));
                default:
                    return null;
            }
        }

        public override object VisitExpressions([NotNull] AntlrMySqlParser.ExpressionsContext context)
        {
            var res = new List<Expression>();
            foreach (var exp in context.expression())
                res.Add((Expression)Visit(exp));
            return res;
        }

        public override object VisitInPredicate([NotNull] AntlrMySqlParser.InPredicateContext context)
        {
            var res = new BooleanIn();
            res.Column = (ColumnRef)Visit(context.predicate());
            foreach (var exp in (List<Expression>)Visit(context.expressions()))
                res.InValues.Add((Constant)exp);
            if (context.NOT() != null)
                res.NOT = true;
            return res;
        }

        public override object VisitIsNullPredicate([NotNull] AntlrMySqlParser.IsNullPredicateContext context)
        {
            return new BooleanIsNull((ColumnRef)Visit(context.predicate()), (bool)Visit(context.nullNotnull()));
        }

        public override object VisitBinaryComparasionPredicate([NotNull] AntlrMySqlParser.BinaryComparasionPredicateContext context)
        {
            switch (context.comparisonOperator().GetText())
            {
                case "=":
                    return new BooleanEquals((Expression)Visit(context.left), (Expression)Visit(context.right));
                case ">":
                    return new BooleanGreaterThan((Expression)Visit(context.left), (Expression)Visit(context.right));
                case "<":
                    return new BooleanGreaterThan((Expression)Visit(context.right), (Expression)Visit(context.left));
                case ">=":
                    {
                        var exprLeft = new BooleanGreaterThan((Expression)Visit(context.left), (Expression)Visit(context.right));
                        var exprRight = new BooleanEquals((Expression)Visit(context.left), (Expression)Visit(context.right));
                        return new OrClause(exprLeft, exprRight);
                    }
                case "<=":
                    {
                        var exprLeft = new BooleanGreaterThan((Expression)Visit(context.right), (Expression)Visit(context.left));
                        var exprRight = new BooleanEquals((Expression)Visit(context.right), (Expression)Visit(context.left));
                        return new OrClause(exprLeft, exprRight);
                    }
                case "<>":
                case "!=":
                    return new BooleanEquals((Expression)Visit(context.left), (Expression)Visit(context.right), true);
                default:
                    return null;
            }
        }

        public override object VisitLikePredicate([NotNull] AntlrMySqlParser.LikePredicateContext context)
        {
            var res = new BooleanLike();
            res.Column = (ColumnRef)Visit(context.predicate()[0]);
            res.SearchValue = (StringConstant)Visit(context.predicate()[1]);
            if (context.NOT() != null)
                res.NOT = true;
            return res;
        }

        public override object VisitFunctionCallExpressionAtom([NotNull] AntlrMySqlParser.FunctionCallExpressionAtomContext context)
        {
            return Visit(context.functionCall());
        }

        public override object VisitSimpleFunctionCall([NotNull] AntlrMySqlParser.SimpleFunctionCallContext context)
        {
            return new ScalarFunction(context.GetText());
        }

        public override object VisitScalarFunctionCall([NotNull] AntlrMySqlParser.ScalarFunctionCallContext context)
        {
            if (context.scalarFunctionName().SUM() != null)
                return new SumAggregationFunction(context.scalarFunctionName().GetText(), (List<Expression>)Visit(context.functionArgs()));
            else if (context.scalarFunctionName().COUNT() != null)
                return new CountAggregationFunction(context.scalarFunctionName().GetText(), (List<Expression>)Visit(context.functionArgs()));
            else if (context.scalarFunctionName().AVG() != null)
                return new AvgAggregationFunction(context.scalarFunctionName().GetText(), (List<Expression>)Visit(context.functionArgs()));
            else
                return new ScalarFunction(context.scalarFunctionName().GetText(), (List<Expression>)Visit(context.functionArgs()));
        }

        public override object VisitUdfFunctionCall([NotNull] AntlrMySqlParser.UdfFunctionCallContext context)
        {
            var res = new ScalarFunction((Identifier)Visit(context.uid()));
            if (context.functionArgs() != null)
                res.Parameters = (List<Expression>)Visit(context.functionArgs());
            return res;
        }

        public override object VisitFunctionArgs([NotNull] AntlrMySqlParser.FunctionArgsContext context)
        {
            var res = new List<Expression>();
            foreach (var arg in context.functionArg())
                if (arg.star == null)
                    res.Add((Expression)Visit(arg));
            return res;
        }

        public override object VisitMysqlVariable([NotNull] AntlrMySqlParser.MysqlVariableContext context)
        {
            var str = context.GLOBAL_ID().GetText().TrimStart('@');
            if (str.StartsWith("`"))
                str = str.Trim('`');
            return new MySqlVariable(str);
        }

        public override object VisitUnaryExpressionAtom([NotNull] AntlrMySqlParser.UnaryExpressionAtomContext context)
        {
            var exp = Visit(context.expressionAtom());
            ((BooleanExpression)exp).NOT = !((BooleanExpression)exp).NOT;
            return exp;
        }

        public override object VisitMathExpressionAtom([NotNull] AntlrMySqlParser.MathExpressionAtomContext context)
        {
            switch (context.mathOperator().GetText())
            {
                case "+":
                    return new Addition((Expression)Visit(context.left), (Expression)Visit(context.right));
                case "-":
                    return new Subtraction((Expression)Visit(context.left), (Expression)Visit(context.right));
                case "*":
                    return new Multiplication((Expression)Visit(context.left), (Expression)Visit(context.right));
                case "/":
                    return new Division((Expression)Visit(context.left), (Expression)Visit(context.right));
                default:
                    return null;
            }
        }

        public override object VisitNestedExpression([NotNull] AntlrMySqlParser.NestedExpressionContext context)
        {
            return Visit(context.expression());
        }

        public override object VisitNestedPredicate([NotNull] AntlrMySqlParser.NestedPredicateContext context)
        {
            return Visit(context.predicate());
        }

        public override object VisitNestedExpressionAtom([NotNull] AntlrMySqlParser.NestedExpressionAtomContext context)
        {
            return Visit(context.expressionAtom());
        }
    }
}