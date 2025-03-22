input("*.exr","*.hdr","*.tga","*.png")
{
	stringlist("filter", "");
	int("maxSize",1);
	string("prop",""){
		multiple(["cube","mipmap"],[1,2]);
	};
	feature("source", "project");
	feature("menu", "0.Asset Processors/Scene Light Setting");
	feature("description", "just so so");
}
filter
{
	$v0 = loadasset(assetpath);
	$v1 = $v0.width;
	$v2 = $v0.height;
	$v3 = importer.textureShape == parseenum("TextureImporterShape", "TextureCube");
	$v4 = importer.mipmapEnabled;
	//unloadasset($v0);
	if(($v1 > maxSize || $v2 > maxSize) && stringcontains(assetpath, filter) && stringnotcontains(assetpath, "SplatAlpha") && (prop.Contains("1") && $v3 || !prop.Contains("1")) && (prop.Contains("2") && $v4 || !prop.Contains("2"))){
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
	SetMipMapFalse;
	SetStreamingMipMapsFalse;
	SetDirty;
	SaveAndReimport;
};