using FakeApimGateway;
using HttpSignatures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace FakeApimGatewayTests
{
    public class HttpSignatureTests
    {

        public static class HttpSignatureHelper
        {

            public static string CreateDigest(HttpContent content)
            {
                SHA256 digestSHA256 = SHA256Managed.Create();
                var payloadHash = digestSHA256.ComputeHash(content.ReadAsByteArrayAsync().Result);
                return Convert.ToBase64String(payloadHash);

            }

            internal static AuthenticationHeaderValue CreateAuthHeader(HttpRequestMessage requestMessage, string keyId, string[] signatureHeaders, string secret)
            {
                List<string> headersignature = new List<string>();
                foreach (var header in signatureHeaders)
                {
                    switch (header)
                    {
                        case "(request-target)":
                            headersignature.Add($"(request-target): {requestMessage.Method.ToString().ToLower()} {requestMessage.RequestUri.AbsolutePath.ToLower()}");
                            break;
                        default:
                            if (!requestMessage.Headers.Contains(header)) { throw new ArgumentException("Missing required header"); }
                            headersignature.Add($"{header.ToLower()}: {String.Join(", ", requestMessage.Headers.GetValues(header))}");
                            break;
                    }
                }
                var signatureString = String.Join("\n", headersignature);

                var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signatureString));
                var base64hash = Convert.ToBase64String(hash);

                return new AuthenticationHeaderValue("Signature", $"keyId=\"{keyId}\", algorithm=\"hmac-­sha256\", headers=\"{String.Join(" ",signatureHeaders)}\", signature=\"{base64hash}");

            }
        }

        [Fact]
        public void RequestPolicyWithoutBody()
        {
            // Assert
            var requestMessage = new HttpRequestMessage()
            {
                RequestUri = new Uri("http://example.org/foo?param=value&pet=dog")
            };

            requestMessage.Headers.Date = new DateTime(2014, 01, 05, 21, 31, 40,DateTimeKind.Utc);
            requestMessage.Content = new StringContent("123456789012345678", Encoding.UTF8, "application/json");

            requestMessage.Headers.Add("digest", $"SHA-256={HttpSignatureHelper.CreateDigest(requestMessage.Content)}");
            requestMessage.Headers.Authorization = HttpSignatureHelper.CreateAuthHeader(requestMessage,"foo:bar", new[] { "(request-target)", "date", "digest" }, "abc123");

            var context = new ApimContext(requestMessage: requestMessage);

            // Act
            string policyResult = ValidateSignature(context);

            //Assert
            Assert.Null(policyResult);
        }

        [Fact]
        public void RequestPolicyWithNoSignature()
        {
            // Assert
            var requestMessage = new HttpRequestMessage()
            {
                RequestUri = new Uri("http://example.org/foo?param=value&pet=dog")
            };
            var context = new ApimContext(requestMessage: requestMessage);

            // Act
            string policyResult = ValidateSignature(context);

            //Assert
            Assert.Equal("Missing Authorization header", policyResult);
        }

        [Fact]
        public void RequestPolicyWithDifferentAuthSchema()
        {
            // Assert
            var requestMessage = new HttpRequestMessage()
            {
                RequestUri = new Uri("http://example.org/foo?param=value&pet=dog")
            };
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("foo");

            var context = new ApimContext(requestMessage: requestMessage);

            // Act
            string policyResult = ValidateSignature(context);

            //Assert
            Assert.Equal("Authorization scheme must be 'signature'", policyResult);
        }

        [Fact]
        public void RequestPolicyWithDifferentSignatureSchemeButNoParameters()
        {
            // Assert
            var requestMessage = new HttpRequestMessage()
            {
                RequestUri = new Uri("http://example.org/foo?param=value&pet=dog")
            };
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("signature");

            var context = new ApimContext(requestMessage: requestMessage);

            // Act
            string policyResult = ValidateSignature(context);

            //Assert
            Assert.Equal("Missing signature parameters", policyResult);
        }
        [Fact]
        public void RequestPolicyWithDifferentSignatureSchemeButHeadersParameters()
        {
            // Assert
            var requestMessage = new HttpRequestMessage()
            {
                RequestUri = new Uri("http://example.org/foo?param=value&pet=dog")
            };
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("signature","foo-bar");

            var context = new ApimContext(requestMessage: requestMessage);

            // Act
            string policyResult = ValidateSignature(context);

            //Assert
            Assert.Equal("Missing headers parameter in signature", policyResult);
        }

        [Fact]
        public void RequestPolicyWithDifferentSignatureSchemeWithMissingKeyId()
        {
            // Assert
            var requestMessage = new HttpRequestMessage()
            {
                RequestUri = new Uri("http://example.org/foo?param=value&pet=dog")
            };
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("signature", "headers=\"(request-target)\"");

            var context = new ApimContext(requestMessage: requestMessage);

            // Act
            string policyResult = ValidateSignature(context);

            //Assert
            Assert.Equal("Missing keyId in signature", policyResult);
        }

        [Fact]
        public void RequestPolicyWithDifferentSignatureSchemeWithMissingSignature()
        {
            // Assert
            var requestMessage = new HttpRequestMessage()
            {
                RequestUri = new Uri("http://example.org/foo?param=value&pet=dog")
            };
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("signature", @"headers=""(request-target)"", keyId=""foo:bar""");

            var context = new ApimContext(requestMessage: requestMessage);

            // Act
            string policyResult = ValidateSignature(context);

            //Assert
            Assert.Equal("Missing signature parameter", policyResult);
        }

        [Fact]
        public void RequestPolicyWithMissingSignature()
        {
            // Assert
            var requestMessage = new HttpRequestMessage()
            {
                RequestUri = new Uri("http://example.org/foo?param=value&pet=dog")
            };
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("signature", @"headers=""(request-target)"", keyId=""foo:bar""");

            var context = new ApimContext(requestMessage: requestMessage);

            // Act
            string policyResult = ValidateSignature(context);

            //Assert
            Assert.Equal("Missing signature parameter", policyResult);
        }

        [Fact]
        public void RequestPolicyWithInvalidSignature()
        {
            // Assert
            var requestMessage = new HttpRequestMessage()
            {
                RequestUri = new Uri("http://example.org/foo?param=value&pet=dog")
            };
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("signature", @"headers=""(request-target)"", keyId=""foo:bar"", signature=""blah""");

            var context = new ApimContext(requestMessage: requestMessage);

            // Act
            string policyResult = ValidateSignature(context);

            //Assert
            Assert.Equal("Signature does not match calculated value", policyResult);
        }

        // Returns null string if no error occured
        public string ValidateSignature(ApimContext context)
        {

            string auth = context.Request.Headers.GetValueOrDefault("Authorization", "");
            if (auth == "") { return "Missing Authorization header"; }

            var authvalues = auth.Split(new char[] { ' ' },2).Select(s=>s.Trim()).ToArray();
            string scheme = authvalues[0].ToLower();
            string parameterValues = authvalues.Length == 2 ? authvalues[1] : "";

            if (scheme != "signature") { return "Authorization scheme must be 'signature'"; }

            if (string.IsNullOrEmpty(parameterValues)) { return "Missing signature parameters"; }

            // Get Signature Parameters
            var parms = parameterValues.Split(',').Select(v => {
                var splits = v.Split(new char[] {'='},2 ).Select(s=>s.Trim()).ToArray();  
                var pkey = splits[0];
                var pvalue = splits.Length == 2 ? splits[1] : ""; 
                return new { key = pkey, value = pvalue.Replace("\"", "") };
            }).Where(k => !string.IsNullOrEmpty(k.key))
            .ToDictionary(k => k.key, v => v.value);

            if (!parms.ContainsKey("headers")) { return "Missing headers parameter in signature"; }

            // Get Headers
            var headers = parms["headers"].Split(' ');
            List<string> headersignature = new List<string>();
            foreach (var header in headers)
            {
                switch (header)
                {
                    case "(request-target)":
                        headersignature.Add($"(request-target): {context.Request.Method.ToLower()} {context.Request.Url.Path.ToLower()}");
                        break;
                    default:
                        if (!context.Request.Headers.ContainsKey(header)) { return "Missing header for signature"; }
                        headersignature.Add($"{header.ToLower()}: {String.Join(", ", context.Request.Headers[header])}");
                        break;
                }
            }

            var signatureString = String.Join("\n", headersignature);

            if (!parms.ContainsKey("keyId")) { return "Missing keyId in signature"; }

            string secretKey = "abc123";  // I'm assuming this is going to be obtained from a lookup on keyId

            // Hardcoded to always use HMACSHA256 instead of respecting algorithm parameter
            var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signatureString));
            var base64hash = Convert.ToBase64String(hash);

            if (!parms.ContainsKey("signature")) { return "Missing signature parameter"; }


            if (base64hash != parms["signature"]) { return "Signature does not match calculated value"; }

            // Check digest value
            string digest = context.Request.Headers.GetValueOrDefault("digest", "SHA256=");
            string[] digestValue = digest.Split(new char[] { '=' },2);
            if (string.IsNullOrEmpty(digestValue[1])) { return "missing digest header"; }

            SHA256 digestSHA256 = SHA256Managed.Create();
            var body = context.Request.Body.As<string>(true);
            var bodyHash = digestSHA256.ComputeHash(Encoding.UTF8.GetBytes(body));
            var bodyBase64hash = Convert.ToBase64String(bodyHash);

            if (bodyBase64hash != digestValue[1]) { return "Digest values do not match"; }
            return null;
        }


    }
}
