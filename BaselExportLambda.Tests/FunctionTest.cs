using Xunit;
using Amazon.Lambda.TestUtilities;
using BrazilHolidays.Net;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace BaselExportLambda.Tests;

public class FunctionTest
{
    [Fact]
    public void TestToUpperFunction()
    {

        // Invoke the lambda function and confirm the string was upper cased.
        var function = new Function();
        var context = new TestLambdaContext();
        var obj = new { Environment = "DEV_ENV" };
        var upperCase = function.FunctionHandler(obj, context);

        Assert.Equal(true, upperCase);
    }

    [Fact]
    public void TestIfIsHoliday()
    {
        var ehFeriado = new DateTime(2022, 9, 7).IsHoliday();

        Assert.True(ehFeriado);
    }

    [Fact]
    public void TestIfIsNotHoliday()
    {
        var ehFeriado = new DateTime(2022, 12, 7).IsHoliday();

        Assert.False(ehFeriado);
    }

    [Fact]
    public void TestDateValidateExport()
    {
        var date = DateTime.Now;
        var dataRetornada = DateValidateExport.ValidateDate(date);

        Assert.NotNull(dataRetornada);
    }
}

