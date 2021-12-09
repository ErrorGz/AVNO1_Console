var DebugFlag = true;

if (DebugFlag == false)
    var JavLibUrl = "http://192.168.31.92:8010";
else
    var JavLibUrl = "http://localhost:8010";


mui.init();

var cur = 1;
var app = new Vue({
    el: '#app',
    data: {
        listPost: [],
        msg: '',
    },
    mounted: function () {
        $("html,body").animate({
            scrollTop: 0
        }, "slow");
        getLastPostJson();
    },
    methods: {
        clickSearch(event) {
            if (event)
                event.preventDefault();
            if (TextBoxSearch.value == "") {
                getLastPostJson();
            }
            else {
                RangePage.value = cur = 1;
                this.$data.listPost = [];
                getSearchPostJson(TextBoxSearch.value);
                $("html,body").animate({
                    scrollTop: 0
                }, "slow");
            }


   
       
         
        },
    }
});

function OnPageChange() {
    if (TextBoxSearch.value == "") {
        getLastPostJson();
    }
    else {
        getSearchPostJson(TextBoxSearch.value);
    }
}

function getSearchPostJson(searchText) {
    cur = RangePage.value;
    $.getJSON(JavLibUrl + "/GetSearchPost/" + cur.toString() + "-1" + "/" + searchText, function (data, status, xhr) {
        if (status == "success") {
            data.Posts.forEach(o => {
                app.$data.listPost.push(o);
            });
            RangePage.max = data.Count;
            cur++;
            RangePage.value = cur;
        }
    });
}

function getLastPostJson() {
    cur = RangePage.value;
    $.getJSON(JavLibUrl + "/GetLastPost/" + cur.toString() + "-1", function (data, status, xhr) {
        if (status == "success") {

            data.Posts.forEach(o => {
                app.$data.listPost.push(o);
            });
            RangePage.max = data.Count;
            cur++;
            RangePage.value = cur;
        }
    });
}



function thumbImg(obj, method) {
    if (!obj) {
        return;
    }
    obj.onload = null;
    file = obj.src;
    zw = obj.offsetWidth;
    zh = obj.offsetHeight;
    if (zw < 2) {
        if (!obj.id) {
            obj.id = 'img_' + Math.random();
        }
        setTimeout("thumbImg($('" + obj.id + "'), " + method + ")", 100);
        return;
    }
    zr = zw / zh;
    method = !method ? 0 : 1;
    if (method) {
        fixw = obj.getAttribute('_width');
        fixh = obj.getAttribute('_height');
        if (zw > fixw) {
            zw = fixw;
            zh = zw / zr;
        }
        if (zh > fixh) {
            zh = fixh;
            zw = zh * zr;
        }
    } else {
        fixw = typeof imagemaxwidth == 'undefined' ? '600' : imagemaxwidth;
        if (zw > fixw) {
            zw = fixw;
            zh = zw / zr;
            obj.style.cursor = 'pointer';
            if (!obj.onclick) {
                obj.onclick = function () {
                    zoom(obj, obj.src);
                }
                    ;
            }
        }
    }
    obj.width = zw;
    obj.height = zh;
}



LoadingTimer = null;
Loadingflag = false;
topDistance = $(window).height() / 2; //goToTop距浏览器顶端的距离，这个距离可以根据你的需求修改
showDistance = 1; //距离浏览器顶端多少距离开始显示goToTop按钮，这个距离也可以修改，但不能超过浏览器默认宽度，为了兼容不同分辨率的浏览器，我建议在这里设置值为1；
$(window).scroll(function () {
    //当时滚动条离底部60px时开始加载下一页的内容
    //console.log($(window).height() + "  " + $(window).scrollTop() + "  " + topDistance + "  " + $(document).height());
    if (($(window).height() + $(window).scrollTop() + topDistance) >= $(document).height()) {

        if (Loadingflag == false) {

            Loadingflag = true;
            if (TextBoxSearch.value == "") {
                getLastPostJson();
            }
            else {
                getSearchPostJson(TextBoxSearch.value);
            }

            clearTimeout(LoadingTimer);
            LoadingTimer = setTimeout(function () {
                Loadingflag = false;
            }, 2000);
        }
    }

    var scrollTop = $(document).scrollTop();
    var scrollLeft = $(document).scrollLeft();



});