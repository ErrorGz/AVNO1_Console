using HtmlAgilityPack;
using SQLite;
using SQLiteNetExtensions.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace AVNO1_Console
{
    internal class Program
    {
        public bool Exit = false;
        public CancellationTokenSource source = new CancellationTokenSource();
        public CookieContainer cookies = new CookieContainer();

        WebServiceHost _serviceHost;

        static void Main(string[] args)
        {
            Program p = new Program();
            ReaderWriterLockSlim locker = new ReaderWriterLockSlim();
            string dbfile = "db.sqlite";
            if (args.Count() > 0)
                dbfile = args[0];
            var conn = p.InitDataBase(locker, dbfile);
            var site = conn.GetWithChildren<Site>(1);
            site.Cookies.ForEach(o =>
            {
                p.cookies.Add(new Uri(site.Url), new Cookie() { Name = o.Name, Value = o.Value });
            });


            WriteLine($"getHtmlPage({site.Url})");
            var html = p.getHtmlPage(site.Url);
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);
            var allLinks = doc.DocumentNode
                  .Descendants("a")
                  .Where(o => o.Attributes["href"] != null && o.Attributes["href"].Value.Contains("portal.php?") && o.Attributes["href"].Value.Contains("catid"))
                  .ToList();

            locker.EnterWriteLock();
            conn.RunInTransaction(() =>
            {
                foreach (var link in allLinks)
                {
                    var href = link.Attributes["href"].Value;
                    var r = Regex.Match(href, @"catid=(\d+)");
                    if (r.Success)
                    {
                        var u = new Uri(new Uri(site.Url), link.Attributes["href"].Value);
                        var b = new Block() { Id = int.Parse(r.Groups[1].Value), Name = link.InnerText, Url = u.AbsoluteUri, Site = site };
                        conn.InsertOrReplaceWithChildren(b);

                    }
                }
            });
            locker.ExitWriteLock();

            WriteLine("OpenService()");
            p.OpenService(locker, conn);

            WriteLine("CommandThread()");
            p.CommandThread(p.source.Token, locker, conn);

            WriteLine($"PageToPostThread()");
            p.PageToPostThread(p.source.Token, locker, conn);

            WriteLine($"GetBlockThread(78)");
            p.GetBlockThread(p.source.Token, locker, conn, 78);


            Console.TreatControlCAsInput = true;

            //while (p.Exit != true)
            //{
            //    Console.WriteLine("Press Escape key to exit.");
            //    var input = Console.ReadKey();
            //    WriteLine($"input.Key:{input.Key.ToString()}");
            //    if (input.Key == ConsoleKey.Escape)
            //    {
            //        break;
            //    }
            //}
        }

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
        private SQLiteConnection InitDataBase(ReaderWriterLockSlim locker, string file)
        {
            bool flagCreateNewFile = false;
            SQLiteConnection conn;
            if (File.Exists(file) != true)
                flagCreateNewFile = true;

            locker.EnterWriteLock();
            conn = new SQLite.SQLiteConnection(file, false);
            if (flagCreateNewFile == true)
            {
                conn.CreateTable<Site>();
                conn.CreateTable<Cookies>();
                conn.CreateTable<Block>();
                conn.CreateTable<Page>();
                conn.CreateTable<Post>();
                conn.CreateTable<Video>();
            }


            if (conn.Table<Cookies>().Any() == false)
            {
                var c = new Cookies() { Name = "playno1", Value = "playno1Cookie" };
                conn.Insert(c);
                var s = new Site() { Name = "AVNO1", Url = @"http://www.playno1.com/", Cookies = new List<Cookies>() { c } };
                conn.InsertWithChildren(s);
            }
            locker.ExitWriteLock();

            return conn;
        }


        private void CommandThread(CancellationToken cancel, ReaderWriterLockSlim locker, SQLiteConnection conn)
        {
            var t = new Thread(new ThreadStart(() =>
            {

                while (true)
                {
                    if (cancel.IsCancellationRequested)
                        break;

                    var key = Console.ReadKey();
                    if (key.KeyChar == ' ')
                    {
                        Console.WriteLine("Press any key to continue...");
                        locker.EnterWriteLock();
                        Console.ReadKey();
                        locker.ExitWriteLock();
                    }
                    if (key.KeyChar == 'q' || key.KeyChar == 'Q')
                    {
                        Console.WriteLine("end.");
                        source.Cancel();
                        Exit = true;
                        break;

                    }

                }
            }));
            t.Start();
        }

        private void PageToPostThread(CancellationToken cancel, ReaderWriterLockSlim locker, SQLiteConnection conn)
        {
            var t = new Thread(new ThreadStart(() =>
            {
                while (true)
                {
                    if (cancel.IsCancellationRequested)
                        break;
                    locker.EnterReadLock();
                    var EmptyPageIds = conn.Table<Page>().Where(o => o.TranslateFlag == false).Select(o => o.Id).ToList();

                    locker.ExitReadLock();

                    if (EmptyPageIds.Any())
                    {
                        //WriteLine($"EmptyPageIds.Count:{EmptyPageIds.Count}");
                        Random random = new Random();
                        var PageIndex = random.Next(EmptyPageIds.Count);
                        //WriteLine($"PageIndex:{PageIndex}");
                        var PageId = EmptyPageIds.ElementAt(PageIndex);
                        //WriteLine($"PageId:{PageId}");
                        locker.EnterReadLock();
                        var Page = conn.GetWithChildren<Page>(PageId, true);
                        locker.ExitReadLock();
                        WriteLine($"Get page in ID:{PageId}/Count:{EmptyPageIds.Count}");
                        if (cancel.IsCancellationRequested)
                            break;
                        //WriteLine($"Page.TranslateFlag:{Page.TranslateFlag}");
                        //WriteLine($"Page.Content:{Page.Content}");
                        //Console.ReadKey();
                        //WriteLine("按任意键继续...");
                        HtmlDocument doc = new HtmlDocument();
                        doc.LoadHtml(Page.Content);
                        var div标题 = doc.DocumentNode.SelectSingleNode(@"//*[@id='ct']/div[1]/div[2]/div[1]/h1");
                        var div日期作者 = doc.DocumentNode.SelectSingleNode(@"//*[@id='ct']/div[1]/div[2]/div[1]/p");
                        //var div作者 = doc.DocmentNode.SelectSingleNode(@"//*[@id='ct']/div[1]/div[2]/div[1]/p/span");
                        var div内容 = doc.DocumentNode.SelectSingleNode(@"//*[@id='article_content']");
                        var r日期 = Regex.Match(div日期作者.InnerText, @"(\d{4}-\d{1,2}-\d{1,2})");
                        var r作者 = Regex.Match(div日期作者.InnerText, @"作者: (\S*)");
                        var imgs = doc.DocumentNode.Descendants("img").Where(o => o.Attributes["src"] != null && o.Attributes["file"] != null && o.Attributes["src"].Value == @"static/image/common/none.gif").ToList();
                        imgs.ForEach(o =>
                        {
                            o.Attributes["src"].Value = new Uri(new Uri(Page.Block.Url), o.Attributes["file"].Value).AbsoluteUri;
                        });


                        Post p = new Post()
                        {
                            标题 = div标题.InnerText,
                            日期 = r日期.Success ? DateTime.Parse(r日期.Groups[1].Value) : new DateTime(),
                            作者 = r作者.Success ? r作者.Groups[1].Value : null,
                            内容 = div内容.InnerHtml,
                            封面 = Page.Image
                        };
                        Page.Content = null;
                        Page.TranslateFlag = true;
                        locker.EnterWriteLock();
                        conn.Insert(p);
                        conn.Update(Page);
                        locker.ExitWriteLock();
                        WriteLine($"Save Post,Id:{p.Id} 标题:{p.标题} 日期:{p.日期} 作者:{p.作者} 封面:{p.封面}");



                        var str作品名 = "作品名：";
                        var str番号 = "品　番：";
                        var str发行日 = "發行日：";
                        var str女优名 = "女優名：";
                        var str事务所 = "事務所：";
                        var str身高罩杯 = "身高／罩杯：";

                        Video v = new Video();
                        var div = doc.DocumentNode.Descendants("p").Where(o => o.InnerText.StartsWith(str作品名)).FirstOrDefault();
                        v.作品名 = div != null ? div.InnerText.Substring(str作品名.Length) : null;

                        div = doc.DocumentNode.Descendants("p").Where(o => o.InnerText.StartsWith(str番号)).FirstOrDefault();
                        v.番号 = div != null ? div.InnerText.Substring(str番号.Length) : null;


                        div = doc.DocumentNode.Descendants("p").Where(o => o.InnerText.StartsWith(str发行日)).FirstOrDefault();
                        v.发行日 = div != null ? div.InnerText.Substring(str发行日.Length) : null;

                        div = doc.DocumentNode.Descendants("p").Where(o => o.InnerText.StartsWith(str女优名)).FirstOrDefault();
                        v.女优名 = div != null ? div.InnerText.Substring(str女优名.Length) : null;

                        div = doc.DocumentNode.Descendants("p").Where(o => o.InnerText.StartsWith(str事务所)).FirstOrDefault();
                        v.事务所 = div != null ? div.InnerText.Substring(str事务所.Length) : null;

                        div = doc.DocumentNode.Descendants("p").Where(o => o.InnerText.StartsWith(str身高罩杯)).FirstOrDefault();
                        v.身高罩杯 = div != null ? div.InnerText.Substring(str身高罩杯.Length) : null;

                        v.Post = p;

                        if (v.番号 != null)
                        {
                            locker.EnterWriteLock();
                            conn.InsertWithChildren(v);
                            locker.ExitWriteLock();
                            WriteLine($"Save Video,Id:{v.Id} 作品名:{v.作品名} 番号:{v.番号} 发行日:{v.发行日} 女优名:{v.女优名} 事务所:{v.事务所} 身高杯罩:{v.身高罩杯}");
                        }

                    }
                    else
                    {
                        WriteLine("已没有需要转换的页面，等待1分钟后继续尝试。");
                        Thread.Sleep(1000 * 60);
                    }
                }

            }));

            t.Start();

        }
        private void GetBlockThread(CancellationToken cancel, ReaderWriterLockSlim locker, SQLiteConnection conn, int BlockId, int Page = 1)
        {
            var mt = new Thread(new ThreadStart(() =>
            {
                locker.EnterReadLock();
                var block = conn.GetWithChildren<Block>(BlockId);
                locker.ExitReadLock();

                if (block != null)
                {

                    var href = $"portal.php?mod=list&catid={BlockId}&page={Page}";
                    var uri = new Uri(new Uri(block.Site.Url), href);
                    var url = HttpUtility.HtmlDecode(uri.AbsoluteUri);

                    while (true)
                    {
                        if (cancel.IsCancellationRequested)
                            break;

                        WriteLine($"getHtmlPage({url})");
                        var html = getHtmlPage(url);
                        if (html != null)
                        {
                            HtmlDocument doc = new HtmlDocument();
                            doc.LoadHtml(html);

                            var allLinks = doc.DocumentNode
                                 .Descendants("a")
                                 .Where(o => o.Attributes["href"] != null && o.Attributes["href"].Value.Contains("article-") && o.HasChildNodes)
                                 .ToList();

                            if (allLinks != null)
                            {
                                if (cancel.IsCancellationRequested)
                                    break;
                                #region 获取本页有效链接
                                List<Page> allPages = new List<Page>();

                                foreach (var link in allLinks)
                                {
                                    var url3 = link.Attributes["href"].Value;
                                    var r = Regex.Match(url3, @"article-(\d+)-(\d+)");
                                    var img = link.Descendants("img").Where(o => o.Attributes["src"] != null).Select(o => o.Attributes["src"].Value).FirstOrDefault();
                                    if (r.Success)
                                    {
                                        var PageId = int.Parse(r.Groups[1].Value);
                                        var PageNumber = int.Parse(r.Groups[2].Value);
                                        var ImgSrc = img;
                                        if (allPages.Where(o => o.Id == PageId).Any() == false)
                                            allPages.Add(new Page() { Id = PageId, Image = ImgSrc, Block = block });
                                    }

                                }
                                #endregion
                                if (cancel.IsCancellationRequested)
                                    break;
                                if (allPages.Count > 0)
                                {
                                    #region 有效链接转为下载任务，并等待任务完成
                                    List<Task<Page>> tasks = new List<Task<Page>>();
                                    foreach (var p in allPages)
                                    {
                                        var t = Task.Run(() =>
                                        {
                                            locker.EnterReadLock();
                                            var any = conn.Table<Page>().Where(o => o.Id == p.Id).Any();
                                            locker.ExitReadLock();

                                            if (any == false)
                                            {
                                                var PageNumber = 1;
                                                var relativeUrl = $"article-{p.Id}-{PageNumber}.html";
                                                var u = new Uri(new Uri(block.Site.Url), relativeUrl);

                                                WriteLine($"getHtmlPage({u.AbsoluteUri})");
                                                var page = new Page();
                                                page.Id = p.Id;
                                                page.Url = u.AbsoluteUri;
                                                page.Content = getHtmlPage(u.AbsoluteUri);
                                                page.Image = p.Image;
                                                page.Block = p.Block;
                                                page.TranslateFlag = false;
                                                if (page.Content == null)
                                                    return null;
                                                else
                                                    return page;
                                            }
                                            else
                                            {
                                                return null;
                                            }
                                        });
                                        tasks.Add(t);
                                    }
                                    Task.WaitAll(tasks.ToArray());
                                    #endregion

                                    #region 将下载内容放入保存线程，并执行线程
                                    allPages = tasks.Where(o => o.Result != null).Select(o => o.Result).ToList();
                                    if (allPages.Count > 0)
                                    {
                                        Thread thread = new Thread(new ThreadStart(() =>
                                                                 {
                                                                     locker.EnterWriteLock();
                                                                     conn.InsertOrReplaceAllWithChildren(allPages);
                                                                     WriteLine($"保存Pages数据共{allPages.Count}条.");
                                                                     allPages.ForEach(o => { Write($"{o.Id} "); });
                                                                     Write("\n");

                                                                     locker.ExitWriteLock();
                                                                 }));
                                        thread.Start();

                                    }
                                    else
                                    {
                                        // 该页没有需要保存的新贴
                                        //continue;
                                        WriteLine("该页没有需要保存的新贴，将Page置1重新下载。等待60分钟。");
                                        Page = 1;
                                        href = $"portal.php?mod=list&catid={BlockId}&page={Page}";
                                        uri = new Uri(new Uri(block.Site.Url), href);
                                        url = HttpUtility.HtmlDecode(uri.AbsoluteUri);
                                        Thread.Sleep(1000 * 60 * 60);
                                        continue;
                                    }
                                    #endregion
                                }
                                else
                                {
                                    // 该页没有新贴
                                    //continue;
                                    WriteLine("该页没有新贴，将Page置1重新下载。等待60分钟。");
                                    Page = 1;
                                    href = $"portal.php?mod=list&catid={BlockId}&page={Page}";
                                    uri = new Uri(new Uri(block.Site.Url), href);
                                    url = HttpUtility.HtmlDecode(uri.AbsoluteUri);
                                    Thread.Sleep(1000 * 60 * 60);
                                    continue;

                                }
                            }


                            #region 查找并保存最大页码
                            var blockLinks = doc.DocumentNode
                                .Descendants("a")
                                .Where(o => o.Attributes["href"] != null && o.Attributes["href"].Value.Contains("portal.php?"))
                                .ToList();
                            if (blockLinks != null)
                            {

                                foreach (var link in blockLinks)
                                {
                                    var uri2 = new Uri(new Uri(block.Site.Url), link.Attributes["href"].Value);
                                    var url2 = HttpUtility.HtmlDecode(uri2.AbsoluteUri);
                                    var r = Regex.Match(url2, @"page=(\d+)");
                                    if (r.Success)
                                    {
                                        var curPage = int.Parse(r.Groups[1].Value);
                                        if (curPage > block.MaxPage)
                                        {
                                            block.MaxPage = curPage;
                                        }

                                    }
                                }
                                locker.EnterWriteLock();
                                conn.Update(block);
                                locker.ExitWriteLock();
                            }
                            #endregion

                            #region 保持当前页码
                            locker.EnterWriteLock();
                            block.CurrentPage = Page;
                            conn.Update(block);
                            locker.ExitWriteLock();
                            #endregion

                            #region 获取下一页码
                            var nextLink = doc.DocumentNode
                           .Descendants("a")
                           .Where(o => o.Attributes["class"] != null && o.Attributes["class"].Value == "nxt")
                           .FirstOrDefault();
                            if (nextLink != null)
                            {
                                uri = new Uri(new Uri(block.Site.Url), nextLink.Attributes["href"].Value);
                                url = HttpUtility.HtmlDecode(uri.AbsoluteUri);
                                var r = Regex.Match(url, @"page=(\d+)");
                                if (r.Success)
                                {
                                    Page = int.Parse(r.Groups[1].Value);
                                }
                            }
                            else
                            {

                                #region 已没有下一页（已完成最大页），将Page置1重新下载。等待60分钟再执行。
                                WriteLine("已没有下一页，将Page置1重新下载。等待60分钟。");
                                Page = 1;
                                href = $"portal.php?mod=list&catid={BlockId}&page={Page}";
                                uri = new Uri(new Uri(block.Site.Url), href);
                                url = HttpUtility.HtmlDecode(uri.AbsoluteUri);
                                Thread.Sleep(1000 * 60 * 60);

                                #endregion
                            }
                            #endregion


                        }
                        else
                        {
                            #region 下载板块错误，等待5秒继续。
                            //break;
                            WriteLine($"getHtmlPage({url}) fail,sleep 5 seconds to continue...");
                            Thread.Sleep(5000);
                            continue;
                            #endregion
                        }
                        WriteLine($"getHtmlPage() Done. Sleep 2s to continue.");
                        Thread.Sleep(1000 * 2);

                    }
                    WriteLine($"GetBlockThread() Done.");
                }
            }));

            mt.Start();

        }

        private string getHtmlPage(string url)
        {
            string strGetHtml = null;
            var httpreq = (HttpWebRequest)WebRequest.Create(url);
            httpreq.CookieContainer = cookies;
            httpreq.Method = "GET";
            try
            {
                var resp = httpreq.GetResponse();
                StreamReader sr = new StreamReader(resp.GetResponseStream());
                strGetHtml = sr.ReadToEnd();
                resp.Close();
            }
            catch
            {
                strGetHtml = null;
            }

            return strGetHtml;
        }
        private void OpenService(ReaderWriterLockSlim locker, SQLiteConnection conn)
        {
            WebServiceInterfaceRESTful myServices = new WebServiceInterfaceRESTful(locker, conn);
            _serviceHost = new WebServiceHost(myServices, new Uri("http://localhost:8010"));
            try
            {
                _serviceHost.Open();

                WriteLine("http://localhost:8010 Service is running");

            }
            catch (CommunicationException cex)
            {
                WriteLine($"http://localhost:8010 exception occurred: {cex.Message}");
                _serviceHost.Abort();
            }
        }
    }
}
