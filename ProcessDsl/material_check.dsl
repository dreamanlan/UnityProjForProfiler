input("*.mat")
{
	stringlist("pathfilter","","any path filter");
	bool("allMaterial",false);
	float("pathwidth",240){range(20,4096);};
	feature("source", "project");
	feature("menu", "2.Project Resources/Material Check");
	feature("description", "just so so");
}
filter
{
	if(stringcontainsany(assetpath, pathfilter) || allMaterial){
		$v0 = loadasset(assetpath);
		$valid = !isnull($v0);
		unloadasset($v0);
		if($valid && checkyaml(assetpath)){
			if(ispathtoolong(assetpath)){
				info = assetpath + ", path too long";
				1;
			}
			else{
				0;
			};
		}else{
			info = assetpath + ", there may be conflicts";
			1;
		};
	}else{
		0;
	};
}
process
{
};