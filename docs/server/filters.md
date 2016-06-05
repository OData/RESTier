# Entity Set Filters

Entity set filter convention helps plug in a piece of filtering logic for entity set. It is done via adding an 
`OnFilter[entity set name](IQueryable<T> entityset)` method to the `Api` class.

1. The filter method name must be OnFilter[entity set name], ending with the target entity set name.
2. It must be a **protected** method on the `Api` class.
3. It should accept an IQueryable<T> parameter and return an IQueryable<T> result where T is the entity type. 

Supposed that ~/AdventureWorksLT/Products can get all the Product entities, the below OnFilterProducts method will filter some Product entities by checking the ProductID.

```cs
using Microsoft.Restier.Core;
using Microsoft.Restier.Provider.EntityFramework;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace AdventureWorksLTSample.Models
{
    public class AdventureWorksApi : EntityFrameworkApi<AdventureWorksContext>
    {
        protected IQueryable<Product> OnFilterProducts(IQueryable<Product> entitySet)
        {
            return entitySet.Where(s => s.ProductID % 3 == 0).AsQueryable();
        }
    }
}
```

Now some testings will show that:

1. ~/AdventureWorksLT/Products will only get the Product entities whose ProductID is  3,6,9,12,15,... 
2. ~/AdventureWorksLT/Products([product id]) will only be able to get a Product entity whose ProductID mod 3 results a zero. 