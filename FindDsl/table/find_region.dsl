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
		$ix = findcellindex($header, "Type");
		$ix2 = findcellindex($header, "Para1");
		$ix3 = findcellindex($header, "Para2");

		$sid = 0;
		$sname="";
		$sbeginmsg="";
		$sendmsg="";
		$mid = 0;
		if(getcellnumeric(row, $ix)==4){
				$mid = getcellnumeric(row, $ix2);
				$sid = getcellnumeric(row, $ix3);
				$mrow = mission.GetRow(findrowindex(mission, 0, $mid));
				$mname = getcellstring($mrow, 1);
				$row = story.GetRow(findrowindex(story, 0, $sid));
				$sname = getcellstring($row,1);
				$sbeginmsg = getcellstring($row,2);
				$sendmsg = getcellstring($row,3);
		};

		order = row.RowIndex;
		if(isnullorempty(fields)){
				$v0 = row.GetLine();
		}else{
				$v1 = stringsplit(fields,[","]);
				$v2 = findcellindexes(sheet.GetRow(2), $v1);
				$v0 = rowtoline(row, 0, $v2);
		};
		if((sid=="" || $sid==sid) && (sname=="" || $sname.Contains(sname)) && (mname=="" || $mname.Contains(mname)) && $v0.Contains(contains) && (String.IsNullOrEmpty(notcontains1) || !$v0.Contains(notcontains1)) && (String.IsNullOrEmpty(notcontains2) || !$v0.Contains(notcontains2))){
				info = $mid+"\t"+$mname+"\t"+$sid+"\t"+$sname+"\t"+$sbeginmsg+"\t"+$sendmsg+"\t"+$v0;
		    value = 0;
		    1;
		}else{
		    0;
		};
};