input("*.prefab")
{
	feature("source", "project");
	feature("menu", "2.Project Resources/Prefab Setting");
	feature("description", "just so so");
}
filter
{
	object = load_asset(asset_path);
	$v0 = get_component_in_children(object,"SkinnedMeshRenderer");
	if(!is_null($v0)){
		1;
	}else{
		0;
	};
}
process
{
	loop_list(get_components_in_children(object,"SkinnedMeshRenderer")){
		$$.skinnedMotionVectors=false;
	};
	loop_list(get_components_in_children(object,"Playables.PlayableDirector")){
		$$.playOnAwake=false;
	};
};