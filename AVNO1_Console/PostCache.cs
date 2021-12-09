using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVNO1_Console
{
    public class PostCache
    {
        public string Search { get; set; }
        public List<Post> CacheData { get; set; }
        public DateTime CacheTime { get; set; }

    }

    public class ListPostCache
    {
        List<PostCache> listPostCache = new List<PostCache>();


        public void Add(string s, List<Post> d)
        {
            listPostCache.RemoveAll(o => (DateTime.Now - o.CacheTime).TotalMinutes > 30);
            listPostCache.Add(new PostCache() { Search = s, CacheData = d, CacheTime = DateTime.Now });
        }

        public List<Post> Get(string s)
        {
            listPostCache.RemoveAll(o => (DateTime.Now - o.CacheTime).TotalMinutes > 30);
            var r = listPostCache.Where(o => o.Search == s).Select(o => o.CacheData).FirstOrDefault();
            return r;
        }
    }
}
