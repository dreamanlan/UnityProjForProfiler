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
	if(assetpath.Contains(assetfilter)){
		$v0 = getdependencies(assetpath);
		looplist($v0){
			if($$.Contains(dependencefilter)){
				$v1 = newitem();
				$v1.AssetPath = assetpath;
				$v1.Info = $$;
				$v1.Order = 0;
				$v1.Value = 0;
			};
		};
	};
	0;
};