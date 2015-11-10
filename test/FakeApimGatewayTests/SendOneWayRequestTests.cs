using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FakeApimGateway;
using Newtonsoft.Json.Linq;
using Xunit;

namespace FakeApimGatewayTests
{
    public class SendOneWayRequestTests
    {

        public string CreateSlackMessage(ApimContext context)
        {
            return new JObject(
                                new JProperty("username", $"APIM Alert for {context.Product.Name}"),
        			            new JProperty("icon_emoji", ":ghost:"),
        			            new JProperty("text", String.Format("{0} {1}\nHost: {2}\n{3} {4}\n User: {5}",
        			                                    context.Request.Method, 
        			                                    context.Request.Url.Path + context.Request.Url.QueryString,
        			                                    context.Request.Url.Host,
        			                                    context.Response.StatusCode,
        			                                    context.Response.StatusReason,
        			                                    context.User.Email
        			                                    ))
        			            ).ToString();

        }

        [Fact]
        public void MessageIncludesStaticUserName()
        {
            var context = new ApimContext(product: new Product() { Name = "foo" });
            var message = CreateSlackMessage(context);

            var jobject = JObject.Parse(message);
            Assert.Equal("APIM Alert for foo", (string)jobject.Property("username"));

        }

        [Fact]
        public void MessageTextStartsWithMethodAndUrl()
        {
            var context = new ApimContext();
            var message = CreateSlackMessage(context);

            var jobject = JObject.Parse(message);
            Assert.StartsWith("GET /", (string)jobject.Property("text"));

        }
    }
}
