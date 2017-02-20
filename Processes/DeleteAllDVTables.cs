using CRMCleaner.Classes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CRMCleaner.Processes
{
    class DeleteAllDVTables
    {
        internal void Start()
        {
            ArrayList DeleteList = getDeleteListing();
            foreach (string AccountDVID in DeleteList)
            {
                Guid guidAccountDVID = new Guid(AccountDVID);
                DeleteMSCAccountChangesVerification(guidAccountDVID);
            }
        }

        private ArrayList getDeleteListing()
        {
            ArrayList output = new ArrayList();
            output.Add("3AFDE1A6-8A82-4FDC-8043-7A2C8293B0E2");
            output.Add("67DEF02A-9378-4D35-BA94-A8BE20407C5F");
            output.Add("B42B6F3B-E604-4A7D-BAA8-0C75DDD54AB8");
            output.Add("8FF54725-8474-4694-9B27-25D0E3FE839E");
            output.Add("A597D864-C8CC-4497-8615-F8DA10090142");
            output.Add("4313DBB0-EACF-474A-A243-F21C6D2F60D9");
            output.Add("CAC96815-EEEB-4A74-BE1B-35FE1A0514F7");
            output.Add("154E190C-D91A-46A9-ABFD-C20B42CF0C19");
            output.Add("AAA00364-B36D-42C1-B3B5-9FF8D7B3AC07");
            output.Add("92E2BA0A-CC26-4DE7-B6B3-B2B076FAEB4C");
            output.Add("D940A2D7-9C3A-4242-899E-FFF54BAB0B06");
            output.Add("1EEAAF41-D90E-47EE-8228-CEB1E6391453");
            output.Add("7A695987-0642-4084-9236-2A2A1F437EEC");
            return output;
        }
        public int DeleteMSCAccountChangesVerification(Guid? AccountDVID)
        {
            using (SqlConnection conn = SQLHelper.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand("", conn))
                {

                    int affectedRows = 0;
                    StringBuilder sql = new StringBuilder();
                    sql.AppendLine("DELETE FROM AddressDV WHERE OwnerID = @AccountDVID");
                    sql.AppendLine("DELETE FROM ContactDV WHERE AccountDVID = @AccountDVID");
                    sql.AppendLine("DELETE FROM FinancialAndWorkerForecastDV WHERE AccountDVID = @AccountDVID");
                    sql.AppendLine("DELETE FROM AccountManagerAssignmentDV WHERE AccountDVID = @AccountDVID");
                    sql.AppendLine("DELETE FROM ShareHolderDV WHERE AccountDVID = @AccountDVID");
                    //sql.AppendLine("DELETE FROM ShareHolderDVOLD WHERE AccountDVID = @AccountDVID");
                    sql.AppendLine("DELETE FROM AccountDV WHERE AccountDVID = @AccountDVID");
                    cmd.CommandText = sql.ToString();
                    cmd.Parameters.AddWithValue("@AccountDVID", AccountDVID);
                    affectedRows += cmd.ExecuteNonQuery();

                    return affectedRows;
                }
            }
        }
    }
}
