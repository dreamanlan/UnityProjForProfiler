input("*.meta")
{
	stringlist("pathfilter","","any path filter");
	bool("checkPath",false);
	float("pathwidth",240){range(20,4096);};
	feature("source", "project");
	feature("menu", "2.Project Resources/Meta Check");
	feature("description", "just so so");
}
filter
{
	if(string_contains_any(asset_path, pathfilter)){
		if(check_yaml(asset_path)){
			if(checkPath && is_path_too_long(asset_path)){
				info = asset_path + ", path too long";
				1;
			}
			else{
				0;
			};
		}else{
			info = asset_path + ", there may be conflicts";
			1;
		};
	}else{
		0;
	};
}
process
{
};