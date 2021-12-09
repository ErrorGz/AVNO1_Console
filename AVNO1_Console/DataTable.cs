using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;
using SQLiteNetExtensions;
using SQLiteNetExtensions.Attributes;

namespace AVNO1_Console
{
    public class Video
    {
        [AutoIncrement, PrimaryKey] public int Id { get; set; }
        public string 作品名 { get; set; }
        public string 番号 { get; set; }

        public string 发行日 { get; set; }
        public string 女优名 { get; set; }
        public string 前女优名 { get; set; }
        public string 事务所 { get; set; }
        public string 身高罩杯 { get; set; }

        [ForeignKey(typeof(Post))] public int PostId { get; set; }
        [OneToOne] public Post Post { get; set; }

    }
    public class Post
    {
        [AutoIncrement, PrimaryKey] public int Id { get; set; }
        public string 标题 { get; set; }
        public DateTime 日期 { get; set; }
        public string 作者 { get; set; }

        public string 内容 { get; set; }

        public string 封面 { get; set; }
        [OneToOne] public Video Video { get; set; }

    }
    public class Site
    {
        [AutoIncrement, PrimaryKey] public int Id { get; set; }
        public string Name { get; set; }

        public string Url { get; set; }

        [OneToMany] public List<Block> Block { get; set; }

        [OneToMany] public List<Cookies> Cookies { get; set; }


    }


    public class Cookies
    {
        [AutoIncrement, PrimaryKey] public int Id { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }

        [ForeignKey(typeof(Site))] public int SiteId { get; set; }
        [ManyToOne] public Site Site { get; set; }
    }
    public class Block
    {
        [PrimaryKey] public int Id { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        [ForeignKey(typeof(Site))] public int SiteId { get; set; }
        [ManyToOne] public Site Site { get; set; }

        public int MaxPage { get; set; }
        public int CurrentPage { get; set; }

    }


    public class Page
    {
        [PrimaryKey] public int Id { get; set; }
        [ForeignKey(typeof(Block))] public int BlockId { get; set; }
        [ManyToOne] public Block Block { get; set; }

        public string Url { get; set; }
        public string Content { get; set; }
        public string Image { get; set; }
        [Indexed] public bool TranslateFlag { get; set; }

    }
}
