input
{
	string("table", "")
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
	String = get_type("System.String");
	order = row.RowIndex;
	if(is_null_or_empty(fields)){
		$v0 = row.GetLine();
	}else{
		$v1 = string_split(fields,[","]);
		$v2 = find_cell_indexes(sheet.GetRow(2), $v1);
		$v0 = row_to_line(row, 0, $v2);
	};
	if($v0.Contains(contains) && (String.IsNullOrEmpty(notcontains1) || !$v0.Contains(notcontains1)) && (String.IsNullOrEmpty(notcontains2) || !$v0.Contains(notcontains2))){
		info = $v0;
		value = 0;
		1;
	}else{
		0;
	};
};