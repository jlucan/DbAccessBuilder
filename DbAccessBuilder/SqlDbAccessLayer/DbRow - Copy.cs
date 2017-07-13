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
        const string PROC_UPDATE_ROW = "dbo.usp_SafeTableRowUpdate";
        const string UPDATE_PARAM_UPDATE_CLAUSE = "@UpdateClause";
        const string UPDATE_PARAM_WHERE_CLAUSE = "@WhereClause";
        const string UPDATE_PARAM_TABLE_NAME = "@TableName";
        protected SqlConnection GetSqlConn() { return DbAccess.GetSqlConn(); }     // Connection object

        protected DbColumn[] dataFields;       // Array of DbColumns for this row
        public DbRows DbRows;                           // Reference to our DbRows container object
        public bool Modified;                                // Flag indicating whether this row has been modified
        protected bool IsNewRow;                    // Flag indicating that this row is a new row (not in the database yet)

        protected DbRow(bool isNewRow) { this.IsNewRow = isNewRow; }        // This base class constructor

        public DbColumn DataField(int idx) { return this.dataFields[idx]; }         // Returns a DbColumn reference by its index in 'dataFields'
        public abstract string getColumnNameFromIdx(int colIdx);
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
                StringBuilder updateClause = new StringBuilder();
                StringBuilder whereClause = new StringBuilder();
                bool haveUpdate = false;
                bool haveWhere = false;
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
                            updateClause.Append(dbCol.ToSqlStr());
                            haveUpdate = true;
                        }
                    }
                }
                if (haveUpdate && haveWhere)
                {
                    SqlCommand sqlCmd = new SqlCommand(PROC_UPDATE_ROW);
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    sqlCmd.Parameters.Add(UPDATE_PARAM_UPDATE_CLAUSE, SqlDbType.NVarChar).Value = updateClause.ToString();
                    sqlCmd.Parameters.Add(UPDATE_PARAM_WHERE_CLAUSE, SqlDbType.NVarChar).Value = whereClause.ToString();
                    sqlCmd.Parameters.Add(UPDATE_PARAM_TABLE_NAME, SqlDbType.NVarChar).Value = this.PrimaryDatabaseTable;
                    try
                    {
                        sqlCmd.Connection = DbAccess.GetSqlConn();
                        sqlCmd.Connection.Open();
                        sqlCmd.ExecuteNonQuery();
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
}
