input
{
    string("table", "group.csv")
    {
        file("csv");
    };
    string("encoding", "gb2312");
    int("skiprows", 1);
	string("contains", "");
	string("notcontains1", "");
	string("notcontains2", "");
	string("startswith", "");
	string("endswith", "");
	int("mincount",2);
	float("pathwidth",240){range(20,4096);};
	feature("source", "table");
	feature("menu", "9.Table/find memory group table");
	feature("description", "just so so");
}
filter
{
    String = gettype("System.String");
	order = row.RowIndex;
	$v0 = get_cell_string(row, 0);
	$v1 = get_cell_numeric(row, 1);
	if($v0.Contains(contains) && (String.IsNullOrEmpty(notcontains1) || !$v0.Contains(notcontains1)) && (String.IsNullOrEmpty(notcontains2) || !$v0.Contains(notcontains2)) && (String.IsNullOrEmpty(startswith) || $v0.StartsWith(startswith)) && (String.IsNullOrEmpty(endswith) || $v0.EndsWith(endswith)) && $v1>=mincount){
		info = format("type:{0} count:{1} size:{2}",
        get_cell_value(row, 0),
        get_cell_value(row, 1),
        get_cell_value(row, 2)
        );
        value = get_cell_numeric(row, 3);
        1;
	}else{
        0;
	};
};