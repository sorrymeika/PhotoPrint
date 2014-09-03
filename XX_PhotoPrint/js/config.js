seajs.config({
    plugins: ['shim'],
    alias: {
        'jquery': {
            src: 'lib/jquery-1.7.2.min',
            exports: 'jQuery'
        },
        'highcharts': {
            src: 'lib/highcharts',
            exports: 'Highcharts'
        },
        'kindeditor': {
            src: 'kindeditor/kindeditor-min',
            exports: 'KindEditor'
        }
    }
});