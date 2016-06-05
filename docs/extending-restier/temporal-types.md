# Temporal Types

When using the Microsoft.Restier.Providers.EntityFramework provider, temporal types are now supported. The table below 
shows how Temporal Types map to SQL Types:

|        EF Type        |      SQL Type      |      Edm Type      | Need ColumnAttribute? |
|:---------------------:|:------------------:|:------------------:|:---------------------:|
| System.DateTime       | DateTime/DateTime2 | Edm.DateTimeOffset | Y                     |
| System.DateTimeOffset | DateTimeOffset     | Edm.DateTimeOffset | N                     |
| System.DateTime       | Date               | Edm.Date           | Y                     |
| System.TimeSpan       | Time               | Edm.TimeOfDay      | Y                     |
| System.TimeSpan       | Time               | Edm.Duration       | N                     |

The next sections illustrate how to use use temporal types in various scenarios.

## Edm.DateTimeOffset
Suppose you have an entity class `Person`, all the following code define `Edm.DateTimeOffset` properties in the 
EDM model though the underlying SQL types are different (see the value of the `TypeName` property). You can see 
Column attribute is optional here.


    using System;
    using System.ComponentModel.DataAnnotations.Schema;
    
    public class Person
    {
        public DateTime BirthDateTime1 { get; set; }

        [Column(TypeName = "DateTime")]
        public DateTime BirthDateTime2 { get; set; }

        [Column(TypeName = "DateTime2")]
        public DateTime BirthDateTime3 { get; set; }

        public DateTimeOffset BirthDateTime4 { get; set; }
    }


## Edm.Date
The following code define an `Edm.Date` property in the EDM model.

    using System;
    using System.ComponentModel.DataAnnotations.Schema;
    
    public class Person
    {
        [Column(TypeName = "Date")]
        public DateTime BirthDate { get; set; }
    }

## Edm.Duration
The following code define an `Edm.Duration` property in the EDM model.

    using System;
    using System.ComponentModel.DataAnnotations.Schema;

    public class Person
    {
        public TimeSpan WorkingHours { get; set; }
    }

## Edm.TimeOfDay
The following code define an `Edm.TimeOfDay` property in the EDM model. Please note that you MUST NOT omit the 
`ColumnTypeAttribute` on a `TimeSpan` property otherwise it will be recognized as an `Edm.Duration` as described above.

    using System;
    using System.ComponentModel.DataAnnotations.Schema;

    public class Person
    {
        [Column(TypeName = "Time")]
        public TimeSpan BirthTime { get; set; }
    }

As before, if you have the need to override `ODataPayloadValueConverter`, please now change to override 
`RestierPayloadValueConverter` instead in order not to break the payload value conversion specialized for these 
temporal types.