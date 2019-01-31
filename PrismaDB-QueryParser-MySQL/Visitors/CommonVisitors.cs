using Antlr4.Runtime.Misc;
using PrismaDB.QueryAST;
using PrismaDB.QueryAST.DDL;
using PrismaDB.QueryAST.DML;
using PrismaDB.QueryParser.MySQL.AntlrGrammer;
using System;
using System.Collections.Generic;

namespace PrismaDB.QueryParser.MySQL
{
    public partial class MySqlVisitor : MySqlParserBaseVisitor<object>
    {
        #region DB Objects
        public override object VisitDatabaseName([NotNull] MySqlParser.DatabaseNameContext context)
        {
            return new DatabaseRef(((Identifier)Visit(context.uid())).id);
        }

        public override object VisitTableName([NotNull] MySqlParser.TableNameContext context)
        {
            return new TableRef(((Identifier)Visit(context.uid())).id);
        }

        public override object VisitFullColumnName([NotNull] MySqlParser.FullColumnNameContext context)
        {
            if (context.dottedId() == null)
                return new ColumnRef((Identifier)Visit(context.uid()));
            else
                return new ColumnRef(((Identifier)Visit(context.uid())).id, (Identifier)Visit(context.dottedId()));
        }

        public override object VisitMysqlVariable([NotNull] MySqlParser.MysqlVariableContext context)
        {
            var str = context.GLOBAL_ID().GetText().TrimStart('@');
            if (str.StartsWith("`"))
                str = str.Trim('`');
            return new MySqlVariable(str);
        }

        public override object VisitUid([NotNull] MySqlParser.UidContext context)
        {
            if (context.simpleId() != null)
                return Visit(context.simpleId());
            if (context.REVERSE_QUOTE_ID() != null)
                return new Identifier(context.REVERSE_QUOTE_ID().GetText().Trim('`'));
            return null;
        }

        public override object VisitSimpleId([NotNull] MySqlParser.SimpleIdContext context)
        {
            if (context.ID() != null)
                return new Identifier(context.ID().GetText());
            return null;
        }

        public override object VisitDottedId([NotNull] MySqlParser.DottedIdContext context)
        {
            if (context.uid() != null)
                return Visit(context.uid());
            if (context.DOT_ID() != null)
                return new Identifier(context.DOT_ID().GetText().TrimStart('.'));
            return null;
        }
        #endregion


        #region Literals
        public override object VisitIntLiteral([NotNull] MySqlParser.IntLiteralContext context)
        {
            return new IntConstant(Int64.Parse(context.INT_LITERAL().GetText()));
        }

        public override object VisitDecimalLiteral([NotNull] MySqlParser.DecimalLiteralContext context)
        {
            return new FloatingPointConstant(Decimal.Parse(context.DECIMAL_LITERAL().GetText()));
        }

        public override object VisitStringLiteral([NotNull] MySqlParser.StringLiteralContext context)
        {
            var str = context.STRING_LITERAL().GetText();
            if (str.StartsWith("'"))
            {
                str = str.Substring(1, str.Length - 2).Replace("\\'", "'").Replace("''", "'");
                return new StringConstant(str);
            }
            if (str.StartsWith("\""))
            {
                str = str.Substring(1, str.Length - 2).Replace("\\\"", "\"").Replace("\"\"", "\"");
                return new StringConstant(str);
            }
            return null;
        }

        public override object VisitHexadecimalLiteral([NotNull] MySqlParser.HexadecimalLiteralContext context)
        {
            var str = context.HEXADECIMAL_LITERAL().GetText().ToUpperInvariant();
            var length = 0;
            if (str.StartsWith("0X"))
                length = str.Length - 2;
            else
                length = str.Length - 3;
            var bytes = new byte[length / 2];
            for (var i = 0; i < bytes.Length; i++)
                bytes[i] = Convert.ToByte(str.Substring((i * 2) + 2, 2), 16);
            return new BinaryConstant(bytes);
        }

        public override object VisitNullNotnull([NotNull] MySqlParser.NullNotnullContext context)
        {
            if (context.NOT() == null)
                return true;
            return false;
        }

        public override object VisitConstant([NotNull] MySqlParser.ConstantContext context)
        {
            if (context.nullLiteral != null)
                return new NullConstant();
            else
                return base.VisitConstant(context);
        }
        #endregion


        #region Data Types
        public override object VisitStringDataType([NotNull] MySqlParser.StringDataTypeContext context)
        {
            var res = new ColumnDefinition();
            switch (context.typeName.Text.ToUpperInvariant())
            {
                case "CHAR":
                    res.DataType = SqlDataType.MySQL_CHAR;
                    break;
                case "VARCHAR":
                    res.DataType = SqlDataType.MySQL_VARCHAR;
                    break;
                case "TEXT":
                    res.DataType = SqlDataType.MySQL_TEXT;
                    break;
            }
            if (context.lengthOneDimension() != null)
                res.Length = (int?)Visit(context.lengthOneDimension());
            return res;
        }

        public override object VisitSimpleDataType([NotNull] MySqlParser.SimpleDataTypeContext context)
        {
            var res = new ColumnDefinition();
            switch (context.typeName.Text.ToUpperInvariant())
            {
                case "TINYINT":
                    res.DataType = SqlDataType.MySQL_TINYINT;
                    break;
                case "SMALLINT":
                    res.DataType = SqlDataType.MySQL_SMALLINT;
                    break;
                case "INT":
                    res.DataType = SqlDataType.MySQL_INT;
                    break;
                case "BIGINT":
                    res.DataType = SqlDataType.MySQL_BIGINT;
                    break;
                case "DOUBLE":
                    res.DataType = SqlDataType.MySQL_DOUBLE;
                    break;
                case "DATE":
                    res.DataType = SqlDataType.MySQL_DATE;
                    break;
                case "TIMESTAMP":
                    res.DataType = SqlDataType.MySQL_TIMESTAMP;
                    break;
                case "DATETIME":
                    res.DataType = SqlDataType.MySQL_DATETIME;
                    break;
                case "BLOB":
                    res.DataType = SqlDataType.MySQL_BLOB;
                    break;
            }
            return res;
        }

        public override object VisitDimensionDataType([NotNull] MySqlParser.DimensionDataTypeContext context)
        {
            var res = new ColumnDefinition();
            switch (context.typeName.Text.ToUpperInvariant())
            {
                case "BINARY":
                    res.DataType = SqlDataType.MySQL_BINARY;
                    break;
                case "VARBINARY":
                    res.DataType = SqlDataType.MySQL_VARBINARY;
                    break;
            }
            if (context.lengthOneDimension() != null)
                res.Length = (int?)Visit(context.lengthOneDimension());
            return res;
        }

        public override object VisitCollectionDataType([NotNull] MySqlParser.CollectionDataTypeContext context)
        {
            var res = new ColumnDefinition();
            switch (context.typeName.Text.ToUpperInvariant())
            {
                case "ENUM":
                    res.DataType = SqlDataType.MySQL_ENUM;
                    break;
            }
            foreach (var str in context.stringLiteral())
                res.EnumValues.Add((StringConstant)Visit(str));
            return res;
        }

        public override object VisitLengthOneDimension([NotNull] MySqlParser.LengthOneDimensionContext context)
        {
            return (int?)((IntConstant)Visit(context.intLiteral())).intvalue;
        }
        #endregion


        #region Common Lists
        public override object VisitUidList([NotNull] MySqlParser.UidListContext context)
        {
            var res = new List<Identifier>();
            foreach (var uid in context.uid())
                res.Add((Identifier)Visit(uid));
            return res;
        }

        public override object VisitTables([NotNull] MySqlParser.TablesContext context)
        {
            var res = new List<TableRef>();
            foreach (var tableName in context.tableName())
                res.Add((TableRef)Visit(tableName));
            return res;
        }

        public override object VisitExpressions([NotNull] MySqlParser.ExpressionsContext context)
        {
            var res = new List<Expression>();
            foreach (var exp in context.expression())
                res.Add((Expression)Visit(exp));
            return res;
        }

        public override object VisitConstants([NotNull] MySqlParser.ConstantsContext context)
        {
            var res = new List<Constant>();
            foreach (var constant in context.constant())
                res.Add((Constant)Visit(constant));
            return res;
        }
        #endregion


        #region Common Expressions
        public override object VisitCurrentTimestamp([NotNull] MySqlParser.CurrentTimestampContext context)
        {
            return new ScalarFunction(context.GetText());
        }
        #endregion


        #region Functions
        public override object VisitFunctionCallExpressionAtom([NotNull] MySqlParser.FunctionCallExpressionAtomContext context)
        {
            return Visit(context.functionCall());
        }

        public override object VisitScalarFunctionCall([NotNull] MySqlParser.ScalarFunctionCallContext context)
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

        public override object VisitUdfFunctionCall([NotNull] MySqlParser.UdfFunctionCallContext context)
        {
            var res = new ScalarFunction((Identifier)Visit(context.uid()));
            if (context.functionArgs() != null)
                res.Parameters = (List<Expression>)Visit(context.functionArgs());
            return res;
        }

        public override object VisitSimpleFunctionCall([NotNull] MySqlParser.SimpleFunctionCallContext context)
        {
            return new ScalarFunction(context.GetText());
        }

        public override object VisitFunctionArgs([NotNull] MySqlParser.FunctionArgsContext context)
        {
            var res = new List<Expression>();
            foreach (var arg in context.functionArg())
                if (arg.star == null)
                    res.Add((Expression)Visit(arg));
            return res;
        }
        #endregion


        #region Expressions, predicates
        public override object VisitNotExpression([NotNull] MySqlParser.NotExpressionContext context)
        {
            var exp = Visit(context.expression());
            ((BooleanExpression)exp).NOT = !((BooleanExpression)exp).NOT;
            return exp;
        }

        public override object VisitLogicalExpression([NotNull] MySqlParser.LogicalExpressionContext context)
        {
            switch (context.logicalOperator().GetText().ToUpperInvariant())
            {
                case "AND":
                    return new AndClause((Expression)Visit(context.expression()[0]), (Expression)Visit(context.expression()[1]));
                case "OR":
                    return new OrClause((Expression)Visit(context.expression()[0]), (Expression)Visit(context.expression()[1]));
                default:
                    return null;
            }
        }

        public override object VisitNestedExpression([NotNull] MySqlParser.NestedExpressionContext context)
        {
            return Visit(context.expression());
        }

        public override object VisitInPredicate([NotNull] MySqlParser.InPredicateContext context)
        {
            var res = new BooleanIn();
            res.Column = (ColumnRef)Visit(context.predicate());
            foreach (var exp in (List<Expression>)Visit(context.expressions()))
                res.InValues.Add((Constant)exp);
            if (context.NOT() != null)
                res.NOT = true;
            return res;
        }

        public override object VisitIsNullPredicate([NotNull] MySqlParser.IsNullPredicateContext context)
        {
            return new BooleanIsNull((ColumnRef)Visit(context.predicate()), !(bool)Visit(context.nullNotnull()));
        }

        public override object VisitBinaryComparasionPredicate([NotNull] MySqlParser.BinaryComparasionPredicateContext context)
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

        public override object VisitLikePredicate([NotNull] MySqlParser.LikePredicateContext context)
        {
            var res = new BooleanLike();
            res.Column = (ColumnRef)Visit(context.predicate()[0]);
            res.SearchValue = (StringConstant)Visit(context.predicate()[1]);
            if (context.NOT() != null)
                res.NOT = true;
            return res;
        }

        public override object VisitNestedPredicate([NotNull] MySqlParser.NestedPredicateContext context)
        {
            return Visit(context.predicate());
        }

        public override object VisitUnaryExpressionAtom([NotNull] MySqlParser.UnaryExpressionAtomContext context)
        {
            var exp = Visit(context.expressionAtom());
            ((BooleanExpression)exp).NOT = !((BooleanExpression)exp).NOT;
            return exp;
        }

        public override object VisitMathExpressionAtom([NotNull] MySqlParser.MathExpressionAtomContext context)
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

        public override object VisitNestedExpressionAtom([NotNull] MySqlParser.NestedExpressionAtomContext context)
        {
            return Visit(context.expressionAtom());
        }
        #endregion


        #region Encryption
        public override object VisitEncryptionOptions([NotNull] MySqlParser.EncryptionOptionsContext context)
        {
            var res = ColumnEncryptionFlags.None;
            foreach (var encType in context.encryptionType())
                res |= (ColumnEncryptionFlags)Visit(encType);
            return res;
        }

        public override object VisitEncryptionType([NotNull] MySqlParser.EncryptionTypeContext context)
        {
            switch (context.GetText().ToUpperInvariant())
            {
                case "ADDITION":
                    return ColumnEncryptionFlags.Addition;
                case "SEARCH":
                    return ColumnEncryptionFlags.Search;
                case "STORE":
                    return ColumnEncryptionFlags.Store;
                case "MULTIPLICATION":
                    return ColumnEncryptionFlags.Multiplication;
                case "RANGE":
                    return ColumnEncryptionFlags.Range;
                case "WILDCARD":
                    return ColumnEncryptionFlags.Wildcard;
            }
            return null;
        }
        #endregion
    }
}