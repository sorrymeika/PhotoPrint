define(function(require,exports,module) {
    var $=require('jquery'),
        util=require('lib/util'),
        V=require('lib/validation'),
        Buttons=require('lib/buttons');

    var Form=function(container,options) {
        var me=this;

        if(options||!me._options)
            me._options=$.extend({
                title: '',
                url: '',
                controls: [],
                buttons: []
            },options);

        me.validation=new V();
        me._controls=[];

        options=me._options;
        container=$(container);

        var title=options.title&&$('<h1>'+options.title+'</h1>').appendTo(container),
            form=$('<form action="'+options.url+'" method="post" enctype="multipart/form-data" class="form"><table><tbody></tbody></table></form>').appendTo(container),
            tbody=form.find('tbody');

        me._form=form;
        me._formContainer=tbody;

        $.each(options.controls,function(i,inputopt) {
            if(typeof i==="string"&&!inputopt.name) inputopt.name=i;
            me.add(inputopt);
        });

        me.buttons=new Buttons($('<div class="action"></div>').appendTo(container),options.buttons);
    };

    Form.prototype={
        add: function(inputopt) {
            var me=this,
                opt=$.extend(true,{
                    label: '',
                    labelVAlign: 'center',
                    name: '',
                    type: 'text',
                    value: '',
                    validation: null,
                    render: null,
                    width: null,
                    height: null,
                    options: null
                },inputopt);

            var inputValid=opt.validation,
                tbody=me._formContainer,
                name=opt.name,
                controls=me._controls,
                input,
                control={
                    name: name,
                    type: opt.type
                };

            if(opt.type=='hidden') {
                input=$('<input name="'+name+'" type="hidden"/>').appendTo(me._form);
                input.val(opt.value);
            } else {

                var tr=$('<tr><th>'+opt.label+(!inputValid||inputValid.emptyAble?'':'<i>*</i>')+'</th></tr>').appendTo(tbody),
                td=$("<td></td>").appendTo(tr);

                if(opt.labelVAlign=="top") {
                    tr.find('th').addClass('vtop');
                }

                if($.isFunction(opt.render)) {
                    input=opt.render.call(me,td);
                    if(typeof input=="string")
                        td.append(input);
                    controls.type='render';
                    input=td.find('[name="'+name+'"]');
                }
                else {
                    if(opt.type=="html"||opt.type=="editor") {
                        var htmlContainer=$('<div style="position: relative;"></div>').appendTo(td);
                        input=$('<textarea name="'+name+'" style="width: 600px; height: 400px; visibility: hidden;"></textarea>').appendTo(htmlContainer).val(opt.value);
                        opt.width&&input.css({ width: opt.width });
                        opt.height&&input.css({ height: opt.height });

                        inputValid&&!inputValid.position&&(inputValid.position='dock-bottom:this.parent()');

                        seajs.use('kindeditor/kindeditor-min',function(K) {
                            var editor=K.create(input[0],$.extend({
                                uploadJson: "/manage/upload",
                                allowFileManager: false,
                                items: ['source','fontname','fontsize','|','forecolor','hilitecolor','bold','italic','underline',
                'removeformat','|','justifyleft','justifycenter','justifyright','insertorderedlist',
                'insertunorderedlist','|','emoticons','image','link']
                            },opt.options));
                            control.editor=editor;
                        });
                    }
                    else {
                        if(opt.type=="textarea") {
                            input=$('<textarea name="'+name+'" class="text"></textarea>');
                        } else if(opt.type=="calendar") {
                            input=$('<input name="'+name+'" class="text_normal" type="text"/>');
                            seajs.use(['lib/jquery.datepicker','lib/jquery.datepicker.css'],function() {
                                input.datePicker($.extend(opt.options,{
                                    clickInput: true,
                                    verticalOffset: 4
                                }));
                            });
                        } else if(opt.type=="select") {
                            input=$('<select name="'+name+'"></select>');
                            if($.isArray(opt.options)) {
                                $.each(opt.options,function(j,selopt) {
                                    input.each(function() {
                                        this.options.add(new Option(selopt.text,selopt.value));
                                    });
                                });
                            }
                        } else {
                            input=$('<input type="'+opt.type+'" name="'+name+'"'+(opt.className?' class="'+opt.className+'"':opt.type=='text'?' class="text"':opt.type=='password'?' class="text_normal"':'')+'/>');
                        }
                        input.appendTo(td).val(opt.value);

                        opt.width&&input.css({ width: opt.width });
                        opt.height&&input.css({ height: opt.height });
                    }

                }
            }
            control.control=input;

            controls.push(control);

            if(inputValid) {
                if(!inputValid.id) inputValid.id=input;
                var compare=inputValid.compare;
                if(compare) inputValid.compare=me.control(compare);

                me.validation.add(inputValid);
            }
        },
        serialize: function() {
            return this._form.serialize();
        },
        editor: function(name) {
            var me=this,
                editor=null;
            $.each(me._controls,function(i,item) {
                if(item.type=="html"||item.type=="editor") {
                    editor=item.editor;
                    return true;
                }
            });
            return editor;
        },
        control: function(name) {
            return this._form.find('[name="'+name+'"]');
        },
        validate: function() {
            var me=this;
            $.each(me._controls,function(i,item) {
                if(item.type=="html"||item.type=="editor") {
                    item.control.val(item.editor.html());
                }
            });
            return me.validation.validate();
        },
        submit: function(url,fn) {
            var me=this,
                options=me._options;

            var settings=url&&typeof url==="object"?$.extend({
                url: options.url,
                validate: function() {
                    return true;
                },
                beforeSend: $.noop,
                success: $.noop,
                error: $.noop
            },url):{
                url: $.isFunction(url)&&options.url||url,
                success: fn||!fn&&$.isFunction(url)&&url
            };

            if(!me.validate()||(settings.validate&&!settings.validate())) {
                settings.error&&settings.error.call(me,'表单验证失败');
            } else {
                settings.beforeSend&&settings.beforeSend.call(me);
                if(me._form.has('[type="file"]').length>0) {
                    util.submitForm(me._form,function(data) {
                        settings.success&&settings.success.call(me,data)
                    });
                } else {
                    $.ajax({
                        url: settings.url,
                        type: 'post',
                        dataType: 'json',
                        data: me.serialize(),
                        success: function(data) {
                            settings.success&&settings.success.call(me,data)
                        },
                        error: function(xhr) {
                            settings.error&&settings.error.call(me,xhr);
                        }
                    });
                }
            }
        },
        reset: function() {
            var me=this;
            me._form[0].reset();
            me.validation.hideAll();

            return me;
        }
    };

    $.extend($.fn,{
        form: function(options) {
            return new Form(this,options)
        }
    });

    module.exports=Form;
});