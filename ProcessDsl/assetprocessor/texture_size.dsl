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
    	var(0) = loadasset(assetpath);
    	var(1) = var(0).width;
    	var(2) = var(0).height;
		var(3) = gettexturesetting("iPhone");
    	var(4) = gettexturesetting("Android");
    	//unloadasset(var(0));
    	if((var(1) > maxSize || var(2) > maxSize) && (var(3).maxTextureSize > maxSize || var(4).maxTextureSize > maxSize)){
    		info = "size:" + var(1) + "," + var(2);
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