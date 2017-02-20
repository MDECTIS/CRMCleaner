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
    class PostMSCChangesClean
    {
        ArrayList Log = new ArrayList();
        public enum AuditLogType
        {
            Common = 0,
            Account = 1
        }
        internal void Start()
        {
            // DataTable dtClean = getDataToSync();
            //string CompanyName = "";
            //foreach (DataRow row in dtClean.Rows)
            //{
            //    CompanyName = "";
            try
            {
                //CompanyName = row["AccountName"].ToString();
                // Console.WriteLine("POST MSC Process start for " + CompanyName + "...");
                //Log.Add(string.Format("Total need to cleaned: [{0}]", dtClean.Rows.Count.ToString()));
                //Console.WriteLine(string.Format("Total need to cleaned: [{0}]", dtClean.Rows.Count.ToString()));

                //Guid? AccountDVID = new Guid(row["AccountDVID"].ToString());
                DataTable dtPostChanges = getDataToSync();

                foreach (DataRow row in dtPostChanges.Rows)
                {
                    using (SqlConnection Connection = SQLHelper.GetConnection())
                    {
                        Guid? AccountDVID = new Guid(row["AccountDVID"].ToString());
                        //Guid? AccountDVID = new Guid("8B692469-CCAC-4378-92CB-33C76495F366");
                        //if (AccountDVID.Value.ToString() != "8FD45D49-A8B3-4CF5-BECC-2961C2C50215") //AccelTeam Sdn Bhd
                        //{
                        Guid? ActionBy = new Guid("74425431-65A4-498E-A6ED-910A9E20B6FC");
                        string ActionByName = "admin";
                        SqlTransaction Transaction = default(SqlTransaction);
                        int RowCounted = MSCAccountChangesVerification(Connection, Transaction, AccountDVID, ActionBy, ActionByName);
                        //
                    }
                    //else
                    //{
                    //    //Log.Add(string.Format("Skip Record for Company : [{0}]", CompanyName));
                    //    //Console.WriteLine(string.Format("Skip Record for Company : [{0}]", CompanyName));
                    //}
                }
                //if (Log.Count > 0)
                //{
                //    string ModeSync = "POSTMSCChanges_";
                //    LogFileHelper.WriteLog(LogFileHelper.logList, ModeSync);
                //}
                //}


            }
            catch (Exception ex)
            {
                //Console.WriteLine("ERROR for company: " + CompanyName);
            }
            //}
        }

       private DataTable getDataToSync()
        {
            DataTable output = new DataTable();
            using (SqlConnection conn = new SqlConnection(ConfigurationSettings.AppSettings["CRM"].ToString()))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand("", conn))
                {
                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        StringBuilder sql = new StringBuilder();

                        sql.AppendLine("SELECT * FROM (SELECT A.AccountDVID");
                        sql.AppendLine(", cm.CodeName SubmitType ");
                        sql.AppendLine(" , A.AccountName");
                        sql.AppendLine(", A.MSCFileID ");
                        sql.AppendLine(" , dbo.GetMainCluster(A.AccountDVID) AS[MainCluster] ");
                        sql.AppendLine(" , dbo.GetMainClusterID(A.AccountDVID) AS[MainClusterID]");
                        sql.AppendLine("FROM AccountDV A ");
                        sql.AppendLine("INNER JOIN CodeMaster cm ON cm.CodeMasterID = 'D9D47B2E-BA8F-45D7-94ED-44D65C6904B7' ");
                        sql.AppendLine(") T ");
                        //sql.AppendLine("WHERE 1 = 1");

                        cmd.CommandText = sql.ToString();
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        if (dt != null && dt.Rows.Count > 0)
                        {
                            output = dt;
                        }
                    }
                }
            }
            return output;
        }

        private int MSCAccountChangesVerification(SqlConnection Connection, SqlTransaction Transaction, Guid? AccountDVID, Guid? ActionBy, string ActionByName)
        {
            int affectedRows = 0;

            try
            {

                using (SqlCommand cmd = new SqlCommand("", Connection))
                {
                    StringBuilder sql = new StringBuilder();

                    BOL.AuditLog.Modules.AccountLog alMgr = new BOL.AuditLog.Modules.AccountLog();
                    Guid? AccountID = GetAccountIDByAccountDVID(AccountDVID);
                    Guid guidAccountID = new Guid(AccountID.Value.ToString());
                    DataRow currentLogData = alMgr.SelectAccountForLog_MSCAccountChangesVerification(guidAccountID);

                    #region "UPDATE Account"
                    sql.Clear();
                    affectedRows = UpdateAccount(Connection, Transaction, AccountDVID, ActionBy, ActionByName);
                    Console.WriteLine("UPDATE Account DONE");
                    Log.Add(string.Format("UPDATE Account DONE"));
                    #endregion
                    SetShareholderToInActiveIfGotNewRecords(Connection, Transaction, AccountID, AccountDVID);
                    //OK DONE
                    #region "UPDATE ShareHolder"
                    affectedRows = UpdateShareHolder(Connection, Transaction, AccountDVID, ActionBy, ActionByName);
                    Console.WriteLine("UPDATE ShareHolder DONE");
                    Log.Add(string.Format("UPDATE ShareHolder DONE"));
                    #endregion
                    #region "INSERT ShareHolder"
                    Guid? ShareHolderDVID = getIDByAccountDVID(AccountDVID, "ShareHolder", "ShareHolderDV");
                    if (ShareHolderDVID != null)
                    {
                        if (!CheckShareHolderExisted(ShareHolderDVID))
                        {
                            affectedRows = InsertShareHolder(Connection, Transaction, AccountDVID, ActionBy, ActionByName);
                            Console.WriteLine("INSERT ShareHolder DONE");
                            Log.Add(string.Format("INSERT ShareHolder DONE"));
                        }
                    }
                    #endregion

                    Guid? FinancialAnalyst = SyncHelper.GetCodeMasterID(Connection, Transaction, BOL.AppConst.AccountManagerType.FinancialAnalyst, BOL.AppConst.CodeType.AccountManagerType);
                    Guid? BusinessAnalyst = SyncHelper.GetCodeMasterID(Connection, Transaction, BOL.AppConst.AccountManagerType.BusinessAnalyst, BOL.AppConst.CodeType.AccountManagerType);
                    Guid? postMSC = SyncHelper.GetCodeMasterID(Connection, Transaction, BOL.AppConst.AccountManagerType.PostMSC, BOL.AppConst.CodeType.AccountManagerType);

                    SetAccountManagerToInActiveIfGotNewRecords(Connection, Transaction, AccountID, AccountDVID, FinancialAnalyst);
                    SetAccountManagerToInActiveIfGotNewRecords(Connection, Transaction, AccountID, AccountDVID, BusinessAnalyst);
                    SetAccountManagerToInActiveIfGotNewRecords(Connection, Transaction, AccountID, AccountDVID, postMSC);
                    //OK DONE
                    #region "UPDATE AccountManagerAssignment"
                    //UpdateAccountManagerAssignment(Connection, Transaction, AccountDVID, ActionBy, ActionByName);
                    Console.WriteLine("UPDATE AccountManagerAssignment DONE");
                    Log.Add(string.Format("UPDATE AccountManagerAssignment DONE"));
                    #endregion
                    #region "INSERT AccountManagerAssignment"
                    Guid? AccountManagerAssignmentDVID = getIDByAccountDVID(AccountDVID, "AccountManagerAssignment", "AccountManagerAssignmentDV");
                    if (AccountManagerAssignmentDVID != null)
                    {
                        if (!CheckAccountManagerAssignment(AccountManagerAssignmentDVID))
                        {
                            affectedRows = InsertAccountManagerAssignment(Connection, Transaction, AccountDVID, ActionBy, ActionByName);
                            Console.WriteLine("INSERT AccountManagerAssignment DONE");
                            Log.Add(string.Format("INSERT AccountManagerAssignment DONE"));
                        }
                    }
                    #endregion
                    #region "UPDATE FinancialAndWorkerForecast"
                    UpdateFinancialAndWorkerForecast(Connection, Transaction, AccountDVID, ActionBy, ActionByName);
                    Console.WriteLine("UPDATE FinancialAndWorkerForecast DONE");
                    Log.Add(string.Format("UPDATE FinancialAndWorkerForecast DONE"));
                    #endregion
                    #region "INSERT FinancialAndWorkerForecast"
                    Guid? FinancialAndWorkerForecastDVID = getIDByAccountDVID(AccountDVID, "FinancialAndWorkerForecast", "FinancialAndWorkerForecastDV");
                    if (FinancialAndWorkerForecastDVID != null)
                    {
                        if (!CheckFinancialAndWorkerForecast(FinancialAndWorkerForecastDVID))
                            affectedRows = InsertFinancialAndWorkerForecast(Connection, Transaction, AccountDVID, ActionBy, ActionByName);
                    }
                    Console.WriteLine("INSERT FinancialAndWorkerForecast DONE");
                    Log.Add(string.Format("INSERT FinancialAndWorkerForecast DONE"));
                    #endregion

                    SetContactToInActiveIfGotNewRecords(Connection, Transaction, AccountID, AccountDVID);
                    //OK DONE
                    #region "UPDATE Contact"
                    affectedRows = UpdateContact(Connection, Transaction, AccountDVID, ActionBy, ActionByName);
                    Console.WriteLine("UPDATE Contact DONE");
                    Log.Add(string.Format("UPDATE Contact DONE"));
                    #endregion
                    //OK DONE
                    #region "INSERT Contact"
                    //Guid? ContactDVID = getIDByAccountDVID(AccountDVID, "Contact", "ContactDV");
                    //if (ContactDVID != null)
                    //{
                    //    if (!CheckContactExisted(ContactDVID))
                    //    {
                    //        affectedRows = InsertContact(Connection, Transaction, AccountDVID, ActionBy, ActionByName);
                    //        Console.WriteLine("INSERT Contact DONE");
                    //        Log.Add(string.Format("INSERT Contact DONE"));
                    //    }
                    //}
                    #endregion

                    #region "UPDATE Address"
                    affectedRows = UpdateAddress(Connection, Transaction, AccountDVID, ActionBy, ActionByName);
                    Console.WriteLine("UPDATE Address DONE");
                    Log.Add(string.Format("UPDATE Address DONE"));
                    #endregion
                    //OK DONE
                    #region "INSERT Address"

                    Guid? AddresstDVID = getIDByAccountDVIDADD(AccountDVID, "Address", "AddressDV");
                    if (AddresstDVID != null)
                    {
                        if (!CheckAddressExisted(AddresstDVID))
                        {
                            affectedRows = InsertAddress(Connection, Transaction, AccountDVID, ActionBy, ActionByName);
                            Console.WriteLine("INSERT Address DONE");
                            Log.Add(string.Format("INSERT Address DONE"));
                        }
                    }
                    #endregion

                    #region "UPDATE Auditkey"
                    //sql.Clear();
                    //sql.AppendLine("UPDATE al SET al.Auditkey = adv.AccountID FROM AuditLog al JOIN AccountDV adv ON al.AuditKey = adv.AccountDVID");
                    //sql.AppendLine("UPDATE al SET al.Auditkey = shdv.ShareHolderID FROM AuditLog al JOIN ShareHolderDV shdv ON al.AuditKey = shdv.ShareHolderDVID");
                    //sql.AppendLine("WHERE shdv.ShareHolderID IS NOT NULL");
                    //sql.AppendLine("UPDATE al SET al.Auditkey = amadv.AccountManagerAssignmentID FROM AuditLog al JOIN AccountManagerAssignmentDV amadv ON al.AuditKey = amadv.AccountManagerAssignmentDVID");
                    //sql.AppendLine("WHERE amadv.AccountManagerAssignmentID IS NOT NULL");
                    //sql.AppendLine("UPDATE al SET al.Auditkey = fwfdv.FinancialAndWorkerForecastID FROM AuditLog al JOIN FinancialAndWorkerForecastDV fwfdv ON al.AuditKey = fwfdv.FinancialAndWorkerForecastDVID");
                    //sql.AppendLine("WHERE fwfdv.FinancialAndWorkerForecastID IS NOT NULL");
                    //sql.AppendLine("UPDATE al SET al.Auditkey = cdv.ContactID FROM AuditLog al JOIN ContactDV cdv ON al.AuditKey = cdv.ContactDVID");
                    //sql.AppendLine("WHERE cdv.ContactID IS NOT NULL");
                    //sql.AppendLine("UPDATE al SET al.Auditkey = adv.AddressID FROM AuditLog al JOIN AddressDV adv ON al.AuditKey = adv.AddressDVID");
                    //sql.AppendLine("WHERE adv.AddressID IS NOT NULL");
                    ////No9
                    //cmd.CommandText = sql.ToString();
                    //cmd.CommandTimeout = int.MaxValue;
                    //cmd.Transaction = Transaction;
                    //cmd.Parameters.AddWithValue("@AccountDVID", AccountDVID);
                    //cmd.Parameters.AddWithValue("@ActionBy", ActionBy);
                    //cmd.Parameters.AddWithValue("@ActionByName", ActionByName);
                    //affectedRows += cmd.ExecuteNonQuery();
                    //Console.WriteLine("UPDATE Auditkey DONE");
                    //Log.Add(string.Format("UPDATE Auditkey DONE"));
                    #endregion

                    #region UPDATES AccountJVCategory/AccountBumiClassification/AccountClassification
                    affectedRows = UpdateAccountJVCategory(Connection, Transaction, AccountID, ActionBy, ActionByName);
                    affectedRows = UpdateAccountBumiClassification(Connection, Transaction, AccountID, ActionBy, ActionByName);
                    affectedRows = UpdateAccountClassification(Connection, Transaction, AccountID, ActionBy, ActionByName);
                    Console.WriteLine("UPDATES AccountJVCategory/AccountBumiClassification/AccountClassification DONE");
                    Log.Add(string.Format("UPDATES AccountJVCategory/AccountBumiClassification/AccountClassification DONE"));
                    #endregion

                    #region LOG int AccountLog
                    //DataRow newLogData = alMgr.SelectAccountForLog_MSCAccountChangesVerification(guidAccountID);
                    //if (currentLogData != null && newLogData != null)
                    //{
                    //    Guid guidActionBy = new Guid(ActionBy.ToString());
                    //    alMgr.CreateAccountLog_MSCAccountChangesVerification(guidAccountID, currentLogData, newLogData, guidActionBy, ActionByName);
                    //}
                    #endregion

                    #region DELETE All Related to DV tables
                    affectedRows = DeleteMSCAccountChangesVerification(Connection, Transaction, AccountDVID);
                    Console.WriteLine("DELETE All Related to DV tables DONE");
                    Log.Add(string.Format("DELETE All Related to DV tables DONE"));
                    #endregion
                    //Transaction.Commit();
                }
            }
            catch (Exception ex)
            {
                Log.Add(string.Format("Error Hit : [{)}]", ex.Message.ToString()));
                Console.WriteLine(string.Format("Error Hit : [{)}]", ex.Message.ToString()));
                Transaction.Rollback();
            }
            return affectedRows;
        }

        private static int InsertAddress(SqlConnection Connection, SqlTransaction Transaction, Guid? AccountDVID, Guid? ActionBy, string ActionByName)
        {
            int output = 0;
            try
            {
                using (SqlCommand cmd = new SqlCommand("", Connection))
                {
                    StringBuilder sql = new StringBuilder();
                    sql.AppendLine("INSERT INTO Address");
                    sql.AppendLine("(AddressID, OwnerName, OwnerID, AddressTypeID, Address1, City, PostCode, State, CountryRegionID, BusinessPhoneCtry, BusinessPhoneStt, BusinessPhoneCC, BusinessPhone, BusinessPhoneExt, FaxCtry, FaxStt, FaxCC, Fax, CreatedBy, CreatedByName, CreatedDate, ModifiedBy, ModifiedByName, ModifiedDate)");
                    sql.AppendLine("SELECT ddv.AddressDVID, 'Account', adv.AccountID, ddv.AddressTypeID, ddv.Address1, ddv.City, ddv.PostCode, ddv.State, ddv.CountryRegionID, ddv.BusinessPhoneCtry, ddv.BusinessPhoneStt, ddv.BusinessPhoneCC, ddv.BusinessPhone, ddv.BusinessPhoneExt, ddv.FaxCtry, ddv.FaxStt, ddv.FaxCC, ddv.Fax, @ActionBy, @ActionByName, GETDATE(), @ActionBy, @ActionByName, GETDATE()");
                    sql.AppendLine("FROM AddressDV ddv JOIN AccountDV adv ON ddv.OwnerID = adv.AccountDVID");
                    sql.AppendLine("WHERE ddv.OwnerID = @AccountDVID");
                    sql.AppendLine("AND ddv.AddressID IS NULL");
                    //No8
                    cmd.CommandText = sql.ToString();
                    cmd.CommandTimeout = 0;
                    cmd.Transaction = Transaction;
                    cmd.Parameters.AddWithValue("@AccountDVID", AccountDVID);
                    cmd.Parameters.AddWithValue("@ActionBy", ActionBy);
                    cmd.Parameters.AddWithValue("@ActionByName", ActionByName);
                    output = cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("Error Hit : [{)}]", ex.Message.ToString()));
                Transaction.Rollback();
            }

            return output;
        }

        private static int UpdateAddress(SqlConnection Connection, SqlTransaction Transaction, Guid? AccountDVID, Guid? ActionBy, string ActionByName)
        {
            int output = 0;
            try
            {
                using (SqlCommand cmd = new SqlCommand("", Connection))
                {
                    StringBuilder sql = new StringBuilder();
                    sql.AppendLine("UPDATE d SET");
                    sql.AppendLine("d.Address1 = ddv.Address1,");
                    sql.AppendLine("d.City = ddv.City,");
                    sql.AppendLine("d.PostCode = ddv.PostCode,");
                    sql.AppendLine("d.State = ddv.State,");
                    sql.AppendLine("d.CountryRegionID = ddv.CountryRegionID,");
                    sql.AppendLine("d.BusinessPhoneCtry = ddv.BusinessPhoneCtry,");
                    sql.AppendLine("d.BusinessPhoneStt = ddv.BusinessPhoneStt,");
                    sql.AppendLine("d.BusinessPhoneCC = ddv.BusinessPhoneCC,");
                    sql.AppendLine("d.BusinessPhone = ddv.BusinessPhone, ");
                    sql.AppendLine("d.BusinessPhoneExt = ddv.BusinessPhoneExt,");
                    sql.AppendLine("d.FaxCtry = ddv.FaxCtry,");
                    sql.AppendLine("d.FaxStt = ddv.FaxStt,");
                    sql.AppendLine("d.FaxCC = ddv.FaxCC,");
                    sql.AppendLine("d.Fax = ddv.Fax, ");
                    sql.AppendLine("d.ModifiedBy = @ActionBy,");
                    sql.AppendLine("d.ModifiedByName = @ActionByName,");
                    sql.AppendLine("d.ModifiedDate = GETDATE()");
                    sql.AppendLine("FROM AddressDV ddv JOIN Address d ON ddv.AddressID = d.AddressID");
                    sql.AppendLine("WHERE ddv.OwnerID = @AccountDVID");
                    //No7
                    cmd.CommandText = sql.ToString();
                    cmd.CommandTimeout = 0;
                    cmd.Transaction = Transaction;
                    cmd.Parameters.AddWithValue("@AccountDVID", AccountDVID);
                    cmd.Parameters.AddWithValue("@ActionBy", ActionBy);
                    cmd.Parameters.AddWithValue("@ActionByName", ActionByName);
                    output = cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("Error Hit : [{)}]", ex.Message.ToString()));
                Transaction.Rollback();
            }

            return output;
        }

        private static int InsertContact(SqlConnection Connection, SqlTransaction Transaction, Guid? AccountDVID, Guid? ActionBy, string ActionByName)
        {

            int output = 0;
            try
            {
                using (SqlCommand cmd = new SqlCommand("", Connection))
                {
                    StringBuilder sql = new StringBuilder();
                    sql.AppendLine("INSERT INTO Contact");
                    sql.AppendLine("(ContactID, AccountID, DesignationCID, Name, Email, BusinessPhoneCtry, BusinessPhoneStt, BusinessPhoneCC, BusinessPhone, BusinessPhoneExt, MobilePhoneCtry, MobilePhoneCC, MobilePhone, FaxCtry, FaxStt, FaxCC, Fax, ContactTypeCID, Published, ContactStatus, CreatedBy, CreatedByName, CreatedDate, ModifiedBy, ModifiedByName, ModifiedDate)");
                    sql.AppendLine("SELECT cdv.ContactDVID, adv.AccountID, cdv.DesignationCID, cdv.Name, cdv.Email, cdv.BusinessPhoneCtry, cdv.BusinessPhoneStt, cdv.BusinessPhoneCC, cdv.BusinessPhone, cdv.BusinessPhoneExt, cdv.MobilePhoneCtry, cdv.MobilePhoneCC, cdv.MobilePhone, cdv.FaxCtry, cdv.FaxStt, cdv.FaxCC, cdv.Fax, cdv.ContactTypeCID, cdv.Published, cdv.ContactStatus, @ActionBy, @ActionByName, GETDATE(), @ActionBy, @ActionByName, GETDATE()");
                    sql.AppendLine("FROM ContactDV cdv JOIN AccountDV adv ON cdv.AccountDVID = adv.AccountDVID");
                    sql.AppendLine("WHERE cdv.AccountDVID = @AccountDVID");
                    sql.AppendLine("AND cdv.ContactID IS NULL");
                    //No6
                    cmd.CommandText = sql.ToString();
                    cmd.CommandTimeout = 0;
                    cmd.Transaction = Transaction;
                    cmd.Parameters.AddWithValue("@AccountDVID", AccountDVID);
                    cmd.Parameters.AddWithValue("@ActionBy", ActionBy);
                    cmd.Parameters.AddWithValue("@ActionByName", ActionByName);
                    output = cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("Error Hit : [{)}]", ex.Message.ToString()));
                Transaction.Rollback();
            }

            return output;
        }

        private static int UpdateContact(SqlConnection Connection, SqlTransaction Transaction, Guid? AccountDVID, Guid? ActionBy, string ActionByName)
        {
            int output = 0;
            try
            {
                using (SqlCommand cmd = new SqlCommand("", Connection))
                {
                    StringBuilder sql = new StringBuilder();
                    sql.AppendLine("UPDATE c SET");
                    sql.AppendLine("c.Name = cdv.Name,");
                    sql.AppendLine("c.Email = cdv.Email,");
                    sql.AppendLine("c.BusinessPhoneCtry = cdv.BusinessPhoneCtry,");
                    sql.AppendLine("c.BusinessPhoneStt = cdv.BusinessPhoneStt,");
                    sql.AppendLine("c.BusinessPhoneCC = cdv.BusinessPhoneCC,");
                    sql.AppendLine("c.BusinessPhone = cdv.BusinessPhone,");
                    sql.AppendLine("c.BusinessPhoneExt = cdv.BusinessPhoneExt,");
                    sql.AppendLine("c.MobilePhoneCtry = cdv.MobilePhoneCtry,");
                    sql.AppendLine("c.MobilePhoneCC = cdv.MobilePhoneCC,");
                    sql.AppendLine("c.MobilePhone = cdv.MobilePhone,");
                    sql.AppendLine("c.FaxCtry = cdv.FaxCtry,");
                    sql.AppendLine("c.FaxStt = cdv.FaxStt,");
                    sql.AppendLine("c.FaxCC = cdv.FaxCC,");
                    sql.AppendLine("c.Fax = cdv.Fax,");
                    sql.AppendLine("c.ModifiedBy = @ActionBy,");
                    sql.AppendLine("c.ModifiedByName = @ActionByName,");
                    sql.AppendLine("c.ModifiedDate = GETDATE()");
                    sql.AppendLine("FROM ContactDV cdv JOIN Contact c ON cdv.ContactID = c.ContactID");
                    sql.AppendLine("WHERE cdv.AccountDVID = @AccountDVID");
                    //No5
                    cmd.CommandText = sql.ToString();
                    cmd.CommandTimeout = 0;
                    cmd.Transaction = Transaction;
                    cmd.Parameters.AddWithValue("@AccountDVID", AccountDVID);
                    cmd.Parameters.AddWithValue("@ActionBy", ActionBy);
                    cmd.Parameters.AddWithValue("@ActionByName", ActionByName);
                    output = cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("Error Hit : [{)}]", ex.Message.ToString()));
                Transaction.Rollback();
            }

            return output;
        }

        private Guid? getIDByAccountDVID(Guid? AccountDVID, string FieldName, string TableName)
        {
            Guid? output = null;
            using (SqlConnection conn = SQLHelper.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand("", conn))
                {
                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        StringBuilder sql = new StringBuilder();
                        sql.AppendLine("SELECT " + FieldName + "DVID");
                        sql.AppendLine("FROM  " + TableName);
                        sql.AppendLine("WHERE AccountDVID =@AccountDVID ");
                        cmd.CommandText = sql.ToString();
                        cmd.Parameters.AddWithValue("@AccountDVID", AccountDVID);
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        if (dt.Rows.Count > 0)
                        {
                            output = new Guid(dt.Rows[0][0].ToString());
                        }
                    }
                }
            }
            return output;
        }
        private Guid? getIDByAccountDVIDADD(Guid? AccountDVID, string FieldName, string TableName)
        {
            Guid? output = null;
            using (SqlConnection conn = SQLHelper.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand("", conn))
                {
                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        StringBuilder sql = new StringBuilder();
                        sql.AppendLine("SELECT " + FieldName + "DVID");
                        sql.AppendLine("FROM  " + TableName);
                        sql.AppendLine("WHERE OwnerID =@AccountDVID ");
                        cmd.CommandText = sql.ToString();
                        cmd.Parameters.AddWithValue("@AccountDVID", AccountDVID);
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        if (dt.Rows.Count > 0)
                        {
                            output = new Guid(dt.Rows[0][0].ToString());
                        }
                    }
                }
            }
            return output;
        }

        private bool CheckFinancialAndWorkerForecast(Guid? FinancialAndWorkerForecastDVID)
        {
            bool Existed = false;
            try
            {
                using (SqlConnection conn = SQLHelper.GetConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("", conn))
                    {
                        using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                        {
                            StringBuilder sql = new StringBuilder();
                            sql.AppendLine("SELECT CASE WHEN EXISTS (SELECT  *");
                            sql.AppendLine(" FROM  FinancialAndWorkerForecast");
                            sql.AppendLine("WHERE FinancialAndWorkerForecastID =@FinancialAndWorkerForecastDVID) ");
                            sql.AppendLine("THEN CAST (1 AS BIT) ");
                            sql.AppendLine("ELSE CAST (0 AS BIT) END ");
                            cmd.CommandText = sql.ToString();
                            cmd.Parameters.AddWithValue("@FinancialAndWorkerForecastDVID", FinancialAndWorkerForecastDVID);
                            DataTable dt = new DataTable();
                            adapter.Fill(dt);
                            if (Convert.ToInt32(dt.Rows[0][0]) > 0)
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return false;
            }
            return Existed;
        }

        private static int InsertFinancialAndWorkerForecast(SqlConnection Connection, SqlTransaction Transaction, Guid? AccountDVID, Guid? ActionBy, string ActionByName)
        {
            int output = 0;
            try
            {
                using (SqlCommand cmd = new SqlCommand("", Connection))
                {
                    StringBuilder sql = new StringBuilder();
                    sql.AppendLine("INSERT INTO FinancialAndWorkerForecast");
                    sql.AppendLine("(FinancialAndWorkerForecastID, AccountID, Year, LocalKW, ForeignKW, LocalWorker, ForeignWorker, Investment, RnDExpenditure, LocalSales, ExportSales, NetProfit, CashFlow, Asset, Equity, Liabilities, CreatedBy, CreatedByName, CreatedDate, ModifiedBy, ModifiedByName, ModifiedDate)");
                    sql.AppendLine("SELECT fwfdv.FinancialAndWorkerForecastDVID, adv.AccountID, fwfdv.Year, fwfdv.LocalKW, fwfdv.ForeignKW, fwfdv.LocalWorker, fwfdv.ForeignWorker, fwfdv.Investment, fwfdv.RnDExpenditure, fwfdv.LocalSales, fwfdv.ExportSales, fwfdv.NetProfit, fwfdv.CashFlow, fwfdv.Asset, fwfdv.Equity, fwfdv.Liabilities, @ActionBy, @ActionByName, GETDATE(), @ActionBy, @ActionByName, GETDATE()");
                    sql.AppendLine("FROM FinancialAndWorkerForecastDV fwfdv JOIN AccountDV adv ON fwfdv.AccountDVID = adv.AccountDVID");
                    sql.AppendLine("WHERE fwfdv.AccountDVID = @AccountDVID");
                    sql.AppendLine("AND fwfdv.FinancialAndWorkerForecastID IS NULL");
                    //No4
                    cmd.CommandText = sql.ToString();
                    cmd.CommandTimeout = 0;
                    cmd.Transaction = Transaction;
                    cmd.Parameters.AddWithValue("@AccountDVID", AccountDVID);
                    cmd.Parameters.AddWithValue("@ActionBy", ActionBy);
                    cmd.Parameters.AddWithValue("@ActionByName", ActionByName);
                    output = cmd.ExecuteNonQuery();
                }

            }
            catch (Exception ex)
            {

                throw;
            }
            return output;

        }

        private static void UpdateFinancialAndWorkerForecast(SqlConnection Connection, SqlTransaction Transaction, Guid? AccountDVID, Guid? ActionBy, string ActionByName)
        {
            using (SqlCommand cmd = new SqlCommand("", Connection))
            {
                StringBuilder sql = new StringBuilder();
                sql.AppendLine("UPDATE fwf SET");
                sql.AppendLine("fwf.LocalKW = fwfdv.LocalKW,");
                sql.AppendLine("fwf.ForeignKW = fwfdv.ForeignKW,");
                sql.AppendLine("fwf.LocalWorker = fwfdv.LocalWorker,");
                sql.AppendLine("fwf.ForeignWorker = fwfdv.ForeignWorker,");
                sql.AppendLine("fwf.Investment = fwfdv.Investment,");
                sql.AppendLine("fwf.RnDExpenditure = fwfdv.RnDExpenditure,");
                sql.AppendLine("fwf.LocalSales = fwfdv.LocalSales,");
                sql.AppendLine("fwf.ExportSales = fwfdv.ExportSales,");
                sql.AppendLine("fwf.NetProfit = fwfdv.NetProfit,");
                sql.AppendLine("fwf.CashFlow = fwfdv.CashFlow,");
                sql.AppendLine("fwf.Asset = fwfdv.Asset,");
                sql.AppendLine("fwf.Equity = fwfdv.Equity,");
                sql.AppendLine("fwf.Liabilities = fwfdv.Liabilities,");
                sql.AppendLine("fwf.ModifiedBy = @ActionBy,");
                sql.AppendLine("fwf.ModifiedByName = @ActionByName,");
                sql.AppendLine("fwf.ModifiedDate = GETDATE()");
                sql.AppendLine("FROM FinancialAndWorkerForecastDV fwfdv JOIN FinancialAndWorkerForecast fwf ON fwfdv.FinancialAndWorkerForecastID = fwf.FinancialAndWorkerForecastID");
                sql.AppendLine("WHERE fwfdv.AccountDVID = @AccountDVID");

                cmd.CommandText = sql.ToString();
                cmd.CommandTimeout = 0;
                cmd.Transaction = Transaction;
                cmd.Parameters.AddWithValue("@AccountDVID", AccountDVID);
                cmd.Parameters.AddWithValue("@ActionBy", ActionBy);
                cmd.Parameters.AddWithValue("@ActionByName", ActionByName);
                int affectedRows = cmd.ExecuteNonQuery();
            }
        }

        private static int InsertAccountManagerAssignment(SqlConnection Connection, SqlTransaction Transaction, Guid? AccountDVID, Guid? ActionBy, string ActionByName)
        {
            int output = 0;
            using (SqlCommand cmd = new SqlCommand("", Connection))
            {
                StringBuilder sql = new StringBuilder();
                sql.AppendLine("INSERT INTO AccountManagerAssignment");
                sql.AppendLine("(AccountManagerAssignmentID, AccountID, EEManagerName, AccountManagerTypeCID, DataSource, AssignmentDate, CreatedBy, CreatedByName, CreatedDate, ModifiedBy, ModifiedByName, ModifiedDate, Active)");
                sql.AppendLine("SELECT amadv.AccountManagerAssignmentDVID, adv.AccountID, amadv.EEManagerName, amadv.AccountManagerTypeCID, amadv.DataSource, amadv.AssignmentDate, @ActionBy, @ActionByName, GETDATE(), @ActionBy, @ActionByName, GETDATE(), amadv.Active");
                sql.AppendLine("FROM AccountManagerAssignmentDV amadv JOIN AccountDV adv ON amadv.AccountDVID = adv.AccountDVID");
                sql.AppendLine("WHERE amadv.AccountDVID = @AccountDVID");
                sql.AppendLine("AND amadv.AccountManagerAssignmentID IS NULL");
                //No3.2
                cmd.CommandText = sql.ToString();
                cmd.CommandTimeout = 0;
                cmd.Transaction = Transaction;
                cmd.Parameters.AddWithValue("@AccountDVID", AccountDVID);
                cmd.Parameters.AddWithValue("@ActionBy", ActionBy);
                cmd.Parameters.AddWithValue("@ActionByName", ActionByName);
                output = cmd.ExecuteNonQuery();
            }
            return output;
        }

        private static void UpdateAccountManagerAssignment(SqlConnection Connection, SqlTransaction Transaction, Guid? AccountDVID, Guid? ActionBy, string ActionByName)
        {
            using (SqlCommand cmd = new SqlCommand("", Connection))
            {
                StringBuilder sql = new StringBuilder();
                sql.AppendLine("UPDATE ama SET");
                sql.AppendLine("ama.AssignmentDate = amadv.AssignmentDate,");
                sql.AppendLine("ama.StartDate = amadv.StartDate,");
                sql.AppendLine("ama.EndDate = amadv.EndDate,");
                sql.AppendLine("ama.ModifiedBy = @ActionBy,");
                sql.AppendLine("ama.ModifiedByName = @ActionByName,");
                sql.AppendLine("ama.ModifiedDate = GETDATE()");
                sql.AppendLine("FROM AccountManagerAssignmentDV amadv JOIN AccountManagerAssignment ama ON amadv.AccountManagerAssignmentID = ama.AccountManagerAssignmentID");
                sql.AppendLine("WHERE amadv.AccountDVID = @AccountDVID");
                //No3.1
                cmd.CommandText = sql.ToString();
                cmd.CommandTimeout = 0;
                cmd.Transaction = Transaction;
                cmd.Parameters.AddWithValue("@AccountDVID", AccountDVID);
                cmd.Parameters.AddWithValue("@ActionBy", ActionBy);
                cmd.Parameters.AddWithValue("@ActionByName", ActionByName);
            }
        }

        private static int InsertShareHolder(SqlConnection Connection, SqlTransaction Transaction, Guid? AccountDVID, Guid? ActionBy, string ActionByName)
        {
            int output = 0;
            using (SqlCommand cmd = new SqlCommand("", Connection))
            {
                StringBuilder sql = new StringBuilder();
                sql.AppendLine("INSERT INTO ShareHolder");
                sql.AppendLine("(ShareHolderID, AccountID, ShareHolderName, Percentage, BumiShare, Status, CountryRegionID, CreatedBy, CreatedByName, CreatedDate, ModifiedBy, ModifiedByName, ModifiedDate)");
                sql.AppendLine("SELECT shdv.ShareHolderDVID, adv.AccountID, shdv.ShareholderName, shdv.Percentage, shdv.BumiShare, shdv.Status, shdv.CountryRegionID, @ActionBy, @ActionByName, GETDATE(), @ActionBy, @ActionByName, GETDATE()");
                sql.AppendLine("FROM ShareHolderDV shdv JOIN AccountDV adv ON shdv.AccountDVID = adv.AccountDVID");
                sql.AppendLine("WHERE shdv.AccountDVID = @AccountDVID");
                sql.AppendLine("AND shdv.ShareHolderID IS NULL");
                //No2.2
                cmd.CommandText = sql.ToString();
                cmd.CommandTimeout = 0;
                cmd.Transaction = Transaction;
                cmd.Parameters.AddWithValue("@AccountDVID", AccountDVID);
                cmd.Parameters.AddWithValue("@ActionBy", ActionBy);
                cmd.Parameters.AddWithValue("@ActionByName", ActionByName);
                output = cmd.ExecuteNonQuery();

            }
            return output;
        }

        private static int UpdateShareHolder(SqlConnection Connection, SqlTransaction Transaction, Guid? AccountDVID, Guid? ActionBy, string ActionByName)
        {
            int output = 0;
            using (SqlCommand cmd = new SqlCommand("", Connection))
            {
                StringBuilder sql = new StringBuilder();
                sql.AppendLine("UPDATE sh SET");
                sql.AppendLine("sh.Percentage = shdv.Percentage,");
                sql.AppendLine("sh.BumiShare = shdv.BumiShare,");
                sql.AppendLine("sh.CountryRegionID = shdv.CountryRegionID,");
                sql.AppendLine("sh.ApprovalDate = shdv.ApprovalDate,");
                sql.AppendLine("sh.ModifiedBy = @ActionBy,");
                sql.AppendLine("sh.ModifiedByName = @ActionByName,");
                sql.AppendLine("sh.ModifiedDate = GETDATE()");
                sql.AppendLine("FROM ShareHolderDV shdv JOIN ShareHolder sh ON shdv.ShareHolderID = sh.ShareHolderID");
                sql.AppendLine("WHERE shdv.AccountDVID = @AccountDVID");
                //No2.1
                cmd.CommandText = sql.ToString();
                cmd.CommandTimeout = 0;
                cmd.Transaction = Transaction;
                cmd.Parameters.AddWithValue("@AccountDVID", AccountDVID);
                cmd.Parameters.AddWithValue("@ActionBy", ActionBy);
                cmd.Parameters.AddWithValue("@ActionByName", ActionByName);
                output = cmd.ExecuteNonQuery();
            }
            return output;
        }

        private static int UpdateAccount(SqlConnection Connection, SqlTransaction Transaction, Guid? AccountDVID, Guid? ActionBy, string ActionByName)
        {
            int output = 0;
            try
            {
                using (SqlCommand cmd = new SqlCommand("", Connection))
                {
                    StringBuilder sql = new StringBuilder();
                    sql.AppendLine("UPDATE a SET");
                    sql.AppendLine("a.AccountName = adv.AccountName,");
                    sql.AppendLine("a.BumiClassificationCID = adv.BumiClassificationCID,");
                    sql.AppendLine("a.ClassificationCID = adv.ClassificationCID,");
                    sql.AppendLine("a.JVCategoryCID = adv.JVCategoryCID,");
                    sql.AppendLine("a.CoreActivities = adv.CoreActivities,");
                    sql.AppendLine("a.BusinessPhone = adv.BusinessPhone,");
                    sql.AppendLine("a.Fax = adv.Fax,");
                    sql.AppendLine("a.WebSiteUrl = adv.WebSiteUrl,");
                    sql.AppendLine("a.MSCApprovedCourses = adv.MSCApprovedCourses,");
                    sql.AppendLine("a.InstitutionName = adv.InstitutionName,");
                    sql.AppendLine("a.InstitutionType = adv.InstitutionType,");
                    sql.AppendLine("a.InstitutionURL = adv.InstitutionURL,");
                    sql.AppendLine("a.OperationStatus = adv.OperationStatus,");
                    sql.AppendLine("a.CompanyTypeCID = adv.CompanyTypeCID,");
                    sql.AppendLine("a.CompanyRegNo = adv.CompanyRegNo,");
                    sql.AppendLine("a.AccountCategoryCID = adv.AccountCategoryCID,");
                    sql.AppendLine("a.IndustryCID = adv.IndustryCID,");
                    sql.AppendLine("a.ParentAccountID = adv.ParentAccountID,");
                    sql.AppendLine("a.CompanyLocationID = adv.CompanyLocationID,");
                    sql.AppendLine("a.DateOfIncorporation = adv.DateOfIncorporation,");
                    sql.AppendLine("a.BursaMalaysiaCID = adv.BursaMalaysiaCID,");
                    sql.AppendLine("a.CounterName = adv.CounterName,");
                    sql.AppendLine("a.EquityOwnershipCID = adv.EquityOwnershipCID,");
                    sql.AppendLine("a.WomanOwnCompany = adv.WomanOwnCompany,");
                    sql.AppendLine("a.Acc5YearsTax = adv.Acc5YearsTax,");
                    sql.AppendLine("a.LeadGenerator = adv.LeadGenerator,");
                    sql.AppendLine("a.PDG = adv.PDG,");
                    sql.AppendLine("a.EXPat = adv.EXPat,");
                    sql.AppendLine("a.Remarks = adv.Remarks,");
                    sql.AppendLine("a.FinancialIncentiveCID = adv.FinancialIncentiveCID,");
                    sql.AppendLine("a.BumiStatusCID = adv.BumiStatusCID,");
                    sql.AppendLine("a.CompanyEmail = adv.CompanyEmail,");
                    sql.AppendLine("a.Logo = adv.Logo,");
                    sql.AppendLine("a.LeadSubmitDate = adv.LeadSubmitDate,");
                    sql.AppendLine("a.CompanyLogo = adv.CompanyLogo,");
                    sql.AppendLine("a.ModifiedBy = @ActionBy,");
                    sql.AppendLine("a.ModifiedByName = @ActionByName,");
                    sql.AppendLine("a.ModifiedDate = GETDATE()");
                    sql.AppendLine("FROM AccountDV adv JOIN Account a ON adv.AccountID = a.AccountID");
                    sql.AppendLine("WHERE adv.AccountDVID = @AccountDVID");
                    //No1
                    cmd.CommandText = sql.ToString();
                    cmd.CommandTimeout = 0;
                    cmd.Transaction = Transaction;
                    cmd.Parameters.AddWithValue("@AccountDVID", AccountDVID);
                    cmd.Parameters.AddWithValue("@ActionBy", ActionBy);
                    cmd.Parameters.AddWithValue("@ActionByName", ActionByName);
                    output = cmd.ExecuteNonQuery();

                }
            }
            catch (Exception)
            {

                throw;
            }


            return output;
        }

        private bool CheckShareHolderExisted(Guid? ShareHolderDVID)
        {
            bool Existed = false;
            try
            {
                using (SqlConnection conn = SQLHelper.GetConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("", conn))
                    {
                        using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                        {
                            StringBuilder sql = new StringBuilder();
                            sql.AppendLine("SELECT CASE WHEN EXISTS (SELECT  *");
                            sql.AppendLine(" FROM  ShareHolder");
                            sql.AppendLine("WHERE ShareHolderID =@ShareHolderDVID) ");
                            sql.AppendLine("THEN CAST (1 AS BIT) ");
                            sql.AppendLine("ELSE CAST (0 AS BIT) END ");
                            cmd.CommandText = sql.ToString();
                            cmd.Parameters.AddWithValue("@ShareHolderDVID", ShareHolderDVID);
                            DataTable dt = new DataTable();
                            adapter.Fill(dt);
                            if (Convert.ToInt32(dt.Rows[0][0]) > 0)
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return false;
            }
            return Existed;
        }

        private bool CheckAccountManagerAssignment(Guid? AccountManagerAssignmentDVID)
        {
            bool Existed = false;
            try
            {
                using (SqlConnection conn = SQLHelper.GetConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("", conn))
                    {
                        using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                        {
                            StringBuilder sql = new StringBuilder();
                            sql.AppendLine("SELECT CASE WHEN EXISTS (SELECT  *");
                            sql.AppendLine(" FROM  AccountManagerAssignment");
                            sql.AppendLine("WHERE AccountManagerAssignmentID =@AccountManagerAssignmentDVID) ");
                            sql.AppendLine("THEN CAST (1 AS BIT) ");
                            sql.AppendLine("ELSE CAST (0 AS BIT) END ");
                            cmd.CommandText = sql.ToString();
                            cmd.Parameters.AddWithValue("@AccountManagerAssignmentDVID", AccountManagerAssignmentDVID);
                            DataTable dt = new DataTable();
                            adapter.Fill(dt);
                            if (Convert.ToInt32(dt.Rows[0][0]) > 0)
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return false;
            }
            return Existed;
        }

        private bool CheckContactExisted(Guid? ContactDVID)
        {
            bool Existed = false;
            try
            {
                using (SqlConnection conn = SQLHelper.GetConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("", conn))
                    {
                        using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                        {
                            StringBuilder sql = new StringBuilder();
                            sql.AppendLine("SELECT CASE WHEN EXISTS (SELECT  *");
                            sql.AppendLine(" FROM  Contact");
                            sql.AppendLine("WHERE ContactID =@ContactDVID) ");
                            sql.AppendLine("THEN CAST (1 AS BIT) ");
                            sql.AppendLine("ELSE CAST (0 AS BIT) END ");
                            cmd.CommandText = sql.ToString();
                            cmd.Parameters.AddWithValue("@ContactDVID", ContactDVID);
                            DataTable dt = new DataTable();
                            adapter.Fill(dt);
                            if (Convert.ToInt32(dt.Rows[0][0]) > 0)
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return false;
            }
            return Existed;
        }

        private bool CheckAddressExisted(Guid? AddresstDVID)
        {
            bool Existed = false;
            try
            {
                using (SqlConnection conn = SQLHelper.GetConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("", conn))
                    {
                        using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                        {
                            StringBuilder sql = new StringBuilder();
                            sql.AppendLine("SELECT CASE WHEN EXISTS (SELECT  *");
                            sql.AppendLine(" FROM  Address");
                            sql.AppendLine("WHERE AddresstID =@AddresstDVID) ");
                            sql.AppendLine("THEN CAST (1 AS BIT) ");
                            sql.AppendLine("ELSE CAST (0 AS BIT) END ");
                            cmd.CommandText = sql.ToString();
                            cmd.Parameters.AddWithValue("@AddresstDVID", AddresstDVID);
                            DataTable dt = new DataTable();
                            adapter.Fill(dt);
                            if (Convert.ToInt32(dt.Rows[0][0]) > 0)
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return false;
            }
            return Existed;
        }

        public int DeleteMSCAccountChangesVerification(SqlConnection Connection, SqlTransaction Transaction, Guid? AccountDVID)
        {
            int affectedRows = 0;
            try
            {
                using (SqlCommand cmd = new SqlCommand("", Connection))
                {


                    StringBuilder sql = new StringBuilder();
                    sql.AppendLine("DELETE FROM AddressDV WHERE OwnerID = @AccountDVID");
                    sql.AppendLine("DELETE FROM ContactDV WHERE AccountDVID = @AccountDVID");
                    sql.AppendLine("DELETE FROM FinancialAndWorkerForecastDV WHERE AccountDVID = @AccountDVID");
                    sql.AppendLine("DELETE FROM AccountManagerAssignmentDV WHERE AccountDVID = @AccountDVID");
                    sql.AppendLine("DELETE FROM ShareHolderDV WHERE AccountDVID = @AccountDVID");
                    //sql.AppendLine("DELETE FROM ShareHolderDVOLD WHERE AccountDVID = @AccountDVID");
                    sql.AppendLine("DELETE FROM AccountDV WHERE AccountDVID = @AccountDVID");
                    cmd.CommandText = sql.ToString();
                    cmd.CommandTimeout = 0;
                    cmd.Transaction = Transaction;
                    cmd.Parameters.AddWithValue("@AccountDVID", AccountDVID);
                    affectedRows = cmd.ExecuteNonQuery();


                }
            }
            catch (Exception)
            {

                throw;
            }
            return affectedRows;
        }

        private bool SetContactToInActiveIfGotNewRecords(SqlConnection Connection, SqlTransaction Transaction, Guid? AccountID, Guid? AccountDVID)
        {
            int affectedRows = 0;
            try
            {

                using (SqlCommand cmd = new SqlCommand("", Connection))
                {
                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        StringBuilder sql = new StringBuilder();
                        sql.AppendLine("IF EXISTS(SELECT * FROM contactdv WHERE AccountDVID = @AccountDVID)");
                        sql.AppendLine("BEGIN");
                        sql.AppendLine("UPDATE Contact SET ContactStatus = 0 WHERE AccountID = @AccountID");
                        sql.AppendLine("             AND Contactid NOT IN ");
                        sql.AppendLine("                (SELECT DISTINCT Contactid FROM Contactdv ");
                        sql.AppendLine("                    WHERE AccountDVID = @AccountDVID AND Contactid IS NOT NULL) ");
                        sql.AppendLine("END");
                        cmd.CommandText = sql.ToString();
                        cmd.CommandTimeout = 0;
                        cmd.Transaction = Transaction;
                        cmd.Parameters.AddWithValue("@AccountDVID", AccountDVID);
                        cmd.Parameters.AddWithValue("@AccountID", AccountID);
                        affectedRows = cmd.ExecuteNonQuery();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private bool SetShareholderToInActiveIfGotNewRecords(SqlConnection Connection, SqlTransaction Transaction, Guid? AccountID, Guid? AccountDVID)
        {
            int affectedRows = 0;
            try
            {

                using (SqlCommand cmd = new SqlCommand("", Connection))
                {
                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        StringBuilder sql = new StringBuilder();
                        sql.AppendLine("IF EXISTS(SELECT * FROM shareholderdv WHERE AccountDVID = @AccountDVID)");
                        sql.AppendLine("BEGIN");
                        sql.AppendLine("UPDATE shareholder SET Status = 0 WHERE AccountID = @AccountID");
                        sql.AppendLine("             AND shareholderid NOT IN ");
                        sql.AppendLine("                (SELECT DISTINCT shareholderid FROM shareholderdv ");
                        sql.AppendLine("                    WHERE AccountDVID = @AccountDVID AND shareholderid IS NOT NULL) ");
                        sql.AppendLine("END");
                        cmd.CommandText = sql.ToString();
                        cmd.CommandTimeout = 0;
                        cmd.Transaction = Transaction;
                        cmd.Parameters.AddWithValue("@AccountDVID", AccountDVID);
                        cmd.Parameters.AddWithValue("@AccountID", AccountID);
                        affectedRows = cmd.ExecuteNonQuery();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private bool SetAccountManagerToInActiveIfGotNewRecords(SqlConnection Connection, SqlTransaction Transaction, Guid? AccountID, Guid? AccountDVID, Guid? AccountManagerTypeCID)
        {
            int affectedRows = 0;
            try
            {

                using (SqlCommand cmd = new SqlCommand("", Connection))
                {
                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        StringBuilder sql = new StringBuilder();
                        sql.AppendLine("IF EXISTS(SELECT * ");
                        sql.AppendLine("          FROM   accountmanagerassignmentdv ");
                        sql.AppendLine("          WHERE  AccountDVID = @AccountDVID ");
                        sql.AppendLine("                 AND AccountmanagertypeCID = @AccountmanagertypeCID) ");
                        sql.AppendLine("  BEGIN ");
                        sql.AppendLine("      UPDATE accountmanagerassignment ");
                        sql.AppendLine("      SET    active = 0 ");
                        sql.AppendLine("      WHERE  AccountID = @AccountID ");
                        sql.AppendLine("             AND AccountmanagertypeCID = @AccountmanagertypeCID ");
                        sql.AppendLine("             AND AccountManagerAssignmentID NOT IN ");
                        sql.AppendLine("                (SELECT DISTINCT AccountManagerAssignmentID FROM AccountManagerAssignmentDV ");
                        sql.AppendLine("                    WHERE AccountDVID = @AccountDVID AND AccountManagerAssignmentID IS NOT NULL) ");
                        sql.AppendLine("  END ");
                        cmd.CommandText = sql.ToString();
                        cmd.CommandTimeout = 0;
                        cmd.Transaction = Transaction;
                        cmd.Parameters.AddWithValue("@AccountDVID", AccountDVID);
                        cmd.Parameters.AddWithValue("@AccountID", AccountID);
                        cmd.Parameters.AddWithValue("@AccountmanagertypeCID", AccountManagerTypeCID);
                        affectedRows = cmd.ExecuteNonQuery();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public Nullable<Guid> GetAccountIDByAccountDVID(Guid? AccountDVID)
        {
            using (SqlConnection conn = SQLHelper.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand("", conn))
                {
                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        StringBuilder sql = new StringBuilder();
                        sql.AppendLine("SELECT AccountID");
                        sql.AppendLine("FROM AccountDV");
                        sql.AppendLine("WHERE AccountDVID = @AccountDVID");
                        cmd.CommandText = sql.ToString();
                        cmd.Parameters.AddWithValue("@AccountDVID", AccountDVID);
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        if (dt != null && dt.Rows.Count > 0)
                        {
                            return new Guid(dt.Rows[0][0].ToString());
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
            }
        }

        public int UpdateAccountJVCategory(SqlConnection Connection, SqlTransaction Transaction, Guid? AccountID, Guid? ActionBy, string ActionByName)
        {
            int affectedRows = 0;

            Nullable<Guid> currentJVCategoryCID = GetJVCategoryCID(AccountID);
            Nullable<Guid> JVCategoryCID = CalculateJVCategory(AccountID);

            if (!currentJVCategoryCID.Equals(JVCategoryCID))
            {

                using (SqlCommand cmd = new SqlCommand("", Connection))
                {
                    StringBuilder sql = new StringBuilder();
                    sql.AppendLine("UPDATE Account");
                    sql.AppendLine("SET JVCategoryCID = @JVCategoryCID, ModifiedDate = getdate(), ModifiedBy = @ActionBy, ModifiedByName = @ActionByName");
                    sql.AppendLine("WHERE AccountID = @AccountID");
                    cmd.CommandText = sql.ToString();
                    cmd.CommandTimeout = 0;
                    cmd.Transaction = Transaction;
                    cmd.Parameters.AddWithValue("@AccountID", AccountID);
                    cmd.Parameters.AddWithValue("@JVCategoryCID", ConvertDbNull(JVCategoryCID));
                    cmd.Parameters.AddWithValue("@ActionBy", ActionBy);
                    cmd.Parameters.AddWithValue("@ActionByName", ActionByName);
                    affectedRows = cmd.ExecuteNonQuery();
                }
            }

            return affectedRows;
        }
        private static object ConvertDbNull(object obj)
        {
            return obj == null ? DBNull.Value : obj;
        }
        public int UpdateAccountBumiClassification(SqlConnection Connection, SqlTransaction Transaction, Guid? AccountID, Guid? ActionBy, string ActionByName)
        {
            int affectedRows = 0;

            Nullable<Guid> currentBumiClassificationCID = GetBumiClassificationCID(AccountID);
            Nullable<Guid> BumiClassificationCID = CalculateBumiClassification(AccountID);

            if (!currentBumiClassificationCID.Equals(BumiClassificationCID))
            {

                using (SqlCommand cmd = new SqlCommand("", Connection))
                {
                    StringBuilder sql = new StringBuilder();
                    sql.AppendLine("UPDATE Account");
                    sql.AppendLine("SET BumiClassificationCID = @BumiClassificationCID, ModifiedDate = getdate(), ModifiedBy = @ActionBy, ModifiedByName = @ActionByName");
                    sql.AppendLine("WHERE AccountID = @AccountID");
                    cmd.CommandText = sql.ToString();
                    cmd.CommandTimeout = 0;
                    cmd.Transaction = Transaction;
                    cmd.Parameters.AddWithValue("@AccountID", AccountID);
                    cmd.Parameters.AddWithValue("@BumiClassificationCID", ConvertDbNull(BumiClassificationCID));
                    cmd.Parameters.AddWithValue("@ActionBy", ActionBy);
                    cmd.Parameters.AddWithValue("@ActionByName", ActionByName);
                    affectedRows = cmd.ExecuteNonQuery();
                }
            }

            return affectedRows;
        }

        public static Nullable<Guid> GetBumiClassificationCID(Guid? AccountID)
        {
            using (SqlConnection conn = new SqlConnection(ConfigurationSettings.AppSettings["CRM"].ToString()))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand("", conn))
                {
                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        StringBuilder sql = new StringBuilder();
                        sql.AppendLine("SELECT BumiClassificationCID");
                        sql.AppendLine("FROM Account");
                        sql.AppendLine("WHERE AccountID = @AccountID");
                        cmd.CommandText = sql.ToString();
                        cmd.Parameters.AddWithValue("@AccountID", AccountID);
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        if (dt != null && dt.Rows.Count > 0 && !string.IsNullOrEmpty(dt.Rows[0][0].ToString()))
                        {
                            return new Guid(dt.Rows[0][0].ToString());
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
            }
        }

        public static int UpdateAccountClassification(SqlConnection Connection, SqlTransaction Transaction, Guid? AccountID, Guid? ActionBy, string ActionByName)
        {
            int affectedRows = 0;

            Nullable<Guid> currentClassificationCID = GetClassificationCID(AccountID);
            Nullable<Guid> ClassificationCID = CalculateClassification(AccountID);

            if (!currentClassificationCID.Equals(ClassificationCID) && !ClassificationCID.Equals(Guid.Empty))
            {

                using (SqlCommand cmd = new SqlCommand("", Connection))
                {
                    StringBuilder sql = new StringBuilder();
                    sql.AppendLine("UPDATE Account");
                    sql.AppendLine("SET ClassificationCID = @ClassificationCID, ModifiedDate = getdate(), ModifiedBy = @ActionBy, ModifiedByName = @ActionByName");
                    sql.AppendLine("WHERE AccountID = @AccountID");
                    cmd.CommandText = sql.ToString();
                    cmd.CommandTimeout = 0;
                    cmd.Transaction = Transaction;
                    cmd.Parameters.AddWithValue("@AccountID", AccountID);
                    cmd.Parameters.AddWithValue("@ClassificationCID", ConvertDbNull(ClassificationCID));
                    cmd.Parameters.AddWithValue("@ActionBy", ActionBy);
                    cmd.Parameters.AddWithValue("@ActionByName", ActionByName);
                    affectedRows = cmd.ExecuteNonQuery();
                }
            }

            return affectedRows;
        }

        public static Nullable<Guid> GetClassificationCID(Guid? AccountID)
        {
            using (SqlConnection conn = new SqlConnection(ConfigurationSettings.AppSettings["CRM"].ToString()))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand("", conn))
                {
                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        StringBuilder sql = new StringBuilder();
                        sql.AppendLine("SELECT ClassificationCID");
                        sql.AppendLine("FROM Account");
                        sql.AppendLine("WHERE AccountID = @AccountID");
                        cmd.CommandText = sql.ToString();
                        cmd.Parameters.AddWithValue("@AccountID", AccountID);
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        if (dt != null && dt.Rows.Count > 0 && !string.IsNullOrEmpty(dt.Rows[0][0].ToString()))
                        {
                            return new Guid(dt.Rows[0][0].ToString());
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
            }
        }

        public static Nullable<Guid> CalculateJVCategory(Guid? AccountID)
        {
            Nullable<Guid> JVCategoryCID = null;
            double foreignSharePercentage = GetForeignCountryShareholderPercentage(AccountID);
            double localSharePercentage = GetLocalCountryShareholderPercentage(AccountID);
            CodeMaster mgr = new CodeMaster();

            if (localSharePercentage == 100 && foreignSharePercentage == 0)
            {
                JVCategoryCID = mgr.GetCodeMasterIDWithNull(CodeType.JVCategory, "100% Malaysia");
            }
            else if (localSharePercentage == 0 && foreignSharePercentage == 100)
            {
                JVCategoryCID = mgr.GetCodeMasterIDWithNull(CodeType.JVCategory, "100% Foreign");
            }
            else if (localSharePercentage == 50 && foreignSharePercentage == 50)
            {
                JVCategoryCID = mgr.GetCodeMasterIDWithNull(CodeType.JVCategory, "50/50");
            }
            else if (localSharePercentage < foreignSharePercentage)
            {
                JVCategoryCID = mgr.GetCodeMasterIDWithNull(CodeType.JVCategory, "Majority Foreign");
            }
            else if (localSharePercentage > foreignSharePercentage)
            {
                JVCategoryCID = mgr.GetCodeMasterIDWithNull(CodeType.JVCategory, "Majority Local");
            }

            return JVCategoryCID;
        }

        public static Nullable<Guid> GetJVCategoryCID(Guid? AccountID)
        {
            using (SqlConnection conn = new SqlConnection(ConfigurationSettings.AppSettings["CRM"].ToString()))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand("", conn))
                {
                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        StringBuilder sql = new StringBuilder();
                        sql.AppendLine("SELECT JVCategoryCID");
                        sql.AppendLine("FROM Account");
                        sql.AppendLine("WHERE AccountID = @AccountID");
                        cmd.CommandText = sql.ToString();
                        cmd.Parameters.AddWithValue("@AccountID", AccountID);
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        if (dt != null && dt.Rows.Count > 0 && !string.IsNullOrEmpty(dt.Rows[0][0].ToString()))
                        {
                            return new Guid(dt.Rows[0][0].ToString());
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
            }
        }

        public static Guid? CalculateBumiClassification(Guid? AccountID)
        {
            try
            {
                Nullable<Guid> BumiClassificationCID = null;
                decimal bumiSharePercentage = GetBumiShareholderPercentage(AccountID);
                CodeMaster mgr = new CodeMaster();

                if (bumiSharePercentage == 0)
                {
                    BumiClassificationCID = mgr.GetCodeMasterIDWithNull(CodeType.BumiClassification, "Others");
                }
                else if (bumiSharePercentage > 50)
                {
                    BumiClassificationCID = mgr.GetCodeMasterIDWithNull(CodeType.BumiClassification, "Majority Bumi");
                }
                else if (bumiSharePercentage <= 50)
                {
                    BumiClassificationCID = mgr.GetCodeMasterIDWithNull(CodeType.BumiClassification, "Bumi participation");
                }

                return BumiClassificationCID;
            }
            catch (Exception ex)
            {
                //MsgBox(ex.Message, MsgBoxStyle.OkOnly, "CalculateBumiClassification")
                return null;
            }
        }

        public static object ConvertToNull(object value)
        {
            if (value == null)
            {
                return DBNull.Value;
            }
            else
            {
                return value;
            }
        }


        public static Guid? CalculateClassification(Guid? AccountID)
        {
            try
            {
                Nullable<Guid> ClassificationCID = null;
                double localSharePercentage = GetLocalCountryShareholderPercentage(AccountID);
                double foreignSharePercentage = GetForeignCountryShareholderPercentage(AccountID);
                CodeMaster mgr = new CodeMaster();

                if (localSharePercentage == 50 && foreignSharePercentage == 50)
                {
                    ClassificationCID = mgr.GetCodeMasterIDWithNull(CodeType.Classification, "50/50");
                }
                else if (localSharePercentage > foreignSharePercentage)
                {
                    ClassificationCID = mgr.GetCodeMasterIDWithNull(CodeType.Classification, "Malaysian Owned");
                }
                else if (localSharePercentage < foreignSharePercentage)
                {
                    ClassificationCID = mgr.GetCodeMasterIDWithNull(CodeType.Classification, "Foreign Owned");
                }

                return ClassificationCID;
            }
            catch (Exception ex)
            {
                //MsgBox(ex.Message, MsgBoxStyle.OkOnly, "CalculateClassification")
                return null;
            }
        }

        public static double GetForeignCountryShareholderPercentage(Guid? AccountID)
        {
            using (SqlConnection conn = SQLHelper.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand("", conn))
                {
                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        StringBuilder sql = new StringBuilder();
                        sql.AppendLine("SELECT ISNULL(SUM(ISNULL(s.Percentage, 0)), 0)");
                        sql.AppendLine("FROM Shareholder s");
                        sql.AppendLine("LEFT JOIN Region Country ON Country.RegionID = s.CountryRegionID");
                        sql.AppendLine("WHERE AccountID = @AccountID");
                        sql.AppendLine("AND [Status] = 1");
                        sql.AppendLine("AND (Country.RegionName <> 'Malaysia' OR Country.RegionName IS NULL)");
                        cmd.CommandText = sql.ToString();
                        cmd.Parameters.AddWithValue("@AccountID", AccountID);
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        if (dt != null && dt.Rows.Count > 0)
                        {
                            return Convert.ToDouble(dt.Rows[0][0]);
                        }
                        else
                        {
                            return 0.0;
                        }
                    }
                }
            }
        }


        private static string GetCountryName(string codename)
        {
            SqlConnection con = SQLHelper.GetConnection();
            SqlCommand com = new SqlCommand();
            SqlDataAdapter ad = new SqlDataAdapter();

            try
            {
                ad.SelectCommand = com;
                StringBuilder sql = new StringBuilder();
                sql.AppendLine("select CodeMasterID from CodeMaster where CodeType='AMRTemplateStatus' and");
                sql.AppendLine("codename=@codename");

                com.CommandText = sql.ToString();
                com.Connection = con;

                com.Parameters.Add(new SqlParameter("@codename", codename));

                DataTable dt = new DataTable();
                ad.Fill(dt);
                return dt.Rows[0][0].ToString();

            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {
                con.Close();
            }
        }

        private static double GetLocalCountryShareholderPercentage(Guid? AccountID)
        {
            using (SqlConnection conn = new SqlConnection(ConfigurationSettings.AppSettings["CRM"].ToString()))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand("", conn))
                {
                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        StringBuilder sql = new StringBuilder();
                        sql.AppendLine("SELECT ISNULL(SUM(ISNULL(s.Percentage, 0)), 0)");
                        sql.AppendLine("FROM Shareholder s");
                        sql.AppendLine("INNER JOIN Region Country ON Country.RegionID = s.CountryRegionID");
                        sql.AppendLine("AND Country.RegionName = 'Malaysia'");
                        sql.AppendLine("WHERE AccountID = @AccountID");
                        sql.AppendLine("AND [Status] = 1");
                        cmd.CommandText = sql.ToString();
                        cmd.Parameters.AddWithValue("@AccountID", AccountID);
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        if (dt != null && dt.Rows.Count > 0)
                        {
                            return Convert.ToDouble(dt.Rows[0][0]);
                        }
                        else
                        {
                            return 0.0;
                        }
                    }
                }
            }
        }

        private static int GetBumiShareholderPercentage(Guid? AccountID)
        {
            using (SqlConnection conn = new SqlConnection(ConfigurationSettings.AppSettings["CRM"].ToString()))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand("", conn))
                {
                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        StringBuilder sql = new StringBuilder();
                        sql.AppendLine("SELECT ISNULL(SUM(ISNULL(s.Percentage, 0)), 0)");
                        sql.AppendLine("FROM Shareholder s");
                        sql.AppendLine("WHERE AccountID = @AccountID");
                        sql.AppendLine("AND [Status] = 1");
                        sql.AppendLine("AND s.BumiShare = 1");
                        cmd.CommandText = sql.ToString();
                        cmd.Parameters.AddWithValue("@AccountID", AccountID);
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        if (dt != null && dt.Rows.Count > 0)
                        {
                            return Convert.ToInt32(dt.Rows[0][0]);
                        }
                        else
                        {
                            return 0;
                        }
                    }
                }
            }
        }
    }
}
