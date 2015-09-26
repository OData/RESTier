// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.OData.Client;
using Xunit;

namespace Microsoft.Restier.WebApi.Test.Scenario
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
                    new Uri(this.ServiceBaseUri, uriStringAfterServiceRoot),
                    useDefaultCredentials: true,
                    usePostTunneling: false,
                    headers: new Dictionary<string, string>()));
            Assert.Equal(statusCode, requestMessage.GetResponse().StatusCode);
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

        protected void TestPostStatusCodeIs(string uriStringAfterServiceRoot, int statusCode)
        {
            var requestMessage = new HttpWebRequestMessage(
                new DataServiceClientRequestMessageArgs(
                    "POST",
                    new Uri(this.ServiceBaseUri, uriStringAfterServiceRoot),
                    useDefaultCredentials: true,
                    usePostTunneling: false,
                    headers: new Dictionary<string, string>() { { "Content-Length", "0" } }));
            Assert.Equal(statusCode, requestMessage.GetResponse().StatusCode);
        }
        #endregion

        protected void ResetDataSource()
        {
            this.TestClientContext.Execute(new Uri("/ResetDataSource", UriKind.Relative), "POST");
        }
    }
}
