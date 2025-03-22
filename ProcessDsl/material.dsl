input("*.mat")
{
  string("shaderName","Standard");
  bool("allMaterial",false);
  string("replaceWithShaderName","M1Toolv5/BasicPBR"){
    popup("M1Toolv5/BasicPBR","M1Toolv5/BasicPBR");
    popup("M1Toolv5/BodySkinSilk","M1Toolv5/BodySkinSilk");
    popup("Mobile/Diffuse","Mobile/Diffuse");
  };
	feature("source", "project");
	feature("menu", "1.Project Resources/Material Replace");
	feature("description", "just so so");
}
filter
{
	$v0 = loadasset(assetpath);
	$v1 = $v0.name;
	$v2 = $v0.shader.name;
	unloadasset($v0);
	if($v2==shaderName || allMaterial){
  	info = "mat:" + $v1 + " shader:" + $v2;
  	1;
  }else{
    0;
  };
}
process
{
  $v0 = loadasset(assetpath);
  $v0.shader = gettype("Shader").Find(replaceWithShaderName);
	//unloadasset($v0);
  saveandreimport();
};