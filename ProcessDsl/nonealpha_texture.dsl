input("*.tga","*.png","*.jpg","*.exr")
{
	string("filter", "");
	feature("source", "project");
	feature("menu", "1.Project Resources/Set None Alpha Texture");
	feature("description", "just so so");
}
filter
{
	$v0 = loadasset(assetpath);
	$v1 = $v0.width;
	$v2 = $v0.height;
	if(!istexturenoalphasource() && assetpath.Contains(filter)){
		info = "size:" + $v1 + "," + $v2;
		$r = 1;
	} else {
		$r = 0;
	};
	unloadasset($v0);
	$r;
}
process
{
	setnonealphatexture();
	debuglog("processed:{0}",assetpath);
	saveandreimport();
};