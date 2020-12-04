input("*.prefab")
{
	feature("source", "project");
	feature("menu", "1.Project Resources/Remove Mesh Collider");
	feature("description", "just so so");
}
filter
{
	object = loadasset(assetpath);
	var(0) = getcomponentinchildren(object, "MeshCollider");
	if(isnull(var(0))){
		0;
	}else{
		1;
	};
}
process
{
	object = loadasset(assetpath);
	var(0) = getcomponentinchildren(object, "MeshCollider");
	destroyobject(var(0), true);
};