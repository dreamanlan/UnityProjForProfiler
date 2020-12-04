input("*.tga","*.png","*.jpg","*.exr")
{
	stringlist("filter", "");
	stringlist("notfilter", "/Select/");
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
    	var(0) = loadasset(assetpath);
    	var(1) = var(0).width;
    	var(2) = var(0).height;
		  var(3) = gettexturesetting("iPhone");
    	var(4) = gettexturesetting("Android");
    	//unloadasset(var(0));
    	if((var(1) > maxSize || var(2) > maxSize) && (var(3).maxTextureSize > maxSize || var(4).maxTextureSize > maxSize)){
    		info = format("size:{0},{1} filter:{2}", var(1), var(2), importer.filterMode);
    		1;
    	} else {
    		0;
    	};
    }else{
        0;  
    };
}
process
{
    importer.filterMode = parseenum('FilterMode', filterMode);
    saveandreimport();
};