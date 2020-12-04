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
	var(0) = loadasset(assetpath);
	var(1) = var(0).name;
	var(2) = var(0).shader.name;
	unloadasset(var(0));
	if(var(2)==shaderName || allMaterial){
  	info = "mat:" + var(1) + " shader:" + var(2);
  	1;
  }else{
    0;
  };
}
process
{
  var(0) = loadasset(assetpath);
  var(0).shader = gettype("Shader").Find(replaceWithShaderName);
	//unloadasset(var(0));
  saveandreimport();
};