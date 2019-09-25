// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using FluentAssertions;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Operation;
using Microsoft.Restier.Core.Submit;
using Microsoft.Restier.Tests.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Core.Query;
using System.Data.Entity;

namespace Microsoft.Restier.Tests.Core
{

    [TestClass]
    public class ConventionBasedMethodNameFactoryTests : RestierTestBase
    {

        [TestMethod]
        public void ConventionBasedMethodNameFactory_Insert_PreSubmit()
        {
            var item = new DataModificationItem("TestItems", typeof(string), typeof(string), RestierEntitySetOperation.Insert, null, null, null);
            var name = ConventionBasedMethodNameFactory.GetEntitySetMethodName(item, RestierPipelineState.PreSubmit);
            name.Should().Be("OnInsertingString");
        }

        [TestMethod]
        public void ConventionBasedMethodNameFactory_Insert_PostSubmit()
        {
            var item = new DataModificationItem("TestItems", typeof(string), typeof(string), RestierEntitySetOperation.Insert, null, null, null);
            var name = ConventionBasedMethodNameFactory.GetEntitySetMethodName(item, RestierPipelineState.PostSubmit);
            name.Should().Be("OnInsertedString");
        }

        [TestMethod]
        public void ConventionBasedMethodNameFactory_Update_PreSubmit()
        {
            var item = new DataModificationItem("TestItems", typeof(string), typeof(string), RestierEntitySetOperation.Update, null, null, null);
            var name = ConventionBasedMethodNameFactory.GetEntitySetMethodName(item, RestierPipelineState.PreSubmit);
            name.Should().Be("OnUpdatingString");
        }

        [TestMethod]
        public void ConventionBasedMethodNameFactory_Update_PostSubmit()
        {
            var item = new DataModificationItem("TestItems", typeof(string), typeof(string), RestierEntitySetOperation.Update, null, null, null);
            var name = ConventionBasedMethodNameFactory.GetEntitySetMethodName(item, RestierPipelineState.PostSubmit);
            name.Should().Be("OnUpdatedString");
        }

        [TestMethod]
        public void ConventionBasedMethodNameFactory_Delete_PreSubmit()
        {
            var item = new DataModificationItem("TestItems", typeof(string), typeof(string), RestierEntitySetOperation.Delete, null, null, null);
            var name = ConventionBasedMethodNameFactory.GetEntitySetMethodName(item, RestierPipelineState.PreSubmit);
            name.Should().Be("OnDeletingString");
        }

        [TestMethod]
        public void ConventionBasedMethodNameFactory_Delete_PostSubmit()
        {
            var item = new DataModificationItem("TestItems", typeof(string), typeof(string), RestierEntitySetOperation.Delete, null, null, null);
            var name = ConventionBasedMethodNameFactory.GetEntitySetMethodName(item, RestierPipelineState.PostSubmit);
            name.Should().Be("OnDeletedString");
        }

        [TestMethod]
        public void ConventionBasedMethodNameFactory_Filter_PreSubmit()
        {
            var item = new DataModificationItem("TestItems", typeof(string), typeof(string), RestierEntitySetOperation.Filter, null, null, null);
            var name = ConventionBasedMethodNameFactory.GetEntitySetMethodName(item, RestierPipelineState.PreSubmit);
            name.Should().Be("");
        }

        [TestMethod]
        public void ConventionBasedMethodNameFactory_Filter_Submit()
        {
            var item = new DataModificationItem("TestItems", typeof(string), typeof(string), RestierEntitySetOperation.Filter, null, null, null);
            var name = ConventionBasedMethodNameFactory.GetEntitySetMethodName(item, RestierPipelineState.Submit);
            name.Should().Be("OnFilterTestItems");
        }

        [TestMethod]
        public void ConventionBasedMethodNameFactory_Filter_PostSubmit()
        {
            var item = new DataModificationItem("TestItems", typeof(string), typeof(string), RestierEntitySetOperation.Filter, null, null, null);
            var name = ConventionBasedMethodNameFactory.GetEntitySetMethodName(item, RestierPipelineState.PostSubmit);
            name.Should().Be("");
        }

        [TestMethod]
        public void ConventionBasedMethodNameFactory_ExecuteMethod_Authorize()
        {
            var container = new RestierContainerBuilder(typeof(TestApi));
            container.Services.AddRestierCoreServices(typeof(TestApi)).AddRestierConventionBasedServices(typeof(TestApi)).AddTestStoreApiServices();
            var provider = container.BuildContainer();
            var api = provider.GetService<TestApi>();

            var context = new OperationContext(api, (string test) => { return null; }, "TestMethod", true, null);
            var name = ConventionBasedMethodNameFactory.GetFunctionMethodName(context, RestierPipelineState.Authorization, RestierOperationMethod.Execute);
            name.Should().Be("CanExecuteTestMethod");
        }


        [TestMethod]
        public void ConventionBasedMethodNameFactory_ExecuteMethod_PreSubmit()
        {
            var container = new RestierContainerBuilder(typeof(TestApi));
            container.Services.AddRestierCoreServices(typeof(TestApi)).AddRestierConventionBasedServices(typeof(TestApi)).AddTestStoreApiServices();
            var provider = container.BuildContainer();
            var api = provider.GetService<TestApi>();

            var context = new OperationContext(api, (string test) => { return null; }, "TestMethod", true, null);
            var name = ConventionBasedMethodNameFactory.GetFunctionMethodName(context, RestierPipelineState.PreSubmit, RestierOperationMethod.Execute);
            name.Should().Be("OnExecutingTestMethod");
        }

        [TestMethod]
        public void ConventionBasedMethodNameFactory_ExecuteMethod_Submit()
        {
            var container = new RestierContainerBuilder(typeof(TestApi));
            container.Services.AddRestierCoreServices(typeof(TestApi)).AddRestierConventionBasedServices(typeof(TestApi)).AddTestStoreApiServices();
            var provider = container.BuildContainer();
            var api = provider.GetService<TestApi>();

            var context = new OperationContext(api, (string test) => { return null; }, "TestMethod", true, null);
            var name = ConventionBasedMethodNameFactory.GetFunctionMethodName(context, RestierPipelineState.Submit, RestierOperationMethod.Execute);
            name.Should().Be("");
        }

        [TestMethod]
        public void ConventionBasedMethodNameFactory_ExecuteMethod_PostSubmit()
        {
            var container = new RestierContainerBuilder(typeof(TestApi));
            container.Services.AddRestierCoreServices(typeof(TestApi)).AddRestierConventionBasedServices(typeof(TestApi)).AddTestStoreApiServices();
            var provider = container.BuildContainer();
            var api = provider.GetService<TestApi>();

            var context = new OperationContext(api, (string test) => { return null; }, "TestMethod", true, null);
            var name = ConventionBasedMethodNameFactory.GetFunctionMethodName(context, RestierPipelineState.PostSubmit, RestierOperationMethod.Execute);
            name.Should().Be("OnExecutedTestMethod");
        }


        private class TestApi : ApiBase
        {

            public static IServiceCollection ConfigureApi(Type apiType, IServiceCollection services)
            {
                var changeSetPreparer = new StoreChangeSetInitializer();
                var submitExecutor = new DefaultSubmitExecutor();
                var queryExpressionSourcer = new StoreQueryExpressionSourcer();

                //ApiBase.ConfigureApi(apiType, services);
                services.AddChainedService<IChangeSetInitializer>((sp, next) => changeSetPreparer);
                services.AddChainedService<ISubmitExecutor>((sp, next) => submitExecutor);
                services.AddChainedService<IQueryExpressionSourcer>((sp, next) => queryExpressionSourcer);

                return services;
            }
            public TestApi(IServiceProvider serviceProvider) : base(serviceProvider)
            {
            }
        }


    }

}
