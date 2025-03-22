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
	$v0 = loadasset(assetpath);
	$v1 = $v0.width;
	$v2 = $v0.height;
	$v3 = importer.npotScale;
	$v4 = ispoweroftwo($v1) && ispoweroftwo($v2);
	//unloadasset($v0);
	if(($v1 > maxSize || $v2 > maxSize) && assetpath.Contains(filter) && !$v4){
		info = "size:" + $v1 + "," + $v2 + " npot scale:" + $v3;
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