﻿using System;
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
        [Fact(DisplayName = "Parse ALTER TABLE")]
        public void Parse_AlterTable()
        {
            // Setup
            var parser = new MySqlParser();
            var test = "ALTER TABLE table1 " +
                       "MODIFY COLUMN col1 TEXT ENCRYPTED FOR (STORE, SEARCH) NULL";

            // Act
            var result = parser.ParseToAst(test);

            // Assert
            var actual = (AlterTableQuery) result[0];

            Assert.Equal(new TableRef("table1"), actual.TableName);
            Assert.Equal(AlterType.MODIFY, actual.AlterType);

            Assert.Equal(new Identifier("col1"), actual.AlteredColumns[0].ColumnDefinition.ColumnName);
            Assert.Equal(SqlDataType.TEXT, actual.AlteredColumns[0].ColumnDefinition.DataType);
            Assert.NotEqual(ColumnEncryptionFlags.None,
                ColumnEncryptionFlags.Store & actual.AlteredColumns[0].ColumnDefinition.EncryptionFlags);
            Assert.NotEqual(ColumnEncryptionFlags.None,
                ColumnEncryptionFlags.Search & actual.AlteredColumns[0].ColumnDefinition.EncryptionFlags);
            Assert.Equal(ColumnEncryptionFlags.None,
                ColumnEncryptionFlags.IntegerAddition & actual.AlteredColumns[0].ColumnDefinition.EncryptionFlags);
            Assert.Equal(ColumnEncryptionFlags.None,
                ColumnEncryptionFlags.IntegerMultiplication &
                actual.AlteredColumns[0].ColumnDefinition.EncryptionFlags);
            Assert.True(actual.AlteredColumns[0].ColumnDefinition.Nullable);
            Assert.Null(actual.AlteredColumns[0].ColumnDefinition.Length);
        }

        [Fact(DisplayName = "Parse CREATE TABLE w\\DATETIME")]
        public void Parse_CreateTable_DATETIME()
        {
            // Setup
            var parser = new MySqlParser();
            var test = "CREATE TABLE table1 " +
                       "(col1 DATETIME NOT NULL)";

            // Act
            var result = parser.ParseToAst(test);

            // Assert
            var actual = (CreateTableQuery) result[0];

            Assert.Equal(new TableRef("table1"), actual.TableName);

            Assert.Equal(new Identifier("col1"), actual.ColumnDefinitions[0].ColumnName);
            Assert.Equal(SqlDataType.DATETIME, actual.ColumnDefinitions[0].DataType);
            Assert.Equal(ColumnEncryptionFlags.None, actual.ColumnDefinitions[0].EncryptionFlags);
            Assert.False(actual.ColumnDefinitions[0].Nullable);
            Assert.Null(actual.ColumnDefinitions[0].Length);
        }

        [Fact(DisplayName = "Parse NULLs")]
        public void Parse_NULLExpressions()
        {
            // Setup
            var parser = new MySqlParser();
            var test1 = "SELECT NULL";
            var test2 = "INSERT INTO tbl1 ( col1 ) VALUES ( NULL )";

            // Act
            var result1 = parser.ParseToAst(test1)[0];

            // Assert
            Assert.IsType<SelectQuery>(result1);
            Assert.IsType<NullConstant>(((SelectQuery)result1).SelectExpressions[0]);

            // Act
            var result2 = parser.ParseToAst(test2)[0];

            // Assert
            Assert.IsType<InsertQuery>(result2);
            Assert.IsType<NullConstant>(((InsertQuery)result2).Values[0][0]);
        }

        [Fact(DisplayName = "Parse CREATE TABLE w\\TEXT")]
        public void Parse_CreateTable_TEXT()
        {
            // Setup
            var parser = new MySqlParser();
            var test = "CREATE TABLE table1 " +
                       "(col1 TEXT, " +
                       "col2 TEXT ENCRYPTED FOR (STORE, SEARCH) NULL)";

            // Act
            var result = parser.ParseToAst(test);

            // Assert
            var actual = (CreateTableQuery) result[0];

            Assert.Equal(new TableRef("table1"), actual.TableName);

            Assert.Equal(new Identifier("col1"), actual.ColumnDefinitions[0].ColumnName);
            Assert.Equal(SqlDataType.TEXT, actual.ColumnDefinitions[0].DataType);
            Assert.Equal(ColumnEncryptionFlags.None, actual.ColumnDefinitions[0].EncryptionFlags);
            Assert.True(actual.ColumnDefinitions[0].Nullable);
            Assert.Null(actual.ColumnDefinitions[0].Length);

            Assert.Equal(new Identifier("col2"), actual.ColumnDefinitions[1].ColumnName);
            Assert.Equal(SqlDataType.TEXT, actual.ColumnDefinitions[1].DataType);
            Assert.NotEqual(ColumnEncryptionFlags.None,
                ColumnEncryptionFlags.Store & actual.ColumnDefinitions[1].EncryptionFlags);
            Assert.NotEqual(ColumnEncryptionFlags.None,
                ColumnEncryptionFlags.Search & actual.ColumnDefinitions[1].EncryptionFlags);
            Assert.Equal(ColumnEncryptionFlags.None,
                ColumnEncryptionFlags.IntegerAddition & actual.ColumnDefinitions[1].EncryptionFlags);
            Assert.Equal(ColumnEncryptionFlags.None,
                ColumnEncryptionFlags.IntegerMultiplication & actual.ColumnDefinitions[1].EncryptionFlags);
            Assert.True(actual.ColumnDefinitions[1].Nullable);
            Assert.Null(actual.ColumnDefinitions[1].Length);
        }

        [Fact(DisplayName = "Parse CREATE TABLE w\\partial encryption")]
        public void Parse_CreateTable_WithPartialEncryption()
        {
            // Setup
            var parser = new MySqlParser();
            var test = "CREATE TABLE ttt " +
                       "(aaa INT ENCRYPTED FOR (INTEGER_ADDITION, INTEGER_MULTIPLICATION) NOT NULL, " +
                       "`bbb` INT NULL, " +
                       "ccc VARCHAR(80) NOT NULL, " +
                       "ddd VARCHAR(20) ENCRYPTED FOR (STORE, SEARCH), " +
                       "eee TEXT NULL, " +
                       "fff TEXT ENCRYPTED NULL, " +
                       "ggg DOUBLE, " +
                       "hhh ENUM('ABC', 'def') ENCRYPTED NULL, " +
                       "iii TIMESTAMP ENCRYPTED DEFAULT CURRENT_TIMESTAMP" + ")";
            // Act
            var result = parser.ParseToAst(test);

            // Assert
            var actual = (CreateTableQuery) result[0];

            Assert.Equal(new TableRef("ttt"), actual.TableName);
            Assert.Equal(new Identifier("aaa"), actual.ColumnDefinitions[0].ColumnName);
            Assert.Equal(SqlDataType.INT, actual.ColumnDefinitions[0].DataType);
            Assert.Equal(ColumnEncryptionFlags.IntegerAddition | ColumnEncryptionFlags.IntegerMultiplication,
                actual.ColumnDefinitions[0].EncryptionFlags);
            Assert.False(actual.ColumnDefinitions[0].Nullable);
            Assert.Equal(new Identifier("bbb"), actual.ColumnDefinitions[1].ColumnName);
            Assert.Equal(SqlDataType.INT, actual.ColumnDefinitions[1].DataType);
            Assert.Equal(ColumnEncryptionFlags.None, actual.ColumnDefinitions[1].EncryptionFlags);
            Assert.True(actual.ColumnDefinitions[1].Nullable);
            Assert.Equal(new Identifier("ccc"), actual.ColumnDefinitions[2].ColumnName);
            Assert.Equal(SqlDataType.VARCHAR, actual.ColumnDefinitions[2].DataType);
            Assert.Equal(80, actual.ColumnDefinitions[2].Length);
            Assert.Equal(ColumnEncryptionFlags.None, actual.ColumnDefinitions[2].EncryptionFlags);
            Assert.False(actual.ColumnDefinitions[2].Nullable);
            Assert.Equal(new Identifier("ddd"), actual.ColumnDefinitions[3].ColumnName);
            Assert.Equal(SqlDataType.VARCHAR, actual.ColumnDefinitions[3].DataType);
            Assert.Equal(20, actual.ColumnDefinitions[3].Length);
            Assert.Equal(ColumnEncryptionFlags.Store | ColumnEncryptionFlags.Search,
                actual.ColumnDefinitions[3].EncryptionFlags);
            Assert.True(actual.ColumnDefinitions[3].Nullable);
            Assert.Equal(new Identifier("eee"), actual.ColumnDefinitions[4].ColumnName);
            Assert.Equal(SqlDataType.TEXT, actual.ColumnDefinitions[4].DataType);
            Assert.True(actual.ColumnDefinitions[4].Nullable);
            Assert.Equal(new Identifier("fff"), actual.ColumnDefinitions[5].ColumnName);
            Assert.Equal(SqlDataType.TEXT, actual.ColumnDefinitions[5].DataType);
            Assert.Equal(ColumnEncryptionFlags.Store, actual.ColumnDefinitions[5].EncryptionFlags);
            Assert.True(actual.ColumnDefinitions[5].Nullable);
            Assert.Equal(new Identifier("ggg"), actual.ColumnDefinitions[6].ColumnName);
            Assert.Equal(SqlDataType.DOUBLE, actual.ColumnDefinitions[6].DataType);
            Assert.Equal(ColumnEncryptionFlags.None, actual.ColumnDefinitions[6].EncryptionFlags);
            Assert.True(actual.ColumnDefinitions[6].Nullable);
            Assert.Equal(new Identifier("hhh"), actual.ColumnDefinitions[7].ColumnName);
            Assert.Equal(SqlDataType.ENUM, actual.ColumnDefinitions[7].DataType);
            Assert.Equal(ColumnEncryptionFlags.Store, actual.ColumnDefinitions[7].EncryptionFlags);
            Assert.True(actual.ColumnDefinitions[7].Nullable);
            Assert.Null(actual.ColumnDefinitions[7].DefaultValue);
            Assert.Equal(new Identifier("iii"), actual.ColumnDefinitions[8].ColumnName);
            Assert.Equal(SqlDataType.TIMESTAMP, actual.ColumnDefinitions[8].DataType);
            Assert.Equal(ColumnEncryptionFlags.Store, actual.ColumnDefinitions[8].EncryptionFlags);
            Assert.Equal(new Identifier("CURRENT_TIMESTAMP"),
                ((ScalarFunction) actual.ColumnDefinitions[8].DefaultValue).FunctionName);
            Assert.True(actual.ColumnDefinitions[7].Nullable);
            Assert.Equal(new StringConstant("ABC"), actual.ColumnDefinitions[7].EnumValues[0]);
            Assert.Equal(new StringConstant("def"), actual.ColumnDefinitions[7].EnumValues[1]);
        }


        [Fact(DisplayName = "Parse PRISMADB EXPORT SETTINGS")]
        public void Parse_ExportSettings()
        {
            // Setup 
            var parser = new MySqlParser();
            var test = "PRISMADB EXPORT SETTINGS TO '/home/user/settings.json'";

            // Act 
            var result = parser.ParseToAst(test);

            // Assert 
            var actual = (ExportSettingsCommand) result[0];
            Assert.Equal("/home/user/settings.json", actual.FileUri.strvalue);
        }

        [Fact(DisplayName = "Parse SELECT w\\functions")]
        public void Parse_Function()
        {
            // Setup
            var parser = new MySqlParser();
            var test = "SELECT CONNECTION_ID()";

            // Act
            var result = parser.ParseToAst(test);

            // Assert
            var actual = (SelectQuery) result[0];

            Assert.Equal(new Identifier("CONNECTION_ID"), ((ScalarFunction) actual.SelectExpressions[0]).FunctionName);
            Assert.Equal(new Identifier("CONNECTION_ID()"), ((ScalarFunction) actual.SelectExpressions[0]).Alias);
            Assert.Empty(((ScalarFunction) actual.SelectExpressions[0]).Parameters);
        }

        [Fact(DisplayName = "Parse SELECT w\\functions w\\params")]
        public void Parse_FunctionWithParams()
        {
            // Setup
            var parser = new MySqlParser();
            var test = "SELECT COUNT(tt.col1) AS Num, TEST('string',12)";

            // Act
            var result = parser.ParseToAst(test);

            // Assert
            var actual = (SelectQuery) result[0];

            Assert.Equal(new Identifier("COUNT"), ((ScalarFunction) actual.SelectExpressions[0]).FunctionName);
            Assert.Equal(new Identifier("Num"), ((ScalarFunction) actual.SelectExpressions[0]).Alias);
            Assert.Equal(new TableRef("tt"),
                ((ColumnRef) ((ScalarFunction) actual.SelectExpressions[0]).Parameters[0]).Table);
            Assert.Equal(new Identifier("col1"),
                ((ColumnRef) ((ScalarFunction) actual.SelectExpressions[0]).Parameters[0]).ColumnName);

            Assert.Equal(new Identifier("TEST"), ((ScalarFunction) actual.SelectExpressions[1]).FunctionName);
            Assert.Equal(new Identifier("TEST('string',12)"),
                ((ScalarFunction) actual.SelectExpressions[1]).Alias);
            Assert.Equal("string",
                (((ScalarFunction) actual.SelectExpressions[1]).Parameters[0] as StringConstant)?.strvalue);
            Assert.Equal(12, (((ScalarFunction) actual.SelectExpressions[1]).Parameters[1] as IntConstant)?.intvalue);
        }

        [Fact(DisplayName = "Parse INSERT INTO")]
        public void Parse_InsertInto()
        {
            // Setup
            var parser = new MySqlParser();
            var test =
                "INSERT INTO `tt1` (tt1.col1, `tt1`.col2, `tt1`.`col3`, tt1.`col4`) VALUES ( 1, 12.345 , 'hey', \"hi\" ), (0,050,'  ', '&')";

            // Act
            var result = parser.ParseToAst(test);

            // Assert
            var actual = (InsertQuery) result[0];

            Assert.Equal(new TableRef("tt1"), actual.Into);
            Assert.Equal(new Identifier("col1"), actual.Columns[0].ColumnName);
            Assert.Equal(new TableRef("tt1"), actual.Columns[0].Table);
            Assert.Equal(new Identifier("col2"), actual.Columns[1].ColumnName);
            Assert.Equal(new TableRef("tt1"), actual.Columns[1].Table);
            Assert.Equal(new Identifier("col3"), actual.Columns[2].ColumnName);
            Assert.Equal(new TableRef("tt1"), actual.Columns[2].Table);
            Assert.Equal(new Identifier("col4"), actual.Columns[3].ColumnName);
            Assert.Equal(new TableRef("tt1"), actual.Columns[3].Table);
            Assert.Equal(2, actual.Values.Count);
            Assert.Equal(12.345, (actual.Values[0][1] as FloatingPointConstant)?.floatvalue);
            Assert.Equal("hey", (actual.Values[0][2] as StringConstant)?.strvalue);
            Assert.Equal(50, (actual.Values[1][1] as IntConstant)?.intvalue);
            Assert.Equal("  ", (actual.Values[1][2] as StringConstant)?.strvalue);
            Assert.Equal("&", (actual.Values[1][3] as StringConstant)?.strvalue);
        }

        [Fact(DisplayName = "Parse SELECT")]
        public void Parse_Select()
        {
            // Setup
            var parser = new MySqlParser();
            var test = "SELECT (a+b)*(a+b), ((a+b)*(a+b)), (((a+b)*(a+b))) FROM t ORDER BY a ASC, b DESC, c";

            // Act
            var result = parser.ParseToAst(test);

            // Assert
            var actual = (SelectQuery) result[0];

            Assert.Equal("(a+b)*(a+b)", actual.SelectExpressions[0].Alias.id);
            Assert.Equal("((a+b)*(a+b))", actual.SelectExpressions[1].Alias.id);
            Assert.Equal("((a+b)*(a+b))", actual.SelectExpressions[2].Alias.id);

            Assert.Equal(new Identifier("a"), actual.OrderBy.OrderColumns[0].Item1.ColumnName);
            Assert.Equal(new Identifier("b"), actual.OrderBy.OrderColumns[1].Item1.ColumnName);
            Assert.Equal(new Identifier("c"), actual.OrderBy.OrderColumns[2].Item1.ColumnName);
            Assert.Equal(OrderDirection.ASC, actual.OrderBy.OrderColumns[0].Item2);
            Assert.Equal(OrderDirection.DESC, actual.OrderBy.OrderColumns[1].Item2);
            Assert.Equal(OrderDirection.ASC, actual.OrderBy.OrderColumns[2].Item2);
        }

        [Fact(DisplayName = "Parse USE")]
        public void Parse_Use()
        {
            // Setup
            var parser = new MySqlParser();
            var test = "USE ThisDB";

            // Act
            var ex = Assert.Throws<NotSupportedException>(() => parser.ParseToAst(test));
            Assert.Equal("Database switching not supported.", ex.Message);
        }

        [Fact(DisplayName = "Parse variables")]
        public void Parse_Variables()
        {
            // Setup
            var parser = new MySqlParser();
            var test = "select @@version_comment limit 1; " +
                       "select @@`version_comment`";

            // Act
            var result = parser.ParseToAst(test);

            // Assert
            {
                var actual = (SelectQuery) result[0];
                Assert.Equal(new MySqlVariable("version_comment", "@@version_comment"), actual.SelectExpressions[0]);
                Assert.Equal((uint) 1, actual.Limit);
            }
            {
                var actual = (SelectQuery) result[1];
                Assert.Equal(new MySqlVariable("version_comment", "@@`version_comment`"), actual.SelectExpressions[0]);
                Assert.Null(actual.Limit);
            }
        }

        [Fact(DisplayName = "Parse JOIN")]
        public void Parse_Join()
        {
            // Setup
            var parser = new MySqlParser();
            var test = "select tt1.a AS abc, tt2.b FROM tt1 INNER JOIN tt2 ON tt1.c=tt2.c; " +
                       "select tt1.a, tt2.b FROM tt1 JOIN tt2 ON tt1.c=tt2.c WHERE tt1.a=123; ";

            // Act
            var result = parser.ParseToAst(test);

            // Assert
            {
                var actual = (SelectQuery)result[0];
                Assert.Equal(new ColumnRef("tt1", "a", "abc"), actual.SelectExpressions[0]);
                Assert.Equal(new ColumnRef("tt2", "b"), actual.SelectExpressions[1]);
                Assert.Equal(new TableRef("tt1"), actual.FromTables[0]);
                Assert.Equal(new TableRef("tt2"), actual.FromTables[1]);
                Assert.Equal(new ColumnRef("tt1", "c"), ((BooleanEquals)actual.Where.CNF.AND[0].OR[0]).left);
                Assert.Equal(new ColumnRef("tt2", "c"), ((BooleanEquals)actual.Where.CNF.AND[0].OR[0]).right);
            }
            {
                var actual = (SelectQuery)result[1];
                Assert.Equal(new ColumnRef("tt1", "a"), actual.SelectExpressions[0]);
                Assert.Equal(new ColumnRef("tt2", "b"), actual.SelectExpressions[1]);
                Assert.Equal(new TableRef("tt1"), actual.FromTables[0]);
                Assert.Equal(new TableRef("tt2"), actual.FromTables[1]);
                Assert.Equal(new ColumnRef("tt1", "c"), ((BooleanEquals)actual.Where.CNF.AND[0].OR[0]).left);
                Assert.Equal(new ColumnRef("tt2", "c"), ((BooleanEquals)actual.Where.CNF.AND[0].OR[0]).right);
                Assert.Equal(new ColumnRef("tt1", "a"), ((BooleanEquals)actual.Where.CNF.AND[1].OR[0]).left);
                Assert.Equal(new IntConstant(123), ((BooleanEquals)actual.Where.CNF.AND[1].OR[0]).right);
            }
        }

        [Fact(DisplayName = "Parse All Columns")]
        public void Parse_AllColumns()
        {
            // Setup
            var parser = new MySqlParser();
            var test = "select * from tt;";

            // Act
            var result = parser.ParseToAst(test);

            // Assert
            {
                var actual = (SelectQuery)result[0];
                Assert.Equal(new AllColumns(), actual.SelectExpressions[0]);
            }
        }
    }
}