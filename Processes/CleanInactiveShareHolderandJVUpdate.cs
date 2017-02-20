using CRMCleaner.Classes;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CRMCleaner.Processes
{
    class CleanInactiveShareHolderandJVUpdate
    {
        internal void Start()
        {
            DataTable dtUpdateClassification = getDTStruc();
            Dictionary<string, string> dicNULL = new Dictionary<string, string>();
            //get all the INACTIVE
            //DataTable dtInactiveShareHolder = getAllInactiveShareHolder();
            DataTable dtActiveShareHolder =getAllActiveShareHolder();
            //DataTable dtNULLJVCategory= getAllNULLJVCategoryCID();
            int RecordCounted = dtActiveShareHolder.Rows.Count;
            //int RecordCounted = dtNULLJVCategory.Rows.Count;
            Console.WriteLine("Total Record to clean up: " + RecordCounted.ToString());
            //CHECK 1 by 1 ShareHolder
            int Counter = 0;
            foreach (DataRow dr1 in dtActiveShareHolder.Rows)
            //foreach (DataRow dr1 in dtNULLJVCategory.Rows)
            {
                string FileID = dr1["MSCFileID"].ToString();
                Guid ShareHolderID = new Guid(dr1["ShareHolderID"].ToString());
                Guid AccountID = new Guid(dr1["AccountID"].ToString());
                string ShareHolderName = dr1["ShareholderName"].ToString();
                string Percentage = dr1["Percentage"].ToString();
                string CountryRegion = getCountryName(dr1["CountryRegionID"].ToString());
                string outPercentage = "";
                if (CheckExistedShareHolderInView(FileID, ShareHolderName, Percentage, CountryRegion, out outPercentage))
                {
                    Console.WriteLine("ShareHolder Found in View Table: " + ShareHolderName);
                    using (SqlConnection Connection = SQLHelper.GetConnection())
                    {
                        SqlTransaction Transaction = default(SqlTransaction);
                        Transaction = Connection.BeginTransaction("UpdateProcess");
                        DataRow dr = dtUpdateClassification.NewRow();
                        //Update to ACTIVE
                        int EffectedRows = 0;
                        SetShareHolderToActive(Connection, Transaction, ShareHolderID, outPercentage, out EffectedRows);
                        if (EffectedRows > 0)
                            dr[4] = ShareHolderName;
                        //UPDATE all CLASSIFICATION
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
                        dr[5] = DateTime.Now.ToString("dd/MM/yyyy");
                        dtUpdateClassification.Rows.Add(dr);
                        Counter++;
                        Console.WriteLine("UPDATED for record no : " + Counter.ToString() + " / " + RecordCounted + " for company : " + CompanyName);
                    }

                }
                else
                {
                    Counter++;
                    Console.WriteLine("ShareHolder NOT Found in View Table: " + ShareHolderName + " " + Counter + " / " + RecordCounted);
                }
            }
            if (RecordCounted > 0)
            {
                string folderPath = ConfigurationSettings.AppSettings["CRMCleanUpExcelLoc"].ToString();
                ExcelFileHelper.GenerateExcelFile(folderPath, dtUpdateClassification, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss tt"), "ShareHolderCleanUp");

            }
            if (dicNULL.Keys.Count > 0)
            {
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"C:\CRMCleanUp\NULLClassification.txt"))
                {
                    foreach (string key in dicNULL.Keys)
                    {
                        string Category = "";
                        if (dicNULL.ContainsKey(key))
                        {
                            Category = dicNULL[key].ToString();
                        }
                        file.WriteLine(key + ": " + Category);
                    }
                    System.Diagnostics.Process.Start("notepad.exe", @"C:\CRMCleanUp\NULLClassification_" + DateTime.Now.ToString("dd-MM-yyyy") + ".txt");
                }
            }
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
        private DataTable getDTStruc()
        {
            DataTable output = new DataTable();
            output.Columns.Add("CompanyName", typeof(string));
            output.Columns.Add("JVClassification", typeof(string));
            output.Columns.Add("BumiClassification", typeof(string));
            output.Columns.Add("AccountClassification", typeof(string));
            output.Columns.Add("ShareHolderStatus", typeof(string));
            output.Columns.Add("UpdateOn", typeof(string));
            return output;
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
                            if (!dtNULL.ContainsKey(AccountID.ToString()) && !dtNULL.ContainsValue("AccountGroupType"))
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
                            if (!dtNULL.ContainsKey(AccountID.ToString()) && !dtNULL.ContainsValue("BUMI_CLASSIFICATION"))
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
                            if (!dtNULL.ContainsKey(AccountID.ToString()) && !dtNULL.ContainsValue("JV_CATEGORY"))
                                dtNULL.Add(AccountID.ToString(), "JV_CATEGORY");
                        }
                        else
                            Class = GetCodeMasterName(Connection, Transaction, ID, "JV_CATEGORY");
                    }
                }
            }
            return Class;
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
        private void SetShareHolderToActive(SqlConnection Connection, SqlTransaction Transaction, Guid ShareHolderID,string outPercentage, out int EffectedRows)
        {
            EffectedRows = 0;
            using (SqlCommand cmd = new SqlCommand("", Connection))
            {
                StringBuilder sql = new StringBuilder();
                sql.AppendLine("UPDATE ShareHolder");
                sql.AppendLine("SET Status =1 , Percentage =@Percentage");
                sql.AppendLine("WHERE ShareHolderID = @ShareHolderID");
                cmd.CommandText = sql.ToString();
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Connection;
                cmd.Transaction = Transaction;
                cmd.CommandTimeout = int.MaxValue;
                cmd.Parameters.AddWithValue("@ShareHolderID", ShareHolderID);
                cmd.Parameters.AddWithValue("@Percentage", outPercentage);
                EffectedRows = cmd.ExecuteNonQuery();
            }
        }
        private string getCountryName(string CountryRegionID)
        {
            string Country = "";
            using (SqlConnection Connection = SQLHelper.GetConnection())
            {
                SqlTransaction Transaction = default(SqlTransaction);
                Transaction = Connection.BeginTransaction("getCountryName");
                using (SqlCommand cmd = new SqlCommand("", Connection))
                {
                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        StringBuilder sql = new StringBuilder();
                        sql.AppendLine("SELECT RegionName");
                        sql.AppendLine("FROM Region");
                        sql.AppendLine("WHERE RegionID=@RegionID");
                        cmd.CommandText = sql.ToString();
                        cmd.CommandType = CommandType.Text;
                        cmd.Connection = Connection;
                        cmd.Transaction = Transaction;
                        cmd.CommandTimeout = int.MaxValue;
                        cmd.Parameters.AddWithValue("@RegionID", CountryRegionID);
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        if (dt.Rows.Count > 0)
                        {
                            Country = dt.Rows[0][0].ToString().Trim();
                        }
                    }
                }
            }
            return Country;
        }
        private bool CheckExistedShareHolderInView(string FileID, string ShareHolderName, string Percentage, string CountryRegion, out string outPercentage)
        {
            outPercentage = "";
            bool Existed = false;
            using (SqlConnection Connection = SQLHelper.GetConnection())
            {
                SqlTransaction Transaction = default(SqlTransaction);
                Transaction = Connection.BeginTransaction("CheckExistedShareHolderInView");
                using (SqlCommand cmd = new SqlCommand("", Connection))
                {
                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        StringBuilder sql = new StringBuilder();
                        sql.AppendLine("SELECT TOP 1 OwnershipSHName, OwnershipPer, OwnershipBumi, OwnershipCName");
                        sql.AppendLine("FROM IntegrationDB.dbo.EIR_PMSCOwnerShipDtls");
                        sql.AppendLine("WHERE FileID =@FileID AND UPPER(OwnershipSHName)=@OwnershipSHName AND OwnershipPer=@OwnershipPer AND UPPER(OwnershipCName)=@OwnershipCName");
                        cmd.CommandText = sql.ToString();
                        cmd.CommandType = CommandType.Text;
                        cmd.Connection = Connection;
                        cmd.Transaction = Transaction;
                        cmd.CommandTimeout = int.MaxValue;
                        cmd.Parameters.AddWithValue("@FileID", FileID);
                        cmd.Parameters.AddWithValue("@OwnershipSHName", ShareHolderName.Trim().ToUpper());
                        cmd.Parameters.AddWithValue("@OwnershipPer", Percentage.Trim());
                        cmd.Parameters.AddWithValue("@OwnershipCName", CountryRegion.Trim().ToUpper());
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        if (dt.Rows.Count > 0)
                        {
                            outPercentage = dt.Rows[0]["OwnershipPer"].ToString();
                            return true;
                        }
                    }
                }
            }
            return Existed;
        }
        private DataTable getAllInactiveShareHolder()
        {
            DataTable output = new DataTable();
            using (SqlConnection Connection = SQLHelper.GetConnection())
            {
                SqlTransaction Transaction = default(SqlTransaction);
                Transaction = Connection.BeginTransaction("getAllInactiveShareHolder");
                using (SqlCommand com = new SqlCommand("", Connection))
                {
                    using (SqlDataAdapter adapter = new SqlDataAdapter(com))
                    {

                        System.Text.StringBuilder sql = new System.Text.StringBuilder();
                        sql.AppendLine("Select a.AccountName as AccountName,a.MSCFileID as MSCFileID ,* FROm ShareHolder s");
                        sql.AppendLine("INNER JOIN Account a on a.AccountID = s.AccountID");
                        sql.AppendLine("where Status = 0");
                        sql.AppendLine("order by a.AccountName");
                        com.CommandText = sql.ToString();
                        com.CommandType = CommandType.Text;
                        com.Connection = Connection;
                        com.Transaction = Transaction;
                        com.CommandTimeout = int.MaxValue;

                        //con.Open()
                        try
                        {
                            DataTable dt = new DataTable();
                            adapter.Fill(dt);
                            if (dt.Rows.Count > 0)
                                output = dt;
                        }
                        catch (Exception ex)
                        {

                        }
                    }
                }
            }
            return output;

        }

        private DataTable getAllActiveShareHolder()
        {
            DataTable output = new DataTable();
            using (SqlConnection Connection = SQLHelper.GetConnection())
            {
                SqlTransaction Transaction = default(SqlTransaction);
                Transaction = Connection.BeginTransaction("getAllInactiveShareHolder");
                using (SqlCommand com = new SqlCommand("", Connection))
                {
                    using (SqlDataAdapter adapter = new SqlDataAdapter(com))
                    {

                        System.Text.StringBuilder sql = new System.Text.StringBuilder();
                        sql.AppendLine("Select a.AccountName as AccountName,a.MSCFileID as MSCFileID ,* FROm ShareHolder s");
                        sql.AppendLine("INNER JOIN Account a on a.AccountID = s.AccountID");
                        sql.AppendLine("where Status = 1");
                        sql.AppendLine("order by a.AccountName");
                        com.CommandText = sql.ToString();
                        com.CommandType = CommandType.Text;
                        com.Connection = Connection;
                        com.Transaction = Transaction;
                        com.CommandTimeout = int.MaxValue;

                        //con.Open()
                        try
                        {
                            DataTable dt = new DataTable();
                            adapter.Fill(dt);
                            if (dt.Rows.Count > 0)
                                output = dt;
                        }
                        catch (Exception ex)
                        {

                        }
                    }
                }
            }
            return output;

        }

        private DataTable getAllNULLJVCategoryCID()
        {
            DataTable output = new DataTable();
            using (SqlConnection Connection = SQLHelper.GetConnection())
            {
                SqlTransaction Transaction = default(SqlTransaction);
                Transaction = Connection.BeginTransaction("getAllInactiveShareHolder");
                using (SqlCommand com = new SqlCommand("", Connection))
                {
                    using (SqlDataAdapter adapter = new SqlDataAdapter(com))
                    {

                        System.Text.StringBuilder sql = new System.Text.StringBuilder();
                        sql.AppendLine("Select a.AccountName as AccountName,a.MSCFileID as MSCFileID ,* FROm ShareHolder s");
                        sql.AppendLine("INNER JOIN Account a on a.AccountID = s.AccountID");
                        sql.AppendLine("where Status = 0 AND a.JVCategoryCID is NULL");
                        sql.AppendLine("order by a.AccountName");
                        com.CommandText = sql.ToString();
                        com.CommandType = CommandType.Text;
                        com.Connection = Connection;
                        com.Transaction = Transaction;
                        com.CommandTimeout = int.MaxValue;

                        //con.Open()
                        try
                        {
                            DataTable dt = new DataTable();
                            adapter.Fill(dt);
                            if (dt.Rows.Count > 0)
                                output = dt;
                        }
                        catch (Exception ex)
                        {

                        }
                    }
                }
            }
            return output;

        }
    }
}
