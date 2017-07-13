using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Windows.Forms;

namespace DbAccessBuilder
{
    public static class SqlInterfaceBuilder
    {
        public static bool CreateSqlInterface(StreamReader sr, StreamWriter sw, MainForm mainForm)
        {
            XmlDocument xmlDoc = new XmlDocument();
            try
            {
                xmlDoc.Load(sr);
            }
            catch (Exception exc)
            {
                MessageBox.Show("Unable to open Xml input file. Reason: " + exc.Message, "Error");
                return false;
            }
            XmlElement rootElement = xmlDoc.DocumentElement;
            if (rootElement.Name != "SqlInterfaceDefs")
            {
                MessageBox.Show("The Xml input file specified is not in the correct format.", "Input File Error");
                return false;
            }

            // Write the header stuff
            sw.WriteLine("using System;");
            sw.WriteLine("using System.Collections;");
            sw.WriteLine("using System.Collections.Generic;");
            sw.WriteLine("using System.Linq;");
            sw.WriteLine("using System.Xml;");
            sw.WriteLine("using System.Data;");
            sw.WriteLine("using System.Data.SqlClient;");
            sw.WriteLine();
            sw.WriteLine("namespace SqlDbAccess");
            sw.WriteLine("{");

            // Generate code based on the xml input file
            foreach (XmlNode interfaceNode in rootElement)
            {
                if (interfaceNode.Name == "SqlInterface")
                {
                    string rowsClass = interfaceNode.Attributes["DbRowsClassName"].Value;
                    string rowClass = interfaceNode.Attributes["DbRowClassName"].Value;
                    string primaryTable = interfaceNode.Attributes["PrimaryTable"].Value;
                    string getRowsProc = interfaceNode.Attributes["GetRowsProc"].Value;
                    string insertRowProc = interfaceNode.Attributes["InsertRowProc"].Value;
                    string deleteRowProc = interfaceNode.Attributes["DeleteRowProc"].Value;
                    bool getRowsProcExists = (interfaceNode.Attributes["GetRowsProcExists"].Value == "T");
                    bool insertRowProcExists = (interfaceNode.Attributes["InsertRowProcExists"].Value == "T");
                    bool deleteRowProcExists = (interfaceNode.Attributes["DeleteRowProcExists"].Value == "T");
                    string nonIdenPrKeys = String.Empty;
                    string nonIdenPrKeysAndTypes = String.Empty;
                    string prKeys1 = String.Empty;
                    string prKeys2 = String.Empty;
                    string prKeysAndTypes1 = String.Empty;
                    string prKeysAndTypes2 = String.Empty;
                    string prkeysForFind = String.Empty;
                    string prKeysForExist = String.Empty;
                    int i = 0;

                    List<string> colNames = new List<string>();     // A handy list of column names
                    foreach (XmlNode node in interfaceNode.ChildNodes)
                    {
                        if (node.Name == "Column")
                        {
                            colNames.Add(node.Attributes["ColName"].Value);
                            string colAttr = node.Attributes["ColAttrib"].Value.ToUpper();
                            if ( colAttr == "AI" || colAttr == "PK")
                            {
                                string paramName = UnCapFirstLetter(node.Attributes["ColName"].Value);
                                string dotNetType = DotNetDataType(node.Attributes["SqlType"].Value, false) + " ";
                                prKeysForExist += ", \"" + node.Attributes["ColName"].Value + "\", " + paramName;
                                if (i++ > 0)
                                {
                                    prkeysForFind += " && ";
                                }
                                if (colAttr == "PK")
                                {
                                    nonIdenPrKeys += paramName + ", ";
                                    nonIdenPrKeysAndTypes += ", " + dotNetType + paramName;
                                }
                                prKeys1 += ", " + paramName;
                                prKeys2 += paramName + ", ";
                                prKeysAndTypes1 += ", " + dotNetType + paramName;
                                prKeysAndTypes2 += dotNetType + paramName + ", ";
                                prkeysForFind += String.Format("x.{0} == {1}", node.Attributes["ColName"].Value, paramName);
                            }
                        }
                    }

                    // Get stored proc input parameters
                    List<SqlProcParam> gProcParams = new List<SqlProcParam>();
                    foreach (XmlNode node in interfaceNode.ChildNodes)
                    {
                        if (node.Name == "GParam")
                        {
                            string pName = node.Attributes["ParamName"].Value;
                            string pType = node.Attributes["ParamType"].Value;
                            string pLen = node.Attributes["ParamLen"].Value;
                            string direction = node.Attributes["Direction"].Value;
                            bool nullable = node.Attributes["Nullable"].Value.ToUpper() == "T";
                            string colName = node.Attributes["DataColName"].Value;
                            gProcParams.Add(new SqlProcParam(pName, pType, pLen, direction, nullable, colName, colNames));
                        }
                    }
                    List<SqlProcParam> iProcParams = new List<SqlProcParam>();
                    foreach (XmlNode node in interfaceNode.ChildNodes)
                    {
                        if (node.Name == "IParam")
                        {
                            string pName = node.Attributes["ParamName"].Value;
                            string pType = node.Attributes["ParamType"].Value;
                            string pLen = node.Attributes["ParamLen"].Value;
                            string direction = node.Attributes["Direction"].Value;
                            bool nullable = node.Attributes["Nullable"].Value.ToUpper() == "T";
                            string colName = node.Attributes["DataColName"].Value;
                            SqlProcParam sqlProcParam = new SqlProcParam(pName, pType, pLen, direction, nullable, colName, colNames);
                            iProcParams.Add(sqlProcParam);
                            if (String.IsNullOrEmpty(sqlProcParam.ColName))
                                mainForm.errorMsgs.AppendText(String.Format("Stored Procedure: {0} - missing or bad data col name for parameter: {1} in IPARAMS. Edit XML file to correct.\n", insertRowProc, pName));
                        }
                    }
                    List<SqlProcParam> dProcParams = new List<SqlProcParam>();
                    foreach (XmlNode node in interfaceNode.ChildNodes)
                    {
                        if (node.Name == "DParam")
                        {
                            string pName = node.Attributes["ParamName"].Value;
                            string pType = node.Attributes["ParamType"].Value;
                            string pLen = node.Attributes["ParamLen"].Value;
                            string direction = node.Attributes["Direction"].Value;
                            bool nullable = node.Attributes["Nullable"].Value.ToUpper() == "T";
                            string colName = node.Attributes["DataColName"].Value;
                            SqlProcParam sqlProcParam = new SqlProcParam(pName, pType, pLen, direction, nullable, colName, colNames);
                            dProcParams.Add(sqlProcParam);
                            if (direction.Equals("Input", StringComparison.InvariantCultureIgnoreCase) && String.IsNullOrEmpty(sqlProcParam.ColName))
                                mainForm.errorMsgs.AppendText(String.Format("Stored Procedure: {0} - missing or bad data col name for parameter: {1} in DPARAMS. Edit XML file to correct.\n", deleteRowProc, pName));
                        }
                    }

                    // ===================================================
                    // Create a class that inherits from the DbRows class
                    //====================================================

                    sw.WriteLine();
                    sw.WriteLine("    //=============================================================================================");
                    sw.WriteLine("    // This class functions primarily as a container for a list of objects that inherit from DbRow.");
                    sw.WriteLine("    //=============================================================================================");
                    sw.WriteLine();
                    sw.WriteLine("    // NOTE: This is automatically generated code. If you change it, your changes will be lost if this");
                    sw.WriteLine("    // code is regenerated. A better approach is to add a partial class with the same name in a different");
                    sw.WriteLine("    // code module and add new properties or methods to it.");
                    sw.WriteLine();
                    sw.WriteLine("    public partial class " + rowsClass + " : DbRows, IEnumerable<" + rowClass + ">");
                    sw.WriteLine("    {");
                    sw.WriteLine("        protected static readonly string PrimaryTableName = \"" + primaryTable + "\";");
                    //foreach (XmlNode node in interfaceNode.ChildNodes)
                    //{
                    //    if (node.Name == "Column")
                    //    {
                    //        if (node.Attributes["ColAttrib"].Value == "AI" || node.Attributes["ColAttrib"].Value == "PK")
                    //        {
                    //            sw.WriteLine("        const string DELETE_PRKEY_" + node.Attributes["ColName"].Value.ToUpper() + " = \"@" + node.Attributes["ColName"].Value + "\";"); ;
                    //        }
                    //    }
                    //}
                    sw.WriteLine("        protected List<" + rowClass + "> DbRowList = null;");
                    sw.WriteLine();
                    sw.WriteLine("        /// <summary>");
                    sw.WriteLine("        ///  Class factory method that returns a new " + rowsClass + " object with its List of " + rowClass + "'s filled with data.");
                    sw.WriteLine("        ///  Returns null on error.");
                    sw.WriteLine("        /// </summary>");
                    sw.Write("        public static " + rowsClass + " Create_" + rowsClass + "(out string errMsg");
                    foreach (SqlProcParam spp in gProcParams)
                    {
                        sw.Write(String.Format(", {0} {1} = null", DotNetDataType(spp.SqlType, true), VarNameFromSqlProcParam(spp.Name)));
                    }
                    sw.WriteLine(")");
                    sw.WriteLine("        {");
                    sw.WriteLine("            " + rowsClass + " _dbrows = new " + rowsClass + "();");
                    sw.Write("            if (_dbrows.getData(out errMsg");
                    foreach (SqlProcParam spp in gProcParams)
                    {
                        sw.Write(String.Format(", {0}", VarNameFromSqlProcParam(spp.Name)));
                    }
                    sw.WriteLine(") == null) return null;");
                    sw.WriteLine("            return _dbrows;");
                    sw.WriteLine("        }");
                    sw.WriteLine();
                    sw.WriteLine("        /// <summary>");
                    sw.WriteLine("        /// Returns a generic list of database row objects as read from the database.");
                    sw.WriteLine("        /// </summary>");
                    sw.Write("        public List<" + rowClass + "> getData(out string errMsg");
                    foreach (SqlProcParam spp in gProcParams)
                    {
                        sw.Write(String.Format(", {0} {1} = null", DotNetDataType(spp.SqlType, true), VarNameFromSqlProcParam(spp.Name)));
                    }
                    sw.WriteLine(")");
                    sw.WriteLine("        {");
                    sw.WriteLine("            errMsg = null;");
                    sw.WriteLine("            if (this.DbRowList == null)");
                    sw.WriteLine("                this.DbRowList = new List<" + rowClass + ">();");
                    sw.WriteLine("            else");
                    sw.WriteLine("                this.DbRowList.Clear();");
                    sw.WriteLine("            SqlConnection sqlConn = DbAccess.GetSqlConn();");
                    sw.WriteLine("            if (sqlConn != null)");
                    sw.WriteLine("            {");
                    sw.WriteLine("                using (sqlConn)");
                    sw.WriteLine("                {");
                    sw.WriteLine("                    try");
                    sw.WriteLine("                    {");
                    sw.WriteLine("                        SqlCommand sqlCmd = new SqlCommand(\"" + getRowsProc + "\", sqlConn);");
                    sw.WriteLine("                        sqlCmd.CommandType = CommandType.StoredProcedure;");
                    foreach (SqlProcParam spp in gProcParams)
                    {
                        sw.WriteLine(String.Format("                        if ({0} != null) sqlCmd.Parameters.Add(\"{1}\", SqlDbType.{2}, {3}).Value = {0};", VarNameFromSqlProcParam(spp.Name), spp.Name, spp.SqlType, spp.Len));
                    }
                    sw.WriteLine("                        sqlConn.Open();");
                    sw.WriteLine("                        SqlDataReader sqlRdr = sqlCmd.ExecuteReader();");
                    sw.WriteLine("                        while (sqlRdr.Read())");
                    sw.WriteLine("                        {");
                    sw.WriteLine("                            DbRowList.Add(new " + rowClass + "(this, sqlRdr));");
                    sw.WriteLine("                        }");
                    sw.WriteLine("                        return this.DbRowList;");
                    sw.WriteLine("                    }");
                    sw.WriteLine("                    catch (DbAccessException dbaexc)");
                    sw.WriteLine("                    {");
                    sw.WriteLine("                        errMsg = dbaexc.Message;");
                    sw.WriteLine("                    }");
                    sw.WriteLine("                    catch (Exception exc)");
                    sw.WriteLine("                    {");
                    sw.WriteLine("                        errMsg = String.Format(\"Error reading data from {0}. Reason: {1}\", PrimaryTableName, exc.Message);");
                    sw.WriteLine("                    }");
                    sw.WriteLine("                }");
                    sw.WriteLine("            }");
                    sw.WriteLine("            else");
                    sw.WriteLine("            {");
                    sw.WriteLine("                errMsg = String.Format(\"Error accessing {0}. Reason:\\nUnable to get database connection object. Possibly, could not determine a valid database connection string,\", PrimaryTableName);");
                    sw.WriteLine("            }");
                    sw.WriteLine("            return null;");
                    sw.WriteLine("        }");
                    sw.WriteLine();
                    sw.WriteLine("        /// <summary>");
                    sw.WriteLine("        /// Returns a count of contained DbRow objects.");
                    sw.WriteLine("        /// </summary>");
                    sw.WriteLine("        public int Count { get { return this.DbRowList == null ? 0 : this.DbRowList.Count; } }");
                    sw.WriteLine();
                    sw.WriteLine("        /// <summary>");
                    sw.WriteLine("        /// Adds a new " + rowClass + " object to this class's contained list.");
                    sw.WriteLine("        /// </summary>");
                    sw.WriteLine("        public void Add" + rowClass + "(" + rowClass + " dbRow)");
                    sw.WriteLine("        {");
                    sw.WriteLine("            DbRowList.Add(dbRow);");
                    sw.WriteLine("        }");
                    sw.WriteLine();
                    sw.WriteLine("        /// <summary>");
                    sw.WriteLine("        /// Finds a Project in the this class's contained list.");
                    sw.WriteLine("        /// Returns null on error.");
                    sw.WriteLine("        /// </summary>");
                    sw.WriteLine("        public " + rowClass + " Find" + rowClass + "ByPrKey(out string errMsg" + prKeysAndTypes1 + ")");
                    sw.WriteLine("        {");
                    sw.WriteLine("            if (this.DbRowList == null)");
                    sw.WriteLine("            {");
                    sw.WriteLine("                errMsg = \"The " + rowClass + " list has not been initialized.\";");
                    sw.WriteLine("                return null;");
                    sw.WriteLine("            }");
                    sw.WriteLine("            " + rowClass + " _dbRow = this.DbRowList.Find(delegate(" + rowClass + " x) { return (" + prkeysForFind + "); });");
                    sw.WriteLine("            errMsg = (_dbRow == null) ? \"Find" + rowClass + "ByPrKey could not find a " + rowClass + " with the specified primary key.\" : null;");
                    sw.WriteLine("            return _dbRow;");
                    sw.WriteLine("        }");
                    sw.WriteLine();
                    sw.WriteLine("        /// <summary>");
                    sw.WriteLine("        /// Creates a new database row object with non-identity primary keys initialized with the  passed parameters and adds it to the list of database records this object contains.");
                    sw.WriteLine("        /// Row object can be modified and saved to the database using the updateDatabase method.");
                    sw.WriteLine("        /// </summary>");
                    sw.WriteLine("        public " + rowClass + " AddNew" + rowClass + "(out string errMsg" + nonIdenPrKeysAndTypes + ")");
                    sw.WriteLine("        {");
                    sw.WriteLine("            try");
                    sw.WriteLine("            {");
                    sw.WriteLine("                " + rowClass + " rowObj = new " + rowClass + "(" + (String.IsNullOrEmpty(nonIdenPrKeys) ? String.Empty : nonIdenPrKeys + "false") + ");");
                    sw.WriteLine("                if (this.DbRowList == null)");
                    sw.WriteLine("                    this.DbRowList = new List<" + rowClass + ">();");
                    sw.WriteLine("                this.DbRowList.Add(rowObj);");
                    sw.WriteLine("                errMsg = null;");
                    sw.WriteLine("                return rowObj;");
                    sw.WriteLine("            }");
                    sw.WriteLine("            catch (Exception exc)");
                    sw.WriteLine("            {");
                    sw.WriteLine("                errMsg = \"Error creating new " + rowClass + " row object - \" + exc.Message;");
                    sw.WriteLine("                return null;");
                    sw.WriteLine("            }");
                    sw.WriteLine("        }");
                    if (!String.IsNullOrEmpty(deleteRowProc))
                    {
                        sw.WriteLine();
                        sw.WriteLine("        /// <summary>");
                        sw.WriteLine("        /// Deletes a database record and removes it from the list of database records this object contains.");
                        sw.WriteLine("        /// Returns false on error.");
                        sw.WriteLine("        /// </summary>");
                        sw.WriteLine("        public bool RemoveAndDelete" + rowClass + "ByPrKey(out string errMsg" + prKeysAndTypes1 + ")");
                        sw.WriteLine("        {");
                        sw.WriteLine("            " + rowClass + " dbRow = this.Find" + rowClass + "ByPrKey(out errMsg" + prKeys1 + ");");
                        sw.WriteLine("            if (dbRow == null)");
                        sw.WriteLine("            {");
                        sw.WriteLine("                errMsg = \"Error removing " + rowClass + " - a " + rowClass + " with the specified primary key was not found in the contained " + rowsClass + " list.\";");
                        sw.WriteLine("                return false;");
                        sw.WriteLine("            }");
                        sw.WriteLine("            this.DbRowList.Remove(dbRow);");
                        sw.WriteLine("            return Delete" + rowClass + "ByPrKey(out errMsg" + prKeys1 + ");");
                        sw.WriteLine("        }");
                    }
                    sw.WriteLine();
                    sw.WriteLine("        /// <summary>");
                    sw.WriteLine("        /// Updates the database for each " + rowClass + " row that has been modified.");
                    sw.WriteLine("        /// Returns false on error.");
                    sw.WriteLine("        /// </summary>");
                    sw.WriteLine("        public bool updateDatabase(out string errMsg)");
                    sw.WriteLine("        {");
                    sw.WriteLine("            errMsg = null;");
                    sw.WriteLine("            if (this.DbRowList != null && this.DbRowList.Count > 0)");
                    sw.WriteLine("            {");
                    sw.WriteLine("                foreach (" + rowClass + " dbr in this.DbRowList)");
                    sw.WriteLine("                {");
                    sw.WriteLine("                    if (dbr.Modified)");
                    sw.WriteLine("                    {");
                    sw.WriteLine("                        if (!dbr.updateDatabase(out errMsg)) return false;");
                    sw.WriteLine("                    }");
                    sw.WriteLine("                }");
                    sw.WriteLine("            }");
                    sw.WriteLine("            return true;");
                    sw.WriteLine("        }");
                    if (!String.IsNullOrEmpty(deleteRowProc))
                    {
                        sw.WriteLine();
                        sw.WriteLine("        /// <summary>");
                        sw.WriteLine("        /// Static method that deletes a database record.");
                        sw.WriteLine("        /// Returns false on error.");
                        sw.WriteLine("        /// </summary>");
                        sw.WriteLine("        public static bool Delete" + rowClass + "ByPrKey(out string errMsg" + prKeysAndTypes1 + ")");
                        sw.WriteLine("        {");
                        sw.WriteLine("            errMsg = null;");
                        sw.WriteLine("            SqlConnection sqlConn = DbAccess.GetSqlConn();");
                        sw.WriteLine("            if (sqlConn != null)");
                        sw.WriteLine("            {");
                        sw.WriteLine("                using (sqlConn)");
                        sw.WriteLine("                {");
                        sw.WriteLine("                    try");
                        sw.WriteLine("                    {");
                        sw.WriteLine("                        SqlCommand sqlCmd = new SqlCommand(\"" + deleteRowProc + "\", sqlConn);");
                        sw.WriteLine("                        sqlCmd.CommandType = CommandType.StoredProcedure;");

                        foreach (XmlNode node in interfaceNode.ChildNodes)
                        {
                            if (node.Name == "Column")
                            {
                                if (node.Attributes["ColAttrib"].Value == "AI" || node.Attributes["ColAttrib"].Value == "PK")
                                {
                                    string sqlLen = node.Attributes["Len"].Value.Trim();
                                    if (sqlLen.Length > 0) sqlLen = ", " + sqlLen;
                                    sw.WriteLine("                        sqlCmd.Parameters.Add(\"@" + node.Attributes["ColName"].Value + "\", SqlDbType." + SqlDbType(node.Attributes["SqlType"].Value) + sqlLen + ").Value = " +  UnCapFirstLetter(node.Attributes["ColName"].Value + ";"));
                                }
                            }
                        }
                        sw.WriteLine("                        sqlConn.Open();");
                        sw.WriteLine("                        int nRows = sqlCmd.ExecuteNonQuery();");
                        sw.WriteLine("                    }");
                        sw.WriteLine("                    catch (DbAccessException dbaexc)");
                        sw.WriteLine("                    {");
                        sw.WriteLine("                        errMsg = dbaexc.Message;");
                        sw.WriteLine("                        return false;");
                        sw.WriteLine("                    }");
                        sw.WriteLine("                    catch (Exception exc)");
                        sw.WriteLine("                    {");
                        sw.WriteLine("                        errMsg = String.Format(\"Error deleting a row from {0}. Reason: {1}\", PrimaryTableName, exc.Message);");
                        sw.WriteLine("                        return false;");
                        sw.WriteLine("                    }");
                        sw.WriteLine("                }");
                        sw.WriteLine("            }");
                        sw.WriteLine("            else");
                        sw.WriteLine("            {");
                        sw.WriteLine("                errMsg = String.Format(\"Error accessing {0}. Reason:\\nUnable to get database connection object. Possibly, could not determine a valid database connection string,\", PrimaryTableName);");
                        sw.WriteLine("                return false;");
                        sw.WriteLine("            }");
                        sw.WriteLine("            return true;");
                        sw.WriteLine("        }");
                    }
                    sw.WriteLine();
                    sw.WriteLine("        /// <summary>");
                    sw.WriteLine("        /// Static method that checks for the existence of a database row by it's primary key.");
                    sw.WriteLine("        /// Returns false on error.");
                    sw.WriteLine("        /// </summary>");
                    sw.WriteLine("        public static bool Check" + rowClass + "Exists(out string errMsg, out bool exists" + prKeysAndTypes1 + ")");
                    sw.WriteLine("        {");
                    sw.WriteLine("            return CheckClientContactExists(out errMsg, out exists" + prKeysForExist + ");");
                    sw.WriteLine("        }");
                    sw.WriteLine();
                    sw.WriteLine("        /// <summary>");
                    sw.WriteLine("        /// Static method that checks for the existence of a database row by up to four columns and their contents .");
                    sw.WriteLine("        /// Returns false on error.");
                    sw.WriteLine("        /// </summary>");
                    sw.WriteLine("        public static bool CheckClientContactExists(out string errMsg, out bool exists, string col1, object val1, string col2 = null, object val2 = null, string col3 = null, object val3 = null, string col4 = null, object val4 = null)");
                    sw.WriteLine("        {");
                    sw.WriteLine("            return DbAccess.CheckRowExists(out errMsg, out exists, PrimaryTableName, col1, val1, col2, val2, col3, val3, col4, val4);");
                    sw.WriteLine("        }");
                    sw.WriteLine();
                    sw.WriteLine("        // Implement an indexer into the contained list");
                    sw.WriteLine("        public " + rowClass + " this[int index]");
                    sw.WriteLine("        {");
                    sw.WriteLine("            get { return this.DbRowList == null ? null : this.DbRowList[index]; }");
                    sw.WriteLine("        }");
                    sw.WriteLine();
                    sw.WriteLine("        // Implement the IEnumerable interface");
                    sw.WriteLine("        public IEnumerator<" + rowClass + "> GetEnumerator() { return this.DbRowList.GetEnumerator(); }");
                    sw.WriteLine("        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }");
                    sw.WriteLine("    }");
                    sw.WriteLine();

                    //==================================
                    // Create the DbRrow class
                    //==================================

                    sw.WriteLine("    //=========================================================================");
                    sw.WriteLine("    // This class represents one row from a result set generated by a stored procedure");
                    sw.WriteLine("    //=========================================================================");
                    sw.WriteLine();
                    sw.WriteLine("    public partial class " + rowClass + " : DbRow");
                    sw.WriteLine("    {");
                    sw.WriteLine();
                    sw.WriteLine("        // A list of fields and their attributes contained in the " + rowClass + " type");
                    foreach (XmlNode node in interfaceNode.ChildNodes)
                    {
                        if (node.Name == "Column")
                        {
                            bool isNullable = (node.Attributes["Nullable"].Value == "T");
                            sw.WriteLine("        // " + node.Attributes["ColName"].Value + ", " + DotNetDataType(node.Attributes["SqlType"].Value, isNullable)
                                                            + ", " + node.Attributes["Len"].Value + ", " + (isNullable ? "null" : "not null") + ", " + node.Attributes["ColAttrib"].Value);
                        }
                    }
                    sw.WriteLine();
                    sw.WriteLine("        /// <summary>");
                    sw.WriteLine("        /// Static factory method that creates a new database row object with non-identity primary keys initialized with the passed parameters.");
                    sw.WriteLine("        /// Non primary key columns that are not read-only and not nullable need to be set before saving to the database.");
                    sw.WriteLine("        /// The new row object can be modified and saved to the database using the object's updateDatabase method.");
                    sw.WriteLine("        /// </summary>");
                    sw.WriteLine("        public static " + rowClass + " CreateNew" + rowClass + "(out string errMsg" + nonIdenPrKeysAndTypes + ")");
                    sw.WriteLine("        {");
                    sw.WriteLine("            try");
                    sw.WriteLine("            {");
                    sw.WriteLine("                " + rowClass + " rowObj = new " + rowClass + "(" + (String.IsNullOrEmpty(nonIdenPrKeys) ? String.Empty : nonIdenPrKeys + "false") + ");");
                    sw.WriteLine("                errMsg = null;");
                    sw.WriteLine("                return rowObj;");
                    sw.WriteLine("            }");
                    sw.WriteLine("            catch (Exception exc)");
                    sw.WriteLine("            {");
                    sw.WriteLine("                errMsg = \"Error creating new " + rowClass + " row object - \" + exc.Message;");
                    sw.WriteLine("                return null;");
                    sw.WriteLine("            }");
                    sw.WriteLine("        }");
                    sw.WriteLine();
                    sw.WriteLine("        /// <summary>");
                    sw.WriteLine("        /// Static method that returns the database row object that contains the passed primary key.");
                    sw.WriteLine("        /// Returns null on error.");
                    sw.WriteLine("        /// </summary>");
                    sw.WriteLine("        public static " + rowClass + " Get" + rowClass + "ByPrimaryKey(out string errMsg" + prKeysAndTypes1 + ")");
                    sw.WriteLine("        {");
                    sw.WriteLine("            errMsg = null;");
                    sw.WriteLine("            try");
                    sw.WriteLine("            {");
                    sw.WriteLine("                return new " + rowClass + "(" + prKeys2 + "true);");
                    sw.WriteLine("            }");
                    sw.WriteLine("            catch (DbAccessException dbaexc)");
                    sw.WriteLine("            {");
                    sw.WriteLine("                errMsg = dbaexc.Message;");
                    sw.WriteLine("                return null;");
                    sw.WriteLine("            }");
                    sw.WriteLine("            catch (Exception exc)");
                    sw.WriteLine("            {");
                    sw.WriteLine("                errMsg = String.Format(\"Error getting a data row object from {0}. Reason: {1}\", PrimaryTableName, exc.Message);");
                    sw.WriteLine("                return null;");
                    sw.WriteLine("            }");
                    sw.WriteLine("        }");
                    sw.WriteLine();
                    sw.WriteLine("        // Static and constant definitions");
                    sw.WriteLine("        protected static readonly string PrimaryTableName = \"" + primaryTable + "\";");
                    //foreach (XmlNode node in interfaceNode.ChildNodes)
                    //{
                    //    if (node.Name == "Column")
                    //    {

                    //        if (node.Attributes["ColAttrib"].Value == "AI" || node.Attributes["ColAttrib"].Value == "PK")
                    //        {
                    //            sw.WriteLine("        const string PARAM_PRKEY_" + node.Attributes["ColName"].Value.ToUpper() + " = \"@" + node.Attributes["ColName"].Value + "\";"); ;
                    //        }
                    //    }
                    //}
                    //foreach (XmlNode node in interfaceNode.ChildNodes)
                    //{
                    //    if (node.Name == "Column")
                    //    {
                        
                    //        string colAttr = node.Attributes["ColAttrib"].Value.ToUpper();
                    //        if ((colAttr == "AI" || colAttr == "PK" || node.Attributes["Nullable"].Value == "F") && (colAttr != "MD" && colAttr !=  "CD" && colAttr != "JC"))
                    //        {
                    //            sw.WriteLine("        const string INSERT_PARAM_" + node.Attributes["ColName"].Value.ToUpper() + " = \"@" + node.Attributes["ColName"].Value + "\";");
                    //        }
                    //    }
                    //}
                    sw.WriteLine();
                    sw.WriteLine("        // This is a collection of static definitions that provide a more descriptive name to the ordinal value assigned to a column");
                    sw.WriteLine("        // NOTE - CHANGING THESE VALUES COULD HAVE DISASTEROUS CONSEQUENCES");
                    i = 0;
                    foreach (XmlNode node in interfaceNode.ChildNodes)
                    {
                        if (node.Name == "Column")
                        {
                            sw.WriteLine("        public static readonly int ColIdx_" + node.Attributes["ColName"].Value + " = " + (i++).ToString() + ";");             // = (int)ColNames." + node.Attributes["ColName"].Value + ";");
                        }
                    }
                    sw.WriteLine();
                    sw.WriteLine("        // Defines a table of column attributes of a row from the result set used to create this object");
                    sw.WriteLine("        public static ColumnAttributes[] ColAttributes =");
                    sw.WriteLine("        {");
                    i = 0;
                    foreach (XmlNode node in interfaceNode.ChildNodes)
                    {
                        if (node.Name == "Column")
                        {
                            if (i > 0) sw.WriteLine(",");
                            string sqlLen = node.Attributes["Len"].Value;
                            int tempLen;
                            if (String.IsNullOrEmpty(sqlLen) || !int.TryParse(sqlLen, out tempLen)) sqlLen = "null";
                            sw.Write("            new ColumnAttributes(" + i.ToString() + ", \"" + node.Attributes["ColName"].Value + "\", SqlDbType." + SqlDbType(node.Attributes["SqlType"].Value) + ", " + sqlLen + ")");
                            i++;
                        }
                    }
                    sw.WriteLine();
                    sw.WriteLine("        };");
                    sw.WriteLine();
                    sw.WriteLine("        // Properties or Methods that return various row or column attributes");
                    sw.WriteLine("        protected override string PrimaryDatabaseTable { get { return PrimaryTableName; } }");
                    sw.WriteLine();
                    sw.WriteLine("        public override string getColumnNameFromIdx(int colIdx)");
                    sw.WriteLine("        {");
                    sw.WriteLine("            return ColAttributes[colIdx].ColName;");
                    sw.WriteLine("        }");
                    sw.WriteLine();
                    sw.WriteLine("        public override SqlDbType getColumnSqlTypeFromIdx(int colIdx)");
                    sw.WriteLine("        {");
                    sw.WriteLine("            return ColAttributes[colIdx].SqlType;");
                    sw.WriteLine("        }");
                    sw.WriteLine();
                    sw.WriteLine("        public override int? getColumnSqlLenFromIdx(int colIdx)");
                    sw.WriteLine("        {");
                    sw.WriteLine("            return ColAttributes[colIdx].SqlLen;");
                    sw.WriteLine("        }");
                    sw.WriteLine();
                    sw.WriteLine("        //=================================");
                    sw.WriteLine("        // Constructors");
                    sw.WriteLine("        //=================================");
                    sw.WriteLine();
                    sw.WriteLine("        // This constructor reads a row from the database if it is passed an sql reader reference, or a new blank row otherwise.");
                    sw.WriteLine("        // The first parameter is the index of a column name in as defined above.  It also serves as the index of the column.");
                    sw.WriteLine("        // * THE NUMBER OF AND ORDER OF COLUMNS IN \"datafields\" MUST EXACTLY MATCH THE NUMBER AND ORDER OF THE DATABASE COLUMN ENUMERATION ABOVE *");
                    sw.WriteLine("        public " + rowClass + "(DbRows dbRows, SqlDataReader sqlRdr)");
                    sw.WriteLine("            : base((sqlRdr == null))        // Sets 'IsNewRow' flag to true if no sqlRdr (no data read from database)");
                    sw.WriteLine("        {");
                    sw.WriteLine("            this.DbRows = dbRows;");
                    sw.WriteLine("            this.dataFields = new DbColumn[]");
                    sw.WriteLine("            {");
                    i = 0;
                    foreach (XmlNode node in interfaceNode.ChildNodes)
                    {
                        if (node.Name == "Column")
                        {
                            if (i++ > 0) sw.WriteLine(",");
                            sw.Write("                new DbCol" + CapFirstLetter(DotNetDataType(node.Attributes["SqlType"].Value, false)));
                            if (node.Attributes["Nullable"].Value == "T") sw.Write("Null");
                            sw.Write("(ColIdx_" + node.Attributes["ColName"].Value + ", this, sqlRdr, DbColumn.ColAttrib." + GetLongColAttrib(node.Attributes["ColAttrib"].Value) + ")");
                        }
                    }
                    sw.WriteLine();
                    sw.WriteLine("            };");
                    sw.WriteLine("        }");
                    sw.WriteLine();
                    sw.WriteLine("        // Creates a new blank row that is not attached to a DbRows collection");
                    sw.WriteLine("        public " + rowClass + "()");
                    sw.WriteLine("            : this(null, null)");
                    sw.WriteLine("        {");
                    sw.WriteLine("            this.IsNewRow = true;");
                    sw.WriteLine("        }");
                    sw.WriteLine();
                    if (!String.IsNullOrEmpty(prKeysAndTypes2))
                    {
                        sw.WriteLine("        // Creates a new row that is not attached to a DbRows container");
                        sw.WriteLine("        // IF the 'getRowData param is true, the row is then filled with data using the passed primary key value");
                        sw.WriteLine("        public " + rowClass + "(" + prKeysAndTypes2 + "bool getRowData)");
                        sw.WriteLine("            : this(null, null)");
                        sw.WriteLine("        {");
                        // Add rows that will init each primary key value
                        foreach (XmlNode node in interfaceNode.ChildNodes)
                        {
                            if (node.Name == "Column")
                            {
                                if ((node.Attributes["ColAttrib"].Value.ToUpper().Substring(0, 2) == "AI") || (node.Attributes["ColAttrib"].Value.ToUpper().Substring(0, 2) == "PK"))
                                {
                                    sw.WriteLine("            ((DbCol" + CapFirstLetter(DotNetDataType(node.Attributes["SqlType"].Value, false)) + ")dataFields[ColIdx_" + node.Attributes["ColName"].Value + "]).initValue(" + UnCapFirstLetter(node.Attributes["ColName"].Value) + ");");
                                }
                            }
                        }
                        sw.WriteLine("            if (getRowData)");
                        sw.WriteLine("            {");
                        sw.WriteLine("                this.IsNewRow = false;");
                        sw.WriteLine("                string errMsg;");
                        sw.WriteLine("                if (!this.refreshData(out errMsg))       // Read data from database");
                        sw.WriteLine("                    throw new DbAccessException(errMsg);");
                        sw.WriteLine("            }");
                        sw.WriteLine("        }");
                    }
                    sw.WriteLine();
                    sw.WriteLine("        //=================================");
                    sw.WriteLine("        // Data field properties");
                    sw.WriteLine("        //=================================");
                    sw.WriteLine();
                    foreach (XmlNode node in interfaceNode.ChildNodes)
                    {
                        if (node.Name == "Column")
                        {
                            sw.Write("        public " + DotNetDataType(node.Attributes["SqlType"].Value, (node.Attributes["Nullable"].Value == "T")));
                            sw.Write(" " + node.Attributes["ColName"].Value + " { get { return ((DbCol" + CapFirstLetter(DotNetDataType(node.Attributes["SqlType"].Value, false)));
                            if (node.Attributes["Nullable"].Value.ToUpper() == "T") sw.Write("Null");
                            sw.Write(")dataFields[ColIdx_" + node.Attributes["ColName"].Value + "]).Value; }");
                            if (node.Attributes["ColAttrib"].Value == "RW")
                            {
                                sw.Write(" set { ((DbCol" + CapFirstLetter(DotNetDataType(node.Attributes["SqlType"].Value, false)));
                                if (node.Attributes["Nullable"].Value.ToUpper() == "T") sw.Write("Null");
                                sw.Write(")dataFields[ColIdx_" + node.Attributes["ColName"].Value + "]).Value = value; }");
                            }
                            sw.WriteLine(" }");
                        }
                    }
                    sw.WriteLine();
                    sw.WriteLine("        // Provide for special cases where a read only field needs to be initialized");
                    sw.WriteLine("        public void initColValue(string colName, object value)");
                    sw.WriteLine("        {");
                    sw.WriteLine("            switch (colName)");
                    sw.WriteLine("            {");
                    foreach (XmlNode node in interfaceNode.ChildNodes)
                    {
                        if (node.Name == "Column" && node.Attributes["ColAttrib"].Value == "RO")
                        {
                            sw.WriteLine("                case \"" + node.Attributes["ColName"].Value + "\":");
                            sw.Write("                    ((DbCol" + CapFirstLetter(DotNetDataType(node.Attributes["SqlType"].Value, false)) +  ((node.Attributes["Nullable"].Value.ToUpper() == "T") ? "Null" : "") + ")");
                            sw.WriteLine("dataFields[ColIdx_" + node.Attributes["ColName"].Value + "]).initValue((" + DotNetDataType(node.Attributes["SqlType"].Value, (node.Attributes["Nullable"].Value.ToUpper() =="T")) + ")value);");
                            sw.WriteLine("                    break;");
                        }
                    }
                    sw.WriteLine("            }");
                    sw.WriteLine("        }");
                    sw.WriteLine();
                    sw.WriteLine("        //=================================");
                    sw.WriteLine("        // Insert a new database row");
                    sw.WriteLine("        //=================================");
                    sw.WriteLine();
                    sw.WriteLine("        // The insertNewRow method is called by the updateDatabase method in DbRow which is a base class to this class.");
                    sw.WriteLine("        // It only inserts non nullable columns when creating a new record. Nullable columns are then updated with an update operation.");
                    sw.WriteLine("        protected override bool insertNewRow(out string errMsg)");
                    sw.WriteLine("        {");
                    if (insertRowProcExists == false)
                    {
                    sw.WriteLine("        errMsg = \"System Error - There is either not an insert row procedure specified or it has been specified incorrectly. Please correct and regenerate this code.\";");
                    sw.WriteLine("        return false;");
                    sw.WriteLine();
                    }
                    sw.WriteLine("            // Make sure that this is intended to be a new row and that there is the required content in primary key columns");
                    sw.WriteLine("            if (!this.IsNewRow)");
                    sw.WriteLine("            {");
                    sw.WriteLine("                errMsg = \"System Error - attempt to insert a new database row from an object that does not represent a new database row.\";");
                    sw.WriteLine("                return false;");
                    sw.WriteLine("            }");
                    // Count 'AI' columns
                    i = 0;
                    foreach (XmlNode node in interfaceNode.ChildNodes)
                    {
                        if (node.Name == "Column")
                        {

                            if ((node.Attributes["ColAttrib"].Value.ToUpper().Substring(0, 2) == "AI")) i++;
                        }
                    }
                    if (i > 0)
                    {

                        sw.Write("            if (");
                        i = 0;
                        foreach (XmlNode node in interfaceNode.ChildNodes)
                        {
                            if (node.Name == "Column")
                            {

                                if ((node.Attributes["ColAttrib"].Value.ToUpper().Substring(0, 2) == "AI"))
                                {
                                    if (i++ > 0)
                                    {
                                        sw.WriteLine("");
                                        sw.Write("                || ");
                                    }
                                    sw.Write("this." + node.Attributes["ColName"].Value + " != null");
                                }
                            }
                        }
                        sw.WriteLine(")");
                        sw.WriteLine("            {");
                        sw.WriteLine("                errMsg = String.Format(\"Cannot insert new {0} row - an auto ID column already contains a non-zero value.\", PrimaryTableName);");
                        sw.WriteLine("                return false;");
                        sw.WriteLine("            }");
                        sw.WriteLine("");
                    }

                    // Count 'PK' columns
                    i = 0;
                    foreach (XmlNode node in interfaceNode.ChildNodes)
                    {
                        if (node.Name == "Column")
                        {
                            if ((node.Attributes["ColAttrib"].Value == "PK")) i++;
                        }
                    }
                    if (i > 0)
                    {

                        sw.Write("            if (");
                        i = 0;
                        foreach (XmlNode node in interfaceNode.ChildNodes)
                        {
                            if (node.Name == "Column")
                            {
                                if ((node.Attributes["ColAttrib"].Value == "PK"))
                                {
                                    if (i++ > 0)
                                    {
                                        sw.WriteLine("");
                                        sw.Write("                || ");
                                    }
                                    sw.Write("!this.dataFields[ColIdx_" + node.Attributes["ColName"].Value + "].modified");
                                }
                            }
                        }
                        sw.WriteLine(")");
                        sw.WriteLine("            {");
                        sw.WriteLine("                errMsg = String.Format(\"Cannot insert new {0} row - one or more primary key columns has not been set.\", PrimaryTableName);");
                        sw.WriteLine("                return false;");
                        sw.WriteLine("            }");
                    }
                    // Count string, Guid, or DateTime 'RW' columns that are not nullable
                    i = 0;
                    foreach (XmlNode node in interfaceNode.ChildNodes)
                    {
                        if (node.Name == "Column")
                        {
                            string dataType = DotNetDataType(node.Attributes["SqlType"].Value, node.Attributes["Nullable"].Value == "T");
                            if ((node.Attributes["ColAttrib"].Value == "RW" && node.Attributes["Nullable"].Value == "F") && (dataType == "string" || dataType == "Guid" || dataType == "DateTime")) i++;
                        }
                    }
                    if (i > 0)
                    {
                        sw.Write("            if (");
                        i = 0;
                        foreach (XmlNode node in interfaceNode.ChildNodes)
                        {
                            if (node.Name == "Column")
                            {
                                string dataType = DotNetDataType(node.Attributes["SqlType"].Value, node.Attributes["Nullable"].Value == "T");
                                if ((node.Attributes["ColAttrib"].Value == "RW")  && (node.Attributes["Nullable"].Value == "F") && (dataType == "string" || dataType == "Guid"   || dataType == "DateTime"))
                                {
                                    if (i++ > 0)
                                    {
                                        sw.WriteLine("");
                                        sw.Write("                || ");
                                    }
                                    sw.Write("!this.dataFields[ColIdx_" + node.Attributes["ColName"].Value + "].modified");
                                }
                            }
                        }
                        sw.WriteLine(")");
                        sw.WriteLine("            {");
                        sw.WriteLine("                errMsg = String.Format(\"Cannot insert new {0} row - one or more required columns has not been set.\", PrimaryTableName);");
                        sw.WriteLine("                return false;");
                        sw.WriteLine("            }");
                    }

                    sw.WriteLine("            SqlCommand sqlCmd = new SqlCommand(\"" + insertRowProc + "\");");
                    sw.WriteLine("            sqlCmd.CommandType = CommandType.StoredProcedure;");


                    foreach (SqlProcParam spp in iProcParams)
                    {
                        if (spp.IsOutput)
                        {
                            sw.WriteLine(String.Format("            sqlCmd.Parameters.Add(\"{0}\", SqlDbType.{1}, {2}).Direction = ParameterDirection.Output;",  spp.Name, spp.SqlType, spp.Len));
                        }
                        else
                        {
                            sw.WriteLine(String.Format("            sqlCmd.Parameters.Add(\"{1}\", SqlDbType.{2}, {3}).Value = (object){0} ?? DBNull.Value;", spp.ColName, spp.Name, spp.SqlType, spp.Len));
                        }
                    }
                    sw.WriteLine("            if (!execInsertCmd(sqlCmd, out errMsg)) return false;");
                    foreach (SqlProcParam spp in iProcParams)
                    {
                        if (spp.IsOutput)
                        {
                            sw.WriteLine("            ((DbCol" + CapFirstLetter(DotNetDataType(spp.SqlType, false) + ")dataFields[ColIdx_" + spp.ColName + "]).initValue((int)sqlCmd.Parameters[\"" + spp.Name + "\"].Value);  // Set the primary key with ID returned from stored procedure")); 
                        }
                    }
                    sw.WriteLine();
                    sw.WriteLine("            // Reset modified flags for columns already written to database");
                    foreach (SqlProcParam spp in iProcParams)
                    {
                        if (!spp.IsOutput)
                        {
                            sw.WriteLine("            this.dataFields[ColIdx_" + spp.ColName + "].resetModified();");
                        }
                    }

                    //foreach (XmlNode node in interfaceNode.ChildNodes)
                    //{
                    //    if (node.Name == "Column")
                    //    {
                    //        if (node.Attributes["ColAttrib"].Value == "AI" || node.Attributes["ColAttrib"].Value == "PK" || (node.Attributes["Nullable"].Value == "F" && node.Attributes["ColAttrib"].Value == "RW"))
                    //        {
                    //            sw.WriteLine("            this.dataFields[ColIdx_" + node.Attributes["ColName"].Value + "].resetModified();");
                    //        }
                    //    }
                    //}
                    sw.WriteLine("            errMsg = null;");
                    sw.WriteLine("            return true;");
                    sw.WriteLine("        }");
                    sw.WriteLine();
                    sw.WriteLine("        //======================================");
                    sw.WriteLine("        // Refresh row data from the database");
                    sw.WriteLine("        //======================================");
                    sw.WriteLine();
                    sw.WriteLine("        // The RefreshData method fills this record with current data from the database");
                    sw.WriteLine("        // It uses the same stored procedure as that used to read all records except that the primary key for the record is supplied.");
                    sw.WriteLine("        // The stored procedure is written so that when a primary key is not supplied all active records are returned");
                    sw.WriteLine("        public bool refreshData(out string errMsg)");
                    sw.WriteLine("        {");
                    sw.WriteLine("            if (this.IsNewRow)");
                    sw.WriteLine("            {");
                    sw.WriteLine("                errMsg = \"System error - the refresh data method cannot be called on a new row that has not yet been commited to the database.\";");
                    sw.WriteLine("                return false;");
                    sw.WriteLine("            }");
                    sw.WriteLine("            SqlCommand sqlCmd = new SqlCommand(\"" + getRowsProc + "\");");
                    sw.WriteLine("            sqlCmd.CommandType = CommandType.StoredProcedure;");

                    foreach (XmlNode node in interfaceNode.ChildNodes)
                    {
                        if (node.Name == "Column")
                        {
                            if (node.Attributes["ColAttrib"].Value == "AI" || node.Attributes["ColAttrib"].Value == "PK")
                            {
                                string sqlLen = node.Attributes["Len"].Value.Trim();
                                if (sqlLen.Length > 0) sqlLen = ", " + sqlLen;
                                sw.WriteLine("            sqlCmd.Parameters.Add(\"@" + node.Attributes["ColName"].Value + "\", SqlDbType." + SqlDbType(node.Attributes["SqlType"].Value) + sqlLen + ").Value = this." + node.Attributes["ColName"].Value + ";");
                            }
                        }
                    }
                    sw.WriteLine("            try");
                    sw.WriteLine("            {");
                    sw.WriteLine("                refreshRowsData(sqlCmd);");
                    sw.WriteLine("            }");
                    sw.WriteLine("            catch (DbAccessException dbaexc)");
                    sw.WriteLine("            {");
                    sw.WriteLine("                errMsg = \"Error refreshing " + rowsClass + " record from database. Reason:\\n\" + dbaexc.Message;");
                    sw.WriteLine("                return false;");
                    sw.WriteLine("            }");
                    sw.WriteLine("            errMsg = null;");
                    sw.WriteLine("            return true;");
                    sw.WriteLine("        }");
                    sw.WriteLine("    }");
                }
            }
            sw.WriteLine("}        // End of namspace");
            return true;
        }

        private static string CapFirstLetter(string s)
        {
            return s.Substring(0, 1).ToUpper() + s.Substring(1);
        }

        private static string UnCapFirstLetter(string s)
        {
            return s.Substring(0, 1).ToLower() + s.Substring(1);
        }

        // Convert a string to all caps
        private static string ToAllCaps(string s)
        {
            return s.ToUpper();
        }

        private static string VarNameFromSqlProcParam(string paramName)
        {
            string varName = paramName;
            if (varName.StartsWith("@")) varName = varName.Substring(1);
            return UnCapFirstLetter(varName);
        }

        // Convert a two char column attribute to long column type definitions
        private static string GetLongColAttrib(string attrib)
        {
            if (attrib.ToUpper().Substring(0, 2) == "AI")
            {
                return "AutoID";
            }
            else if (attrib.ToUpper().Substring(0, 2) == "PK")
            {
                return "PrKey";
            }
            else if (attrib.ToUpper().Substring(0, 2) == "RW")
            {
                return "ReadWrite";
            }
            else if (attrib.ToUpper().Substring(0, 2) == "RO")
            {
                return "ReadOnly";
            }
            else if (attrib.ToUpper().Substring(0, 2) == "JC")
            {
                return "ReadOnly";
            }
            else if (attrib.ToUpper().Substring(0, 2) == "MD")
            {
                return "ModDate";
            }
            else if (attrib.ToUpper().Substring(0, 2) == "CD")
            {
                return "CreateDate";
            }
            else return "*Column Atttribute Error*";
        }

        // Returns a .Net datatype that corresponds to the SQL Data type
        private static string DotNetDataType(string sqlDataType, bool isNullable)
        {
            string sqlType = sqlDataType.ToUpper();
            if ((sqlType == "BIGINT") && !isNullable) return "long";
            if ((sqlType == "BIGINT") && isNullable) return "long?";
            else if ((sqlType == "BIT") && !isNullable) return "bool";
            else if ((sqlType == "BIT") && isNullable) return "bool?";
            else if ((sqlType == "CHAR") && !isNullable) return "string";
            else if ((sqlType == "CHAR") && isNullable) return "string";
            else if ((sqlType == "DATETIME") && !isNullable) return "DateTime";
            else if ((sqlType == "DATETIME") && isNullable) return "DateTime?";
            else if ((sqlType == "DATE") && !isNullable) return "DateTime";
            else if ((sqlType == "DATE") && isNullable) return "DateTime?";
            else if ((sqlType == "DECIMAL") && !isNullable) return "decimal";
            else if ((sqlType == "DECIMAL") && isNullable) return "decimal?";
            else if ((sqlType == "FLOAT") && !isNullable) return "float";
            else if ((sqlType == "FLOAT") && isNullable) return "float?";
            else if ((sqlType == "INT") && !isNullable) return "int";
            else if ((sqlType == "INT") && isNullable) return "int?";
            else if ((sqlType == "MONEY") && !isNullable) return "decimal";
            else if ((sqlType == "MONEY") && isNullable) return "decimal?";
            else if ((sqlType == "NCHAR") && !isNullable) return "string";
            else if ((sqlType == "NCHAR") && isNullable) return "string";
            else if ((sqlType == "NTEXT") && !isNullable) return "string";
            else if ((sqlType == "NTEXT") && isNullable) return "string";
            else if ((sqlType == "NUMERIC") && !isNullable) return "decimal";
            else if ((sqlType == "NUMERIC") && isNullable) return "decimal?";
            else if ((sqlType == "NVARCHAR") && !isNullable) return "string";
            else if ((sqlType == "NVARCHAR") && isNullable) return "string";
            else if ((sqlType == "REAL") && !isNullable) return "float";
            else if ((sqlType == "REAL") && isNullable) return "float?";
            else if ((sqlType == "SMALLDATETIME") && !isNullable) return "DateTime";
            else if ((sqlType == "SMALLDATETIME") && isNullable) return "DateTime?";
            else if ((sqlType == "SMALLINT") && !isNullable) return "short";
            else if ((sqlType == "SMALLINT") && isNullable) return "short?";
            else if ((sqlType == "SMALLMONEY") && !isNullable) return "decimal";
            else if ((sqlType == "SMALLMONEY") && isNullable) return "decimal?";
            else if ((sqlType == "TEXT") && !isNullable) return "string";
            else if ((sqlType == "TEXT") && isNullable) return "string";
            else if ((sqlType == "TIMESTAMP") && !isNullable) return "byte[]";
            else if ((sqlType == "TIMESTAMP") && isNullable) return "byte[]?";
            else if ((sqlType == "TINYINT") && !isNullable) return "byte";
            else if ((sqlType == "TINYINT") && isNullable) return "byte?";
            else if ((sqlType == "UNIQUEIDENTIFIER") && !isNullable) return "Guid";
            else if ((sqlType == "UNIQUEIDENTIFIER") && isNullable) return "Guid?";
            else if ((sqlType == "SMALLDATETIME") && !isNullable) return "DateTime";
            else if ((sqlType == "SMALLDATETIME") && isNullable) return "DateTime?";
            else if ((sqlType == "VARCHAR") && !isNullable) return "string";
            else if ((sqlType == "VARCHAR") && isNullable) return "string";
            else return "TypeIsError";
        }

        // Returns a Sql datatype in correct case for SQL Parameter Defs
        private static string SqlDbType(string sqlDataType)
        {
            string sqlType = sqlDataType.ToUpper();
            if (sqlType == "BIGINT") return "BigInt";
            else if (sqlType == "BIT") return "Bit";
            else if (sqlType == "CHAR") return "Char";
            else if (sqlType == "DATETIME") return "DateTime";
            else if (sqlType == "DATE") return "Date";
            else if (sqlType == "DECIMAL") return "Decimal";
            else if (sqlType == "FLOAT") return "Float";
            else if (sqlType == "INT") return "Int";
            else if (sqlType == "MONEY") return "Money";
            else if (sqlType == "NCHAR") return "NChar";
            else if (sqlType == "NTEXT") return "NText";
            else if (sqlType == "NVARCHAR") return "NVarChar";
            else if (sqlType == "REAL") return "Real";
            else if (sqlType == "SMALLDATETIME") return "SmallDateTime";
            else if (sqlType == "SMALLINT") return "SmallInt";
            else if (sqlType == "SMALLMONEY") return "SmallMoney";
            else if (sqlType == "TEXT") return "Text";
            else if (sqlType == "TIMESTAMP") return "Timestamp";
            else if (sqlType == "TINYINT") return "TinyInt";
            else if (sqlType == "UNIQUEIDENTIFIER") return "UniqueIdentifier";
            else if (sqlType == "UNIQUEIDENTIFIER") return "UniqueIdentifier";
            else if (sqlType == "SMALLDATETIME") return "SmallDateTime";
            else if (sqlType == "VARCHAR") return "VarChar";
            else return "SqlTypeError";
        }
    }

    // Helper class that stores SqlCommand parameter attributes
    public class SqlProcParam
    {
        public string Name { get; private set; }
        public string SqlType { get; private set; }
        public string Len { get; private set; }
        public bool IsOutput { get; private set; }
        public bool Nullable { get; private set; }
        public string ColName { get; private set; }

        public SqlProcParam(string name, string sqlType, string len, string direction, bool nullable, string colName, List<string> colNames)
        {
            this.Name = name;
            this.SqlType = sqlType;
            this.Len = len;
            this.IsOutput = (direction == "InputOutput" || direction == "Output");
            this.Nullable = nullable;
            this.ColName = colNames.Find(s => s.Equals(colName, StringComparison.InvariantCultureIgnoreCase));
        }
    }

}
