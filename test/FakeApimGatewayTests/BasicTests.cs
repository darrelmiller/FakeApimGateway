using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FakeApimGateway;
using Xunit;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Collections.ObjectModel;

namespace FakeApimGatewayTests
{
    public class BasicTests
    {

        [Fact]
        public void FakeApiInfo()
        {
            var context = new ApimContext(api: new Api() { Id = "foo", Name = "App" });

            Assert.Equal("foo",context.Api.Id);
            Assert.Equal("App", context.Api.Name);
        }

        [Fact]
        public void FakeProductInfo()
        {
            var context = new ApimContext(product: new Product()
                {   Id = "foo",
                    Name = "App",
                    Apis = new List<IApi> { new Api()},
                    SubscriptionRequired = true
                });

            Assert.Equal("foo", context.Product.Id);
            Assert.Equal("App", context.Product.Name);
            Assert.Equal(1, context.Product.Apis.Count());
        }

        [Fact]
        public void IfHeaderEquals()
        {
            var request = new System.Net.Http.HttpRequestMessage()
            {
                RequestUri = new Uri("http://example.org/")
            };
            request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/xml"));
            var context = new ApimContext(requestMessage: request);

            Assert.True(context.Request.Headers.GetValueOrDefault("Accept","").Contains("application/xml"));
        }


        [Fact]
        public void GetAuthParameter()
        {
            var request = new System.Net.Http.HttpRequestMessage()
            {
                RequestUri = new Uri("http://example.org/")
            };
            var token = "xyzpq";
            request.Headers.Authorization = new AuthenticationHeaderValue("bearer",token);
            var context = new ApimContext(requestMessage: request);

            Assert.Equal(token,context.Request.Headers.GetValueOrDefault("Authorization","scheme param").Split(' ').Last());
        }
        
        


        [Fact]
        public void AccessSendRequestBody()
        {
            var request = new System.Net.Http.HttpRequestMessage()
            {
                RequestUri = new Uri("http://example.org/")
            };
            var token = new JObject( new JProperty("active",false));
            var context = new ApimContext(requestMessage: request);
            context.Variables["tokenstate"] = new Response(new HttpResponseMessage() { Content = new StringContent(token.ToString()) });

            Assert.True((bool)((IResponse)context.Variables["tokenstate"]).Body.As<JObject>()["active"] == false);
        }

        [Fact]
        public void AccessSendRequestBodySuccess()
        {
            var request = new System.Net.Http.HttpRequestMessage()
            {
                RequestUri = new Uri("http://example.org/")
            };
            var token = new JObject(new JProperty("active", true));
            var context = new ApimContext(requestMessage: request);
            context.Variables["tokenstate"] = new Response(new HttpResponseMessage() { Content = new StringContent(token.ToString()) } );

            Assert.False((bool)((IResponse)context.Variables["tokenstate"]).Body.As<JObject>()["active"] == false);
        }

      
    }

    /*
    <policies>
	<inbound>
		<choose>
			<when condition="@(String.Join(",",context.Request.Headers["Accept"]).Contains("application/xml"))">
				<set-header name="Accept" exists-action="override">
					<value>application/hal+json</value>
				</set-header>
				<set-variable name="ToXml" value="True" />
			</when>
			<otherwise>
				<set-variable name="ToXml" value="False" />
			</otherwise>
		</choose>
        <base/>
	</inbound>
	<backend>
        <base/>
	</backend>
	<outbound>
        <base/>
		<set-header name="Content-Type" exists-action="override">
			<value>application/json</value>
		</set-header>
		<choose>
			<when condition="@((string)context.Variables["ToXml"] == "True")">
				<json-to-xml apply="always" consider-accept-header="false" />
			</when>
		</choose>
	</outbound>
</policies>
    */
}
