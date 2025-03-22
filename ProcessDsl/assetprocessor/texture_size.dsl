input("*.tga","*.png","*.jpg")
{
	string("filter", "");
	string("notfilter", "/Select/");
	int("maxSize",1024);
	feature("source", "project");
	feature("menu", "0.Asset Processors/Texture Size Setting");
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
    	//unloadasset($v0);
    	if(($v1 > maxSize || $v2 > maxSize) && ($v3.maxTextureSize > maxSize || $v4.maxTextureSize > maxSize)){
    		info = "size:" + $v1 + "," + $v2;
    		1;
    	} else {
    		0;
    	};
    } else {
		0;
	};
}
assetprocessor
{
	SetAndroidMaxSize1024;
	SetIPhoneMaxSize1024;
	SetDirty;
	SaveAndReimport;
};