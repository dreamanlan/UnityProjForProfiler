input("*.tga","*.png","*.jpg","*.exr","*.hdr")
{
	int("maxSize",256){
		range(1,1024);
	};
	string("prop",""){
		multiple(["readable","mipmap"],[1,2]);
	};
	stringlist("filter", "", "contains any");
	stringlist("filter2", "", "and contains any");
	stringlist("notfilter", "", "not contains");
	float("pathwidth",240){range(20,4096);};
	feature("source", "sceneassets");
	feature("menu", "2.Current Scene Resources/Textures");
	feature("description", "just so so");
}
filter
{
	if(order<1){
		@hash = hashtable();
	};
	if(hashtableget(@hash, assetpath, false)){
		$r = 0;
	}
	else{
		hashtableadd(@hash, assetpath, true);
		$v0 = loadasset(assetpath);
		if(isnull($v0)){
			$r = 0;
		} else {
			$v1 = $v0.width;
			$v2 = $v0.height;
			$v3 = importer.isReadable;
			$v4 = importer.mipmapEnabled;
			$v5 = gettexturestorage($v0);
			$v6 = gettexturememory($v0);
			order = $v6;
			value = $v6/1024.0/1024.0;
			if(($v1 > maxSize || $v2 > maxSize) && stringcontainsany(assetpath, filter) && stringcontainsany(assetpath, filter2) && stringnotcontains(assetpath, notfilter) && (prop.Contains("1") && $v3 || !prop.Contains("1")) && (prop.Contains("2") && $v4 || !prop.Contains("2"))){
				info = format("size:{0},{1} readable:{2} mipmap:{3} storage:{4:f3}mb runtime memory:{5:f3}mb", $v1, $v2, $v3, $v4, $v5/1024.0/1024.0, $v6/1024.0/1024.0);
				$r = 1;
			} else {
				$r = 0;
			};
		};
	};
	unloadasset($v0);
	$r;
};