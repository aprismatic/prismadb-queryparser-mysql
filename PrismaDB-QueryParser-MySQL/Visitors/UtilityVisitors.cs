using Antlr4.Runtime.Misc;
using PrismaDB.QueryAST;
using PrismaDB.QueryAST.DDL;
using PrismaDB.QueryParser.MySQL.AntlrGrammer;

namespace PrismaDB.QueryParser.MySQL
{
    public partial class MySqlVisitor : MySqlParserBaseVisitor<object>
    {
        public override object VisitUseStatement([NotNull] MySqlParser.UseStatementContext context)
        {
            return new UseStatement((DatabaseRef)Visit(context.databaseName()));
        }
    }
}