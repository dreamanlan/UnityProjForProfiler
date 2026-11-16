input("*.*")
{
	string("assetfilter", "");
	string("dependencefilter", "");
	float("pathwidth",240){range(20,4096);};
	feature("source", "project");
	feature("menu", "1.Project Resources/Dependencies");
	feature("description", "just so so");
}
filter
{
	if(asset_path.Contains(assetfilter)){
		$v0 = get_dependencies(asset_path);
		loop_list($v0){
			if($$.Contains(dependencefilter)){
				$v1 = new_item();
				$v1.AssetPath = asset_path;
				$v1.Info = $$;
				$v1.Order = 0;
				$v1.Value = 0;
			};
		};
	};
	0;
};