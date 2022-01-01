using Community.OData.Linq.xTests.SampleData;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace Community.OData.Linq.xTests
{
    public class DateTimeFilterTests
    {
        private readonly ITestOutputHelper output;
        private static readonly DateTime dtUtc = new DateTime(2018, 1, 26, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime dtLocal = new DateTime(2018, 1, 26, 0, 0, 0, DateTimeKind.Local);
        private static readonly DateTime dt = new DateTime(2018, 1, 26, 0, 0, 0);
        private static readonly DateTimeOffset dto = new DateTimeOffset(dt, TimeZoneInfo.Local.BaseUtcOffset);
        //private readonly DateTimeOffset dtoUtc = new DateTimeOffset(new DateTime(2018, 1, 26).ToUniversalTime());

        public DateTimeFilterTests(ITestOutputHelper output)
        {
            this.output = output ?? throw new ArgumentNullException(nameof(output));
        }

        [Fact]
        public void LocalDateTimeEqualCorrectUtcOffset()
        {
            string value = dto.ToString("u").Replace(" ", "T");
            string filter = $"DateTime eq {value}";
            output.WriteLine(filter);

            var result = SimpleClass.CreateQuery().OData().Filter(filter).ToArray();
            Assert.Single(result);
            output.WriteLine(result.Single().DateTime.ToString());
        }

        [Fact]
        public void LocalDateTimeEqualCorrectLocalOffset()
        {
            string value = dto.ToString("s") + dto.ToString("zzz");
            string filter = $"DateTime eq {value}";
            output.WriteLine(filter);

            var result = SimpleClass.CreateQuery().OData().Filter(filter).ToArray();
            Assert.Single(result);
            output.WriteLine(result.Single().DateTime.ToString());
        }

        [Fact]
        public void LocalDateTimetNotEquaIncorrectUtcOffset()
        {
            string value = dto.ToString("s") + "Z";
            string filter = $"DateTime eq {value}";
            output.WriteLine(filter);

            var result = SimpleClass.CreateQuery().OData().Filter(filter).ToArray();
            Assert.Empty(result);
        }

        [Fact]
        public void DateTimeOffsetEqualCorrectUtcOffset()
        {
            string value = dto.ToString("u").Replace(" ", "T");
            string filter = $"DateTimeOffset eq {value}";
            output.WriteLine(filter);

            var result = SimpleClass.CreateQuery().OData().Filter(filter).ToArray();
            Assert.Single(result);
            output.WriteLine(result.Single().DateTimeOffset.ToString());

        }

        [Fact]
        public void DateTimeOffsetEqualCorrectLocalOffset()
        {
            string value = dto.ToString("s") + dto.ToString("zzz");
            string filter = $"DateTimeOffset eq {value}";
            output.WriteLine(filter);

            var result = SimpleClass.CreateQuery().OData().Filter(filter).ToArray();
            Assert.Single(result);
            output.WriteLine(result.Single().DateTimeOffset.ToString());
        }

        [Fact]
        public void DateTimeOffsetNotEqualIncorrectUtcOffset()
        {
            string value = dto.ToString("s") + "Z";
            string filter = $"DateTimeOffset eq {value}";
            output.WriteLine(filter);

            var result = SimpleClass.CreateQuery().OData().Filter(filter).ToArray();
            Assert.Empty(result);
        }

        [Fact]
        public void UtcDateTimeEqualCorrectUtcOffset()
        {
            string value = dto.ToString("s") + "Z";
            //string value = dto.ToString("u").Replace(" ", "T");
            string filter = $"DateTime eq {value}";
            output.WriteLine(filter);

            var result = SimpleClass.CreateQuery().OData(c=>c.QuerySettings.DefaultTimeZone = TimeZoneInfo.Utc).Filter(filter).ToArray();
            Assert.Single(result);
            output.WriteLine(result.Single().DateTime.ToString() + "UTC");

        }

        [Fact]
        public void UtcDateTimeEqualCorrectLocalOffset()
        {
            string value = dto.DateTime.ToLocalTime().ToString("s").Replace(" ", "T") + dto.ToString("zzz");
            string filter = $"DateTime eq {value}";
            output.WriteLine(filter);

            var result = SimpleClass.CreateQuery().OData(c => c.QuerySettings.DefaultTimeZone = TimeZoneInfo.Utc).Filter(filter).ToArray();
            Assert.Single(result);
            output.WriteLine(result.Single().DateTime.ToString() + "UTC");
        }

        [Fact]
        public void UtcDateTimeNotEqualIncorrectLocalOffset()
        {
            string value = dto.ToString("u").Replace(" ", "T").Replace("Z", dto.ToString("zzz"));
            string filter = $"DateTime eq {value}";
            output.WriteLine(filter);

            var result = SimpleClass.CreateQuery().OData(c => c.QuerySettings.DefaultTimeZone = TimeZoneInfo.Utc).Filter(filter).ToArray();
            Assert.Empty(result);
        }

        [Fact]
        public void UtcDateTimeNotEqualIncorrectLocalOffset2()
        {
            string value = dto.ToString("s") + dto.ToString("zzz");
            string filter = $"DateTime eq {value}";
            output.WriteLine(filter);

            var result = SimpleClass.CreateQuery().OData(c => c.QuerySettings.DefaultTimeZone = TimeZoneInfo.Utc).Filter(filter).ToArray();
            Assert.Empty(result);
        }

        [Fact]
        public void UtcDateTimetNotEqualIncorrectUtcOffset()
        {
            string value = dto.ToString("u").Replace(" ", "T");
            string filter = $"DateTime eq {value}";
            output.WriteLine(filter);

            var result = SimpleClass.CreateQuery().OData(c => c.QuerySettings.DefaultTimeZone = TimeZoneInfo.Utc).Filter(filter).ToArray();
            Assert.Empty(result);
        }

        [Fact]
        public void DateTimeCompare()
        {
            // Compare method compares the Ticks property of t1 and t2 but ignores their Kind property.
            // Before comparing DateTime objects, ensure that the objects represent times in the same time zone.
            Assert.Equal(dt, dtLocal);
            Assert.Equal(dt, dtUtc);
            Assert.Equal(dtUtc, dtLocal);

            Assert.NotEqual(TimeZoneInfo.Local, TimeZoneInfo.Utc);
            Assert.NotEqual(TimeZoneInfo.Local.GetHashCode(), TimeZoneInfo.Utc.GetHashCode());

            Assert.Equal(TimeZoneInfo.Utc, TimeZoneInfo.Utc);
            Assert.Equal(TimeZoneInfo.Utc.GetHashCode(), TimeZoneInfo.Utc.GetHashCode());
        }
    }                
}
