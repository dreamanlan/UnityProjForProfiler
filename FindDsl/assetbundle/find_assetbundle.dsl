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
	if(stringcontains(assetpath, abfilter)){
	    $v0 = asset_bundle_info.assetNames;
	    $v1 = asset_bundle_info.dependencies;
	    looplist($v0){
	        $v2 = $$;
	        if(stringcontains($v2, assetfilter)){
	            $v10 = newitem();
	            $v10.AssetPath = assetpath;
				$v10.Info = "asset:"+$v2;
				$v10.Order = 0;
				$v10.Value = 0;
	        };
	    };
	    looplist($v1){
	        $v2 = $$;
	        if(stringcontains($v2, assetfilter)){
	            $v10 = newitem();
	            $v10.AssetPath = assetpath;
				$v10.Info = "dep:"+$v2;
				$v10.Order = 0;
				$v10.Value = 0;
	        };
	    };
	};
	0;
};