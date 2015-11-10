using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FakeApimGateway;
using Xunit;

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
    }
}
