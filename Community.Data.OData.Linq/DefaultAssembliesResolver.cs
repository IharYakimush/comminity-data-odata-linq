namespace Community.OData.Linq
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    internal class DefaultAssembliesResolver : IAssembliesResolver
    {
        /// <summary> Returns a list of assemblies available for the application. </summary>
        /// <returns>A &lt;see cref="T:System.Collections.ObjectModel.Collection`1" /&gt; of assemblies.</returns>
        public virtual ICollection<Assembly> GetAssemblies()
        {
            return (ICollection<Assembly>)((IEnumerable<Assembly>)AppDomain.CurrentDomain.GetAssemblies()).ToList<Assembly>();
        }
    }
}