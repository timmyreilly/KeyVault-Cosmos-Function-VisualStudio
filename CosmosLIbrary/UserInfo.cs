using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace CosmosLibrary
{
    public class UserInfo
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
        public string Data { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
