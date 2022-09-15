using System;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace FileTransferConnector
{
    public class FileServiceHandler
    {
        private readonly string filename;
        private readonly string data;
        private const string URI_PRODUCTION = "http://financefiletransfer:9010/FileTransfer";
        private const string URI_UAT = "http://financefiletransfer-uat:9010/FileTransfer";

        public string EnvironmentSetting { get; set; }

        public string URI
        {
            get
            {
                if (this.EnvironmentSetting.Equals("RELEASE_ENV", StringComparison.OrdinalIgnoreCase))
                    return URI_PRODUCTION;
                else
                    return URI_UAT;
            }
        }

        public enum Origin
        {
            Accounting,
            AccountingExportFiles,
            Basel,
            CoeRecon,
            DynamicPortfolio,
            PassivoRecon,
            PrivateBond,
            OffshoreTRS,
            SaldoRemuneradoRecon,
        };

        public FileServiceHandler(string filename, string data, string env)
        {
            this.filename = filename;
            this.data = data;
            this.EnvironmentSetting = env;
        }

        public void SendToFileService(Origin type)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                Uri fileTransferURI = new Uri(URI);
                HttpRequestMessage requestMessage = MountRequestMessage(type);

                requestMessage.Content = MountRequestContent();

                Task<HttpResponseMessage> httpRequest = httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseContentRead, System.Threading.CancellationToken.None);
                HttpResponseMessage httpResponse = httpRequest.Result;
                httpResponse.EnsureSuccessStatusCode();
            }
        }

        public HttpRequestMessage MountRequestMessage(Origin type)
        {
            Uri fileTransferURI = new Uri(URI);
            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, fileTransferURI);
            requestMessage.Headers.ExpectContinue = false;
            requestMessage.Headers.ConnectionClose = true;
            requestMessage.Headers.Add("Origin", type.ToString());
            return requestMessage;
        }

        public MultipartFormDataContent MountRequestContent()
        {
            MultipartFormDataContent multiPartContent = new MultipartFormDataContent("----" + DateTime.Now.ToString());
            byte[] fileContents = Encoding.UTF8.GetBytes(this.data.ToString());
            byte[] utfbom = Encoding.UTF8.GetPreamble().Concat(fileContents).ToArray();
            ByteArrayContent byteArrayContent = new ByteArrayContent(utfbom);
            byteArrayContent.Headers.Add("Content-Type", "application/octet-stream");
            multiPartContent.Add(byteArrayContent, "File", this.filename);
            return multiPartContent;
        }
    }
}