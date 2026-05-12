using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Bond.Persistence
{
    public class ProfessionConverter : JsonConverter<Profession>
    {
        public override bool CanWrite => false;

        public override void WriteJson(JsonWriter writer, Profession value, Newtonsoft.Json.JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override Profession ReadJson(JsonReader reader, Type objectType, Profession existingValue, bool hasExistingValue,
            Newtonsoft.Json.JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);
            var a = jo["Name"].Value<string>();

            Profession profession = new SampleProfession();
            profession.Name = a;
            
            serializer.Populate(jo.CreateReader(), profession);
            Debug.Log(profession.Name);
            return profession;
        }
    }
}