input("*.asset")
{
    stringlist("filter","");
	float("pathwidth",240){range(20,4096);};
	feature("source", "project");
	feature("menu", "1.Project Resources/Assets");
	feature("description", "just so so");
}
filter
{
	if(stringcontains(assetpath,filter)){
		$v0 = loadasset(assetpath);
		$v1 = $v0.name;
		$v2 = getruntimememory($v0);
		unloadasset($v0);
		value = $v2;
		order = $v2;
        info = "name:" + $v1 + " mem:" + $v2;
        1;
    }else{
        0;
    };
};