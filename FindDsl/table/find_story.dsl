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
		var(0) = row.GetLine();
	}else{
		var(1) = stringsplit(fields,[","]);
		var(2) = findcellindexes(sheet.GetRow(2), var(1));
		var(0) = rowtoline(row, 0, var(2));
	};
	if((mid=="" || $mid==mid) && (mname=="" || $mname.Contains(mname)) && var(0).Contains(contains) && (String.IsNullOrEmpty(notcontains1) || !var(0).Contains(notcontains1)) && (String.IsNullOrEmpty(notcontains2) || !var(0).Contains(notcontains2))){
		info = $mid+"\t"+$mname+"\t"+var(0);
	    value = 0;
	    1;
	}else{
	    0;
	};
};