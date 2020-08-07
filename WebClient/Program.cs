using System;
using System.Data;
using System.Data.OleDb;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using Renci.SshNet;
using HtmlAgilityPack;
using System.Threading;

namespace WebClient
{
    class Program
    {
        public static (SshClient SshClient, uint Port) ConnectSsh(string sshHostName, string sshUserName, string sshPassword = null,
    string sshKeyFile = null, string sshPassPhrase = null, int sshPort = 63222, string databaseServer = "localhost", int databasePort = 3306)
        {
            if (string.IsNullOrEmpty(sshHostName))
                throw new ArgumentException($"{nameof(sshHostName)} must be specified.", nameof(sshHostName));
            if (string.IsNullOrEmpty(sshHostName))
                throw new ArgumentException($"{nameof(sshUserName)} must be specified.", nameof(sshUserName));
            if (string.IsNullOrEmpty(sshPassword) && string.IsNullOrEmpty(sshKeyFile))
                throw new ArgumentException($"One of {nameof(sshPassword)} and {nameof(sshKeyFile)} must be specified.");
            if (string.IsNullOrEmpty(databaseServer))
                throw new ArgumentException($"{nameof(databaseServer)} must be specified.", nameof(databaseServer));

            var authenticationMethods = new List<AuthenticationMethod>();
            if (!string.IsNullOrEmpty(sshKeyFile))
            {
                authenticationMethods.Add(new PrivateKeyAuthenticationMethod(sshUserName,
                    new PrivateKeyFile(sshKeyFile, string.IsNullOrEmpty(sshPassPhrase) ? null : sshPassPhrase)));
            }
            if (!string.IsNullOrEmpty(sshPassword))
            {
                authenticationMethods.Add(new PasswordAuthenticationMethod(sshUserName, sshPassword));
            }

            var sshClient = new SshClient(new ConnectionInfo(sshHostName, sshPort, sshUserName, authenticationMethods.ToArray()));
            sshClient.Connect();

            var forwardedPort = new ForwardedPortLocal("127.0.0.1", databaseServer, (uint)databasePort);
            sshClient.AddForwardedPort(forwardedPort);
            forwardedPort.Start();

            return (sshClient, forwardedPort.BoundPort);
        }
        async static void postRequest()
        {
            var handler = new HttpClientHandler
            {
                UseCookies = false,

                AutomaticDecompression = ~DecompressionMethods.None,
                ServerCertificateCustomValidationCallback = (requestMessage, certificate, chain, policyErrors) => true
            };

            using var httpClient = new HttpClient(handler);
            using (var request = new HttpRequestMessage(new HttpMethod("POST"), "https://192.168.100.51/index.php"))
            {
                request.Headers.TryAddWithoutValidation("Connection", "keep-alive");
                request.Headers.TryAddWithoutValidation("Cache-Control", "max-age=0");
                request.Headers.TryAddWithoutValidation("Upgrade-Insecure-Requests", "1");
                request.Headers.TryAddWithoutValidation("Origin", "https://192.168.100.51");
                request.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/84.0.4147.89 Safari/537.36");
                request.Headers.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*;q=0.8,application/signed-exchange;v=b3;q=0.9");
                request.Headers.TryAddWithoutValidation("Sec-Fetch-Site", "same-origin");
                request.Headers.TryAddWithoutValidation("Sec-Fetch-Mode", "navigate");
                request.Headers.TryAddWithoutValidation("Sec-Fetch-User", "?1");
                request.Headers.TryAddWithoutValidation("Sec-Fetch-Dest", "document");
                request.Headers.TryAddWithoutValidation("Referer", "https://192.168.100.51/index.php");
                request.Headers.TryAddWithoutValidation("Accept-Language", "ru-RU,ru;q=0.9,en-US;q=0.8,en;q=0.7");
                request.Headers.TryAddWithoutValidation("Cookie", "elastixSession=ub2irbrlb8rdi4g9k2vo76s7a7");

                request.Content = new StringContent("input_user=admin&input_pass=3899955&submit_login=");
                request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/x-www-form-urlencoded");

                var response = await httpClient.SendAsync(request);
            }
        }
        async static void getRequest(object obj)
        {
            // SSH and DATABASE CONNECTION
            var server = "140.82.36.37";
            var sshUserName = "root";
            var sshPassword = "1+Vp6xtrV!9C)QnL";
            var databaseUserName = "root";
            var databasePassword = "=RqQf!s*&W6]g??G";

            var (sshClient, localPort) = ConnectSsh(server, sshUserName, sshPassword);

            using (sshClient)
            {
                MySqlDataReader myReader;
                MySqlConnectionStringBuilder csb = new MySqlConnectionStringBuilder
                {
                    Server = "127.0.0.1",
                    Port = localPort,
                    UserID = databaseUserName,
                    Password = databasePassword,
                    Database = "acum_test"
                };

                using var connection = new MySqlConnection(csb.ConnectionString);
                connection.Open();

                using var cmd = new MySqlCommand
                {
                    Connection = connection
                };

                cmd.CommandText = $"SELECT * FROM commands";
                myReader = cmd.ExecuteReader();

                var handler = new HttpClientHandler
                {
                    UseCookies = false,

                    AutomaticDecompression = ~DecompressionMethods.None,
                    ServerCertificateCustomValidationCallback = (requestMessage, certificate, chain, policyErrors) => true
                };

                List<string> firstPageData = new List<string>();

                using var httpClient = new HttpClient(handler);
                myReader.Read();

                List<string> myReaderResults = new List<string>();
                List<string> distinctResults = new List<string>();

                for (int k = 0; k < myReader.FieldCount; k++)
                {
                    myReaderResults.Add(myReader[k].ToString());
                }

                myReader.Close();

                // Comparing date and phone
                cmd.CommandText = $"SELECT date FROM calls LIMIT 20";
                myReader = cmd.ExecuteReader();

                while (myReader.Read())
                {
                    distinctResults.Add(myReader["date"].ToString());
                }

                myReader.Close();

                List<string> afterFirstPageResults = new List<string>();

                // count data after 20 rows
                cmd.CommandText = $"SELECT COUNT(date) - 20 FROM calls";
                myReader = cmd.ExecuteReader();

                myReader.Read();

                string countRows = myReader[0].ToString();

                myReader.Close();

                // get data after 20 rows
                cmd.CommandText = $"SELECT date FROM calls LIMIT {countRows} OFFSET 20";
                myReader = cmd.ExecuteReader();

                while(myReader.Read())
                {
                    afterFirstPageResults.Add(myReader["date"].ToString());
                }

                myReader.Close();

                // First Page
                using var firstPage = new HttpRequestMessage(new HttpMethod("GET"), myReaderResults[1].ToString());
                firstPage.Headers.TryAddWithoutValidation("Connection", "keep-alive");
                firstPage.Headers.TryAddWithoutValidation("Upgrade-Insecure-Requests", "1");
                firstPage.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/84.0.4147.89 Safari/537.36");
                firstPage.Headers.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*;q=0.8,application/signed-exchange;v=b3;q=0.9");
                firstPage.Headers.TryAddWithoutValidation("Sec-Fetch-Site", "same-origin");
                firstPage.Headers.TryAddWithoutValidation("Sec-Fetch-Mode", "navigate");
                firstPage.Headers.TryAddWithoutValidation("Sec-Fetch-User", "?1");
                firstPage.Headers.TryAddWithoutValidation("Sec-Fetch-Dest", "document");
                firstPage.Headers.TryAddWithoutValidation("Referer", "https://192.168.100.51/index.php");
                firstPage.Headers.TryAddWithoutValidation("Accept-Language", "ru-RU,ru;q=0.9,en-US;q=0.8,en;q=0.7");
                firstPage.Headers.TryAddWithoutValidation("Cookie", "elastixSession=kb9i31unb38l162agodea0g4k5");

                HttpResponseMessage responseFirstPage = await httpClient.SendAsync(firstPage);

                HtmlDocument docFirstPage = new HtmlDocument();
                docFirstPage.LoadHtml(responseFirstPage.Content.ReadAsStringAsync().Result);
                var myDivFirstPage = docFirstPage.DocumentNode.SelectNodes("//table[@class='elastix-standard-table']//tbody//td[@class='table_data']//text()");
                var lastRowFirstPage = docFirstPage.DocumentNode.SelectNodes("//table[@class='elastix-standard-table']//tbody//td[@class='table_data_last_row']//text()");

                //for first page
                foreach (var first_page in myDivFirstPage)
                {
                    firstPageData.Add(first_page.InnerText);
                }

                //first page last row
                foreach (var first_page_last_row in lastRowFirstPage)
                {
                    firstPageData.Add(first_page_last_row.InnerText);
                }

                for (int k = 0; k < firstPageData.Count; k += 9)
                {
                    int date = k + 1;
                    int phone = k + 2;
                    int duration = k + 4;

                    if (!distinctResults.Contains($"{firstPageData[k]} {firstPageData[date]}"))
                    {
                        cmd.CommandText = $"INSERT INTO calls(calls_id, phone, date, duration) VALUES('12', '{firstPageData[phone]}', '{firstPageData[k]} {firstPageData[date]}', '{firstPageData[duration]}')";
                        cmd.ExecuteNonQuery();
                    }
                }

                for (int i = 1; i <= 2000; i += 20)
                {
                    using var request = new HttpRequestMessage(new HttpMethod("GET"), $"{myReaderResults[1]}&nav=next&start={i}");
                    request.Headers.TryAddWithoutValidation("Connection", "keep-alive");
                    request.Headers.TryAddWithoutValidation("Upgrade-Insecure-Requests", "1");
                    request.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/84.0.4147.89 Safari/537.36");
                    request.Headers.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9");
                    request.Headers.TryAddWithoutValidation("Sec-Fetch-Site", "same-origin");
                    request.Headers.TryAddWithoutValidation("Sec-Fetch-Mode", "navigate");
                    request.Headers.TryAddWithoutValidation("Sec-Fetch-User", "?1");
                    request.Headers.TryAddWithoutValidation("Sec-Fetch-Dest", "document");
                    request.Headers.TryAddWithoutValidation("Referer", "https://192.168.100.51/index.php");
                    request.Headers.TryAddWithoutValidation("Accept-Language", "ru-RU,ru;q=0.9,en-US;q=0.8,en;q=0.7");
                    request.Headers.TryAddWithoutValidation("Cookie", "elastixSession=kb9i31unb38l162agodea0g4k5");

                    var response = await httpClient.SendAsync(request);

                    // XPath
                    HtmlDocument doc = new HtmlDocument();
                    doc.LoadHtml(response.Content.ReadAsStringAsync().Result);
                    HtmlNode docNode = doc.DocumentNode;
                    var myDiv = doc.DocumentNode.SelectNodes("//table[@class='elastix-standard-table']//tbody//td[@class='table_data']//text()");
                    var lastRow = doc.DocumentNode.SelectNodes("//table[@class='elastix-standard-table']//tbody//td[@class='table_data_last_row']//text()");
                    HtmlNodeCollection idNodes = docNode.SelectNodes("//table[@class='elastix-standard-table']//tbody//td[@class='table_data']//input");
                    HtmlNodeCollection idNodesLastRow = docNode.SelectNodes("//table[@class='elastix-standard-table']//tbody//td[@class='table_data_last_row']//input");

                    // other pages
                    List<string> data = new List<string>();

                    if (myDiv.Count > 0 && myDiv != null)
                    {
                        for (int j = 0; j < myDiv.Count; j++)
                        {
                            if (myDiv[j].InnerText == "No records match the filter criteria")
                            {
                                data.Add(myDiv[j].InnerText);
                                break;
                            }
                            data.Add(myDiv[j].InnerText);
                        }
                    }

                    List<string> dataLastRow = new List<string>();

                    //last row
                    if (data.Contains("No records match the filter criteria"))
                    {
                        break;
                    }
                    else
                    {
                        for (int j = 0; j < lastRow.Count; j++)     
                        {
                            dataLastRow.Add(lastRow[j].InnerText);
                        }
                    }

                    data.AddRange(dataLastRow);

                    for (int k = 0; k < data.Count; k += 9)
                    {
                        int date = k + 1;
                        int phone = k + 2;
                        int duration = k + 4;

                        if (!afterFirstPageResults.Contains($"{data[k]} {data[date]}"))
                        {
                            cmd.CommandText = $"INSERT INTO calls(calls_id, phone, date, duration) VALUES('12', '{data[phone]}', '{data[k]} {data[date]}', '{data[duration]}')";
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }   
        }
        static void Main()
        {
            postRequest();
            TimerCallback tm = new TimerCallback(getRequest);

            Timer timer = new Timer(tm, null, 0, 300000);
            Console.ReadLine();
        }
    }
}

