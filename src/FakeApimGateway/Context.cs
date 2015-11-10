using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace FakeApimGateway
{
    public class ApimContext : IContext
    {
        public IApi Api { get; private set; }
        public IDeployment Deployment { get; private set; }
        public IOperation Operation { get; private set; }
        public IProduct Product { get; private set; }
        public IRequest Request { get; private set; }
        public IResponse Response { get; private set; }
        public ISubscription Subscription { get; private set; }
        public bool Tracing { get; private set; }
        public IUser User { get; private set; }
        public IDictionary<string, object> Variables { get; private set; }
        void Trace(string message) { if (_Logger != null) _Logger(message); }

        private Action<string> _Logger;
        public ApimContext(HttpRequestMessage requestMessage = null,
                        HttpResponseMessage responseMessage = null,
                        IDictionary<string, object> variables = null,
                        Action<string> logger = null,
                        IApi api = null,
                        IDeployment deployment = null,
                        IOperation operation = null,
                        IProduct product = null,
                        ISubscription subscription = null)
        {
            Api = api ?? new Api();
            Deployment = deployment ?? new Deployment();
            Operation = operation ?? new Operation();
            Product = product ?? new Product();
            Request = new Request(requestMessage);
            Response = new Response(responseMessage);
            Subscription = subscription ?? new Subscription();
            User = new User();
            _Logger = logger;

            Variables = variables ?? new Dictionary<string, object>();
        }
    }


    public class Subscription : ISubscription
    {
        public DateTime CreatedTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string Id { get; set; }
        public string Key { get; set; }
        public string Name { get; set; }
        public string PrimaryKey { get; set; }
        public string SecondaryKey { get; set; }
        public DateTime? StartDate { get; set; }
    }

    public class Api : IApi
    {
        public Api()
        {
            Id = "DefaultApiId";
            Name = "DefaultApiName";
            Path = "DefaultApiPath";
            ServiceUrl = new ApimUrl(new Uri("https://example.org"));

        }
        public string Id { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public IUrl ServiceUrl { get; set; }
    }

    public class Product : IProduct
    {
        public Product()
        {
            Apis = new List<IApi>();
            Groups = new List<IGroup>();
        }
        public IEnumerable<IApi> Apis { get; set; }
        public bool ApprovalRequired { get; set; }
        public IEnumerable<IGroup> Groups { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
        public ProductState State { get; set; }
        public int? SubscriptionLimit { get; set; }
        public bool SubscriptionRequired { get; set; }
    }

    public class Deployment : IDeployment
    {
        public string Region { get; set; }
        public string ServiceName { get; set; }
    }

    public class Operation : IOperation
    {
        public string Id { get; set; }
        public string Method { get; set; }
        public string Name { get; set; }
        public string UrlTemplate { get; set; }
    }

    public class Response : IResponse
    {
        private HttpResponseMessage _Response;
        private IHeaders _Headers;
        private MessageBody _Body;

        public Response(HttpResponseMessage response = null)
        {
            _Response = response ?? new HttpResponseMessage();
            _Headers = new IHeaders();
            if (response != null)
            {
                foreach (var h in response.Headers)
                {
                    _Headers.Add(h.Key, h.Value.ToArray());
                }
                if (response.Content != null)
                {
                    foreach (var h in response.Content.Headers)
                    {
                        _Headers.Add(h.Key, h.Value.ToArray());
                    }
                    _Headers.Add("Content-Length", new string[] { response.Content.Headers.ContentLength.ToString() });
                    _Body = new MessageBody(response.Content);
                }
            }
        }

        public IMessageBody Body
        {
            get { return _Body; }
        }

        public IHeaders Headers
        {
            get { return _Headers; }
        }

        public int StatusCode
        {
            get { return (int)_Response.StatusCode; }
        }

        public string StatusReason
        {
            get { return _Response.ReasonPhrase; }
        }
    }

    internal class Request : IRequest
    {
        private ApimUrl _Url;
        private HttpRequestMessage _Request;
        private IHeaders _Headers;
        private MessageBody _Body;
        public Request(HttpRequestMessage request = null)
        {
            _Request = request ?? new HttpRequestMessage()
            {
                RequestUri = new Uri("https://example.org")
            };

            _Url = new ApimUrl(_Request.RequestUri);
            _Headers = new IHeaders();

            if (request != null)
            {
                _Headers.Add("Host", new string[] { _Request.RequestUri.Host });
                foreach (var h in request.Headers)
                {
                    _Headers.Add(h.Key, h.Value.ToArray());
                }
                if (request.Content != null)
                {
                    foreach (var h in request.Content.Headers)
                    {
                        _Headers.Add(h.Key, h.Value.ToArray());
                    }
                    _Headers.Add("Content-Length", new string[] { request.Content.Headers.ContentLength.ToString() });
                    _Body = new MessageBody(request.Content);
                }
            }

        }

        public IMessageBody Body
        {
            get { return _Body; }
        }

        public IHeaders Headers
        {
            get { return _Headers; }
        }

        public string Method
        {
            get { return _Request.Method.ToString(); }
        }

        public IUrl Url
        {
            get { return _Url; }
        }
    }

    public class MessageBody : IMessageBody
    {
        private HttpContent _Content;
        public MessageBody(HttpContent content)
        {
            _Content = content;
        }
        public T As<T>(bool preserveContent = false) where T : class
        {
            return _Content.ReadAsStringAsync().Result as T;
        }
    }
    public class ApimUrl : IUrl
    {
        private Uri _Uri;

        public ApimUrl(Uri uri)
        {
            _Uri = uri;
            Query = new Dictionary<string, string[]>();
        }

        public string Host
        {
            get { return _Uri.Host; }
        }

        public string Path
        {
            get { return _Uri.AbsolutePath; }
        }

        public int Port
        {
            get { return _Uri.Port; }
        }

        public IReadOnlyDictionary<string, string[]> Query
        {
            get; set;
        }

        public string QueryString
        {
            get { return _Uri.Query; }

        }

        public string Scheme
        {
            get { return _Uri.Scheme; }
        }
    }

    public class User : IUser
    {
        public User()
        {
            Groups = new List<IGroup>();
            Identities = new List<IUserIdentity>();
        }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public IEnumerable<IGroup> Groups { get; set; }
        public string Id { get; set; }
        public IEnumerable<IUserIdentity> Identities { get; set; }
        public string LastName { get; set; }
        public string Note { get; set; }
        public DateTime RegistrationDate { get; set; }
    }


}
