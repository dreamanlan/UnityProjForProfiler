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
	$ix = findcellindex($header, "SceneDslFile");
	$ix2 = findcellindex($header, "Prefab");
	$ix3 = findcellindex($header, "FollowSceneId");
	$ix4 = findcellindex($header, "WeiMianSceneFlag");

	$v0 = getcellstring(row, $ix);
	$v1 = stringsplit($v0, [";"]);
	$v2 = 1;
	$v3 = getcellnumeric(row, $ix3);
	$v4 = getcellnumeric(row, $ix4);
	looplist($v1){
		if(!fileexist("../../Product/DslFile/"+$$+".dsl")){
			$v2 = 0;
		};
	};

	if((getcellstring(row, 0)=='*' || getcellstring(row, 1)=='+') && $v0.Contains(filter) && (!notexist || notexist && $v2==0) && (!weimian || weimian && $v3!=0 && $v4>=1 && $v4<=3)){
		assetpath = "Assets/SceneRes/Scenes/"+getcellstring(row, $ix2)+".unity";
		info = format("scene:{0} name:{1} dsl:{2} exists:{3} follow:{4} weimian:{5}",
	      getcellvalue(row, 2),
	      getcellvalue(row, 3),
	      getcellstring(row, $ix),
	      $v2,
	      $v3,
	      $v4
	    );
	  value = getcellnumeric(row, 2);
	  1;
	}else{
	  0;
	};
};