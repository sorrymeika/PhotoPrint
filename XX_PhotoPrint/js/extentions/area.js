define(function(require,exports,module) {
    var $=require('jquery');

    return {
        'change [name="province"]': function(e) {
            var that=this,
                target=e.currentTarget,
                $city=this.$('[name="city"]'),
                city=$city[0];

            if(!target.value) {
                city.options.length=1;
                return;
            }

            city.options.length=0;
            city.options.add(new Option('正在载入...',''));

            $.ajax({
                url: '/json/user/getCitiesByProvID',
                type: 'GET',
                dataType: 'json',
                data: {
                    provId: target.value
                },
                success: function(res) {
                    if(res.success) {
                        city.options[0].text="请选择";
                        $.each(res.data,function(i,item) {
                            city.options.add(new Option(item.CityName,item.CityID));
                        });
                        $city.trigger('reload');
                    } else {
                        city.options[0].text=res.msg;
                    }
                }
            });
        },
        'change [name="city"]': function(e) {

            var that=this,
                target=e.currentTarget,
                $region=this.$('[name="region"]'),
                region=$region[0];

            if(!target.value) {
                region.options.length=1;
                return;
            }

            region.options.length=0;
            region.options.add(new Option('正在载入...',''));

            $.ajax({
                url: '/json/user/getRegionsByCityID',
                type: 'GET',
                dataType: 'json',
                data: {
                    cityId: target.value
                },
                success: function(res) {
                    if(res.success) {
                        region.options[0].text="请选择";
                        $.each(res.regions,function(i,item) {
                            region.options.add(new Option(item.RegionName,item.RegionID));
                        });
                        $region.trigger('reload');
                    } else {
                        region.options[0].text=res.msg;
                    }
                }
            });
        }
    };
});