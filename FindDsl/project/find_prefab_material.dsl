input("*.prefab")
{
	string("matName","Default");
	string("shaderName","Standard");
	float("pathwidth",240){range(20,4096);};
	feature("source", "project");
	feature("menu", "1.Project Resources/Prefab Material");
	feature("description", "just so so");
}
filter
{
	$v10 = 0;
  $v0 = loadasset(assetpath);
  $v1 = getcomponentsinchildren($v0, "Renderer");
  looplist($v1){
  	$v2 = $$.sharedMaterials;
  	looplist($v2){
			if(isnull($$))
				continue;
  		$v3 = $$.name;
  		$v4 = $$.shader.name;
  		if($v3.Contains(matName) && $v4.Contains(shaderName)){
		  	info = "mat:" + $v3 + " shader:" + $v4;
		  	$v10 = 1;
		  };
  	};
  };
  $v10;
};