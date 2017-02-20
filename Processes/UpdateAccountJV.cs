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
    class UpdateAccountJV
    {
        internal void StartUpdate()
        {
            ArrayList AccountIDList = getAllAccountID();
            Console.WriteLine(string.Format("[{0}] : {1} Account ID Found Found in Account Table",DateTime.Now,AccountIDList.Count.ToString()));
            int TotalIDs = AccountIDList.Count;
            int Counter = 0;
            foreach (string AccountIDstr in AccountIDList)
            {
                Guid AccountID = new Guid(AccountIDstr);
                using (SqlConnection Connection = SQLHelper.GetConnection())
                {
                    Counter++;
                    SqlTransaction Transaction = default(SqlTransaction);
                    Transaction = Connection.BeginTransaction("UpdateProcess");
                    BOL.AccountContact.odsAccount mgr = new BOL.AccountContact.odsAccount();
                    //Update JV Category
                    mgr.UpdateAccountJVCategory_Wizard(Connection, Transaction, AccountID, new Guid(SyncHelper.AdminID), SyncHelper.AdminName);
                    //Update BumiClassification
                    mgr.UpdateAccountBumiClassification_Wizard(Connection, Transaction, AccountID, new Guid(SyncHelper.AdminID), SyncHelper.AdminName);
                    //Update Classification
                    mgr.UpdateAccountClassification_Wizard(Connection, Transaction, AccountID, new Guid(SyncHelper.AdminID), SyncHelper.AdminName);
                    Transaction.Commit();
                    Console.WriteLine(string.Format("[{0}] : {1} / {2} Account ID : {3} Updated", DateTime.Now,Counter.ToString(),TotalIDs.ToString(), AccountIDstr));
                }
            }
            Console.WriteLine(string.Format("[{0}] : Finished Updated All Account JV Category", DateTime.Now));
            Console.ReadLine();
        }

        private ArrayList getAllAccountID()
        {
            ArrayList output = new ArrayList();
            using (SqlConnection Connection = SQLHelper.GetConnection())
            {
                SqlTransaction Transaction = default(SqlTransaction);
                Transaction = Connection.BeginTransaction("getAllAccountID");
                using (SqlCommand com = new SqlCommand("", Connection))
                {
                    using (SqlDataAdapter adapter = new SqlDataAdapter(com))
                    {

                        System.Text.StringBuilder sql = new System.Text.StringBuilder();
                        sql.AppendLine("Select AccountID FROM Account");
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
                            {
                                foreach (DataRow row in dt.Rows)
                                {
                                    output.Add(row[0].ToString());
                                }
                            }
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
