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
        public static JToken SelectExpandJsonToken<T>(
            this ODataQuery<T> query,
            string selectText = null,
            string expandText = null,
            Action<JsonSerializer> configureSerializer = null,
            string entitySetName = null)
        {
            JTokenWriter writer = new JTokenWriter();
            using (writer)
            {
                SelectExpandJson(query, writer, selectText, expandText, configureSerializer, entitySetName);

                return writer.Token;
            }            
        }

        public static void SelectExpandJson<T>(
            this ODataQuery<T> query,
            JsonWriter writer,
            string selectText = null,
            string expandText = null,
            Action<JsonSerializer> configureSerializer = null,
            string entitySetName = null)
        {
            ISelectExpandWrapper[] result = query.SelectExpand(selectText, expandText, entitySetName).ToArray();

            JsonSerializer serializer = new JsonSerializer();

            configureSerializer?.Invoke(serializer);
            serializer.Converters.Add(new SelectExpandWrapperConverter());

            serializer.Serialize(writer, result);
        }
    }
}