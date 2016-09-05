// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.OData.Client;
using Microsoft.OData.Edm.Library;
using Microsoft.OData.Service.Sample.Trippin.Models;
using Xunit;

namespace Microsoft.OData.Service.Sample.Tests
{
    public class TemporalTests : TrippinE2ETestBase
    {
        #region Date

        [Fact]
        public void CURDEntityWithDateProperty()
        {
            this.TestClientContext.MergeOption = MergeOption.OverwriteChanges;

            // Post an entity
            Person person = new Person
            {
                FirstName = "Sheldon",
                LastName = "Cooper",
                UserName = "SheldonCooper",
                BirthDate = new Date(2000, 1, 1),
                BirthDate2 = new Date(2000, 1, 1)
            };

            this.TestClientContext.AddToPeople(person);
            this.TestClientContext.SaveChanges();
            long? personId = person.PersonId;

            // Count this entity
            var count = this.TestClientContext.People.Count();
            Assert.Equal(personId, count);

            // Update an entity
            person.BirthDate = new Date(2012, 12, 20);
            person.BirthDate2 = person.BirthDate;
            this.TestClientContext.UpdateObject(person);
            this.TestClientContext.SaveChanges();

            // Query an entity
            this.TestClientContext.Detach(person);
            person = this.TestClientContext.People
                .ByKey(new Dictionary<string, object> { { "PersonId", personId } }).GetValue();
            Assert.Equal("Cooper", person.LastName);

            // Delete an entity
            this.TestClientContext.DeleteObject(person);
            this.TestClientContext.SaveChanges();

            // Filter this entity
            var persons = this.TestClientContext.People
                .AddQueryOption("$filter", "BirthDate eq 1986-02-09").ToList();
            Assert.Equal(1, persons.Count);
            persons = this.TestClientContext.People
                .AddQueryOption("$filter", "year(BirthDate) eq 1986").ToList();
            Assert.Equal(1, persons.Count);
            persons = this.TestClientContext.People
                .AddQueryOption("$filter", "BirthDate2 eq 1985-01-10").ToList();
            Assert.Equal(1, persons.Count);
            persons = this.TestClientContext.People
                .AddQueryOption("$filter", "year(BirthDate2) eq 1985").ToList();
            Assert.Equal(1, persons.Count);
        }
        
        [Fact]
        public void UQDateProperty()
        {
            this.TestClientContext.MergeOption = MergeOption.OverwriteChanges;

            // Post an entity
            Person person = new Person
            {
                FirstName = "Sheldon",
                LastName = "Cooper",
                UserName = "SheldonCooper",
                BirthDate = new Date(2000, 1, 1),
                BirthDate2 = new Date(2000, 1, 1)
            };

            this.TestClientContext.AddToPeople(person);
            this.TestClientContext.SaveChanges();
            long personId = person.PersonId;

            // Query a property
            var birthDate = this.TestClientContext.People
                .Where(p => p.PersonId == personId).Select(p => p.BirthDate)
                .SingleOrDefault();
            var birthDate2 = this.TestClientContext.People
                .Where(p => p.PersonId == personId).Select(p => p.BirthDate2)
                .SingleOrDefault();
            Assert.Equal(new Date(2000, 1, 1), birthDate);
            Assert.Equal(birthDate, birthDate2);

            // Update a property
            this.TestPut(string.Format("People({0})/BirthDate", personId), @"{""value"":""2012-12-20""}");
            this.TestPut(string.Format("People({0})/BirthDate2", personId), @"{""value"":""2012-12-20""}");

            // Query a property's value : ~/$value
            this.TestGet(string.Format("People({0})/BirthDate/$value", personId), "2012-12-20");
            this.TestGet(string.Format("People({0})/BirthDate2/$value", personId), "2012-12-20");
        }

        [Fact]
        public void QueryOptionsOnDateProperty()
        {
            this.TestClientContext.MergeOption = MergeOption.OverwriteChanges;
            var birthDate = new Date(2012, 12, 20);

            // Post an entity
            Person person = new Person
            {
                FirstName = "Sheldon",
                LastName = "Cooper",
                UserName = "SheldonCooper",
                BirthDate = birthDate,
                BirthDate2 = birthDate
            };

            this.TestClientContext.AddToPeople(person);
            this.TestClientContext.SaveChanges();
            long? personId = person.PersonId;

            // Filter this entity
            var persons = this.TestClientContext.People
                .AddQueryOption("$filter", "BirthDate eq 2012-12-20")
                .ToList();
            Assert.Equal(1, persons.Count);
            persons = this.TestClientContext.People
                .AddQueryOption("$filter", "BirthDate2 eq 2012-12-20")
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
                        BirthDate = p.BirthDate,
                        BirthDate2 = p.BirthDate2
                    }).FirstOrDefault();
            Assert.Equal(personId, person.PersonId);
            Assert.Equal(birthDate, person.BirthDate);
            Assert.Equal(birthDate, person.BirthDate2);
            Assert.NotNull(person.UserName);
            Assert.Null(person.FirstName);
            Assert.Null(person.LastName);

            // Order by BirthDate desc
            var people2 = this.TestClientContext.People.OrderByDescending(p => p.BirthDate).ToList();
            Assert.Equal(birthDate, people2.First().BirthDate);
            people2 = this.TestClientContext.People.OrderByDescending(p => p.BirthDate2).ToList();
            Assert.Equal(birthDate, people2.First().BirthDate2);

            // Order by BirthDate
            people2 = this.TestClientContext.People.OrderBy(p => p.BirthDate).ToList();
            Assert.Equal(birthDate, people2.Last().BirthDate);
            people2 = this.TestClientContext.People.OrderBy(p => p.BirthDate2).ToList();
            Assert.Equal(birthDate, people2.Last().BirthDate2);

            // top
            people2 = this.TestClientContext.People.OrderBy(p => p.BirthDate).Take(3).ToList();
            Assert.Equal(3, people2.Count);
            people2 = this.TestClientContext.People.OrderBy(p => p.BirthDate2).Take(3).ToList();
            Assert.Equal(3, people2.Count);

            // skip
            people2 = this.TestClientContext.People.Skip((int)(personId - 1)).ToList();
            Assert.Equal(birthDate, people2.First().BirthDate);
            Assert.Equal(birthDate, people2.First().BirthDate2);

            // TODO GitHubIssue#46 : case for $count=true
        }

        #endregion

        #region TimeOfDay

        [Fact]
        public void CURDEntityWithTimeOfDayProperty()
        {
            this.TestClientContext.MergeOption = MergeOption.OverwriteChanges;

            // Post an entity
            Person person = new Person
            {
                FirstName = "Sheldon",
                LastName = "Cooper",
                UserName = "SheldonCooper",
                BirthTime = new TimeOfDay(12, 12, 12, 340),
                BirthTime2 = new TimeOfDay(12, 12, 12, 340)
            };

            this.TestClientContext.AddToPeople(person);
            this.TestClientContext.SaveChanges();
            long personId = person.PersonId;

            // Count this entity
            var count = this.TestClientContext.People.Count();
            Assert.Equal(personId, count);

            // Update an entity
            person.BirthTime = new TimeOfDay(12, 12, 12, 340);
            person.BirthTime2 = person.BirthTime;
            this.TestClientContext.UpdateObject(person);
            this.TestClientContext.SaveChanges();

            // Query an entity
            this.TestClientContext.Detach(person);
            person = this.TestClientContext.People
                .ByKey(new Dictionary<string, object> { { "PersonId", personId } }).GetValue();
            Assert.Equal("Cooper", person.LastName);

            // Delete an entity
            this.TestClientContext.DeleteObject(person);
            this.TestClientContext.SaveChanges();

            // Filter this entity
            var persons = this.TestClientContext.People
                .AddQueryOption("$filter", "BirthTime eq 22:58:02.0000000").ToList();
            Assert.Equal(1, persons.Count);
            persons = this.TestClientContext.People
                .AddQueryOption("$filter", "hour(BirthTime) eq 22").ToList();
            Assert.Equal(1, persons.Count);
            persons = this.TestClientContext.People
                .AddQueryOption("$filter", "BirthTime2 eq 23:59:01.0000000").ToList();
            Assert.Equal(1, persons.Count);
            persons = this.TestClientContext.People
                .AddQueryOption("$filter", "hour(BirthTime2) eq 23").ToList();
            Assert.Equal(1, persons.Count);
        }

        [Fact]
        public void UQTimeOfDayProperty()
        {
            this.TestClientContext.MergeOption = MergeOption.OverwriteChanges;

            // Post an entity
            Person person = new Person
            {
                FirstName = "Sheldon",
                LastName = "Cooper",
                UserName = "SheldonCooper",
                BirthTime = new TimeOfDay(12, 12, 12, 340),
                BirthTime2 = new TimeOfDay(12, 12, 12, 340)
            };

            this.TestClientContext.AddToPeople(person);
            this.TestClientContext.SaveChanges();
            long personId = person.PersonId;

            // Query a property
            var birthTime = this.TestClientContext.People
                .Where(p => p.PersonId == personId).Select(p => p.BirthTime)
                .SingleOrDefault();
            var birthTime2 = this.TestClientContext.People
                .Where(p => p.PersonId == personId).Select(p => p.BirthTime2)
                .SingleOrDefault();
            Assert.Equal(new TimeOfDay(12, 12, 12, 340), birthTime);
            Assert.Equal(birthTime, birthTime2);

            // Update a property
            this.TestPut(string.Format("People({0})/BirthTime", personId), @"{""value"":""12:12:12.34""}");
            this.TestPut(string.Format("People({0})/BirthTime2", personId), @"{""value"":""12:12:12.34""}");

            // Query a property's value : ~/$value
            this.TestGet(string.Format("People({0})/BirthTime/$value", personId), "12:12:12.3400000");
            this.TestGet(string.Format("People({0})/BirthTime2/$value", personId), "12:12:12.3400000");
        }

        [Fact]
        public void QueryOptionsOnTimeOfDayProperty()
        {
            this.TestClientContext.MergeOption = MergeOption.OverwriteChanges;
            var birthTime = new TimeOfDay(23, 59, 50, 340);

            // Post an entity
            Person person = new Person
            {
                FirstName = "Sheldon",
                LastName = "Cooper",
                UserName = "SheldonCooper",
                BirthTime = birthTime,
                BirthTime2 = birthTime,
            };

            this.TestClientContext.AddToPeople(person);
            this.TestClientContext.SaveChanges();
            long? personId = person.PersonId;

            // Filter this entity
            var persons = this.TestClientContext.People
                .AddQueryOption("$filter", "BirthTime eq 23:59:50.34")
                .ToList();
            Assert.Equal(1, persons.Count);
            persons = this.TestClientContext.People
                .AddQueryOption("$filter", "BirthTime2 eq 23:59:01.00")
                .ToList();
            Assert.Equal(1, persons.Count);

            // Filter with Parameter alias
            // ODL Bug: https://github.com/OData/odata.net/issues/316
            //this.TestClientContext.People
            //    .AddQueryOption("$filter", string.Format("BirthTime eq @p1 and PersonId gt {0}&@p1=23:59:50.34", personId - 1))
            //    .ToList();
            //Assert.Equal(1, persons.Count);

            // Projection
            this.TestClientContext.Detach(person);
            person = this.TestClientContext.People.Where(p => p.PersonId == personId)
                .Select(p =>
                    new Person
                    {
                        PersonId = p.PersonId,
                        UserName = p.UserName,
                        BirthTime = p.BirthTime,
                        BirthTime2 = p.BirthTime2
                    }).FirstOrDefault();
            Assert.Equal(personId, person.PersonId);
            Assert.Equal(birthTime, person.BirthTime);
            Assert.NotNull(person.UserName);
            Assert.Null(person.FirstName);
            Assert.Null(person.LastName);

            // Order by BirthTime desc
            var people2 = this.TestClientContext.People.OrderByDescending(p => p.BirthTime).ToList();
            Assert.Equal(birthTime, people2.First().BirthTime);
            people2 = this.TestClientContext.People.OrderByDescending(p => p.BirthTime2).ToList();
            Assert.Equal(birthTime, people2.First().BirthTime2);

            // Order by BirthTime
            people2 = this.TestClientContext.People.OrderBy(p => p.BirthTime).ToList();
            Assert.Equal(birthTime, people2.Last().BirthTime);
            people2 = this.TestClientContext.People.OrderBy(p => p.BirthTime2).ToList();
            Assert.Equal(birthTime, people2.Last().BirthTime2);

            // top
            people2 = this.TestClientContext.People.OrderBy(p => p.BirthTime).Take(3).ToList();
            Assert.Equal(3, people2.Count);
            people2 = this.TestClientContext.People.OrderBy(p => p.BirthTime2).Take(3).ToList();
            Assert.Equal(3, people2.Count);

            // skip
            people2 = this.TestClientContext.People.Skip((int)(personId - 1)).ToList();
            Assert.Equal(birthTime, people2.First().BirthTime);
            Assert.Equal(birthTime, people2.First().BirthTime2);

            // TODO GitHubIssue#46 : case for $count=true
        }

        #endregion

        #region DateTimeOffset

        [Fact]
        public void CURDEntityWithDateTimeOffsetProperty()
        {
            this.TestClientContext.MergeOption = MergeOption.OverwriteChanges;

            // Post an entity
            Person person = new Person
            {
                FirstName = "Sheldon",
                LastName = "Cooper",
                UserName = "SheldonCooper",
                BirthDateTime = new DateTimeOffset(2000, 1, 1, 12, 34, 56, TimeSpan.Zero),
                BirthDateTime2 = new DateTimeOffset(2000, 1, 1, 12, 34, 56, TimeSpan.Zero)
            };

            this.TestClientContext.AddToPeople(person);
            this.TestClientContext.SaveChanges();
            long? personId = person.PersonId;

            // Count this entity
            var count = this.TestClientContext.People.Count();
            Assert.Equal(personId, count);

            // Update an entity
            person.BirthDateTime = new DateTimeOffset(2012, 12, 20, 13, 24, 35, TimeSpan.Zero);
            person.BirthDateTime2 = person.BirthDateTime;
            this.TestClientContext.UpdateObject(person);
            this.TestClientContext.SaveChanges();

            // Query an entity
            this.TestClientContext.Detach(person);
            person = this.TestClientContext.People
                .ByKey(new Dictionary<string, object> { { "PersonId", personId } }).GetValue();
            Assert.Equal("Cooper", person.LastName);

            // Delete an entity
            this.TestClientContext.DeleteObject(person);
            this.TestClientContext.SaveChanges();

            // Filter this entity
            var persons = this.TestClientContext.People
                .AddQueryOption("$filter", "BirthDateTime eq 1986-02-09T22:58:02Z").ToList();
            Assert.Equal(1, persons.Count);
            persons = this.TestClientContext.People
                .AddQueryOption("$filter", "year(BirthDateTime) eq 1986").ToList();
            Assert.Equal(1, persons.Count);
            persons = this.TestClientContext.People
                .AddQueryOption("$filter", "BirthDateTime2 eq 1985-01-10T23:59:01Z").ToList();
            Assert.Equal(1, persons.Count);
            persons = this.TestClientContext.People
                .AddQueryOption("$filter", "year(BirthDateTime2) eq 1985").ToList();
            Assert.Equal(1, persons.Count);
        }

        [Fact]
        public void UQDateTimeOffsetProperty()
        {
            this.TestClientContext.MergeOption = MergeOption.OverwriteChanges;

            // Post an entity
            Person person = new Person
            {
                FirstName = "Sheldon",
                LastName = "Cooper",
                UserName = "SheldonCooper",
                BirthDateTime = new DateTimeOffset(2000, 1, 1, 12, 34, 56, TimeSpan.Zero),
                BirthDateTime2 = new DateTimeOffset(2000, 1, 1, 12, 34, 56, TimeSpan.Zero)
            };

            this.TestClientContext.AddToPeople(person);
            this.TestClientContext.SaveChanges();
            long personId = person.PersonId;

            // Query a property
            var birthDateTime = this.TestClientContext.People
                .Where(p => p.PersonId == personId).Select(p => p.BirthDateTime)
                .SingleOrDefault();
            var birthDateTime2 = this.TestClientContext.People
                .Where(p => p.PersonId == personId).Select(p => p.BirthDateTime2)
                .SingleOrDefault();
            Assert.Equal(new DateTimeOffset(2000, 1, 1, 12, 34, 56, TimeSpan.Zero), birthDateTime);
            Assert.Equal(birthDateTime, birthDateTime2);

            // Update a property
            this.TestPut(string.Format("People({0})/BirthDateTime", personId), @"{""value"":""2012-12-20T13:24:35Z""}");
            this.TestPut(string.Format("People({0})/BirthDateTime2", personId), @"{""value"":""2012-12-20T13:24:35Z""}");

            // Query a property's value : ~/$value
            this.TestGet(string.Format("People({0})/BirthDateTime/$value", personId), "2012-12-20T13:24:35Z");
            this.TestGet(string.Format("People({0})/BirthDateTime2/$value", personId), "2012-12-20T13:24:35Z");
        }

        [Fact]
        public void QueryOptionsOnDateTimeOffsetProperty()
        {
            this.TestClientContext.MergeOption = MergeOption.OverwriteChanges;
            var birthDateTime = new DateTimeOffset(2000, 1, 1, 15, 34, 56, TimeSpan.Zero);

            // Post an entity
            Person person = new Person
            {
                FirstName = "Sheldon",
                LastName = "Cooper",
                UserName = "SheldonCooper",
                BirthDateTime = birthDateTime,
                BirthDateTime2 = birthDateTime,
            };

            this.TestClientContext.AddToPeople(person);
            this.TestClientContext.SaveChanges();
            long? personId = person.PersonId;

            // Filter this entity
            var persons = this.TestClientContext.People
                .AddQueryOption("$filter", "BirthDateTime eq 2000-01-01T15:34:56Z")
                .ToList();
            Assert.Equal(1, persons.Count);
            persons = this.TestClientContext.People
                .AddQueryOption("$filter", "BirthDateTime2 eq 2000-01-01T15:34:56Z")
                .ToList();
            Assert.Equal(1, persons.Count);

            // Filter with Parameter alias
            // ODL Bug: https://github.com/OData/odata.net/issues/316
            //this.TestClientContext.People
            //    .AddQueryOption("$filter", string.Format("BirthDateTime eq @p1 and PersonId gt {0}&@p1=2000-01-01T12:34:56Z", personId - 1))
            //    .ToList();
            //Assert.Equal(1, persons.Count);

            // Projection
            this.TestClientContext.Detach(person);
            person = this.TestClientContext.People.Where(p => p.PersonId == personId)
                .Select(p =>
                    new Person
                    {
                        PersonId = p.PersonId,
                        UserName = p.UserName,
                        BirthDateTime = p.BirthDateTime,
                        BirthDateTime2 = p.BirthDateTime2
                    }).FirstOrDefault();
            Assert.Equal(personId, person.PersonId);
            Assert.Equal(birthDateTime, person.BirthDateTime);
            Assert.Equal(birthDateTime, person.BirthDateTime2);
            Assert.NotNull(person.UserName);
            Assert.Null(person.FirstName);
            Assert.Null(person.LastName);

            // Order by BirthDateTime desc
            var people2 = this.TestClientContext.People.OrderByDescending(p => p.BirthDateTime).ToList();
            Assert.Equal(birthDateTime, people2.First().BirthDateTime);
            people2 = this.TestClientContext.People.OrderByDescending(p => p.BirthDateTime2).ToList();
            Assert.Equal(birthDateTime, people2.First().BirthDateTime2);

            // Order by BirthDateTime
            people2 = this.TestClientContext.People.OrderBy(p => p.BirthDateTime).ToList();
            Assert.Equal(birthDateTime, people2.Last().BirthDateTime);
            people2 = this.TestClientContext.People.OrderBy(p => p.BirthDateTime2).ToList();
            Assert.Equal(birthDateTime, people2.Last().BirthDateTime2);

            // top
            people2 = this.TestClientContext.People.OrderBy(p => p.BirthDateTime).Take(3).ToList();
            Assert.Equal(3, people2.Count);
            people2 = this.TestClientContext.People.OrderBy(p => p.BirthDateTime2).Take(3).ToList();
            Assert.Equal(3, people2.Count);

            // skip
            people2 = this.TestClientContext.People.Skip((int)(personId - 1)).ToList();
            Assert.Equal(birthDateTime, people2.First().BirthDateTime);
            Assert.Equal(birthDateTime, people2.First().BirthDateTime2);

            // TODO GitHubIssue#46 : case for $count=true
        }

        #endregion

        #region Test Helper Methods

        private void TestPut(string relativeUri, string payload)
        {
            var headers = new Dictionary<string, string>
            {
                { "Content-Type", "application/json" }
            };

            var request = new HttpWebRequestMessage(
                new DataServiceClientRequestMessageArgs(
                    "Put",
                    new Uri(this.TestClientContext.BaseUri, relativeUri),
                    false,
                    false,
                    headers));
            using (var stream = request.GetStream())
            using (var writer = new StreamWriter(stream))
            {
                writer.Write(payload);
            }

            using (var response = request.GetResponse() as HttpWebResponseMessage)
            {
                Assert.Equal(200, response.StatusCode);
            }
        }

        private void TestGet(string relativeUri, string expectedPayload)
        {
            var headers = new Dictionary<string, string>
            {
                { "Content-Type", "application/json" }
            };

            var request = new HttpWebRequestMessage(
                new DataServiceClientRequestMessageArgs(
                    "Get",
                    new Uri(this.TestClientContext.BaseUri, relativeUri),
                    false,
                    false,
                    headers));
            using (var response = request.GetResponse() as HttpWebResponseMessage)
            {
                Assert.Equal(200, response.StatusCode);
                using (var stream = response.GetStream())
                {
                    var reader = new StreamReader(stream);
                    var content = reader.ReadToEnd();
                    Assert.Equal(expectedPayload, content);
                }
            }
        }

        #endregion
    }
}
