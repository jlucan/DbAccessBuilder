using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Xml;
using System.Data.SqlClient;

namespace DbAccessBuilder
{
    public partial class MainForm : Form
    {
        const int PHASE_1 = 1;
        const int PHASE_2 = 2;
        const int PHASE_3 = 3;
        int CurPhase = 0;
        public bool HaveErrors = false;
        string Phase1OutPutFile = null;
        string Phase2OutputFile = null;

        // Rules for the design of get stored procedures
        // 1. Foreign key ID's - include the ID from the base table, not the target table
        // Rules for design of insert procedures
        // 1. Passed parameters should include all non nullable fields in primary table and no nullable fields

        public MainForm()
        {
            InitializeComponent();
            this.rBtnPhase1.Checked = true;
        }


        private void MainForm_Load(object sender, EventArgs e)
        {
            this.Text += " Version " + ApplicationInfo.Version;
        }

        private void btnXmlBrowse_Click(object sender, EventArgs e)
        {
            switch (this.CurPhase)
            {
                case PHASE_1:
                case PHASE_2:
                    break;
                case PHASE_3:
                    break;
            }
            this.openFileDialog1.Filter = "Xml Files (.xml)|*.xml|All Files (*.*)|*.*";
            this.openFileDialog1.FilterIndex = 1;
            if (this.openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                this.txtXmlIn.Text = this.openFileDialog1.FileName;
            }
        }

        private void btnOutputBrowse_Click(object sender, EventArgs e)
        {
            switch (this.CurPhase)
            {
                case PHASE_1:
                case PHASE_2:
                    this.saveFileDialog1.Filter = "Xml Files (.xml)|*.xml";
                    break;
                case PHASE_3:
                    this.saveFileDialog1.Filter = "Code File (*.cs)|*.cs|Text File (*.txt)|*.txt";
                    break;
            }
            saveFileDialog1.FilterIndex = 1;
            saveFileDialog1.RestoreDirectory = true;
            this.saveFileDialog1.AddExtension = true;

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string outputFile = this.saveFileDialog1.FileName;
                this.txtOutputFile.Text = this.saveFileDialog1.FileName;
                this.txtOutputFile.Refresh();
            }
        }

        private string CapFirstLetter(string s)
        {
            return s.Substring(0, 1).ToUpper() + s.Substring(1);
        }

        // Convert a string to all caps
        private string ToAllCaps(string s)
        {
            return s.ToUpper();
        }

        private void createXmlStoredProcList()
        {
            // Get the SQL Server name and database and create a connection string
            string dbServer = this.txtSqlSvr.Text.Trim();
            if (String.IsNullOrEmpty(dbServer))
            {
                MessageBox.Show("A Sql Server Instance name has not been entered.", "Error");
                return;
            }
            string dataBaseName = this.txtDatabaseName.Text.Trim();
            if (String.IsNullOrEmpty(dbServer))
            {
                MessageBox.Show("A Sql Server database  name has not been entered.", "Error");
                return;
            }
            string sProcStartsWith = this.txtProcStartsWith.Text.Trim();
            if (String.IsNullOrEmpty(sProcStartsWith))
            {
                MessageBox.Show("A 'Starts With' string for stored procedures has not been defined.", "Error");
                return;
            }
            string connStr = String.Format(@"Data Source={0};Initial Catalog={1};Integrated Security=True", dbServer, dataBaseName);
            string cmdText = String.Format(@"SELECT (SCHEMA_NAME([schema_id]) + '.' + name) AS ProcName FROM sys.procedures WHERE name LIKE '{0}%'", sProcStartsWith);
            this.Phase1OutPutFile = this.txtOutputFile.Text;
            
            // Open sql reader to get proc names
            SqlConnection sqlConn = new SqlConnection(connStr);
            SqlCommand sqlCmd = new SqlCommand(cmdText, sqlConn);
            SqlDataReader sqlRdr;
            try
            {
                sqlConn.Open();
                sqlRdr = sqlCmd.ExecuteReader();
            }
            catch (Exception exc)
            {
                MessageBox.Show("Unable to get Xml Module List. Reason: " + exc.Message, "Error");
                return;
            }
            if (sqlRdr.HasRows)
            {
                // Create XmlWriter
                XmlWriterSettings xmlWriterSettings = new XmlWriterSettings();
                xmlWriterSettings.NewLineOnAttributes = false;
                xmlWriterSettings.Indent = true;

                XmlWriter xmlWriter = XmlWriter.Create(this.Phase1OutPutFile, xmlWriterSettings);

                xmlWriter.WriteStartElement("SqlInterfaceModules");
                xmlWriter.WriteAttributeString("DbServer", dbServer );
                xmlWriter.WriteAttributeString("DbName", dataBaseName);

                while (sqlRdr.Read())
                {
                    xmlWriter.WriteStartElement("SqlInterface");
                    xmlWriter.WriteAttributeString("GetRowsProc", sqlRdr["ProcName"].ToString());
                    xmlWriter.WriteAttributeString("PrimaryTableSchema", "TableSchema");
                    xmlWriter.WriteAttributeString("PrimaryTable", "TableName");
                    xmlWriter.WriteAttributeString("DbRowsClassName", "RowsClass");
                    xmlWriter.WriteAttributeString("DbRowClassName", "RowClass");
                    xmlWriter.WriteAttributeString("InsertRowProc", "InsertProc");
                    xmlWriter.WriteAttributeString("DeleteRowProc", "DeleteProc");
                    xmlWriter.WriteEndElement();
                }
                xmlWriter.WriteEndDocument();
                xmlWriter.Close();
            }
            sqlRdr.Close();
            sqlConn.Close();
            MessageBox.Show("An xml List of database access modules has been created based on stored procedures found. "
                                                    + "The list at: " + this.Phase1OutPutFile + " needs to be edited to supply missing values and to remove unnecessary rows.", "Success");
        }

        private void genXmlColumns()
        {
            string errMsg = null;
            this.warningMsgs.Text = null;
            this.errorMsgs.Text = null;

            //string connStr = String.Format(@"Data Source={0};Initial Catalog={1};Integrated Security=True", dbServer, dataBaseName);
            string xmlIn = this.txtXmlIn.Text;
            if (String.IsNullOrEmpty(xmlIn))
            {
                MessageBox.Show("The input file cannot be empty.", "File Selection Error");
                return;
            }
            string outputFile = this.txtOutputFile.Text;
            if (String.IsNullOrEmpty(outputFile))
            {
                MessageBox.Show("The output file cannot be empty.", "File Selection Error");
                return;
            }

            FileInfo inFile = new FileInfo(xmlIn);
            FileInfo outFile = new FileInfo(outputFile);
            if (inFile.FullName == outFile.FullName)
            {
                MessageBox.Show("The output file cannot be the same as the input file.", "File Selection Error");
                return;
            }

            // Create XmlWriter
            XmlWriterSettings xmlWriterSettings = new XmlWriterSettings();
            xmlWriterSettings.NewLineOnAttributes = false;
            xmlWriterSettings.Indent = true;

            if (inFile.Exists)
            {
                XmlWriter xmlWriter = XmlWriter.Create(outputFile, xmlWriterSettings);  // Create an XmlWriter
                using (StreamReader sr = inFile.OpenText())
                {
                    XmlDocument xmlInDoc = new XmlDocument();
                    try
                    {
                        xmlInDoc.Load(sr);
                    }
                    catch (Exception exc)
                    {
                        MessageBox.Show("Unable to open Xml input file. Reason: " + exc.Message, "Error");
                        return;
                    }
                    XmlElement rootElement = xmlInDoc.DocumentElement;
                    if (rootElement.Name != "SqlInterfaceModules")
                    {
                        MessageBox.Show("The Xml input file specified is not in the correct format.", "Input File Error");
                        return;
                    }

                    // Get database server and database - then create a connction string
                    string dbServer = rootElement.Attributes["DbServer"].Value;
                    string dbName = rootElement.Attributes["DbName"].Value;
                    // Write the root element, get SqlConnection object
                    xmlWriter.WriteStartElement("SqlInterfaceDefs");
                    xmlWriter.WriteAttributeString("DbServer", dbServer);
                    xmlWriter.WriteAttributeString("DbName", dbName);
                    string connStr = String.Format(@"Data Source={0};Initial Catalog={1};Integrated Security=True", dbServer, dbName);
                    SqlConnection sqlConn = new SqlConnection(connStr);
                    foreach (XmlNode interfaceNode in rootElement)  // Loop through interface module defs
                    {
                        if (interfaceNode.Name == "SqlInterface")
                        {
                            // Copy interface attributes from input file to output
                            string sqlProc = interfaceNode.Attributes["GetRowsProc"].Value;
                            string rowsClass = interfaceNode.Attributes["DbRowsClassName"].Value;
                            string rowClass = interfaceNode.Attributes["DbRowClassName"].Value;
                            string schema = interfaceNode.Attributes["PrimaryTableSchema"].Value;
                            string priTable = interfaceNode.Attributes["PrimaryTable"].Value;
                            string insProc = interfaceNode.Attributes["InsertRowProc"].Value;
                            string delProc = interfaceNode.Attributes["DeleteRowProc"].Value;
                            // Write the column definitions
                            if (!GenSqlColDefs.InsertColumnDefs(xmlWriter, sqlConn, sqlProc, rowsClass, rowClass, schema, priTable, insProc, delProc, this, out errMsg)) break;
                            xmlWriter.WriteEndElement();
                        }
                    }
                }
                xmlWriter.WriteEndDocument();
                xmlWriter.Close();
                if (errMsg == null)
                {
                    MessageBox.Show(String.Format("Xml column definitions completed {0}. The column definitions at: " + outputFile + " need to be checked for accuracy and redundancy. NOTE that data columns that are the result of a self join may need to be set to RO",
                                    String.IsNullOrEmpty(this.warningMsgs.Text) ? "successfully" : "with warnings"), "Success");
                }
                else
                {
                    MessageBox.Show("Xml column definitions did not complete successfuly - " + errMsg);
                }
            }
        }

        private void genSqlDbInterfaceCode()
        {
            string xmlIn = this.txtXmlIn.Text;
            if (String.IsNullOrEmpty(xmlIn))
            {
                MessageBox.Show("The input file cannot be empty.", "File Selection Error");
                return;
            }
            string outputFile = this.txtOutputFile.Text;
            if (String.IsNullOrEmpty(outputFile))
            {
                MessageBox.Show("The output file cannot be empty.", "File Selection Error");
                return;
            }
            // Create FileInfo's for input and output files
            FileInfo inFile = new FileInfo(xmlIn);
            FileInfo outFile = new FileInfo(outputFile);
            if (inFile.FullName == outFile.FullName)
            {
                MessageBox.Show("The output file cannot be the same as the input file.", "File Selection Error");
                return;
            }

            String outDir = outFile.DirectoryName;
            String outFilename = outFile.Name;
            if (outFilename.Equals("DbAccess.cs", StringComparison.InvariantCultureIgnoreCase))
            {
                MessageBox.Show("The output file name cannot be 'DbAccess.cs' - that name is reserved.", "Error Creating SQL Interface Code", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (inFile.Exists)
            {
                using (StreamReader sr = inFile.OpenText())
                {
                    using (StreamWriter sw = outFile.CreateText())
                    {
                        try
                        {
                            if (!SqlInterfaceBuilder.CreateSqlInterface(sr, sw, this)) return;
                        }
                        catch (Exception exc)
                        {
                            MessageBox.Show(exc.Message, "Error Creating SQL Interface Code", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("Could not open the file: " + this.txtXmlIn.Text, "Error Processing Xml Input file", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            string dbAccessCode = DbAccessBuilder.Properties.Resources.DbAccessCode;
            using (StreamWriter outfile = new StreamWriter(Path.Combine(outFile.DirectoryName, "DbAccess.cs")))
            {
                outfile.Write(dbAccessCode);
            }
            string msg;
            if (!String.IsNullOrEmpty(this.errorMsgs.Text))
            {
                msg = "Sql interface code generation did not completed successfully. Check Error Message box for errors.";
                MessageBox.Show(msg, "Error!");
            }
            else
            {
                msg = String.Format("Sql interface code generation completed successfully. Code files: '{0}' and 'DbAccess.cs' have been created in the directory: {1}.", outFilename, outDir);
                MessageBox.Show(msg, "Success!");
            }

        }

        private void btnDoPhase_Click(object sender, EventArgs e)
        {
            switch (this.CurPhase)
            {
                case PHASE_1:
                    createXmlStoredProcList();
                    break;
                case PHASE_2:
                    genXmlColumns();
                    break;
                case PHASE_3:
                    genSqlDbInterfaceCode();
                    break;
            }
        }

        private void rBtnPhase1_CheckedChanged(object sender, EventArgs e)
        {
            if (rBtnPhase1.Checked)
            {
                this.CurPhase = PHASE_1;
                this.txtXmlIn.Text = null;
                this.txtXmlIn.Enabled = this.btnXmlBrowse.Enabled = false;
                this.txtSqlSvr.Enabled = this.txtDatabaseName.Enabled = this.txtProcStartsWith.Enabled = true;
                this.btnDoPhase.Text = "Get Xml Stored Procedure list";
            }
            else
            {
                this.Phase1OutPutFile = this.txtOutputFile.Text;
            }
        }

        private void rBtnPhase2_CheckedChanged(object sender, EventArgs e)
        {
            if (rBtnPhase2.Checked)
            {
                this.CurPhase = PHASE_2;
                this.txtXmlIn.Enabled = this.btnXmlBrowse.Enabled = true;
                this.txtXmlIn.Text = this.Phase1OutPutFile;
                this.txtOutputFile.Text = null;
                this.txtSqlSvr.Enabled = this.txtDatabaseName.Enabled = this.txtProcStartsWith.Enabled = false;
                this.btnDoPhase.Text = "Gen Xml Column Definitions";
            }
            else
            {
                this.Phase2OutputFile = this.txtOutputFile.Text;
            }
        }

        private void rBtnPhase3_CheckedChanged(object sender, EventArgs e)
        {
            this.txtXmlIn.Enabled = this.btnXmlBrowse.Enabled = true;
            this.txtSqlSvr.Enabled = this.txtDatabaseName.Enabled = this.txtProcStartsWith.Enabled = false;
            this.btnDoPhase.Text = "Gen Database Interface Code";
            this.CurPhase = PHASE_3;
            this.txtXmlIn.Enabled = true;
            this.txtXmlIn.Text = this.Phase2OutputFile;
            this.txtOutputFile.Text = null;
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}
