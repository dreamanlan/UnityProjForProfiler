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
	
	var(0) = getcellstring(row, $ix);
	var(1) = stringsplit(var(0), [";"]);
	var(2) = 1;
	var(3) = getcellnumeric(row, $ix3);
	var(4) = getcellnumeric(row, $ix4);
	looplist(var(1)){
		if(!fileexist("../../Product/DslFile/"+$$+".dsl")){
			var(2) = 0;
		};
	};
		
	if((getcellstring(row, 0)=='*' || getcellstring(row, 1)=='+') && var(0).Contains(filter) && (!notexist || notexist && var(2)==0) && (!weimian || weimian && var(3)!=0 && var(4)>=1 && var(4)<=3)){
		assetpath = "Assets/SceneRes/Scenes/"+getcellstring(row, $ix2)+".unity";
		info = format("scene:{0} name:{1} dsl:{2} exists:{3} follow:{4} weimian:{5}",
	      getcellvalue(row, 2),
	      getcellvalue(row, 3),
	      getcellstring(row, $ix),
	      var(2),
	      var(3),
	      var(4)
	    );
	  value = getcellnumeric(row, 2);
	  1;
	}else{
	  0;
	};
};