using Amazon.Lambda.Core;
using FileTransferConnector;
using FS.EndOfDay.Integration.Basel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Reflection.Metadata;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace BaselExportLambda;

public class Function
{

    /// <summary>
    /// A simple function that takes a string and does a ToUpper
    /// </summary>
    /// <param name="input"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    /// 
    public bool FunctionHandler(object cloudWatchEvents, ILambdaContext context)
    {

        var parametro = JsonConvert.DeserializeObject<Parameter>(cloudWatchEvents.ToString());
        context.Logger.LogLine("Environment: " + parametro.Environment);

        DateTime ReferenceDate = DateTime.Now;

        ReferenceDate = DateValidateExport.ValidateDate(ReferenceDate);

        Settings settings = Settings.FromAppSettingsAwsS3();

        decimal weekOfMonth = (Math.Ceiling(decimal.Parse((ReferenceDate.Day / 7.0).ToString())));
        string pathName = string.Format("/{0:####}/{1:##}/{2:##}/{3:yyyy_MM_dd}", ReferenceDate.Year, ReferenceDate.Month, weekOfMonth, ReferenceDate);
        List<TypeExportBasel> listTypeExportBasel = new List<TypeExportBasel> { TypeExportBasel.Credit, TypeExportBasel.Swap, TypeExportBasel.Others };

        try
        {
            context.Logger.LogLine("Start Function");
            using (BaselServiceBucketS3 serviceS3 = new BaselServiceBucketS3(settings, parametro.Environment))
            {
                foreach (TypeExportBasel type in listTypeExportBasel)
                {
                    string fileName = string.Format("Exportacao_" + (type == TypeExportBasel.Others ? "" : type + "_") + "{0:yyyyMMdd}_{1:yyyyMMddHHmmss}.csv", ReferenceDate, DateTime.Now);
                    context.Logger.LogLine("Start DownloadFileS3");
                    using (Stream file = serviceS3.DownloadFileS3(pathName, type))
                    {
                        context.Logger.LogLine("End DownloadFileS3");
                        if (file != null)
                        {
                            using (StreamReader fileReader = new StreamReader(file))
                            {
                                FileServiceHandler handler = new FileServiceHandler(fileName, fileReader.ReadToEnd(), parametro.Environment);
                                handler.SendToFileService(FileServiceHandler.Origin.Basel);

                                context.Logger.LogLine("End SendToFileService");
                            }
                        }
                        else
                        {
                            context.Logger.LogLine("File is null");
                        }
                    }
                }
            }
            context.Logger.LogLine("End Function");
        }
        catch (Exception e)
        {
            context.Logger.LogLine(e.Message);
            context.Logger.LogLine(e.StackTrace);
            return false;
        }

        return true;
    }
}
