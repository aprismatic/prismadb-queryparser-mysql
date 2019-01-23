using Antlr4.Runtime.Misc;
using PrismaDB.QueryAST;
using PrismaDB.QueryAST.DDL;
using System.Collections.Generic;

namespace PrismaDB.QueryParser.MySQL
{
    public partial class MySqlVisitor : AntlrMySqlParserBaseVisitor<object>
    {
        public override object VisitCreateTable([NotNull] AntlrMySqlParser.CreateTableContext context)
        {
            var res = new CreateTableQuery();
            res.TableName = (TableRef)Visit(context.tableName());
            res.ColumnDefinitions = (List<ColumnDefinition>)Visit(context.createDefinitions());
            return res;
        }

        public override object VisitCreateDefinitions([NotNull] AntlrMySqlParser.CreateDefinitionsContext context)
        {
            var res = new List<ColumnDefinition>();
            foreach (var createDefinition in context.createDefinition())
                res.Add((ColumnDefinition)Visit(createDefinition));
            return res;
        }

        public override object VisitColumnDeclaration([NotNull] AntlrMySqlParser.ColumnDeclarationContext context)
        {
            var res = (ColumnDefinition)Visit(context.columnDefinition());
            res.ColumnName = (Identifier)Visit(context.uid());
            return res;
        }
    }
}
