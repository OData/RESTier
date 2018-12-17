// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.Restier.Core.Submit;
using Xunit;

namespace Microsoft.Restier.Core.Tests
{
    public class ConventionBasedMethodNameFactoryTests
    {

        [Fact]
        public void ConventionBasedMethodNameFactory_Insert_PreSubmit()
        {
            var item = new DataModificationItem("TestItems", typeof(string), typeof(string), RestierEntitySetOperations.Insert, null, null, null);
            var name = ConventionBasedMethodNameFactory.GetEntitySetMethodName(item, RestierPipelineStates.PreSubmit);
            Assert.Equal("OnInsertingString", name);
        }

        [Fact]
        public void ConventionBasedMethodNameFactory_Insert_PostSubmit()
        {
            var item = new DataModificationItem("TestItems", typeof(string), typeof(string), RestierEntitySetOperations.Insert, null, null, null);
            var name = ConventionBasedMethodNameFactory.GetEntitySetMethodName(item, RestierPipelineStates.PostSubmit);
            Assert.Equal("OnInsertedString", name);
        }

        [Fact]
        public void ConventionBasedMethodNameFactory_Update_PreSubmit()
        {
            var item = new DataModificationItem("TestItems", typeof(string), typeof(string), RestierEntitySetOperations.Update, null, null, null);
            var name = ConventionBasedMethodNameFactory.GetEntitySetMethodName(item, RestierPipelineStates.PreSubmit);
            Assert.Equal("OnUpdatingString", name);
        }

        [Fact]
        public void ConventionBasedMethodNameFactory_Update_PostSubmit()
        {
            var item = new DataModificationItem("TestItems", typeof(string), typeof(string), RestierEntitySetOperations.Update, null, null, null);
            var name = ConventionBasedMethodNameFactory.GetEntitySetMethodName(item, RestierPipelineStates.PostSubmit);
            Assert.Equal("OnUpdatedString", name);
        }

        [Fact]
        public void ConventionBasedMethodNameFactory_Delete_PreSubmit()
        {
            var item = new DataModificationItem("TestItems", typeof(string), typeof(string), RestierEntitySetOperations.Delete, null, null, null);
            var name = ConventionBasedMethodNameFactory.GetEntitySetMethodName(item, RestierPipelineStates.PreSubmit);
            Assert.Equal("OnDeletingString", name);
        }

        [Fact]
        public void ConventionBasedMethodNameFactory_Delete_PostSubmit()
        {
            var item = new DataModificationItem("TestItems", typeof(string), typeof(string), RestierEntitySetOperations.Delete, null, null, null);
            var name = ConventionBasedMethodNameFactory.GetEntitySetMethodName(item, RestierPipelineStates.PostSubmit);
            Assert.Equal("OnDeletedString", name);
        }

        [Fact]
        public void ConventionBasedMethodNameFactory_Filter_PreSubmit()
        {
            var item = new DataModificationItem("TestItems", typeof(string), typeof(string), RestierEntitySetOperations.Filter, null, null, null);
            var name = ConventionBasedMethodNameFactory.GetEntitySetMethodName(item, RestierPipelineStates.PreSubmit);
            Assert.Equal("", name);
        }

        [Fact]
        public void ConventionBasedMethodNameFactory_Filter_Submit()
        {
            var item = new DataModificationItem("TestItems", typeof(string), typeof(string), RestierEntitySetOperations.Filter, null, null, null);
            var name = ConventionBasedMethodNameFactory.GetEntitySetMethodName(item, RestierPipelineStates.Submit);
            Assert.Equal("OnFilterTestItems", name);
        }

        [Fact]
        public void ConventionBasedMethodNameFactory_Filter_PostSubmit()
        {
            var item = new DataModificationItem("TestItems", typeof(string), typeof(string), RestierEntitySetOperations.Filter, null, null, null);
            var name = ConventionBasedMethodNameFactory.GetEntitySetMethodName(item, RestierPipelineStates.PostSubmit);
            Assert.Equal("", name);
        }

    }

}
