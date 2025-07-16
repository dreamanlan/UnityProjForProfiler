input("MapSymbols")
{
    string("il2cpp", ""){
        file("*");
    };
    string("unity", ""){
        file("*");
    };
    string("gcalloc", ""){
        file("txt");
    };
    string("libil2cpp_start", "0");
    string("libil2cpp_end", "0");
    string("libunity_start", "0");
    string("libunity_end", "0");
    int("il2cppsymtype",1){
        toggle(["bugly","idapro"],[1,2]);
    };
    int("unitysymtype",2){
        toggle(["bugly","idapro"],[1,2]);
    };
    bool("reloadmap",false);
	feature("source", "list");
	feature("menu", "1.Tools/Map Symbols");
	feature("description", "just so so");
}
process
{
    if(isnull(@syms) && !isnullorempty(il2cpp) || isnull(@syms2) && !isnullorempty(unity) || reloadsymbol){
        if(!isnullorempty(il2cpp)){
            if(il2cppsymtype==1){
                @syms=loadbuglyandroidsymbols(il2cpp);
            }else{
                @syms=loadidaprosymbols(il2cpp);
            };
        };
        if(!isnullorempty(unity)){
            if(unitysymtype==1){
                @syms2=loadbuglyandroidsymbols(unity);
            }else{
                @syms2=loadidaprosymbols(unity);
            };
        };
    };
    $lines=readalllines(gcalloc);
    if(!isnull(@syms)){
        $lines=mapmyhooksymbols($lines,hex2ulong(libil2cpp_start),hex2ulong(libil2cpp_end),@syms,"libil2cpp");
    };
    if(!isnull(@syms2)){
        $lines=mapmyhooksymbols($lines,hex2ulong(libunity_start),hex2ulong(libunity_end),@syms2,"libunity");
    };
    $dir = getdirectoryname(gcalloc);
    $filename = getfilenamewithoutextension(gcalloc);
    $file = combinepath($dir,$filename+"_with_sym.txt");
    writealllines($file,$lines);
};