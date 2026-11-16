input("*.tga","*.png","*.jpg","*.exr")
{
	int("maxSize",1){
		range(1,1024);
	};
	string("prop",""){
		multiple(["readable","mipmap"],[1,2]);
	};
	stringlist("filter", "");
	stringlist("notfilter", "");
	stringlist("anyfilter", "");
	stringlist("anynotfilter", "");
	float("pathwidth",240){range(20,4096);};
	feature("source", "project");
	feature("menu", "2.Project Resources/Revert Sized Textures");
	feature("description", "just so so");
}
filter
{
    if(string_contains(asset_path, filter) && string_not_contains(asset_path, notfilter) && string_contains_any(asset_path, anyfilter) && string_not_contains_any(asset_path, anynotfilter)){
    	$v0 = load_asset(asset_path);
    	if(is_null($v0)){
    		0;
    	} else {
    		$v1 = $v0.width;
    		$v2 = $v0.height;
    		$v3 = importer.isReadable;
    		$v4 = importer.mipmapEnabled;

    		$v5 = get_texture_setting("iPhone");
        	$v6 = get_texture_setting("Android");

    		order = $v1 < $v2 ? $v2 : $v1;
    		if(($v1 > maxSize || $v2 > maxSize) && ($v5.maxTextureSize<$v1 && $v5.maxTextureSize<$v2 || $v6.maxTextureSize<$v1 && $v6.maxTextureSize<$v2) && (prop.Contains("1") && $v3 || !prop.Contains("1")) && (prop.Contains("2") && $v4 || !prop.Contains("2"))){
    			info = format("size:{0},{1} readable:{2} mipmap:{3}", $v1, $v2, $v3, $v4);
    			$r = 1;
    		} else {
    		    $r = 0;
    		};
    		unload_asset($v0);
			$r;
    	};
    }else{
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
	$v2.maxTextureSize = 2048;
	set_astc_texture($v2);
	set_texture_setting($v2);

	$v3 = get_texture_setting("Android");
	$v3.overridden=true;
	$v3.maxTextureSize = 2048;
	set_astc_texture($v3);
	set_texture_setting($v3);

  save_and_reimport();
};