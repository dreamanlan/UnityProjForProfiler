input("*.prefab")
{
	stringlist("filter", "");
	stringlist("notfilter", "");
	stringlist("depfilter", "");
	stringlist("notdepfilter", "");
	float("pathwidth",240){range(20,4096);};
	feature("source", "project");
	feature("menu", "4.All Assets/Prefab Dependent Resources");
	feature("description", "just so so");
}
filter
{
	if(stringcontains(assetpath,filter) && stringnotcontains(assetpath,notfilter)){
	  var(0) = getdependencies(assetpath);
	  var(1) = listsize(var(0));
	  var(2) = 0;
	  if(var(1)>0){
		  looplist(var(0)){
		  	if(stringcontains($$,depfilter) && stringnotcontains($$,notdepfilter)){
					var(1) = newitem();
					var(1).AssetPath = $$;
					var(1).Info = assetpath;
					var(1).Order = 0;
					var(1).Value = 0;
					var(2) = 1;
				};
		  };
		};
		var(2);
	}else{
		0;
	};
};