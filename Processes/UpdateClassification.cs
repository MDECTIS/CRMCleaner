using CRMCleaner.Classes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CRMCleaner.Processes
{
    class UpdateClassification
    {
        internal void Start()
        {
            DataTable dtUpdateClassification = getDTStruc();
            Dictionary<string, string> dicNULL = new Dictionary<string, string>();
            ArrayList AccountIDList = getAllAccountIDList();
            foreach (string strAccountID in AccountIDList)
            {
                DataRow dr = dtUpdateClassification.NewRow();
                Guid AccountID = new Guid(strAccountID);
                using (SqlConnection Connection = SQLHelper.GetConnection())
                {
                    SqlTransaction Transaction = default(SqlTransaction);
                    Transaction = Connection.BeginTransaction("WizardSync");

                    string CompanyName = GetAccountDetailsNameByAccountID(Connection, Transaction, AccountID);
                    dr[0] = CompanyName;
                    BOL.AccountContact.odsAccount mgr = new BOL.AccountContact.odsAccount();
                    //Update JV Category
                    mgr.UpdateAccountJVCategory_Wizard(Connection, Transaction, AccountID, new Guid(SyncHelper.AdminID), SyncHelper.AdminName);
                    string JVCategory = getJVCategory(Connection, Transaction, AccountID, ref dicNULL);
                    dr[1] = JVCategory;
                    //Update BumiClassification
                    mgr.UpdateAccountBumiClassification_Wizard(Connection, Transaction, AccountID, new Guid(SyncHelper.AdminID), SyncHelper.AdminName);
                    string BumiClass = getBumiClass(Connection, Transaction, AccountID, ref dicNULL);
                    dr[2] = BumiClass;
                    //Update Classification
                    mgr.UpdateAccountClassification_Wizard(Connection, Transaction, AccountID, new Guid(SyncHelper.AdminID), SyncHelper.AdminName);
                    string AccountClass = getAccountClass(Connection, Transaction, AccountID, ref dicNULL);
                    dr[3] = AccountClass;
                    Transaction.Commit();
                    dr[4] = "UPDATED";
                    dtUpdateClassification.Rows.Add(dr);

                }
            }
            //2016 - 03 - 22 00:00:00 AM
            string folderPath = ConfigurationSettings.AppSettings["CRMCleanUpExcelLoc"].ToString();
            ExcelFileHelper.GenerateExcelFile(folderPath,dtUpdateClassification, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss tt"), "UpdateAccountClassification");
            if (dicNULL.Keys.Count > 0)
            {
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"C:\Users\User\Desktop\NULClassification.txt"))
                {
                    foreach (string key in dicNULL.Keys)
                    {
                        string Category = "";
                        if (dicNULL.ContainsKey(key))
                        {
                            Category=dicNULL[key].ToString();
                        }
                        file.WriteLine(key + ": " + Category);
                    }
                    System.Diagnostics.Process.Start("notepad.exe", @"C:\Users\User\Desktop\NULClassification.txt");
                }
            }

        }

        private string getAccountClass(SqlConnection Connection, SqlTransaction Transaction, Guid AccountID, ref Dictionary<string, string> dtNULL)
        {
            string ID = "";
            string Class = "";
            using (SqlCommand com = new SqlCommand("", Connection))
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter(com))
                {

                    StringBuilder sql = new StringBuilder();
                    sql.AppendLine("SELECT AccountGroupTypeCID");
                    sql.AppendLine("FROM Account");
                    sql.AppendLine("WHERE AccountID = @AccountID");
                    com.CommandText = sql.ToString();
                    com.CommandTimeout = int.MaxValue;
                    com.Transaction = Transaction;
                    com.Parameters.Add(new SqlParameter("@AccountID", AccountID));

                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    if (dt != null && dt.Rows.Count > 0)
                    {
                        ID = dt.Rows[0][0].ToString();
                        if (ID == "")
                        {
                            dtNULL.Add(AccountID.ToString(), "AccountGroupType");
                        }
                        else
                            Class = GetCodeMasterName(Connection, Transaction, ID, "AccountGroupType");
                    }
                }
            }
            return Class;
        }

        private string getBumiClass(SqlConnection Connection, SqlTransaction Transaction, Guid AccountID, ref Dictionary<string, string> dtNULL)
        {
            string ID = "";
            string Class = "";
            using (SqlCommand com = new SqlCommand("", Connection))
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter(com))
                {
                    StringBuilder sql = new StringBuilder();
                    sql.AppendLine("SELECT BumiClassificationCID");
                    sql.AppendLine("FROM Account");
                    sql.AppendLine("WHERE AccountID = @AccountID");
                    com.CommandText = sql.ToString();
                    com.CommandTimeout = int.MaxValue;
                    com.Transaction = Transaction;
                    com.Parameters.Add(new SqlParameter("@AccountID", AccountID));

                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    if (dt != null && dt.Rows.Count > 0)
                    {
                        ID = dt.Rows[0][0].ToString();
                        if (ID == "")
                        {
                            dtNULL.Add(AccountID.ToString(), "BUMI_CLASSIFICATION");
                        }
                        else
                            Class = GetCodeMasterName(Connection, Transaction, ID, "BUMI_CLASSIFICATION");
                    }
                }
            }
            return Class;
        }

        private string getJVCategory(SqlConnection Connection, SqlTransaction Transaction, Guid AccountID, ref Dictionary<string, string> dtNULL)
        {
            string ID = "";
            string Class = "";
            using (SqlCommand com = new SqlCommand("", Connection))
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter(com))
                {
                    StringBuilder sql = new StringBuilder();
                    sql.AppendLine("SELECT JVCategoryCID");
                    sql.AppendLine("FROM Account");
                    sql.AppendLine("WHERE AccountID = @AccountID");
                    com.CommandText = sql.ToString();
                    com.CommandTimeout = int.MaxValue;
                    com.Transaction = Transaction;
                    com.Parameters.Add(new SqlParameter("@AccountID", AccountID));

                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    if (dt != null && dt.Rows.Count > 0)
                    {
                        ID = dt.Rows[0][0].ToString();
                        if (ID == "")
                        {
                            dtNULL.Add(AccountID.ToString(), "JV_CATEGORY");
                        }
                        else
                            Class = GetCodeMasterName(Connection, Transaction, ID, "JV_CATEGORY");
                    }
                }
            }
            return Class;
        }
        public static string GetCodeMasterName(SqlConnection Connection, SqlTransaction Transaction, string CodeMasterID, string CodeType)
        {
            string Name = "";
            using (SqlCommand com = new SqlCommand("", Connection))
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter(com))
                {

                    System.Text.StringBuilder sql = new System.Text.StringBuilder();
                    sql.AppendLine("SELECT CodeName");
                    sql.AppendLine("FROM CodeMaster");
                    sql.AppendLine("WHERE CodeMasterID = @CodeMasterID");
                    sql.AppendLine("AND CodeType = @CodeType");
                    com.CommandText = sql.ToString();
                    com.CommandTimeout = int.MaxValue;
                    com.Transaction = Transaction;

                    try
                    {
                        com.Parameters.Add(new SqlParameter("@CodeType", CodeType));
                        com.Parameters.Add(new SqlParameter("@CodeMasterID", CodeMasterID));

                        DataTable dt = new DataTable();
                        adapter.Fill(dt);

                        if (dt.Rows.Count > 0)
                        {
                            return dt.Rows[0][0].ToString();
                        }

                    }
                    catch (Exception ex)
                    {
                        throw;
                    }
                    finally
                    {
                    }
                }
            }
            return Name;
        }
        public static string GetAccountDetailsNameByAccountID(SqlConnection Connection, SqlTransaction Transaction, Guid AccountID)
        {
            SqlCommand com = new SqlCommand();
            SqlDataAdapter ad = new SqlDataAdapter(com);

            System.Text.StringBuilder sql = new System.Text.StringBuilder();
            sql.AppendLine("SELECT AccountName FROM Account WHERE AccountID = @AccountID");

            com.CommandText = sql.ToString();
            com.CommandType = CommandType.Text;
            com.Connection = Connection;
            com.Transaction = Transaction;
            com.CommandTimeout = int.MaxValue;

            //con.Open()
            try
            {
                com.Parameters.Add(new SqlParameter("@AccountID", AccountID));

                DataTable dt = new DataTable();
                ad.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    return dt.Rows[0][0].ToString();
                }
                else {
                    return null;
                }
            }
            catch (Exception ex)
            {
                throw;
                //Finally
                //	con.Close()
            }
        }
        private DataTable getDTStruc()
        {
            DataTable output = new DataTable();
            output.Columns.Add("CompanyName", typeof(string));
            output.Columns.Add("JVClassification", typeof(string));
            output.Columns.Add("BumiClassification", typeof(string));
            output.Columns.Add("AccountClassification", typeof(string));
            output.Columns.Add("Status", typeof(string));
            return output;
        }
        private ArrayList getAllAccountIDList()
        {
            ArrayList arrAccountIDList = new ArrayList();
            arrAccountIDList.Add("C23BCA68-B259-4429-A92E-03C95E120546");
            arrAccountIDList.Add("4C764FDA-2F38-4C8F-A90B-13B5804C3904");
            arrAccountIDList.Add("FD32D0BF-B7D7-4974-BCCA-21C644F70ED6");
            arrAccountIDList.Add("90AC33AB-42DE-42A6-B678-21DAEFF4FA58");
            arrAccountIDList.Add("3145B4D8-DA03-457B-8EE9-361BCEC702DC");
            arrAccountIDList.Add("F9BB0213-0091-4739-A5B5-376C8F743B9E");
            arrAccountIDList.Add("EAA89221-6B79-4F09-BF03-3AD78786EBA2");
            arrAccountIDList.Add("93B3D85D-7959-4D79-8313-40C16180D433");
            arrAccountIDList.Add("47FB58B7-72D7-4FF9-9D25-41938A922B0A");
            arrAccountIDList.Add("A0E49677-D53D-4524-90EE-43561E492472");
            arrAccountIDList.Add("64546263-B7A4-4047-A59D-4500A613BB39");
            arrAccountIDList.Add("CA843E04-9E07-464B-ADFD-46859FD14D10");
            arrAccountIDList.Add("66630E29-5B38-4519-91E6-4C0022D11413");
            arrAccountIDList.Add("03FF186A-24AE-49E6-B512-4C6E0A78DC95");
            arrAccountIDList.Add("3987CF71-DBDC-4836-9A92-5328FD30B627");
            arrAccountIDList.Add("4081A3B2-B22F-4668-9925-64CE43927DCF");
            arrAccountIDList.Add("DEBF29DF-3608-4DA2-A570-68EACC12B373");
            arrAccountIDList.Add("F34FB9D8-8FCA-4D52-988A-695B377AB0CD");
            arrAccountIDList.Add("B3B3FBB7-E483-4D97-88FD-6AA193EDDD52");
            arrAccountIDList.Add("A58E254F-7278-4D52-8965-6F76912C7EA9");
            arrAccountIDList.Add("FF99C527-53BA-497F-949E-7B0F029F0001");
            arrAccountIDList.Add("F4E038A5-2381-467B-A3CB-7BF85314DC96");
            arrAccountIDList.Add("9E202AF3-9544-4D90-977E-8244D3514FFF");
            arrAccountIDList.Add("7C607C30-29E6-4108-9EA6-916149C8398A");
            arrAccountIDList.Add("EC3B50D9-4D15-4574-ABA2-97C467C3F0E5");
            arrAccountIDList.Add("90792AD8-67CB-4578-8DDF-B0A016557B0B");
            arrAccountIDList.Add("167D0DE7-D973-45B8-8598-BBA58C7EAD66");
            arrAccountIDList.Add("67F4DC64-163A-4299-818D-BD70FCA97DDF");
            arrAccountIDList.Add("FD35A419-280C-4A6E-982E-C27D6A9EEAD5");
            arrAccountIDList.Add("5AA00D01-E596-4651-8431-C31068AF7070");
            arrAccountIDList.Add("50CD6A91-E17C-4B61-BDAD-C52EA8E67832");
            arrAccountIDList.Add("066FD347-D93F-44C7-BC29-C9CB5FB2C576");
            arrAccountIDList.Add("4F23FE00-9A93-4C34-A911-CB25DA72F66A");
            arrAccountIDList.Add("D9DAB472-3FAC-4FFE-B5F5-CC88A4E78D9A");
            arrAccountIDList.Add("40CC78CB-D78F-4A8A-AD93-D1EF2428653C");
            arrAccountIDList.Add("8CBAE3E9-67D4-46E3-AE43-EC956ED042DF");
            return arrAccountIDList;
        }
    }
}
