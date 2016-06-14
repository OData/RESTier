using System.ComponentModel.DataAnnotations;

namespace Microsoft.OData.Service.Sample.Trippin.Models
{   
     /// <summary>
     /// This class is demo how to extend more entity set and type 
     /// with conversion model builder of existing model from EF
     /// </summary>
    public class Extender
    {
        public int ExtenderID { get; set; }

        [Required]
        public string Name { get; set; }

        public string Description { get; set; }
    }
}