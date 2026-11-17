input
{
    string("table", "")
    {
        file("csv");
    };
    string("encoding", "utf-8");
    int("skiprows", 0);
    int("ft",1)
    {
        toggle("fields1",0);
        toggle("fields2",1);
    };
    string("fields1", "product_version,crash_id,elapsed_time,crash_time,issue_id,is_root,ram,rom,is_new_issue");
    string("fields2", "product_version,issue_id,retrace_status,exception_type,process_name,crash_time,crash_id,type,device_id,elapsed_time,is_new_issue,ram,rom,cpu_name,is_root,cpu_type");
    stringlist("contains", "");
    stringlist("containsany", "");
    stringlist("notcontains", "");
    stringlist("notcontainsany", "");
    string("il2cpp", ""){
        file("*");
    };
    string("unity", ""){
        file("*");
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
    bool("mapsymbol",false);
    string("style", "itemlist"){
        popup(["itemlist", "grouplist"]);
    };
    float("pathwidth",160){range(20,4096);};
    feature("source", "table");
    feature("menu", "9.Table/find bugly table");
    feature("description", "just so so");
    feature("itemcommand", "$item.Group = $item.ScenePath");
    feature("groupcommand", "if($item.Items.Count>100){$item.Info=format(\"Count:{0},DevCount:{1},UserCount:{2}\",$item.Items.Count,calc_extra_object_field_count($item.Items,8),calc_extra_object_field_count($item.Items,16));$exinfo=$item.Items[0].ExtraObject[18]+'\\n\\n'+$item.Items[0].ExtraObject[19]+'\\n\\n'+string_replace(unescape_url($item.Items[0].ExtraObject[32]),';','\\n')+'\\n\\n'+unescape_url($item.Items[0].ExtraObject[34],'+')+'\\n\\n'+call_script('parseCrash',unescape_url($item.Items[0].ExtraObject[33],'+'));$item.ExtraList=new_extra_list($exinfo=>$exinfo);1;}else{0;};");
}
filter
{
    order = row.RowIndex;
    if(ft==0){
        $fields = fields1;
    }else{
        $fields = fields2;
    };
    if(is_null_or_empty($fields)){
        $v0 = row.GetLine();
        if(string_contains($v0,contains) && string_contains_any($v0,containsany) && string_not_contains($v0,notcontains) && string_not_contains_any($v0, notcontainsany)){
        info = $v0;
            value = 0;
            1;
        }else{
            0;
        };
    }else{
        $header = sheet.GetRow(0);
        $v1 = string_split($fields,[","]);
        $v2 = find_cell_indexes($header, $v1);
        $v3 = call_script("getfieldstring", $header, row, "kv");
        $v4 = parse_url_args($v3, "+");
        $v5 = hashtable_get($v4, "C03_B1");
        $v6 = hashtable_get($v4, "C03_B2");
        $v7 = hashtable_get($v4, "C03_B3");
        $v8 = hashtable_get($v4, "C03_B4");
        $v9 = call_script("getfieldstring", $header, row, "crash_id");
        $v10 = call_script("getfieldstring", $header, row, "issue_id");
        $v11 = call_script("getfieldstring", $header, row, "user");

        $f_kv = $v3;
        $ukv = string_replace(unescape_url($f_kv), ";", "\n");

        //device_id,hardware,os,cpu_name,exception_type,stack,exception_message
        $txt = call_script("getfieldstring", $header, row, "device_id");
        $device_id = unescape_url($txt, "+");

        $txt = call_script("getfieldstring", $header, row, "hardware");
        $hardware = unescape_url($txt, "+");

        $txt = call_script("getfieldstring", $header, row, "os");
        $uos = string_replace(unescape_url($txt, "+"), ",", "|");

        $txt = call_script("getfieldstring", $header, row, "cpu_name");
        $cpu_name = unescape_url($txt, "+");

        $txt = call_script("getfieldstring", $header, row, "exception_type");
        $exception_type = unescape_url($txt, "+");

        $txt = call_script("getfieldstring", $header, row, "stack");
        $f_stack = $txt;
        $cstack = unescape_url($txt, "+");
        if(is_null($cstack)){
            $cstack = "";
        };

        $txt = call_script("getfieldstring", $header, row, "exception_message");
        $f_exception = $txt;
        $exception_message = unescape_url($txt, "+");
        if(is_null($exception_message)){
            $exception_message = "";
        };

        if($f_kv=="kv" && $f_stack=="stack" && $f_exception=="exception_message"){
            $v0 = row_to_line(row, 0, $v2)+","+$v11+","+$device_id+","+$hardware+","+$uos+","+$cpu_name+","+$exception_type+",menpai,lvl,scene,hz,native,graphics,unknown,pss,vss,mono,"+$f_kv+","+$f_stack+","+$f_exception;
        }elseif(!is_null_or_empty($v5) && !is_null_or_empty($v6) && !is_null_or_empty($v7)){
            $v5 = string_replace($v5, "menpai_", "");
            $v5 = string_replace($v5, "level_", "");
            $v6 = string_replace($v6, "scene_", "");
            $v6 = string_replace($v6, "hz_", "");
            $v7 = string_replace($v7, "native_", "");
            $v7 = string_replace($v7, "graphics_", "");
            $v7 = string_replace($v7, "unknown_", "");
            $v8 = string_replace($v8, "pss_", "");
            $v8 = string_replace($v8, "vss_", "");
            $v8 = string_replace($v8, "mono_", "");
            $v0 = row_to_line(row, 0, $v2)+",uid:"+$v11+","+$device_id+","+$hardware+","+$uos+","+$cpu_name+","+$exception_type+","+$v5+","+$v6+","+$v7+","+$v8+","+$f_kv+","+$f_stack+","+$f_exception;
        }else{
            $v0 = row_to_line(row, 0, $v2)+",uid:"+$v11+","+$device_id+","+$hardware+","+$uos+","+$cpu_name+","+$exception_type+",,,,,,,,,,,"+$f_kv+","+$f_stack+","+$f_exception;
        };

        if(string_contains($v0,contains) && string_contains_any($v0,containsany) && string_not_contains($v0,notcontains) && string_not_contains_any($v0, notcontainsany)){
            info = $v0;
            value = 0;
            asset_path = $v9;
            scene_path = $v10;
            extraobject = string_split(info, [',']);
            $exinfo = $uos+"\n\n"+$ukv+"\n\n"+$exception_message+"\n\n"+call_script("parseCrash", $cstack);
            extra_list = new_extra_list($exinfo => $exinfo);
            1;
        }else{
            0;
        };
    };
};

script(getfieldstring)args($header, $row, $name)
{
    $ix = find_cell_index($header, $name);
    $str = get_cell_string($row, $ix);
    return($str);
};
script(parseCrash)args($crashStack)
{
    if(!mapsymbol)
    {
        return($crashStack);
    };
    if(is_null(@syms) && !is_null_or_empty(il2cpp) || is_null(@syms2) && !is_null_or_empty(unity) || reloadsymbol){
        if(!is_null_or_empty(il2cpp)){
            if(il2cppsymtype==1){
                if(crashtype==1){
                    @syms=load_bugly_android_symbols(il2cpp);
                }else{
                    @syms=load_bugly_ios_symbols(il2cpp);
                };
            }else{
                @syms=load_idapro_symbols(il2cpp);
            };
        };
        if(!is_null_or_empty(unity)){
            if(unitysymtype==1){
                if(crashtype==1){
                    @syms2=load_bugly_android_symbols(unity);
                }else{
                    @syms2=load_bugly_ios_symbols(unity);
                };
            }else{
                @syms2=load_idapro_symbols(unity);
            };
        };
    };

    $lines=string_split($crashStack, ["\n"]);
    if(crashtype==1){
        if(!is_null(@syms)){
            $lines=map_bugly_android_symbols($lines,@syms,"libil2cpp","0 ");
        };
        if(!is_null(@syms2)){
            $lines=map_bugly_android_symbols($lines,@syms2,"libunity","0 ");
        };
    }else{
        if(!is_null(@syms)){
            $lines=map_bugly_ios_symbols($lines,@syms,"libil2cpp","0 ");
        };
        if(!is_null(@syms2)){
            $lines=map_bugly_ios_symbols($lines,@syms2,"libunity","0 ");
        };
    };
    $txt=string_join("\r\n", $lines);
    return($txt);
};