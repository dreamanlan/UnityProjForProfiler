input("Transform")
{
	float("pathwidth",240){range(20,4096);};
	feature("source", "sceneobjects");
	feature("menu", "3.Current Scene Objects/Find Mesh Collider");
	feature("description", "just so so");
}
filter
{
	$v0 = getcomponentinchildren(object, "MeshCollider");
	if(isnull($v0)){
		0;
	}else{
		1;
	};
};