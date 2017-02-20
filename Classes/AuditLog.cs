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
    class AuditLog
    {
        public const string General = "General";
        public const string MSCStatus = "MSC Status";
        public const string Relocation = "Relocation";
        public const string FinancialAndWorkerForecast = "Financial and Worker Forecast";
        public const string Cluster = "Cluster";
        public const string BusinessAnalyst = "Business Analyst";
        public const string Address = "Address";
        public const string Contact = "Contact";
        public enum AuditAction
        {
            Insert = 0,
            Update = 1,
            Delete = 2,
            Migrate = 3
        }
        public enum AuditLogType
        {
            Common = 0,
            Account = 1
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

        public static DataTable InsertAuditLog(SqlConnection Connection, SqlTransaction Transaction, Guid?
             AuditID, string AuditSource, Guid? AuditKey, string AuditName, AuditLogType AuditLogType, AuditLog.AuditAction AuditAction, string AuditContent, string ReferenceTable,
Guid? ReferenceID, string LogXML, Guid? AuditGroupID, Guid? UserID, string Username)
        {
            using (SqlCommand cmd = new SqlCommand("", Connection))
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                {
                    StringBuilder sql = new StringBuilder();

                    sql.AppendLine("INSERT INTO AuditLog (AuditID, AuditSource, AuditKey, AuditName, AuditLogType, AuditAction, AuditContent, ReferenceTable, ReferenceID, LogXML, AuditGroupID, CreatedBy, CreatedByName, ModifiedBy, ModifiedByName) ");
                    sql.AppendLine("VALUES (@AuditID, @AuditSource, @AuditKey, @AuditName, @AuditLogType, @AuditAction, @AuditContent, @ReferenceTable, @ReferenceID, @LogXML, @AuditGroupID, @UserID, @Username, @UserID, @Username) ");

                    cmd.CommandText = sql.ToString();
                    cmd.Transaction = Transaction;
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

        internal void CreateAccountLog_MSCStatusHistory_Wizard(SqlConnection Connection, SqlTransaction Transaction, Guid? MSCStatusHistoryID, Guid? AccountID, DataRow CurrentData, DataRow NewData, Guid UserID, string UserName)
        {
            CreateLog_Wizard(Connection, Transaction, AuditLogType.Account, MSCStatusHistoryID, "MSCStatusHistory", MSCStatus, "Account", AccountID, CurrentData, NewData, UserID, UserName);
        }

        public void CreateAccountLog_Shareholder_Wizard(SqlConnection Connection, SqlTransaction Transaction, Guid? ShareholderID, Guid? AccountID, DataRow CurrentData, DataRow NewData, Guid UserID, string UserName)
        {
            CreateLog_Wizard(Connection, Transaction, AuditLogType.Account, ShareholderID, "Shareholder", CodeType.Shareholder, "Account", AccountID, CurrentData, NewData,
            UserID, UserName);
        }
        private static void CreateLog_Wizard(SqlConnection Connection, SqlTransaction Transaction, AuditLog.AuditLogType AuditLogType, Guid? AuditKey, string AuditSource, string AuditName, string ReferenceTable, Guid? ReferenceID, DataRow CurrentData, DataRow NewData, Guid? UserID, string UserName)
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
                    auditGroupID = GetLastAuditGroupID_Wizard(Connection, Transaction, AuditLogType, AuditName, AuditSource, AuditKey);
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
                    auditGroupID = GetLastAuditGroupID_Wizard(Connection, Transaction, AuditLogType, AuditName, AuditSource, AuditKey);
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
                    string xml = SerializeToXML(objContainer);
                    if (!auditGroupID.HasValue)
                        auditGroupID = Guid.NewGuid();
                    AuditLog.InsertAuditLog(Connection, Transaction, auditID, AuditSource, AuditKey, AuditName, AuditLogType, auditAction, "", ReferenceTable, ReferenceID, xml, auditGroupID, UserID, UserName);
                }

            }
        }

        internal void CreateAccountLog_General_Wizard(SqlConnection Connection, SqlTransaction Transaction, Guid? AccountID, DataRow CurrentData, DataRow NewData, Guid? UserID, string adminName)
        {
            CreateLog_Wizard(Connection, Transaction, AuditLogType.Account, AccountID, "Account", General, "Account", AccountID, CurrentData, NewData, UserID, adminName);
        }

        internal void CreateAccountLog_RelocationPlan_Wizard(SqlConnection Connection, SqlTransaction Transaction, Guid? AccountID, DataRow CurrentData, DataRow NewData, Guid UserID, string adminName)
        {
            CreateLog_Wizard(Connection, Transaction, AuditLogType.Account, AccountID, "Account", General, "Account", AccountID, CurrentData, NewData, UserID, adminName);
        }

        internal DataRow SelectAccountForLog_RelocationPlan_Wizard(SqlConnection Connection, SqlTransaction Transaction, Guid? AccountID)
        {
            using (SqlCommand cmd = new SqlCommand("", Connection))
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                {
                    StringBuilder sql = new StringBuilder();

                    sql.AppendLine("SELECT a.RequirementSpace AS [Requirement Space],");
                    sql.AppendLine("a.PlanMoveTo AS [Plan to Move to]");
                    sql.AppendLine("FROM Account a");
                    sql.AppendLine("WHERE a.AccountID = @AccountID");

                    cmd.CommandText = sql.ToString();
                    cmd.CommandTimeout = int.MaxValue;
                    cmd.Transaction = Transaction;
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
            //End Using
        }

        internal void CreateAccountLog_Portfolio_Wizard(SqlConnection Connection, SqlTransaction Transaction, Guid? AccountID, DataRow CurrentData, DataRow NewData, Guid UserID, string adminName)
        {
            CreateLog_Wizard(Connection, Transaction, AuditLogType.Account, AccountID, "Account", General, "Account", AccountID, CurrentData, NewData, UserID, adminName);
        }

        internal DataRow SelectAccountForLog_Portfolio_Wizard(SqlConnection Connection, SqlTransaction Transaction, Guid? AccountID)
        {
            using (SqlCommand cmd = new SqlCommand("", Connection))
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                {
                    StringBuilder sql = new StringBuilder();

                    sql.AppendLine("SELECT convert(varchar, a.DateOfIncorporation, 106) AS [Date Of Incorporation], ");
                    sql.AppendLine("CAST(a.OperationStatus AS varchar) AS [Operational Status],");
                    sql.AppendLine("a.Acc5YearsTax AS [Accumulated 5 Years Tax Loss to Government], ");
                    sql.AppendLine("a.LeadGenerator AS [Lead Generator], ");
                    sql.AppendLine("convert(varchar, a.LeadSubmitDate, 106) AS [Lead Submission],");
                    sql.AppendLine("CASE a.PDG WHEN 1 THEN 'Yes' WHEN 0 THEN 'No' ELSE '' END AS [PDG],");
                    sql.AppendLine("cmcr.CodeName AS [Customer Ranking], cmfi.CodeName AS [Financial Incentive],");
                    sql.AppendLine("cmbm.CodeName AS [Stock Exchange], a.CounterName AS [Counter Name],");
                    sql.AppendLine("cmeo.CodeName AS [Equity Owner], cmbc.CodeName AS [Bumi Classification],");
                    sql.AppendLine("cmc.CodeName AS [Classification], CASE a.WomanOwnCompany WHEN 1 THEN 'Yes' WHEN 0 THEN 'No' ELSE '' END AS [Woman Own Company],");
                    sql.AppendLine("cmjvc.CodeName AS [JV Category], CASE a.EXPat WHEN 1 THEN 'Yes' WHEN 0 THEN 'No' ELSE '' END AS [E-Xpat],");
                    sql.AppendLine("a.CoreActivities, dbo.GetAccountFlagshipString (a.AccountID) AS [Flagship]");
                    sql.AppendLine("FROM Account a");
                    sql.AppendLine("LEFT JOIN CodeMaster cmcr ON cmcr.CodeMasterID = a.CustomerRankingCID");
                    sql.AppendLine("LEFT JOIN CodeMaster cmfi ON cmfi.CodeMasterID = a.FinancialIncentiveCID");
                    sql.AppendLine("LEFT JOIN CodeMaster cmbm ON cmbm.CodeMasterID = a.BursaMalaysiaCID");
                    sql.AppendLine("LEFT JOIN CodeMaster cmeo ON cmeo.CodeMasterID = a.EquityOwnershipCID");
                    sql.AppendLine("LEFT JOIN CodeMaster cmbc ON cmbc.CodeMasterID = a.BumiClassificationCID");
                    sql.AppendLine("LEFT JOIN CodeMaster cmc ON cmc.CodeMasterID = a.ClassificationCID");
                    sql.AppendLine("LEFT JOIN CodeMaster cmjvc ON cmjvc.CodeMasterID = a.JVCategoryCID");
                    sql.AppendLine("WHERE a.AccountID = @AccountID");

                    cmd.CommandText = sql.ToString();
                    cmd.Transaction = Transaction;
                    cmd.CommandTimeout = int.MaxValue;
                    cmd.Parameters.AddWithValue("@AccountID", AccountID);
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    if (dataTable != null && dataTable.Rows.Count > 0)
                    {
                        if (!string.IsNullOrEmpty(dataTable.Rows[0]["Operational Status"].ToString()))
                        {
                            Console.WriteLine("359" + dataTable.Rows[0]["Operational Status"].ToString());
                            dataTable.Rows[0]["Operational Status"] = Enum.GetName(typeof(EnumSync.OperationalStatus), Convert.ToInt32(dataTable.Rows[0]["Operational Status"]));
                        }
                        return dataTable.Rows[0];
                    }
                    else {
                        return null;
                    }
                }
            }
            //End Using
        }

        internal DataRow SelectAccountForLog_General_Wizard(SqlConnection Connection, SqlTransaction Transaction, Guid? AccountID)
        {
            using (SqlCommand cmd = new SqlCommand("", Connection))
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
                    sql.AppendLine("a.Fax AS [Fax], a.WebSiteUrl AS [Web Site], a.InstitutionName AS [Institution Name], a.InstitutionType AS [Institution Type], a.InstitutionURL AS [Institution Web Site],");
                    sql.AppendLine("CASE a.OperationStatus WHEN 0 THEN 'NULL' WHEN 1 THEN 'Closed' WHEN 2 THEN 'Uncontactable' WHEN 3 THEN 'Surrendering' WHEN 4 THEN 'Revoked but unofficial' WHEN 5 THEN 'Revoked' WHEN 6 THEN 'Active' WHEN 7 THEN 'Merged' WHEN 8 THEN 'Dormant' WHEN 9 THEN 'Unincorporated' WHEN 10 THEN 'Others' WHEN 11 THEN 'Surrendered' ELSE '-' END AS [Operational Status],");
                    sql.AppendLine("cmcr.CodeName [Customer Ranking]");
                    sql.AppendLine("FROM Account a");
                    sql.AppendLine("LEFT JOIN CodeMaster cmat ON cmat.CodeMasterID = a.AccountTypeCID");
                    sql.AppendLine("LEFT JOIN CodeMaster cmct ON cmct.CodeMasterID = a.CompanyTypeCID");
                    sql.AppendLine("LEFT JOIN CodeMaster cmac ON cmac.CodeMasterID = a.AccountCategoryCID");
                    sql.AppendLine("LEFT JOIN CodeMaster cmi ON cmi.CodeMasterID = a.IndustryCID");
                    sql.AppendLine("LEFT JOIN Account pa ON pa.AccountID = a.ParentAccountID");
                    sql.AppendLine("LEFT JOIN Region rl ON rl.RegionID = a.CompanyLocationID");
                    sql.AppendLine("LEFT JOIN CodeMaster cmbs ON cmbs.CodeMasterID = a.BumiStatusCID");
                    sql.AppendLine("LEFT JOIN CodeMaster cmcr ON cmcr.CodeMasterID = a.CustomerRankingCID");
                    sql.AppendLine("WHERE a.AccountID = @AccountID");

                    cmd.CommandText = sql.ToString();
                    cmd.CommandTimeout = int.MaxValue;
                    cmd.Transaction = Transaction;
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

        public static Nullable<Guid> GetLastAuditGroupID_Wizard(SqlConnection Connection, SqlTransaction Transaction, AuditLog.AuditLogType AuditLogType, string AuditName, string AuditSource, Guid? AuditKey)
        {
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
        private static string SerializeToXML(AuditFieldContainer Value)
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

        public DataRow SelectAccountForLog_Relocation_Wizard(SqlConnection Connection, SqlTransaction Transaction, Guid? RelocationID)
        {
            using (SqlCommand cmd = new SqlCommand("", Connection))
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                {
                    StringBuilder sql = new StringBuilder();

                    sql.AppendLine("SELECT convert(varchar, r.RelocationDeadline, 106) AS [Relocation Deadline],");
                    sql.AppendLine("r.RelocationStatus AS [Relocation Status], ");
                    sql.AppendLine("cmcc.CodeName AS [Cyber Center], r.SpaceRented AS [Space Rented], ");
                    sql.AppendLine("convert(varchar, r.RelocatedDate, 106) AS [Relocated Date],");
                    sql.AppendLine("convert(varchar, r.LOADate, 106) AS [LOA Date],");
                    sql.AppendLine("convert(varchar, r.TenancyExpiryDate, 106) AS [Tenancy Expiry Date],");
                    sql.AppendLine("convert(varchar, r.NoticeOfBreach, 106) AS [Notice of Breach],");
                    sql.AppendLine("convert(varchar, r.ExtensionNotice, 106) AS [Extension Notice],");
                    sql.AppendLine("r.Location, r.Remark");
                    sql.AppendLine("FROM Relocation r");
                    sql.AppendLine("LEFT JOIN CodeMaster cmcc ON cmcc.CodeMasterID = r.CyberCenterCID");
                    sql.AppendLine("WHERE r.RelocationID = @RelocationID");

                    cmd.CommandText = sql.ToString();
                    cmd.CommandTimeout = int.MaxValue;
                    cmd.Transaction = Transaction;
                    cmd.Parameters.AddWithValue("@RelocationID", RelocationID);
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


        public void CreateAccountLog_Relocation_Wizard(SqlConnection Connection, SqlTransaction Transaction, Guid? RelocationID, Guid? AccountID, DataRow CurrentData, DataRow NewData, Guid UserID, string UserName)
        {
            CreateLog_Wizard(Connection, Transaction, AuditLogType.Account, RelocationID, "Relocation", Relocation, "Account", AccountID, CurrentData, NewData,
            UserID, UserName);
        }
        public DataRow SelectAccountForLog_FinancialAndWorkerForecast_Wizard(SqlConnection Connection, SqlTransaction Transaction, Guid? FinancialAndWorkerForecastID)
        {
            using (SqlCommand cmd = new SqlCommand("", Connection))
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                {
                    StringBuilder sql = new StringBuilder();

                    sql.AppendLine("SELECT fwf.[Year], ");
                    sql.AppendLine("fwf.Investment, fwf.RnDExpenditure AS [R&D Expenditure],");
                    sql.AppendLine("fwf.LocalSales AS [Local Sales], fwf.ExportSales AS [Export Sales],");
                    sql.AppendLine("fwf.Revenue, fwf.NetProfit AS [Net Profit], fwf.CashFlow AS [Cash Flow],");
                    sql.AppendLine("fwf.Asset, fwf.Equity, fwf.Liabilities, fwf.LocalKW AS [Local KW],");
                    sql.AppendLine("fwf.ForeignKW AS [Foreign KW], fwf.LocalWorker AS [Local Worker], ");
                    sql.AppendLine("fwf.ForeignWorker AS [Foreign Worker]");
                    sql.AppendLine("FROM FinancialAndWorkerForecast fwf");
                    sql.AppendLine("WHERE fwf.FinancialAndWorkerForecastID = @FinancialAndWorkerForecastID");

                    cmd.CommandText = sql.ToString();
                    cmd.CommandTimeout = int.MaxValue;
                    cmd.Transaction = Transaction;
                    cmd.Parameters.AddWithValue("@FinancialAndWorkerForecastID", FinancialAndWorkerForecastID);
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

        public void CreateAccountLog_FinancialAndWorkerForecast_Wizard(SqlConnection Connection, SqlTransaction Transaction, Guid? FinancialAndWorkerForecastID, Guid? AccountID, DataRow CurrentData, DataRow NewData, Guid UserID, string UserName)
        {
            CreateLog_Wizard(Connection, Transaction, AuditLogType.Account, FinancialAndWorkerForecastID, "FinancialAndWorkerForecast", FinancialAndWorkerForecast, "Account", AccountID, CurrentData, NewData,
            UserID, UserName);
        }
        public DataRow SelectAccountForLog_Shareholder_Wizard(SqlConnection Connection, SqlTransaction Transaction, Guid? ShareholderID)
        {
            using (SqlCommand cmd = new SqlCommand("", Connection))
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                {
                    StringBuilder sql = new StringBuilder();

                    sql.AppendLine("SELECT s.ShareholderName AS [Shareholder Name], s.Percentage AS [Percentage Hold],");
                    sql.AppendLine("CASE s.BumiShare WHEN 1 THEN 'Yes' WHEN 0 THEN 'No' ELSE '' END AS [Bumi Status],");
                    sql.AppendLine("r.RegionName AS [Country], CASE s.Status WHEN 1 THEN 'Active' WHEN 0 THEN 'Inactive' ELSE '' END AS [Status]");
                    sql.AppendLine("FROM Shareholder s");
                    sql.AppendLine("LEFT JOIN Region r ON r.RegionID = s.CountryRegionID");
                    sql.AppendLine("WHERE s.ShareholderID = @ShareholderID");

                    cmd.CommandText = sql.ToString();
                    cmd.CommandTimeout = int.MaxValue;
                    cmd.Transaction = Transaction;
                    cmd.Parameters.AddWithValue("@ShareholderID", ShareholderID);
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
        public DataRow SelectAccountForLog_Cluster_Wizard(SqlConnection Connection, SqlTransaction Transaction, Guid? AccountClusterID)
        {
            using (SqlCommand cmd = new SqlCommand("", Connection))
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                {
                    StringBuilder sql = new StringBuilder();

                    sql.AppendLine("SELECT dbo.Cluster_GetFullCluster(ac.ClusterID, '\\') AS Cluster");
                    sql.AppendLine("FROM AccountCluster ac");
                    sql.AppendLine("WHERE ac.AccountClusterID = @AccountClusterID");

                    cmd.CommandText = sql.ToString();
                    cmd.CommandTimeout = int.MaxValue;
                    cmd.Transaction = Transaction;
                    cmd.Parameters.AddWithValue("@AccountClusterID", AccountClusterID);
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
        public void CreateAccountLog_Cluster_Wizard(SqlConnection Connection, SqlTransaction Transaction, Guid? AccountClusterID, Guid? AccountID, DataRow CurrentData, DataRow NewData, Guid UserID, string UserName)
        {
            CreateLog_Wizard(Connection, Transaction, AuditLogType.Account, AccountClusterID, "AccountCluster", Cluster, "Account", AccountID, CurrentData, NewData,
            UserID, UserName);
        }

        public DataRow SelectAccountForLog_EEManager_Wizard(SqlConnection Connection, SqlTransaction Transaction, Guid? AccountManagerAssignmentID)
        {
            using (SqlCommand cmd = new SqlCommand("", Connection))
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                {
                    StringBuilder sql = new StringBuilder();

                    sql.AppendLine("SELECT ama.EEManagerName AS [Account Manager], ");
                    sql.AppendLine("convert(varchar, ama.AssignmentDate, 106) AS [Created Date]");
                    sql.AppendLine("FROM AccountManagerAssignment ama");
                    sql.AppendLine("WHERE ama.AccountManagerAssignmentID = @AccountManagerAssignmentID");

                    cmd.CommandText = sql.ToString();
                    cmd.CommandTimeout = int.MaxValue;
                    cmd.Transaction = Transaction;
                    cmd.Parameters.AddWithValue("@AccountManagerAssignmentID", AccountManagerAssignmentID);
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
        public void CreateAccountLog_BusinessAnalyst_Wizard(SqlConnection Connection, SqlTransaction Transaction, Guid? AccountManagerAssignmentID, Guid? AccountID, DataRow CurrentData, DataRow NewData, Guid UserID, string UserName)
        {
            CreateLog_Wizard(Connection, Transaction, AuditLogType.Account, AccountManagerAssignmentID, "AccountManagerAssignment", BusinessAnalyst, "Account", AccountID, CurrentData, NewData,
            UserID, UserName);
        }

        public DataRow SelectAccountForLog_EEManager(Guid? AccountManagerAssignmentID)
        {
            using (SqlConnection conn = SQLHelper.GetConnection())
            {
                //conn.Open();
                using (SqlCommand cmd = new SqlCommand("", conn))
                {
                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        StringBuilder sql = new StringBuilder();

                        sql.AppendLine("SELECT ama.EEManagerName AS [Account Manager], ");
                        sql.AppendLine("convert(varchar, ama.AssignmentDate, 106) AS [Created Date]");
                        sql.AppendLine("FROM AccountManagerAssignment ama");
                        sql.AppendLine("WHERE ama.AccountManagerAssignmentID = @AccountManagerAssignmentID");

                        cmd.CommandText = sql.ToString();
                        cmd.CommandTimeout = int.MaxValue;
                        cmd.Parameters.AddWithValue("@AccountManagerAssignmentID", AccountManagerAssignmentID);
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
        public DataRow SelectAccountForLog_Address_Wizard(SqlConnection Connection, SqlTransaction Transaction, Guid? AddressID)
        {
            using (SqlCommand cmd = new SqlCommand("", Connection))
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                {
                    StringBuilder sql = new StringBuilder();

                    sql.AppendLine("SELECT cmat.CodeName AS [Address Type], ");
                    sql.AppendLine("ISNULL(a.Address1, '') + ' ' + ISNULL(a.Address2, '') + ' ' + ISNULL(a.Address3, '') AS [Address],");
                    sql.AppendLine("a.Postcode, a.City, a.State, dbo.GetFormattedPhoneNo(a.BusinessPhoneCC, a.BusinessPhone, a.BusinessPhoneExt) AS [Business Phone],");
                    sql.AppendLine("a.OtherBusinessPhone AS [Other Business Phone], dbo.GetFormattedPhoneNo(a.FaxCC, a.Fax, NULL) Fax,");
                    sql.AppendLine("a.OtherFax AS [Other Fax], r.RegionName AS [Country],");
                    sql.AppendLine("cmcc.CodeName AS [Cyber Center]");
                    sql.AppendLine("FROM Address a");
                    sql.AppendLine("LEFT JOIN Region r ON r.RegionID = a.CountryRegionID");
                    sql.AppendLine("LEFT JOIN CodeMaster cmat ON cmat.CodeMasterID = a.AddressTypeID");
                    sql.AppendLine("LEFT JOIN CodeMaster cmcc ON cmcc.CodeMasterID = a.CyberCenterCID");
                    sql.AppendLine("WHERE a.AddressID = @AddressID");

                    cmd.CommandText = sql.ToString();
                    cmd.CommandTimeout = int.MaxValue;
                    cmd.Transaction = Transaction;
                    cmd.Parameters.AddWithValue("@AddressID", AddressID);
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

        public void CreateAccountLog_Address_Wizard(SqlConnection Connection, SqlTransaction Transaction, Guid? AddressID, Guid? AccountID, DataRow CurrentData, DataRow NewData, Guid UserID, string UserName)
        {
            CreateLog_Wizard(Connection, Transaction, AuditLogType.Account, AddressID, "Address", Address, "Account", AccountID, CurrentData, NewData,
            UserID, UserName);
        }
        public void CreateAccountLog_Contact_Wizard(SqlConnection Connection, SqlTransaction Transaction, Guid? ContactID, Guid? AccountID, DataRow CurrentData, DataRow NewData, Guid UserID, string UserName)
        {
            AuditLog.CreateLog_Wizard(Connection, Transaction, AuditLogType.Account, ContactID, "Contact", Contact, "Account", AccountID, CurrentData, NewData,
            UserID, UserName);
        }

        public string Add(string Table, string KeyColumn, string KeyValue, string Desc, AuditAction Action, string UserID)
        {
            AccelTeam.Utilities.Data Data = new AccelTeam.Utilities.Data(ConfigurationSettings.AppSettings["CRM"].ToString());

            DataTable dt = Data.Select(Table, "*", KeyColumn, KeyValue, "");
            string AuditID = "";

            if (dt.Rows.Count > 0)
            {
                string Content = "";
                foreach (DataColumn dc in dt.Columns)
                {
                    Content += dc.ColumnName + "\t" + dt.Rows[0][dc].ToString() + "\r\n";
                }

                AuditID = Guid.NewGuid().ToString();
                string[] col = {
            "AuditID",
            "AuditSource",
            "AuditKey",
            "AuditName",
            "AuditAction",
            "AuditContent",
            "CreatedBy",
            "ModifiedBy"
        };
                object[] val = new object[] {
            AuditID,
            Table,
            KeyValue,
            Desc,
            Action,
            Content,
            UserID,
            UserID
        };
                Data.Insert("AuditLog", col, val);
            }

            return AuditID;
        }

    }
}
