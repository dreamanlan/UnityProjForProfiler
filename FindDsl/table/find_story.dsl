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
    String = gettype("System.String");
    $header = mevent.GetRow(2);
	$ix = findcellindex($header, "SubType");
	$ix2 = findcellindex($header, "Target");

	$row = mevent.GetRow(findrowindex(mevent, $ix, 4, $ix2, getcellnumeric(row, 0)));
	$mid = getcellstring($row, 0);

	$mrow = mission.GetRow(findrowindex(mission, 0, $mid));
	$mname = getcellstring($mrow, 1);

	order = row.RowIndex;
	if(isnullorempty(fields)){
		$v0 = row.GetLine();
	}else{
		$v1 = stringsplit(fields,[","]);
		$v2 = findcellindexes(sheet.GetRow(2), $v1);
		$v0 = rowtoline(row, 0, $v2);
	};
	if((mid=="" || $mid==mid) && (mname=="" || $mname.Contains(mname)) && $v0.Contains(contains) && (String.IsNullOrEmpty(notcontains1) || !$v0.Contains(notcontains1)) && (String.IsNullOrEmpty(notcontains2) || !$v0.Contains(notcontains2))){
		info = $mid+"\t"+$mname+"\t"+$v0;
	    value = 0;
	    1;
	}else{
	    0;
	};
};