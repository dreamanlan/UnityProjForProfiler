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
		var(0) = row.GetLine();
	}else{
		var(1) = stringsplit(fields,[","]);
		var(2) = findcellindexes(sheet.GetRow(2), var(1));
		var(0) = rowtoline(row, 0, var(2));
	};
	if(var(0).Contains(contains) && (String.IsNullOrEmpty(notcontains1) || !var(0).Contains(notcontains1)) && (String.IsNullOrEmpty(notcontains2) || !var(0).Contains(notcontains2))){
		info = var(0);
	  value = 0;
	  1;
	}else{
	  0;
	};
};