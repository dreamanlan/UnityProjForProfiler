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
  $v0 = load_asset(asset_path);
  $v1 = getcomponentsinchildren($v0, "Renderer");
  loop_list($v1){
  	$v2 = $$.sharedMaterials;
  	loop_list($v2){
			if(is_null($$))
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