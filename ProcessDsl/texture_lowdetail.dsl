input("*.tga","*.png","*.jpg","*.exr")
{
	string("filter", "");
	string("notfilter", "");
	int("maxSize",1);
	float("pathwidth",240){range(20,4096);};
	feature("source", "project");
	feature("menu", "1.Project Resources/Texture Low Detail");
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
    	$v5 = getfilenamewithoutextension(assetpath);
    	if(($v1 > maxSize || $v2 > maxSize) && ($v3.maxTextureSize > maxSize || $v4.maxTextureSize > maxSize) && (stringtolower($v5).EndsWith("_n") || stringtolower($v5).EndsWith("_s")) && !(importer.lowDetail)){
    		info = "size:" + $v1 + "," + $v2;
    		$r = 1;
    	} else {
    		$r = 0;
    	};
    	unloadasset($v0);
		$r;
	} else {
		0;
	};
}
process
{
	importer.lowDetail = true;
  saveandreimport();
};