using CRMCleaner.CLARITAS;
using CRMCleaner.Processes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CRMCleaner
{
    class Program
    {
        static void Main(string[] args)
        {
            PostMSCChangesClean PostMSC = new PostMSCChangesClean();
            PostMSC.Start();
            ////
            //CleanMainContact Contact = new CleanMainContact();
            //Contact.Start();
            //DeleteAllDVTables DeleteAll = new DeleteAllDVTables();
            //DeleteAll.Start();
            //CleanShareHolderWizardCRM CleanSHWizardCRM = new CleanShareHolderWizardCRM();
            //CleanSHWizardCRM.Start();
            //UpdateClassification updateClassifications = new UpdateClassification();
            //updateClassifications.Start();
            //CleanInactiveShareHolderandJVUpdate cleanShareAndJV = new CleanInactiveShareHolderandJVUpdate();
            //cleanShareAndJV.Start();
            //EmailCleanUp emailCleaner = new EmailCleanUp();
            //emailCleaner.Start();
            //GetADUserListing adUsers = new GetADUserListing();
            //adUsers.Start();
            //DeleteDuplicatedShareHolder deleteShareHolder = new DeleteDuplicatedShareHolder();
            //deleteShareHolder.Start();
            //UpdateAccountJV accountJV = new UpdateAccountJV();
            //accountJV.StartUpdate();
            //InsertTicketTable.Start();
            ///MSCHistoryStatusUpdate.Start();

        }
}
}
