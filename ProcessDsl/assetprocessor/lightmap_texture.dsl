input("*.exr","*.hdr","Lightmap*.tga","Lightmap*.png")
{
	stringlist("filter", "");
	int("maxSize",1);
	string("prop",""){
		multiple(["cube","mipmap"],[1,2]);
	};
	feature("source", "project");
	feature("menu", "0.Asset Processors/Lightmap Setting");
	feature("description", "just so so");
}
filter
{
	var(0) = loadasset(assetpath);
	var(1) = var(0).width;
	var(2) = var(0).height;
	var(3) = importer.textureShape == parseenum("TextureImporterShape", "TextureCube");
	var(4) = importer.mipmapEnabled;
	//unloadasset(var(0));
	if((var(1) > maxSize || var(2) > maxSize) && stringcontains(assetpath, filter) && stringnotcontains(assetpath, "SplatAlpha") && (prop.Contains("1") && var(3) || !prop.Contains("1")) && (prop.Contains("2") && var(4) || !prop.Contains("2"))){
		info = "size:" + var(1) + "," + var(2);
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