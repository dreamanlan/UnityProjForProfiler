input("*.*")
{
	string("assetfilter", "");
	string("dependencefilter", "");
	string("style", "grouplist"){
		popup(["itemlist", "grouplist"]);
	};
	float("pathwidth",240){range(20,4096);};
	feature("source", "project");
	feature("menu", "1.Project Resources/Group Dependencies");
	feature("description", "just so so");
}
filter
{
	if(assetpath.Contains(assetfilter)){
		$v0 = getdependencies(assetpath);
		looplist($v0){
			if($$.Contains(dependencefilter)){
				$v1 = newitem();
				$v1.AssetPath = $$;
				$v1.Info = assetpath;
				$v1.Order = 0;
				$v1.Value = 0;
				$v1.Group = $$;
			};
		};
	};
	0;
}
group
{
		order = count;
		value = count;
		info = format("{0}=>{1}", group, count);
	  1;
};