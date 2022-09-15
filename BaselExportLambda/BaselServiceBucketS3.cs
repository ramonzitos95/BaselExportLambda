using Amazon.S3;
using Amazon.S3.Transfer;
using Amazon;
using Amazon.Runtime;
using Amazon.Athena;
using Amazon.Athena.Model;
using BaselExportLambda;

namespace FS.EndOfDay.Integration.Basel
{

    public class BaselServiceBucketS3 : IDisposable
    {
        private readonly IAmazonS3 amazonS3Client;
        private readonly IAmazonAthena amazonAthenaClient;
        private readonly string amazonS3BucketNameUpload;
        private readonly string amazonS3BucketNameDownload;
        private readonly string amazonAthenaDB;

        private string Path { get; set; }
        private string ReferenceDatePath { get; set; }
        private string Table { get; set; }
        private TypeExportBasel Type { get; set; }
      
        public BaselServiceBucketS3(Settings settings, string environment)
        {
            this.amazonAthenaDB = settings.AwsAthenaDb;

            if (environment.Equals("RELEASE_ENV", StringComparison.OrdinalIgnoreCase))
            {
                this.amazonS3Client = new AmazonS3Client(RegionEndpoint.SAEast1);
                this.amazonAthenaClient = new AmazonAthenaClient(RegionEndpoint.SAEast1);
                this.amazonS3BucketNameUpload = settings.AwsBucketProd + settings.AwsBucketNameUpload;
                this.amazonS3BucketNameDownload = settings.AwsBucketProd + settings.AwsBucketNameDownload;
            }
            else if(environment.Equals("UAT_ENV", StringComparison.OrdinalIgnoreCase))
            {
                this.amazonS3Client = new AmazonS3Client(RegionEndpoint.SAEast1);
                this.amazonAthenaClient = new AmazonAthenaClient(RegionEndpoint.SAEast1);
                this.amazonS3BucketNameUpload = settings.AwsBucketUat + settings.AwsBucketNameUpload;
                this.amazonS3BucketNameDownload = settings.AwsBucketUat + settings.AwsBucketNameDownload;
            }
            else if (environment.Equals("DEV_ENV", StringComparison.OrdinalIgnoreCase))
            {
                //SessionAWSCredentials tempCredentials =
                //new SessionAWSCredentials("ASIASBMIGARHWBM6JBYI",
                //                           "dnZxPuCCfXah+qjUiOLAiEdUHB4mgMQlE+K3pBo0",
                //                           "IQoJb3JpZ2luX2VjEBwaCXVzLWVhc3QtMSJHMEUCIAh/bqBtkuNUyxXViA2j1cdBmb7jPVS+Z5NshJVXkk6aAiEA7yB1qJgNt6lBnzcpFAvzFPrJR5iNi+FzpT0bkVudSFsqngMIlP//////////ARACGgwxNDA0MDg5MTUwMjMiDN+2PowJjFHn3/74ASryAtpGI0elXFRyQuN5QRU8d06M80gAgup+iz5BQJQQhBEq6tTAuD/igEjPIbq4in/bmNViGBG+wHvrPHbhVYS9UGWFSbZJECxxiJJiaNEd3O7u0dNdiYUivFltqtL6NNijssNfNiEdrmcntWYWpAqCBjTeeZxoV39qQD1sSpmitzCDMzrvE7cwBg/JAsodHI9P6eDSc0EKU/j/fuo66HG0zksbtiBqQ+RoIeGk/JVLNz/cJMcP05uqpCMD13gnL6k3OkU/pxG61agSI/JZFGcSfFJPh17AHeBbJjML5r82nrSag1alvpdhZR1NWoxj1Zqe+EgAzOZqMj0XXNmZyFT3lg31Pxi01RRINf26d3IJAOUdhm+3ql+4SKKXCQSbGUoGKTIPH+kZmHI0MhlC0kaAFSR8XcRYnUtsOQPhhI/m0sJyf4oV025AHElDmvIMF1XY6YeQjdx+1oeksJ0SJV/hwRq4/uyYgJI30xHUr7qIQ/dgld8wgtGUmAY6pgFuVvVjs4i9hdUFiiT7v40i7pyt7iV9ALJltxIv9ZVxhtJ0VJqaLbJLDZAbQ1u56vH6LKJCMpJPP60/G2h9ZtvxSN4CYig9zfTHajaDdLyLKs0Z7CWyzJ7GInGWEcRwl0Y/0XZTzhi1QGwP29boUGtKxWAO/A+j92tpm/itpkl0FkOyBCeXgxDpJvX0xI+UBZ/75dpZK6Xdoa6BGBclJFwU1Gt5A3TK");
                this.amazonS3Client = new AmazonS3Client(RegionEndpoint.USEast1);
                this.amazonAthenaClient = new AmazonAthenaClient(RegionEndpoint.USEast1);
                this.amazonS3BucketNameUpload = settings.AwsBucketDev + settings.AwsBucketNameUpload;
                this.amazonS3BucketNameDownload = settings.AwsBucketDev + settings.AwsBucketNameDownload;
            }

        }

        private string GetAlterTableAthena
        {
            get
            {

                string query;
                query = $@"ALTER TABLE {this.amazonAthenaDB}.{this.Table} ADD IF NOT EXISTS
                           PARTITION(data_referencia_pasta = '{this.ReferenceDatePath}') 
                           LOCATION '{"s3://" + (this.Table == "tb_basel_principal" ? this.amazonS3BucketNameUpload : (this.amazonS3BucketNameDownload + "/ControlExport")) + this.Path}'";

                return query;
            }

        }

        public void UploadFileS3(Stream streamFile, string _fileName, string pathName)
        {
            using (TransferUtility transferUtility = new TransferUtility(this.amazonS3Client))
            {
                transferUtility.UploadAsync(streamFile, this.amazonS3BucketNameUpload + pathName, _fileName).Wait();
            }
        }
        private string QueryExecutionAthena(string query)
        {
            QueryExecutionContext qContext = new QueryExecutionContext();
            ResultConfiguration resConf = new ResultConfiguration();
            StartQueryExecutionRequest queryRequest = new StartQueryExecutionRequest()
            {
                QueryString = query,
                QueryExecutionContext = qContext,
                ResultConfiguration = resConf
            };

            qContext.Database = this.amazonAthenaDB;
            resConf.OutputLocation = "s3://" + this.amazonS3BucketNameDownload + this.Path;


            Task<StartQueryExecutionResponse> info = this.amazonAthenaClient.StartQueryExecutionAsync(queryRequest);
            string idFileDownload = info.Result.QueryExecutionId;
            this.WaitForQueryToComplete(idFileDownload);

            return idFileDownload;

        }

        public Stream DownloadFileS3(string pathName, TypeExportBasel type)
        {
            Stream stream;
            string idFileDownload;

            this.Path = pathName;
            this.Type = type;
            this.ReferenceDatePath = pathName.Substring(pathName.Length - 10, 10).Replace("_", "-");

            this.Table = "tb_basel_control";
            this.QueryExecutionAthena(this.GetAlterTableControlAthena);

            this.Table = "tb_basel_principal";
            this.QueryExecutionAthena(this.GetAlterTableAthena);
            idFileDownload = this.QueryExecutionAthena(QueriesAthena.GetSelectTableAthena(this.Type, this.ReferenceDatePath));

            this.Table = "tb_basel_control_export";
            this.QueryExecutionAthena(this.GetAlterTableAthena);

            this.Path = "/ControlExport" + pathName;
            this.QueryExecutionAthena(QueriesAthena.GetSelectExportAthena(this.Type, this.ReferenceDatePath));

            using (TransferUtility transferUtility = new TransferUtility(this.amazonS3Client))
            {
                stream = transferUtility.OpenStream(this.amazonS3BucketNameDownload + pathName, $"{idFileDownload}.csv");
            }

            return stream;

        }

        private string GetAlterTableControlAthena
        {
            get
            {
                string query;
                query = $@"ALTER TABLE {this.amazonAthenaDB}.{this.Table} ADD IF NOT EXISTS
                           PARTITION(data_referencia_pasta = '{this.ReferenceDatePath}') 
                           LOCATION '{"s3://" + this.amazonS3BucketNameUpload + "/ArquivosBasel/parquetControl" + this.Path}'";

                return query;
            }

        }

        private void WaitForQueryToComplete(string queryExecutionId)
        {

            GetQueryExecutionRequest getQueryExecutionRequest = new GetQueryExecutionRequest();
            getQueryExecutionRequest.QueryExecutionId = queryExecutionId;
            GetQueryExecutionResponse getQueryExecutionResponse;


            bool isQueryStillRunning = true;
            while (isQueryStillRunning)
            {
                getQueryExecutionResponse = this.amazonAthenaClient.GetQueryExecutionAsync(getQueryExecutionRequest).Result;
                string queryState = getQueryExecutionResponse.QueryExecution.Status.State.ToString();

                if (queryState.Equals(QueryExecutionState.FAILED.ToString()))
                {
                    throw new Exception("The Amazon Athena query failed to run with error message: " + getQueryExecutionResponse.QueryExecution.Status.StateChangeReason.ToString());
                }
                else if (queryState.Equals(QueryExecutionState.CANCELLED.ToString()))
                {
                    throw new Exception("The Amazon Athena query was cancelled.");
                }
                else if (queryState.Equals(QueryExecutionState.SUCCEEDED.ToString()))
                {
                    isQueryStillRunning = false;
                }
                else
                {

                    Thread.Sleep(6000);
                }
            }
        }

        public void Dispose()
        {
            this.amazonS3Client.Dispose();
            this.amazonAthenaClient.Dispose();

        }
    }

    public class Settings
    {
        public Settings(string AwsBucketNameUpload,
                        string AwsBucketNameDownload,
                        string awsBucketRegion,
                        string awsAthenaDb,
                        string awsBucketUat,
                        string awsBucketProd,
                        string awsBucketDev)
        {
            this.AwsBucketNameUpload = AwsBucketNameUpload;
            this.AwsBucketNameDownload = AwsBucketNameDownload;
            this.AwsBucketRegion = awsBucketRegion;
            this.AwsAthenaDb = awsAthenaDb;
            this.AwsBucketUat = awsBucketUat;
            this.AwsBucketProd = awsBucketProd;
            this.AwsBucketDev = awsBucketDev;
        }

        public string AwsBucketNameUpload { get; }
        public string AwsBucketNameDownload { get; }
        public string AwsBucketRegion { get; }
        public string AwsAthenaDb { get; set; }
        public string AwsBucketUat { get; set; }
        public string AwsBucketProd { get; set; }
        public string AwsBucketDev { get; set; }
        public string Environment { get; set; }

        private const string AWS_BUCKET_UPLOAD = "/ArquivosBasel/parquet";
        private const string AWS_BUCKET_DOWNLOAD = "/ArquivosBasel/csv";
        private const string AWS_BUCKET_REGION = "us-east-1";
        private const string AWS_ATHENA_DB = "dbbasel";
        private const string AWS_BUCKET_UAT = "bucket-mars-basel-export";
        private const string AWS_BUCKET_PROD = "bucket-mars-basel-export-prod";
        private const string AWS_BUCKET_DEV = "bucket-mars-basel-export-dev";

        public static Settings FromAppSettingsAwsS3()
        {
            return new Settings
           (
               AWS_BUCKET_UPLOAD,
               AWS_BUCKET_DOWNLOAD,
               AWS_BUCKET_REGION,
               AWS_ATHENA_DB,
               AWS_BUCKET_UAT,
               AWS_BUCKET_PROD,
               AWS_BUCKET_DEV
           );
        }

    }

}
