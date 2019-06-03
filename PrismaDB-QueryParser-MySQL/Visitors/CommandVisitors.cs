using Antlr4.Runtime.Misc;
using PrismaDB.QueryAST.DCL;
using PrismaDB.QueryAST.DDL;
using PrismaDB.QueryAST.DML;
using PrismaDB.QueryParser.MySQL.AntlrGrammer;
using System.Collections.Generic;

namespace PrismaDB.QueryParser.MySQL
{
    public partial class MySqlVisitor : MySqlParserBaseVisitor<object>
    {
        public override object VisitExportSettingsCommand([NotNull] MySqlParser.ExportSettingsCommandContext context)
        {
            return new ExportSettingsCommand(((StringConstant)Visit(context.stringLiteral())).strvalue);
        }

        public override object VisitUpdateKeysCommand([NotNull] MySqlParser.UpdateKeysCommandContext context)
        {
            var res = new UpdateKeysCommand();
            if (context.STATUS() != null)
                res.StatusCheck = true;
            return res;
        }

        public override object VisitEncryptCommand([NotNull] MySqlParser.EncryptCommandContext context)
        {
            var res = new EncryptColumnCommand();
            res.Column = (ColumnRef)Visit(context.fullColumnName());
            res.EncryptionFlags = ColumnEncryptionFlags.Store;
            if (context.encryptionOptions() != null)
                res.EncryptionFlags = (ColumnEncryptionFlags)Visit(context.encryptionOptions());
            if (context.STATUS() != null)
                res.StatusCheck = true;
            return res;
        }

        public override object VisitDecryptCommand([NotNull] MySqlParser.DecryptCommandContext context)
        {
            var res = new DecryptColumnCommand();
            res.Column = (ColumnRef)Visit(context.fullColumnName());
            if (context.STATUS() != null)
                res.StatusCheck = true;
            return res;
        }

        public override object VisitRegisterUserCommand([NotNull] MySqlParser.RegisterUserCommandContext context)
        {
            var res = new RegisterUserCommand();
            res.UserId = (StringConstant)Visit(context.user);
            res.Password = (StringConstant)Visit(context.password);
            return res;
        }

        public override object VisitRebalanceOpetreeCommand([NotNull] MySqlParser.RebalanceOpetreeCommandContext context)
        {
            var res = new RebalanceOpetreeCommand();
            if (context.constants() != null)
                res.WithValues = (List<Constant>)Visit(context.constants());
            if (context.STATUS() != null)
                res.StatusCheck = true;
            return res;
        }

        public override object VisitSaveOpetreeCommand([NotNull] MySqlParser.SaveOpetreeCommandContext context)
        {
            return new SaveOpetreeCommand();
        }
    }
}