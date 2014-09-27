define(function (require,exports,module) {
    var $=require('jquery');

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
        "tmpl": {
            _default: { $2: "null" },
            open: "if($notnull_1){__=__.concat($item.nest($1,$2));}"
        },
        "each": {
            _default: { $2: "$index, $value" },
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
            // Unecoded expression evaluation.
            open: "if($notnull_1){__.push($1a);}"
        },
        "=": {
            // Encoded expression evaluation. Abbreviated form is ${}.
            _default: { $1: "$data" },
            open: "if($notnull_1){__.push($.encode($1a));}"
        },
        "!": {
            // Comment tag. Skipped by parser
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
                console.group("Exception");
                console.error(markup);
                console.error('Unknown tag: ',type);
                console.error(all);
                console.groupEnd("Exception");
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
                // Support for target being things like a.toLowerCase();
                // In that case don't call with template item as 'this' pointer. Just evaluate...
                expr=parens?(target.indexOf(".")> -1?target+unescape(parens):("("+target+").call($item"+args)):target;
                exprAutoFnDetect=parens?expr:"(typeof("+target+")==='function'?("+target+").call($item):("+target+"))";
            } else {
                exprAutoFnDetect=expr=def.$1||"null";
            }
            fnargs=unescape(fnargs);
            return "');"+
                   tag[slash?"close":"open"]
                           .split("$notnull_1").join(target?"typeof("+target+")!=='undefined' && ("+target+")!=null":"true")
                           .split("$1a").join(exprAutoFnDetect)
                           .split("$1").join(expr)
                           .split("$2").join(fnargs||def.$2||"")+
                   "__.push('";
        };

        var depreciated_parse=function () {
            if(tmpl_tag[arguments[2]]) {
                console.group("Depreciated");
                console.info(markup);
                console.info('Markup has old style indicators, use {% %} instead of {{ }}');
                console.info(arguments[0]);
                console.groupEnd("Depreciated");
                return parse_tag.apply(this,arguments);
            } else {
                return "');__.push('{{"+arguments[2]+"}}');__.push('";
            }
        };

        // Use the variable __ to hold a string array while building the compiled template. (See https://github.com/jquery/jquery-tmpl/issues#issue/10).
        // Introduce the data as local variables using with(){}
        var parsed_markup_data="var $=$,call,__=[],$data=$item.data; with($data){__.push('";

        // Convert the template into pure JavaScript
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

    $.extend({
        encode: function (text) {
            // Do HTML encoding replacing < > & and ' and " by corresponding entities.
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
    },
    template=function (html,data) {
        var result;
        if($.isArray(data)) {
            result=[];
            $.each(data,function (i,item) {
                result.push(tmpl(html,item).join(''));
            });
        } else
            result=tmpl(html,data);
        return result.join('');
    };

    $.fn.tmpl=function (data) {
        return $(template(this[0].innerHTML,data));
    };

    module.exports=template;
});