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
    if(stringcontains(assetpath, filter) && stringnotcontains(assetpath, notfilter)){
    	$v0 = loadasset(assetpath);
    	$v1 = $v0.width;
    	$v2 = $v0.height;
		$v3 = gettexturesetting("iPhone");
    	$v4 = gettexturesetting("Android");
    	$v5 = $v0.mipMapBias;
    	//unloadasset($v0);
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