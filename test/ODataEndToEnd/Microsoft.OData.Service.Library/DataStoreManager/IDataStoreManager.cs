// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Microsoft.OData.Service.Library.DataStoreManager
{
    /// <summary>
    /// Resource management interface.
    /// </summary>
    public interface IDataStoreManager<TKey, TDataStoreType>
    {
        TDataStoreType GetDataStoreInstance(TKey key);
        TDataStoreType ResetDataStoreInstance(TKey key);
    }
}