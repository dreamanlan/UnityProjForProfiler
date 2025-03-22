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
	if(stringcontains(assetpath,filter) && stringnotcontains(assetpath,notfilter)){
	  $v0 = getdependencies(assetpath);
	  $v1 = listsize($v0);
	  $v2 = 0;
	  if($v1>0){
		  looplist($v0){
		  	if(stringcontains($$,depfilter) && stringnotcontains($$,notdepfilter)){
					$v1 = newitem();
					$v1.AssetPath = $$;
					$v1.Info = assetpath;
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