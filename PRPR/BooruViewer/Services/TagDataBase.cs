﻿using PRPR.BooruViewer.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Json;

namespace PRPR.BooruViewer.Services
{
    public static class TagDataBase
    {
        public static int CurrentVersion = 0;

        /// <summary>
        /// The complete tags list pre-exist in the App
        /// Already sorted by Count
        /// Updated manually by the programmer when app updates as it is slow to download from the server directly
        /// </summary>
        public static Dictionary<string, TagDetail> AllTags = new Dictionary<string, TagDetail>();

        public static IEnumerable<TagDetail> Search(string key)
        {
            return AllTags.Values.Where(o => o.Name.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0).OrderByDescending(o => Similiarity(o.Name, key));
        }


        public static async Task DownloadLatestTagDBAsync()
        {
            Windows.Web.Http.HttpClient httpClient2 = new Windows.Web.Http.HttpClient();
            var json = await httpClient2.GetStringAsync(new Uri($"{YandeClient.HOST}/tag/summary.json"));
            
            
            JsonObject root = JsonValue.Parse(json).GetObject();
            var version = root.GetNamedNumber("version");
            var data = root.GetNamedString("data");

            if (version > CurrentVersion)
            {
                AllTags.Clear();

                var items = data.Split(' ');
                foreach (var item in items)
                {
                    var names = item.Split('`');

                    if (names.Length >= 2)
                    {

                        var tagType = int.Parse(names[0]);
                        var name = names[1];
                        var orginal = new TagDetail() { Name = name, Type = (TagType)tagType };

                        AllTags[name] = orginal;
                        for (int i = 2; i < names.Length; i++)
                        {

                            AllTags[names[i]] = new TagDetail() { Name = names[i], Type = (TagType)tagType, Parent = name };
                        }
                    }
                }
            }
        }


        private static Char[] specialChars = { '_', '(' , '/' };
        public static int Similiarity(string name, string key)
        {
            var index = name.IndexOf(key, StringComparison.OrdinalIgnoreCase);
            var lengthDiff = name.Length - key.Length;


            var baseValue = int.MaxValue;
            if (index == -1)
            {
                // Name doesnt contain the key, totally different
                return int.MinValue;
            }
            else 
            {
                if (index != 0)
                {
                    baseValue = baseValue >> 1;
                    if (!specialChars.Contains(name[index - 1]))
                    {
                        baseValue = baseValue >> 1;
                    }
                }


                if (index + key.Length != name.Length)
                {
                    baseValue = baseValue >> 1;
                    if (!specialChars.Contains(name[index + key.Length]))
                    {
                        baseValue = baseValue >> 1;
                    }
                }

                // Name contains the key at the beginning, Very similar
                return baseValue - lengthDiff;
            }
        }
    }


    public enum TagType
    {
        None = 0,
        Artist,
        NotUsed,
        Copyright,
        Character,
        Circle,
        Faults
    }

    public class TagDetail
    {
        public string Name { get; set; }

        public TagType Type { get; set; }

        public string Parent { get; set; } = null;


        public bool IsAlias
        {
            get
            {
                return Parent != null;
            }
        }
        
        public override string ToString()
        {
            return this.Name;
        }
    }



    public class TagDetailInMiddle : TagDetail
    {
        public TagDetailInMiddle(TagDetail tag, string prefix, string suffix)
        {
            Name = tag.Name;
            Type = tag.Type;
            Parent = tag.Parent;
            Prefix = prefix;
            Suffix = suffix;
        }

        public string Prefix { get; set; } = "";

        public string Suffix { get; set; } = "";

        
        public override string ToString()
        {
            return Prefix + this.Name + Suffix;
        }
    }
}
