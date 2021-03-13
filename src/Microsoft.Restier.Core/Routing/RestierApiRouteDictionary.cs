using System;
using System.Collections.Generic;

namespace Microsoft.Restier.Core.Routing
{

    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class RestierApiRouteDictionary : Dictionary<string, RestierApiModelMap>
    {

        /// <summary>
        /// 
        /// </summary>
        public RestierApiRouteDictionary()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serializationInfo"></param>
        /// <param name="streamingContext"></param>
        protected RestierApiRouteDictionary(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext)
        {
            throw new NotImplementedException();
        }

    }

}
