define(function(require,exports,module) {
    var $=require('jquery'),
        _guid=0,
        doc=document;

    var util={
        guid: function() {
            return ++_guid;
        },
        stringify: function(o) {
            var r=[];
            if(typeof o=="string") return quote(o);
            if(typeof o=="undefined") return "undefined";
            if(typeof o=="object") {
                if(o===null) return "null";
                else if(Object.prototype.toString.call(o)!="[object Array]") {
                    for(var i in o)
                        r.push(arguments.callee(i)+":"+arguments.callee(o[i]));
                    r="{"+r.join()+"}";
                } else {
                    for(var i=0;i<o.length;i++)
                        r.push(arguments.callee(o[i]));
                    r="["+r.join()+"]";
                }
                return r;
            }
            return o.toString();

            function quote(string) {
                var escapable=/[\\\"\x00-\x1f\x7f-\x9f\u00ad\u0600-\u0604\u070f\u17b4\u17b5\u200c-\u200f\u2028-\u202f\u2060-\u206f\ufeff\ufff0-\uffff]/g;
                var meta={
                    '\b': '\\b',
                    '\t': '\\t',
                    '\n': '\\n',
                    '\f': '\\f',
                    '\r': '\\r',
                    '"': '\\"',
                    '\\': '\\\\'
                };
                escapable.lastIndex=0;
                return escapable.test(string)?
                    '"'+string.replace(escapable,function(a) {
                        var c=meta[a];
                        return typeof c==='string'?c:
                            '\\u'+('0000'+a.charCodeAt(0).toString(16)).slice(-4);
                    })+'"':
                '"'+string+'"';
            }
        },
        parse: function(str) {
            return (new Function("return "+str))()
        },
        bind: function() {
            var slice=Array.prototype.slice,
                args=slice.call(arguments),
                fn=args.shift(),
                object=args.shift();
            return function() {
                return fn.apply(object,args.concat(slice.call(arguments)));
            }
        },
        pad: function(num,n) {
            var a='0000000000000000'+num;
            return a.substr(a.length-(n||2));
        },
        sum: function(arr,f) {
            var res=0;
            if(f) for(var i=0,n=arr.length;i<n;i++) res+=f(arr[i],i,n);
            else for(var i=0,n=arr.length;i<n;i++) res+=arr[i];
            return res;
        },
        formatDate: function(d,f) {
            var y=d.getFullYear()+"",M=d.getMonth()+1,D=d.getDate(),H=d.getHours(),m=d.getMinutes(),s=d.getSeconds(),mill=d.getMilliseconds()+"0000",pad=this.pad;
            return (f||'yyyy-MM-dd HH:mm:ss').replace(/\y{4}/,y)
                .replace(/y{2}/,y.substr(2,2))
                .replace(/M{2}/,pad(M))
                .replace(/M/,M)
                .replace(/d{2,}/,pad(D))
                .replace(/d/,d)
                .replace(/H{2,}/i,pad(H))
                .replace(/H/i,H)
                .replace(/m{2,}/,pad(m))
                .replace(/m/,m)
                .replace(/s{2,}/,pad(s))
                .replace(/s/,s)
                .replace(/f+/,function(w) {
                    return mill.substr(0,w.length)
                })
        },
        addStyle: function(css) {
            var style=doc.createElement("style");
            style.type="text/css";
            try {
                style.appendChild(doc.createTextNode(css));
            } catch(ex) {
                style.styleSheet.cssText=css;
            }
            var head=doc.getElementsByTagName("head")[0];
            head.appendChild(style);
        },
        submit: function(form,url,fn) {
            var me=$(form),
                blankFn=function() { };

            var settings=url&&typeof url==="object"?$.extend({
                url: me.attr('action'),
                validate: function() {
                    return true;
                },
                beforeSend: blankFn,
                success: blankFn,
                error: blankFn
            },url):{
                url: $.isFunction(url)&&me.attr('action')||url,
                success: fn||!fn&&$.isFunction(url)&&url
            };

            if(settings.validate&&!settings.validate()) {
                settings.error&&settings.error.call(me,'表单验证失败');
            } else {
                settings.beforeSend&&settings.beforeSend.call(me);
                if(me.has('[type="file"]').length>0) {
                    me.attr('action',settings.url);

                    util.submitForm(me,function(data) {
                        settings.success&&settings.success.call(me,data)
                    });
                } else {
                    $.ajax({
                        url: settings.url,
                        type: 'POST',
                        dataType: 'json',
                        data: me.serialize(),
                        success: function(data) {
                            settings.success&&settings.success.call(me,data)
                        },
                        error: function(xhr) {
                            settings.error&&settings.error.call(me,xhr);
                        }
                    });
                }
            }
        },
        submitForm: function(form,callback) {
            var $form=$(form),
                $callback=$form.find('[name="callback"]'),
                guid=this.guid(),
                target="_submit_iframe"+guid,
                eventName="_submitForm_"+guid,
                $iframe=$('<iframe style="top:-999px;left:-999px;position:absolute;display:none;" frameborder="0" width="0" height="0" name="'+target+'"></iframe>');

            var timeout=setTimeout(function() {
                timeout=false;
                window[eventName]({ success: false,msg: '登录超时' });

            },15000);

            window[eventName]=function(data) {
                if(timeout) clearTimeout(timeout);

                callback.call($form,data);
                $iframe.remove();
                delete window[eventName];
            };

            $(doc.body).append($iframe);
            if(!$callback.length)
                $callback=$('<input type="hidden" name="callback" />').appendTo($form);

            $callback.val(eventName)
            $form.attr("target",target)
                .submit();

        },
        createOptions: function(args,options) {
            var length=args.length;
            if(length==1&&$.isPlainObject(args[0])) {
                $.extend(options,args[0]);
            }
            else {
                var arr=[];
                $.each(options,function(j,opt) {
                    for(var i=0,arg;i<length;i++) {
                        if($.inArray(i,arr)>=0) break;
                        arg=args[i];
                        if(typeof arg==typeof opt) {
                            options[j]=arg;
                            arr.push(i);
                            break;
                        }
                    }
                });
            }
            return options;
        },
        getCookie: function(name) {
            var res=doc.cookie.match(new RegExp("(^| )"+name+"=([^;]*)(;|$)"));
            if(res!=null)
                return unescape(res[2]);
            return null;
        },
        setCookie: function(a,b,c,p) {
            if(c) {
                var d=new Date();
                d.setTime(d.getTime()+c*24*60*60*1000);
                c=";expires="+d.toGMTString();
            }
            doc.cookie=a+"="+escape(b)+(c||"")+";path="+(p||'/')
        },
        removeCookie: function(name) {
            var v=this.get(name);
            if(v!=null)
                this.set(name,v,-1);
        },
        getQueryString: function(name) {
            var result=location.search.match(new RegExp("[\?\&]"+name+"=([^\&]+)","i"));
            return (result==null||result.length<1)?null:result[1];
        },
        offsetParent: function(el) {
            var parent=el.parent(),
            position;
            while(parent.length!=0&&parent[0].tagName.toLowerCase()!="body") {
                if($.inArray(parent.css('position'),['fixed','absolute','relative']))
                    return parent;
                parent=parent.parent();
            }
            return parent;
        },
        query: function(name) {
            if(/^\?/.test(name)) {
                name=name.substr(1);
                var result=location.search.match(new RegExp("[\?\&]"+name+"=([^\&]+)","i"));
                return (result==null||result.length<1)?null:result[1];
            }
        },
        cookie: function(a,b,c,p) {
            if(typeof b==='undefined') {
                var res=document.cookie.match(new RegExp("(^| )"+a+"=([^;]*)(;|$)"));
                if(res!=null)
                    return unescape(res[2]);
                return null;
            } else {
                if(typeof b===null) {
                    b=this.cookie(name);
                    if(b!=null) c= -1;
                    else return;
                }
                if(c) {
                    var d=new Date();
                    d.setTime(d.getTime()+c*24*60*60*1000);
                    c=";expires="+d.toGMTString();
                }
                document.cookie=a+"="+escape(b)+(c||"")+";path="+(p||'/')
            }
        },
        encodeHTML: function(text) {
            return (""+text).split("<").join("&lt;").split(">").join("&gt;").split('"').join("&#34;").split("'").join("&#39;");
        },
        isIE6: /MSIE 6/.test(navigator.userAgent)
    };

    util.template=util.format=function(t,obj) {
        return t.replace(/\{([^}]+)\}/g,function(g0,key) {
            var $data=obj[key];
            return $data;
        });
    };

    util.tab=function(tabs,contents) {
        tabs=$(tabs);
        contents=$(contents);
        var index=0;

        tabs.click(function() {
            var newIndex=tabs.index(this);
            if(newIndex!=index) {
                tabs.eq(newIndex).addClass("curr");
                tabs.eq(index).removeClass("curr");

                contents.eq(newIndex).fadeIn(500);
                contents.eq(index).hide();

                index=newIndex;
            }
        });
    };

    module.exports=util;
});