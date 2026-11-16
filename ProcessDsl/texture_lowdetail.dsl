input("*.tga","*.png","*.jpg","*.exr")
{
	string("filter", "");
	string("notfilter", "");
	int("maxSize",1);
	float("pathwidth",240){range(20,4096);};
	feature("source", "project");
	feature("menu", "2.Project Resources/Texture Low Detail");
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
    	$v5 = get_file_name_without_extension(asset_path);
    	if(($v1 > maxSize || $v2 > maxSize) && ($v3.maxTextureSize > maxSize || $v4.maxTextureSize > maxSize) && (string_to_lower($v5).EndsWith("_n") || string_to_lower($v5).EndsWith("_s")) && !(importer.lowDetail)){
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
	importer.lowDetail = true;
  save_and_reimport();
};