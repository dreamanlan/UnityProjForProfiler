input("*.mat")
{
  stringlist("pathfilter","","any path filter");
  stringlist("shaderfilter","Cloud","any shader filter");
  bool("allMaterial",false);
	float("pathwidth",240){range(20,4096);};
	feature("source", "project");
	feature("menu", "2.Project Resources/Material Clean");
	feature("description", "just so so");
}
filter
{
  $v0 = loadasset(assetpath);
  if(!isnull($v0)){
      $v1 = $v0.name;
      $v2 = $v0.shader.name;
      unloadasset($v0);
      if(stringcontainsany(assetpath, pathfilter) && stringcontainsany($v2, shaderfilter) || allMaterial){
        info = "mat:" + $v1 + " shader:" + $v2;
        1;
      }else{
        0;
      };
  }else{
    0;
  };
}
process
{
  removeyamlleafproperties(assetpath, "- vs_cbuf10_2:", "- vs_cbuf15_54:");
};