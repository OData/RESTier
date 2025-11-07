using Microsoft.Restier.AspNetCore.Model;
using Microsoft.Restier.EntityFrameworkCore;
using Microsoft.Restier.Samples.Postgres.AspNetCore.Models;
using System;
using System.Diagnostics;

namespace Microsoft.Restier.Samples.Postgres.AspNetCore.Controllers
{
    public class RestierTestContextApi : EntityFrameworkApi<RestierTestContext>
    {

        #region Public Properties

        ///// <summary>
        ///// Gets or sets the message publisher.
        ///// </summary>
        //public IMessagePublisher MessagePublisher { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="PartnerProfileContextApi"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <param name="messagePublisher">The message publisher.</param>
        public RestierTestContextApi(IServiceProvider serviceProvider/*, IMessagePublisher messagePublisher*/) : base(serviceProvider)
        {
            //this.MessagePublisher = messagePublisher;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Checks if the database is online.
        /// </summary>
        /// <returns>True if the database can connect; otherwise, false.</returns>
        [UnboundOperation]
        public bool IsOnline()
        {
            try
            {
                return DbContext.Database.CanConnect();
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                Debug.WriteLine(ex);
                return false;
            }
        }

        #endregion

    }
}
