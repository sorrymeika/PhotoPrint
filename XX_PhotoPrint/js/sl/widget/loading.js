define(['$','./../view'],function(require,exports,module) {
    var $=require('$'),
        view=require('./../view');

    module.exports=view.extend({
        events: {},

        options: {
            url: '',
            pageIndex: 1,
            pageSize: 25,
            param: {}
        },

        initialize: function() {
            var that=this;

            that.pageIndex=1;
            that.url=that.options.url;
            that.param=that.options.param;
            that.pageSize=that.options.pageSize;
            that.pageIndex=that.options.pageIndex;

        },

        showLoading: function() {
        },

        hideLoading: function() {
        },

        showDataLoading: function() {
            var that=this;

            if(that.pageIndex==1) {
                that.showLoading();
                that.$refreshing.hide();
            } else {
                that.$refreshing.find('p').html('加载中');
                that.$refreshing.show().find('i').show();
            }

        },

        hideDataLoading: function() {
            var that=this;

            if(that.pageIndex==1) {
                that.hideLoading();
            }
        },

        showMsg: function(msg) {
            this.$refreshing.find('p').html(msg);
            this.$refreshing.show().find('i').hide();
        },

        showError: function() {
            var that=this;
            if(that.pageIndex==1) {
                that.$list.html('加载失败');

            } else {
                if(that.isError) {
                    //，请点击重试<i class="i-refresh"></i>
                    that.showMsg('加载失败');
                }
            }
        },

        hasData: function(data) {
            return data.data&&data.data.length!=0;
        },

        reload: function() {
            var that=this,
                param=that.param;

            if(that.isLoading) return;

            that.pageIndex=1;

            that.$refreshing.hide();
            window.scrollTo(0,0);

            that.load(true);
        },

        load: function(isClearList) {
            var that=this;

            if(that.isLoading) return;

            that.ajax&&that.ajax.abort();

            that.showDataLoading();

            that.isLoading=true;
            that.isError=false;

            that.param.page=that.pageIndex;
            that.param.pageSize=that.pageSize;

            that.ajax=$.ajax({
                url: that.url,
                data: that.param,
                dataType: that.options.dataType||'json',
                success: function(data) {

                    if(isClearList===true) that.$list.html('');

                    that.hideDataLoading();
                    that.isLoading=false;

                    if(that.hasData(data)) {
                        that.render(data);
                        that.checkAutoRefreshing(data);

                    } else {
                        that._dataNotFound();
                    }
                },
                error: function() {
                    that.isError=true;
                    that.isLoading=false;

                    that.hideDataLoading();
                    that.showError();
                },
                complete: function() {
                    that.ajax=null;
                }
            });

        },

        render: function() { },

        _dataNotFound: function() {
            var that=this;

            if(that.pageIndex==1) {
                that.$list.html('暂无数据');
            } else {
                that.showMsg('没有更多数据了');
            }
            that.disableAutoRefreshing();
        },

        _refresh: function() {
            this.load();
        },

        _scroll: function() {
            var that=this;

            if(!that.isLoading
                &&that._scrollY<$(window).scrollTop()
                &&$(window).scrollTop()+$(window).height()>=that.$refreshing.position().top) {

                that._refresh();
            }

            that._scrollY=$(window).scrollTop();
        },

        _autoRefreshingEnabled: false,

        checkAutoRefreshing: function(res) {
            var that=this;

            if(that.pageIndex*that.pageSize<res.total) {
                that.pageIndex++;
                that.showMsg('继续加载');
                that.enableAutoRefreshing();

            } else {
                that.showMsg('没有更多数据了');
                that.disableAutoRefreshing();
            }
        },

        enableAutoRefreshing: function() {
            if(this._autoRefreshingEnabled) return;
            this._autoRefreshingEnabled=true;

            $(window).on('scroll',$.proxy(this._scroll,this));

            this._scrollY=$(window).scrollTop();


            if(this._scrollY+$(window).height()>=this.$refreshing.offset().top) {
                this._refresh();
            }
        },

        disableAutoRefreshing: function() {
            if(!this._autoRefreshingEnabled) return;
            this._autoRefreshingEnabled=false;

            $(window).off('scroll',this._scroll);
        },

        onDestory: function() {
            this.disableAutoRefreshing();
        }
    });
});