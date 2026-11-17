input("*.tga","*.png","*.jpg","*.exr")
{
	string("filter", "");
	float("pathwidth",240){range(20,4096);};
	feature("source", "project");
	feature("menu", "2.Project Resources/Correct None Alpha Texture");
	feature("description", "just so so");
}
filter
{
	$v0 = load_asset(asset_path);
	$v1 = $v0.width;
	$v2 = $v0.height;
	if(!is_texture_no_alpha_source() && asset_path.Contains(filter)){
		info = "size:" + $v1 + "," + $v2;
		$r = 1;
	} else {
		$r = 0;
	};
	unload_asset($v0);
	$r;
}
process
{
	if(correct_none_alpha_texture()){
		debug_log("processed:{0}",asset_path);
	  save_and_reimport();
	};
};