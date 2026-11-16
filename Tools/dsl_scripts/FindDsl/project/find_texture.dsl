input("*.tga","*.png","*.jpg","*.exr")
{
	int("maxSize",256){
		range(1,1024);
	};
	bool("notastc", "false");
	string("prop",""){
		multiple(["readable","mipmap"],[1,2]);
	};
	string("filterMode","Trilinear"){
		popup(["Any", "Point","Bilinear","Trilinear"]);
	};
	stringlist("filter", "");
	stringlist("notfilter", "");
	float("pathwidth",240){range(20,4096);};
	feature("source", "project");
	feature("menu", "1.Project Resources/Textures");
	feature("description", "just so so");
}
filter
{
    if(string_contains(asset_path, filter) && string_not_contains(asset_path, notfilter) && (filterMode=="Any" || importer.filterMode==parseenum("FilterMode", filterMode))){
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
			$v7 = get_runtime_memory_size($v0);
    		order = $v1 < $v2 ? $v2 : $v1;
			value = $v7;
    		if(($v1 > maxSize || $v2 > maxSize) && ($v5.maxTextureSize > maxSize || $v6.maxTextureSize > maxSize) && (prop.Contains("1") && $v3 || !prop.Contains("1")) && (prop.Contains("2") && $v4 || !prop.Contains("2")) && (!notastc || texture_is_not_astc($v5) || texture_is_not_astc($v6))){
    			info = format("size:{0},{1} readable:{2} mipmap:{3} filter:{4} refby_count:{5} memory:{6}", $v1, $v2, $v3, $v4, importer.filterMode, calc_ref_by_count(asset_path), $v7);
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
};