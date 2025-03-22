input("*.tga","*.png","*.jpg")
{
	string("filter", "");
	int("maxSize",1);
	feature("source", "project");
	feature("menu", "0.Asset Processors/Texture Setting");
	feature("description", "just so so");
}
filter
{
	$v0 = loadasset(assetpath);
	$v1 = $v0.width;
	$v2 = $v0.height;
	//unloadasset($v0);
	if(($v1 > maxSize || $v2 > maxSize) && assetpath.Contains(filter)){
		info = "size:" + $v1 + "," + $v2;
		1;
	} else {
		0;
	};
}
assetprocessor
{
	CorrectNoneAlphaTexture;
	SetAndroidASTC;
	SetIPhoneASTC;
	SetCompressed;
	SetReadableFalse;
	SetMipMapTrue;
	SetFilterBilinear;
	SetStreamingMipMapsTrue;
	SetAnisoLevelN1;
	SetDirty;
	SaveAndReimport;
};