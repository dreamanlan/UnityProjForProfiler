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
  if(string_contains_any(asset_path, pathfilter) && string_contains_any($v2, shaderfilter) || allMaterial){
    $v0 = load_asset(asset_path);
    if(!is_null($v0)){
      $v1 = $v0.name;
      $v2 = $v0.shader.name;
      unload_asset($v0);
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
  remove_yaml_leaf_properties(asset_path, "- vs_cbuf10_2:", "- vs_cbuf15_54:");
};