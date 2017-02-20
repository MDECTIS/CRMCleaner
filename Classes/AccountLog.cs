using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace CRMCleaner.Classes
{
    public enum AuditLogType
    {
        Common = 0,
        Account = 1
    }
    class AccountLog
    {
        public const string MSCAccountChangesVerification = "MSCAccountChangesVerification";
        public const string Contact = "Contact";
        public DataRow SelectAccountForLog_MSCStatus_Wizard(SqlConnection Connection, SqlTransaction Transaction, Guid? MSCStatusHistoryID)
        {
            using (SqlCommand cmd = new SqlCommand("", Connection))
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                {
                    StringBuilder sql = new StringBuilder();

                    sql.AppendLine("SELECT cmmas.CodeName AS [MSC Status], convert(varchar, msh.MSCApprovalDate, 106) AS [Approval Date]");
                    sql.AppendLine("FROM MSCStatusHistory msh");
                    sql.AppendLine("LEFT JOIN CodeMaster cmmas ON cmmas.CodeMasterID = msh.MSCApprovalStatusCID");
                    sql.AppendLine("WHERE msh.MSCStatusHistoryID = @MSCStatusHistoryID");

                    cmd.CommandText = sql.ToString();
                    cmd.CommandTimeout = int.MaxValue;
                    cmd.Transaction = Transaction;
                    cmd.Parameters.AddWithValue("@MSCStatusHistoryID", MSCStatusHistoryID);
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    if (dataTable != null && dataTable.Rows.Count > 0)
                    {
                        return dataTable.Rows[0];
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            //End Using
        }

        public DataRow SelectAccountForLog_MSCAccountChangesVerification(Guid? AccountID)
        {
            using (SqlConnection conn = new SqlConnection(ConfigurationSettings.AppSettings["CRM"].ToString()))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand("", conn))
                {
                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        StringBuilder sql = new StringBuilder();

                        sql.AppendLine("SELECT a.AccountCode AS [Account Code], a.AccountName AS [Company Name],");
                        sql.AppendLine("cmat.CodeName AS [Account Type], a.MSCFileID AS [MSC File ID],");
                        sql.AppendLine("a.CompanyRegNo AS [Company Reg No], cmct.CodeName AS [Company Type],");
                        sql.AppendLine("cmac.CodeName AS [Account Category], cmi.CodeName AS [Industry],");
                        sql.AppendLine("pa.AccountName AS [Parent Company], rl.RegionName AS [Location],");
                        sql.AppendLine("cmbs.CodeName AS [Bumi Status], a.BusinessPhone AS [Business Phone],");
                        sql.AppendLine("a.Fax AS [Fax], a.WebSiteUrl AS [Web Site], a.InstitutionName AS [Institution Name], a.InstitutionType AS [Institution Type], a.InstitutionURL AS [Institution Web Site], a.OperationStatus AS [Operational Status],");
                        sql.AppendLine("a.CoreActivities, cmbc.CodeName AS [Bumi Classification], cmc.CodeName AS [Classification],");
                        sql.AppendLine("cmjvc.CodeName AS [JV Category]");
                        sql.AppendLine("FROM Account a");
                        sql.AppendLine("LEFT JOIN CodeMaster cmat ON cmat.CodeMasterID = a.AccountTypeCID");
                        sql.AppendLine("LEFT JOIN CodeMaster cmct ON cmct.CodeMasterID = a.CompanyTypeCID");
                        sql.AppendLine("LEFT JOIN CodeMaster cmac ON cmac.CodeMasterID = a.AccountCategoryCID");
                        sql.AppendLine("LEFT JOIN CodeMaster cmi ON cmi.CodeMasterID = a.IndustryCID");
                        sql.AppendLine("LEFT JOIN Account pa ON pa.AccountID = a.ParentAccountID");
                        sql.AppendLine("LEFT JOIN Region rl ON rl.RegionID = a.CompanyLocationID");
                        sql.AppendLine("LEFT JOIN CodeMaster cmbs ON cmbs.CodeMasterID = a.BumiStatusCID");
                        sql.AppendLine("LEFT JOIN CodeMaster cmbc ON cmbc.CodeMasterID = a.BumiClassificationCID");
                        sql.AppendLine("LEFT JOIN CodeMaster cmc ON cmc.CodeMasterID = a.ClassificationCID");
                        sql.AppendLine("LEFT JOIN CodeMaster cmjvc ON cmjvc.CodeMasterID = a.JVCategoryCID");
                        sql.AppendLine("WHERE a.AccountID = @AccountID");

                        cmd.CommandText = sql.ToString();
                        cmd.CommandTimeout = int.MaxValue;
                        cmd.Parameters.AddWithValue("@AccountID", AccountID);
                        DataTable dataTable = new DataTable();
                        adapter.Fill(dataTable);

                        if (dataTable != null && dataTable.Rows.Count > 0)
                        {
                            return dataTable.Rows[0];
                        }
                        else {
                            return null;
                        }
                    }
                }
            }
        }

        public void CreateAccountLog_Shareholder_Wizard(SqlConnection Connection, SqlTransaction Transaction, Guid? ShareholderID, Guid? AccountID, DataRow CurrentData, DataRow NewData, Guid UserID, string UserName)
        {
            CreateLog_Wizard(Connection, Transaction, AuditLog.AuditLogType.Account, ShareholderID, "Shareholder", CodeType.Shareholder, "Account", AccountID, CurrentData, NewData,
            UserID, UserName);
        }
        public void CreateAccountLog_Contact_Wizard(SqlConnection Connection, SqlTransaction Transaction, Guid? ContactID, Guid? AccountID, DataRow CurrentData, DataRow NewData, Guid UserID, string UserName)
        {
            CreateLog_Wizard(Connection, Transaction, AuditLog.AuditLogType.Account, ContactID, "Contact", Contact, "Account", AccountID, CurrentData, NewData,
            UserID, UserName);
        }
        private void CreateLog_Wizard(SqlConnection Connection, SqlTransaction Transaction, AuditLog.AuditLogType AuditLogType, Guid? AuditKey, string AuditSource, string AuditName, string ReferenceTable, Guid? ReferenceID, DataRow CurrentData, DataRow NewData, Guid UserID, string UserName)
        {
            if (CurrentData != null || NewData != null)
            {
                AuditFieldContainer objContainer = new AuditFieldContainer();
                List<AuditField> fieldList = new List<AuditField>();
                List<AuditField> fullFieldList = new List<AuditField>();
                AuditLog.AuditAction auditAction = default(AuditLog.AuditAction);
                Nullable<Guid> auditGroupID = default(Nullable<Guid>);
                if (CurrentData == null)
                {
                    auditAction = AuditLog.AuditAction.Insert;
                    //Add
                    foreach (DataColumn co in NewData.Table.Columns)
                    {
                        AuditField fieldObj = new AuditField();
                        fieldObj.FieldName = co.ColumnName;
                        fieldObj.FieldValue = NewData[co.ColumnName].ToString();
                        fieldList.Add(fieldObj);
                    }
                    foreach (DataColumn co in NewData.Table.Columns)
                    {
                        AuditField fieldObj = new AuditField();
                        fieldObj.FieldName = co.ColumnName;
                        fieldObj.FieldValue = NewData[co.ColumnName].ToString();
                        fullFieldList.Add(fieldObj);
                    }
                }
                else if (NewData == null)
                {
                    //Delete
                    auditAction = AuditLog.AuditAction.Delete;
                    auditGroupID = GetLastAuditGroupID_Wizard(AuditLogType, AuditName, AuditSource, AuditKey);
                    foreach (DataColumn co in CurrentData.Table.Columns)
                    {
                        AuditField fieldObj = new AuditField();
                        fieldObj.FieldName = co.ColumnName;
                        fieldObj.FieldValue = CurrentData[co.ColumnName].ToString();
                        fieldList.Add(fieldObj);
                    }
                    foreach (DataColumn co in CurrentData.Table.Columns)
                    {
                        AuditField fieldObj = new AuditField();
                        fieldObj.FieldName = co.ColumnName;
                        fieldObj.FieldValue = CurrentData[co.ColumnName].ToString();
                        fullFieldList.Add(fieldObj);
                    }
                }
                else
                {
                    //Update
                    auditAction = AuditLog.AuditAction.Update;
                    auditGroupID = GetLastAuditGroupID_Wizard(AuditLogType, AuditName, AuditSource, AuditKey);
                    foreach (DataColumn co in CurrentData.Table.Columns)
                    {
                        if (CurrentData[co.ColumnName].ToString() != NewData[co.ColumnName].ToString())
                        {
                            AuditField fieldObj = new AuditField();
                            fieldObj.FieldName = co.ColumnName;
                            fieldObj.FieldValue = NewData[co.ColumnName].ToString();
                            fieldList.Add(fieldObj);
                        }
                    }
                    foreach (DataColumn co in CurrentData.Table.Columns)
                    {
                        AuditField fieldObj = new AuditField();
                        fieldObj.FieldName = co.ColumnName;
                        fieldObj.FieldValue = NewData[co.ColumnName].ToString();
                        fullFieldList.Add(fieldObj);
                    }
                }

                if (fieldList.Count > 0)
                {
                    objContainer.AuditFields = fieldList.ToArray();
                    objContainer.FullAuditFields = fullFieldList.ToArray();
                    AccelTeam.Utilities.Data Data = new AccelTeam.Utilities.Data(ConfigurationSettings.AppSettings["CRM"].ToString());
                    Guid auditID = Guid.NewGuid();
                    string xml = this.SerializeToXML(objContainer);
                    if (!auditGroupID.HasValue)
                        auditGroupID = Guid.NewGuid();
                    AuditLog.InsertAuditLog(Connection, Transaction, auditID, AuditSource, AuditKey, AuditName, AuditLogType, auditAction, "", ReferenceTable, ReferenceID, xml, auditGroupID, UserID, UserName);
                }

            }
        }
        private string SerializeToXML(AuditFieldContainer Value)
        {
            XmlSerializer serializer = new XmlSerializer(Value.GetType());

            StringBuilder xml = new StringBuilder();
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Encoding = Encoding.UTF8;
            settings.Indent = true;

            using (XmlWriter writer = XmlWriter.Create(xml, settings))
            {
                if (writer != null)
                {
                    serializer.Serialize(writer, Value);
                }
            }

            return xml.ToString();

        }
        public DataRow SelectAccountForLog_Contact_Wizard(SqlConnection Connection, SqlTransaction Transaction, Guid? ContactID)
        {

            using (SqlCommand cmd = new SqlCommand("", Connection))
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                {
                    StringBuilder sql = new StringBuilder();

                    sql.AppendLine("SELECT c.Name, cms.CodeName AS [Salutation], cmd.CodeName AS [Position],");
                    sql.AppendLine("CASE c.KeyContact WHEN 1 THEN 'Yes' WHEN 0 THEN 'No' ELSE '' END AS [Key Contact],");
                    sql.AppendLine("a.AccountName AS [Company Name], c.Department, rtc.Name AS [Report To],");
                    sql.AppendLine("n.Content AS [Notes], CASE c.Published WHEN 1 THEN 'Yes' WHEN 0 THEN 'No' ELSE '' END AS [Published],");
                    sql.AppendLine("c.Email, c.OtherEmail AS [Other Email], dbo.GetFormattedPhoneNo(c.BusinessPhoneCC, c.BusinessPhone, c.BusinessPhoneExt) AS [Business Phone], ");
                    sql.AppendLine("c.OtherBusinessPhone AS [Other Business Phone], dbo.GetFormattedPhoneNo(c.MobilePhoneCC, c.MobilePhone, NULL) AS [Mobile],");
                    sql.AppendLine("c.OtherMobilePhone AS [Other Mobile], dbo.GetFormattedPhoneNo(c.FaxCC, c.Fax, NULL) Fax, c.OtherFax AS [OtherFax],");
                    sql.AppendLine("c.IMAddress AS [IM Address], c.SkypeName AS [Skype Name], ");
                    sql.AppendLine("cmcc.CodeName AS [Designation], CASE c.ContactStatus WHEN 1 THEN 'Active' WHEN 0 THEN 'Inactive' ELSE '' END AS [Contact Status]");
                    sql.AppendLine("FROM Contact c");
                    sql.AppendLine("LEFT JOIN CodeMaster cms ON cms.CodeMasterID = c.SalutationCID");
                    sql.AppendLine("LEFT JOIN CodeMaster cmd ON cmd.CodeMasterID = c.DesignationCID");
                    sql.AppendLine("INNER JOIN Account a ON a.AccountID = c.AccountID");
                    sql.AppendLine("LEFT JOIN Contact rtc ON rtc.ContactID = c.ReportToContactID");
                    sql.AppendLine("LEFT JOIN Notes n ON n.OwnerID = c.ContactID");
                    sql.AppendLine("AND n.OwnerName = 'Contact'");
                    sql.AppendLine("LEFT JOIN CodeMaster cmcc ON cmcc.CodeMasterID = c.ContactCategoryCID");
                    sql.AppendLine("WHERE c.ContactID = @ContactID");

                    cmd.CommandText = sql.ToString();
                    cmd.CommandTimeout = int.MaxValue;
                    cmd.Transaction = Transaction;
                    cmd.Parameters.AddWithValue("@ContactID", ContactID);
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    if (dataTable != null && dataTable.Rows.Count > 0)
                    {
                        return dataTable.Rows[0];
                    }
                    else {
                        return null;
                    }
                }
            }
            //End Using
        }
        public DataRow SelectAccountForLog_Contact(Guid ContactID)
        {
            using (SqlConnection conn = SQLHelper.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand("", conn))
                {
                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        StringBuilder sql = new StringBuilder();

                        sql.AppendLine("SELECT c.Name, cms.CodeName AS [Salutation], cmd.CodeName AS [Position],");
                        sql.AppendLine("CASE c.KeyContact WHEN 1 THEN 'Yes' WHEN 0 THEN 'No' ELSE '' END AS [Key Contact],");
                        sql.AppendLine("a.AccountName AS [Company Name], c.Department, rtc.Name AS [Report To],");
                        sql.AppendLine("n.Content AS [Notes], CASE c.Published WHEN 1 THEN 'Yes' WHEN 0 THEN 'No' ELSE '' END AS [Published],");
                        sql.AppendLine("c.Email, c.OtherEmail AS [Other Email], dbo.GetFormattedPhoneNo(c.BusinessPhoneCC, c.BusinessPhone, c.BusinessPhoneExt) AS [Business Phone], ");
                        sql.AppendLine("c.OtherBusinessPhone AS [Other Business Phone], dbo.GetFormattedPhoneNo(c.MobilePhoneCC, c.MobilePhone, NULL) AS [Mobile],");
                        sql.AppendLine("c.OtherMobilePhone AS [Other Mobile], dbo.GetFormattedPhoneNo(c.FaxCC, c.Fax, NULL) Fax, c.OtherFax AS [OtherFax],");
                        sql.AppendLine("c.IMAddress AS [IM Address], c.SkypeName AS [Skype Name], ");
                        sql.AppendLine("cmcc.CodeName AS [Designation], CASE c.ContactStatus WHEN 1 THEN 'Active' WHEN 0 THEN 'Inactive' ELSE '' END AS [Contact Status]");
                        sql.AppendLine("FROM Contact c");
                        sql.AppendLine("LEFT JOIN CodeMaster cms ON cms.CodeMasterID = c.SalutationCID");
                        sql.AppendLine("LEFT JOIN CodeMaster cmd ON cmd.CodeMasterID = c.DesignationCID");
                        sql.AppendLine("INNER JOIN Account a ON a.AccountID = c.AccountID");
                        sql.AppendLine("LEFT JOIN Contact rtc ON rtc.ContactID = c.ReportToContactID");
                        sql.AppendLine("LEFT JOIN Notes n ON n.OwnerID = c.ContactID");
                        sql.AppendLine("AND n.OwnerName = 'Contact'");
                        sql.AppendLine("LEFT JOIN CodeMaster cmcc ON cmcc.CodeMasterID = c.ContactCategoryCID");
                        sql.AppendLine("WHERE c.ContactID = @ContactID");

                        cmd.CommandText = sql.ToString();
                        cmd.CommandTimeout = int.MaxValue;
                        cmd.Parameters.AddWithValue("@ContactID", ContactID);
                        DataTable dataTable = new DataTable();
                        adapter.Fill(dataTable);

                        if (dataTable != null && dataTable.Rows.Count > 0)
                        {
                            return dataTable.Rows[0];
                        }
                        else {
                            return null;
                        }
                    }
                }
            }
        }
        public static DataRow SelectAccountForLog_Contact(SqlConnection Connection, SqlTransaction Transaction, Guid? ContactDVID)
        {
            using (SqlCommand cmd = new SqlCommand("", Connection))
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                {
                    StringBuilder sql = new StringBuilder();

                    sql.AppendLine("SELECT c.Name, cmd.CodeName AS [Position], c.Email, c.BusinessPhoneCC AS [Business Phone CC], c.BusinessPhone AS [Business Phone], c.BusinessPhoneExt AS [Business Phone Ext], c.FaxCC AS [Fax CC], c.Fax AS [Fax]");
                    sql.AppendLine("FROM ContactDV c");
                    sql.AppendLine("LEFT JOIN CodeMaster cmd ON cmd.CodeMasterID = c.DesignationCID");
                    sql.AppendLine("WHERE c.ContactDVID = @ContactDVID");
                    cmd.CommandText = sql.ToString();
                    cmd.CommandTimeout = int.MaxValue;
                    cmd.Transaction = Transaction;
                    cmd.Parameters.AddWithValue("@ContactDVID", ContactDVID);
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    if (dataTable != null && dataTable.Rows.Count > 0)
                    {
                        return dataTable.Rows[0];
                    }
                    else {
                        return null;
                    }
                }
            }
            //End Using
        }
        public void CreateAccountLog_MSCAccountChangesVerification(Guid? AccountID, DataRow CurrentData, DataRow NewData, Guid? UserID, string UserName)
        {
            CreateLog(AuditLog.AuditLogType.Account, AccountID, "Account", MSCAccountChangesVerification, "Account", AccountID, CurrentData, NewData, UserID, UserName);
        }


        private void CreateLog(AuditLog.AuditLogType AuditLogType, Guid? AuditKey, string AuditSource, string AuditName, string ReferenceTable, Guid? ReferenceID, DataRow CurrentData, DataRow NewData, Guid? UserID, string UserName)
        {
            if (CurrentData != null || NewData != null)
            {
                AuditFieldContainer objContainer = new AuditFieldContainer();
                List<AuditField> fieldList = new List<AuditField>();
                List<AuditField> fullFieldList = new List<AuditField>();
                AuditLog.AuditAction auditAction = default(AuditLog.AuditAction);
                Guid? auditGroupID = default(Nullable<Guid>);
                if (CurrentData == null)
                {
                    auditAction = AuditLog.AuditAction.Insert;
                    //Add
                    foreach (DataColumn co in NewData.Table.Columns)
                    {
                        AuditField fieldObj = new AuditField();
                        fieldObj.FieldName = co.ColumnName;
                        fieldObj.FieldValue = NewData[co.ColumnName].ToString();
                        fieldList.Add(fieldObj);
                    }
                    foreach (DataColumn co in NewData.Table.Columns)
                    {
                        AuditField fieldObj = new AuditField();
                        fieldObj.FieldName = co.ColumnName;
                        fieldObj.FieldValue = NewData[co.ColumnName].ToString();
                        fullFieldList.Add(fieldObj);
                    }
                }
                else if (NewData == null)
                {
                    //Delete
                    auditAction = AuditLog.AuditAction.Delete;
                    auditGroupID = GetLastAuditGroupID_Wizard(AuditLogType, AuditName, AuditSource, AuditKey);
                    foreach (DataColumn co in CurrentData.Table.Columns)
                    {
                        AuditField fieldObj = new AuditField();
                        fieldObj.FieldName = co.ColumnName;
                        fieldObj.FieldValue = CurrentData[co.ColumnName].ToString();
                        fieldList.Add(fieldObj);
                    }
                    foreach (DataColumn co in CurrentData.Table.Columns)
                    {
                        AuditField fieldObj = new AuditField();
                        fieldObj.FieldName = co.ColumnName;
                        fieldObj.FieldValue = CurrentData[co.ColumnName].ToString();
                        fullFieldList.Add(fieldObj);
                    }
                }
                else
                {
                    //Update
                    auditAction = AuditLog.AuditAction.Update;
                    auditGroupID = GetLastAuditGroupID_Wizard(AuditLogType, AuditName, AuditSource, AuditKey);
                    foreach (DataColumn co in CurrentData.Table.Columns)
                    {
                        if (CurrentData[co.ColumnName].ToString() == NewData[co.ColumnName].ToString())
                        {
                            AuditField fieldObj = new AuditField();
                            fieldObj.FieldName = co.ColumnName;
                            fieldObj.FieldValue = NewData[co.ColumnName].ToString();
                            fieldList.Add(fieldObj);
                        }
                    }
                    foreach (DataColumn co in CurrentData.Table.Columns)
                    {
                        AuditField fieldObj = new AuditField();
                        fieldObj.FieldName = co.ColumnName;
                        fieldObj.FieldValue = NewData[co.ColumnName].ToString();
                        fullFieldList.Add(fieldObj);
                    }
                }

                if (fieldList.Count > 0)
                {
                    objContainer.AuditFields = fieldList.ToArray();
                    objContainer.FullAuditFields = fullFieldList.ToArray();
                    //AccelTeam.Utilities.Data Data = new AccelTeam.Utilities.Data(ConfigurationSettings.AppSettings["CRM"].ToString());
                    Guid? auditID = Guid.NewGuid();
                    string xml = SerializeToXML(objContainer);
                    if (!auditGroupID.HasValue)
                        auditGroupID = Guid.NewGuid();
                    InsertAuditLog(auditID, AuditSource, AuditKey, AuditName, AuditLogType, auditAction, "", ReferenceTable, ReferenceID, xml, auditGroupID, UserID, UserName);
                }

            }
        }

        public static Nullable<Guid> GetLastAuditGroupID_Wizard(AuditLog.AuditLogType AuditLogType, string AuditName, string AuditSource, Guid? AuditKey)
        {
            using (SqlConnection Connection=SQLHelper.GetConnection())
            {
                SqlTransaction Transaction = Connection.BeginTransaction();

                using (SqlCommand cmd = new SqlCommand("", Connection))
                {
                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        StringBuilder sql = new StringBuilder();

                        sql.AppendLine("SELECT al.AuditGroupID");
                        sql.AppendLine("from AuditLog al");
                        sql.AppendLine("WHERE al.AuditLogType = @AuditLogType");
                        sql.AppendLine("AND al.AuditSource = @AuditSource");
                        sql.AppendLine("AND al.AuditKey = @AuditKey");
                        sql.AppendLine("AND al.AuditName = @AuditName");
                        sql.AppendLine("ORDER BY al.CreatedDate DESC");

                        cmd.CommandText = sql.ToString();
                        cmd.Connection = Connection;
                        cmd.Transaction = Transaction;
                        cmd.CommandTimeout = int.MaxValue;
                        cmd.Parameters.AddWithValue("@AuditLogType", AuditLogType);
                        cmd.Parameters.AddWithValue("@AuditSource", AuditSource);
                        cmd.Parameters.AddWithValue("@AuditKey", AuditKey);
                        cmd.Parameters.AddWithValue("@AuditName", AuditName);

                        DataTable dataTable = new DataTable();
                        adapter.Fill(dataTable);

                        if (dataTable != null && dataTable.Rows.Count > 0 && !string.IsNullOrEmpty(dataTable.Rows[0][0].ToString()))
                        {
                            return new Guid(dataTable.Rows[0][0].ToString());
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
            }
        }

        public static DataTable InsertAuditLog(Guid?
AuditID, string AuditSource, Guid? AuditKey, string AuditName, AuditLog.AuditLogType AuditLogType, AuditLog.AuditAction AuditAction, string AuditContent, string ReferenceTable,
Guid? ReferenceID, string LogXML, Guid? AuditGroupID, Guid? UserID, string Username)
        {
            using (SqlConnection Connection = SQLHelper.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand("", Connection))
                {
                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        StringBuilder sql = new StringBuilder();

                        sql.AppendLine("INSERT INTO AuditLog (AuditID, AuditSource, AuditKey, AuditName, AuditLogType, AuditAction, AuditContent, ReferenceTable, ReferenceID, LogXML, AuditGroupID, CreatedBy, CreatedByName, ModifiedBy, ModifiedByName) ");
                        sql.AppendLine("VALUES (@AuditID, @AuditSource, @AuditKey, @AuditName, @AuditLogType, @AuditAction, @AuditContent, @ReferenceTable, @ReferenceID, @LogXML, @AuditGroupID, @UserID, @Username, @UserID, @Username) ");

                        cmd.CommandText = sql.ToString();
                        cmd.CommandTimeout = int.MaxValue;
                        cmd.Parameters.AddWithValue("@AuditID", AuditID);
                        cmd.Parameters.AddWithValue("@AuditSource", AuditSource);
                        cmd.Parameters.AddWithValue("@AuditKey", AuditKey);
                        cmd.Parameters.AddWithValue("@AuditName", AuditName);
                        cmd.Parameters.AddWithValue("@AuditLogType", AuditLogType);
                        cmd.Parameters.AddWithValue("@AuditAction", AuditAction);
                        cmd.Parameters.AddWithValue("@AuditContent", AuditContent);
                        cmd.Parameters.AddWithValue("@ReferenceTable", ConvertToNull(ReferenceTable));
                        cmd.Parameters.AddWithValue("@ReferenceID", ConvertToNull(ReferenceID));
                        cmd.Parameters.AddWithValue("@LogXML", LogXML);
                        cmd.Parameters.AddWithValue("@AuditGroupID", AuditGroupID);
                        cmd.Parameters.AddWithValue("@UserID", UserID);
                        cmd.Parameters.AddWithValue("@Username", Username);
                        DataTable dataTable = new DataTable();
                        adapter.Fill(dataTable);

                        return dataTable;
                    }
                }
            }
        }
        internal static object ConvertToNull(object value)
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
    }

}
