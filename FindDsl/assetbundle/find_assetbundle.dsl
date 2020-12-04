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
	    var(0) = asset_bundle_info.assetNames;
	    var(1) = asset_bundle_info.dependencies;
	    looplist(var(0)){
	        var(2) = $$;
	        if(stringcontains(var(2), assetfilter)){
	            var(10) = newitem();
	            var(10).AssetPath = assetpath;
				var(10).Info = "asset:"+var(2);
				var(10).Order = 0;
				var(10).Value = 0;
	        };
	    };
	    looplist(var(1)){
	        var(2) = $$;
	        if(stringcontains(var(2), assetfilter)){
	            var(10) = newitem();
	            var(10).AssetPath = assetpath;
				var(10).Info = "dep:"+var(2);
				var(10).Order = 0;
				var(10).Value = 0;
	        };	        
	    };
	};
	0;
};