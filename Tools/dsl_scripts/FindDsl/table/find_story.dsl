input
{
    string("table", "Mission/mission_story.txt")
    {
        file("txt");
    };
    string("encoding", "utf-8");
    int("skiprows", 0);
    string("fields", "");
	string("contains", "");
	string("notcontains1", "");
	string("notcontains2", "");
	string("mid", "");
	string("mname", "");
    table("mission", "../../Product/ServerTable/Mission/Main/mission.txt"){
        encoding("utf-8");
    };
    table("mevent", "../../Product/ServerTable/Mission/Main/mission_event.txt"){
        encoding("utf-8");
    };
	float("pathwidth",240){range(20,4096);};
	feature("source", "table");
	feature("menu", "9.Table/find story");
	feature("description", "just so so");
}
filter
{
    String = get_type("System.String");
    $header = mevent.GetRow(2);
	$ix = find_cell_index($header, "SubType");
	$ix2 = find_cell_index($header, "Target");

	$row = mevent.GetRow(find_row_index(mevent, $ix, 4, $ix2, get_cell_numeric(row, 0)));
	$mid = get_cell_string($row, 0);

	$mrow = mission.GetRow(find_row_index(mission, 0, $mid));
	$mname = get_cell_string($mrow, 1);

	order = row.RowIndex;
	if(is_null_or_empty(fields)){
		$v0 = row.GetLine();
	}else{
		$v1 = string_split(fields,[","]);
		$v2 = find_cell_indexes(sheet.GetRow(2), $v1);
		$v0 = row_to_line(row, 0, $v2);
	};
	if((mid=="" || $mid==mid) && (mname=="" || $mname.Contains(mname)) && $v0.Contains(contains) && (String.IsNullOrEmpty(notcontains1) || !$v0.Contains(notcontains1)) && (String.IsNullOrEmpty(notcontains2) || !$v0.Contains(notcontains2))){
		info = $mid+"\t"+$mname+"\t"+$v0;
	    value = 0;
	    1;
	}else{
	    0;
	};
};