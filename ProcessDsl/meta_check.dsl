input("*.meta")
{
	stringlist("pathfilter","","any path filter");
	float("pathwidth",240){range(20,4096);};
	feature("source", "project");
	feature("menu", "2.Project Resources/Meta Check");
	feature("description", "just so so");
}
filter
{
	if(stringcontainsany(assetpath, pathfilter)){
		if(checkyaml(assetpath)){
			if(ispathtoolong(assetpath)){
				info = assetpath + ", path too long";
				1;
			}
			else{
				0;
			};
		}else{
			info = assetpath + " may be exist conflict";
			1;
		};
	}else{
		0;
	};
}
process
{
};