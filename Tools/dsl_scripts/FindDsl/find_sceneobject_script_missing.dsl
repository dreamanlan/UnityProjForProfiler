input("Transform")
{
	string("filter", "");
	float("pathwidth",240){range(20,4096);};
	feature("source", "sceneobjects");
	feature("menu", "3.Current Scene Objects/Missing Script");
	feature("description", "just so so");
}
filter
{
	if(scene_path.Contains(filter)){
		$v0 = getcomponents(object, "Component");
		$v1 = 0;
		loop_list($v0){
			if(is_null($$)){
				$v1=1;
			};
		};
		$v1;
	}else{
	  0;
	};
};