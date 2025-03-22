input("*.mat")
{
    stringlist("shaderNames","Standard");
	float("pathwidth",240){range(20,4096);};
	feature("source", "project");
	feature("menu", "1.Project Resources/Materials");
	feature("description", "just so so");
}
filter
{
	$v0 = loadasset(assetpath);
	$v1 = $v0.name;
	$v2 = $v0.shader.name;
	unloadasset($v0);
	if(stringcontains($v2,shaderNames)){
        info = "mat:" + $v1 + " shader:" + $v2;
        1;
    }else{
        0;
    };
};