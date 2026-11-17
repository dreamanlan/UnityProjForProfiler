input("*.prefab")
{
	string("filter", "");
	float("pathwidth",240){range(20,4096);};
	feature("source", "project");
	feature("menu", "1.Project Resources/Mesh Filter");
	feature("description", "just so so");
}
filter
{
	if(asset_path.Contains(filter)){
    $v0 = load_asset(asset_path);
	  $v1 = get_components_in_children($v0, "MeshFilter");
	  $v2 = 0;
	  loop_list($v1){
	    if(is_null($$.sharedMesh)){
	    	$v2=1;
	    };
	  };
	  $v2;
	}else{
	  0;
	};
};