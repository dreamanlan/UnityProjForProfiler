input("ParseCrash")
{
    string("il2cpp", ""){
        file("*");
    };
    string("unity", ""){
        file("*");
    };
    string("crash", ""){
        file("txt");
    };
    string("crash_out", ""){
        file("txt");
    };
    int("il2cppsymtype",1){
        toggle(["bugly","idapro"],[1,2]);
    };
    int("unitysymtype",2){
        toggle(["bugly","idapro"],[1,2]);
    };
    int("crashtype",1){
        toggle(["android","iOS"],[1,2]);
    };
    bool("reloadsymbol",false);
    feature("source", "list");
    feature("menu", "1.Tools/Parse Crash");
    feature("description", "just so so");
}
filter
{
    info = format("ParseCrash, il2cppsymtype:{0}, unitysymtype:{1}, crashtype:{2}", il2cppsymtype, unitysymtype, crashtype);
    1;
}
process
{
    if(is_null(@syms) && !is_null_or_empty(il2cpp) || is_null(@syms2) && !is_null_or_empty(unity) || reloadsymbol){
        if(!is_null_or_empty(il2cpp)){
            if(il2cppsymtype==1){
                if(crashtype==1){
                    @syms=loadbugly_android_symbols(il2cpp);
                }else{
                    @syms=loadbuglyios_symbols(il2cpp);
                };
            }else{
                @syms=loadidapro_symbols(il2cpp);
            };
        };
        if(!is_null_or_empty(unity)){
            if(unitysymtype==1){
                if(crashtype==1){
                    @syms2=loadbugly_android_symbols(unity);
                }else{
                    @syms2=loadbuglyios_symbols(unity);
                };
            }else{
                @syms2=loadidapro_symbols(unity);
            };
        };
    };
    $lines=read_all_lines(crash);
    if(crashtype==1){
        if(!is_null(@syms)){
            $lines=map_bugly_android_symbols($lines,@syms,"libil2cpp");
        };
        if(!is_null(@syms2)){
            $lines=map_bugly_android_symbols($lines,@syms2,"libunity");
        };
    }else{
        if(!is_null(@syms)){
            $lines=map_buglyios_symbols($lines,@syms,"qs");
        };
        if(!is_null(@syms2)){
            $lines=map_buglyios_symbols($lines,@syms2,"qs");
        };
    };
    write_all_lines(crash_out,$lines);
};