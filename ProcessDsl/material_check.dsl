input("*.mat")
{
	stringlist("pathfilter","","any path filter");
	bool("allMaterial",false);
	bool("checkPath",false);
	float("pathwidth",240){range(20,4096);};
	feature("source", "project");
	feature("menu", "2.Project Resources/Material Check");
	feature("description", "just so so");
}
filter
{
	if(stringcontainsany(assetpath, pathfilter) || allMaterial){
		if(checkyaml(assetpath)){
			if(checkPath && ispathtoolong(assetpath)){
				info = assetpath + ", path too long";
				1;
			}
			else{
				0;
			};
		}else{
			$v0 = loadasset(assetpath);
			$valid = !isnull($v0);
			unloadasset($v0);
			info = assetpath + ", there may be conflicts, valid:" + $valid;
			1;
		};
	}else{
		0;
	};
}
process
{
};