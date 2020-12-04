input
{
	string("assetbundle", ""){
		asset_bundle_name;
	};
	string("assetfilter", "");
	string("dependencefilter", "");
	bool("findab", false);
	string("dependenceab", ""){
		asset_bundle_name;
	};
	float("pathwidth",240){range(20,4096);};
	feature("source", "assetbundle");
	feature("menu", "0.AssetBundle/Dependencies");
	feature("description", "just so so");
}
filter
{
	str = gettype("System.String");
	if(assetpath.Contains(assetfilter)){
		var(0) = getdependencies(assetpath);
		if(!findab){
			looplist(var(0)){		
				if($$.Contains(dependencefilter)){
					var(1) = newitem();
					var(1).AssetPath = assetpath;
					var(1).Info = $$;
					var(1).Order = 0;
					var(1).Value = 0;
				};
			};
		}else{
			var(10) = all_asset_bundle_info.GetAssetBuildInfoByAssetBundleName(dependenceab);
			looplist(var(0)){		
				var(11) = getfilenamewithoutextension($$);
				looplist(var(10).assetNames){
					var(12) = getfilenamewithoutextension($$);
					if(str.Compare(var(11),var(12),true)==0){
						debuglog("{0} {1}", var(11), var(12));
						var(1) = newitem();
						var(1).AssetPath = assetpath;
						var(1).Info = $$;
						var(1).Order = 0;
						var(1).Value = 0;
					};
				};
			};
		};
	};
	0;
};