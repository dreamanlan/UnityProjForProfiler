input("*.asset")
{
    stringlist("filter","");
	float("pathwidth",240){range(20,4096);};
	feature("source", "project");
	feature("menu", "1.Project Resources/Assets");
	feature("description", "just so so");
}
filter
{
	if(string_contains(asset_path,filter)){
		$v0 = load_asset(asset_path);
		$v1 = $v0.name;
		$v2 = get_runtime_memory_size($v0);
		unload_asset($v0);
		value = $v2;
		order = $v2;
        info = "name:" + $v1 + " mem:" + $v2;
        1;
    }else{
        0;
    };
};