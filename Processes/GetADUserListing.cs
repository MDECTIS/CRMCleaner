using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CRMCleaner.Processes
{
    class GetADUserListing
    {
        internal void Start()
        {
            //try
            //{
            //List<string> UserGroup = GetGroups();
            //GetADUsers();

            string adServer = "10.9.192.139";
            string adDomain = "mdecad.mdc.com.my";
            string adUsername = "administrator";
            string password = "a118L@cK";
            string[] dc = adDomain.Split('.');
            string dcAdDomain = string.Empty;

            foreach (string item in dc)
            {
                if (dc[dc.Length - 1].Equals(item))
                    dcAdDomain = dcAdDomain + "DC=" + item;
                else
                    dcAdDomain = dcAdDomain + "DC=" + item + ",";
            }
            DirectoryEntry entry = new DirectoryEntry(@"LDAP://" + adServer + "/CN=Users," + dcAdDomain, adUsername, password);
            DirectorySearcher Dsearch = new DirectorySearcher(entry);
            String Name = "Hayati Harun";
            Dsearch.Filter = "(&(objectClass=user)(l=" + Name + "))";
            // get all entries from the active directory.
            // Last Name, name, initial, homepostaladdress, title, company etc..
            foreach (SearchResult sResultSet in Dsearch.FindAll())
            {

                // Login Name
                Console.WriteLine(GetProperty(sResultSet, "cn"));
                // First Name
                Console.WriteLine(GetProperty(sResultSet, "givenName"));
                // Middle Initials
                Console.Write(GetProperty(sResultSet, "initials"));
                // Last Name
                Console.Write(GetProperty(sResultSet, "sn"));
                // Address
                string tempAddress = GetProperty(sResultSet, "homePostalAddress");

                if (tempAddress != string.Empty)
                {
                    string[] addressArray = tempAddress.Split(';');
                    string taddr1, taddr2;
                    taddr1 = addressArray[0];
                    Console.Write(taddr1);
                    taddr2 = addressArray[1];
                    Console.Write(taddr2);
                }
                // title
                Console.Write(GetProperty(sResultSet, "title"));
                // company
                Console.Write(GetProperty(sResultSet, "company"));
                //state
                Console.Write(GetProperty(sResultSet, "st"));
                //city
                Console.Write(GetProperty(sResultSet, "l"));
                //country
                Console.Write(GetProperty(sResultSet, "co"));
                //postal code
                Console.Write(GetProperty(sResultSet, "postalCode"));
                // telephonenumber
                Console.Write(GetProperty(sResultSet, "telephoneNumber"));
                //extention
                Console.Write(GetProperty(sResultSet, "otherTelephone"));
                //fax
                Console.Write(GetProperty(sResultSet, "facsimileTelephoneNumber"));

                // email address
                Console.Write(GetProperty(sResultSet, "mail"));
                // Challenge Question
                Console.Write(GetProperty(sResultSet, "extensionAttribute1"));
                // Challenge Response
                Console.Write(GetProperty(sResultSet, "extensionAttribute2"));
                //Member Company
                Console.Write(GetProperty(sResultSet, "extensionAttribute3"));
                // Company Relation ship Exits
                Console.Write(GetProperty(sResultSet, "extensionAttribute4"));
                //status
                Console.Write(GetProperty(sResultSet, "extensionAttribute5"));
                // Assigned Sales Person
                Console.Write(GetProperty(sResultSet, "extensionAttribute6"));
                // Accept T and C
                Console.Write(GetProperty(sResultSet, "extensionAttribute7"));
                // jobs
                Console.Write(GetProperty(sResultSet, "extensionAttribute8"));
                String tEamil = GetProperty(sResultSet, "extensionAttribute9");

                // email over night
                //if (tEamil != string.Empty)
                //{
                //    string em1, em2, em3;
                //    string[] emailArray = tEmail.Split(';');
                //    em1 = emailArray[0];
                //    em2 = emailArray[1];
                //    em3 = emailArray[2];
                //    Console.Write(em1 + em2 + em3);

                //}
                // email daily emerging market
                Console.Write(GetProperty(sResultSet, "extensionAttribute10"));
                // email daily corporate market
                Console.Write(GetProperty(sResultSet, "extensionAttribute11"));
                // AssetMgt Range
                Console.Write(GetProperty(sResultSet, "extensionAttribute12"));
                // date of account created
                Console.Write(GetProperty(sResultSet, "whenCreated"));
                // date of account changed
                Console.Write(GetProperty(sResultSet, "whenChanged"));
            }
            //   using (DirectoryEntry adsEntry = new DirectoryEntry(@"LDAP://" + adServer + "/CN=Users," + dcAdDomain, adUsername, password))
            //    {
            //        using (DirectorySearcher searcher = new DirectorySearcher(adsEntry))
            //        {
            //            //searcher.Filter = "(samaccountname=" + name + ")";
            //            searcher.Filter = "(userPrincipalName=nurliza@mdec.com.my)";
            //            searcher.PropertiesToLoad.Add("displayname");

            //            SearchResult adsSearchResult = searcher.FindOne();

            //            if (adsSearchResult != null)
            //            {
            //                if (adsSearchResult.Properties["displayname"].Count == 1)
            //                {
            //                    string realName = (string)adsSearchResult.Properties["displayname"][0];
            //                }
            //            }
            //        }
            //    }
            //}
            //catch (Exception ex)
            //{

            //}
            //    using (DirectoryEntry adsEntry = new DirectoryEntry(@"LDAP://" + adServer + "/CN=Users," + dcAdDomain, adUsername, password))
            //    {
            //        using (DirectorySearcher adsSearcher = new DirectorySearcher(adsEntry))
            //        {
            //            adsSearcher.Filter = "(&(objectClass=user)(objectCategory=person))";
            //            //adsSearcher.Filter = "(sAMAccountName=" + strAccountId + ")";

            //            try
            //            {
            //                SearchResultCollection adsSearchResult = adsSearcher.FindAll();
            //                foreach (SearchResult item in adsSearchResult)
            //                {
            //                    using (DirectoryEntry directoryEntry = new DirectoryEntry(item.Path, adUsername, password))
            //                    {
            //                        DirectorySearcher search = new DirectorySearcher(directoryEntry);
            //                        search.Filter = "(sAMAccountName=user)";
            //                        search.PropertiesToLoad.Add("memberOf");
            //                        StringBuilder groupNames = new StringBuilder();
            //                        try
            //                        {
            //                            SearchResult result = search.FindOne();
            //                            if (result != null)
            //                            {
            //                                int propertyCount = result.Properties["memberOf"].Count;
            //                                String dn;
            //                                int equalsIndex, commaIndex;

            //                                for (int propertyCounter = 0; propertyCounter < propertyCount; propertyCounter++)
            //                                {
            //                                    dn = (String)result.Properties["memberOf"][propertyCounter];

            //                                    equalsIndex = dn.IndexOf("=", 1);
            //                                    commaIndex = dn.IndexOf(",", 1);
            //                                    if (-1 == equalsIndex)
            //                                    {

            //                                    }
            //                                    groupNames.Append(dn.Substring((equalsIndex + 1), (commaIndex - equalsIndex) - 1));
            //                                    groupNames.Append("|");
            //                                }
            //                            }
            //                        }
            //                        catch (Exception ex)
            //                        {

            //                        }
            //                    }
            //                }
            //            }
            //            catch (Exception ex)
            //            {
            //                // Failed to authenticate. Most likely it is caused by unknown user
            //                // id or bad strPassword.
            //                //strError = ex.Message;
            //            }
            //            finally
            //            {
            //                adsEntry.Close();
            //            }
            //        }
            //    }
            //}
            //catch (Exception)
            //{

            //    throw;
            //}

        }
        internal static string GetProperty(SearchResult searchResult, string PropertyName)
        {
            if (searchResult.Properties.Contains(PropertyName))
            {
                return searchResult.Properties[PropertyName][0].ToString();
            }
            else
            {
                return string.Empty;
            }
        }
        public List<string> GetGroups()
        {
            List<string> result = new List<string>();
            string adServer = "10.9.192.139";
            string adDomain = "mdecad.mdc.com.my";
            string adUsername = "administrator";
            string password = "a118L@cK";
            string[] dc = adDomain.Split('.');
            string dcAdDomain = string.Empty;

            foreach (string item in dc)
            {
                if (dc[dc.Length - 1].Equals(item))
                    dcAdDomain = dcAdDomain + "DC=" + item;
                else
                    dcAdDomain = dcAdDomain + "DC=" + item + ",";
            }
            DirectoryEntry de = new DirectoryEntry(@"LDAP://" + adServer + "/CN=Users," + dcAdDomain, adUsername, password);
            DirectoryEntry objADAM = default(DirectoryEntry);
            // Binding object. 
            DirectoryEntry objGroupEntry = default(DirectoryEntry);
            // Group Results. 
            DirectorySearcher objSearchADAM = default(DirectorySearcher);
            // Search object. 
            SearchResultCollection objSearchResults = default(SearchResultCollection);
            // Results collection. 
            //string strPath = null;
            //// Binding path. 
            //List<string> result = new List<string>();

            //// Construct the binding string. 
            //strPath = "LDAP://stefanserver.stefannet.local";
            //Change to your ADserver 

            // Get the AD LDS object. 
            try
            {
                objADAM = new DirectoryEntry(@"LDAP://" + adServer + "/CN=Users," + dcAdDomain, adUsername, password);
                objADAM.RefreshCache();
            }
            catch (Exception e)
            {
                throw e;
            }

            // Get search object, specify filter and scope, 
            // perform search. 
            try
            {
                objSearchADAM = new DirectorySearcher(objADAM);
                objSearchADAM.Filter = "(&(objectClass=group))";
                objSearchADAM.SearchScope = SearchScope.Subtree;
                objSearchResults = objSearchADAM.FindAll();
            }
            catch (Exception e)
            {
                throw e;
            }

            // Enumerate groups 
            try
            {
                if (objSearchResults.Count != 0)
                {
                    foreach (SearchResult objResult in objSearchResults)
                    {
                        objGroupEntry = objResult.GetDirectoryEntry();
                        result.Add(objGroupEntry.Name);
                    }
                }
                else
                {
                    throw new Exception("No groups found");
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }

            return result;
        }

        private void GetADUser()
        {
            string adServer = "10.9.192.139";
            string adDomain = "mdecad.mdc.com.my";
            string adUsername = "administrator";
            string password = "a118L@cK";
            string[] dc = adDomain.Split('.');
            string dcAdDomain = string.Empty;

            foreach (string item in dc)
            {
                if (dc[dc.Length - 1].Equals(item))
                    dcAdDomain = dcAdDomain + "DC=" + item;
                else
                    dcAdDomain = dcAdDomain + "DC=" + item + ",";
            }
            DirectoryEntry de = new DirectoryEntry(@"LDAP://" + adServer + "/CN=Users," + dcAdDomain, adUsername, password);
            DirectorySearcher search = new DirectorySearcher(de);
            search.Filter = "(sAMAccountName=user)";
            search.PropertiesToLoad.Add("memberOf");
            StringBuilder groupNames = new StringBuilder();
            try
            {
                SearchResult result = search.FindOne();
                int propertyCount = result.Properties["memberOf"].Count;
                String dn;
                int equalsIndex, commaIndex;

                for (int propertyCounter = 0; propertyCounter < propertyCount; propertyCounter++)
                {
                    dn = (String)result.Properties["memberOf"][propertyCounter];

                    equalsIndex = dn.IndexOf("=", 1);
                    commaIndex = dn.IndexOf(",", 1);
                    if (-1 == equalsIndex)
                    {

                    }
                    groupNames.Append(dn.Substring((equalsIndex + 1), (commaIndex - equalsIndex) - 1));
                    groupNames.Append("|");
                }
            }
            catch (Exception ex)
            {

            }
        }

        public List<Users> GetADUsers()
        {
            string adServer = "10.9.192.139";
            string adDomain = "mdecad.mdc.com.my";
            string adUsername = "administrator";
            string password = "a118L@cK";
            string[] dc = adDomain.Split('.');
            string dcAdDomain = string.Empty;

            foreach (string item in dc)
            {
                if (dc[dc.Length - 1].Equals(item))
                    dcAdDomain = dcAdDomain + "DC=" + item;
                else
                    dcAdDomain = dcAdDomain + "DC=" + item + ",";
            }
            List<Users> lstADUsers = new List<Users>();
            try
            {

                string DomainPath = @"LDAP://" + adServer + "/CN=Users," + dcAdDomain;
                DirectoryEntry searchRoot = new DirectoryEntry(DomainPath, adUsername, password);
                DirectorySearcher search = new DirectorySearcher(searchRoot);
                search.Filter = "(&(objectClass=user)(objectCategory=person))";
                search.PropertiesToLoad.Add("samaccountname");
                search.PropertiesToLoad.Add("mail");
                search.PropertiesToLoad.Add("usergroup");
                search.PropertiesToLoad.Add("displayname");//first name
                SearchResult result;
                SearchResultCollection resultCol = search.FindAll();
                if (resultCol != null)
                {
                    for (int counter = 0; counter < resultCol.Count; counter++)
                    {
                        string UserNameEmailString = string.Empty;
                        result = resultCol[counter];
                        if (result.Properties.Contains("samaccountname") &&
                                 result.Properties.Contains("mail") &&
                            result.Properties.Contains("displayname"))
                        {
                            Users objSurveyUsers = new Users();
                            objSurveyUsers.Email = (String)result.Properties["mail"][0] +
                              "^" + (String)result.Properties["displayname"][0];
                            objSurveyUsers.UserName = (String)result.Properties["samaccountname"][0];
                            objSurveyUsers.DisplayName = (String)result.Properties["displayname"][0];
                            lstADUsers.Add(objSurveyUsers);
                        }
                    }
                }

            }
            catch (Exception ex)
            {

            }
            return lstADUsers;
        }
        public class Users
        {
            public string Email { get; set; }
            public string UserName { get; set; }
            public string DisplayName { get; set; }
            public bool isMapped { get; set; }
        }
    }
}
