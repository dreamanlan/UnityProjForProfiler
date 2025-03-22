input("*.tga","*.png","*.jpg","*.exr")
{
	string("filter", "");
	string("notfilter", "/Select/");
	int("maxSize",1024);
	float("pathwidth",240){range(20,4096);};
	feature("source", "project");
	feature("menu", "1.Project Resources/Texture Size");
	feature("description", "just so so");
}
filter
{
    if(stringcontains(assetpath, filter) && stringnotcontains(assetpath, notfilter)){
    	$v0 = loadasset(assetpath);
    	$v1 = $v0.width;
    	$v2 = $v0.height;
		$v3 = gettexturesetting("iPhone");
    	$v4 = gettexturesetting("Android");
    	//unloadasset($v0);
    	if(($v1 > maxSize || $v2 > maxSize) && ($v3.maxTextureSize > maxSize || $v4.maxTextureSize > maxSize)){
    		info = "size:" + $v1 + "," + $v2;
    		1;
    	} else {
    		0;
    	};
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
	setastctexture($v2);
	settexturesetting($v2);

	$v3 = gettexturesetting("Android");
	$v3.overridden=true;
	$v3.maxTextureSize = changetype(maxSize, "int");
	setastctexture($v3);
	settexturesetting($v3);

  saveandreimport();
};