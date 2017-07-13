using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Web;

namespace SqlDbAccessLayer
{
    interface IDbRow
    {
        DbColumn DataField(int idx);
        string getColumnNameFromIdx(int colIdx);
        string PrimaryDatabaseTable { get; }
        bool updateDatabase(out string errMsg);
    }

    public abstract class DbRow : IDbRow
    {
        public abstract string PrimaryDatabaseTable { get; }

        protected DbColumn[] dataFields;     // Array of DbColumns for this row
        public DbRows DbRows;                // Reference to our DbRows container object
        public bool Modified;                // Flag indicating whether this row has been modified
        protected bool IsNewRow;             // Flag indicating that this row is a new row (not in the database yet)

        protected DbRow(bool isNewRow) { this.IsNewRow = isNewRow; }        // This base class constructor

        public DbColumn DataField(int idx) { return this.dataFields[idx]; }         // Returns a DbColumn reference by its index in 'dataFields'
        public abstract string getColumnNameFromIdx(int colIdx);
        public abstract SqlDbType getColumnSqlTypeFromIdx(int colIdx);
        public abstract int? getColumnSqlLenFromIdx(int colIdx);
        protected abstract bool insertNewRow(out string errMsg);
        protected void refreshRowsData(SqlCommand sqlCmd)
        {
            try
            {
                sqlCmd.Connection = DbAccess.GetSqlConn();
                sqlCmd.Connection.Open();
                SqlDataReader sqlRdr = sqlCmd.ExecuteReader();
                if (sqlRdr.HasRows)
                {
                    sqlRdr.Read();
                    foreach (DbColumn dbc in this.dataFields)
                    {
                        if (!dbc.isPrimaryKeyCol) dbc.readData(sqlRdr);
                    }
                }
                else
                {
                    throw new DbAccessException(String.Format("Error reading data from database table: {0}. Reason:\nA record with specified primary key was not found", this.PrimaryDatabaseTable));
                }
            }
            catch (Exception exc)
            {
                if (exc is DbAccessException)
                {
                    throw exc;
                }
                else
                {
                    throw new DbAccessException(String.Format("Error refreshing data from database table: {0}. Reason:\n{1}", this.PrimaryDatabaseTable, exc.Message));
                }
            }
            finally
            {
                sqlCmd.Connection.Close();
            }
        }

        protected bool execInsertCmd(SqlCommand sqlCmd, out string errMsg)
        {
            try
            {
                sqlCmd.Connection = DbAccess.GetSqlConn();
                sqlCmd.Connection.Open();
                sqlCmd.ExecuteNonQuery();
                errMsg = null;
                return true;
            }
            catch (Exception exc)
            {
                if (exc is DbAccessException)
                {
                    errMsg = exc.Message;
                }
                else
                {
                    errMsg = String.Format("Error inserting row in database table: {0}. Reason:\n{1}", this.PrimaryDatabaseTable, exc.Message);
                }
                return false;
            }
            finally
            {
                sqlCmd.Connection.Close();
            }
        }

        public bool isNewRecord()
        {
            return this.IsNewRow;
        }

        public bool updateDatabase(out string errMsg)
        {
            if (this.Modified)
            {
                if (this.IsNewRow)
                {
                    if (!this.insertNewRow(out errMsg)) return false;
                    // Recalculate row's modified flag
                    this.Modified = false;
                    foreach (DbColumn dbc in this.dataFields)
                    {
                        if (dbc.modified)
                        {
                            this.Modified = true;
                            break;
                        }
                    }
                    if (!this.Modified) return true;
                }
                // Update any fields in database that were modified
                StringBuilder testClause = new StringBuilder("SELECT COUNT(*) FROM " + this.PrimaryDatabaseTable);
                StringBuilder updateClause = new StringBuilder("UPDATE " + this.PrimaryDatabaseTable + " SET ");
                StringBuilder whereClause = new StringBuilder(" WHERE ");
                bool haveUpdate = false;
                bool haveWhere = false;
                SqlConnection sqlConn;
                SqlCommand sqlCmd = new SqlCommand();
                foreach (DbColumn dbCol in dataFields)
                {
                    if (dbCol.isPrimaryKeyCol)
                    {
                        if (haveWhere) whereClause.Append(" AND ");
                        whereClause.Append(dbCol.ToSqlStr());
                        haveWhere = true;
                    }
                    else
                    {
                        if (dbCol.isModDate && (dbCol is DbColDateTime || dbCol is DbColDateTimeNull))
                        {
                            if (haveUpdate) updateClause.Append(",");
                            if (dbCol is DbColDateTime)
                            {
                                updateClause.Append(((DbColDateTime)dbCol).ToSqlModDateStr());
                            }
                            else
                            {
                                updateClause.Append(((DbColDateTimeNull)dbCol).ToSqlModDateStr());
                            }
                            haveUpdate = true;
                        }
                        else if (dbCol.modified)
                        {
                            if (haveUpdate) updateClause.Append(",");
                            string colName = dbCol.DbColName;
                            updateClause.Append(colName + " = @" + colName);
                            if ((dbCol.dataType == typeof(string)) && dbCol.SqlDbLen.HasValue)
                                sqlCmd.Parameters.Add("@" + colName, dbCol.SqlDbType, (int)dbCol.SqlDbLen).Value = dbCol.getValue();
                            else
                                sqlCmd.Parameters.Add("@" + colName, dbCol.SqlDbType).Value = dbCol.getValue();
                            haveUpdate = true;
                        }
                    }
                }
                if (haveUpdate && haveWhere)
                {
                    testClause.Append(whereClause);
                    updateClause.Append(whereClause);
                    try
                    {
                        sqlConn = DbAccess.GetSqlConn();
                        SqlCommand sqlCmdTest = new SqlCommand(testClause.ToString(), sqlConn);
                        sqlCmd.CommandText = updateClause.ToString();
                        sqlCmd.Connection = sqlConn;
                        sqlConn.Open();
                        int nRows = (int)(sqlCmdTest.ExecuteScalar() ?? 0);
                        if (nRows == 1)
                        {
                            sqlCmd.ExecuteNonQuery();
                        }
                        else if (nRows == 0)
                        {
                            errMsg = String.Format("Error updating row in database table: {0}. Reason:\nNo database row found with a matching primary key.", this.PrimaryDatabaseTable);
                            return false;
                        }
                        else if (nRows > 1)
                        {
                            errMsg = String.Format("Error updating row in database table: {0}. Reason:\nMultiple database rows would have been modified.", this.PrimaryDatabaseTable);
                            return false;
                        }
                    }
                    catch (Exception exc)
                    {
                        errMsg = String.Format("Error updating row in database table: {0}. Reason:\n{1}", this.PrimaryDatabaseTable, exc.Message);
                        return false;
                    }
                    finally
                    {
                        sqlCmd.Connection.Close();
                    }
                    // Reset modified flags for columns written to database
                    foreach (DbColumn dbCol in dataFields)
                    {
                        if (!dbCol.isReadOnly && dbCol.modified) dbCol.resetModified();
                    }
                }
                this.Modified = false;
            }
            errMsg = null;
            return true;
        }
    }

    public struct ColumnAttributes
    {
        public int ColIdx;
        public string ColName;
        public SqlDbType SqlType;
        public int? SqlLen;

        public ColumnAttributes(int colIdx, string colName, SqlDbType sqlType, int? sqlLen)
        {
            ColIdx = colIdx;
            ColName = colName;
            SqlType = sqlType;
            SqlLen = sqlLen;
        }
    }
}
