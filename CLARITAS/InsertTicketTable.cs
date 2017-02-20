using CRMCleaner.Classes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CRMCleaner.CLARITAS
{
    class InsertTicketTable
    {
        internal static void Start()
        {
            //GET DATA FROM ORIGINAL TABLE
            DataTable dtTicket = getTicketRecords();
            Console.WriteLine("Rows Counted : " + dtTicket.Rows.Count);
            //INSERT INTO TICKET1
            foreach (DataRow dr in dtTicket.Rows)
            {
                InsertDataIntoTable(dr);
            }
        }

        private static void InsertDataIntoTable(DataRow dr)
        {
            using (SqlConnection Connection = SQLHelper.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand("", Connection))
                {
                    StringBuilder sql = new StringBuilder();
                    sql.AppendLine("SET IDENTITY_INSERT Ticket1 ON ");
                    sql.AppendLine("INSERT INTO Ticket1 (TicketID,ContactName,CompanyName,RequestorName,RequestorCompanyName,");
                    sql.AppendLine("RequestorDepartment,RequestorPosition,RequestorBusinessPhone,RequestorMobilePhone,RequestorEmail,");
                    sql.AppendLine("Subject,Description,TicketChannel,TicketCategory,TicketSubCategory,SubServices,TicketRelated,TicketPriority,TicketComplexity,");
                    sql.AppendLine("TicketStatus,AssignedAgent,CreatedDate,ModifiedDate,CreatedByName,ModifiedByName,RequestNo,RequestDate,RequestorUID,WorkflowProcessID,");
                    sql.AppendLine("RespondByDate,InteractionID,WorkflowProfile,OwnerID,EscalateByUserID,EscalateToEntityID,EscalateToUserID,ExtendDays,RespondDate,");
                    sql.AppendLine("WorkflowStartDateTime,BusinessUnitID,AppSource,AppDateTime)");
                    sql.AppendLine("VALUES (" );
                    sql.AppendLine("@TicketID, @ContactName, @CompanyName, @RequestorName, @RequestorCompanyName, ");
                    sql.AppendLine("@RequestorDepartment,@RequestorPosition,@RequestorBusinessPhone,@RequestorMobilePhone,@RequestorEmail,");
                    sql.AppendLine("@Subject,@Description,@TicketChannel,@TicketCategory,@TicketSubCategory,@SubServices,@TicketRelated,@TicketPriority,@TicketComplexity,");
                    sql.AppendLine("@TicketStatus,@AssignedAgent,@CreatedDate,@ModifiedDate,@CreatedByName,@ModifiedByName,@RequestNo,@RequestDate,@RequestorUID,@WorkflowProcessID,");
                    sql.AppendLine("@RespondByDate,@InteractionID,@WorkflowProfile,@OwnerID,@EscalateByUserID,@EscalateToEntityID,@EscalateToUserID,@ExtendDays,@RespondDate,");
                    sql.AppendLine("@WorkflowStartDateTime,@BusinessUnitID,@AppSource,@AppDateTime");
                    sql.AppendLine(" )");
                    //No4
                    cmd.CommandText = sql.ToString();
                    cmd.CommandTimeout = int.MaxValue;
                    cmd.Parameters.AddWithValue("@TicketID", dr["TicketID"]);
                    cmd.Parameters.AddWithValue("@ContactName", dr["ContactName"]);
                    cmd.Parameters.AddWithValue("@CompanyName", dr["CompanyName"]);
                    cmd.Parameters.AddWithValue("@RequestorName", dr["RequestorName"]);
                    cmd.Parameters.AddWithValue("@RequestorCompanyName", dr["RequestorCompanyName"]);
                    cmd.Parameters.AddWithValue("@RequestorDepartment", dr["RequestorDepartment"]);
                    cmd.Parameters.AddWithValue("@RequestorPosition", dr["RequestorPosition"]);
                    cmd.Parameters.AddWithValue("@RequestorBusinessPhone", dr["RequestorBusinessPhone"]);
                    cmd.Parameters.AddWithValue("@RequestorMobilePhone", dr["RequestorMobilePhone"]);
                    cmd.Parameters.AddWithValue("@RequestorEmail", dr["RequestorEmail"]);
                    cmd.Parameters.AddWithValue("@Subject", dr["Subject"]);
                    cmd.Parameters.AddWithValue("@Description", dr["Description"]);
                    cmd.Parameters.AddWithValue("@TicketChannel", dr["TicketChannel"]);
                    cmd.Parameters.AddWithValue("@TicketCategory", dr["TicketCategory"]);
                    cmd.Parameters.AddWithValue("@TicketSubCategory", dr["TicketSubCategory"]);
                    cmd.Parameters.AddWithValue("@SubServices", dr["SubServices"]);
                    cmd.Parameters.AddWithValue("@TicketRelated", dr["TicketRelated"]);
                    cmd.Parameters.AddWithValue("@TicketPriority", dr["TicketPriority"]);
                    cmd.Parameters.AddWithValue("@TicketComplexity", dr["TicketComplexity"]);
                    cmd.Parameters.AddWithValue("@TicketStatus", dr["TicketStatus"]);
                    cmd.Parameters.AddWithValue("@AssignedAgent", dr["AssignedAgent"]);
                    cmd.Parameters.AddWithValue("@CreatedDate", dr["CreatedDate"]);
                    cmd.Parameters.AddWithValue("@ModifiedDate", dr["ModifiedDate"]);
                    cmd.Parameters.AddWithValue("@CreatedByName", dr["CreatedByName"]);
                    cmd.Parameters.AddWithValue("@ModifiedByName", dr["ModifiedByName"]);
                    cmd.Parameters.AddWithValue("@RequestNo", dr["RequestNo"]);
                    cmd.Parameters.AddWithValue("@RequestDate", dr["RequestDate"]);
                    cmd.Parameters.AddWithValue("@RequestorUID", dr["RequestorUID"]);
                    cmd.Parameters.AddWithValue("@WorkflowProcessID", dr["WorkflowProcessID"]);
                    cmd.Parameters.AddWithValue("@RespondByDate", dr["RespondByDate"]);
                    cmd.Parameters.AddWithValue("@InteractionID", dr["InteractionID"]);
                    cmd.Parameters.AddWithValue("@WorkflowProfile", dr["WorkflowProfile"]);
                    cmd.Parameters.AddWithValue("@OwnerID", dr["OwnerID"]);
                    cmd.Parameters.AddWithValue("@EscalateByUserID", dr["EscalateByUserID"]);
                    cmd.Parameters.AddWithValue("@EscalateToEntityID", dr["EscalateToEntityID"]);
                    cmd.Parameters.AddWithValue("@EscalateToUserID", dr["EscalateToUserID"]);
                    cmd.Parameters.AddWithValue("@ExtendDays", dr["ExtendDays"]);
                    cmd.Parameters.AddWithValue("@RespondDate", dr["RespondDate"]);
                    cmd.Parameters.AddWithValue("@WorkflowStartDateTime", dr["WorkflowStartDateTime"]);
                    cmd.Parameters.AddWithValue("@BusinessUnitID", dr["BusinessUnitID"]);
                    cmd.Parameters.AddWithValue("@AppSource", dr["AppSource"]);
                    cmd.Parameters.AddWithValue("@AppDateTime", dr["AppDateTime"]);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private static DataTable getTicketRecords()
        {
            DataTable output = new DataTable();
            using (SqlConnection Connection = SQLHelper.GetConnection())
            {
                using (SqlCommand com = new SqlCommand("", Connection))
                {
                    using (SqlDataAdapter adapter = new SqlDataAdapter(com))
                    {

                        System.Text.StringBuilder sql = new System.Text.StringBuilder();
                        sql.AppendLine("SELECT  t.[TicketID], c.Name as ContactName, (Select AccountName from Account where AccountID = c.AccountID) as CompanyName");
                        sql.AppendLine(",t.[RequestorName], t.[RequestorCompanyName] ,t.[RequestorDepartment] ,t.[RequestorPosition] ,t.[RequestorBusinessPhone] ,t.[RequestorMobilePhone]");
                        sql.AppendLine(",t.[RequestorEmail]  , t.[Subject]  ,t.[Description] ,(Select CodeName from CodeMAster where CodeMasterID = t.[TicketChannelCID]) as TicketChannel");
                        sql.AppendLine(",(Select CodeName from CodeMAster where CodeMasterID = t.[TicketCategoryCID] ) as TicketCategory, (Select CodeName from CodeMAster where CodeMasterID = t.[TicketSubCategoryCID]) as TicketSubCategory");
                        sql.AppendLine(",(Select CodeName from CodeMAster where CodeMasterID = t.[SubServicesCID]) as SubServices      , (Select CodeName From CodeMaster where CodeMasterID = t.[TicketRelatedCID]) as TicketRelated");
                        sql.AppendLine(",(Select CodeName from CodeMAster where CodeMasterID = t.[TicketPriorityCID]) as TicketPriority, (Select CodeName from CodeMAster where CodeMasterID = t.[TicketComplexityCID]) as TicketComplexity");
                        sql.AppendLine(",(Select CodeName from CodeMAster where CodeMasterID = t.[TicketStatusCID]) as TicketStatus , (Select CodeName from CodeMAster where CodeMasterID = t.[AssignedAgentUID]) as AssignedAgent");
                        sql.AppendLine(",t.[CreatedDate] , t.[ModifiedDate] ,t.[CreatedByName],t.[ModifiedByName],t.[RequestNo] ,t.[RequestDate],t.[RequestorUID]");
                        sql.AppendLine(",t.[WorkflowProcessID] ,t.[RespondByDate], t.[InteractionID],(Select WorkflowName FROM WorkflowProfile where WorkflowProfileID = t.[WorkflowProfileID]) As WorkflowProfile");
                        sql.AppendLine(", t.[OwnerID], t.[EscalateByUserID],t.[EscalateToEntityID],t.[EscalateToUserID],t.[ExtendDays] ,t.[RespondDate] ,t.[WorkflowStartDateTime] ,t.[BusinessUnitID],t.[AppSource],t.[AppDateTime]  ");
                        sql.AppendLine("FROM [CRM_PRD].[dbo].[Ticket] t");
                        sql.AppendLine("LEFT JOIN Contact c on c.ContactID=t.ContactID");
                        sql.AppendLine("where c.AccountID is NOT NULL");
                        sql.AppendLine(" Order By [CreatedDate] DESC");
                        com.CommandText = sql.ToString();
                        com.CommandType = CommandType.Text;
                        com.Connection = Connection;
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
