input("*.prefab")
{
	float("pathwidth",240){range(20,4096);};
	feature("source", "project");
	feature("menu", "1.Project Resources/Find Mesh Collider");
	feature("description", "just so so");
}
filter
{
	object = load_asset(asset_path);
	$v0 = get_component(object, "MeshCollider");
	if(is_null($v0)){
		0;
	}else{
		1;
	};
};