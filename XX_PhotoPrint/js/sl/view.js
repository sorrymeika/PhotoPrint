define(function(require,exports,module) {

    var $=require('$'),
        sl=require('./base'),
        tmpl=require('./tmpl');

    /*
    var Class=function() {
    var args=Array.prototype.slice.call(arguments);

    this.options=$.extend({},this.options,args.shift());
    this.initialize.apply(this,args);
    };

    Class.fn=Class.prototype={
    options: {},
    initialize: function() { }
    };
    */

    var View=function() {
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

        that.options=$.extend({},that.options,options);

        that.el=that.$el[0];

        that.listen(that.events);
        that.listen(that.options.events);

        that.initialize.apply(that,args);
    };

    View.fn=View.prototype={
        $el: null,
        template: '',
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
    };

    View.extend=function(prop) {
        var that=this;

        var childClass=function() {
            that.apply(this,arguments);
        };

        var F=function() { };
        F.prototype=that.prototype;

        childClass.fn=childClass.prototype=new F();

        prop.options=$.extend({},that.prototype.options,prop.options);
        prop.events=$.extend({},that.prototype.events,prop.events);

        for(var key in prop) {
            childClass.fn[key]=prop[key];
        }

        childClass.prototype.superClass=that.prototype;
        childClass.prototype.constructor=childClass;

        childClass.extend=arguments.callee;

        return childClass;
    };

    sl.View=View;

    module.exports=View;
});