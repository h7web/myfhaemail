using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Net.Mail;
using System.Text;
using SendGrid.SmtpApi;
using SendGrid.CSharp.HTTP.Client;
using System.Data.SqlClient;
using System.Net.Mime;
using System.Configuration;

namespace myfhaemail
{
    class Program
    {
        static void Main()
        {
            SqlConnection putconnect = new SqlConnection();
            putconnect.ConnectionString = ConfigurationManager.ConnectionStrings["sqlconn"].ConnectionString;

            try
            {
                putconnect.Open();

                //get the list of email templates

                var putsql = "SELECT emailid FROM email_templates WHERE enabled=1";

                //Console.WriteLine("sql=" + putsql);

                SqlCommand putcmd = new SqlCommand(putsql, putconnect);

                SqlDataReader reader;

                reader = putcmd.ExecuteReader();

                while (reader.Read())
                {
                    Main2(Convert.ToInt32(reader["emailid"]));
                }
                reader.Close();

                putconnect.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("get templates err is " + ex.Message);
            }
        }
        static void Main2(int emlid)
        {
            // get the begin/end dates for this email, declar the lists
            List<string> emllist = new List<string>();
            List<string> namlist = new List<string>();
            List<string> fnamlist = new List<string>();

            string subj = "";
            string bodytext = "";
            string bodyhtml = "";
            int days = 0;
            int client_id = 0;
            string rating = "";

            //connect to the database

            SqlConnection putconnect = new SqlConnection();
            putconnect.ConnectionString = ConfigurationManager.ConnectionStrings["sqlconn"].ConnectionString;

            putconnect.Open();

            //get the email content

            var putsql = "SELECT subject, isnull(text,html) as text, html, days, client_id, credit from email_templates where emailid=" + emlid + "";

            //Console.WriteLine("sql=" + putsql);

            SqlCommand putcmd = new SqlCommand(putsql, putconnect);

            putcmd.CommandTimeout = 3000;

            SqlDataReader reader;

            reader = putcmd.ExecuteReader();

            while (reader.Read())
            {
                subj = reader.GetString(0);
                bodytext = reader.GetString(1);
                bodyhtml = reader.GetString(2);
                days = Convert.ToInt32(reader["days"]);
                client_id = Convert.ToInt32(reader["client_id"]);
                rating = reader.GetString(5);
            }
            reader.Close();

            var dt = DateTime.Now;
            var dt1 = dt.AddDays(-Convert.ToInt32(days));
            var dt2 = dt1.AddDays(-1);
            var getstime = dt1.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
            var getetime = dt2.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
            var sendflag = false;
            int cnt = 0;

            //use this if you need to manually set the time
            //getstime = double.Parse("1484722832");
            //getetime = double.Parse("1484697704");

            //get the names and emails for this email batch

            putsql = "SELECT top 20 rtrim(firstname) as firstname, lastname, email from myfhaleads where cam_last_email_time between '" + getetime + "' and '" + getstime;
            putsql = putsql + "' and client_id=" + client_id + "and credit='" + rating + "' and bounced=0";

            Console.WriteLine("sql=" + putsql);

            putcmd = new SqlCommand(putsql, putconnect);

            putcmd.CommandTimeout = 3000;

            reader = putcmd.ExecuteReader();

            sendflag = reader.HasRows;

            while (reader.Read())
            {
             
                //use this line for production
                emllist.Add(reader.GetString(0) + " " + reader.GetString(1) + " <" + reader.GetString(2) + ">");

                //use this line for sendgrid testing
                //emllist.Add(reader.GetString(0) + " " + reader.GetString(1) + " <" + reader.GetString(0) + "@sink.sendgrid.net>");

                //use this line for daily testing
                //emllist.Add("Mike <mikesweb@illinois.edu>");

                namlist.Add(reader.GetString(0));

            bodyhtml = bodyhtml + "\n\r" + reader.GetString(0) + " " + reader.GetString(1) + " <" + reader.GetString(2) + ">";

                cnt++;
            }
            reader.Close();

            putsql = "INSERT INTO sentlog (emailid,day,num) VALUES (" + emlid + ",'" + DateTime.Now + "'," + cnt + ")";
            putcmd = new SqlCommand(putsql, putconnect);
            putcmd.CommandTimeout = 3000;
            var udop = putcmd.ExecuteNonQuery();

            putconnect.Close();

            //adding John Smith to the recipient list for emails
            //emllist.Add("John <jssmith@myfha.us>");
            //namlist.Add("John Scott Smith");

            //build email message

            var client = new SmtpClient
            {
                Host = "smtp.sendgrid.net",
                Port = 587,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Credentials = new System.Net.NetworkCredential(ConfigurationManager.AppSettings["NetworkCredentialUserName"], ConfigurationManager.AppSettings["NetworkCredentialPassword"])
        };

            var mail = new System.Net.Mail.MailMessage();

            var header = new Header();

            header.SetTo(emllist);

            header.AddSubstitution("%name%", namlist);

            mail.From = new System.Net.Mail.MailAddress("webmaster@helicon7.org","MyFHA Services");
            mail.Subject = subj;
            mail.To.Add(new MailAddress("mikesweb@uillinois.edu"));
            mail.Body = bodytext;
            mail.IsBodyHtml = true;
            mail.Headers.Add("X-SMTPAPI", header.JsonString());
            ContentType mimeType = new System.Net.Mime.ContentType("text/html");
            AlternateView alternate = AlternateView.CreateAlternateViewFromString(bodyhtml, mimeType);
            mail.AlternateViews.Add(alternate);

            if (sendflag)
            {
                client.Send(mail);
            }

            Console.WriteLine("header=" + header.JsonString());

        }
    }
}

/*
 * public static string StripHTML(string HTMLText, bool decode = true)
        {
            Regex reg = new Regex("<[^>]+>", RegexOptions.IgnoreCase);
            var stripped = reg.Replace(HTMLText, "");
            return decode ? HttpUtility.HtmlDecode(stripped) : stripped;
        }
*/

