input("t:shader","t:visualeffect")
{
	string("filter", "");
	float("pathwidth",240){range(20,4096);};
	feature("source", "allassets");
	feature("menu", "4.All Assets/Shaders");
	feature("description", "just so so");
}
filter
{
	if(assetpath.Contains(filter)){
		$v0 = loadasset(assetpath);
		order = getshaderpropertycount($v0);
		info = format("{0} guid:{1} property count:{2} texture count:{3}", getfilename(assetpath), assetpath2guid(assetpath), order, getshaderpropertycount($v0, 4));
		1;
	}else{
		0;
	};
};