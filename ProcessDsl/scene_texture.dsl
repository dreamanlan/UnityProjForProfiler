input("*.tga","*.png","*.jpg","*.exr")
{
	string("filter", "");
	string("notfilter", "/Select/");
	int("maxSize",1024);
	feature("source", "project");
	feature("menu", "1.Project Resources/Scene Texture Size");
	feature("description", "just so so");
}
filter
{
	$v0 = loadasset(assetpath);
	$v1 = $v0.width;
	$v2 = $v0.height;
	//unloadasset($v0);
	if(($v1 > maxSize || $v2 > maxSize) && assetpath.Contains(filter) && (notfilter=="" || !assetpath.Contains(notfilter))){
		info = "size:" + $v1 + "," + $v2;
		1;
	} else {
		0;
	};
}
process
{
	/*
	$v0 = getdefaulttexturesetting();
	$v0.maxTextureSize = changetype(maxSize, "int");
	settexturesetting($v0);

	$v1 = gettexturesetting("Standalone");
	$v1.maxTextureSize = changetype(maxSize, "int");
	settexturesetting($v1);
	*/
	$v2 = gettexturesetting("iPhone");
	$v2.overridden=true;
	$v2.maxTextureSize = changetype(maxSize, "int");
	setsceneastctexture($v2);
	settexturesetting($v2);

	$v3 = gettexturesetting("Android");
	$v3.overridden=true;
	$v3.maxTextureSize = changetype(maxSize, "int");
	setsceneastctexture($v3);
	settexturesetting($v3);

    saveandreimport();
};