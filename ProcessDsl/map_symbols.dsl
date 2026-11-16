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
    if(is_null(@syms) && !is_null_or_empty(il2cpp) || is_null(@syms2) && !is_null_or_empty(unity) || reloadsymbol){
        if(!is_null_or_empty(il2cpp)){
            if(il2cppsymtype==1){
                @syms=loadbugly_android_symbols(il2cpp);
            }else{
                @syms=loadidapro_symbols(il2cpp);
            };
        };
        if(!is_null_or_empty(unity)){
            if(unitysymtype==1){
                @syms2=loadbugly_android_symbols(unity);
            }else{
                @syms2=loadidapro_symbols(unity);
            };
        };
    };
    $lines=read_all_lines(gcalloc);
    if(!is_null(@syms)){
        $lines=map_myhook_symbols($lines,hex_2_ulong(libil2cpp_start),hex_2_ulong(libil2cpp_end),@syms,"libil2cpp");
    };
    if(!is_null(@syms2)){
        $lines=map_myhook_symbols($lines,hex_2_ulong(libunity_start),hex_2_ulong(libunity_end),@syms2,"libunity");
    };
    $dir = get_directory_name(gcalloc);
    $filename = get_file_name_without_extension(gcalloc);
    $file = combine_path($dir,$filename+"_with_sym.txt");
    write_all_lines($file,$lines);
};