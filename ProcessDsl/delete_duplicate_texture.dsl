input("*.tga","*.png","*.jpg")
{
	int("maxSize",256){
		range(1,1024);
	};
	string("prop",""){
		multiple(["readable","mipmap"],[1,2]);
	};
	string("filter", "");
	string("style", "itemlist"){
		popup(["itemlist", "grouplist"]);
	};
	feature("source", "project");
	feature("menu", "2.Project Resources/Delete Duplicate Texture");
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
		order = $v1 < $v2 ? $v2 : $v1;
		if(($v1 > maxSize || $v2 > maxSize) && assetpath.Contains(filter) && (prop.Contains("1") && $v3 || !prop.Contains("1")) && (prop.Contains("2") && $v4 || !prop.Contains("2"))){
			info = format("{0} size:{1},{2}", assetpath, $v1, $v2);
			value = calcassetsize(assetpath);
			group = format("{0}|{1}", value, calcassetmd5(assetpath));
			$r = 1;
		} else {
			$r = 0;
		};
		unloadasset($v0);
		$r;
	};
}
group
{
	$v0 = calcrefbycount(assetpath);
	if(count>1 && $v0<=0){
		info = format("{0} count:{1}", group, count);
		1;
	}else{
		0;
	};
}
process
{
	deleteasset(assetpath);
};