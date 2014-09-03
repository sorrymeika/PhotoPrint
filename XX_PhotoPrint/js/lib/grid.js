define(function (require,exports,module) {
    var $=require('jquery'),
        util=require('lib/util'),
        Page=require('lib/page'),
        isIE6=/MSIE 6/.test(navigator.userAgent),
        tpl="<div class='grid'><div class='gridBorderR'><div class='gridHeaderContainer'><ol class='gridHeader'></ol></div><div class='gridBody'></div></div></div>",
        sum=function (array,key) {
            var total=0;
            if(key)
                $.each(array,function (i,item) {
                    total+=parseInt(item[key]);
                });
            else
                $.each(array,function (i,item) {
                    total+=parseInt(item);
                });
            return total;
        },
        compareData=function (v1,v2,asc) {
            var flag;
            if(typeof v1=='number') {
                flag=asc?v1>v2:v1<v2;
            } else {
                flag=(v1+"").localeCompare(v2+"");
                flag=asc?flag>0:flag<0;
            }
            return flag;
        };

    var GridGuid=0,
        Grid=function (container,options) {
            GridGuid++;

            var me=this,
                container=$(container).eq(0),
                settings=$.extend(true,{
                    type: "grid",//grid:普通列表;tree:树形列表
                    pageEnabled: false,
                    rowHeight: 20,
                    multiselect: false,
                    //树形列表的子数据的key
                    subKey: "children",
                    rows: [],
                    //默认数据
                    data: [],
                    columns: [],
                    children: null,
                    ajaxSettings: {},
                    search: null
                },options),
                columnsOpt=settings.columns;

            $.each(columnsOpt,function (i,columnOpt) {
                columnsOpt[i]=$.extend({
                    text: "",
                    bind: "",
                    render: null,
                    width: 10,
                    align: "left",
                    valign: "top",
                    type: "normal",
                    style: {},
                    css: "",
                    defaultVal: ""
                },columnOpt);
            });

            me._settings=settings;

            var guid=GridGuid,
            getColumnWidths=function (settings) {
                var length=settings.columns.length-1,
                    percent=0,
                    totalPercent=0,
                    total=0,
                    column,
                    width,
                    widths=new Array(length);

                $.each(settings.columns,function (i,item) {
                    if(!item.hide) total+=parseInt(item.width);
                });

                for(var i=0;i<length;i++) {
                    column=settings.columns[i];
                    percent=Math.round(100*column.width/total);
                    widths[i]=percent;
                    totalPercent+=percent;
                }
                widths[length]=$.browser.msie?"auto":(100-totalPercent);

                return widths;
            },
            createColumns=function () {

                var columnWidths=getColumnWidths(settings),
                    columnWidth;

                $.each(settings.columns,function (i,column) {
                    columnWidth=columnWidths[i];
                    if(typeof columnWidth=="number") columnWidth+="%";
                    column.index=i;
                    var cell=$("<li></li>"),
                        text;
                    if(column.type=="selector"&&settings.multiselect) {
                        text=$("<input type='checkbox'/>")
                          .prop({ checked: false })
                          .click(function () {
                              settings.body.find("input[name='__gs_"+guid+"']").prop({ checked: this.checked });
                              var f=this.checked?"select":"unselect";
                              settings.selectedRows.length=0;
                              settings.selectedRow=false;
                              $.each(settings.rows,function (i,item) {
                                  item[f]();
                              });
                              if(settings.selectedRows.length==0)
                                  settings.selectedRow=null;
                              else {
                                  var row=settings.selectedRows[0];
                                  settings.selectedRow=row;
                                  if($.isFunction(settings.onRowSelect)) settings.onRowSelect(row,row.data);
                              }
                          });
                        column.align="center";
                        columnWidth=50;
                    } else {
                        text=$("<a>"+column.text+"</a>").appendTo(cell);

                        if(i==0) {
                            if(settings.children) {
                                $("<div class='gridPlus'></div>")
                                    .click(function () {
                                        if($(this).hasClass("gridPlus")) {
                                            $(this).removeClass("gridPlus").addClass("gridMinus");
                                            $.each(settings.rows,function (ri,row) {
                                                row.expand();
                                            });
                                        } else {
                                            $(this).removeClass("gridMinus").addClass("gridPlus");
                                            $.each(settings.rows,function (ri,row) {
                                                row.shrink();
                                            });
                                        }
                                    })
                                    .appendTo(cell)
                                    .append(text);
                                column.align="left";
                            }
                        }
                        var bind=column.bind;
                        if((column.sortAble||column.sortable)&&bind) {
                            text
                                .addClass("sortable")
                                .click(function () {
                                    var ajaxData=settings.ajaxSettings.data;
                                    ajaxData.page=1;
                                    ajaxData.sort=column.bind;

                                    if(settings.currentSort!=this) {
                                        $(settings.currentSort).removeClass("sort_desc sort_asc");
                                        settings.currentSort=this;
                                    }
                                    var asc;
                                    if($(this).hasClass("sort_asc")) {
                                        asc=false;
                                        $(this).removeClass("sort_asc").addClass("sort_desc");
                                    } else {
                                        asc=true;
                                        $(this).removeClass("sort_desc").addClass("sort_asc");
                                    }
                                    ajaxData.asc=asc;

                                    if(settings.pageEnabled) load(settings);
                                    else {
                                        settings.data.sort(function (a,b) {
                                            return compareData(a[bind],b[bind],asc);
                                        });
                                        settings.rows.sort(function (a,b) {
                                            var flag=compareData(a.data[bind],b.data[bind],asc);
                                            if(flag) {
                                                a.el.before(b.el);
                                            }
                                            return flag;
                                        });
                                    }
                                });
                        }
                    }

                    column.curWidth=columnWidth;
                    var css={
                        "text-align": column.align,
                        width: columnWidth
                    }
                    if(columnWidth=='auto'&&$.browser.msie)
                        css['float']='none';

                    cell.css(css)
                        .appendTo(settings.header);
                });
            },
            showMsg=function (msg) {
                settings.body.html('<div style="padding:10px;border-bottom: 1px solid #cdcdcd;">'+msg+'</div>');
            },
            loadData=function () {
                if(!settings.data||!settings.data.length) {
                    showMsg("暂无数据");
                    return;
                }

                settings.body.html("");
                var select=!$.isFunction(settings.onRowSelect)?
                function (row) {
                    settings.selectedRow=row;
                } :
                function (row) {
                    settings.selectedRow=row;
                    if(row) settings.onRowSelect(row,row.data);
                },
                treeRowNumber=[],
                dataForEach=function (item,treeDeep,parentTree) {
                    var rowEl=$("<ul class='gridRow'></ul>")
                            .css({ height: settings.rowHeight })
                            .click(function (e) {
                                if(settings.multiselect) {
                                    if(e.target.name!="__gs_"+guid)
                                        row[row.selected?"unselect":"select"]();
                                } else if(settings.selectedRow!=row) {
                                    if(settings.selectedRow)
                                        settings.selectedRow.unselect();
                                    row.select();
                                }
                            })
                            .appendTo(settings.body),
                        row={
                            el: rowEl,
                            data: item,
                            selected: false,
                            select: function () {
                                row.selected=true;
                                rowEl.addClass("gridRowCur");
                                if(row.selector) row.selector.prop({ checked: true });
                                settings.selectedRows.push(row);
                                if(settings.selectedRow!==false) {
                                    settings.selectedRow=row;
                                    select(row);
                                }
                            },
                            unselect: function () {
                                row.selected=false;
                                rowEl.removeClass("gridRowCur");
                                if(row.selector) row.selector.prop({ checked: false });
                                for(var n=0;n<settings.selectedRows.length;n++) {
                                    if(settings.selectedRows[n]==row) {
                                        settings.selectedRows.splice(n,1);
                                        break;
                                    }
                                }
                                if(settings.selectedRow==row) {
                                    if(settings.selectedRows.length!=0)
                                        select(settings.selectedRows[settings.selectedRows.length-1]);
                                    else
                                        settings.selectedRow=null;
                                }
                            },
                            cells: []
                        };
                    settings.rows.push(row);

                    if(settings.type!="tree"&&settings.children) {
                        var childGrid,
                            childContainer=$('<div class="gridChildNon"></div>').appendTo(settings.body);
                        row.childContainer=childContainer;
                        row.children=[];

                        $.each(settings.children,function (ci,childOpt) {
                            if(typeof childOpt.render==="function") {
                                childOpt.render(childContainer,item,row);
                            } else {
                                if(settings.subKey)
                                    childOpt.data=item[settings.subKey];
                                childGrid=$("<div></div>").appendTo(childContainer).grid(childOpt);
                                childGrid.parentRow=row;
                                row.children.push(childGrid);
                            }
                        });
                    } else if(settings.type=="tree") {
                        treeDeep=treeDeep||0;
                        treeRowNumber[treeDeep]=treeRowNumber[treeDeep]?treeRowNumber[treeDeep]+1:1;
                    }

                    $.each(settings.columns,function (j,column) {
                        var css={
                            width: column.curWidth,
                            height: settings.rowHeight
                        };
                        if(column.curWidth=='auto'&&$.browser.msie)
                            css['float']='none';
                        var cellEl=$("<li class='gridCell'></li>").css(css),
                            cellContent=cellEl,
	                        val=column.bind?(item[column.bind]||(item[column.bind]===0?0:column.defaultVal)):"",
	                        cell={
	                            data: val,
	                            row: row,
	                            append: function (a) {
	                                cellContent.append(a);
	                            },
	                            html: function (a) {
	                                if(typeof a==="undefined")
	                                    return cellContent.html();
	                                cellContent.html(a);
	                            },
	                            val: function (a) {
	                                if(typeof a=="undefined")
	                                    return val;

	                                row.data[column.bind]=val=a;
	                                if(typeof cell.onChange=="function") cell.onChange(a);
	                            },
	                            find: function (a) {
	                                return cellEl.find(a);
	                            }
	                        };
                        row.cells.push(cell);
                        if(j==0) {
                            if(settings.type=="tree") {
                                //if(treeDeep!=0) rowEl.hide();
                                parentTree=parentTree||0;
                                var childTree=parentTree+'_'+treeRowNumber[treeDeep];
                                rowEl.attr({ parenttree: childTree });

                                for(var i=0;i<treeDeep;i++) {
                                    $("<i class='gridTreeSpace'></i>").appendTo(cellEl);
                                }

                                var children=item[settings.subKey];
                                if(children&&children.length) {
                                    var $body=settings.body;
                                    $("<i class='gridTree'></i>")
                                        .click(function () {
                                            var $show=$body.find("[parenttree^='"+childTree+"_']");
                                            if(rowEl.hasClass("gridTreeMinus")) {
                                                rowEl.removeClass("gridTreeMinus").addClass("gridTreePlus");
                                                if(isIE6) $show.css({ height: 0,marginTop: -1 });
                                                else $show.hide();
                                            } else {
                                                rowEl.removeClass("gridTreePlus").addClass("gridTreeMinus");
                                                $show.each(function (i,$s) {
                                                    $s=$($s);
                                                    if($body.find("[parenttree='"+$s.attr('parenttree').replace(/_\d+$/,'')+"']").hasClass('gridTreeMinus')) {
                                                        $s.show();
                                                        if(isIE6) $s.css({ height: settings.rowHeight,marginTop: '' });
                                                    }
                                                });
                                            }
                                        })
                                        .appendTo(cellEl);
                                    rowEl.addClass("gridTreeMinus");
                                    $.each(children,function (i,item) {
                                        dataForEach(item,treeDeep+1,childTree);
                                    });
                                } else
                                    $("<i class='gridTreeNoChildren'></i>").appendTo(cellEl);

                            } else if(settings.children) {
                                cellContent=$('<div class="gridPlus"></div>')
                                    .appendTo(cellEl)
                                    .on("click",function (e) {
                                        if(e.target==this) {
                                            if(this.minus) {
                                                $(this).removeClass("gridMinus").addClass("gridPlus");
                                                row.childContainer.removeClass('gridChildCon').addClass("gridChildNon");

                                            } else {
                                                $(this).removeClass("gridPlus").addClass("gridMinus");
                                                row.childContainer.removeClass('gridChildNon').addClass("gridChildCon");
                                            }
                                            this.minus=!this.minus;
                                        }
                                    }).prop("minus",false);
                                row.expand=function () {
                                    if(!cellContent.prop('minus'))
                                        cellContent.trigger("click");
                                };
                                row.shrink=function () {
                                    if(cellContent.prop('minus'))
                                        cellContent.trigger("click");
                                };
                            }
                        }

                        if(column.style) cellEl.css(column.style);
                        if(column.align) cellEl.css({ "text-align": column.align });
                        if(column.render) {
                            column.render(cell,row.data,settings.data);
                        } else if(column.type=="textbox") {
                            var textbox=$("<textarea class='gridCellTextBox'></textarea>")
                                .css({ height: settings.rowHeight })
                                .blur(function () {
                                    cell.validate();
                                })
                                .val(cellValue);
                            cell.textbox=textbox;
                            cell.append(textbox);
                            cell.onChange=function (a) {
                                textbox.value=a;
                                textbox.validate();
                            };
                            cell.validate=function () {
                                var error=false;

                                if(column.emptyAble===false&&textbox.value=="")
                                    error=column.emptyText||(column.text+"不可为空！");
                                else if(column.regex&&!column.regex.test(textbox.value))
                                    error=column.regexText||(column.text+"格式错误！");

                                if(!!error)
                                    cellEl.addClass("gridCellErr").title=error;
                                else
                                    cellEl.removeClass("gridCellErr").title="";

                                return !error;
                            };
                        } else if(column.type=="selector") {
                            var selector=$("<input type='"+(settings.multiselect?"checkbox":"radio")+"' name='__gs_"+guid+"'>").prop({ "isselector": true,"checked": false });
                            cell.append(selector);
                            cell.selector=row.selector=selector;

                            if(settings.multiselect)
                                selector.click(function () {
                                    if(this.checked) row.select();
                                    else row.unselect();
                                });

                        } else if(column.valign=="center") {
                            cell.append('<table style="height:100%;width:100%;"><tr><td>'+val+'</td></tr></table>');
                        } else
                            cell.append("<i class='gridCellItem'>"+val+"</i>");
                        cellEl.appendTo(rowEl);
                    });
                };
                $.each(settings.data,function (i,item) {
                    dataForEach(item);
                });
            },
            load=function () {
                showMsg("正在载入...");
                if(settings.page) settings.page.clear();

                var ajaxSettings=settings.ajaxSettings,
                    ajaxData=ajaxSettings.data;

                $.ajax({
                    url: ajaxSettings.url,
                    type: 'POST',
                    beforeSend: ajaxSettings.beforeSend,
                    data: ajaxData,
                    dataType: 'json',
                    cache: false,
                    success: function (res) {
                        if(res.success) {
                            settings.data=$.extend([],res.data);
                            loadData();

                            if(settings.pageEnabled)
                                settings.page.change({
                                    page: ajaxData.page,
                                    pageSize: ajaxData.pageSize,
                                    total: res.total
                                });
                            if(ajaxSettings.success) ajaxSettings.success.call(me,res.data);
                        } else
                            showMsg(res.msg);
                    },
                    error: function (xhr) {
                        if(ajaxSettings.error) ajaxSettings.error.call(me,xhr);
                    }
                });
            };

            me._load=load;
            me._loadData=loadData;

            var searchOpt=settings.search;
            if(!searchOpt) {
                me._search=me.load;

            } else {
                var controls=[],
                    searchControls={},
                    searchData=searchOpt.data;

                if(searchData)
                    $.each(searchData,function (controlName,controlOpt) {
                        if($.isPlainObject(controlOpt))
                            searchControls[controlName]=controlOpt;
                    });

                me._search=function () {
                    searchData=searchOpt.data||(searchOpt.data={});
                    $.each(controls,function (j,item) {
                        searchData[item.name]=item.control.val();
                    });
                    me.load(searchOpt);
                };

                if(searchControls&&!$.isEmptyObject(searchControls)) {

                    var searchEl=$('<div class="search"></div>').appendTo(container);

                    me._searchEl=searchEl;

                    $.each(searchControls,function (j,inputopt) {
                        var opt=$.extend({
                            label: '',
                            name: ''||j,
                            type: 'text',
                            value: '',
                            render: null,
                            width: null,
                            options: null
                        },inputopt);

                        opt.label&&$('<i>'+opt.label+'</i>').appendTo(searchEl)||searchEl.append(' ');

                        var name=opt.name;

                        if($.isFunction(opt.render)) {
                            var input=opt.render.call(me,searchEl);
                            if(typeof input=="string")
                                searchEl.append(input);
                            controls.push({
                                type: 'render',
                                name: name,
                                control: searchEl.find('[name="'+name+'"]')
                            });
                        }
                        else {
                            var control={ name: name,type: opt.type };

                            if(opt.type=="calendar") {
                                input=$('<input name="'+name+'" class="text" type="text"/>');
                                seajs.use(['lib/jquery.datepicker','lib/jquery.datepicker.css'],function () {
                                    input.datePicker($.extend(opt.options,{
                                        clickInput: true
                                    }));
                                });
                            } else if(opt.type=="select") {
                                input=$('<select name="'+name+'"></select>');
                                if($.isArray(opt.options)) {
                                    $.each(opt.options,function (j,selopt) {
                                        input.each(function () {
                                            this.options.add(new Option(selopt.text,selopt.value));
                                        });
                                    });
                                }
                            } else {
                                input=$('<input type="'+opt.type+'" name="'+name+'" class="text"/>');
                            }
                            input.appendTo(searchEl).val(opt.value);

                            control.control=input;
                            controls.push(control);

                            if(opt.width) input.css({ width: width });
                        }
                    });

                    delete searchOpt.controls;

                    me._searchBtn=$('<button class="button">搜索</button>')
                        .appendTo(searchEl)
                        .click($.proxy(me._search,me));
                }
            }
            container.append(tpl);

            settings.selectedRows=[];

            settings.header=container.find(".gridHeader");
            settings.body=container.find(".gridBody");
            if(settings.pageEnabled)
                settings.page=$("<DIV class='page'>共0条数据</DIV>")
                        .appendTo(container.find(".grid"))
                        .page({
                            page: 1,
                            pageSize: 10,
                            total: 0,
                            onChange: function (page) {
                                settings.ajaxSettings.data.page=page;
                                load();
                            }
                        });

            createColumns();
            loadData();
        };

    Grid.prototype={
        search: function () {
            var me=this;
            me._search&&me._search();
            return me;
        },
        searchInput: function (name) {
            var me=this;
            return me._searchEl?me._searchEl.find('[name="'+name+'"]'):$('');
        },
        selectedRow: function () {
            return this._settings.selectedRow;
        },
        row: function (i) {
            return this._settings.rows[i];
        },
        cell: function (rowIndex,columnIndex) {
            return this._settings.rows[rowIndex].cells[columnIndex];
        },
        acceptChanges: function () {
        },
        loadData: function (data) {
            var me=this,
                settings=me._settings;
            settings.data=$.extend([],data);
            me._loadData();
        },
        load: function (url,data,fn) {
            var me=this,
                args=arguments,
                i=0,
                ajaxSettings=url&&typeof url=="object"?$.extend({},url):function () {
                    if($.isFunction(data)) {
                        fn=data;
                        data=fn;
                    }
                    return {
                        url: url,
                        success: fn,
                        data: data
                    }
                } ();

            var settings=me._settings;
            ajaxSettings.data=$.extend(settings.pageEnabled?{ page: 1,pageSize: 10}:{},ajaxSettings.data);
            settings.ajaxSettings=ajaxSettings;
            me._load();
        }
    };

    $.extend($.fn,{
        grid: function (options) {
            return new Grid(this,options);
        }
    });

    $.fn.Grid=$.fn.grid;

    module.exports=Grid;
});
