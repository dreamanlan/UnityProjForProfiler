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
    feature("menu", "6.Tools/Parse Crash");
    feature("description", "just so so");
}
filter
{
    info = format("ParseCrash, il2cppsymtype:{0}, unitysymtype:{1}, crashtype:{2}", il2cppsymtype, unitysymtype, crashtype);
    1;
}
process
{	
    if(isnull(@syms) && !isnullorempty(il2cpp) || isnull(@syms2) && !isnullorempty(unity) || reloadsymbol){
        if(!isnullorempty(il2cpp)){
            if(il2cppsymtype==1){
                if(crashtype==1){
                    @syms=loadbuglyandroidsymbols(il2cpp);
                }else{
                    @syms=loadbuglyiossymbols(il2cpp);
                };            
            }else{
                @syms=loadidaprosymbols(il2cpp);
            };
        };
        if(!isnullorempty(unity)){
            if(unitysymtype==1){
                if(crashtype==1){
                    @syms2=loadbuglyandroidsymbols(unity);
                }else{
                    @syms2=loadbuglyiossymbols(unity);
                };            
            }else{
                @syms2=loadidaprosymbols(unity);
            };
        };
    };
    $lines=readalllines(crash);
    if(crashtype==1){
        if(!isnull(@syms)){
            $lines=mapbuglyandroidsymbols($lines,@syms,"libil2cpp");
        };
        if(!isnull(@syms2)){
            $lines=mapbuglyandroidsymbols($lines,@syms2,"libunity");
        };
    }else{
        if(!isnull(@syms)){
            $lines=mapbuglyiossymbols($lines,@syms,"qs");
        };
        if(!isnull(@syms2)){
            $lines=mapbuglyiossymbols($lines,@syms2,"qs");
        };
    };
    writealllines(crash_out,$lines);
};