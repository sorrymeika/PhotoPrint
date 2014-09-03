define(function (require,exports,module) {
    var $=require('jquery'),
        template='<div class="dir{isOpen?"_open":""}"><i></i><span>{name}</span></div>';

    function Menu(options) {
        var defauls={
            container: '',
            data: []
        };
        this._init($.extend(defauls,options));
    };

    Menu.prototype._init=function (options) {
        this.options=options;

        var container=$(options.container);
    };

    module.exports=Menu;
});