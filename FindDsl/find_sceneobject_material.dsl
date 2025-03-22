input("Renderer")
{
	string("matName","Default");
	string("shaderName","Standard");
	float("pathwidth",240){range(20,4096);};
	feature("source", "sceneobjects");
	feature("menu", "3.Current Scene Objects/Materials");
	feature("description", "just so so");
}
filter
{
	$v0 = 0;
	$v1 = getcomponent(object, "Renderer");
	$v2 = $v1.sharedMaterials;
	looplist($v2){
		if(isnull($$))
			continue;
		$v3 = $$.name;
		$v4 = $$.shader.name;
		if($v3.Contains(matName) && $v4.Contains(shaderName)){
	  	info = "mat:" + $v3 + " shader:" + $v4;
	  	$v0 = 1;
	  };
	};
	$v0;
};