input("*.tga","*.png","*.jpg","*.exr")
{
	stringlist("filter", "");
	stringlist("notfilter", "");
	int("maxSize",0);
	string("oldFilterMode","Trilinear"){
		popup(["Point","Bilinear","Trilinear"]);
	};
	string("filterMode","Bilinear"){
		popup(["Point","Bilinear","Trilinear"]);
	};
	float("pathwidth",240){range(20,4096);};
	feature("source", "project");
	feature("menu", "1.Project Resources/Texture Filter Mode");
	feature("description", "just so so");
}
filter
{
    if(stringcontains(assetpath, filter) && stringnotcontains(assetpath, notfilter) && importer.filterMode==parseenum("FilterMode", oldFilterMode)){
    	$v0 = loadasset(assetpath);
    	$v1 = $v0.width;
    	$v2 = $v0.height;
		$v3 = gettexturesetting("iPhone");
    	$v4 = gettexturesetting("Android");
    	if(($v1 > maxSize || $v2 > maxSize) && ($v3.maxTextureSize > maxSize || $v4.maxTextureSize > maxSize)){
    		info = format("size:{0},{1} filter:{2}", $v1, $v2, importer.filterMode);
    		$r = 1;
    	} else {
    		$r = 0;
    	};
    	unloadasset($v0);
		$r;
    }else{
        0;
    };
}
process
{
    importer.filterMode = parseenum('FilterMode', filterMode);
    saveandreimport();
};