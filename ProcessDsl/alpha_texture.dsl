input("*.tga","*.png","*.jpg","*.exr")
{
	string("filter", "");
	float("pathwidth",240){range(20,4096);};
	feature("source", "project");
	feature("menu", "1.Project Resources/Correct None Alpha Texture");
	feature("description", "just so so");
}
filter
{
	$v0 = loadasset(assetpath);
	$v1 = $v0.width;
	$v2 = $v0.height;
	//unloadasset($v0);
	if(!istexturenoalphasource() && assetpath.Contains(filter)){
		info = "size:" + $v1 + "," + $v2;
		1;
	} else {
		0;
	};
}
process
{
	if(correctnonealphatexture()){
		debuglog("processed:{0}",assetpath);
	  saveandreimport();
	};
};