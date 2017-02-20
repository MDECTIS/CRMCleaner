using CRMCleaner.Classes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CRMCleaner.Processes
{
    class MSCHistoryStatusUpdate
    {
        internal static void Start()
        {
            string MSCFileIDList = "'CS/3/9988','MSC-INC/137/66','CS/3/10374','KWD-4/174','CS/3/10029','CS/3/10032','CS/3/10183','CS/3/10208',";
            MSCFileIDList += "'CS/3/10226','CS/3/10253','CS/3/10263','CS/3/10272','CS/3/10306','CS/3/10327','CS/3/8851','CS/3/9082','CS/3/9110',";
            MSCFileIDList += "'CS/3/9204','CS/3/9390','CS/3/9651','CS/3/9778','CS/3/9804','CS/3/9828','CS/3/9839','CS/3/9848','CS/3/9898','CS/3/9912',";
            MSCFileIDList += "'CS/3/9920','CS/3/9922','CS/3/9926','CS/3/9989','CS/3/10007','CS/3/10012','CS/3/10013','CS/3/10148','CS/3/10243','CS/3/5426',";
            MSCFileIDList += "'CS/3/8482','CS/3/9236','CS/3/9339','CS/3/9573','CS/3/9772','CS/3/9814','CS/3/9896','CS/3/9940','MSC-INC/137/64','MSC-INC/137/37'";

            string[] MSCFileIDListing = MSCFileIDList.Split(',');

            foreach (string mscfileid in MSCFileIDListing)
            {
                string AccountID = getAccountIDByMSCFileID(mscfileid);
                if (AccountID != "")
                    InsertApprovalLetterDate(AccountID);
            }
        }

        private static string getAccountIDByMSCFileID(string mscfileid)
        {
            using (SqlConnection conn = SQLHelper.GetConnection())
            {
                StringBuilder sql = new StringBuilder();
                sql.AppendLine("SELECT AccountID FROM dbo.Account WHERE MSCFileID=" + mscfileid);
                using (SqlCommand cmd = new SqlCommand(sql.ToString(), conn))
                {

                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        if (dt != null && dt.Rows.Count > 0)
                        {
                            return dt.Rows[0][0].ToString();
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
            }
        }

        private static void InsertApprovalLetterDate(string AccountID)
        {
            using (SqlConnection conn = SQLHelper.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand("", conn))
                {
                    StringBuilder sql = new StringBuilder();
                    sql.AppendLine("INSERT INTO MSCStatusHistory (MSCStatusHistoryID, AccountID,MSCApprovalStatusCID,MSCApprovalDate,DataSource,CreatedBy,CreatedByName,CreatedDate,ModifiedBy,ModifiedByName,ModifiedDate)");
                    sql.AppendLine("VALUES(");
                    sql.AppendLine("NEWID(),");
                    //AccountID
                    sql.AppendLine("'" + AccountID + "',");
                    //AC Meeting:
                    //'4f6958cf-a21a-4ae6-88de-3b44061d3de6'
                    //Approval Letter:
                    //'4EDCCE9C-9EB8-467D-9982-04C63642F8C3'
                    sql.AppendLine("'4EDCCE9C-9EB8-467D-9982-04C63642F8C3',");
                    sql.AppendLine("'2016-11-30 00:00:00.000',");
                    sql.AppendLine("NULL,");
                    sql.AppendLine("'74425431-65A4-498E-A6ED-910A9E20B6FC',");
                    sql.AppendLine("'admin',");
                    sql.AppendLine("GETDATE(), ");
                    sql.AppendLine("'74425431-65A4-498E-A6ED-910A9E20B6FC',");
                    sql.AppendLine("'admin',");
                    sql.AppendLine("GETDATE())");

                    cmd.CommandText = sql.ToString();
                    cmd.CommandType = CommandType.Text;
                    cmd.Connection = conn;


                    try
                    {
                        cmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        throw;
                    }
                }
            }
        }
    }
}
