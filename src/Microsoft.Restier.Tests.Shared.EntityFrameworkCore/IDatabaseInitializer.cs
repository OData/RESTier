// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.EntityFrameworkCore;

namespace Microsoft.Restier.Tests.Shared.EntityFrameworkCore
{

    public interface IDatabaseInitializer
    {

        public void Seed(DbContext context);

    }

}
