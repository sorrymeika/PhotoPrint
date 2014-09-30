﻿define(function (require,exports,module) {
    var $=require('jquery'),
        util=require('lib/util'),
        format=util.format,
        bind=util.bind;

    function Page(options) {
        this.options=$.extend({
            id: '',
            page: 1,
            pageSize: 10,
            total: 0,
            onChange: function (page) { },
            msg: '共{total}条数据&nbsp;&nbsp;每页{pageSize}&nbsp;&nbsp;当前第{page}/{totalPages}页'
        },options);

        this.container=$(options.id);
        this.draw();
    };

    Page.prototype={
        draw: function () {
            var options=this.options,
            container=this.container,
            pageSize=options.pageSize||10;

            options.pageSize=pageSize;
            var totalPages=options.totalPages=(options.total-(options.total%pageSize||pageSize))/pageSize+1;

            if(totalPages>0) {
                container.html(format(options.msg,options));
                container=$('<div></div>').appendTo(container);

                var html='',
                page=parseInt(options.page),
                total=options.total,
                pageSize=options.pageSize,
                onChange=options.onChange,
                oneButton=bind(this._oneButton,this),
                serialButtons=bind(this._serialButtons,this);

                if(page==1) html+="<span>第一页</span><span>上一页</span>";
                else html+=oneButton(1,"第一页")+oneButton(page-1,"上一页");

                if(totalPages<=7)
                    html+=serialButtons(1,totalPages);
                else {
                    if(page<5) {
                        html+=serialButtons(1,5)+'&nbsp;...&nbsp;';
                    } else {
                        html+=serialButtons(1,2)
                        +'&nbsp;...&nbsp;'
                        +(page+3>=totalPages?
                            serialButtons(totalPages-4,totalPages-1):
                            (serialButtons(page-1,page+2)+'&nbsp;...&nbsp;'));
                    }
                    html+=serialButtons(totalPages,totalPages);
                }

                html+=totalPages==page?'<span>下一页</span><span>最后一页</span>':(oneButton(page+1,"下一页")+oneButton(totalPages,"最后一页"));

                container.html(html)
                .find("a")
                .click(function () {
                    onChange(this.getAttribute("page"));
                });

            } else
                container.html('共0条数据');
        },
        change: function (opt) {
            if(typeof opt=='number')
                this.options.page=page;
            else
                $.extend(this.options,opt);
            this.draw();
        },
        clear: function () {
            this.container.html('&nbsp;');
        },
        _oneButton: function (index,text) {
            text=text||index;
            return format('<a href="javascript:void(0);" page="{0}">{1}</a>',[index,text]);
        },
        _serialButtons: function (startPage,endPage) {
            var currentPage=this.options.page,
            oneButton=this._oneButton,
            html='';
            for(var i=startPage;i<=endPage;i++) {
                html+=(i==currentPage)?'<span>'+i+'</span>':oneButton(i);
            }
            return html;
        }
    }

    $.fn.extend({
        page: function (options) {
            options.id=this;
            return new Page(options);
        }
    });

    module.exports=Page;
});