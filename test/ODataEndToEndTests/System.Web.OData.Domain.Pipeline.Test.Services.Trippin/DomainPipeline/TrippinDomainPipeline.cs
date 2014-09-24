using Microsoft.Data.Domain;
using Microsoft.Data.Domain.Conventions;
using Microsoft.Data.Domain.EntityFramework;
using System.Web.OData.Domain.Test.Services.Trippin.Models;

namespace System.Web.OData.Domain.Test.Services.Trippin.DomainPipeline
{
    [TestAttribute]
    public class TrippinDomainPipeline : DbDomain<TrippinModel>
    {
        public TrippinDomainPipeline()
            :base()
        {
        }
        public TrippinModel Context { get { return DbContext; } }
        /// <summary>
        /// Implements an action import.
        /// </summary>
        [Action]
        public void ResetDataSource()
        {
            TrippinModel.ResetDataSource();
        }
    }

    public class TestAttribute : EnableConventionsAttribute
    {
        public override void Configure(
            DomainConfiguration configuration,
            Type type)
        {
            base.Configure(configuration, type);
            ConventionalActionProvider.ApplyTo(configuration, type);
        } 
    }
}