input("*.fbx","*.FBX")
{
	stringlist("filter", "");
	stringlist("notfilter", "");
	string("style", "itemlist"){
		popup(["itemlist", "grouplist"]);
	};
	float("pathwidth",240){range(20,4096);};
	feature("source", "project");
	feature("menu", "1.Project Resources/Duplicate Fbx Name");
	feature("description", "just so so");
}
filter
{
	$v0 = load_asset(asset_path);
	if(is_null($v0)){
		$r = 0;
	} else {
		if(string_contains(asset_path,filter) && string_not_contains(asset_path,notfilter)){
			info = format("{0} guid:{1}", asset_path, asset_path_2_guid(asset_path));
			order = value;
			value = calc_asset_size(asset_path);
			group = get_file_name(asset_path);
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
		order = count;
		info = format("{0} count:{1} ref by count:{2}", group, count, calc_ref_by_count(asset_path));
		1;
	}else{
		0;
	};
};