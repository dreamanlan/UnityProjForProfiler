input("*.asset")
{
	int("maxSize",256){
		range(1,1024);
	};
	string("prop",""){
		multiple(["readable","mipmap"],[1,2]);
	};
	string("filterMode","Trilinear"){
		popup(["Any", "Point","Bilinear","Trilinear"]);
	};
    stringlist("filter","");
	float("pathwidth",240){range(20,4096);};
	feature("source", "project");
	feature("menu", "1.Project Resources/Texture Assets");
	feature("description", "just so so");
}
filter
{
	if(stringcontains(assetpath,filter)){
		$v0 = loadasset(assetpath);
		if(isnull($v0)){
    		0;
    	} else {
    		$v1 = $v0.width;
    		$v2 = $v0.height;
    		$v3 = $v0.isReadable;
    		$v4 = $v0.mipmapCount;
			$v5 = $v0.filterMode;
			$v6 = getruntimememory($v0);
    		order = $v1 < $v2 ? $v2 : $v1;
			value = $v6;
    		if(($v1 > maxSize || $v2 > maxSize) && (filterMode=="Any" || $v5==parseenum("FilterMode", filterMode)) && (prop.Contains("1") && $v3 || !prop.Contains("1")) && (prop.Contains("2") && $v4 > 1 || !prop.Contains("2"))){
    			info = format("size:{0},{1} readable:{2} mipmap:{3} filter:{4} refby_count:{5} memory:{6}", $v1, $v2, $v3, $v4, $v5, calcrefbycount(assetpath), $v6);
    			$r = 1;
    		} else {
    			$r = 0;
    		};
    		unloadasset($v0);
			$r;
    	};
    }else{
        0;
    };
};