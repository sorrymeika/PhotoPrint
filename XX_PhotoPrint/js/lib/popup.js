define(function (require,exports,module) {
    var $=require('jquery'),
        util=require('lib/util'),
        drag=require('lib/drag'),
        tmpl=require('lib/tmpl'),
        isIE6=util.isIE6,
        win=$(window),
        doc=$(document);

    var maskTmpl='<div class="J_Mask" style="opacity:.3;filter:alpha(opacity=30);position:absolute;z-index:99;top:0px;left:0px;background:#000;"></div>';

    if(isIE6)
        maskTmpl+='<iframe class="J_Mask" style="opacity:0;filter:alpha(opacity=0);position:absolute;z-index:98;top:0px;left:0px;" frameborder="0" scrolling="no"></iframe>';

    var Mask={
        maskTmpl: maskTmpl,
        resize: function () {
            var me=this;
            me._mask&&me._mask.css({
                width: doc.outerWidth(),
                height: doc.outerHeight()
            });
        },
        _visible: false,
        show: function () {
            var me=this;
            if(me._visible) return;
            me._visible=true;

            if(!me._mask) {
                var mask=$('.J_Mask');
                me._mask=mask.length?mask:$(me.maskTmpl).appendTo(document.body);
            }
            win.on('resize',$.proxy(me.resize,me));

            me._mask.css({
                width: doc.outerWidth(),
                height: doc.outerHeight()
            }).fadeIn();
        },
        hide: function () {
            var me=this;
            me._mask&&me._mask.hide();
            win.off('resize',me.resize);
            me._visible=false;
        }
    };

    function Dialog(options) {
        var me=this;

        if(options||!me._options)
            me._options=$.extend({
                title: '',
                width: 350,
                height: null,
                content: '',
                onContentLoad: null
            },options);

        me._init();
    };

    Dialog.prototype={
        _init: function () {
            var me=this,
                options=me._options,
                width=options.width,
                templa='<div class="dialog" style="z-index:100;display:none"><div class="dialog_hd"><i class="dialog_close">X</i></div><div class="dialog_bd"></div></div>',
                container=$(templa).css({
                    width: width
                }).appendTo(document.body),
                title=container.find('.dialog_hd').append('<span>'+options.title+'</span>'),
                content=container.find('.dialog_bd');

            container.addClass('dialog_fixed');
            drag(title,container);

            content.append(options.content).children().show();
            title.find('.dialog_close').click(function () { me.hide(); });

            me._title=title.find('span');
            me.container=container;
            me._content=content;

            options.height&&content.css({ overflowY: 'auto',height: options.height });

            onContentLoad=options.onContentLoad;
            if(onContentLoad) onContentLoad.call(this);
        },
        find: function (selector) {
            return this._content.find(selector);
        },
        content: function (content) {
            var me=this;
            me._content.html('').append(content);

            return me;
        },
        center: function () {
            var me=this;
            var container=me.container;
            if(container.css('display')!='none') {
                if(me._centerTimer) clearTimeout(me._centerTimer);
                me._centerTimer=setTimeout(function () {
                    container.animate({
                        left: (win.width()-container.outerWidth())/2,
                        top: Math.max(0,(win.height()-container.outerHeight())/2),
                        opacity: 1
                    },300,function () {
                        me._centerTimer=null;
                    });
                },200);
            }
            return me;
        },
        show: function () {
            var me=this,
                container=this.container;

            win.on('resize',$.proxy(me.center,me));

            Mask.show();
            container.css({
                top: -9999,
                left: -9999,
                display: 'block',
                opacity: 0
            }).css({
                left: (win.width()-container.outerWidth())/2,
                top: win.height()/2-container.height()
            });

            me.center();

            return me;
        },
        hide: function () {
            var me=this;

            win.off('resize',me.center);
            me.container.fadeOut(200);
            Mask.hide();

            return me;
        },
        title: function (title) {
            this._title.html(title);
        }
    };

    var Form=require('lib/form');

    function FormDialog(options) {
        var me=this;

        me._options=$.extend({},options);

        Dialog.call(me);

        delete me._options.title;
        Form.call(me,me._content);
    };

    $.extend(FormDialog.prototype,Form.prototype,Dialog.prototype,{
        constructor: FormDialog
    });

    function IFrame(options) {
        var defaults={
            title: '',
            width: 450,
            height: 350,
            url: ''
        };
        Dialog.call(this,$.extend(defaults,options));
    };

    $.extend(IFrame.prototype,Dialog.prototype);

    var iframeGuid=0;
    IFrame.prototype._init=function (options) {
        this.frameName='__popup_'+iframeGuid;
        var iframe=$(tmpl('<iframe frameborder="0" name="'+this.frameName+'" scrolling="no" src="${url}" width="${width}" height="${height}"></iframe>',options));
        this.frame=iframe;
        options.content=iframe;
        options.width+=20;
        Dialog.prototype._init.call(this,options);
    };

    IFrame.prototype.constructor=IFrame;
    IFrame.prototype.load=function (url) {
        window.open(url,this.frameName);
        return this;
    };
    IFrame.prototype.resize=function (width,height) {
        var that=this,
            container=this.container,
            frame=this.frame;

        this.frame.animate({
            width: width,
            height: height
        },{
            duration: 400,
            step: function (now,fx) {
                container.css({
                    width: parseInt(frame.width())+20
                }).css({
                    left: (win.width()-container.outerWidth())/2,
                    top: (win.height()-container.outerHeight())/2
                });
            }
        });
        return this;
    };

    module.exports={
        Dialog: Dialog,
        IFrame: IFrame,
        Form: FormDialog
    };
});