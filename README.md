# OnlineNIC-Requester
OnlineNIC API Simply Requester In C#

## Introduction
Language: C#
Namespace: OnlineNIC_API


## Getting Started
Create object.
```
OnlineNIC onlincnic = new OnlineNIC();  // Creates an instance of requester Object
```

Set parameters for connection.
```sh
onlincnic.host = "www.onlinenic.com";   // Connection host.         type: String
onlincnic.port = 30009;                 // Connection port.         type: Int
onlincnic.user = 123456;                // OnlineNIC User ID.       type: Int
onlincnic.pass = "pass123";             // OnlineNIC User Password. type: String
```

Or simply set the config when create instance.
```sh
OnlineNIC onlincnic = new OnlineNIC() {
                host = "www.onlinenic.com",
                port = 30009,
                user = 123456,
                pass = "pass123"
            }; 
```

Request method.
```sh
string[][] check = new string[][] {                     //  Create a string array for parameters
    new string[] { "domaintype", "0" },                 //  Each parameter create a new string array
    new string[] { "domain", "abc.com" }
};
onlincnic.request("domain", "CheckDomain", check);      //  Request to OnlineNIC API
```
Using request method, there are three parameters should included, namely `category`, `action` and `params`.
Each `params` string array should contain two item, `name` and `value`.
Detail can see in [document](http://218.5.81.149/api/demo/3.x/en/?_r=/domain/checkDomain) of OnlineNIC API.




## Get Response
It can get response after every request action. 
```sh
string response = onlinenic.lastRes;                    // Get response of last requested result.
string responseJSON = onlinenic.lastResJSON;            // Get response of last requested result in JSON format.
```




## Example
```sh
using System;
using System.Web.UI;
using OnlineNIC_API;

namespace Program{
    public class program{
        protected void Requester{
            OnlineNIC onlincnic = new OnlineNIC() {
                        host = "www.onlinenic.com",
                        port = 30009,
                        user = 123456,
                        pass = "pass123"
                    }; 
            
            /** 
             * Login
             */
            string[][] login = new string[][] { };                  //  Create a string array for parameters
            onlincnic.request("client", "Login", login);            //  Request to OnlineNIC API
            string response = onlinenic.lastRes;                    // Get response of "Login" request
            
            /** 
             * Check Domain
             */
            string[][] check = new string[][] {                     //  Create a string array for parameters
                new string[] { "domaintype", "0" },
                new string[] { "domain", "abc.com" }
            };
            onlincnic.request("domain", "CheckDomain", check);      //  Request to OnlineNIC API
            string response = onlinenic.lastRes;                    // Get response of "Check Domain" request
            
            /** 
             * Logout
             */
            string[][] logout = new string[][] { };                 //  Create a string array for parameters
            onlincnic.request("client", "Logout", logout);          //  Request to OnlineNIC API
            string response = onlinenic.lastRes;                    // Get response of "Logout" request
        }
    }
}

```


## Method

| Properties | Type | Remark |
| ------ | ------ | ------ |
| request(string, string, string[][]) | void | Send a request to OnlineNIC API base on the category, action, parameters. |
| connect(string) | void | Send a request to OnlineNIC API base on a completed command string. |

## Properties

| Properties    | Type      | Remark                                                        |
| ------        | ------    | ------                                                        |
| host          | String    | Get and Set the parameter of connection host.                 |
| port          | Int32     | Get and Set the parameter of connection port.                 |
| user          | Int32     | Get and Set the parameter of user id.                         |
| pass          | String    | Get and Set the parameter of password.                        |
| lastReq       | String    | Return last request string for reference.                     |
| lastRes       | String    | Return last response of last requested result.                |
| lastResJSON   | JObject   | Return last response of last requested result in JSON format. |






Author
----
Joe Chan
