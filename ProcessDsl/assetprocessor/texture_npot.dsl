input("*.tga","*.png","*.jpg")
{
	string("filter", "");
	int("maxSize",1);
	feature("source", "project");
	feature("menu", "0.Asset Processors/Texture Npot Setting");
	feature("description", "just so so");
}
filter
{
	var(0) = loadasset(assetpath);
	var(1) = var(0).width;
	var(2) = var(0).height;
	var(3) = importer.npotScale;
	var(4) = ispoweroftwo(var(1)) && ispoweroftwo(var(2));
	//unloadasset(var(0));
	if((var(1) > maxSize || var(2) > maxSize) && assetpath.Contains(filter) && !var(4)){
		info = "size:" + var(1) + "," + var(2) + " npot scale:" + var(3);
		1;
	} else {
		0;
	};
}
assetprocessor
{
	CorrectNoneAlphaTexture;
	SetNpotScaleNearest;
	SetDirty;
	SaveAndReimport;
};