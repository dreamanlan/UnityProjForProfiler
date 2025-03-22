input("*.tga","*.png","*.jpg")
{
	int("maxSize",256){
		range(1,1024);
	};
	string("prop",""){
		multiple(["readable","mipmap"],[1,2]);
	};
	string("filter", "");
	string("notfilter", "");
	string("style", "itemlist"){
		popup(["itemlist", "grouplist"]);
	};
	int("duptype",1){
		toggle(["md5","guid"],[1,2]);
	};
	float("pathwidth",240){range(20,4096);};
	feature("source", "project");
	feature("menu", "1.Project Resources/Duplicate Textures");
	feature("description", "just so so");
}
filter
{
	$v0 = loadasset(assetpath);
	if(isnull($v0)){
		0;
	} else {;
		$v1 = $v0.width;
		$v2 = $v0.height;
		$v3 = importer.isReadable;
		$v4 = importer.mipmapEnabled;
		//unloadasset($v0);
		order = $v1 < $v2 ? $v2 : $v1;
		if(($v1 > maxSize || $v2 > maxSize) && assetpath.Contains(filter) && (isnullorempty(notfilter) || !assetpath.Contains(notfilter)) && (prop.Contains("1") && $v3 || !prop.Contains("1")) && (prop.Contains("2") && $v4 || !prop.Contains("2"))){
			info = format("{0} size:{1},{2} guid:{3}", assetpath, $v1, $v2, assetpath2guid(assetpath));
			value = calcassetsize(assetpath);
			if(duptype==1){
				group = format("{0}|{1}", value, calcassetmd5(assetpath));
			}else{
				group = format("{0}", assetpath2guid(assetpath));
			};
			1;
		} else {
			0;
		};
	};
}
group
{
	if(count>1){
		info = format("{0} count:{1} ref by count:{2}", group, count, calcrefbycount(assetpath));
		1;
	}else{
		0;
	};
};