input
{
    string("table", "Scene/scene_sceneinfo.txt")
    {
        file("txt");
    };
    string("encoding", "utf-8");
    int("skiprows", 0);
		string("filter", "");
		string("filter2", "");
		bool("notexist", false);
		bool("weimian", false);
	float("pathwidth",240){range(20,4096);};
		feature("source", "table");
		feature("menu", "9.Table/find scene story");
		feature("description", "just so so");
}
filter
{
    String = gettype("System.String");
		order = row.RowNum;
		$header = sheet.GetRow(2);
		$ix = find_cell_index($header, "SceneDslFile");
		$ix2 = find_cell_index($header, "Prefab");
		$ix3 = find_cell_index($header, "FollowSceneId");
		$ix4 = find_cell_index($header, "WeiMianSceneFlag");

		$v0 = get_cell_string(row, $ix);
		$v1 = string_split($v0, [";"]);
		$v2 = get_cell_numeric(row, $ix3);
		$v3 = get_cell_numeric(row, $ix4);
		$v4 = 1;
		$v5 = list();
		$v6 = get_alldsl_files(get_cell_numeric(row, 0));
		$v7 = string_join(";",$v6);
		loop_list($v1){
				if(!fileexist("../../Product/DslFile/"+$$+".dsl")){
						$v4 = 0;
						list_add($v5,$$);
				};
		};
		loop_list($v6){
				if(!fileexist("../../Product/DslFile/"+$$+".dsl")){
						$v4 = 0;
						list_add($v5,$$);
				};
		};

		if($v0.Contains(filter) && $v7.Contains(filter2) && (!notexist || notexist && $v4==0) && (!weimian || weimian && $v2!=0 && $v3>=1 && $v3<=3)){
				asset_path = "Assets/SceneRes/Scenes/"+get_cell_string(row, $ix2)+".unity";
				info = format("scene:{0} name:{1} follow:{2} weimian:{3} not exists:{4} dsl1:{5} dsl2:{6}",
			      get_cell_value(row, 0),
			      get_cell_value(row, 1),
			      $v2,
			      $v3,
			      string_join(";",$v5),
			      $v0,
			      $v7
			    );
        value = get_cell_numeric(row, 0);
        1;
		}else{
		    0;
		};
};