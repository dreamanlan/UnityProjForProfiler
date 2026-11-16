input("Transform")
{
	feature("source", "sceneobjects");
	feature("menu", "3.Current Scene Objects/Remove Mesh Collider");
	feature("description", "just so so");
}
filter
{
	$v0 = getcomponentinchildren(object, "MeshCollider");
	if(is_null($v0)){
		0;
	}else{
		1;
	};
}
process
{
	$v0 = getcomponentinchildren(object, "MeshCollider");
	destroyobject($v0);
};