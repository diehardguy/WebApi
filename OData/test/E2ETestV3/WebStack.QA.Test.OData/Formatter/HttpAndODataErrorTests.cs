﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using System.Web.Http.OData;
using System.Web.Http.OData.Builder;
using System.Web.Http.OData.Extensions;
using Microsoft.Data.Edm;
using Nuwa;
using WebStack.QA.Common.XUnit;
using WebStack.QA.Test.OData.Common;
using Xunit;
using Xunit.Extensions;

namespace WebStack.QA.Test.OData.Formatter
{
    public enum ErrorType
    {
        ThrowExceptionInAction,
        ThrowHttpResponseExceptionInAction,
        ResponseErrorResponseInAction,
        ResponseHttpErrorResponseInAction,
        ReturnODataErrorResponseInAction,
        QueryableThrowException,
        NotSupportGetException,
        ActionNotFound,
        ModelStateError
    }

    public class HttpError_Todo
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public ErrorType ErrorType { get; set; }
        public IEnumerable<HttpError_Item> Items { get; set; }
    }

    public class HttpError_Item
    {
        public int ID { get; set; }
        public string Name { get; set; }
    }

    public class HttpError_TodoController : ODataController
    {
        [HttpPost]
        public HttpError_Todo Get(int key)
        {
            return null;
        }

        [EnableQuery]
        public HttpResponseMessage Post(HttpError_Todo todo)
        {
            if (!ModelState.IsValid)
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState);
            }

            switch (todo.ErrorType)
            {
                case ErrorType.ThrowExceptionInAction:
                    throw new Exception("ThrowExceptionInAction");
                case ErrorType.ThrowHttpResponseExceptionInAction:
                    throw new HttpResponseException(
                        this.Request.CreateErrorResponse(
                            HttpStatusCode.NotFound,
                            new Exception("ThrowHttpResponseExceptionInAction")));
                case ErrorType.ResponseErrorResponseInAction:
                    return this.Request.CreateErrorResponse(
                        HttpStatusCode.NotFound,
                        new Exception("ResponseErrorResponseInAction"));
                case ErrorType.ReturnODataErrorResponseInAction:
                    return this.Request.CreateErrorResponse(HttpStatusCode.InternalServerError, new Microsoft.Data.OData.ODataError()
                    {
                        Message = "ReturnODataErrorResponseInActionMessage",
                        ErrorCode = "ReturnODataErrorResponseInActionCode",
                        InnerError = new Microsoft.Data.OData.ODataInnerError(new Exception("ReturnODataErrorResponseInActionException"))
                    });
                case ErrorType.ResponseHttpErrorResponseInAction:
                    return
                        this.Request.CreateResponse<HttpError>(
                            HttpStatusCode.NotFound,
                            new HttpError("ResponseHttpErrorResponseInAction"));
                default:
                    return null;
            }
        }

        [EnableQuery]
        public HttpError_Item GetItems(int key)
        {
            return null;
        }
    }

    public class HttpAndODataErrorAlwaysIncludeDetailsTests : ODataTestBase
    {
        public static TheoryDataSet<ErrorType, HttpStatusCode, string, string> TestData
        {
            get
            {
                var testData = new List<object[]>
                {
                    new object[] { ErrorType.ThrowExceptionInAction, HttpStatusCode.InternalServerError, "ThrowExceptionInAction" },
                    new object[] { ErrorType.ThrowHttpResponseExceptionInAction, HttpStatusCode.NotFound, "ThrowHttpResponseExceptionInAction" },
                    new object[] { ErrorType.ResponseErrorResponseInAction, HttpStatusCode.NotFound, "ResponseErrorResponseInAction" },
                    new object[] { ErrorType.ResponseHttpErrorResponseInAction, HttpStatusCode.NotFound, "ResponseHttpErrorResponseInAction" },
                    new object[] { ErrorType.QueryableThrowException, HttpStatusCode.InternalServerError, "Cannot serialize a null 'entry'." },
                    new object[] { ErrorType.NotSupportGetException, HttpStatusCode.MethodNotAllowed, "The requested resource does not support http method 'GET'." },
                    new object[] { ErrorType.ActionNotFound, HttpStatusCode.NotFound, "No HTTP resource was found that matches the request URI" },
                    new object[] { ErrorType.ReturnODataErrorResponseInAction, HttpStatusCode.InternalServerError, "ReturnODataErrorResponseInActionException" },
                    new object[] { ErrorType.ModelStateError, HttpStatusCode.BadRequest, "Requested value 'NotExistType' was not found." }
                };
                var acceptHeaders = new List<string> 
                {
                    //"application/json;odata=minimalmetadata;streaming=true",
                    "application/json;odata=minimalmetadata;streaming=false",
                    "application/json;odata=minimalmetadata",
                    //"application/json;odata=fullmetadata;streaming=true",
                    "application/json;odata=fullmetadata;streaming=false",
                    "application/json;odata=fullmetadata",
                    //"application/json;odata=nometadata;streaming=true",
                    //"application/json;odata=nometadata;streaming=false",
                    //"application/json;odata=nometadata",
                    //"application/json;streaming=true",
                    "application/json;streaming=false",
                    "application/json"
                };
                var theory = new TheoryDataSet<ErrorType, HttpStatusCode, string, string>();
                foreach (var acceptHeader in acceptHeaders)
                {
                    foreach (dynamic item in testData)
                    {
                        theory.Add(item[0], item[1], item[2], acceptHeader);
                    }
                }
                return theory;
            }
        }

        public HttpRequestMessage CreateRequestMessage(ErrorType errorType, string header)
        {
            var request = new HttpRequestMessage();
            switch (errorType)
            {
                case ErrorType.ThrowExceptionInAction:
                    request.Method = HttpMethod.Post;
                    request.RequestUri = new Uri(this.BaseAddress + "/HttpError_Todo/");
                    request.Content = new StringContent(@"{ ""ErrorType"": ""ThrowExceptionInAction"" }");
                    request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(header);
                    break;
                case ErrorType.ThrowHttpResponseExceptionInAction:
                    request.Method = HttpMethod.Post;
                    request.RequestUri = new Uri(this.BaseAddress + "/HttpError_Todo/");
                    request.Content = new StringContent(@"{ ""ErrorType"": ""ThrowHttpResponseExceptionInAction"" }");
                    request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(header);
                    break;
                case ErrorType.ResponseErrorResponseInAction:
                    request.Method = HttpMethod.Post;
                    request.RequestUri = new Uri(this.BaseAddress + "/HttpError_Todo/");
                    request.Content = new StringContent(@"{ ""ErrorType"": ""ResponseErrorResponseInAction"" }");
                    request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(header);
                    break;
                case ErrorType.ResponseHttpErrorResponseInAction:
                    request.Method = HttpMethod.Post;
                    request.RequestUri = new Uri(this.BaseAddress + "/HttpError_Todo/");
                    request.Content = new StringContent(@"{ ""ErrorType"": ""ResponseHttpErrorResponseInAction"" }");
                    request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(header);
                    break;
                case ErrorType.ReturnODataErrorResponseInAction:
                    request.Method = HttpMethod.Post;
                    request.RequestUri = new Uri(this.BaseAddress + "/HttpError_Todo/");
                    request.Content = new StringContent(@"{ ""ErrorType"": ""ReturnODataErrorResponseInAction"" }");
                    request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(header);
                    break;
                case ErrorType.QueryableThrowException:
                    request.Method = HttpMethod.Get;
                    request.RequestUri = new Uri(this.BaseAddress + "/HttpError_Todo(1)/Items");
                    request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse(header));
                    break;
                case ErrorType.NotSupportGetException:
                    request.Method = HttpMethod.Get;
                    request.RequestUri = new Uri(this.BaseAddress + "/HttpError_Todo(1)");
                    request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse(header));
                    break;
                case ErrorType.ActionNotFound:
                    request.Method = HttpMethod.Get;
                    request.RequestUri = new Uri(this.BaseAddress + "/HttpError_Todo(1)/Name");
                    request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse(header));
                    break;
                case ErrorType.ModelStateError:
                    request.Method = HttpMethod.Post;
                    request.RequestUri = new Uri(this.BaseAddress + "/HttpError_Todo/");
                    request.Content = new StringContent(@"{ ""ErrorType"": ""NotExistType"" }");
                    request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(header);
                    break;
                default:
                    break;
            }

            return request;
        }

        [NuwaConfiguration]
        public static void UpdateConfiguration(HttpConfiguration configuration)
        {
            configuration.IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Always;
            configuration.Formatters.JsonFormatter.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;

            configuration.EnableODataSupport(GetEdmModel(configuration));
            configuration.AddODataQueryFilter(new EnableQueryAttribute() { PageSize = 100 });
        }

        [NuwaWebConfig]
        public static void UpdateWebConfig(WebStack.QA.Common.WebHost.WebConfigHelper config)
        {
            config.AddODataLibAssemblyRedirection();
        }

        public static IEdmModel GetEdmModel(HttpConfiguration configuration)
        {
            var mb = new ODataConventionModelBuilder(configuration);
            mb.EntitySet<HttpError_Todo>("HttpError_Todo");
            mb.EntitySet<HttpError_Item>("HttpError_Item");
            return mb.GetEdmModel();
        }

        [TheoryAttribute]
        [PropertyData("TestData")]
        public virtual void TestHttpErrorInAction(ErrorType errorType, HttpStatusCode code, string message, string header)
        {
            if (header != string.Empty)
            {
                SetClient(header);
            }
            var response = this.Client.SendAsync(CreateRequestMessage(errorType, header)).Result;
            Assert.Equal(code, response.StatusCode);
            Assert.Contains(message, response.Content.ReadAsStringAsync().Result);
        }

        public void SetClient(string header)
        {
            Client.DefaultRequestHeaders.Accept.Clear();
            Client.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue.Parse(header));
        }
    }

    public class HttpAndODataErrorNeverIncludeDetailsTests : HttpAndODataErrorAlwaysIncludeDetailsTests
    {
        public static TheoryDataSet<ErrorType, HttpStatusCode, string, string> TestData2
        {
            get
            {
                var testData = new List<object[]>
                {
                    new object[] { ErrorType.ThrowExceptionInAction, HttpStatusCode.InternalServerError, "ThrowExceptionInAction" },
                    new object[] { ErrorType.ThrowHttpResponseExceptionInAction, HttpStatusCode.NotFound, "ThrowHttpResponseExceptionInAction" },
                    new object[] { ErrorType.ResponseErrorResponseInAction, HttpStatusCode.NotFound, "ResponseErrorResponseInAction" },
                    new object[] { ErrorType.QueryableThrowException, HttpStatusCode.InternalServerError, "Cannot serialize a null 'entry'." },
                    new object[] { ErrorType.ReturnODataErrorResponseInAction, HttpStatusCode.InternalServerError, "ReturnODataErrorResponseInActionException" },
                    new object[] { ErrorType.ModelStateError, HttpStatusCode.BadRequest, "Requested value 'NotExistType' was not found." }
                };
                var acceptHeaders = new List<string> 
                {
                    //"application/json;odata=minimalmetadata;streaming=true",
                    "application/json;odata=minimalmetadata;streaming=false",
                    "application/json;odata=minimalmetadata",
                    //"application/json;odata=fullmetadata;streaming=true",
                    "application/json;odata=fullmetadata;streaming=false",
                    "application/json;odata=fullmetadata",
                    //"application/json;odata=nometadata;streaming=true",
                    //"application/json;odata=nometadata;streaming=false",
                    //"application/json;odata=nometadata",
                    //"application/json;streaming=true",
                    "application/json;streaming=false",
                    "application/json"
                };
                var theory = new TheoryDataSet<ErrorType, HttpStatusCode, string, string>();
                foreach (var acceptHeader in acceptHeaders)
                {
                    foreach (dynamic item in testData)
                    {
                        theory.Add(item[0], item[1], item[2], acceptHeader);
                    }
                }
                return theory;
            }
        }

        [NuwaConfiguration]
        public static void UpdateConfiguration2(HttpConfiguration configuration)
        {
            configuration.IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Never;
            configuration.Formatters.JsonFormatter.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;

            configuration.EnableODataSupport(GetEdmModel(configuration));
            configuration.AddODataQueryFilter(new EnableQueryAttribute() { PageSize = 100 });
        }

        [NuwaWebConfig]
        public static new void UpdateWebConfig(WebStack.QA.Common.WebHost.WebConfigHelper config)
        {
            config.AddODataLibAssemblyRedirection();
        }

        [Theory]
        [PropertyData("TestData2")]
        public override void TestHttpErrorInAction(ErrorType errorType, HttpStatusCode code, string message, string header)
        {
            if (header != string.Empty)
            {
                SetClient(header);
            }
            var response = this.Client.SendAsync(CreateRequestMessage(errorType, header)).Result;
            Assert.Equal(code, response.StatusCode);
            string responseMessage = response.Content.ReadAsStringAsync().Result;
            Assert.DoesNotContain(message, responseMessage);
        }
    }
}
