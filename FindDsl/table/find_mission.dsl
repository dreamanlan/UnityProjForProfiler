input
{   
    string("table", "Mission/Main/mission.txt"){
        file("txt");
    };
    string("encoding", "utf-8");
    int("skiprows", 0);
    string("fields", "");
    string("contains", "");
    string("notcontains1", "");
    string("notcontains2", "");
    string("sid", "");
    string("sname", "");
    table("mission_event", "../../Product/ServerTable/Mission/Main/mission_event.txt"){
        encoding("utf-8");
    };
    table("story", "../../Product/ServerTable/Mission/mission_story.txt"){
        encoding("utf-8");
    };
	float("pathwidth",240){range(20,4096);};
    feature("source", "table");
    feature("menu", "9.Table/find mission");
    feature("description", "just so so");
}
filter
{
    String = gettype("System.String");
    $header = mission_event.GetRow(2);
    $ix = findcellindex($header, "SubType");
    $ix2 = findcellindex($header, "Target");        
    $ix3 = findcellindex($header, "ScriptFunc");
    $ix4 = findcellindex($header, "Params");
    
    $mid = getcellstring(row, 0);  
    $erow = mission_event.GetRow(findrowindex(mission_event, 0, $mid));
        
    $sid = 0;
    $sname="";
    $sbeginmsg="";
    $sendmsg="";
    $scpfunc="";
    $scparg="";
    if(getcellnumeric($erow, $ix)==4){
            $sid = getcellnumeric($erow, $ix2);
            $row = story.GetRow(findrowindex(story, 0, $sid));
            $sname = getcellstring($row,1);
            $sbeginmsg = getcellstring($row,2);
            $sendmsg = getcellstring($row,3);
    }elseif(getcellnumeric($erow, $ix)==19){
            $scpfunc = getcellstring($erow, $ix3);
            $scparg = getcellstring($erow, $ix4);
    };
        
    order = row.RowIndex;
    if(isnullorempty(fields)){
            var(0) = row.GetLine();
    }else{
            var(1) = stringsplit(fields,[","]);
            var(2) = findcellindexes(sheet.GetRow(2), var(1));
            var(0) = rowtoline(row, 0, var(2));
    };
    if((sid=="" || $sid==sid) && (sname=="" || $sname.Contains(sname)) && var(0).Contains(contains) && (String.IsNullOrEmpty(notcontains1) || !var(0).Contains(notcontains1)) && (String.IsNullOrEmpty(notcontains2) || !var(0).Contains(notcontains2))){
            info = $sid+"\t"+$sname+"\t"+$sbeginmsg+"\t"+$sendmsg+"\t"+$scpfunc+"\t"+$scparg+"\t"+var(0);
        value = 0;
        1;
    }else{
        0;
    };
};