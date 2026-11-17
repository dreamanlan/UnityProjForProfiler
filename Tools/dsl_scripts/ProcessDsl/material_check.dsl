input("*.mat")
{
	stringlist("pathfilter","","any path filter");
	bool("allMaterial",false);
	bool("checkPath",false);
	float("pathwidth",240){range(20,4096);};
	feature("source", "project");
	feature("menu", "2.Project Resources/Material Check");
	feature("description", "just so so");
}
filter
{
	if(string_contains_any(asset_path, pathfilter) || allMaterial){
		if(check_yaml(asset_path)){
			if(checkPath && is_path_too_long(asset_path)){
				info = asset_path + ", path too long";
				1;
			}
			else{
				0;
			};
		}else{
			$v0 = load_asset(asset_path);
			$valid = !is_null($v0);
			unload_asset($v0);
			info = asset_path + ", there may be conflicts, valid:" + $valid;
			1;
		};
	}else{
		0;
	};
}
process
{
};