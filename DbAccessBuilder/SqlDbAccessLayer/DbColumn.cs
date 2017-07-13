using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;

namespace SqlDbAccessLayer
{

    // * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * 
    //  Data Holders that hold one data item corresponding to on column in one database record
    //  The characteristics or each data holder are:
    //      1. The data type (the .net datatype that best fits datatype of the underlying database field)
    //      2. Nullable which should equal the  nullable property of the underlying database field
    //      3. Read Only or Read/Write which depends mostly on whether the underlying database field is 
    //          a non primary key field in the primary table as opposed to a primary key field or a field 
    //          in a joined table. A field might also be considered read only for other practical reasons.
    //  Data Holders that are Read/Write contain 'modified' flag and a 'ToSqlStr' method that facilitates
    //  writing modified data back to the database.
    // * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *

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
        public void initValue(int val) { this._value = val; }
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
        public void initValue(decimal val) { this._value = val; }
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
        public void initValue(string val) { this._value = val; }
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
        public void initValue(long val) { this._value = val; }
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
        public void initValue(double val) { this._value = val; }
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
        public void initValue(float val) { this._value = val; }
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
}
