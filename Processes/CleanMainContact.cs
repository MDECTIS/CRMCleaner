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
    class CleanMainContact
    {
        internal void Start()
        {
            DataTable dtContactDV = getData();
            foreach (DataRow row in dtContactDV.Rows)
            {
                Guid AccountDVID = new Guid(row["AccountDVID"].ToString());
                string Name = row["Name"].ToString();
                if (ExistedInMainTable(AccountDVID, Name))
                {
                    DeleteDataInMainTable(AccountDVID, Name);
                }
            }
        }

        private void DeleteDataInMainTable(Guid accountDVID, string name)
        {
         
        }

        private bool ExistedInMainTable(Guid AccountDVID, string Name)
        {
            bool Existed = false;
            using (SqlConnection conn = new SqlConnection(ConfigurationSettings.AppSettings["CRM"].ToString()))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand("", conn))
                {
                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        StringBuilder sql = new StringBuilder();
                        sql.AppendLine("Select TOP 1 * FROM Contact ");
                        sql.AppendLine("WHERE AccountID = @AccountDVID");
                        sql.AppendLine("AND Name = @Name");
                        cmd.CommandText = sql.ToString();
                        cmd.CommandType = CommandType.Text;
                        cmd.Connection = conn;

                        cmd.Parameters.Add(new SqlParameter("@AccountDVID", AccountDVID));
                        cmd.Parameters.Add(new SqlParameter("@Name", Name));

                      int Rows=  cmd.ExecuteNonQuery();
                        if (Rows > 0)
                            Existed = true;
                    }
                }
            }
            return Existed;
        }

        private DataTable getData()
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

                        sql.AppendLine("Select cdv.AccountDVID,cdv.Name From ContactDV cdv");
                        sql.AppendLine("INNER JOIN AccountDV adv  on adv.AccountDVID = cdv.AccountDVID");
                        sql.AppendLine("Where  cdv.DataSource='WIZ' AND adv.AccountID in (Select AccountID From Account Where MSCFileID in ('CS/3/8216',");
                        sql.AppendLine("'CS/3/8412','CS/3/1320','CS/3/6850','CS/3/8804','CS/3/7472','CS/3/2106','CS/3/2098','CS/3/5288','CS/3/5356',");
                        sql.AppendLine("'CS/3/3372','CS/3/6528','CS/3/8892','CS/3/6687','CS/3/3109','CS/3/7553','CS/3/8951','CS/3/4719','CS/3/7649',");
                        sql.AppendLine("'CS/3/9044','CS/3/6147','CS/3/338','CS/3/9019'))");

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
    }
}
