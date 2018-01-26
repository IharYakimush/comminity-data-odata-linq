using System.Collections.Generic;
using System.Reflection;

namespace Community.Data.OData.Linq
{
    internal interface IAssembliesResolver
    {
        ICollection<Assembly> GetAssemblies();
    }
}