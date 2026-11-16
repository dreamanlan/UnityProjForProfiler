input
{
    string("table", "Mission/qs_regions.txt")
    {
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
		string("mname", "");
    table("mission", "../../Product/ServerTable/Mission/Main/mission.txt"){
        encoding("utf-8");
    };
    table("story", "../../Product/ServerTable/Mission/mission_story.txt"){
        encoding("utf-8");
    };
	float("pathwidth",240){range(20,4096);};
		feature("source", "table");
		feature("menu", "9.Table/find region");
		feature("description", "just so so");
}
filter
{
    String = gettype("System.String");
    $header = sheet.GetRow(2);
		$ix = find_cell_index($header, "Type");
		$ix2 = find_cell_index($header, "Para1");
		$ix3 = find_cell_index($header, "Para2");

		$sid = 0;
		$sname="";
		$sbeginmsg="";
		$sendmsg="";
		$mid = 0;
		if(get_cell_numeric(row, $ix)==4){
				$mid = get_cell_numeric(row, $ix2);
				$sid = get_cell_numeric(row, $ix3);
				$mrow = mission.GetRow(find_row_index(mission, 0, $mid));
				$mname = get_cell_string($mrow, 1);
				$row = story.GetRow(find_row_index(story, 0, $sid));
				$sname = get_cell_string($row,1);
				$sbeginmsg = get_cell_string($row,2);
				$sendmsg = get_cell_string($row,3);
		};

		order = row.RowIndex;
		if(is_null_or_empty(fields)){
				$v0 = row.GetLine();
		}else{
				$v1 = string_split(fields,[","]);
				$v2 = find_cell_indexes(sheet.GetRow(2), $v1);
				$v0 = row_to_line(row, 0, $v2);
		};
		if((sid=="" || $sid==sid) && (sname=="" || $sname.Contains(sname)) && (mname=="" || $mname.Contains(mname)) && $v0.Contains(contains) && (String.IsNullOrEmpty(notcontains1) || !$v0.Contains(notcontains1)) && (String.IsNullOrEmpty(notcontains2) || !$v0.Contains(notcontains2))){
				info = $mid+"\t"+$mname+"\t"+$sid+"\t"+$sname+"\t"+$sbeginmsg+"\t"+$sendmsg+"\t"+$v0;
		    value = 0;
		    1;
		}else{
		    0;
		};
};