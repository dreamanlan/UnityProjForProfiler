input
{
    string("table", ""){
        file("csv");
    };
    table("table2", ""){
        encoding("utf-8");
        file("csv");
    };
    script("dict2","buildHashtable");
    string("encoding", "utf-8");
    int("skiprows", 1);
    stringlist("classcontainsany", "");
    stringlist("namecontainsany", "");
    stringlist("classnotcontains", "");
    stringlist("namenotcontains", "");
	int("resultopt",1){
		toggle(["all","onlyalone","onlynotalone"],[1,2,3]);
	};
	float("pathwidth",240){range(20,4096);};
    feature("source", "table");
    feature("menu", "9.Table/compare native memory");
    feature("description", "just so so");
}
filter
{
    String = get_type("System.String");
    $header = sheet.GetRow(0);
    $ix = 2;
    $ix2 = 1;
    $ix3 = 5;
    $ix4 = 6;

    $class = get_cell_string(row, $ix);
    $name = get_cell_string(row, $ix2);
    $size = get_cell_numeric(row, $ix3);
    $addr = get_cell_numeric(row, $ix4);

    $v99 = 0;
    if(string_contains_any($class, classcontainsany) && string_contains_any($name, namecontainsany) && string_not_contains($class, classnotcontains) && string_not_contains($name, namenotcontains)){
        $row = find_row_from_hashtable(dict2, [$class, $name]);
        if(!is_null($row)){
            if(resultopt!=2){
                $size2 = get_cell_numeric($row, $ix3);
                info=format("{0}, {1}, size {2}=>{3}",$class,$name,$size,$size2);
                $v99=1;
            };
        }elseif(resultopt!=3){
            info=format("{0}, {1}, size {2}=>",$class,$name,$size);
            $v99=1;
        };
    };
    if($v99==1){
        asset_path = format("{0}({1})",$name,$addr);
        scene_path = $class;
        order = $size;
        value = $size;
    	extraobject = $addr;
    	extralistbuild = "BuildExtraList";
    };
    $v99;
};

script(buildHashtable)args($paramInfo)
{
    $dict = table_to_hashtable(hashtable_get(params,"table2").Value,1,[2,1]);
    return($dict);
};
script(BuildExtraList)args($item)
{
	$r = find_shortest_path_to_root($item.ExtraObject);
	return($r);
};