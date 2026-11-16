input("*.tga","*.png","*.jpg")
{
	stringlist("filter", "");
	stringlist("notfilter", "/Select/");
	int("maxSize",512);
	float("bias",1);
	feature("source", "project");
	feature("menu", "0.Asset Processors/Texture MipmapBias Setting");
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
    	$v5 = $v0.mipMapBias;
    	//unload_asset($v0);
    	if(($v1 > maxSize || $v2 > maxSize) && ($v3.maxTextureSize > maxSize || $v4.maxTextureSize > maxSize) && $v5!=bias){
    		info = "size:" + $v1 + "," + $v2;
    		1;
    	} else {
    		0;
    	};
    }else{
        0;
    };
}
assetprocessor
{
	SetMipMapBiasOne;
	SetDirty;
	SaveAndReimport;
};