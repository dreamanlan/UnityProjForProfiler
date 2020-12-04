input("*.asset")
{
	string("filter", "");
	feature("source", "project");
	feature("menu", "1.Project Resources/Split Animation");
	feature("description", "just so so");
}
filter
{
	object = loadasset(assetpath);
	var(0) = gettypename(object);
	var(1) = gettypefullname(object);
	var(2) = gettypeassemblyname(object);
	if(!isnull(object) && var(0)=="AnimationReference" && assetpath.Contains(filter) && !assetpath.EndsWith("_split.asset")){
		info = var(1)+","+var(2);
		1;
	}else{
		0;
	};
}
process
{
	if(assetpath.Contains("/Player/")){
		splitanimationreference("*",list("Win_Idle"),list("Story_.*"));
	}else{
		splitanimationreference(list("Idle","Idle_.*","Story_Idle","Showup_1","Showup_2","Run","Walk","End","Start","Collision_Before.*","Collision_Behind.*","Idle_Play_.*","StoryAnim.*","AI_.*"),"*",list("Story_.*","dailybehave_.*","Date_.*","HuDong_.*","Baoqi_.*","Beibaoqi_.*","talk_.*","Face.*","Lip_.*"));
	};
};

