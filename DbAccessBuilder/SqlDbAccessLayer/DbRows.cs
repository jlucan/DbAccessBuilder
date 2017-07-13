using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;

namespace SqlDbAccessLayer
{
    interface IDbRows
    {
        string PrimaryDbTableName { get; }
    }

    public abstract class DbRows
    {
        public abstract string PrimaryDbTableName { get; }
        public abstract void AddDbRow(DbRow dbRow);
    }
}
