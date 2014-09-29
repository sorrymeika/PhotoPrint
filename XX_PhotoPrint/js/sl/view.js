define(function(require,exports,module) {

    var $=require('$'),
        sl=require('./base'),
        tmpl=require('./tmpl');

    var View=sl.Class.extend(function() {
        var that=this,
            options,
            args=Array.prototype.slice.call(arguments),
            selector=args.shift();

        if(typeof selector!=='undefined'&&!$.isPlainObject(selector)) {

            that.$el=$(selector);
            options=args.shift();

        } else if(!that.$el) {
            that.$el=$(that.el);
            options=selector;
        }

        if(options&&options.override) {
            var overrideFn;
            $.each(options.override,function(key,fn) {
                overrideFn=that[key];
                (typeof overrideFn!='undefined')&&(that.sealed[key]=overrideFn,fn.sealed=overrideFn);
                that[key]=fn;
            });
            delete options.override;
        }

        that.options=$.extend({},that.options,options);

        that.el=that.$el[0];

        that.listen(that.events);
        that.listen(that.options.events);

        that.initialize.apply(that,args);
        that.options.initialize&&that.options.initialize.apply(that,args);

        that.on('Destory',that.onDestory);

    },{
        $el: null,
        template: '',
        sealed: {},
        options: {},
        events: null,
        _bindDelegateAttrs: [],
        _bindAttrs: [],
        _bindListenTo: [],
        _bind: function(el,name,f) {
            this._bindDelegateAttrs.push([el,name,f]);
            this.$el.delegate(el,name,$.proxy(f,this));

            return this;
        },
        _listenEvents: function(events) {
            var that=this;

            events&&$.each(events,function(evt,f) {
                that.listen(evt,f);
            });
        },
        listen: function(evt,f) {
            var that=this;

            if(!f) {
                that._listenEvents(evt);
            }
            else {
                var arr=evt.split(' '),
                    events=arr.shift();

                events=events.replace(/,/g,' ');

                f=$.isFunction(f)?f:that[f];

                if(arr.length>0&&arr[0]!=='') {
                    that._bind(arr.join(' '),events,f);
                } else {
                    that.bind(events,f);
                }
            }

            return that;
        },

        listenTo: function($target,name,f) {
            this._bindListenTo.push([$target,name,f]);
            $($target).on(name,$.proxy(f,this));

            return this;
        },

        on: function(selector,evt,handler) {
            if(handler) {
                this._bind(selector,evt,handler);
            } else {
                this.bind(selector,evt);
            }
            return this;
        },
        $: function(selector) {
            return $(selector,this.$el);
        },
        one: function(name,f) {
            this.$el.one(name,$.proxy(f,this));
            return this;
        },
        bind: function(name,f) {
            this._bindAttrs.push([name,f]);
            this.$el.bind(name,$.proxy(f,this));
            return this;
        },
        unbind: function(name,f) {
            var that=this,
                $el=that.$el;

            for(var i=that._bindAttrs.length-1,attrs;i>=0;i--) {
                attrs=that._bindAttrs[i];

                if(attrs[0]==name&&(typeof f==='undefined'||f===attrs[1])) {
                    $el.unbind.apply($el,attrs);
                    that._bindAttrs.splice(i,1);
                }
            }

            return this;
        },
        trigger: function() {
            var args=slice.call(arguments),
                name=args.shift();

            this.$el.triggerHandler(name,args);
            return this;
        },
        initialize: function() {
        },

        onDestory: function() { },

        destory: function() {
            var $el=this.$el,
                that=this;

            $.each(this._bindDelegateAttrs,function(i,attrs) {
                $.fn.undelegate.apply($el,attrs);
            });

            $.each(this._bindListenTo,function(i,attrs) {
                $.fn.off.apply(attrs);
            });

            that.one('Destory',function() {
                $.each(that._bindAttrs,function(i,attrs) {
                    $.fn.unbind.apply($el,attrs);
                });
            });

            that.trigger('Destory');
        }
    });

    View.extend=function(childClass,prop) {
        var that=this;

        childClass=sl.Class.extend.call(that,childClass,prop);

        childClass.events=$.extend({},childClass.fn.superClass.events,childClass.prototype.events);

        childClass.extend=arguments.callee;
        childClass.plugin=function(plugin) {
            that.plugin.call(childClass,plugin);
        };

        return childClass;
    };

    View.plugin=function(plugin) {
        var that=this,
            prototype=this.prototype;

        if(plugin.events) {
            $.extend(prototype.events,plugin.events);
            delete plugin.events;
        }

        if(plugin.override) {
            var overrideFn;
            $.each(plugin.override,function(key,fn) {
                overrideFn=prototype[key];
                (typeof overrideFn!='undefined')&&(prototype.sealed[key]=overrideFn,fn.sealed=overrideFn);
                prototype[key]=fn;
            });
            delete plugin.override;
        }

        $.each(plugin,function(key,fn) {
            var proto=prototype[key];

            if(typeof proto==='undefined') {
                prototype[key]=fn;

            } else if($.isFunction(proto)&&$.isFunction(fn)) {
                prototype[key]=function() {
                    proto.apply(this,arguments);
                    return fn.apply(this,arguments);
                };

            } else if($.isPlainObject(proto)&&$.isPlainObject(fn)) {
                prototype[key]=$.extend({},fn,prototype[key]);

            } else
                prototype[key]=fn;

        });
    };

    sl.View=View;

    module.exports=View;
});