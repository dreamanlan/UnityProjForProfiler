input("*.tga","*.png","*.jpg")
{
	stringlist("filter", "");
	stringlist("notfilter", "/Select/");
	int("maxSize",1);
	feature("source", "project");
	feature("menu", "0.Asset Processors/Texture Low Detail Setting");
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
    	var(5) = getfilenamewithoutextension(assetpath);
    	//unloadasset(var(0));
    	if((var(1) > maxSize || var(2) > maxSize) && (var(3).maxTextureSize > maxSize || var(4).maxTextureSize > maxSize) && (var(5).EndsWith("_n") || var(5).EndsWith("_s")) && !(importer.lowDetail)){
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
	SetLowDetailTrue;
	SetDirty;
	SaveAndReimport;
};