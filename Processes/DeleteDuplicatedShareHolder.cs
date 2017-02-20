using CRMCleaner.Classes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CRMCleaner.Processes
{
    class DeleteDuplicatedShareHolder
    {
        internal void Start()
        {
            DataTable dtDuplicateListing = getDuplicateList();
            int RowCounted = dtDuplicateListing.Rows.Count;
            Console.WriteLine(string.Format("Record found {0}", RowCounted));
            string preName = "";
            string prevAccountID = "";
            string prevPercent = "";
            int Counter = 0;
            int RecordCounter = 1;
            foreach (DataRow dr in dtDuplicateListing.Rows)
            {
                string ShareHolderID = dr["ShareHolderID"].ToString();
                string nxtName = dr["ShareholderName"].ToString();
                string nxtAccountID = dr["AccountID"].ToString();
                string nxtPercent= dr["Percentage"].ToString();
                if (prevAccountID != nxtAccountID)
                {
                    Counter = 0;
                    preName = "";
                    prevAccountID = "";
                    prevPercent = "";
                }
                if (Counter == 0)
                {
                    preName = dr["ShareholderName"].ToString();
                    prevAccountID = dr["AccountID"].ToString();
                    prevPercent = dr["Percentage"].ToString();
                }
                else
                {
                    if (prevAccountID == nxtAccountID && preName.ToUpper().Trim() == nxtName.ToUpper().Trim() && prevPercent.Trim() == nxtPercent.Trim())
                    {
                        using (SqlConnection Connection = SQLHelper.GetConnection())
                        {
                            SqlTransaction Transaction = default(SqlTransaction);
                            Transaction = Connection.BeginTransaction("DeleteDuplicateShareHolder");
                            try
                            {
                                DeleteDuplicate(Connection, Transaction, ShareHolderID);
                                Console.WriteLine(string.Format("Record Deleted {0}/{1}  for {2} and ShareHolderID [{3}]", RecordCounter, RowCounted, nxtName, ShareHolderID));
                                Transaction.Commit();
                                //Counter = 0;
                                //preName = "";
                                //prevAccountID = "";
                                //prevPercent = "";
                                //RecordCounter++;
                                //continue;
                            }
                            catch(Exception ex)
                            {

                            }
                        }
                    }
                }


                Counter++;
                RecordCounter++;
            }
        }

        private DataTable getDuplicateList()
        {
            using (SqlConnection Connection = SQLHelper.GetConnection())
            {
                SqlTransaction Transaction = default(SqlTransaction);
                Transaction = Connection.BeginTransaction("GetAllDuplicateShareHolder");
                SqlCommand com = new SqlCommand();
                SqlDataAdapter ad = new SqlDataAdapter(com);

                System.Text.StringBuilder sql = new System.Text.StringBuilder();
                sql.AppendLine("SELECT s.AccountID,s.ShareHolderID,s.ShareholderName,s.Percentage,s.BumiShare,s.Status,(Select r.RegionName from Region r where r.RegionID = s.CountryRegionID) as Country ");
                sql.AppendLine("FROM ShareHolder s ");
                sql.AppendLine("WHERE ShareholderName IN (SELECT ShareholderName FROM ShareHolder GROUP BY ShareholderName HAVING COUNT(1) > 1) AND s.ShareholderName <> '-' AND Status=1");
                sql.AppendLine("ORDER BY s.ShareholderName");
                com.CommandText = sql.ToString();
                com.CommandType = CommandType.Text;
                com.Connection = Connection;
                com.Transaction = Transaction;
                com.CommandTimeout = int.MaxValue;
                try
                {
                    DataTable dt = new DataTable();
                    ad.Fill(dt);

                    return dt;
                }
                catch (Exception ex)
                {
                    throw;
                }
                finally
                {
                    Connection.Close();
                }
            }
        }

        private void DeleteDuplicate(SqlConnection Connection, SqlTransaction Transaction, string ShareHolderID)
        {
            using (SqlCommand cmd = new SqlCommand("", Connection))
            {

                int affectedRows = 0;
                StringBuilder sql = new StringBuilder();
                sql.AppendLine("DELETE FROM ShareHolder WHERE ShareHolderID = @ShareHolderID");
                cmd.CommandText = sql.ToString();
                cmd.CommandTimeout = int.MaxValue;
                cmd.Transaction = Transaction;
                cmd.Parameters.AddWithValue("@ShareHolderID", ShareHolderID);
                affectedRows = cmd.ExecuteNonQuery();
            }
        }
    }
}
