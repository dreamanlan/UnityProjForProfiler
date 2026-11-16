input
{
  string("excel", "Scene.xls");
  string("sheetname", "sceneinfo");
  int("skiprows", 5);
	string("filter", "");
	bool("notexist", False);
	bool("weimian", False);
	feature("source", "excel");
	feature("menu", "8.Excel/find scene story");
	feature("description", "just so so");
}
filter
{
  String = gettype("System.String");
	order = row.RowNum;
	$header = sheet.GetRow(3);
	$ix = find_cell_index($header, "SceneDslFile");
	$ix2 = find_cell_index($header, "Prefab");
	$ix3 = find_cell_index($header, "FollowSceneId");
	$ix4 = find_cell_index($header, "WeiMianSceneFlag");

	$v0 = get_cell_string(row, $ix);
	$v1 = string_split($v0, [";"]);
	$v2 = 1;
	$v3 = get_cell_numeric(row, $ix3);
	$v4 = get_cell_numeric(row, $ix4);
	loop_list($v1){
		if(!fileexist("../../Product/DslFile/"+$$+".dsl")){
			$v2 = 0;
		};
	};

	if((get_cell_string(row, 0)=='*' || get_cell_string(row, 1)=='+') && $v0.Contains(filter) && (!notexist || notexist && $v2==0) && (!weimian || weimian && $v3!=0 && $v4>=1 && $v4<=3)){
		asset_path = "Assets/SceneRes/Scenes/"+get_cell_string(row, $ix2)+".unity";
		info = format("scene:{0} name:{1} dsl:{2} exists:{3} follow:{4} weimian:{5}",
	      get_cell_value(row, 2),
	      get_cell_value(row, 3),
	      get_cell_string(row, $ix),
	      $v2,
	      $v3,
	      $v4
	    );
	  value = get_cell_numeric(row, 2);
	  1;
	}else{
	  0;
	};
};