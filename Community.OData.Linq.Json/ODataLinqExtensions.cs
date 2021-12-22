namespace Community.OData.Linq.Json
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public static class ODataLinqExtensions
    {        
        /// <summary>
        /// The to Json.
        /// </summary>
        /// <param name="value">
        /// The value.
        /// </param>
        /// <param name="configureSerializer">
        /// The configure serializer.
        /// </param>
        /// <returns>
        /// The <see cref="JToken"/>.
        /// </returns>
        public static JToken ToJson(this IEnumerable<ISelectExpandWrapper> value, Action<JsonSerializer> configureSerializer = null)
        {
            JTokenWriter writer = new JTokenWriter();
            using (writer)
            {
                value.ToJson(writer, configureSerializer);

                return writer.Token;
            }
        }

        /// <summary>
        /// The to json.
        /// </summary>
        /// <param name="value">
        /// The value.
        /// </param>
        /// <param name="writer">
        /// The writer.
        /// </param>
        /// <param name="configureSerializer">
        /// The configure serializer.
        /// </param>
        public static void ToJson(this IEnumerable<ISelectExpandWrapper> value, JsonWriter writer, Action<JsonSerializer> configureSerializer = null)
        {
            WriteToSerializer(writer, configureSerializer, value);
        }

        private static void WriteToSerializer(JsonWriter writer, Action<JsonSerializer> configureSerializer, IEnumerable<ISelectExpandWrapper> result)
        {
            JsonSerializer serializer = new JsonSerializer();

            configureSerializer?.Invoke(serializer);
            SelectExpandWrapperConverter jsonConverter = new SelectExpandWrapperConverter();
            
            serializer.Converters.Add(jsonConverter);

            serializer.Serialize(writer, result.ToArray());
        }
    }
}