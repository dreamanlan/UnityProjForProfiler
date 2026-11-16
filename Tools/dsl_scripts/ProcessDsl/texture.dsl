input("*.tga","*.png","*.jpg","*.exr")
{
	string("filter", "");
	string("notfilter", "");
	int("maxSize",1024);
	float("pathwidth",240){range(20,4096);};
	feature("source", "project");
	feature("menu", "2.Project Resources/Texture Size");
	feature("description", "just so so");
}
filter
{
    if(string_contains(asset_path, filter) && string_not_contains(asset_path, notfilter)){
    	$v0 = load_asset(asset_path);
    	$v1 = $v0.width;
    	$v2 = $v0.height;
		$v3 = get_texture_setting("iPhone");
    	$v4 = get_texture_setting("Android");
    	if(($v1 > maxSize || $v2 > maxSize) && ($v3.maxTextureSize > maxSize || $v4.maxTextureSize > maxSize)){
    		info = "size:" + $v1 + "," + $v2;
    		$r = 1;
    	} else {
    		$r = 0;
    	};
    	unload_asset($v0);
		$r;
	} else {
		0;
	};
}
process
{
	/*
	$v0 = get_default_texture_setting();
	$v0.maxTextureSize = changetype(maxSize, "int");
	set_texture_setting($v0);

	$v1 = get_texture_setting("Standalone");
	$v1.maxTextureSize = changetype(maxSize, "int");
	set_texture_setting($v1);
	*/
	$v2 = get_texture_setting("iPhone");
	$v2.overridden=true;
	$v2.maxTextureSize = changetype(maxSize, "int");
	set_astc_texture($v2);
	set_texture_setting($v2);

	$v3 = get_texture_setting("Android");
	$v3.overridden=true;
	$v3.maxTextureSize = changetype(maxSize, "int");
	set_astc_texture($v3);
	set_texture_setting($v3);

  save_and_reimport();
};