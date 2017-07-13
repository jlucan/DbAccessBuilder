using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;

namespace SqlDbAccess
{
    // Declare a delegate that will return a connection string
    public delegate void getconnstring(ref string connStr);

    // To use the delegate method of getting database connection string 
    //
    //      SqlDbAccess.DbAccess.GetConnString = getConnStr;        // Set the delegate callback method
    //
    // Put a method like this where it has access to the root web.config ( A good place to put this is 'Application_Start' in Global.asax file)
    //      public static string GetConnString()
    //      {
    //          return ConfigurationManager.ConnectionStrings["SqlProjectData"].ConnectionString;
    //       }

    public static class DbAccess
    {
        static string ConnString = null;
        public static getconnstring GetConnString = null;

        public static void SetConnectionString(string connStr)
        {
            DbAccess.ConnString = connStr;
        }

        public static string GetConnectionString()
        {
            SqlConnection sqlConn = GetSqlConn();
            return ConnString;
        }

        internal static SqlConnection GetSqlConn()
        {
            // If connection string hasn't been set yet see if a callback has been set up
            if (String.IsNullOrEmpty(ConnString))
            {
                if (GetConnString != null) GetConnString(ref ConnString);
            }

            return String.IsNullOrEmpty(ConnString) ? null : new SqlConnection(ConnString);
        }

        // Returns a SqlDataReader object given a comma separated list of columns, a database table name and an optional WHERE clause
        public static SqlDataReader getSqlDataReader(string returnedColumns, string whereClause, string primaryTable)
        {
            StringBuilder sqlQuery = new StringBuilder("SELECT ");
            sqlQuery.Append(returnedColumns);
            sqlQuery.Append(" FROM ");
            sqlQuery.Append(primaryTable);
            if (!String.IsNullOrEmpty(whereClause))
            {
                sqlQuery.Append(" WHERE ");
                sqlQuery.Append(whereClause);
            }
            SqlCommand sqlCmd = new SqlCommand(sqlQuery.ToString(), DbAccess.GetSqlConn());
            return sqlCmd.ExecuteReader(CommandBehavior.CloseConnection);
        }

        public static bool ExecuteNonQueryCmd(string cmdText, out string errMsg)
        {
            SqlCommand sqlCmd = new SqlCommand(cmdText);
            return ExecuteNonQueryCmd(sqlCmd, out errMsg);
        }

        public static bool ExecuteNonQueryCmd(SqlCommand sqlCmd, out string errMsg)
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
                errMsg = exc.Message;
                return false;
            }
            finally
            {
                sqlCmd.Connection.Close();
            }
        }

        /// <summary>
        /// Static method that calls ExecuteScalar on the passed SqlCommand object).
        /// </summary>
        public static object execScalar(SqlCommand sqlCmd, string cmdDesc)
        {
            try
            {
                sqlCmd.Connection = DbAccess.GetSqlConn();
                sqlCmd.Connection.Open();
                return sqlCmd.ExecuteScalar();
            }
            catch (Exception exc)
            {
                if (exc is DbAccessException)
                {
                    throw;
                }
                else
                {
                    throw new DbAccessException(String.Format("Error executing scalar command: {0}. Reason:\n{1}", cmdDesc, exc.Message), exc);
                }
            }
            finally
            {
                sqlCmd.Connection.Close();
            }
        }

        public static bool CheckRowExists(out string errMsg, out bool exists, string fromTable, string col1, object val1, string col2 = null, object val2 = null, string col3 = null, object val3 = null, string col4 = null, object val4 = null)
        {
            errMsg = null;
            exists = false;
            if (String.IsNullOrEmpty(fromTable) || String.IsNullOrEmpty(col1))
            {
                errMsg = "DbAccess.CheckRowExists method called without specifying a database table or at least one column.";
                return false;
            }
            bool haveCol2 = !String.IsNullOrEmpty(col2);
            bool haveCol3 = !String.IsNullOrEmpty(col3);
            bool haveCol4 = !String.IsNullOrEmpty(col4);
            // Make a select string
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("SELECT 1 FROM {0} WHERE {1} = @{1}" , fromTable, col1);
            if (haveCol2) sb.AppendFormat(" AND {0} = @{0}", col2);
            if (haveCol3) sb.AppendFormat(" AND {0} = @{0}", col3);
            if (haveCol4) sb.AppendFormat(" AND {0} = @{0}", col4);

            try
            {
                SqlCommand sqlCmd = new SqlCommand(sb.ToString());
                sqlCmd.CommandType = CommandType.Text;
                sqlCmd.Parameters.AddWithValue("@" + col1, val1);
                if (haveCol2) sqlCmd.Parameters.AddWithValue("@" + col2, val2);
                if (haveCol3) sqlCmd.Parameters.AddWithValue("@" + col3, val3);
                if (haveCol4) sqlCmd.Parameters.AddWithValue("@" + col4, val4);
                sqlCmd.Connection = DbAccess.GetSqlConn();
                sqlCmd.Connection.Open();
                exists = ((int?)sqlCmd.ExecuteScalar() == 1);
            }
            catch (DbAccessException dbaexc)
            {
                errMsg = dbaexc.Message;
                return false;
            }
            catch (Exception exc)
            {
                errMsg = String.Format("Error checking existence of a row from {0}. Reason: {1}", fromTable, exc.Message);
                return false;
            }
            return true;


        }
    }   // End DbAccess

    //======================================================================
    //  Base Class for a generated class that contains a list of DbRow derived objects
    //======================================================================

    interface IDbRows
    {
    }

    public abstract class DbRows
    {
        protected bool execNonQuery(SqlCommand sqlCmd, out string errMsg)
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
                    errMsg = String.Format("Error executing SQL command: {0}. Reason:\n{1}", sqlCmd.CommandText, exc.Message);
                }
                return false;
            }
            finally
            {
                sqlCmd.Connection.Close();
            }
        }
    }

    //=================================================================
    //  Base Class for a generated class that contains a DbRow derived object
    //=================================================================

    interface IDbRow
    {
        DbColumn DataField(int idx);
        string getColumnNameFromIdx(int colIdx);
        bool updateDatabase(out string errMsg);
    }

    public abstract class DbRow : IDbRow
    {
        protected abstract string PrimaryDatabaseTable { get; }

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
            }
            if (this.Modified)
            {
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
                            if (!this.IsNewRow)     // Don't set mod date if this is a new row
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
                        }
                        else if (dbCol.modified)
                        {
                            if (haveUpdate) updateClause.Append(",");
                            string colName = dbCol.DbColName;
                            updateClause.Append(colName + " = @" + colName);
                            if ((dbCol.dataType == typeof(string)) && dbCol.SqlDbLen.HasValue)
                                sqlCmd.Parameters.Add("@" + colName, dbCol.SqlDbType, (int)dbCol.SqlDbLen).Value = dbCol.getValue();
                            else
                                sqlCmd.Parameters.Add("@" + colName, dbCol.SqlDbType).Value = dbCol.getValue() ?? DBNull.Value;
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
            this.IsNewRow = false;      // Cannot be a new row at this point
            errMsg = null;
            return true;
        }
    }  // End of DbRow

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

    //======================================================================================= 
    //  DbColumn:
    //  Data Holders that hold one data item corresponding to on column in one database record
    //  The characteristics or each data holder are:
    //      1. The data type (the .net datatype that best fits datatype of the underlying database field)
    //      2. Nullable which should equal the  nullable property of the underlying database field
    //      3. Read Only or Read/Write which depends mostly on whether the underlying database field is 
    //          a non primary key field in the primary table as opposed to a primary key field or a field 
    //          in a joined table. A field might also be considered read only for other practical reasons.
    //  Data Holders that are Read/Write contain 'modified' flag and a 'ToSqlStr' method that facilitates
    //  writing modified data back to the database.
    //=======================================================================================

    public interface IDbColumn
    {
        int DbColNameIdx { get; }
        DbRow DbRow { get; }
        bool modified { get; }
        bool isAutoID { get; }
        bool isPrimaryKeyCol { get; }
        bool isReadOnly { get; }
        Type dataType { get; }
        bool isNullable { get; }

        void readData(SqlDataReader rdr);
        void getData(SqlDataReader rdr, int ord);
        string ToSqlStr();
        void resetModified();
    }

    public abstract class DbColumn : IDbColumn
    {
        // Column type enum
        public enum ColAttrib { AutoID, PrKey, ReadWrite, ReadOnly, ModDate, CreateDate };

        public DbRow DbRow { get; private set; }
        public int DbColNameIdx { get; private set; }
        public string DbColName { get { return DbRow.getColumnNameFromIdx(this.DbColNameIdx); } }
        public SqlDbType SqlDbType { get { return DbRow.getColumnSqlTypeFromIdx(this.DbColNameIdx); } }
        public int? SqlDbLen { get { return DbRow.getColumnSqlLenFromIdx(this.DbColNameIdx); } }
        public bool modified { get; protected set; }
        public bool isAutoID { get; private set; }
        public bool isPrimaryKeyCol { get; private set; }
        public bool isReadOnly { get; private set; }
        public bool isNullable { get; private set; }
        public bool isModDate { get; private set; }
        public Type dataType { get; private set; }

        protected DbColumn(DbRow dbRow, int dbColIdx, ColAttrib colAttrib)
        {
            this.DbRow = dbRow;
            this.DbColNameIdx = dbColIdx;
            //ColAttrib colType = dbRow.getColumnAttrb(dbColIdx);
            switch (colAttrib)
            {
                case ColAttrib.AutoID:
                    this.isPrimaryKeyCol = true;
                    this.isAutoID = true;
                    this.isNullable = false;
                    this.isReadOnly = true;
                    break;
                case ColAttrib.PrKey:
                    this.isPrimaryKeyCol = true;
                    this.isNullable = false;
                    break;
                case ColAttrib.ReadWrite:
                    break;
                case ColAttrib.ModDate:
                    this.isReadOnly = true;
                    this.isModDate = true;
                    break;
                case ColAttrib.ReadOnly:
                case ColAttrib.CreateDate:
                default:
                    this.isReadOnly = true;
                    break;
            }
            this.modified = false;
        }

        // Gets column ordinal by column name and calls getData which is must be defined in a derived class
        public void readData(SqlDataReader rdr)
        {
            int ord;
            try
            {
                ord = rdr.GetOrdinal(DbRow.getColumnNameFromIdx(DbColNameIdx));
            }
            catch (IndexOutOfRangeException)
            {
                throw new DbAccessException(string.Format("Db Column name '{0}' was not found.", DbRow.getColumnNameFromIdx(this.DbColNameIdx)));
            }
            getData(rdr, ord);
        }

        public abstract void getData(SqlDataReader rdr, int ord);
        public abstract string ToSqlStr();
        public abstract object getValue();
        public void resetModified() { this.modified = false; }
    }

    //========================
    // Int Data Columns
    // ========================

    public class DbColInt : DbColumn
    {
        int _value;
        public int Value
        {
            get { return _value; }
            set
            {
                if (this.isReadOnly) throw new DbAccessException(String.Format("Column {0} cannot be modified = it is readonly", DbRow.getColumnNameFromIdx(this.DbColNameIdx)));
                else if (value != this._value) { this._value = value; modified = true; DbRow.Modified = true; }
            }
        }
        public void initValue(int val) { if (this._value != val) { this._value = val; this.modified = true; } }
        public new bool isNullable { get { return false; } }
        public DbColInt(int colIdx, DbRow dbRow, SqlDataReader rdr, ColAttrib colAttrib)
            : base(dbRow, colIdx, colAttrib)
        {
            if (rdr != null) readData(rdr);
            else this._value = 0;
        }

        public override void getData(SqlDataReader rdr, int ord)
        {
            try
            {
                this._value = rdr.GetInt32(ord);
                this.modified = false;
            }
            catch (Exception exc)
            {
                throw new DbAccessException(string.Format("Error reading column '{0}' - {1}", DbRow.getColumnNameFromIdx(this.DbColNameIdx), exc.Message));
            }
        }
        public override object getValue() { return this.Value; }
        public override string ToString() { return this._value.ToString(); }
        public override string ToSqlStr() { return DbRow.getColumnNameFromIdx(this.DbColNameIdx) + " = " + this.Value.ToString(); }
    }

    public class DbColIntNull : DbColumn
    {
        int? _value;
        public int? Value
        {
            get { return _value; }
            set
            {
                if (this.isReadOnly) throw new DbAccessException(String.Format("Column {0} cannot be modified = it is readonly", DbRow.getColumnNameFromIdx(this.DbColNameIdx)));
                else if (value != this._value) { this._value = value; modified = true; DbRow.Modified = true; }
            }
        }
        public void initValue(int? val) { if (this._value != val) { this._value = val; this.modified = true; } }
        public new bool isNullable { get { return true; } }
        public DbColIntNull(int colIdx, DbRow dbRow, SqlDataReader rdr, ColAttrib colAttrib)
            : base(dbRow, colIdx, colAttrib)
        {
            if (rdr != null) readData(rdr);
            else this._value = null;
        }

        public override void getData(SqlDataReader rdr, int ord)
        {
            try
            {
                this._value = rdr.IsDBNull(ord) ? (int?)null : rdr.GetInt32(ord);
                this.modified = false;
            }
            catch (Exception exc)
            {
                throw new DbAccessException(string.Format("Error reading column '{0}' - {1}", DbRow.getColumnNameFromIdx(this.DbColNameIdx), exc.Message));
            }
        }
        public override object getValue() { return this.Value; }
        public override string ToString() { return (this.Value.HasValue) ? ((int)this.Value).ToString() : null; }
        public override string ToSqlStr() { return DbRow.getColumnNameFromIdx(this.DbColNameIdx) + " = " + ((this.Value.HasValue) ? ((int)this.Value).ToString() : "NULL"); }
    }

    //========================
    // Short Data Columns
    // ========================

    public class DbColShort : DbColumn
    {
        short _value;
        public short Value
        {
            get { return _value; }
            set
            {
                if (this.isReadOnly) throw new DbAccessException(String.Format("Column {0} cannot be modified = it is readonly", DbRow.getColumnNameFromIdx(this.DbColNameIdx)));
                else if (value != this._value) { this._value = value; modified = true; DbRow.Modified = true; }
            }
        }
        public void initValue(short val) { if (this._value != val) { this._value = val; this.modified = true; } }
        public new bool isNullable { get { return false; } }
        public DbColShort(int colIdx, DbRow dbRow, SqlDataReader rdr, ColAttrib colAttrib)
            : base(dbRow, colIdx, colAttrib)
        {
            if (rdr != null) readData(rdr);
            else this._value = 0;
        }

        public override void getData(SqlDataReader rdr, int ord)
        {
            try
            {
                this._value = rdr.GetInt16(ord);
                this.modified = false;
            }
            catch (Exception exc)
            {
                throw new DbAccessException(string.Format("Error reading column '{0}' - {1}", DbRow.getColumnNameFromIdx(this.DbColNameIdx), exc.Message));
            }
        }
        public override object getValue() { return this.Value; }
        public override string ToString() { return this._value.ToString(); }
        public override string ToSqlStr() { return DbRow.getColumnNameFromIdx(this.DbColNameIdx) + " = " + this.Value.ToString(); }
    }

    public class DbColShortNull : DbColumn
    {
        short? _value;
        public short? Value
        {
            get { return _value; }
            set
            {
                if (this.isReadOnly) throw new DbAccessException(String.Format("Column {0} cannot be modified = it is readonly", DbRow.getColumnNameFromIdx(this.DbColNameIdx)));
                else if (value != this._value) { this._value = value; modified = true; DbRow.Modified = true; }
            }
        }
        public void initValue(short? val) { if (this._value != val) { this._value = val; this.modified = true; } }
        public new bool isNullable { get { return true; } }
        public DbColShortNull(int colIdx, DbRow dbRow, SqlDataReader rdr, ColAttrib colAttrib)
            : base(dbRow, colIdx, colAttrib)
        {
            if (rdr != null) readData(rdr);
            else this._value = null;
        }

        public override void getData(SqlDataReader rdr, int ord)
        {
            try
            {
                this._value = rdr.IsDBNull(ord) ? (short?)null : rdr.GetInt16(ord);
                this.modified = false;
            }
            catch (Exception exc)
            {
                throw new DbAccessException(string.Format("Error reading column '{0}' - {1}", DbRow.getColumnNameFromIdx(this.DbColNameIdx), exc.Message));
            }
        }
        public override object getValue() { return this.Value; }
        public override string ToString() { return (this.Value.HasValue) ? ((short)this.Value).ToString() : null; }
        public override string ToSqlStr() { return DbRow.getColumnNameFromIdx(this.DbColNameIdx) + " = " + ((this.Value.HasValue) ? ((short)this.Value).ToString() : "NULL"); }
    }

    //========================
    // Byte Data Columns
    // ========================

    public class DbColByte : DbColumn
    {
        byte _value;
        public byte Value
        {
            get { return _value; }
            set
            {
                if (this.isReadOnly) throw new DbAccessException(String.Format("Column {0} cannot be modified = it is readonly", DbRow.getColumnNameFromIdx(this.DbColNameIdx)));
                else if (value != this._value) { this._value = value; modified = true; DbRow.Modified = true; }
            }
        }
        public void initValue(byte val) { if (this._value != val) { this._value = val; this.modified = true; } }
        public new bool isNullable { get { return false; } }
        public DbColByte(int colIdx, DbRow dbRow, SqlDataReader rdr, ColAttrib colAttrib)
            : base(dbRow, colIdx, colAttrib)
        {
            if (rdr != null) readData(rdr);
            else this._value = 0;
        }

        public override void getData(SqlDataReader rdr, int ord)
        {
            try
            {
                this._value = rdr.GetByte(ord);
                this.modified = false;
            }
            catch (Exception exc)
            {
                throw new DbAccessException(string.Format("Error reading column '{0}' - {1}", DbRow.getColumnNameFromIdx(this.DbColNameIdx), exc.Message));
            }
        }
        public override object getValue() { return this.Value; }
        public override string ToString() { return this._value.ToString(); }
        public override string ToSqlStr() { return DbRow.getColumnNameFromIdx(this.DbColNameIdx) + " = " + this.Value.ToString(); }
    }

    public class DbColByteNull : DbColumn
    {
        byte? _value;
        public byte? Value
        {
            get { return _value; }
            set
            {
                if (this.isReadOnly) throw new DbAccessException(String.Format("Column {0} cannot be modified = it is readonly", DbRow.getColumnNameFromIdx(this.DbColNameIdx)));
                else if (value != this._value) { this._value = value; modified = true; DbRow.Modified = true; }
            }
        }
        public void initValue(byte? val) { if (this._value != val) { this._value = val; this.modified = true; } }
        public new bool isNullable { get { return true; } }
        public DbColByteNull(int colIdx, DbRow dbRow, SqlDataReader rdr, ColAttrib colAttrib)
            : base(dbRow, colIdx, colAttrib)
        {
            if (rdr != null) readData(rdr);
            else this._value = null;
        }

        public override void getData(SqlDataReader rdr, int ord)
        {
            try
            {
                this._value = rdr.IsDBNull(ord) ? (byte?)null : rdr.GetByte(ord);
                this.modified = false;
            }
            catch (Exception exc)
            {
                throw new DbAccessException(string.Format("Error reading column '{0}' - {1}", DbRow.getColumnNameFromIdx(this.DbColNameIdx), exc.Message));
            }
        }
        public override object getValue() { return this.Value; }
        public override string ToString() { return (this.Value.HasValue) ? ((byte)this.Value).ToString() : null; }
        public override string ToSqlStr() { return DbRow.getColumnNameFromIdx(this.DbColNameIdx) + " = " + ((this.Value.HasValue) ? ((int)this.Value).ToString() : "NULL"); }
    }

    //========================
    // Decimal Data Columns
    // ========================

    public class DbColDecimal : DbColumn
    {
        decimal _value;
        public decimal Value
        {
            get { return _value; }
            set
            {
                if (this.isReadOnly) throw new DbAccessException(String.Format("Column {0} cannot be modified = it is readonly", DbRow.getColumnNameFromIdx(this.DbColNameIdx)));
                else if (value != this._value) { this._value = value; modified = true; DbRow.Modified = true; }
            }
        }
        public void initValue(decimal val) { if (this._value != val) { this._value = val; this.modified = true; } }
        public new bool isNullable { get { return false; } }
        public DbColDecimal(int colIdx, DbRow dbRow, SqlDataReader rdr, ColAttrib colAttrib)
            : base(dbRow, colIdx, colAttrib)
        {
            if (rdr != null) readData(rdr);
            else this._value = 0;
        }

        public override void getData(SqlDataReader rdr, int ord)
        {
            try
            {
                this._value = rdr.GetDecimal(ord);
                this.modified = false;
            }
            catch (Exception exc)
            {
                throw new DbAccessException(string.Format("Error reading column '{0}' - {1}", DbRow.getColumnNameFromIdx(this.DbColNameIdx), exc.Message));
            }
        }
        public override object getValue() { return this.Value; }
        public override string ToString() { return this._value.ToString(); }
        public override string ToSqlStr() { return DbRow.getColumnNameFromIdx(this.DbColNameIdx) + " = " + this.Value.ToString(); }
    }

    public class DbColDecimalNull : DbColumn
    {
        decimal? _value;
        public decimal? Value
        {
            get { return _value; }
            set
            {
                if (this.isReadOnly) throw new DbAccessException(String.Format("Column {0} cannot be modified = it is readonly", DbRow.getColumnNameFromIdx(this.DbColNameIdx)));
                else if (value != this._value) { this._value = value; modified = true; DbRow.Modified = true; }
            }
        }
        public void initValue(decimal? val) { if (this._value != val) { this._value = val; this.modified = true; } }
        public new bool isNullable { get { return true; } }
        public DbColDecimalNull(int colIdx, DbRow dbRow, SqlDataReader rdr, ColAttrib colAttrib)
            : base(dbRow, colIdx, colAttrib)
        {
            if (rdr != null) readData(rdr);
            else this._value = null;
        }

        public override void getData(SqlDataReader rdr, int ord)
        {
            try
            {
                this._value = rdr.IsDBNull(ord) ? (decimal?)null : rdr.GetDecimal(ord);
                this.modified = false;
            }
            catch (Exception exc)
            {
                throw new DbAccessException(string.Format("Error reading column '{0}' - {1}", DbRow.getColumnNameFromIdx(this.DbColNameIdx), exc.Message));
            }
        }
        public override object getValue() { return this.Value; }
        public override string ToString() { return (this.Value.HasValue) ? ((decimal)this.Value).ToString() : null; }
        public override string ToSqlStr() { return DbRow.getColumnNameFromIdx(this.DbColNameIdx) + " = " + ((this.Value.HasValue) ? ((decimal)this.Value).ToString() : "NULL"); }
    }

    //========================
    // String Data Columns
    // ========================
    public class DbColString : DbColumn
    {
        string _value;
        public string Value
        {
            get { return _value; }
            set
            {
                if (this.isReadOnly) throw new DbAccessException(String.Format("Column {0} cannot be modified = it is readonly", DbRow.getColumnNameFromIdx(this.DbColNameIdx)));
                else if (value != this._value) { this._value = value; modified = true; DbRow.Modified = true; }
            }
        }
        public void initValue(string val) { if (this._value != val) { this._value = val; this.modified = true; } }
        public new bool isNullable { get { return false; } }
        public DbColString(int colIdx, DbRow dbRow, SqlDataReader rdr, ColAttrib colAttrib)
            : base(dbRow, colIdx, colAttrib)
        {
            if (rdr != null) readData(rdr);
            else this._value = String.Empty;
        }

        public override void getData(SqlDataReader rdr, int ord)
        {
            try
            {
                this._value = rdr.GetString(ord);
                this.modified = false;
            }
            catch (Exception exc)
            {
                throw new DbAccessException(string.Format("Error reading column '{0}' - {1}", DbRow.getColumnNameFromIdx(this.DbColNameIdx), exc.Message));
            }
        }
        public override object getValue() { return this.Value; }
        public override string ToString() { return this._value.ToString(); }
        public override string ToSqlStr() { return DbRow.getColumnNameFromIdx(this.DbColNameIdx) + " = '" + this.Value.Replace("'", "''") + "'"; }
    }

    public class DbColStringNull : DbColumn
    {
        string _value;
        public string Value
        {
            get { return _value; }
            set
            {
                if (this.isReadOnly) throw new DbAccessException(String.Format("Column {0} cannot be modified = it is readonly", DbRow.getColumnNameFromIdx(this.DbColNameIdx)));
                else if ((value ?? String.Empty) != (this._value ?? String.Empty)) { this._value = value; modified = true; DbRow.Modified = true; }
            }
        }
        public void initValue(string val) { if (this._value != val) { this._value = val; this.modified = true; } }
        public new bool isNullable { get { return true; } }
        public DbColStringNull(int colIdx, DbRow dbRow, SqlDataReader rdr, ColAttrib colAttrib)
            : base(dbRow, colIdx, colAttrib)
        {
            if (rdr != null) readData(rdr);
            else this._value = null;
        }

        public override void getData(SqlDataReader rdr, int ord)
        {
            try
            {
                this._value = rdr.IsDBNull(ord) ? (string)null : rdr.GetString(ord);
                this.modified = false;
            }
            catch (Exception exc)
            {
                throw new DbAccessException(string.Format("Error reading column '{0}' - {1}", DbRow.getColumnNameFromIdx(this.DbColNameIdx), exc.Message));
            }
        }
        public override object getValue() { return this.Value; }
        public override string ToString() { return this._value.ToString(); }
        public override string ToSqlStr() { return DbRow.getColumnNameFromIdx(this.DbColNameIdx) + " = " + ((this.Value == null) ? "NULL" : "'" + this.Value.Replace("'", "''") + "'"); }
    }

    //========================
    // Datetime Data Columns
    // ========================

    public class DbColDateTime : DbColumn
    {
        DateTime _value;
        public DateTime Value
        {
            get { return _value; }
            set
            {
                if (this.isReadOnly) throw new DbAccessException(String.Format("Column {0} cannot be modified = it is readonly", DbRow.getColumnNameFromIdx(this.DbColNameIdx)));
                else if (value != this._value) { this._value = value; modified = true; DbRow.Modified = true; }
            }
        }
        public void initValue(DateTime val) { if (this._value != val) { this._value = val; this.modified = true; } }
        public new bool isNullable { get { return false; } }
        public DbColDateTime(int colIdx, DbRow dbRow, SqlDataReader rdr, ColAttrib colAttrib)
            : base(dbRow, colIdx, colAttrib)
        {
            if (rdr != null) readData(rdr);
            else this._value = default(DateTime);
        }

        public override void getData(SqlDataReader rdr, int ord)
        {
            try
            {
                this._value = rdr.GetDateTime(ord);
                this.modified = false;
            }
            catch (Exception exc)
            {
                throw new DbAccessException(string.Format("Error reading column '{0}' - {1}", DbRow.getColumnNameFromIdx(this.DbColNameIdx), exc.Message));
            }
        }
        public override object getValue() { return this.Value; }
        public override string ToString() { return this._value.ToString("g"); }
        public override string ToSqlStr() { return DbRow.getColumnNameFromIdx(this.DbColNameIdx) + " = '" + this._value.ToString("yyyy-MM-dd HH:mm:ss") + "'"; }
        public string ToSqlModDateStr() { return DbRow.getColumnNameFromIdx(this.DbColNameIdx) + " = GETDATE()"; }
        //public string ToSqlModDateStr() { return DbRow.getColumnNameFromIdx(this.DbColNameIdx) + " = GETUTCDATE()"; }
        //public string ToSqlModDateStr() { return DbRow.getColumnNameFromIdx(this.DbColNameIdx) + " = '" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "'"; }
    }

    public class DbColDateTimeNull : DbColumn
    {
        DateTime? _value;
        public DateTime? Value
        {
            get { return _value; }
            set
            {
                if (this.isReadOnly) throw new DbAccessException(String.Format("Column {0} cannot be modified = it is readonly", DbRow.getColumnNameFromIdx(this.DbColNameIdx)));
                else if (value != this._value) { this._value = value; modified = true; DbRow.Modified = true; }
            }
        }
        public void initValue(DateTime? val) { if (this._value != val) { this._value = val; this.modified = true; } }
        public new bool isNullable { get { return true; } }
        public DbColDateTimeNull(int colIdx, DbRow dbRow, SqlDataReader rdr, ColAttrib colAttrib)
            : base(dbRow, colIdx, colAttrib)
        {
            if (rdr != null) readData(rdr);
            else this._value = null;
        }

        public override void getData(SqlDataReader rdr, int ord)
        {
            try
            {
                this._value = rdr.IsDBNull(ord) ? (DateTime?)null : rdr.GetDateTime(ord);
                this.modified = false;
            }
            catch (Exception exc)
            {
                throw new DbAccessException(string.Format("Error reading column '{0}' - {1}", DbRow.getColumnNameFromIdx(this.DbColNameIdx), exc.Message));
            }
        }
        public override object getValue() { return this.Value; }
        public override string ToString() { return (this._value.HasValue) ? ((DateTime)this.Value).ToString("g") : null; }
        public override string ToSqlStr() { return DbRow.getColumnNameFromIdx(this.DbColNameIdx) + " = '" + ((this._value.HasValue) ? ((DateTime)this._value).ToString("yyyy-MM-dd HH:mm:ss") + "'" : "NULL"); }
        public string ToSqlModDateStr() { return DbRow.getColumnNameFromIdx(this.DbColNameIdx) + " = GETDATE()"; }
        //public string ToSqlModDateStr() { return DbRow.getColumnNameFromIdx(this.DbColNameIdx) + " = GETUTCDATE()"; }
        //public string ToSqlModDateStr() { return DbRow.getColumnNameFromIdx(this.DbColNameIdx) + " = '" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "'"; }
    }
    //========================
    // Boolean Data Columns
    // ========================

    public class DbColBool : DbColumn
    {
        bool _value;
        public bool Value
        {
            get { return _value; }
            set
            {
                if (this.isReadOnly) throw new DbAccessException(String.Format("Column {0} cannot be modified = it is readonly", DbRow.getColumnNameFromIdx(this.DbColNameIdx)));
                else if (value != this._value) { this._value = value; modified = true; DbRow.Modified = true; }
            }
        }
        public void initValue(bool val) { if (this._value != val) { this._value = val; this.modified = true; } }
        public new bool isNullable { get { return false; } }
        public DbColBool(int colIdx, DbRow dbRow, SqlDataReader rdr, ColAttrib colAttrib)
            : base(dbRow, colIdx, colAttrib)
        {
            if (rdr != null) readData(rdr);
            else this._value = false;
        }

        public override void getData(SqlDataReader rdr, int ord)
        {
            try
            {
                this._value = rdr.GetBoolean(ord);
                this.modified = false;
            }
            catch (Exception exc)
            {
                throw new DbAccessException(string.Format("Error reading column '{0}' - {1}", DbRow.getColumnNameFromIdx(this.DbColNameIdx), exc.Message));
            }
        }
        public override object getValue() { return this.Value; }
        public override string ToString() { return this._value.ToString(); }
        public override string ToSqlStr() { return DbRow.getColumnNameFromIdx(this.DbColNameIdx) + " = " + (this.Value ? "1" : "0"); }
    }

    public class DbColBoolNull : DbColumn
    {
        bool? _value;
        public bool? Value
        {
            get { return _value; }
            set
            {
                if (this.isReadOnly) throw new DbAccessException(String.Format("Column {0} cannot be modified = it is readonly", DbRow.getColumnNameFromIdx(this.DbColNameIdx)));
                else if (value != this._value) { this._value = value; modified = true; DbRow.Modified = true; }
            }
        }
        public void initValue(bool? val) { if (this._value != val) { this._value = val; this.modified = true; } }
        public new bool isNullable { get { return true; } }
        public DbColBoolNull(int colIdx, DbRow dbRow, SqlDataReader rdr, ColAttrib colAttrib)
            : base(dbRow, colIdx, colAttrib)
        {
            if (rdr != null) readData(rdr);
            else this._value = null;
        }

        public override void getData(SqlDataReader rdr, int ord)
        {
            try
            {
                this._value = rdr.IsDBNull(ord) ? (bool?)null : rdr.GetBoolean(ord);
                this.modified = false;
            }
            catch (Exception exc)
            {
                throw new DbAccessException(string.Format("Error reading column '{0}' - {1}", DbRow.getColumnNameFromIdx(this.DbColNameIdx), exc.Message));
            }
        }
        public override object getValue() { return this.Value; }
        public override string ToString() { return (this.Value.HasValue) ? ((bool)this.Value).ToString() : null; }
        public override string ToSqlStr() { return DbRow.getColumnNameFromIdx(this.DbColNameIdx) + " = " + ((this.Value.HasValue) ? ((bool)this.Value ? "1" : "0") : "NULL"); }
    }

    //======================================
    // Guid (UniqueIdentifier) Data Columns
    // =====================================

    public class DbColGuid : DbColumn
    {
        Guid _value;
        public Guid Value
        {
            get { return _value; }
            set
            {
                if (this.isReadOnly) throw new DbAccessException(String.Format("Column {0} cannot be modified = it is readonly", DbRow.getColumnNameFromIdx(this.DbColNameIdx)));
                else if (value != this._value) { this._value = value; modified = true; DbRow.Modified = true; }
            }
        }
        public void initValue(Guid val) { if (this._value != val) { this._value = val; this.modified = true; } }
        public new bool isNullable { get { return false; } }
        public DbColGuid(int colIdx, DbRow dbRow, SqlDataReader rdr, ColAttrib colAttrib)
            : base(dbRow, colIdx, colAttrib)
        {
            if (rdr != null) readData(rdr);
            else this._value = Guid.Empty;
        }

        public override void getData(SqlDataReader rdr, int ord)
        {
            try
            {
                this._value = rdr.GetGuid(ord);
                this.modified = false;
            }
            catch (Exception exc)
            {
                throw new DbAccessException(string.Format("Error reading column '{0}' - {1}", DbRow.getColumnNameFromIdx(this.DbColNameIdx), exc.Message));
            }
        }
        public override object getValue() { return this.Value; }
        public override string ToString() { return this._value.ToString(); }
        public override string ToSqlStr() { return DbRow.getColumnNameFromIdx(this.DbColNameIdx) + " = '" + this.Value.ToString() + "'"; }
    }

    public class DbColGuidNull : DbColumn
    {
        Guid? _value;
        public Guid? Value
        {
            get { return _value; }
            set
            {
                if (this.isReadOnly) throw new DbAccessException(String.Format("Column {0} cannot be modified = it is readonly", DbRow.getColumnNameFromIdx(this.DbColNameIdx)));
                else if (value != this._value) { this._value = value; modified = true; DbRow.Modified = true; }
            }
        }
        public void initValue(Guid? val) { if (this._value != val) { this._value = val; this.modified = true; } }
        public new bool isNullable { get { return true; } }
        public DbColGuidNull(int colIdx, DbRow dbRow, SqlDataReader rdr, ColAttrib colAttrib)
            : base(dbRow, colIdx, colAttrib)
        {
            if (rdr != null) readData(rdr);
            else this._value = null;
        }

        public override void getData(SqlDataReader rdr, int ord)
        {
            try
            {
                this._value = rdr.IsDBNull(ord) ? (Guid?)null : rdr.GetGuid(ord);
                this.modified = false;
            }
            catch (Exception exc)
            {
                throw new DbAccessException(string.Format("Error reading column '{0}' - {1}", DbRow.getColumnNameFromIdx(this.DbColNameIdx), exc.Message));
            }
        }
        public override object getValue() { return this.Value; }
        public override string ToString() { return (this.Value.HasValue) ? ((Guid)this.Value).ToString() : null; }
        public override string ToSqlStr() { return DbRow.getColumnNameFromIdx(this.DbColNameIdx) + " = " + ((this.Value.HasValue) ? "NULL" : "'" + this.Value + "'"); }
    }

    //========================
    // Long Data Columns
    // ========================

    public class DbColLong : DbColumn
    {
        long _value;
        public long Value
        {
            get { return _value; }
            set
            {
                if (this.isReadOnly) throw new DbAccessException(String.Format("Column {0} cannot be modified = it is readonly", DbRow.getColumnNameFromIdx(this.DbColNameIdx)));
                else if (value != this._value) { this._value = value; modified = true; DbRow.Modified = true; }
            }
        }
        public void initValue(long val) { if (this._value != val) { this._value = val; this.modified = true; } }
        public new bool isNullable { get { return false; } }
        public DbColLong(int colIdx, DbRow dbRow, SqlDataReader rdr, ColAttrib colAttrib)
            : base(dbRow, colIdx, colAttrib)
        {
            if (rdr != null) readData(rdr);
            else this._value = 0;
        }

        public override void getData(SqlDataReader rdr, int ord)
        {
            try
            {
                this._value = rdr.GetInt64(ord);
                this.modified = false;
            }
            catch (Exception exc)
            {
                throw new DbAccessException(string.Format("Error reading column '{0}' - {1}", DbRow.getColumnNameFromIdx(this.DbColNameIdx), exc.Message));
            }
        }
        public override object getValue() { return this.Value; }
        public override string ToString() { return this._value.ToString(); }
        public override string ToSqlStr() { return DbRow.getColumnNameFromIdx(this.DbColNameIdx) + " = " + this.Value.ToString(); }
    }

    public class DbColLongNull : DbColumn
    {
        long? _value;
        public long? Value
        {
            get { return _value; }
            set
            {
                if (this.isReadOnly) throw new DbAccessException(String.Format("Column {0} cannot be modified = it is readonly", DbRow.getColumnNameFromIdx(this.DbColNameIdx)));
                else if (value != this._value) { this._value = value; modified = true; DbRow.Modified = true; }
            }
        }
        public void initValue(long? val) { if (this._value != val) { this._value = val; this.modified = true; } }
        public new bool isNullable { get { return true; } }
        public DbColLongNull(int colIdx, DbRow dbRow, SqlDataReader rdr, ColAttrib colAttrib)
            : base(dbRow, colIdx, colAttrib)
        {
            if (rdr != null) readData(rdr);
            else this._value = null;
        }

        public override void getData(SqlDataReader rdr, int ord)
        {
            try
            {
                this._value = rdr.IsDBNull(ord) ? (long?)null : rdr.GetInt64(ord);
                this.modified = false;
            }
            catch (Exception exc)
            {
                throw new DbAccessException(string.Format("Error reading column '{0}' - {1}", DbRow.getColumnNameFromIdx(this.DbColNameIdx), exc.Message));
            }
        }
        public override object getValue() { return this.Value; }
        public override string ToString() { return (this.Value.HasValue) ? ((long)this.Value).ToString() : null; }
        public override string ToSqlStr() { return DbRow.getColumnNameFromIdx(this.DbColNameIdx) + " = " + ((this.Value.HasValue) ? ((long)this.Value).ToString() : "NULL"); }
    }

    //========================
    // Double Data Columns
    // ========================

    public class DbColDouble : DbColumn
    {
        double _value;
        public double Value
        {
            get { return _value; }
            set
            {
                if (this.isReadOnly) throw new DbAccessException(String.Format("Column {0} cannot be modified = it is readonly", DbRow.getColumnNameFromIdx(this.DbColNameIdx)));
                else if (value != this._value) { this._value = value; modified = true; DbRow.Modified = true; }
            }
        }
        public void initValue(double val) { if (this._value != val) { this._value = val; this.modified = true; } }
        public new bool isNullable { get { return false; } }
        public DbColDouble(int colIdx, DbRow dbRow, SqlDataReader rdr, ColAttrib colAttrib)
            : base(dbRow, colIdx, colAttrib)
        {
            if (rdr != null) readData(rdr);
            else this._value = 0;
        }

        public override void getData(SqlDataReader rdr, int ord)
        {
            try
            {
                this._value = rdr.GetDouble(ord);
                this.modified = false;
            }
            catch (Exception exc)
            {
                throw new DbAccessException(string.Format("Error reading column '{0}' - {1}", DbRow.getColumnNameFromIdx(this.DbColNameIdx), exc.Message));
            }
        }
        public override object getValue() { return this.Value; }
        public override string ToString() { return this._value.ToString(); }
        public override string ToSqlStr() { return DbRow.getColumnNameFromIdx(this.DbColNameIdx) + " = " + this.Value.ToString(); }
    }

    public class DbColDoubleNull : DbColumn
    {
        double? _value;
        public double? Value
        {
            get { return _value; }
            set
            {
                if (this.isReadOnly) throw new DbAccessException(String.Format("Column {0} cannot be modified = it is readonly", DbRow.getColumnNameFromIdx(this.DbColNameIdx)));
                else if (value != this._value) { this._value = value; modified = true; DbRow.Modified = true; }
            }
        }
        public void initValue(double? val) { if (this._value != val) { this._value = val; this.modified = true; } }
        public new bool isNullable { get { return true; } }
        public DbColDoubleNull(int colIdx, DbRow dbRow, SqlDataReader rdr, ColAttrib colAttrib)
            : base(dbRow, colIdx, colAttrib)
        {
            if (rdr != null) readData(rdr);
            else this._value = null;
        }

        public override void getData(SqlDataReader rdr, int ord)
        {
            try
            {
                this._value = rdr.IsDBNull(ord) ? (double?)null : rdr.GetDouble(ord);
                this.modified = false;
            }
            catch (Exception exc)
            {
                throw new DbAccessException(string.Format("Error reading column '{0}' - {1}", DbRow.getColumnNameFromIdx(this.DbColNameIdx), exc.Message));
            }
        }
        public override object getValue() { return this.Value; }
        public override string ToString() { return (this.Value.HasValue) ? ((double)this.Value).ToString() : null; }
        public override string ToSqlStr() { return DbRow.getColumnNameFromIdx(this.DbColNameIdx) + " = " + ((this.Value.HasValue) ? ((double)this.Value).ToString() : "NULL"); }
    }

    //========================
    // Float Data Columns
    // ========================

    public class DbColFloat : DbColumn
    {
        float _value;
        public float Value
        {
            get { return _value; }
            set
            {
                if (this.isReadOnly) throw new DbAccessException(String.Format("Column {0} cannot be modified = it is readonly", DbRow.getColumnNameFromIdx(this.DbColNameIdx)));
                else if (value != this._value) { this._value = value; modified = true; DbRow.Modified = true; }
            }
        }
        public void initValue(float val) { if (this._value != val) { this._value = val; this.modified = true; } }
        public new bool isNullable { get { return false; } }
        public DbColFloat(int colIdx, DbRow dbRow, SqlDataReader rdr, ColAttrib colAttrib)
            : base(dbRow, colIdx, colAttrib)
        {
            if (rdr != null) readData(rdr);
            else this._value = 0;
        }

        public override void getData(SqlDataReader rdr, int ord)
        {
            try
            {
                this._value = rdr.GetFloat(ord);
                this.modified = false;
            }
            catch (Exception exc)
            {
                throw new DbAccessException(string.Format("Error reading column '{0}' - {1}", DbRow.getColumnNameFromIdx(this.DbColNameIdx), exc.Message));
            }
        }
        public override object getValue() { return this.Value; }
        public override string ToString() { return this._value.ToString(); }
        public override string ToSqlStr() { return DbRow.getColumnNameFromIdx(this.DbColNameIdx) + " = " + this.Value.ToString(); }
    }

    public class DbColFloatNull : DbColumn
    {
        float? _value;
        public float? Value
        {
            get { return _value; }
            set
            {
                if (this.isReadOnly) throw new DbAccessException(String.Format("Column {0} cannot be modified = it is readonly", DbRow.getColumnNameFromIdx(this.DbColNameIdx)));
                else if (value != this._value) { this._value = value; modified = true; DbRow.Modified = true; }
            }
        }
        public void initValue(float? val) { if (this._value != val) { this._value = val; this.modified = true; } }
        public new bool isNullable { get { return true; } }
        public DbColFloatNull(int colIdx, DbRow dbRow, SqlDataReader rdr, ColAttrib colAttrib)
            : base(dbRow, colIdx, colAttrib)
        {
            if (rdr != null) readData(rdr);
            else this._value = null;
        }

        public override void getData(SqlDataReader rdr, int ord)
        {
            try
            {
                this._value = rdr.IsDBNull(ord) ? (float?)null : rdr.GetFloat(ord);
                this.modified = false;
            }
            catch (Exception exc)
            {
                throw new DbAccessException(string.Format("Error reading column '{0}' - {1}", DbRow.getColumnNameFromIdx(this.DbColNameIdx), exc.Message));
            }
        }
        public override object getValue() { return this.Value; }
        public override string ToString() { return (this.Value.HasValue) ? ((float)this.Value).ToString() : null; }
        public override string ToSqlStr() { return DbRow.getColumnNameFromIdx(this.DbColNameIdx) + " = " + ((this.Value.HasValue) ? ((float)this.Value).ToString() : "NULL"); }
    }


    [Serializable]
    public class DbAccessException : Exception
    {
        public string MsgTitle { get; private set; }
        public DbAccessException() : base() { }
        public DbAccessException(string msg) : base(msg) { }
        public DbAccessException(string msg, string msgTitle) : base(msg) { MsgTitle = msgTitle; }
        public DbAccessException(string msg, Exception inner) : base(msg, inner) { }
        public DbAccessException(string msg, string msgTitle, Exception inner) : base(msg, inner) { MsgTitle = msgTitle; }
        protected DbAccessException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
