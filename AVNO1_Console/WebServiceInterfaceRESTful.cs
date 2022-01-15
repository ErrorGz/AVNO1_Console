using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.ServiceModel.Web;
using System.IO;
using Newtonsoft.Json.Linq;

using System.Threading;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.ServiceModel.Channels;
using SQLite;
using SQLiteNetExtensions.Extensions;
using Newtonsoft.Json;

namespace AVNO1_Console
{


    [ServiceContract(Name = "WebServiceInterfaceRESTful")]
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Single, IncludeExceptionDetailInFaults = true)]
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    public class WebServiceInterfaceRESTful
    {
        ReaderWriterLockSlim locker;
        SQLiteConnection conn;

        ListPostCache cache = new ListPostCache();


        public WebServiceInterfaceRESTful(ReaderWriterLockSlim l, SQLiteConnection c)
        {
            locker = l;
            conn = c;
        }

        public class ResponseJson
        {
            public List<PostJson> Posts { get; set; }
            public int Count { get; set; }
            public string Msg { get; set; }
            public long ms { get; set; }
        }

        public class PostJson
        {
            public int Id { get; set; }
            public string 标题 { get; set; }
            public string 作者 { get; set; }
            public string 日期 { get; set; }
            public string 封面 { get; set; }
            public string 内容 { get; set; }
            public string 作品名 { get; set; }
            public string 番号 { get; set; }

            public string 发行日 { get; set; }
            public string 女优名 { get; set; }

            public string 事务所 { get; set; }

            public string 身高罩杯 { get; set; }

        }



        [OperationContract]
        [WebGet(UriTemplate = "/GetLastPost/{page}-{pagesize}", ResponseFormat = WebMessageFormat.Json)]
        public ResponseJson GetLastPost(string page, string pagesize)
        {
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Origin", "*");
            int ipage = 1;
            int ipagesize = 5;
            Stopwatch sw = new Stopwatch();

            int.TryParse(page, out ipage);
            int.TryParse(pagesize, out ipagesize);

            sw.Start();
            locker.EnterReadLock();

            ////List<Post> listPost = null;

            ////listPost = cache.Get("");
            ////if (listPost == null)
            ////{
            ////    listPost = conn.GetAllWithChildren<Post>()
            ////        .OrderByDescending(o => o.日期)
            ////        .ToList();
            ////    cache.Add("", listPost);
            ////}

            //var listPost = conn.GetAllWithChildren<Post>().OrderByDescending(o => o.日期);
            //var takePost = listPost.Skip(ipage * ipagesize)
            //    .Take(ipagesize)
            //    .Select(o => new PostJson
            //    {
            //        Id = o.Id,
            //        标题 = o.标题,
            //        作者 = o.作者,
            //        日期 = o.日期.ToShortDateString(),
            //        封面 = o.封面,
            //        内容 = o.内容,
            //        作品名 = o.Video != null ? o.Video.作品名 : null,
            //        番号 = o.Video != null ? o.Video.番号 : null,
            //        发行日 = o.Video != null ? o.Video.发行日 : null,
            //        女优名 = o.Video != null ? o.Video.女优名 : null,
            //        事务所 = o.Video != null ? o.Video.事务所 : null,
            //        身高罩杯 = o.Video != null ? o.Video.身高罩杯 : null
            //    }).ToList();
            int count2 = 0;
            List<PostJson> takePost2 = null;
            conn.RunInTransaction(() =>
            {
                count2 = conn.Table<Post>().Count();
                var listPost2 = conn.Table<Post>().OrderByDescending(o => o.日期).Skip(ipage * ipagesize).Take(ipagesize).ToList();
                takePost2 = listPost2.Select(o =>
                  {
                      var v = conn.Table<Video>().Where(p => p.PostId == o.Id).FirstOrDefault();
                      if (v != null)
                      {
                          return new PostJson
                          {
                              Id = o.Id,
                              标题 = o.标题,
                              作者 = o.作者,
                              日期 = o.日期.ToShortDateString(),
                              封面 = o.封面,
                              内容 = o.内容,
                              作品名 = v != null ? v.作品名 : null,
                              番号 = v != null ? v.番号 : null,
                              发行日 = v != null ? v.发行日 : null,
                              女优名 = v != null ? v.女优名 : null,
                              事务所 = v != null ? v.事务所 : null,
                              身高罩杯 = v != null ? v.身高罩杯 : null
                          };
                      }
                      else
                      {
                          return new PostJson
                          {
                              Id = o.Id,
                              标题 = o.标题,
                              作者 = o.作者,
                              日期 = o.日期.ToShortDateString(),
                              封面 = o.封面,
                              内容 = o.内容,
                          };
                      }
                  }).ToList();
            });


            locker.ExitReadLock();
            sw.Stop();

            WriteLine($"GetLastPost,page:{page},size:{ipagesize},time:{sw.ElapsedMilliseconds / 1000f:0.##}");

            takePost2.ForEach(o =>
            {
                WriteLine($"GetLastPost:{o.标题}");
            });

            ResponseJson json = new ResponseJson()
            {
                Posts = takePost2,
                //Count = listPost2.Count(),
                Count = count2,
                ms = sw.ElapsedMilliseconds,
                Msg = null

            };

            return json;
        }



        [OperationContract]
        [WebGet(UriTemplate = "/GetSearchPost/{page}-{pagesize}/{searchText}", ResponseFormat = WebMessageFormat.Json)]
        public ResponseJson GetSearchPost(string page, string pagesize, string searchText)
        {
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Origin", "*");
            int ipage = 1;
            int ipagesize = 5;

            int.TryParse(page, out ipage);
            int.TryParse(pagesize, out ipagesize);

            Stopwatch sw = new Stopwatch();
            sw.Start();
            locker.EnterReadLock();

            List<Post> listPost = null;

            listPost = cache.Get(searchText);
            if (listPost == null)
            {
                listPost = conn.GetAllWithChildren<Post>()
                  .Where(o => o.标题.Contains(searchText) || o.内容.Contains(searchText))
                  .OrderByDescending(o => o.日期)
                  .ToList();
                cache.Add(searchText, listPost);
            }

            var takePost = listPost.Skip(ipage * ipagesize)
                .Take(ipagesize)
                 .Select(o => new PostJson
                 {
                     Id = o.Id,
                     标题 = o.标题,
                     作者 = o.作者,
                     日期 = o.日期.ToShortDateString(),
                     封面 = o.封面,
                     内容 = o.内容,
                     作品名 = o.Video != null ? o.Video.作品名 : null,
                     番号 = o.Video != null ? o.Video.番号 : null,
                     发行日 = o.Video != null ? o.Video.发行日 : null,
                     女优名 = o.Video != null ? o.Video.女优名 : null,
                     事务所 = o.Video != null ? o.Video.事务所 : null,
                     身高罩杯 = o.Video != null ? o.Video.身高罩杯 : null
                 }).ToList();

            locker.ExitReadLock();
            sw.Stop();

            WriteLine($"GetLastPost,page:{page},size:{ipagesize},searchtext:{searchText},time:{sw.ElapsedMilliseconds / 1000:0.##}");

            takePost.ForEach(o =>
            {
                WriteLine($"GetLastPost:{o.标题}");
            });

            ResponseJson json = new ResponseJson()
            {
                Posts = takePost,
                Count = listPost.Count(),
                ms = sw.ElapsedMilliseconds,
                Msg = null

            };
            return json;
        }


        //private Stream GetStream(string str)
        //{
        //    //str = Base64.EncodeBase64(str);
        //    MemoryStream ms = new MemoryStream();
        //    StreamWriter sw = new StreamWriter(ms);
        //    sw.AutoFlush = true;
        //    sw.Write(str);
        //    ms.Position = 0;
        //    WebOperationContext.Current.OutgoingResponse.ContentType = "text/plain";
        //    return ms;
        //}

        static private void Write(string msg)
        {
            Debug.Write(msg);
            Console.Write(msg);
        }
        static private void WriteLine(string msg)
        {
            Debug.WriteLine(DateTime.Now.ToString() + " " + msg);
            Console.WriteLine(DateTime.Now.ToString() + " " + msg);
        }
    }
}
