input("MeshRenderer", "SkinnedMeshRenderer")
{
	string("filter", "");
	float("pathwidth",240){range(20,4096);};
	feature("source", "sceneobjects");
	feature("menu", "3.Current Scene Objects/Mesh Texture Ratio");
	feature("description", "just so so");
}
filter
{
	$v0 = calcmeshtexratio(object);
	$v1 = $v0[0];
	$v2 = $v0[1];
	$v3 = $v0[2];
	$v4 = $v0[3];
	order = changetype($v2 * 1000,"int");
	if($v2 > 0 && scenepath.Contains(filter)){
		info = $v1;
		1;
	}else{
		0;
	};
};