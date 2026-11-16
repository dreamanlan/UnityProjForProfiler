input
{
	stringlist("abfilter", "");
	stringlist("assetfilter", "");
	stringlist("depfilter", "");
	float("pathwidth",240){range(20,4096);};
	feature("source", "assetbundle");
	feature("menu", "0.AssetBundle/Find AB");
	feature("description", "just so so");
}
filter
{
	if(string_contains(asset_path, abfilter)){
	    $v0 = asset_bundle_info.assetNames;
	    $v1 = asset_bundle_info.dependencies;
	    loop_list($v0){
	        $v2 = $$;
	        if(string_contains($v2, assetfilter)){
	            $v10 = new_item();
	            $v10.AssetPath = asset_path;
				$v10.Info = "asset:"+$v2;
				$v10.Order = 0;
				$v10.Value = 0;
	        };
	    };
	    loop_list($v1){
	        $v2 = $$;
	        if(string_contains($v2, assetfilter)){
	            $v10 = new_item();
	            $v10.AssetPath = asset_path;
				$v10.Info = "dep:"+$v2;
				$v10.Order = 0;
				$v10.Value = 0;
	        };
	    };
	};
	0;
};