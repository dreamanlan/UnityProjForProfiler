input("*.asset")
{
	string("filter", "");
	feature("source", "project");
	feature("menu", "1.Project Resources/Split Animation");
	feature("description", "just so so");
}
filter
{
	object = load_asset(asset_path);
	$v0 = get_type_name(object);
	$v1 = get_type_full_name(object);
	$v2 = get_type_assembly_name(object);
	if(!is_null(object) && $v0=="AnimationReference" && asset_path.Contains(filter) && !asset_path.EndsWith("_split.asset")){
		info = $v1+","+$v2;
		1;
	}else{
		0;
	};
}
process
{
	if(asset_path.Contains("/Player/")){
		split_animation_reference("*",list("Win_Idle"),list("Story_.*"));
	}else{
		split_animation_reference(list("Idle","Idle_.*","Story_Idle","Showup_1","Showup_2","Run","Walk","End","Start","Collision_Before.*","Collision_Behind.*","Idle_Play_.*","StoryAnim.*","AI_.*"),"*",list("Story_.*","dailybehave_.*","Date_.*","HuDong_.*","Baoqi_.*","Beibaoqi_.*","talk_.*","Face.*","Lip_.*"));
	};
};

