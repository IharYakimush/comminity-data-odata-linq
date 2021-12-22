using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;

namespace Community.OData.Linq.xTests
{
    public class HashTests
    {
        [Fact]
        public void Hashes()
        {
            ODataSettings s1 = new ODataSettings();
            ODataSettings s2 = new ODataSettings();

            Assert.Equal(s1.QuerySettings.GetHashCode(), s2.QuerySettings.GetHashCode());
            Assert.Equal(s1.DefaultQuerySettings.GetHashCode(), s2.DefaultQuerySettings.GetHashCode());
            Assert.Equal(HashCode.Combine(s1.QuerySettings, s1.DefaultQuerySettings), HashCode.Combine(s2.QuerySettings, s2.DefaultQuerySettings));

            s1.QuerySettings.EnsureStableOrdering = false;
            Assert.NotEqual(s1.QuerySettings.GetHashCode(), s2.QuerySettings.GetHashCode());
            Assert.Equal(s1.DefaultQuerySettings.GetHashCode(), s2.DefaultQuerySettings.GetHashCode());
            Assert.NotEqual(HashCode.Combine(s1.QuerySettings, s1.DefaultQuerySettings), HashCode.Combine(s2.QuerySettings, s2.DefaultQuerySettings));

            s1.QuerySettings.EnsureStableOrdering = true;
            Assert.Equal(s1.QuerySettings.GetHashCode(), s2.QuerySettings.GetHashCode());
            Assert.Equal(s1.DefaultQuerySettings.GetHashCode(), s2.DefaultQuerySettings.GetHashCode());
            Assert.Equal(HashCode.Combine(s1.QuerySettings, s1.DefaultQuerySettings), HashCode.Combine(s2.QuerySettings, s2.DefaultQuerySettings));

            s1.DefaultQuerySettings.EnableExpand = false;
            Assert.Equal(s1.QuerySettings.GetHashCode(), s2.QuerySettings.GetHashCode());
            Assert.NotEqual(s1.DefaultQuerySettings.GetHashCode(), s2.DefaultQuerySettings.GetHashCode());
            Assert.NotEqual(HashCode.Combine(s1.QuerySettings, s1.DefaultQuerySettings), HashCode.Combine(s2.QuerySettings, s2.DefaultQuerySettings));

            s1.DefaultQuerySettings.EnableExpand = true;
            Assert.Equal(s1.QuerySettings.GetHashCode(), s2.QuerySettings.GetHashCode());
            Assert.Equal(s1.DefaultQuerySettings.GetHashCode(), s2.DefaultQuerySettings.GetHashCode());
            Assert.Equal(HashCode.Combine(s1.QuerySettings, s1.DefaultQuerySettings), HashCode.Combine(s2.QuerySettings, s2.DefaultQuerySettings));

            // Microsoft classes public properties            
            Assert.Equal(HashCode.Combine(s1.ParserSettings.MaximumExpansionCount, s1.ParserSettings.MaximumExpansionDepth), HashCode.Combine(s2.ParserSettings.MaximumExpansionCount, s2.ParserSettings.MaximumExpansionDepth));
            s1.ParserSettings.MaximumExpansionCount = 1;
            Assert.NotEqual(HashCode.Combine(s1.ParserSettings.MaximumExpansionCount, s1.ParserSettings.MaximumExpansionDepth), HashCode.Combine(s2.ParserSettings.MaximumExpansionCount, s2.ParserSettings.MaximumExpansionDepth));
        }
    }
}
