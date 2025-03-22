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
		$v0 = getdependencies(assetpath);
		if(!findab){
			looplist($v0){
				if($$.Contains(dependencefilter)){
					$v1 = newitem();
					$v1.AssetPath = assetpath;
					$v1.Info = $$;
					$v1.Order = 0;
					$v1.Value = 0;
				};
			};
		}else{
			$v10 = all_asset_bundle_info.GetAssetBuildInfoByAssetBundleName(dependenceab);
			looplist($v0){
				$v11 = getfilenamewithoutextension($$);
				looplist($v10.assetNames){
					$v12 = getfilenamewithoutextension($$);
					if(str.Compare($v11,$v12,true)==0){
						debuglog("{0} {1}", $v11, $v12);
						$v1 = newitem();
						$v1.AssetPath = assetpath;
						$v1.Info = $$;
						$v1.Order = 0;
						$v1.Value = 0;
					};
				};
			};
		};
	};
	0;
};