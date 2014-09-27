define(['$'],function (require,exports,module) {

    var $=require('$');

    var regex={
        sq_escape: /([\\'])/g,
        sq_unescape: /\\'/g,
        dq_unescape: /\\\\/g,
        nl_strip: /[\r\t\n]/g,
        shortcut_replace: /\$\{([^\}]*)\}/g,
        lang_parse: /\{\%(\/?)(\w+|.)(?:\(((?:[^\%]|\%(?!\}))*?)?\))?(?:\s+(.*?)?)?(\(((?:[^\%]|\%(?!\}))*?)\))?\s*\%\}/g,
        old_lang_parse: /\{\{(\/?)(\w+|.)(?:\(((?:[^\}]|\}(?!\}))*?)?\))?(?:\s+(.*?)?)?(\(((?:[^\}]|\}(?!\}))*?)\))?\s*\}\}/g,
        template_anotate: /(<\w+)(?=[\s>])(?![^>]*_tmplitem)([^>]*)/g,
        text_only_template: /^\s*([^<\s][^<]*)?(<[\w\W]+>)([^>]*[^>\s])?\s*$/,
        html_expr: /^[^<]*(<[\w\W]+>)[^>]*$|\{\{\! |\{\%! /,
        last_word: /\w$/
    };

    var tmpl_tag={
        "each": {
            _default: {
                $2: "$index, $value"
            },
            open: "if($notnull_1){$.each($1a,function($2){with(this){",
            close: "}});}"
        },
        "if": {
            open: "if(($notnull_1) && $1a){",
            close: "}"
        },
        "else": {
            open: "}else{"
        },
        "elif": {
            open: "}else if(($notnull_1) && $1a){"
        },
        "elseif": {
            open: "}else if(($notnull_1) && $1a){"
        },
        "html": {
            open: "if($notnull_1){__.push($1a);}"
        },
        "break": {
            open: "return false;"
        },
        "=": {
            _default: {
                $1: "$data"
            },
            open: "if($notnull_1){__.push($.encode($1a));}"
        },
        "!": {
            open: ""
        }
    };

    function buildTmplFn(markup) {
        var parse_tag=function (all,slash,type,fnargs,target,parens,args) {
            if(!type) {
                return "');__.push('";
            }

            var tag=tmpl_tag[type],def,expr,exprAutoFnDetect;
            if(!tag) {
                return "');__.push('";
            }
            def=tag._default||[];
            if(parens&&!regex.last_word.test(target)) {
                target+=parens;
                parens="";
            }
            if(target) {
                target=unescape(target);
                args=args?(","+unescape(args)+")"):(parens?")":"");
                expr=parens?(target.indexOf(".")> -1?target+unescape(parens):("("+target+").call($item"+args)):target;
                exprAutoFnDetect=parens?expr:"(typeof("+target+")==='function'?("+target+").call($item):("+target+"))";
            } else {
                exprAutoFnDetect=expr=def.$1||"null";
            }
            fnargs=unescape(fnargs);
            return "');"+tag[slash?"close":"open"].split("$notnull_1").join(target?"typeof("+target+")!=='undefined' && ("+target+")!=null":"true").split("$1a").join(exprAutoFnDetect).split("$1").join(expr).split("$2").join(fnargs||def.$2||"")+"__.push('";
        };

        var depreciated_parse=function () {
            if(tmpl_tag[arguments[2]]) {
                return parse_tag.apply(this,arguments);
            } else {
                return "');__.push('{{"+arguments[2]+"}}');__.push('";
            }
        };
        var parsed_markup_data="var $=$,call,__=[],$data=$item.data; with($data){__.push('";

        var parsed_markup=$.trim(markup);
        parsed_markup=parsed_markup.replace(regex.sq_escape,"\\$1");
        parsed_markup=parsed_markup.replace(regex.nl_strip," ");
        parsed_markup=parsed_markup.replace(regex.shortcut_replace,"{%= $1%}");
        parsed_markup=parsed_markup.replace(regex.lang_parse,parse_tag);
        parsed_markup=parsed_markup.replace(regex.old_lang_parse,depreciated_parse);
        parsed_markup_data+=parsed_markup;

        parsed_markup_data+="');}return __;";

        return new Function("$","$item",parsed_markup_data);
    }

    $.extend($,{
        encode: function (text) {
            return (""+text).split("<").join("&lt;").split(">").join("&gt;").split('"').join("&#34;").split("'").join("&#39;");
        }
    });

    var tmpl=function (html,data) {
        var fn=buildTmplFn(html);
        return fn($,{
            data: data,
            nest: function (s,d) {
                return tmpl(s,d);
            }
        });
    };

    $.fn.tmpl=function (data) {
        return $(tmpl(this[0].innerHTML,data).join(''));
    };

    $.tmpl=module.exports=function(html,data) {
        if(typeof data==='undefined') {
            var fn=buildTmplFn(html);

            return function(data) {
                fn($,{
                    data: data,
                    nest: function(s,d) {
                        return tmpl(s,d);
                    }
                }).join('');
            }
        }
        //不返回$()，因为如果html是“div”，将返回所有div
        return tmpl(html,data).join('');
    };
});

