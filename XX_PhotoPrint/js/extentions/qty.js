define(function (require,exports,module) {
    var $=require('jquery');

    return {
        'keyup .js_qty': function (e) {
            var $qty=$(e.currentTarget),
                        qty=$qty.val();

            if(qty!=''&&(!/^\d+$/.test(qty)||qty=='0')) {
                $qty.val($qty.data('qty')).css({
                    background: '#f9d0d0',
                    border: '1px solid #c00'
                });

                setTimeout(function () {
                    $qty.css({
                        background: '',
                        border: ''
                    });
                },200);

            } else if(qty!='') {
                $qty.data('qty',qty);
                $qty.trigger('changeQty',qty);
            }
        },
        'blur .js_qty': function (e) {
            var $qty=$(e.currentTarget),
                        qty=$qty.val();

            if(qty=='') {
                $qty.val($qty.data('qty'));
            }
        }
    };
});