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
    String = get_type("System.String");
    $header = mission_event.GetRow(2);
    $ix = find_cell_index($header, "SubType");
    $ix2 = find_cell_index($header, "Target");
    $ix3 = find_cell_index($header, "ScriptFunc");
    $ix4 = find_cell_index($header, "Params");

    $mid = get_cell_string(row, 0);
    $erow = mission_event.GetRow(find_row_index(mission_event, 0, $mid));

    $sid = 0;
    $sname="";
    $sbeginmsg="";
    $sendmsg="";
    $scpfunc="";
    $scparg="";
    if(get_cell_numeric($erow, $ix)==4){
            $sid = get_cell_numeric($erow, $ix2);
            $row = story.GetRow(find_row_index(story, 0, $sid));
            $sname = get_cell_string($row,1);
            $sbeginmsg = get_cell_string($row,2);
            $sendmsg = get_cell_string($row,3);
    }elseif(get_cell_numeric($erow, $ix)==19){
            $scpfunc = get_cell_string($erow, $ix3);
            $scparg = get_cell_string($erow, $ix4);
    };

    order = row.RowIndex;
    if(is_null_or_empty(fields)){
            $v0 = row.GetLine();
    }else{
            $v1 = string_split(fields,[","]);
            $v2 = find_cell_indexes(sheet.GetRow(2), $v1);
            $v0 = row_to_line(row, 0, $v2);
    };
    if((sid=="" || $sid==sid) && (sname=="" || $sname.Contains(sname)) && $v0.Contains(contains) && (String.IsNullOrEmpty(notcontains1) || !$v0.Contains(notcontains1)) && (String.IsNullOrEmpty(notcontains2) || !$v0.Contains(notcontains2))){
            info = $sid+"\t"+$sname+"\t"+$sbeginmsg+"\t"+$sendmsg+"\t"+$scpfunc+"\t"+$scparg+"\t"+$v0;
        value = 0;
        1;
    }else{
        0;
    };
};