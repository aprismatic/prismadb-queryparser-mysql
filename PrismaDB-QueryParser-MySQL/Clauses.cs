using System.Collections.Generic;
using PrismaDB.QueryAST.DML;
using PrismaDB.QueryAST.Result;

namespace PrismaDB.QueryParser.MySQL
{
    internal class AndClause : Expression
    {
        public Expression left, right;

        public AndClause(Expression left, Expression right)
        {
            this.left = left;
            this.right = right;
        }

        public override object Clone()
        {
            throw new System.NotImplementedException();
        }

        public override bool Equals(object other)
        {
            throw new System.NotImplementedException();
        }

        public override object Eval(ResultRow r)
        {
            throw new System.NotImplementedException();
        }

        public override List<ColumnRef> GetColumns()
        {
            throw new System.NotImplementedException();
        }

        public override int GetHashCode()
        {
            throw new System.NotImplementedException();
        }

        public override List<ColumnRef> GetNoCopyColumns()
        {
            throw new System.NotImplementedException();
        }

        public override void setValue(params object[] value)
        {
            throw new System.NotImplementedException();
        }

        public override string ToString()
        {
            throw new System.NotImplementedException();
        }
    }

    internal class OrClause : Expression
    {
        public Expression left, right;

        public OrClause(Expression left, Expression right)
        {
            this.left = left;
            this.right = right;
        }

        public override object Clone()
        {
            throw new System.NotImplementedException();
        }

        public override bool Equals(object other)
        {
            throw new System.NotImplementedException();
        }

        public override object Eval(ResultRow r)
        {
            throw new System.NotImplementedException();
        }

        public override List<ColumnRef> GetColumns()
        {
            throw new System.NotImplementedException();
        }

        public override int GetHashCode()
        {
            throw new System.NotImplementedException();
        }

        public override List<ColumnRef> GetNoCopyColumns()
        {
            throw new System.NotImplementedException();
        }

        public override void setValue(params object[] value)
        {
            throw new System.NotImplementedException();
        }

        public override string ToString()
        {
            throw new System.NotImplementedException();
        }
    }
}
