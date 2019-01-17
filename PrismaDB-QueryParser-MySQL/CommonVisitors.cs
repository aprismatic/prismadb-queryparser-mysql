using Antlr4.Runtime.Misc;
using PrismaDB.QueryAST;
using PrismaDB.QueryAST.DML;
using System;
using System.Collections.Generic;

namespace PrismaDB.QueryParser.MySQL
{
    public partial class MySqlVisitor : AntlrMySqlParserBaseVisitor<object>
    {
        public override object VisitFullId([NotNull] AntlrMySqlParser.FullIdContext context)
        {
            var res = new List<Identifier>();
            foreach (var uid in context.uid())
                res.Add((Identifier)Visit(uid));
            return res;
        }

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

        public override object VisitFullColumnName([NotNull] AntlrMySqlParser.FullColumnNameContext context)
        {
            switch (context.dottedId().Length)
            {
                case 0:
                    return new ColumnRef((Identifier)Visit(context.uid()));
                case 1:
                    return new ColumnRef(((Identifier)Visit(context.uid())).id, (Identifier)Visit(context.dottedId()[0]));
                default:
                    return null;
            }
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
                    return new BooleanLessThan((Expression)Visit(context.left), (Expression)Visit(context.right));
                case ">=":
                    {
                        var exprLeft = new BooleanGreaterThan((Expression)Visit(context.left), (Expression)Visit(context.right));
                        var exprRight = new BooleanEquals((Expression)Visit(context.left), (Expression)Visit(context.right));
                        return new OrClause(exprLeft, exprRight);
                    }
                case "<=":
                    {
                        var exprLeft = new BooleanLessThan((Expression)Visit(context.left), (Expression)Visit(context.right));
                        var exprRight = new BooleanEquals((Expression)Visit(context.left), (Expression)Visit(context.right));
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

        public override object VisitSimpleFunctionCall([NotNull] AntlrMySqlParser.SimpleFunctionCallContext context)
        {
            return new ScalarFunction(context.GetText());
        }

        public override object VisitScalarFunctionCall([NotNull] AntlrMySqlParser.ScalarFunctionCallContext context)
        {
            return new ScalarFunction(context.scalarFunctionName().GetText(), (List<Expression>)Visit(context.functionArgs()));
        }

        public override object VisitUdfFunctionCall([NotNull] AntlrMySqlParser.UdfFunctionCallContext context)
        {
            var ids = (List<Identifier>)Visit(context.fullId());
            if (ids.Count == 1)
                return new ScalarFunction(ids[0], (List<Expression>)Visit(context.functionArgs()));
            return null;
        }

        public override object VisitFunctionArgs([NotNull] AntlrMySqlParser.FunctionArgsContext context)
        {
            var res = new List<Expression>();
            foreach (var arg in context.functionArg())
                res.Add((Expression)Visit(arg));
            return res;
        }

        public override object VisitMysqlVariable([NotNull] AntlrMySqlParser.MysqlVariableContext context)
        {
            var str = context.GLOBAL_ID().GetText().TrimStart('@');
            if (str.StartsWith("`"))
                str.Trim('`');
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
    }
}