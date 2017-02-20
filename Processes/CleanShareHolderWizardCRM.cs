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
    class CleanShareHolderWizardCRM
    {
        internal void Start()
        {
            //1. Remove Duplicated ShareHolder
            //2. Get Latest ShareHolder from Wizard
            //3. Find match shareholder between Wizard n CRM and ACTIVE back in CRM
            //4. If ACTIVE ShareHolder in Wizard not existed in CRM, add new data in CRM
            ArrayList MSCFileList = getMSCFileID();
            Console.WriteLine(string.Format("Total Company to Clean : {0}",MSCFileList.Count));
            foreach (string MSCFileID in MSCFileList)
            {
                using (SqlConnection Connection = SQLHelper.GetConnection())
                {
                    SqlTransaction Transaction = default(SqlTransaction);
                    try
                    {
                        DataTable dtShareHolder = GetShareHolder(Connection, Transaction, MSCFileID);
                        int Counter = 0;
                        foreach (DataRow dr in dtShareHolder.Rows)
                        {
                            Guid AccountID = GetAccountIDByFileID(Connection, Transaction, MSCFileID);
                            string strShareName = dr["OwnershipSHName"].ToString();
                            string strPercentage = dr["OwnershipPer"].ToString();
                            string RegionID = getRegionID(dr["OwnershipCName"].ToString());
                            string ShareHolderID = "";
                            Console.WriteLine(string.Format(" ShareHolder : {0} || Percentage : {1} || Country : {2}",strShareName,strPercentage, dr["OwnershipCName"].ToString()));
                            if (ShareHolderExisted(AccountID.ToString(), strShareName, strPercentage, RegionID, out ShareHolderID)&&ShareHolderID!="")
                            {
                              
                                Transaction = Connection.BeginTransaction("WizardSync");
                                UpdateShareHolderStatustoActive(Connection, Transaction, ShareHolderID);
                                Transaction.Commit();
                                Console.WriteLine(string.Format("UPDATED for {0}", strShareName));
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        Transaction.Rollback();
                    }
                }
            }


        }
        public static Guid GetAccountIDByFileID(SqlConnection Connection, SqlTransaction Transaction, string FileID)
        {
            Guid output = new Guid();
            SqlCommand com = new SqlCommand();
            SqlDataAdapter ad = new SqlDataAdapter(com);

            System.Text.StringBuilder sql = new System.Text.StringBuilder();
            sql.AppendLine("SELECT AccountID FROM Account WHERE MSCFileID = @FileID");

            com.CommandText = sql.ToString();
            com.CommandType = CommandType.Text;
            com.Connection = Connection;
            com.Transaction = Transaction;
            com.CommandTimeout = int.MaxValue;
            try
            {
                com.Parameters.Add(new SqlParameter("@FileID", FileID));

                DataTable dt = new DataTable();
                ad.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    output= new Guid(dt.Rows[0][0].ToString());
                }
            }
            catch (Exception ex)
            {

            }
            return output;
        }
        private string getRegionID(string CountryName)
        {
            string CountryRegionID = "";
            using (SqlConnection conn = SQLHelper.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand("", conn))
                {
                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        StringBuilder sql = new StringBuilder();
                        sql.AppendLine("SELECT RegionID");
                        sql.AppendLine("FROM Region");
                        sql.AppendLine("WHERE UPPER(RegionName) = @CountryName ");
                        cmd.CommandText = sql.ToString();
                        cmd.Parameters.AddWithValue("@CountryName", CountryName.Trim().ToUpper());
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        if (dt.Rows.Count > 0)
                        {
                            return CountryRegionID= dt.Rows[0][0].ToString();
                        }
                    }
                }
            }
            return CountryRegionID;
        }

        private bool ShareHolderExisted(string strAccountID, string strShareName, string strPercentage, string RegionID, out string ShareHolderID)
        {
            bool Existed = false;
            ShareHolderID = "";
            using (SqlConnection conn = SQLHelper.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand("", conn))
                {
                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        StringBuilder sql = new StringBuilder();
                        sql.AppendLine("SELECT TOP 1 *");
                        sql.AppendLine(" FROM  ShareHolder");
                        sql.AppendLine("WHERE AccountID =@AccountID AND UPPER(ShareholderName)=@ShareholderName AND Percentage=@Percentage AND CountryRegionID=@CountryRegionID");
                        cmd.CommandText = sql.ToString();
                        cmd.Parameters.AddWithValue("@AccountID", strAccountID);
                        cmd.Parameters.AddWithValue("@ShareholderName", strShareName.Trim().ToUpper());
                        cmd.Parameters.AddWithValue("@Percentage", strPercentage.Trim());
                        cmd.Parameters.AddWithValue("@CountryRegionID", RegionID.Trim());
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        if (dt.Rows.Count > 0)
                        {
                            ShareHolderID = dt.Rows[0][0].ToString();
                            ; return true;
                        }
                    }
                }
            }
            return Existed;
        }

        private void UpdateShareHolderStatustoActive(SqlConnection Connection, SqlTransaction Transaction, string ShareHolderID)
        {
            using (SqlCommand cmd = new SqlCommand("", Connection))
            {
                try
                {
                    StringBuilder sql = new StringBuilder();
                    sql.AppendLine("UPDATE ShareHolder");
                    sql.AppendLine("SET Status = 1");
                    sql.AppendLine("WHERE ShareHolderID =@ShareHolderID");
                    cmd.CommandText = sql.ToString();
                    cmd.CommandTimeout = int.MaxValue;
                    cmd.Transaction = Transaction;
                    cmd.Parameters.AddWithValue("@ShareHolderID", ShareHolderID);
                    int affectedRows = 0;
                    affectedRows = cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {

                }
                   
            }
        }

        private ArrayList getMSCFileID()
        {
            ArrayList output = new ArrayList();
            output.Add("CS/3/338");
            output.Add("CS/3/5288");
            output.Add("CS/3/7649");
            output.Add("CS/3/8892");
            output.Add("CS/3/4719");
            output.Add("CS/3/2106");
            output.Add("CS/3/8412");
            output.Add("CS/3/7472");
            output.Add("CS/3/6687");
            output.Add("CS/3/7553");
            output.Add("CS/3/5356");
            output.Add("CS/3/2098");
            output.Add("CS/3/3109");
            output.Add("CS/3/3372");
            output.Add("CS/3/6528");
            output.Add("CS/3/6147");
            output.Add("CS/3/6850");
            output.Add("CS/3/8951");
            output.Add("CS/3/8804");
            output.Add("CS/3/9019");
            output.Add("CS/3/9044");
            return output;
        }

        private DataTable GetShareHolder(SqlConnection Connection, SqlTransaction Transaction, string MSCFileID)
        {
            SqlCommand com = new SqlCommand();
            SqlDataAdapter ad = new SqlDataAdapter(com);

            System.Text.StringBuilder sql = new System.Text.StringBuilder();
            sql.AppendLine("SELECT OwnershipSHName, OwnershipPer, OwnershipBumi, OwnershipCName ");
            sql.AppendLine("FROM IntegrationDB.dbo.EIR_PMSCOwnerShipDtls ");
            sql.AppendLine("WHERE FileID = @MSCFileID");

            com.CommandText = sql.ToString();
            com.CommandType = CommandType.Text;
            com.Connection = Connection;
            com.Transaction = Transaction;
            com.CommandTimeout = int.MaxValue;

            //con.Open()
            try
            {
                com.Parameters.Add(new SqlParameter("@MSCFileID", MSCFileID));

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
                //con.Close()
            }
        }
    }
}
