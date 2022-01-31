using System.Text.Json;
using Org.BouncyCastle.Utilities.Encoders;

namespace SpotifyNET.Models
{
    public readonly struct CommandBody
    {
        public JsonElement? Obj
        {
            get;
        }
        public byte[] Data
        {
            get;
        }
        public string Value
        {
            get;
        }

        internal CommandBody(JsonElement? obj)
        {
            this.Obj = obj;
            if (obj?.TryGetProperty("data", out var data) ?? false)
            {
                Data = Base64.Decode(data.GetString());
            }
            else Data = null;

            if (obj?.TryGetProperty("value", out var value) ?? false)
            {
                Value = value.GetString();
            }
            else Value = null;
        }


        public int? ValueInt()
        {
            return Value == null ? null : int.Parse(Value);
        }

        public bool? ValueBool()
        {
            return Value == null ? null : bool.Parse(Value);
        }
    }
}
