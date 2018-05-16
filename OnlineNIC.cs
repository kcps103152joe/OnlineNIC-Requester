/*
 * Author:          Joe Chan
 * Module:          OnlineNIC API Requester
 * Version:         1.0.0
 * Created:         19/4/2018
 * Last Modified:   16/5/2018
 */
using System;
using System.Text;
using System.IO;
using System.Net.Sockets;
using System.Diagnostics;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace OnlineNIC_API
{
    public class assets{
        public static string[] categoryList = {
            "client",
            "domain",
            "account",
            "ssl"
        };
        public static string[] actionList = {

            // Connection (client)
            "Login",
            "Logout",

            // Domain Name (domain)
            "CheckDomain",
            "InfoDomain",
            "CreateDomain",
            "RenewDomain",
            "InfoDomainExtra",
            "UpdateDomainExtra",
            "UpdateDomainDns",
            "UpdateDomainStatus",
            "UpdateDomainPwd",
            "UpdateDomainPwd",
            "GetAuthcode",
            "GetTmNotice",
            "GetDomainPrice",
            "UpdateXxxMemberId",

            // Contact Manage (domain)
            "CheckContact",
            "CreateContact",
            "UpdateContact",
            "ChangeRegistrant",
            "QueryEuTrade",

            // Name Server (domain)
            "CheckHost",
            "CreateHost",
            "InfoHost",
            "UpdateHost",
            "DeleteHost",

            // ID Shield (domain)
            "InfoIDShield",
            "AppIDShield",
            "UpdateIDShield",
            "RenewIDShield",
            "DeleteIDShield",

            // Transfer Resellers (domain)
            "QueryCustTransfer",
            "RequestCustTransfer",
            "CustTransferSetPwd",

            // Transfer Registrars (domain)
            "QueryRegTransfer",
            "RequestRegTransfer",
            "CancelRegTransfer",

            // Account Management (account)
            "GetAccountBalance",
            "GetCustomerInfo",
            "ModCustomerInfo",

            // SSL (ssl)
            "ParseCSR",
            "Order",
            "GetApproverEmailList",
            "ResendApproverEmail",
            "Cancel",
            "Info",
            "Reissue",
            "GetCerts",
            "ResendFulfillmentEmail",
            "ChangeApproverEmail",
        };
        public static string[] component = {
            "category",
            "action",
            "code",
            "msg",
            "value",
            "cltrid",
            "svtrid",
            "chksum"
        };
    }

    public class OnlineNIC {
        public String host { get; set; }
        public Int32 port { get; set; }
        public Int32 user { get; set; }
        public String pass { get; set; }

        private TcpClient client;
        private NetworkStream stream;
        
        public String lastReq { get; protected set; }
        public String lastRes { get; protected set; }
        public JObject lastResJSON { get; protected set; }



        /**
         * Check whether all config parameter is valid
         */
        protected bool validConfig()
        {
            bool isValid_h = false;
            bool isValid_p = false;
            bool isValid_u = false;
            bool isValid_s = false;

            isValid_h = !string.IsNullOrEmpty(host);
            isValid_p = port > 0 ? true : false;
            isValid_u = user > 0 ? true : false;
            isValid_s = !string.IsNullOrEmpty(pass);

            return isValid_h & isValid_p & isValid_u & isValid_s;
        }

        /**
         * User accessible function for build command and request
         */
        public void request(string _category, string _action, string[][] _params){
            if (validConfig()){
                string command = buildCommand(_category, _action, _params);
                connect(command);
            }
        }
        
        /**
         * Build request command
         */
        protected string buildCommand(string _category, string _action, string[][] _params)
        {
            string xml;
            string cltrid = getCltrid();

            JObject paramsJSON = new JObject();
            if (_params.Length > 0)
                foreach (string[] param in _params)
                    paramsJSON.Add(new JProperty(param[0], param[1]));

            string chksum = getChksum(cltrid, _action, paramsJSON);
            
            xml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"no\"?>" + "\n";
            xml += "<request>" + "\n";
            xml += "    <category>" + _category + "</category>" + "\n";
            xml += "    <action>" + _action + "</action>" + "\n";

            if (_params.Length > 0){

                xml += "    <params>" + "\n";

                foreach (string[] param in _params)
                    xml += "        <param name=\"" + param[0] + "\">" + param[1] + "</param>" + "\n";

                xml += "    </params>" + "\n";

            }else if (_action == "Login" || _action == "Logout"){

                xml += "    <params>" + "\n";
                xml += "        <param name=\"" + "clid" + "\">" + user + "</param>" + "\n";
                xml += "    </params>" + "\n";

            }

            xml += "    <cltrid>" + cltrid + "</cltrid>" + "\n";
            xml += "    <chksum>" + chksum + "</chksum>" + "\n";
            xml += "</request>" + "\n";

            lastReq = xml;
            xml = xml.Replace("\n", string.Empty);
            return xml;
        }

        /**
         * Check whether command is valid
         */
        protected bool validCommand(string command)
        {
            bool isValid_a = false;
            bool isValid_c = false;

            string categoryPtn = @"<category>([\s\S]*)<\/category>";
            string actionPtn = @"<action>([\s\S]*)<\/action>";

            string category = regex(categoryPtn, command)[0];
            string action = regex(actionPtn, command)[0];

            if(category != "" && action != "")
            {
                for (int i = 0; i < assets.actionList.Length; i++)
                {
                    if (action == assets.actionList[i])
                        isValid_a = true;
                }
                for (int i = 0; i < assets.categoryList.Length; i++)
                {
                    if (category == assets.categoryList[i])
                        isValid_c = true;
                }
            }
            
            return isValid_a & isValid_c;
        }

        /**
         * User accessible function for connect to Network Client and prepare reader and writer
         */
        public void connect(string command){
            if (validCommand(command) && validConfig()){
                if (client == null) client = new TcpClient(host, port);                                       // Create a new TcpClient object.
                if (stream == null) stream = client.GetStream();                                              // Get a client stream for reading and writing.
                try
                {
                    StreamReader reader = new StreamReader(stream, Encoding.UTF8);                           // Get a reader for Stream reading.
                    StreamWriter writer = new StreamWriter(stream, Encoding.UTF8);                           // Get a writer for Stream writing.

                    String responseData = sendCommand(reader, writer, command);
                    lastRes = responseData;
                    lastResJSON = parseResult(responseData);
                    //Debug.WriteLine("Received:\n" + responseData);
                }
                catch (ArgumentNullException err) { Debug.WriteLine("ArgumentNullException: " + err); }
                catch (SocketException err) { Debug.WriteLine("SocketException: " + err); }
                finally
                {
                    //stream.Close();                                                                         // Close Network Stream.
                    //client.Close();                                                                         // Close TCP Client.
                }
            }
        }

        /**
         * Request command to Network Client
         */
        protected string sendCommand(StreamReader reader, StreamWriter writer, string command){
            string[][] _params = new string[][] { };

            if (stream.CanWrite){
                /**
                 * Request Command
                 */
                writer.Write(command, 0, Encoding.UTF8.GetBytes(command).Length);
                writer.Flush();
                //Debug.WriteLine("Sent:\n" + command);
            }
            return getResponse(reader);
        }

        /**
         * Get response From Network Client
         */
        protected string getResponse(StreamReader reader)
        {
            String responseData = String.Empty;

            reader.ReadLine();                                                                              // Subtract the first line to prevent confliction with Regex Matching

            if (stream.CanRead)
                while (reader.Peek() >= 0)
                    responseData += reader.ReadLine() + "\n";
            return responseData;
        }

        /**
         * Analyze all data and change to JSON Object
         */
        protected JObject parseResult(string responseData){

            JObject BlockData   = new JObject();

            foreach (string matchItem in assets.component)
                BlockData.Add(matchItem, matchStr_Data(matchItem, responseData));
            
            if (BlockData != null)
                if(BlockData["code"] != null && BlockData["code"].ToString() != "")
                    if(BlockData["code"].ToObject<int>() >= 1000 && BlockData["code"].ToObject<int>() < 2000){
                        string data = matchStr_Data("resData", responseData);
                        if (data != ""){
                            BlockData.Add("resData", matchStr_resData(data));
                            //Debug.WriteLine("BlockData: " + BlockData);
                        }
                    }
            return BlockData;
        }

        /**
         * Match all response Data, except <resData>...</resData>
         */
        protected string matchStr_Data(string matchStr, string content){
            string pattern = @"<" + matchStr + @">([\s\S]*)<\/" + matchStr + ">";
            return regex(pattern, content)[0];
        }
        
        /**
         * Match the <resData>...</resData> Data
         */
        protected JObject matchStr_resData(string content){
            JObject Data = new JObject();

            string pattern = @"<data name="+"\""+"(.*)"+"\""+ @">(.*)<\/data>";
            
            string[] stringSeparators = new string[] { "\n" };
            string[] lines = content.Split(stringSeparators, StringSplitOptions.None);
            
            for (int i = 0; i < lines.Length; i++)
                if (lines[i].IndexOf("<") > 0){
                    string[] match = regex(pattern, lines[i]);
                    Data.Add(match[0], match[1]);
                }
            return Data;
        }

        /**
         * Regex Match Function
         */
        protected string[] regex(string pattern, string content){
            Regex regex = new Regex(pattern);                                       // Create a Regex Object
            var v = regex.Match(content);                                           // Match the content with pattern

            string[] data = { v.Groups[1].ToString(), v.Groups[2].ToString() };     // Get the from format <data name="[1]">[2]</data>

            return data;
        }

        /**
         * Generate Checksum
         */
        protected string getChksum(string cltrid, string action, JObject _params)
        {
            string md5_clpass = md5(pass.ToString());
            string commonStr = user + md5_clpass + cltrid;
            string temp;
            switch (action){
                case "Login":                   //client
                case "Logout":                  //client

                case "GetAccountBalance":       //account
                case "GetCustomerInfo":         //account
                case "ModCustomerInfo":         //account
                    return md5(commonStr + action.ToLower());

                case "CheckDomain":             //domain
                case "InfoDomain":              //domain
                case "DeleteDomain":            //domain
                case "InfoDomainExtra":         //domain
                case "UpdateDomainExtra":       //domain
                case "UpdateDomainDns":         //domain
                case "UpdateDomainStatus":      //domain
                case "GetAuthcode":             //domain
                case "InfoIDShield":            //domain
                case "AppIDShield":             //domain
                case "UpdateIDShield":          //domain
                case "RenewIDShield":           //domain
                case "DeleteIDShield":          //domain
                case "QueryRegTransfer":        //domain
                case "RequestRegTransfer":      //domain
                case "CancelRegTransfer":       //domain
                    return md5(commonStr + action.ToLower() + _params["domaintype"] + _params["domain"]);
                case "CreateDomain":
                    if (_params["domain"].HasValues) {
                        if (_params["domain"].ToString().IndexOf(".eu") > 0)
                            return md5(commonStr + action.ToLower() + _params["domaintype"] + _params["domain"] + _params["period"] + _params["dns"][0] + _params["dns"][1] + _params["registrant"] + _params["password"]);
                        else if (_params["domain"].ToString().IndexOf(".asia") > 0)
                            return md5(commonStr + action.ToLower() + _params["domaintype"] + _params["domain"] + _params["period"] + _params["dns"][0] + _params["dns"][1] + _params["registrant"] + _params["admin"] + _params["tech"] + _params["billing"] + _params["ced"] + _params["password"]);
                        else if (_params["domain"].ToString().IndexOf(".tv") > 0)
                            return md5(commonStr + action.ToLower() + _params["domaintype"] + _params["domain"] + _params["period"] + _params["dns"][0] + _params["dns"][1] + _params["admin"] + _params["tech"] + _params["password"]);
                        else
                            return md5(commonStr + action.ToLower() + _params["domaintype"] + _params["domain"] + _params["period"] + _params["dns"][0] + _params["dns"][1] + _params["registrant"] + _params["admin"] + _params["tech"] + _params["billing"] + _params["password"]);
                    }else
                        return md5(commonStr + action.ToLower());
                case "RenewDomain":             //domain
                    return md5(commonStr + action.ToLower() + _params["domaintype"] + _params["domain"] + _params["period"]);
                case "UpdateDomainPwd":         //domain
                    return md5(commonStr + action.ToLower() + _params["domaintype"] + _params["domain"] + _params["password"]);
                case "GetTmNotice":             //domain
                    return md5(commonStr + action.ToLower() + _params["domaintype"] + _params["lookupkey"]);
                case "GetDomainPrice":          //domain
                    return md5(commonStr + action.ToLower() + _params["domaintype"] + _params["domain"] + _params["op"] + _params["period"]);
                case "UpdateXxxMemberId":       //domain
                    return md5(commonStr + action.ToLower() + _params["domaintype"] + _params["lookupkey"] + _params["memberid"]);
                case "CheckContact":            //domain
                    return md5(commonStr + action.ToLower() + _params["domaintype"] + _params["contactid"]);
                case "CreateContact":           //domain
                    temp = commonStr + action.ToLower();
                    if (_params["name"].HasValues && _params["name"] != null)
                        temp = temp + _params["name"];
                    if (_params["org"].HasValues && _params["org"] != null)
                        temp = temp + _params["org"];
                    if (_params["email"].HasValues && _params["email"] != null)
                        temp = temp + _params["email"];
                    return md5(temp);
                case "UpdateContact":           //domain
                    return md5(commonStr + action.ToLower() + _params["domaintype"] + _params["domain"] + _params["contacttype"]);
                case "ChangeRegistrant":        //domain
                    temp = commonStr + action.ToLower() + _params["domaintype"] + _params["domain"];
                    if (_params["name"].HasValues && _params["name"] != null)
                        temp = temp + _params["name"];
                    if (_params["org"].HasValues && _params["org"] != null)
                        temp = temp + _params["org"];
                    if (_params["email"].HasValues && _params["email"] != null)
                        temp = temp + _params["email"];
                    return md5(temp);
                case "CheckHost":               //domain
                case "InfoHost":                //domain
                case "DeleteHost":              //domain
                    return md5(commonStr + action.ToLower() + _params["domaintype"] + _params["hostname"]);
                case "CreateHost":              //domain
                    temp = commonStr + action.ToLower() + _params["domaintype"] + _params["hostname"];
                    if (_params["addr"].HasValues && _params["addr"] != null)
                        temp = temp + _params["addr"];
                    return md5(temp);
                case "UpdateHost":              //domain
                    temp = commonStr + action.ToLower() + _params["domaintype"] + _params["hostname"];
                    if (_params["addaddr"].HasValues && _params["addaddr"] != null)
                        temp = temp + _params["addaddr"];
                    if (_params["remaddr"].HasValues && _params["remaddr"] != null)
                        temp = temp + _params["remaddr"];
                    return md5(temp);
                case "QueryCustTransfer":       //domain
                    return md5(commonStr + action.ToLower() + _params["domaintype"] + _params["domain"] + _params["op"]);
                case "RequestCustTransfer":     //domain
                    return md5(commonStr + action.ToLower() + _params["domaintype"] + _params["domain"] + _params["password"] + _params["curID"]);
                case "CustTransferSetPwd":      //domain
                    return md5(commonStr + action.ToLower() + _params["domaintype"] + _params["domain"] + _params["password"]);

                case "ParseCSR":                //SSL
                case "Order":                   //SSL
                case "GetApproverEmailList":    //SSL
                case "GetCerts":                //SSL
                    return md5(commonStr + action.ToLower());
                case "ResendApproverEmail":     //SSL
                case "Cancel":                  //SSL
                case "Info":                    //SSL
                case "Reissue":                 //SSL
                case "ResendFulfillmentEmail":  //SSL
                case "ChangeApproverEmail":     //SSL
                    return md5(commonStr + action.ToLower() + _params["orderId"]);
                default:
                    return "Get_Checksum_Error";
            }
        }

        /**
         * Generate Cltrid for Checksum
         */
        protected string getCltrid()
        {
            Random ran = new Random();
            return "client" + user.ToString() + DateTime.Now.ToString("yMdhmt") + ran.Next(10000);
        }
        
        /**
         * Function for MD5 encryption
         */
        static string md5(string input){
            MD5 md5Hash = MD5.Create();

            byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));                               // Convert the input string to a byte array and compute the hash.

            StringBuilder sBuilder = new StringBuilder();                                                   // Create a new Stringbuilder to collect the bytes and create a string.

            for (int i = 0; i < data.Length; i++)                                                           // Loop through each byte of the hashed data and format each one as a hexadecimal string.
                sBuilder.Append(data[i].ToString("x2"));

            return sBuilder.ToString();                                                                     // Return the hexadecimal string.
        }
    }
}
