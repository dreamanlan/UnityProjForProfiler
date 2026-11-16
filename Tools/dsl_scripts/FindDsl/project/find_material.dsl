input("*.mat")
{
    stringlist("shaderNames","Standard");
	float("pathwidth",240){range(20,4096);};
	feature("source", "project");
	feature("menu", "1.Project Resources/Materials");
	feature("description", "just so so");
}
filter
{
	$v0 = load_asset(asset_path);
	$v1 = $v0.name;
	$v2 = $v0.shader.name;
	$v3 = get_runtime_memory_size($v0);
	unload_asset($v0);
	if(string_contains($v2,shaderNames)){
        info = "mat:" + $v1 + " shader:" + $v2 + " mem:" + $v3;
        1;
    }else{
        0;
    };
};