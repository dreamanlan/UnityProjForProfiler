input("*.tga","*.png","*.jpg","*.exr")
{
	int("maxSize",256){
		range(1,1024);
	};
	int("maxRefBy",3);
	string("prop",""){
		multiple(["readable","mipmap"],[1,2]);
	};
	stringlist("filter", "");
	stringlist("notfilter", "");
	float("pathwidth",240){range(20,4096);};
	feature("source", "project");
	feature("menu", "1.Project Resources/Textures with refby");
	feature("description", "just so so");
}
filter
{
    if(string_contains(asset_path, filter) && string_not_contains(asset_path, notfilter)){
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
			$v7 = calc_ref_by_count(asset_path);
    		//order = $v1 < $v2 ? $v2 : $v1;
    		if(($v1 > maxSize || $v2 > maxSize) && ($v5.maxTextureSize > maxSize || $v6.maxTextureSize > maxSize) && (prop.Contains("1") && $v3 || !prop.Contains("1")) && (prop.Contains("2") && $v4 || !prop.Contains("2")) && $v7<=maxRefBy){
    		    $v8 = get_reference_by_assets(asset_path);
    		    loop_list($v8){
    		        $asset = $$;
    		        $v9 = new_item();
    		        $v9.AssetPath = asset_path;
    		        $v9.ScenePath = "";
    		        $v9.Info = format("size:{0},{1} readable:{2} mipmap:{3} refby_count:{4} refby_asset:{5}", $v1, $v2, $v3, $v4, $v7, $asset);
    		        $v9.Order = $v7;
    		    };
    		};
    		unload_asset($v0);
    	};
    };
    0;
};