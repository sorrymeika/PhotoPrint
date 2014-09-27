define(function(require,exports,module) {

    var Class=function() {
        var that=this,
            args=Array.prototype.slice.call(arguments),
            options=args.shift();

        if(options) {
            for(var i in options) {
                that.options[i]=options[i];
            }
        }
        that.initialize.apply(that,args);
        that.options.initialize&&that.options.initialize.apply(that,args);
    };

    Class.fn=Class.prototype={
        options: {},
        initialize: function() { }
    };

    Class.extend=function(childClass,prop) {
        var that=this;

        childClass=typeof childClass=='function'?childClass:(prop=childClass,function() {
            that.apply(this,arguments);
        });

        var F=function() { };
        F.prototype=that.prototype;

        childClass.fn=childClass.prototype=new F();

        var options=that.fn.options;
        if(!prop.options) prop.options={};
        for(var i in options) {
            prop.options[i]=options[i];
        }

        for(var i in prop) {
            childClass.prototype[i]=prop[i];
        }

        childClass.fn.superClass=that.fn;
        childClass.fn.superConstructor=that;
        childClass.fn.constructor=childClass;

        childClass.extend=arguments.callee;

        return childClass;
    };

    module.exports={
        Class: Class
    };
});