//Sample IPN C# implementation, adds all variables to the database
//To run it please add reference to PayPal SDK and create database 
//Database used in this example has following structure:
//Table IPN_Main (Contains generic information on IPN recieved like it's id, timestamp, raw message)
//Table IPN_Variables (Contains detailed information on IPN:  Variable ID, IPN Variable Name and Value

//To create this database in SQL Server 2005 and higher execute query in the comment below:

/*
CREATE DATABASE MyTestDB
GO
 
USE [MyTestDB]
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

SET ANSI_PADDING ON
GO

CREATE TABLE [dbo].[IPN_Main](
 [IPN_ID] [int] IDENTITY(1,1) NOT NULL,
 [IPN_Status] [varchar](255) NOT NULL,
 [DateTimeStamp] [datetime] NOT NULL,
 [RawString] [text] NOT NULL,
 CONSTRAINT [PK_IPN_Main] PRIMARY KEY CLUSTERED 
(
 [IPN_ID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO

SET ANSI_PADDING OFF
GO

ALTER TABLE [dbo].[IPN_Main] ADD  CONSTRAINT [DF_IPN_Main_IPN_Status]  DEFAULT (' ') FOR [IPN_Status]
GO

ALTER TABLE [dbo].[IPN_Main] ADD  CONSTRAINT [DF_IPN_Main_DateTimeStamp]  DEFAULT (getdate()) FOR [DateTimeStamp]
GO

ALTER TABLE [dbo].[IPN_Main] ADD  CONSTRAINT [DF_IPN_Main_RawString]  DEFAULT (' ') FOR [RawString]
GO

 USE [MyTestDB]
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

SET ANSI_PADDING ON
GO

CREATE TABLE [dbo].[IPN_Variables](
 [VarID] [int] IDENTITY(1,1) NOT NULL,
 [IPN_ID] [int] NOT NULL,
 [Name] [varchar](255) NOT NULL,
 [Variable] [varchar](255) NOT NULL,
 CONSTRAINT [PK_IPN_Variables] PRIMARY KEY CLUSTERED 
(
 [VarID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

SET ANSI_PADDING OFF
GO

ALTER TABLE [dbo].[IPN_Variables] ADD  CONSTRAINT [DF_IPN_Variables_Variable]  DEFAULT (' ') FOR [Variable]
GO
 */

//Then add to the project new ADO.NET Entity Data Model of this database (for quick start with ADO.NET EF please read http://msdn.microsoft.com/en-us/library/bb399182.aspx )


using System;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Net;
using System.IO;
using com.paypal.sdk.util;

namespace AspIPNTemplates
{
    public partial class _Default : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            try
            {
                //Read The IPN POST
                string strFormValues = Encoding.ASCII.GetString(Request.BinaryRead(Request.ContentLength));
                string strNewRequest;

                //Create IPN verification request
                HttpWebRequest req = WebRequest.Create("https://www.sandbox.paypal.com/cgi-bin/webscr") as HttpWebRequest;

                req.Method = "POST";
                req.ContentType = "application/x-www-form-urlencoded";
                strNewRequest = strFormValues + "&cmd=_notify-validate";
                req.ContentLength = strNewRequest.Length;

                StreamWriter swOut = new StreamWriter(req.GetRequestStream(), Encoding.ASCII);
                swOut.Write(strNewRequest);
                swOut.Close();

                HttpWebResponse httwebresponseResponse = req.GetResponse() as HttpWebResponse;
                Stream stIPNResponseStream = httwebresponseResponse.GetResponseStream();
                Encoding encEncode = System.Text.Encoding.GetEncoding("utf-8");
                StreamReader srStream = new StreamReader(stIPNResponseStream, encEncode);

                NVPCodec nvpResponse = new NVPCodec();
                //Getting Name Value Pairs Collection
                nvpResponse.Decode(strFormValues);

                string strIPNResponse = srStream.ReadToEnd();
                Label lblMessage = new Label();
                lblMessage.Text = strIPNResponse;
                Page.Form.Controls.Add(lblMessage);

                //Creating new database object
                MyTestDBEntities MyTestDB = new MyTestDBEntities();

                IPN_Main ipn_main = new IPN_Main() { IPN_Status = strIPNResponse, DateTimeStamp = DateTime.Now, RawString = strFormValues };
                MyTestDB.IPN_Main.AddObject(ipn_main);
                MyTestDB.SaveChanges();

                for (int intCounter = 0; intCounter < nvpResponse.Count; ++intCounter)
                {
                    IPN_Variables ipn_variables = new IPN_Variables() { IPN_ID = ipn_main.IPN_ID, Name = nvpResponse.GetKey(intCounter), Variable = nvpResponse.Get(intCounter) };
                    MyTestDB.IPN_Variables.AddObject(ipn_variables);
                    //Writing to debug stream for debugging pupose
                    string strTemp = nvpResponse.GetKey(intCounter) + nvpResponse.Get(intCounter) + Environment.NewLine;
                    Debug.Write(strTemp);
                }

                MyTestDB.SaveChanges();
                srStream.Close();
            }
            catch (Exception exErrors)
            {
                //generic exception handling: adding label on page with exception details
                Label lblErrorMessage = new Label();
                lblErrorMessage.Text = "Exception: " + exErrors.Message + "<br/>" + exErrors.ToString();
                form1.Controls.Add(lblErrorMessage);

                Debug.WriteLine("Exception: " + exErrors.Message + "\n\t" + exErrors.ToString());
            }
        }
    }
}
