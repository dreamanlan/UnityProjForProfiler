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
    feature("groupcommand", "$item.Info=format(\"{0},DevCount:{1},UserCount:{2}\",$item.Info,calcextraobjectfieldcount($item.Items,8),calcextraobjectfieldcount($item.Items,16));$item.Items.Count>100;");
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
        var(0) = row.GetLine();
        if(stringcontains(var(0),contains) && stringcontainsany(var(0),containsany) && stringnotcontains(var(0),notcontains) && stringnotcontainsany(var(0), notcontainsany)){
        info = var(0);
           value = 0;
           1;
        }else{
           0;
        };
    }else{	    
        $header = sheet.GetRow(0);
        var(1) = stringsplit($fields,[","]);
        var(2) = findcellindexes($header, var(1));
        var(3) = callscript("getfieldstring", $header, row, "kv");
        var(4) = parseurlargs(var(3), "+");
        var(5) = hashtableget(var(4), "C03_B1");
        var(6) = hashtableget(var(4), "C03_B2");
        var(7) = hashtableget(var(4), "C03_B3");
        var(8) = hashtableget(var(4), "C03_B4");
        var(9) = callscript("getfieldstring", $header, row, "crash_id");
        var(10) = callscript("getfieldstring", $header, row, "issue_id");
        var(11) = callscript("getfieldstring", $header, row, "user");

        $f_kv = var(3);
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
            var(0) = rowtoline(row, 0, var(2))+","+var(11)+","+$device_id+","+$hardware+","+$uos+","+$cpu_name+","+$exception_type+",menpai,lvl,scene,hz,native,graphics,unknown,pss,vss,mono,"+$f_kv+","+$f_stack+","+$f_exception;
        }elseif(!isnullorempty(var(5)) && !isnullorempty(var(6)) && !isnullorempty(var(7))){
            var(5) = stringreplace(var(5), "menpai_", "");
            var(5) = stringreplace(var(5), "level_", "");
            var(6) = stringreplace(var(6), "scene_", "");
            var(6) = stringreplace(var(6), "hz_", "");
            var(7) = stringreplace(var(7), "native_", "");
            var(7) = stringreplace(var(7), "graphics_", "");
            var(7) = stringreplace(var(7), "unknown_", "");
            var(8) = stringreplace(var(8), "pss_", "");
            var(8) = stringreplace(var(8), "vss_", "");
            var(8) = stringreplace(var(8), "mono_", "");
            var(0) = rowtoline(row, 0, var(2))+",uid:"+var(11)+","+$device_id+","+$hardware+","+$uos+","+$cpu_name+","+$exception_type+","+var(5)+","+var(6)+","+var(7)+","+var(8)+","+$f_kv+","+$f_stack+","+$f_exception;
        }else{
            var(0) = rowtoline(row, 0, var(2))+",uid:"+var(11)+","+$device_id+","+$hardware+","+$uos+","+$cpu_name+","+$exception_type+",,,,,,,,,,,"+$f_kv+","+$f_stack+","+$f_exception;
        };

        if(stringcontains(var(0),contains) && stringcontainsany(var(0),containsany) && stringnotcontains(var(0),notcontains) && stringnotcontainsany(var(0), notcontainsany)){
            info = var(0);
            value = 0;
            assetpath = var(9);
            scenepath = var(10);
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