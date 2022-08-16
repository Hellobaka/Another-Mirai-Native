﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace CQP
{
    public static class Helper
    {
        public static string QQ { get; set; }
        public static string NickName { get; set; }
        public static string WsURL { get; set; }
        public static string WsAuthKey { get; set; }
        public static int MaxLogCount { get; set; } = 500;
        public static long TimeStamp => (long)(DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
        public static DateTime TimeStamp2DateTime(long timestamp) => new DateTime(1970, 1, 1, 8, 0, 0, DateTimeKind.Local).AddSeconds(timestamp);
        public static bool ContainsKey(this JToken json, string key)
        {
            try
            {
                foreach (JProperty item in json.Children())
                {
                    if (item.Name == key) return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
        public static async Task<Stream> Get(string url)
        {
            try
            {
                using var http = new HttpClient();
                var r = http.GetAsync(url);
                return await r.Result.Content.ReadAsStreamAsync();
            }
            catch
            {
                return null;
            }
        }
        public static T String2Enum<T>(string value)
        {
            return (T)Enum.Parse(typeof(T), value);
        }       
        public static T Int2Enum<T>(int value)
        {
            return (T)Enum.Parse(typeof(T), Enum.GetName(typeof(T), value));
        }
        public static string ToJson(this object obj) => JsonConvert.SerializeObject(obj);
    }
}
