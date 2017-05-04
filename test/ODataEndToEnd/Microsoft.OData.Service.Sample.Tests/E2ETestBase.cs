// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.OData.Client;
using Xunit;

namespace Microsoft.OData.Service.Sample.Tests
{
    /// <summary>
    /// Summary description for E2ETestBase
    /// </summary>
    public class E2ETestBase<TDSC> where TDSC : DataServiceContext
    {
        protected Uri ServiceBaseUri { get; set; }

        protected TDSC TestClientContext { get; private set; }

        public E2ETestBase(Uri serviceBaseUri)
        {
            this.ServiceBaseUri = serviceBaseUri;
            TestClientContext = CreateClientContext();
        }

        protected TDSC CreateClientContext()
        {
            return (TDSC)Activator.CreateInstance(typeof(TDSC), this.ServiceBaseUri);
        }

        #region Helper Methods
        protected void TestGetPayloadContains(string uriStringAfterServiceRoot, string expectedSubString)
        {
            this.TestGetPayload(uriStringAfterServiceRoot,
                payloadString => Assert.Contains(expectedSubString, payloadString));
        }

        protected void TestGetPayloadDoesNotContain(string uriStringAfterServiceRoot, string expectedSubString)
        {
            this.TestGetPayload(uriStringAfterServiceRoot,
                payloadString => Assert.DoesNotContain(expectedSubString, payloadString));
        }

        protected void TestGetPayloadIs(string uriStringAfterServiceRoot, string expectedString)
        {
            this.TestGetPayload(uriStringAfterServiceRoot,
                payloadString => Assert.Equal(expectedString, payloadString));
        }

        protected void TestGetPayload(string uriStringAfterServiceRoot, Action<string> testMethod)
        {
            var requestMessage = new HttpWebRequestMessage(
                new DataServiceClientRequestMessageArgs(
                    "GET",
                    new Uri(this.ServiceBaseUri, uriStringAfterServiceRoot),
                    useDefaultCredentials: true,
                    usePostTunneling: false,
                    headers: new Dictionary<string, string>()));
            using (var r = new StreamReader(requestMessage.GetResponse().GetStream()))
            {
                var payloadString = r.ReadToEnd();
                testMethod(payloadString);
            }
        }

        protected void TestGetStatusCodeIs(string uriStringAfterServiceRoot, int statusCode)
        {
            var requestMessage = new HttpWebRequestMessage(
                new DataServiceClientRequestMessageArgs(
                    "GET",
                    new Uri(this.ServiceBaseUri + uriStringAfterServiceRoot, UriKind.Absolute),
                    useDefaultCredentials: true,
                    usePostTunneling: false,
                    headers: new Dictionary<string, string>()));

            try
            {
                Assert.Equal(statusCode, requestMessage.GetResponse().StatusCode);
            }
            catch (DataServiceTransportException e)
            {
                // In case of 404 or 500, it will be handled here
                var response = e.Response;
                Assert.Equal(statusCode, response.StatusCode);
            }
        }

        protected async void TestPostPayloadContains(string uriStringAfterServiceRoot, string postContent, string expectedSubString)
        {
            var requestUri = string.Format("{0}/{1}", this.ServiceBaseUri, uriStringAfterServiceRoot);
            var request = new HttpRequestMessage(HttpMethod.Post, requestUri);

            request.Content = new StringContent(postContent);
            request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

            HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.SendAsync(request);
            string responseString = response.Content.ReadAsStringAsync().Result;

            response.EnsureSuccessStatusCode();
            Assert.Contains(expectedSubString, responseString);
        }

        protected void TestPostPayloadContains(string uriStringAfterServiceRoot, string expectedSubString)
        {
            var requestMessage = new HttpWebRequestMessage(
                new DataServiceClientRequestMessageArgs(
                    "POST",
                    new Uri(this.ServiceBaseUri, uriStringAfterServiceRoot),
                    useDefaultCredentials: true,
                    usePostTunneling: false,
                    headers: new Dictionary<string, string>() { { "Content-Length", "0" } }));
            using (var r = new StreamReader(requestMessage.GetResponse().GetStream()))
            {
                var payloadString = r.ReadToEnd();
                Assert.Contains(expectedSubString, payloadString);
            }
        }

        protected async void TestPostStatusCodeIs(string uriStringAfterServiceRoot, string postContent, HttpStatusCode statusCode)
        {
            var requestUri = string.Format("{0}/{1}", this.ServiceBaseUri, uriStringAfterServiceRoot);
            var request = new HttpRequestMessage(HttpMethod.Post, requestUri);

            request.Content = new StringContent(postContent);
            request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

            HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.SendAsync(request);

            Assert.Equal(statusCode, response.StatusCode);
        }

        protected void TestPostStatusCodeIs(string uriStringAfterServiceRoot, int statusCode)
        {
            var requestMessage = new HttpWebRequestMessage(
                new DataServiceClientRequestMessageArgs(
                    "POST",
                    new Uri(this.ServiceBaseUri, uriStringAfterServiceRoot),
                    useDefaultCredentials: true,
                    usePostTunneling: false,
                    headers: new Dictionary<string, string>() { { "Content-Length", "0" } }));
            try
            {
                Assert.Equal(statusCode, requestMessage.GetResponse().StatusCode);
            }
            catch (DataServiceTransportException e)
            {
                // In case of 404 or 500, it will be handled here
                var response = e.Response;
                Assert.Equal(statusCode, response.StatusCode);
            }
        }

        protected async Task TestPatchStatusCodeIs(string uriStringAfterServiceRoot, string patchContent, HttpStatusCode statusCode)
        {
            var requestUri = string.Format("{0}/{1}", this.ServiceBaseUri, uriStringAfterServiceRoot);
            var request = new HttpRequestMessage(new HttpMethod("PATCH"), requestUri);

            request.Content = new StringContent(patchContent);
            request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

            HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.SendAsync(request);

            Assert.Equal(statusCode, response.StatusCode);
        }

        #endregion

        protected void ResetDataSource()
        {
            this.TestClientContext.Execute(new Uri("/ResetDataSource", UriKind.Relative), "POST");
        }
    }
}
