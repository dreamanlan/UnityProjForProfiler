input("*.tga","*.png","*.jpg")
{
	string("filter", "");
	int("maxSize",1);
	feature("source", "project");
	feature("menu", "0.Asset Processors/Terrain Texture Setting");
	feature("description", "just so so");
}
filter
{
	var(0) = loadasset(assetpath);
	var(1) = var(0).width;
	var(2) = var(0).height;
	//unloadasset(var(0));
	if((var(1) > maxSize || var(2) > maxSize) && assetpath.Contains(filter)){
		info = "size:" + var(1) + "," + var(2);
		1;
	} else {
		0;
	};
}
assetprocessor
{
	CorrectNoneAlphaTexture;
	SetAndroidOverrideFalse;
	SetIPhoneOverrideFalse;
	SetCompressed;
	SetMipMapTrue;
	SetFilterBilinear;
	SetStreamingMipMapsFalse;
	SetDirty;
	SaveAndReimport;
};