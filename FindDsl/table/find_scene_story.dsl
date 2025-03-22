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
		$ix = findcellindex($header, "SceneDslFile");
		$ix2 = findcellindex($header, "Prefab");
		$ix3 = findcellindex($header, "FollowSceneId");
		$ix4 = findcellindex($header, "WeiMianSceneFlag");

		$v0 = getcellstring(row, $ix);
		$v1 = stringsplit($v0, [";"]);
		$v2 = getcellnumeric(row, $ix3);
		$v3 = getcellnumeric(row, $ix4);
		$v4 = 1;
		$v5 = list();
		$v6 = getalldslfiles(getcellnumeric(row, 0));
		$v7 = stringjoin(";",$v6);
		looplist($v1){
				if(!fileexist("../../Product/DslFile/"+$$+".dsl")){
						$v4 = 0;
						listadd($v5,$$);
				};
		};
		looplist($v6){
				if(!fileexist("../../Product/DslFile/"+$$+".dsl")){
						$v4 = 0;
						listadd($v5,$$);
				};
		};

		if($v0.Contains(filter) && $v7.Contains(filter2) && (!notexist || notexist && $v4==0) && (!weimian || weimian && $v2!=0 && $v3>=1 && $v3<=3)){
				assetpath = "Assets/SceneRes/Scenes/"+getcellstring(row, $ix2)+".unity";
				info = format("scene:{0} name:{1} follow:{2} weimian:{3} not exists:{4} dsl1:{5} dsl2:{6}",
			      getcellvalue(row, 0),
			      getcellvalue(row, 1),
			      $v2,
			      $v3,
			      stringjoin(";",$v5),
			      $v0,
			      $v7
			    );
        value = getcellnumeric(row, 0);
        1;
		}else{
		    0;
		};
};