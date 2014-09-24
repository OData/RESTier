namespace System.Web.OData.Domain
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ActionAttribute : Attribute
    {
        public string Name { get; set; }
        
        public string Namespace { get; set; }
    }
}
