input("*.tga","*.png","*.jpg")
{
	stringlist("filter", "");
	int("maxSize",1);
	feature("source", "project");
	feature("menu", "0.Asset Processors/Scene Texture Setting");
	feature("description", "just so so");
}
filter
{
	$v0 = load_asset(asset_path);
	$v1 = $v0.width;
	$v2 = $v0.height;
	//unload_asset($v0);
	if(($v1 > maxSize || $v2 > maxSize) && string_contains(asset_path, filter) && string_not_contains(asset_path, "SplatAlpha")){
		info = "size:" + $v1 + "," + $v2;
		1;
	} else {
		0;
	};
}
assetprocessor
{
	CorrectNoneAlphaTexture;
	SetAndroidSceneASTC;
	SetIPhoneSceneASTC;
	SetCompressed;
	SetReadableFalse;
	SetAnisoLevelN1;
	SetMipMapTrue;
	SetMipMapFilterKaiser;
	SetStreamingMipMapsTrue;
	SetFilterTrilinear;
	SetDirty;
	SaveAndReimport;
};