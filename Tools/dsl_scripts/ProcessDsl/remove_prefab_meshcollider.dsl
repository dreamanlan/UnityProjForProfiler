input("*.prefab")
{
	feature("source", "project");
	feature("menu", "2.Project Resources/Remove Mesh Collider");
	feature("description", "just so so");
}
filter
{
	object = load_asset(asset_path);
	$v0 = get_component_in_children(object, "MeshCollider");
	if(is_null($v0)){
		0;
	}else{
		1;
	};
}
process
{
	object = load_asset(asset_path);
	$v0 = get_component_in_children(object, "MeshCollider");
	destroy_object($v0, true);
};