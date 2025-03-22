input("*.prefab")
{
	feature("source", "project");
	feature("menu", "1.Project Resources/Remove Mesh Collider");
	feature("description", "just so so");
}
filter
{
	object = loadasset(assetpath);
	$v0 = getcomponentinchildren(object, "MeshCollider");
	if(isnull($v0)){
		0;
	}else{
		1;
	};
}
process
{
	object = loadasset(assetpath);
	$v0 = getcomponentinchildren(object, "MeshCollider");
	destroyobject($v0, true);
};