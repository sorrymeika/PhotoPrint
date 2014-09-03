define(function (require,exports,module) {
    var $=require('jquery');

    var offsetParent=function (el) {
        var parent=el.parent(),
            position;
        while(parent.length!=0&&parent[0].tagName.toLowerCase()!="body") {
            if($.inArray(parent.css('position'),['fixed','absolute','relative']))
                return parent;
            parent=parent.parent();
        }
        return parent;
    };

    module.exports=function (options) {
        var defaults={
            id: "",
            left: 0,
            top: 4,
            emptyAble: true,
            emptyText: null,
            regex: null,
            regexText: null,
            compare: null,
            compareText: null,
            validate: null,
            validateText: null,
            success: null,
            successText: '√',
            position: 'after:this',
            msg: null,
            msgClass: 'msg_tip',
            wrongClass: 'error_tip',
            rightClass: 'right_tip',
            beforeValidate: null
        };
        var validations=[],
            add=function (option) {
                option=$.extend({},defaults,option);
                var selector=option.selector||option.id,
                    o=typeof selector=="string"?$(selector):selector,
                    body=document.body,
                    left=option.left,
                    topFix=option.top,
                    position=option.position.split(':'),
                    where=position[0],
                    isDock=/^dock/.test(where),
                    el=(new Function('return '+(position[1]||'this'))).call(o),
                    compare=option.compare;

                var displayControlParent=$("<span></span>")
                        .css({
                            position: "absolute",
                            top: 0,
                            left: 0,
                            width: 300,
                            padding: 0,
                            margin: 0
                        }),
                    displayControl=$("<span>").appendTo(displayControlParent).css({
                        position: "absolute",
                        top: 0,
                        left: 0,
                        display: "none",
                        "z-index": 1000
                    }),
                    show_displayControl=function (s) {
                        displayControl.show();

                        displayControlParent.appendTo(isDock?el:offsetParent(el));
                        var pos=o.position();

                        if('bottom'==where) {
                            displayControlParent.css({
                                left: pos.left+left,
                                top: pos.top+o.outerHeight()+topFix
                            });
                        }
                        else if('dock-bottom'==where) {
                            displayControlParent.css({
                                top: '',
                                left: left,
                                bottom: 0-topFix
                            });
                        } else if('dock-top'==where) {
                            displayControlParent.css({
                                top: -1*o.outerHeight()-pos.top,
                                left: left,
                                bottom: 0-topFix
                            });
                        } else if('top'==where) {
                            displayControlParent.css({
                                left: pos.left,
                                top: pos.top+topFix
                            });
                        }
                        else
                            displayControlParent.css({
                                left: pos.left+o.outerWidth()+left,
                                top: pos.top+topFix
                            });
                        return displayControl;
                    },
                    showError=function (msg) {
                        show_displayControl().html(msg||"").removeClass(option.rightClass)
                            .addClass(option.wrongClass);
                        o.trigger('validate',[false]);
                    },
                    validate=function () {
                        var v=o.val(),
                            flag=false,
                            success=function () {
                                flag=true;
                                show_displayControl(1).removeClass(option.wrongClass)
                                    .addClass(option.rightClass)
                                    .html(option.successText);
                                if($.isFunction(option.success)) option.success();

                                o.trigger('validate',[true]);
                            };

                        if((option.emptyAble===false||($.isFunction(option.emptyAble)&&!option.emptyAble()))&&v=="")
                            showError(option.emptyText);
                        else if(v!=""&&option.regex!==null&&!option.regex.test(v))
                            showError(option.regexText);
                        else if(option.compare&&option.compare.val()!=v)
                            showError(option.compareText);
                        else if(option.validate&&!option.validate.call(option,v,function (validateRes) {
                            if(!validateRes) { showError(option.validationText); }
                            else success();
                        })) showError(option.validationText);
                        else
                            success();

                        return flag;
                    };

                option.validateError=function (msg) {
                    this.validationText=msg;
                    showError(msg);
                };

                validations.push({
                    el: o,
                    validate: validate,
                    showError: function (msg) {
                        showError(msg);
                    },
                    hide: function () {
                        displayControl.hide();
                    }
                });

                if(compare)
                    compare.on('validate',function (evt,suc) {
                        if(suc&&o.val()&&this.value!=o.val()) {
                            showError(option.compareText);
                        }
                    });

                o.focus(function () {
                    if(option.beforeValidate) option.beforeValidate.call(option);
                    displayControl.hide();
                    if(option.msg) show_displayControl().html(option.msg)
                                    .removeClass(option.wrongClass+" "+option.rightClass)
                                    .addClass(option.msgClass);
                });

                o.blur(function () {
                    displayControl.removeClass(option.msgClass).hide();
                    validate();
                });

                $(window).bind("unload",function () {
                    displayControl.remove();
                })
            };

        if(options)
            $.each(options,function (i,option) {
                add(option);
            });

        this.validate=function () {
            var result=true;
            $.each(validations,function (i,v) {
                result&=v.validate();
            });
            return result||false;
        };

        this.hideAll=function () {
            $.each(validations,function (i,v) {
                v.hide();
            });
        };

        this.fire=function (selector,err) {
            $.each(validations,function (i,v) {
                if(v.el.selector==selector) {
                    v.showError(err);
                    return false;
                }
            });
        };

        this.clear=function () {
            validations.length=0;
        };

        this.add=function (option) {
            add(option);
        };
    };
});
