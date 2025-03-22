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
    feature("groupcommand", "if($item.Items.Count>100){$item.Info=format(\"Count:{0},DevCount:{1},UserCount:{2}\",$item.Items.Count,calcextraobjectfieldcount($item.Items,8),calcextraobjectfieldcount($item.Items,16));$exinfo=$item.Items[0].ExtraObject[18]+'\\n\\n'+$item.Items[0].ExtraObject[19]+'\\n\\n'+stringreplace(unescapeurl($item.Items[0].ExtraObject[32]),';','\\n')+'\\n\\n'+unescapeurl($item.Items[0].ExtraObject[34],'+')+'\\n\\n'+callscript('parseCrash',unescapeurl($item.Items[0].ExtraObject[33],'+'));$item.ExtraList=newextralist($exinfo=>$exinfo);1;}else{0;};");
}
filter
{
    order = row.RowIndex;
    if(ft==0){
        $fields = fields1;
    }else{
        $fields = fields2;
    };
    if(isnullorempty($fields)){
        $v0 = row.GetLine();
        if(stringcontains($v0,contains) && stringcontainsany($v0,containsany) && stringnotcontains($v0,notcontains) && stringnotcontainsany($v0, notcontainsany)){
        info = $v0;
           value = 0;
           1;
        }else{
           0;
        };
    }else{
        $header = sheet.GetRow(0);
        $v1 = stringsplit($fields,[","]);
        $v2 = findcellindexes($header, $v1);
        $v3 = callscript("getfieldstring", $header, row, "kv");
        $v4 = parseurlargs($v3, "+");
        $v5 = hashtableget($v4, "C03_B1");
        $v6 = hashtableget($v4, "C03_B2");
        $v7 = hashtableget($v4, "C03_B3");
        $v8 = hashtableget($v4, "C03_B4");
        $v9 = callscript("getfieldstring", $header, row, "crash_id");
        $v10 = callscript("getfieldstring", $header, row, "issue_id");
        $v11 = callscript("getfieldstring", $header, row, "user");

        $f_kv = $v3;
        $ukv = stringreplace(unescapeurl($f_kv), ";", "\n");

        //device_id,hardware,os,cpu_name,exception_type,stack,exception_message
        $txt = callscript("getfieldstring", $header, row, "device_id");
        $device_id = unescapeurl($txt, "+");

        $txt = callscript("getfieldstring", $header, row, "hardware");
        $hardware = unescapeurl($txt, "+");

        $txt = callscript("getfieldstring", $header, row, "os");
        $uos = stringreplace(unescapeurl($txt, "+"), ",", "|");

        $txt = callscript("getfieldstring", $header, row, "cpu_name");
        $cpu_name = unescapeurl($txt, "+");

        $txt = callscript("getfieldstring", $header, row, "exception_type");
        $exception_type = unescapeurl($txt, "+");

        $txt = callscript("getfieldstring", $header, row, "stack");
        $f_stack = $txt;
        $cstack = unescapeurl($txt, "+");
        if(isnull($cstack)){
            $cstack = "";
        };

        $txt = callscript("getfieldstring", $header, row, "exception_message");
        $f_exception = $txt;
        $exception_message = unescapeurl($txt, "+");
        if(isnull($exception_message)){
            $exception_message = "";
        };

        if($f_kv=="kv" && $f_stack=="stack" && $f_exception=="exception_message"){
            $v0 = rowtoline(row, 0, $v2)+","+$v11+","+$device_id+","+$hardware+","+$uos+","+$cpu_name+","+$exception_type+",menpai,lvl,scene,hz,native,graphics,unknown,pss,vss,mono,"+$f_kv+","+$f_stack+","+$f_exception;
        }elseif(!isnullorempty($v5) && !isnullorempty($v6) && !isnullorempty($v7)){
            $v5 = stringreplace($v5, "menpai_", "");
            $v5 = stringreplace($v5, "level_", "");
            $v6 = stringreplace($v6, "scene_", "");
            $v6 = stringreplace($v6, "hz_", "");
            $v7 = stringreplace($v7, "native_", "");
            $v7 = stringreplace($v7, "graphics_", "");
            $v7 = stringreplace($v7, "unknown_", "");
            $v8 = stringreplace($v8, "pss_", "");
            $v8 = stringreplace($v8, "vss_", "");
            $v8 = stringreplace($v8, "mono_", "");
            $v0 = rowtoline(row, 0, $v2)+",uid:"+$v11+","+$device_id+","+$hardware+","+$uos+","+$cpu_name+","+$exception_type+","+$v5+","+$v6+","+$v7+","+$v8+","+$f_kv+","+$f_stack+","+$f_exception;
        }else{
            $v0 = rowtoline(row, 0, $v2)+",uid:"+$v11+","+$device_id+","+$hardware+","+$uos+","+$cpu_name+","+$exception_type+",,,,,,,,,,,"+$f_kv+","+$f_stack+","+$f_exception;
        };

        if(stringcontains($v0,contains) && stringcontainsany($v0,containsany) && stringnotcontains($v0,notcontains) && stringnotcontainsany($v0, notcontainsany)){
            info = $v0;
            value = 0;
            assetpath = $v9;
            scenepath = $v10;
            extraobject = stringsplit(info, [',']);
            $exinfo = $uos+"\n\n"+$ukv+"\n\n"+$exception_message+"\n\n"+callscript("parseCrash", $cstack);
            extralist = newextralist($exinfo => $exinfo);
            1;
        }else{
            0;
        };
    };
};

script(getfieldstring)args($header, $row, $name)
{
    $ix = findcellindex($header, $name);
    $str = getcellstring($row, $ix);
    return($str);
};
script(parseCrash)args($crashStack)
{
    if(!mapsymbol)
    {
        return($crashStack);
    };
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

    $lines=stringsplit($crashStack, ["\n"]);
    if(crashtype==1){
        if(!isnull(@syms)){
            $lines=mapbuglyandroidsymbols($lines,@syms,"libil2cpp","0 ");
        };
        if(!isnull(@syms2)){
            $lines=mapbuglyandroidsymbols($lines,@syms2,"libunity","0 ");
        };
    }else{
        if(!isnull(@syms)){
            $lines=mapbuglyiossymbols($lines,@syms,"libil2cpp","0 ");
        };
        if(!isnull(@syms2)){
            $lines=mapbuglyiossymbols($lines,@syms2,"libunity","0 ");
        };
    };
    $txt=stringjoin("\r\n", $lines);
    return($txt);
};