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
		
		var(0) = getcellstring(row, $ix);
		var(1) = stringsplit(var(0), [";"]);
		var(2) = getcellnumeric(row, $ix3);
		var(3) = getcellnumeric(row, $ix4);
		var(4) = 1;
		var(5) = list();
		var(6) = getalldslfiles(getcellnumeric(row, 0));
		var(7) = stringjoin(";",var(6));
		looplist(var(1)){
				if(!fileexist("../../Product/DslFile/"+$$+".dsl")){
						var(4) = 0;
						listadd(var(5),$$);
				};
		};
		looplist(var(6)){
				if(!fileexist("../../Product/DslFile/"+$$+".dsl")){
						var(4) = 0;
						listadd(var(5),$$);
				};
		};
			
		if(var(0).Contains(filter) && var(7).Contains(filter2) && (!notexist || notexist && var(4)==0) && (!weimian || weimian && var(2)!=0 && var(3)>=1 && var(3)<=3)){
				assetpath = "Assets/SceneRes/Scenes/"+getcellstring(row, $ix2)+".unity";
				info = format("scene:{0} name:{1} follow:{2} weimian:{3} not exists:{4} dsl1:{5} dsl2:{6}",
			      getcellvalue(row, 0),
			      getcellvalue(row, 1),
			      var(2),
			      var(3),
			      stringjoin(";",var(5)),
			      var(0),
			      var(7)
			    );
        value = getcellnumeric(row, 0);
        1;
		}else{
		    0;
		};
};