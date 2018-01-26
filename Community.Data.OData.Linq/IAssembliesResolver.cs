namespace Community.OData.Linq
{
    using System.Collections.Generic;
    using System.Reflection;

    internal interface IAssembliesResolver
    {
        ICollection<Assembly> GetAssemblies();
    }
}