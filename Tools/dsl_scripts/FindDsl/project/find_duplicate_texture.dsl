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
	$v0 = load_asset(asset_path);
	if(is_null($v0)){
		$r = 0;
	} else {;
		$v1 = $v0.width;
		$v2 = $v0.height;
		$v3 = importer.isReadable;
		$v4 = importer.mipmapEnabled;
		order = $v1 < $v2 ? $v2 : $v1;
		if(($v1 > maxSize || $v2 > maxSize) && asset_path.Contains(filter) && (is_null_or_empty(notfilter) || !asset_path.Contains(notfilter)) && (prop.Contains("1") && $v3 || !prop.Contains("1")) && (prop.Contains("2") && $v4 || !prop.Contains("2"))){
			info = format("{0} size:{1},{2} guid:{3}", asset_path, $v1, $v2, asset_path_2_guid(asset_path));
			value = calc_asset_size(asset_path);
			if(duptype==1){
				group = format("{0}|{1}", value, calc_asset_md5(asset_path));
			}else{
				group = format("{0}", asset_path_2_guid(asset_path));
			};
			$r = 1;
		} else {
			$r = 0;
		};
	};
	unload_asset($v0);
	$r;
}
group
{
	if(count>1){
		info = format("{0} count:{1} ref by count:{2}", group, count, calc_ref_by_count(asset_path));
		1;
	}else{
		0;
	};
};