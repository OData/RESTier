// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.OData.Client;
using Microsoft.OData.Core;
using Microsoft.OData.Edm.Library;
using Microsoft.Restier.WebApi.Test.Services.Trippin.Models;
using Xunit;

namespace Microsoft.Restier.WebApi.Test.Scenario
{
    public class DateTests : TrippinE2ETestBase
    {
        [Fact]
        public void CURDEntityWithDateProperty()
        {
            this.TestClientContext.MergeOption = Microsoft.OData.Client.MergeOption.OverwriteChanges;
            // Post an entity
            Person person = new Person()
            {
                FirstName = "Sheldon",
                LastName = "Cooper",
                UserName = "SheldonCooper",
                BirthDate = new Date(2000, 1, 1)
            };

            this.TestClientContext.AddToPeople(person);
            this.TestClientContext.SaveChanges();
            int personId = person.PersonId;

            // Count this entity
            var count = this.TestClientContext.People.Count();
            Assert.Equal(personId, count);

            // Update an entity
            person.BirthDate = new Date(2012, 12, 20);
            this.TestClientContext.UpdateObject(person);
            this.TestClientContext.SaveChanges();

            // Query an entity
            this.TestClientContext.Detach(person);
            person = this.TestClientContext.People
                .ByKey(new Dictionary<string, object>() { { "PersonId", personId } }).GetValue();
            Assert.Equal("Cooper", person.LastName);

            // Delete an entity
            this.TestClientContext.DeleteObject(person);
            this.TestClientContext.SaveChanges();

            // Filter this entity
            var persons = this.TestClientContext.People
                .AddQueryOption("$filter", string.Format("BirthDate eq 1986-02-09")).ToList();
            Assert.Equal(1, persons.Count);
            persons = this.TestClientContext.People
                .AddQueryOption("$filter", string.Format("year(BirthDate) eq 1986")).ToList();
            Assert.Equal(1, persons.Count);
        }
        
        [Fact]
        public void UQDateProperty()
        {
            this.TestClientContext.MergeOption = MergeOption.OverwriteChanges;
            // Post an entity
            Person person = new Person()
            {
                FirstName = "Sheldon",
                LastName = "Cooper",
                UserName = "SheldonCooper",
                BirthDate = new Date(2000, 1, 1)
            };

            this.TestClientContext.AddToPeople(person);
            this.TestClientContext.SaveChanges();
            int personId = person.PersonId;

            // Query a property
            var birthDate = this.TestClientContext.People
                .Where(p => p.PersonId == personId).Select(p => p.BirthDate)
                .SingleOrDefault();
            Assert.Equal(new Date(2000, 1, 1), birthDate);

            // Update a property
            Dictionary<string, string> headers = new Dictionary<string, string>() 
            {
                { "Content-Type", "application/json" } 
            };
            
            HttpWebRequestMessage request = new HttpWebRequestMessage(
                new DataServiceClientRequestMessageArgs(
                    "Put",
                    new Uri(string.Format(this.TestClientContext.BaseUri + "People({0})/BirthDate", personId),
                        UriKind.Absolute),
                    false,
                    false,
                    headers));
            using (var stream = request.GetStream())
            using (StreamWriter writer = new StreamWriter(stream))
            {
                var Payload = @"{""value"":""2012-12-20""}";
                writer.Write(Payload);
            }

            using (var response = request.GetResponse() as HttpWebResponseMessage)
            {
                Assert.Equal(200, response.StatusCode);
            }

            // Query a property's value : ~/$value
            headers.Clear();
            ODataMessageReaderSettings readerSettings = new ODataMessageReaderSettings { BaseUri = this.TestClientContext.BaseUri };

            request = new HttpWebRequestMessage(
                new DataServiceClientRequestMessageArgs(
                    "Get",
                    new Uri(string.Format(this.TestClientContext.BaseUri + "/People({0})/BirthDate/$value", personId),
                        UriKind.Absolute),
                    false,
                    false,
                    headers));
            using (var response = request.GetResponse() as HttpWebResponseMessage)
            {
                Assert.Equal(200, response.StatusCode);
                using (var stream = response.GetStream())
                {
                    StreamReader reader = new StreamReader(stream);
                    var expectedPayload = "2012-12-20";
                    var content = reader.ReadToEnd();
                    Assert.Equal(expectedPayload, content);
                }
            }
        }

        [Fact]
        public void QueryOptionsOnDateProperty()
        {
            this.TestClientContext.MergeOption = Microsoft.OData.Client.MergeOption.OverwriteChanges;
            var birthDate = new Date(2012, 12, 20);

            // Post an entity
            Person person = new Person()
            {
                FirstName = "Sheldon",
                LastName = "Cooper",
                UserName = "SheldonCooper",
                BirthDate = birthDate
            };

            this.TestClientContext.AddToPeople(person);
            this.TestClientContext.SaveChanges();
            int personId = person.PersonId;

            // Filter this entity
            var persons = this.TestClientContext.People
                .AddQueryOption("$filter", string.Format("BirthDate eq 2012-12-20"))
                .ToList();
            Assert.Equal(1, persons.Count);

            // Filter with Parameter alias
            // ODL Bug: https://github.com/OData/odata.net/issues/316
            //this.TestClientContext.People
            //    .AddQueryOption("$filter", string.Format("BirthDate eq @p1 and PersonId gt {0}&@p1=2012-12-20", personId - 1))
            //    .ToList();
            //Assert.Equal(1, persons.Count);

            // Projection
            this.TestClientContext.Detach(person);
            person = this.TestClientContext.People.Where(p => p.PersonId == personId)
                .Select(p =>
                    new Person()
                    {
                        PersonId = p.PersonId,
                        UserName = p.UserName,
                        BirthDate = p.BirthDate
                    }).FirstOrDefault();
            Assert.Equal(personId, person.PersonId);
            Assert.Equal(birthDate, person.BirthDate);
            Assert.NotNull(person.UserName);
            Assert.Null(person.FirstName);
            Assert.Null(person.LastName);

            // Order by BirthDate desc
            var people2 = this.TestClientContext.People.OrderByDescending(p => p.BirthDate).ToList();
            Assert.Equal(birthDate, people2.First().BirthDate);

            // Order by BirthDate
            people2 = this.TestClientContext.People.OrderBy(p => p.BirthDate).ToList();
            Assert.Equal(birthDate, people2.Last().BirthDate);

            // top
            people2 = this.TestClientContext.People.OrderBy(p => p.BirthDate).Take(3).ToList();
            Assert.Equal(3, people2.Count);

            // skip
            people2 = this.TestClientContext.People.Skip(personId - 1).ToList();
            Assert.Equal(birthDate, people2.First().BirthDate);

            // TODO GitHubIssue#46 : case for $count=true
        }
    }
}
