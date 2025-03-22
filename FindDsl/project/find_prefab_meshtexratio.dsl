input("*.prefab")
{
	string("filter", "");
	float("pathwidth",240){range(20,4096);};
	feature("source", "project");
	feature("menu", "1.Project Resources/Mesh Texture Ratio");
	feature("description", "just so so");
}
filter
{
	$v0 = loadasset(assetpath);
	if(isnull($v0)){
		0;
	}else{
		$v0 = calcmeshtexratio($v0, 1);
		$v1 = $v0[0];
		$v2 = $v0[1];
		$v3 = $v0[2];
		$v4 = $v0[3];
		order = changetype($v2 * 1000,"int");
		if($v2 > 0 && assetpath.Contains(filter)){
		  info = $v1;
		  1;
		}else{
		  0;
		};
	};
};