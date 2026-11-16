input("*.asset")
{
	stringlist("filter", "");
	stringlist("notfilter", "");
	stringlist("depfilter", "");
	stringlist("notdepfilter", "");
	float("pathwidth",240){range(20,4096);};
	feature("source", "project");
	feature("menu", "4.All Assets/Asset Dependent Resources");
	feature("description", "just so so");
}
filter
{
	if(string_contains(asset_path,filter) && string_not_contains(asset_path,notfilter)){
		$v0 = get_dependencies(asset_path);
		$v1 = list_size($v0);
		$v2 = 0;
		if($v1>0){
			loop_list($v0){
		  		if(string_contains($$,depfilter) && string_not_contains($$,notdepfilter)){
					$v1 = new_item();
					$v1.AssetPath = $$;
					$v1.Info = asset_path;
					$v1.Order = 0;
					$v1.Value = 0;
					$v2 = 1;
				};
			};
		};
		$v2;
	}else{
		0;
	};
};