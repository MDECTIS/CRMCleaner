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
    class EmailCleanUp
    {
        internal void Start()
        {
            DataTable dtUpdateEmail = getDTStruc();
            DataTable dtEmailCleanList = getAllEmail();
        }

        private DataTable getDTStruc()
        {
            DataTable output = new DataTable();
            output.Columns.Add("CompanyName", typeof(string));
            output.Columns.Add("MSCFileID", typeof(string));
            output.Columns.Add("EmailOld", typeof(string));
            output.Columns.Add("EmailNew", typeof(string));
            return output;
        }

        private DataTable getAllEmail()
        {
            DataTable output = new DataTable();
            using (SqlConnection Connection = SQLHelper.GetConnection())
            {
                SqlTransaction Transaction = default(SqlTransaction);
                Transaction = Connection.BeginTransaction("getAllInactiveShareHolder");
                using (SqlCommand com = new SqlCommand("", Connection))
                {
                    using (SqlDataAdapter adapter  =new SqlDataAdapter(com))

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
    }
}
