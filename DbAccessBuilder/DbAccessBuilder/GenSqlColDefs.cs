using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Data;
using System.Data.SqlClient;

namespace DbAccessBuilder
{
    public static class GenSqlColDefs
    {
        public static Dictionary<string, ColumnInfo> GetDbColInfo(SqlConnection sqlConn, MainForm mainForm, out string errMsg)
        {
            errMsg = null;
            Dictionary<string, ColumnInfo> colInfo = new Dictionary<string,ColumnInfo>();
            StringBuilder cmd = new StringBuilder();
            cmd.Append("SELECT  SC.name + '.' + TB.name + '.' +  C.name AS ColName, C.is_computed, C.is_identity, C.is_nullable, C.max_length, C.system_type_id, C.user_type_id, ");
            cmd.Append("TP.name AS data_type, C.[precision], C.scale, ");
            cmd.Append("CASE WHEN KC.type = 'PK' THEN CONVERT(bit, 1) ELSE CONVERT(bit, 0) END AS is_pri_key, ");
            cmd.Append("CASE WHEN FK.type = 'F' THEN CONVERT(bit, 1) ELSE CONVERT(bit, 0) END AS is_for_key, ");
            cmd.Append("TBF.name AS for_key_table,  CF.name AS for_key_col, ");
            cmd.Append("CASE WHEN C.default_object_id > 0 THEN (SELECT definition FROM sys.default_constraints DC WHERE DC.object_id = C.default_object_id) ELSE '' END AS col_default\n");
            cmd.Append("FROM 	sys.Columns C INNER JOIN sys.tables TB ON C.object_id = TB.object_id ");
            cmd.Append("INNER JOIN sys.schemas SC ON TB.schema_id = SC.schema_id INNER JOIN sys.types TP ON C.user_type_id = TP.user_type_id\n");
            cmd.Append("LEFT JOIN ( sys.key_constraints KC JOIN sys.index_columns IC ON KC.unique_index_id = IC.index_id ) ");
            cmd.Append("ON C.column_id = IC.column_id AND C.object_id = KC.parent_object_id AND C.object_id = IC.Object_id AND KC.type = 'PK'\n");
            cmd.Append("LEFT JOIN ( sys.foreign_keys FK INNER JOIN sys.foreign_key_columns FKC ON  FK.object_id = FKC.constraint_object_id\n");
             cmd.Append("INNER JOIN sys.tables TBF ON FKC.referenced_object_id = TBF.object_id\n");
            cmd.Append("INNER JOIN sys.Columns CF ON FKC.referenced_object_id = CF.object_id AND FKC.referenced_column_id = CF.column_id ) ");
            cmd.Append("ON FKC.parent_object_id = C.object_id AND FKC.parent_column_id = C.column_id AND FK.type = 'F'\n");
            cmd.Append("WHERE (C.name IS NOT NULL) ORDER BY TB.Name, C.column_id;");

            SqlCommand ColInfoSqlCmd = new SqlCommand();
            ColInfoSqlCmd.Connection = sqlConn;
            ColInfoSqlCmd.CommandType = CommandType.Text;
            ColInfoSqlCmd.CommandText = cmd.ToString();
            SqlDataReader sqlRdr = null;
            string lastColName = null;
            try
            {
                sqlConn.Open();
                sqlRdr = ColInfoSqlCmd.ExecuteReader();
                if (sqlRdr.HasRows)
                {
                    while (sqlRdr.Read())
                    {
                        int ord;
                        ColumnInfo cInfo = new ColumnInfo();
                        lastColName = cInfo.ColName = sqlRdr.GetString(sqlRdr.GetOrdinal("ColName"));
                        cInfo.IsComputed = sqlRdr.GetBoolean(sqlRdr.GetOrdinal("is_computed"));
                        cInfo.IsIdentity = sqlRdr.GetBoolean(sqlRdr.GetOrdinal("is_identity"));
                        cInfo.IsNullable = sqlRdr.GetBoolean(sqlRdr.GetOrdinal("is_nullable"));
                        cInfo.DataType = sqlRdr.GetString(sqlRdr.GetOrdinal("data_type"));
                        cInfo.MaxLen = sqlRdr.GetInt16(sqlRdr.GetOrdinal("max_length"));
                        cInfo.Precision = (int)sqlRdr.GetByte(sqlRdr.GetOrdinal("precision"));
                        cInfo.Scale = (int)sqlRdr.GetByte(sqlRdr.GetOrdinal("scale"));
                        cInfo.IsPriKey = sqlRdr.GetBoolean(sqlRdr.GetOrdinal("is_pri_key"));
                        cInfo.IsForKey = sqlRdr.GetBoolean(sqlRdr.GetOrdinal("is_for_key"));
                        ord = sqlRdr.GetOrdinal("for_key_table");
                        if (!sqlRdr.IsDBNull(ord))  cInfo.ForKeyTable = sqlRdr.GetString(ord);
                        ord = sqlRdr.GetOrdinal("for_key_col");
                        if (!sqlRdr.IsDBNull(ord))  cInfo.ForKeyCol = sqlRdr.GetString(ord);
                        cInfo.ColDefault = sqlRdr.GetString(sqlRdr.GetOrdinal("col_default"));
                        if (colInfo.ContainsKey(cInfo.ColName))
                        {
                            colInfo.Add(cInfo.ColName + "_dup", cInfo);
                            mainForm.warningMsgs.AppendText(String.Format("Duplicate column found: {0}. Possible duplicate foreign key.\n", cInfo.ColName));
                        }
                        else
                        {
                            colInfo.Add(cInfo.ColName, cInfo);
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                errMsg = String.Format("Error getting column info from the database - {0} - near column name: {1}.", exc.Message, lastColName);
                return null;
            }
            finally
            {
                sqlRdr.Close();
                sqlConn.Close();
            }
            return colInfo;
        }


        public static bool InsertColumnDefs(XmlWriter xmlWriter, SqlConnection sqlConn, string sqlGetProc, string dbRowsClassName,
                            string dbRowClassName, string schema, string primaryTable, string sqlInsertProc, string sqlDeleteProc, MainForm mainForm, out string errMsg)
        {
            Dictionary<string, ColumnInfo> colInfoList = GetDbColInfo(sqlConn, mainForm, out errMsg);
            if (colInfoList == null) return false;

            SqlCommand sqlCmd = new SqlCommand();
            DataTable schemaTable;
            SqlDataReader sqlRdr;

            // if specified primary table name has a prefix - strip it, then prefix the specified schema name
            int pos = primaryTable.LastIndexOf(".");
            string primaryTableName = String.Format("{0}.{1}",  schema, (pos >= 0) ? primaryTable.Substring(pos + 1) : primaryTable); 

            // Check for existence of stored procedures
            bool gspExists = (!String.IsNullOrEmpty(sqlGetProc) && checkIfStoredProcExists(sqlConn, sqlGetProc));
            bool ispExists = (!String.IsNullOrEmpty(sqlInsertProc) && checkIfStoredProcExists(sqlConn, sqlInsertProc));
            bool dspExists = (!String.IsNullOrEmpty(sqlDeleteProc) && checkIfStoredProcExists(sqlConn, sqlDeleteProc));

            if (!gspExists) mainForm.warningMsgs.AppendText(String.Format("Stored Procedure: {0} does not exist.\n", sqlGetProc));
            if (!ispExists) mainForm.warningMsgs.AppendText(String.Format("Stored Procedure: {0} does not exist.\n", sqlInsertProc));
            if (!dspExists) mainForm.warningMsgs.AppendText(String.Format("Stored Procedure: {0} does not exist.\n", sqlDeleteProc));

            xmlWriter.WriteStartElement("SqlInterface");
            xmlWriter.WriteAttributeString("GetRowsProc", sqlGetProc);
            xmlWriter.WriteAttributeString("DbRowsClassName", dbRowsClassName);
            xmlWriter.WriteAttributeString("DbRowClassName", dbRowClassName);
            xmlWriter.WriteAttributeString("PrimaryTable", primaryTableName);
            xmlWriter.WriteAttributeString("InsertRowProc", sqlInsertProc );
            xmlWriter.WriteAttributeString("DeleteRowProc", sqlDeleteProc );
             xmlWriter.WriteAttributeString("GetRowsProcExists", gspExists ? "T" : "F");
             xmlWriter.WriteAttributeString("InsertRowProcExists", ispExists ? "T" : "F");
             xmlWriter.WriteAttributeString("DeleteRowProcExists", dspExists ? "T" : "F");

            sqlCmd.Connection = sqlConn;
            sqlConn.Open();

            if (ispExists)
            {
                // Get 'insert'  stored proc info and write dbcolumn xml elements
                sqlCmd.CommandText = sqlInsertProc;
                sqlCmd.CommandType = CommandType.StoredProcedure;
                SqlCommandBuilder.DeriveParameters(sqlCmd);
                foreach (SqlParameter p in sqlCmd.Parameters)
                {
                    if (p.Direction == ParameterDirection.Input || p.Direction == ParameterDirection.InputOutput)
                    {
                        xmlWriter.WriteStartElement("IParam");
                        xmlWriter.WriteAttributeString("ParamName", p.ParameterName);
                        xmlWriter.WriteAttributeString("ParamType", p.SqlDbType.ToString());
                        xmlWriter.WriteAttributeString("ParamLen", p.Size.ToString());
                        xmlWriter.WriteAttributeString("Direction", p.Direction.ToString());
                        xmlWriter.WriteAttributeString("Nullable", p.IsNullable ? "T" : "F");
                        xmlWriter.WriteAttributeString("DataColName", p.ParameterName.StartsWith("@") ? p.ParameterName.Substring(1) : String.Empty);
                        xmlWriter.WriteEndElement();
                    }
                }
            }

            if (dspExists)
            {
                // Get 'insert'  stored proc info and write dbcolumn xml elements
                sqlCmd.CommandText = sqlDeleteProc;
                sqlCmd.CommandType = CommandType.StoredProcedure;
                SqlCommandBuilder.DeriveParameters(sqlCmd);
                foreach (SqlParameter p in sqlCmd.Parameters)
                {
                    if (p.Direction == ParameterDirection.Input || p.Direction == ParameterDirection.InputOutput)
                    {
                        xmlWriter.WriteStartElement("DParam");
                        xmlWriter.WriteAttributeString("ParamName", p.ParameterName);
                        xmlWriter.WriteAttributeString("ParamType", p.SqlDbType.ToString());
                        xmlWriter.WriteAttributeString("ParamLen", p.Size.ToString());
                        xmlWriter.WriteAttributeString("Direction", p.Direction.ToString());
                        xmlWriter.WriteAttributeString("Nullable", p.IsNullable ? "T" : "F");
                        xmlWriter.WriteAttributeString("DataColName", p.ParameterName.StartsWith("@") ? p.ParameterName.Substring(1) : String.Empty);
                        xmlWriter.WriteEndElement();
                    }
                }
            }

            if (gspExists)
            {
                // Get 'get'  stored proc info and write dbcolumn xml elements
                sqlCmd.CommandText = sqlGetProc;
                sqlCmd.CommandType = CommandType.StoredProcedure;
                SqlCommandBuilder.DeriveParameters(sqlCmd);
                foreach (SqlParameter p in sqlCmd.Parameters)
                {
                    if (p.Direction == ParameterDirection.Input || p.Direction == ParameterDirection.InputOutput || p.Direction == ParameterDirection.Output)
                    {
                        xmlWriter.WriteStartElement("GParam");
                        xmlWriter.WriteAttributeString("ParamName", p.ParameterName);
                        xmlWriter.WriteAttributeString("ParamType", p.SqlDbType.ToString());
                        xmlWriter.WriteAttributeString("ParamLen", p.Size.ToString());
                        xmlWriter.WriteAttributeString("Direction", p.Direction.ToString());
                        xmlWriter.WriteAttributeString("Nullable", p.IsNullable ? "T" : "F");
                        xmlWriter.WriteAttributeString("DataColName", p.ParameterName.StartsWith("@") ? p.ParameterName.Substring(1) : String.Empty);
                        xmlWriter.WriteEndElement();
                    }
                }

                sqlRdr = sqlCmd.ExecuteReader(CommandBehavior.KeyInfo);
                schemaTable = sqlRdr.GetSchemaTable();
                List<string> colNames = new List<string>();
                foreach (DataRow field in schemaTable.Rows)     //For each field in the table...
                {
                    bool isPrKey = false;
                    bool isForKey = false;
                    bool isIdentity = false;
                    bool isJoined = false;
                    bool isHidden = false;
                    bool isReadOnly = false;
                    bool isAliased = false;
                    bool isExpression = false;
                    bool isNullable = true;
                    bool isComputed = false;
                    string schemaName = string.Empty;
                    string tableName = string.Empty;
                    string colName = string.Empty;
                    string colOrdinal = string.Empty;
                    string dataType = string.Empty;
                    string sqlDataType = string.Empty;
                    string len = string.Empty;
                    string nullableParam = string.Empty;
                    string colAttr = string.Empty;
                    string forKeyTable = string.Empty;
                    string forKeyCol = string.Empty;

                    string baseColName = string.Empty;
                    string baseServerName = string.Empty;

                    foreach (DataColumn property in schemaTable.Columns)  //For each property of the field...
                    {
                        switch (property.ColumnName)
                        {
                            case "ColumnName":
                                colName = field[property].ToString();
                                break;
                            case "ColumnOrdinal":
                                colOrdinal = field[property].ToString();
                                break;
                            case "IsKey":
                                isPrKey = (bool)field[property];
                                break;
                            case "IsIdentity":
                                isIdentity = (bool)field[property];
                                break;
                            case "IsHidden":
                                isHidden = (bool)field[property];
                                break;
                            case "IsAliased":
                                isAliased = (bool)field[property];
                                break;
                            case "IsExpression":
                                isExpression = (bool)field[property];
                                break;
                            case "IsReadOnly":
                                isReadOnly = (bool)field[property];
                                break;
                            case "BaseSchemaName":
                                schemaName = field[property].ToString();
                                break;
                            case "BaseTableName":
                                tableName = field[property].ToString();
                                break;
                            case "BaseColumnName":
                                baseColName = field[property].ToString();
                                break;
                            case "AllowDBNull":
                                nullableParam = (((bool)field[property]) ? "T" : "F");
                                break;
                            case "DataType":
                                dataType = field[property].ToString();
                                break;
                            case "DataTypeName":
                                sqlDataType = field[property].ToString();
                                break;
                            case "ColumnSize":
                                len = field[property].ToString();
                                break;
                            default:
                                //xmlWriter.WriteAttributeString("Unknown", field[property].ToString());
                                break;
                        }
                    }
                    // Add more column attributes if column is in the primary table and  we have entry for column in our column dictionary
                    string colTableName = String.Format("{0}.{1}", schemaName, tableName);
                    string fullColName = String.Format("{0}.{1}",colTableName, colName);
                    isJoined = (!String.IsNullOrEmpty(tableName) && !colTableName.Equals(primaryTableName, StringComparison.InvariantCultureIgnoreCase));
                    if (!isJoined && !isExpression && colInfoList.ContainsKey(fullColName))
                    {
                        ColumnInfo colInfo = colInfoList[fullColName];
                        isNullable = colInfo.IsNullable;
                        isComputed = colInfo.IsComputed;
                        isPrKey = colInfo.IsPriKey;
                        isIdentity = colInfo.IsIdentity;
                        if (colInfo.IsForKey)
                        {
                            isForKey = true;
                            forKeyTable = colInfo.ForKeyTable;
                            forKeyCol = colInfo.ForKeyCol;
                        }
                    }
                    if (isComputed || isAliased || isExpression || isJoined) isReadOnly = isNullable = true;     // Computed, aliased and joined columns are always read only and nullable

                    // Determine the column attribute
                    if (isPrKey && !isJoined)
                    {
                        colAttr = (isIdentity) ? "AI" : "PK";   // Primary key col is either Auto ID or regular primary key col
                    }
                    else if ((dataType == "System.DateTime") &&  colName.EndsWith("ModDate", StringComparison.InvariantCultureIgnoreCase) && !isReadOnly)
                    {
                        colAttr = "MD";
                    }
                    else if  ((dataType == "System.DateTime") && (colName.EndsWith("CreateDate", StringComparison.InvariantCultureIgnoreCase)
                                    || colName.EndsWith("EnterDate", StringComparison.InvariantCultureIgnoreCase)) && !isReadOnly)
                    {
                        colAttr = "CD";
                    }
                    else
                    {
                        colAttr = (isReadOnly) ? "RO" : "RW";
                    }

                    //bool nameExists = colNames.Exists(x => x == colName);
                    //if ( !(isPrKey && colNames.Exists(x => x == colName)))     // Eliminate foreign key columns and self referenced columns
                    //if (!(isPrKey && isJoined) && !(isPrKey && colNames.Exists(x => x == colName)))     // Eliminate foreign key columns and self referenced columns
                    if (!isHidden)
                    {
                        colNames.Add(colName);
                        xmlWriter.WriteStartElement("Column");
                        xmlWriter.WriteAttributeString("ColName", colName);
                        xmlWriter.WriteAttributeString("ColOrdinal", colOrdinal);
                        xmlWriter.WriteAttributeString("DataType", dataType);
                        xmlWriter.WriteAttributeString("SqlType", sqlDataType);
                        xmlWriter.WriteAttributeString("Len", len);
                        xmlWriter.WriteAttributeString("Nullable", isNullable ? "T" : "F");
                        xmlWriter.WriteAttributeString("IsPrKey", isPrKey ? "T" : "F");
                        xmlWriter.WriteAttributeString("ColAttrib", colAttr);
                        if (isForKey)
                        {
                            xmlWriter.WriteAttributeString("IsForKey", isForKey ? "T" : "F");
                            xmlWriter.WriteAttributeString("ForKeyTable", forKeyTable);
                            xmlWriter.WriteAttributeString("ForKeyCol", forKeyCol);
                        }
                        xmlWriter.WriteEndElement();
                    }
                }
                sqlRdr.Close();
            }
            sqlConn.Close();
            return true;
        }

        // Check for existence of stored procedures
        private static bool checkIfStoredProcExists(SqlConnection sqlConn, string spName)
        {
            int pos = spName.LastIndexOf(".");            // if Primary stored proc has a prefix - strip it
            spName = (pos >= 0) ? spName.Substring(pos + 1) : spName; 
            SqlCommand sqlCmd = new SqlCommand();
            sqlCmd.Connection = sqlConn;
            sqlCmd.CommandText = String.Format("SELECT 1 FROM sys.procedures WHERE type = 'P' AND name = '{0}'", spName);
            sqlCmd.CommandType = CommandType.Text;
            sqlConn.Open();
            Object ret = sqlCmd.ExecuteScalar();
            sqlConn.Close();
            return (ret != null);
        }
    }

    public class ColumnInfo
    {
        public string ColName { get; set; }
        public bool IsComputed { get; set; }
        public bool IsIdentity { get; set; }
        public bool IsNullable { get; set; }
        public string DataType { get; set; }
        public int MaxLen { get; set; }
        public int Precision { get; set; }
        public int Scale { get; set; }
        public bool IsPriKey { get; set; }
        public bool IsForKey { get; set; }
        public string ForKeyTable { get; set; }
        public string ForKeyCol { get; set; }
        public string ColDefault { get; set; }

    }
}
