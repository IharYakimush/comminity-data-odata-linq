using System.Collections.Generic;
using System.Reflection;

namespace Community.Data.OData.Linq
{
    public interface IAssembliesResolver
    {
        ICollection<Assembly> GetAssemblies();
    }
}