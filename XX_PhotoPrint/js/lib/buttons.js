define(function (require,exports,module) {
    var $=require('jquery');

    var defaults={
        text: "",
        ico: null,//delete,add,modify,view
        disabled: false,
        handle: function () { }
    };

    function Buttons(container,options) {

        var container=this.container=$(container),
            buttons=this.buttons=[];

        $.each(options,function (i,item) {
            item=$.extend({},defaults,item);

            var button=$("<a class='button'>"+item.text+"</a>")
                .appendTo(container)
                .click(function () {
                    if(button.disabled) {
                        return;
                    }
                    item.handle.call(this);
                });
            button.disable=function () {
                this.disabled=true;
                this.addClass("disabled");
                return this;
            };
            button.enable=function () {
                this.disabled=false;
                this.removeClass("disabled");
                return this;
            };
            if(item.ico)
                button.contents().first().before("<em class='ico-"+item.ico+"'></em>");

            if(item.disabled)
                button.disable();
            else
                button.enable();

            buttons.push(button);
        });
    };

    Buttons.prototype={
        eq: function (i) {
            return buttons[i];
        },
        disable: function () {
            var buttons=this.buttons;

            $.each($.isArray(arguments[0])?arguments[0]:arguments,function (i,index) {
                buttons[index].disable();
            });
        },
        enable: function () {
            var buttons=this.buttons;

            $.each($.isArray(arguments[0])?arguments[0]:arguments,function (i,index) {
                buttons[index].enable();
            });
        }
    }

    module.exports=Buttons;
});