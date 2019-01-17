using Antlr4.Runtime.Misc;
using PrismaDB.QueryAST.DDL;

namespace PrismaDB.QueryParser.MySQL
{
    public partial class MySqlVisitor : AntlrMySqlParserBaseVisitor<object>
    {
        public override object VisitCreateTable([NotNull] AntlrMySqlParser.CreateTableContext context)
        {
            var ctq = new CreateTableQuery();
            return ctq;
        }
    }
}
