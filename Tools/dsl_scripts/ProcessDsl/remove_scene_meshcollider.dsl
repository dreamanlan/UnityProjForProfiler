input("Transform")
{
	feature("source", "sceneobjects");
	feature("menu", "3.Current Scene Objects/Remove Mesh Collider");
	feature("description", "just so so");
}
filter
{
	$v0 = get_component_in_children(object, "MeshCollider");
	if(is_null($v0)){
		0;
	}else{
		1;
	};
}
process
{
	$v0 = get_component_in_children(object, "MeshCollider");
	destroy_object($v0);
};