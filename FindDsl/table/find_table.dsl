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
	float("pathwidth",240){range(20,4096);};
	feature("source", "table");
	feature("menu", "9.Table/find table");
	feature("description", "just so so");
}
filter
{
  String = gettype("System.String");
	order = row.RowIndex;
	if(isnullorempty(fields)){
		$v0 = row.GetLine();
	}else{
		$v1 = stringsplit(fields,[","]);
		$v2 = findcellindexes(sheet.GetRow(2), $v1);
		$v0 = rowtoline(row, 0, $v2);
	};
	if($v0.Contains(contains) && (String.IsNullOrEmpty(notcontains1) || !$v0.Contains(notcontains1)) && (String.IsNullOrEmpty(notcontains2) || !$v0.Contains(notcontains2))){
		info = $v0;
	  value = 0;
	  1;
	}else{
	  0;
	};
};